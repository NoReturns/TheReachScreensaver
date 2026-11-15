from pathlib import Path
from collections import defaultdict
from datetime import datetime
import json
import re
import statistics

import cv2
import numpy as np
from PIL import Image, ImageDraw

base = Path(__file__).parent / 'r17-runs'
reports = {}
for path in base.glob('run-*/r17-manifest.json'):
    manifest = json.loads(path.read_text(encoding='utf-8'))
    if manifest.get('exitCode') != 0:
        continue
    metrics = list(path.parent.glob('metrics/r17-*.json'))
    if len(metrics) != 1:
        continue
    key = (manifest['scenario'], manifest['variant'])
    if key in reports:
        raise RuntimeError(f'duplicate successful R17 run: {key}')
    reports[key] = (path.parent, manifest, json.loads(metrics[0].read_text(encoding='utf-8')))

pid_pattern = re.compile(r'pid_(\d+)_')
summary = {'schema': 1, 'runs': {}, 'pairs': {}}
for (scenario, variant), (run, manifest, metrics) in reports.items():
    pids = {metrics['hostPid']} | {p['pid'] for p in metrics['processes']}
    samples = []
    for line in (run / 'counter-samples.jsonl').read_text(encoding='utf-8').splitlines():
        entry = json.loads(line)
        utc_ms = datetime.fromisoformat(entry['utc'].replace('Z', '+00:00')).timestamp() * 1000
        if not metrics['epoch'] + 10000 <= utc_ms <= metrics['epoch'] + 59000:
            continue
        dedicated = sum(c['value'] for c in entry.get('counters', [])
                        if 'dedicated usage' in c['path'].lower()
                        and (match := pid_pattern.search(c['path']))
                        and int(match.group(1)) in pids)
        adapter = entry.get('nvidia', '').split(',')
        if len(adapter) == 3 and dedicated > 0:
            samples.append({'elapsedSeconds': (utc_ms - metrics['epoch']) / 1000,
                            'dedicatedMiB': dedicated / 1048576,
                            'adapterMiB': float(adapter[0]),
                            'gpuUtilPercent': float(adapter[1])})
    windows = {}
    for name, data in metrics['summaries'].items():
        intervals = np.array(data['intervals'], dtype=float)
        windows[name] = {'callbacks': data['callbacks'], 'captures': data['captured'],
                         'overflow': data['overflow'], 'errors': data['errors'],
                         'contextLost': data['contextLost'],
                         'intervalMedianMs': float(np.median(intervals)),
                         'intervalP95Ms': float(np.percentile(intervals, 95)),
                         'intervalP99Ms': float(np.percentile(intervals, 99))}
    summary['runs'][f'{scenario}/{variant}'] = {
        'path': str(run), 'stateKey': metrics['stateKey'],
        'frames': len(metrics['frames']), 'counterSamples': len(samples),
        'targets': {status['fixture']['display']:
                    {'centersDeg': status['fixture']['cylindrical']['sectorCentersDeg'],
                     'sectorTargets': status['fixture']['cylindrical']['sectorTargets']}
                    for status in metrics['statuses']},
        'dedicatedMedianMiB': statistics.median(s['dedicatedMiB'] for s in samples) if samples else None,
        'dedicatedFirstTenMiB': statistics.median(s['dedicatedMiB'] for s in samples
                                                  if s['elapsedSeconds'] < 20) if samples else None,
        'dedicatedLastTenMiB': statistics.median(s['dedicatedMiB'] for s in samples
                                                 if s['elapsedSeconds'] > 49) if samples else None,
        'adapterMedianMiB': statistics.median(s['adapterMiB'] for s in samples) if samples else None,
        'gpuUtilMedianPercent': statistics.median(s['gpuUtilPercent'] for s in samples) if samples else None,
        'windows': windows}

def image_for(frame):
    crop = frame['crop']
    raw = Path(frame['path']).read_bytes()
    return Image.frombytes('RGBA', (crop['width'], crop['height']), raw).transpose(Image.Transpose.FLIP_TOP_BOTTOM).convert('RGB')

review = base / 'review'
review.mkdir(exist_ok=True)
for scenario in ('planet', 'planetjoin', 'planetjoinaligned', 'stars', 'hole'):
    if not all((scenario, v) in reports for v in ('unbounded', 'cap2433')):
        continue
    control = reports[(scenario, 'unbounded')][2]
    candidate = reports[(scenario, 'cap2433')][2]
    if control['stateKey'] != candidate['stateKey']:
        raise RuntimeError(f'fixture state differs: {scenario}')
    def indexed(report):
        return {(f['display'].split('DISPLAY')[-1], f['second']): f for f in report['frames']}
    left_a = indexed(control)
    left_b = indexed(candidate)
    if left_a.keys() != left_b.keys():
        raise RuntimeError(f'capture timestamps differ: {scenario}')
    for key in left_a:
        a, b = left_a[key], left_b[key]
        if a['virtualMs'] != b['virtualMs'] or a['yawDegrees'] != b['yawDegrees'] or a['crop'] != b['crop']:
            raise RuntimeError(f'capture scene state differs: {scenario} {key}')
    pair = {'stateKey': control['stateKey'], 'frameKeys': len(left_a),
            'left': [], 'primary': []}
    for display, keyname in (('1', 'left'), ('3', 'primary')):
        keys = sorted((k for k in left_a if k[0] == display), key=lambda k: k[1])
        if not keys:
            continue
        writer = None
        for key in keys:
            a, b = left_a[key], left_b[key]
            im_a, im_b = image_for(a), image_for(b)
            combined = Image.new('RGB', (im_a.width * 2, im_a.height + 36))
            combined.paste(im_a, (0, 36)); combined.paste(im_b, (im_a.width, 36))
            draw = ImageDraw.Draw(combined)
            draw.text((8, 8), f'{scenario} {keyname} t={a["second"]}s yaw={a["yawDegrees"]:.6f} control', fill='white')
            draw.text((im_a.width + 8, 8), '2433x2007 candidate', fill='white')
            rgb = np.asarray(combined)
            if writer is None:
                out = review / f'{scenario}-{keyname}-paired.mp4'
                writer = cv2.VideoWriter(str(out), cv2.VideoWriter_fourcc(*'mp4v'),
                                         4, (combined.width, combined.height))
                if not writer.isOpened():
                    raise RuntimeError(f'cannot create review sequence: {out}')
            writer.write(cv2.cvtColor(rgb, cv2.COLOR_RGB2BGR))
            if a['second'] in (1, 15, 30, 45, 59):
                im_a.save(review / f'{scenario}-{keyname}-control-{a["second"]:02}.png')
                im_b.save(review / f'{scenario}-{keyname}-candidate-{a["second"]:02}.png')
                combined.save(review / f'{scenario}-{keyname}-paired-{a["second"]:02}.png')
            pair[keyname].append({'second': a['second'], 'virtualMs': a['virtualMs'],
                                  'yawDegrees': a['yawDegrees'], 'control': a['path'],
                                  'candidate': b['path'], 'crop': a['crop'],
                                  'readbackMs': [a['readEndMs'] - a['readStartMs'],
                                                b['readEndMs'] - b['readStartMs']]})
        writer.release()
    if scenario == 'stars':
        star = {'control': [], 'candidate': []}
        for second in range(1, 60):
            if ('1', second) not in left_a:
                continue
            control_image = np.asarray(image_for(left_a[('1', second)]), dtype=float).mean(axis=2)
            candidate_image = np.asarray(image_for(left_b[('1', second)]), dtype=float).mean(axis=2)
            ry, rx = np.unravel_index(np.argmax(control_image[115:160, 470:530]), (45, 60))
            y, x = int(ry + 115), int(rx + 470)
            for label, frame in (('control', control_image), ('candidate', candidate_image)):
                patch = frame[y-8:y+9, x-8:x+9]
                star[label].append({'second': second, 'x': x, 'y': y,
                                    'patchFlux': float(patch.sum()),
                                    'pixelsAbove10': int((patch > 10).sum()),
                                    'cropFlux': float(frame.sum())})
        pair['starDiagnostic'] = {'region': [470, 115, 60, 45],
                                  'patch': '17x17 around control peak in region',
                                  'threshold': 'mean RGB > 10, descriptive only',
                                  'samples': star}
    if scenario == 'planetjoinaligned':
        writer = None
        for second in range(1, 60):
            key = ('1', second)
            primary = ('3', second)
            if key not in left_a or primary not in left_a:
                continue
            joins = []
            for entries in (left_a, left_b):
                left = image_for(entries[key]).crop((880, 0, 1200, 720))
                right = image_for(entries[primary])
                stitched = Image.new('RGB', (640, 720))
                stitched.paste(left); stitched.paste(right, (320, 0))
                joins.append(stitched)
            combined = Image.new('RGB', (1280, 720))
            combined.paste(joins[0]); combined.paste(joins[1], (640, 0))
            if writer is None:
                out = review / 'planet-join-stitched-paired.mp4'
                writer = cv2.VideoWriter(str(out), cv2.VideoWriter_fourcc(*'mp4v'),
                                         4, (1280, 720))
                if not writer.isOpened():
                    raise RuntimeError(f'cannot create join video: {out}')
            writer.write(cv2.cvtColor(np.asarray(combined), cv2.COLOR_RGB2BGR))
            if second in (1, 15, 30, 45, 50, 52, 54, 59):
                combined.save(review / f'planet-join-stitched-paired-{second:02}.png')
        if writer is not None:
            writer.release()
    summary['pairs'][scenario] = pair

(base / 'R17_ANALYSIS.json').write_text(json.dumps(summary, indent=2), encoding='utf-8')
print(base / 'R17_ANALYSIS.json')
