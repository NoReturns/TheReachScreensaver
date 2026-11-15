import hashlib
import json
import re
from datetime import datetime
from pathlib import Path

import numpy as np
from PIL import Image, ImageDraw

root = Path(__file__).parent / 'r15-runs'
runs = {}
for manifest_path in root.glob('run-*/r15-manifest.json'):
    item = json.loads(manifest_path.read_text())
    runs[(item['variant'], item['case'])] = item

cases = ['hole-yaw0', 'hole-yaw-6', 'hole-yaw-plus', 'peripheral', 'deep-space']
variants = ['4x', '2x', '1x']
out = {'captures': {}, 'measurements': {}}
montage = Image.new('RGB', (1800, 3120), (20, 20, 24))
draw = ImageDraw.Draw(montage)
for row, case in enumerate(cases):
    records = {}
    for variant in variants:
        item = runs[(variant, case)]
        run = Path(item['run'])
        fixture = json.loads(next((run / 'metrics').glob('fixture-*.json')).read_text())
        r13 = json.loads(next((run / 'metrics').glob('r13-*.json')).read_text())
        assert len(fixture['captures']) == 3
        assert all(r['measurement']['glError'] == 0 and not r['measurement']['contextLost']
                   for r in r13['records'])
        full = Image.new('RGB', (8320, 2160))
        for capture in fixture['captures']:
            path = Path(capture['path'])
            assert hashlib.sha256(path.read_bytes()).hexdigest().upper() == capture['sha256'].upper()
            display = next(d for d in fixture['layout']['displays'] if d['Name'] == capture['display'])
            b = display['Bounds']
            with Image.open(path) as im:
                assert im.size == (b['Width'], b['Height'])
                full.paste(im.convert('RGB'), (b['X'] + 3840, b['Y'] + 720))
        samples = sorted({target[2] for status in fixture['statuses']
                          for target in status['fixture']['cylindrical']['sectorTargets']})
        dims = sorted({tuple(target[:2]) for status in fixture['statuses']
                       for target in status['fixture']['cylindrical']['sectorTargets']})
        records[variant] = {'image': full, 'stateKey': fixture['stateKey'],
                            'samples': samples, 'dimensions': dims,
                            'draws': sum(r['measurement']['drawCalls'][0] for r in r13['records']),
                            'run': str(run)}
    assert len({r['stateKey'] for r in records.values()}) == 1
    assert len({tuple(r['dimensions']) for r in records.values()}) == 1
    assert [records[v]['samples'] for v in variants] == [[4], [2], [0]]
    box = ((3540, 850, 4140, 1450) if case.startswith('hole') else
           (2650, 570, 3250, 1170) if case == 'peripheral' else
           (800, 400, 1400, 1000))
    for col, variant in enumerate(variants):
        montage.paste(records[variant]['image'].crop(box), (col * 600, row * 624 + 24))
        draw.text((col * 600 + 6, row * 624 + 5), f'{case} {variant}', fill='white')
    base = np.asarray(records['4x']['image'])
    active = np.max(base, axis=2) > 8
    seams = {}
    for variant in variants:
        image = np.asarray(records[variant]['image'])
        def seam(x, top, bottom):
            left = image[top:bottom, x - 1, :].astype(np.float32)
            right = image[top:bottom, x, :].astype(np.float32)
            step = np.abs((left - right) @ np.array([.2126, .7152, .0722]))
            return {'meanEncodedLumaStep': round(float(np.mean(step)), 3),
                    'rowsAbove20': int(np.sum(step > 20))}
        seams[variant] = {'leftPrimary': seam(3840, 720, 2160),
                          'primaryRight': seam(6400, 1080, 2160)}
    comparisons = {}
    for variant in variants[1:]:
        image = np.asarray(records[variant]['image'])
        difference = np.abs(base.astype(np.int16) - image.astype(np.int16))
        per_pixel = np.max(difference, axis=2)
        compared = per_pixel[active]
        comparisons[variant] = {
            'meanMaxRgbDifferenceActive': round(float(np.mean(compared)), 4),
            'p99MaxRgbDifferenceActive': round(float(np.percentile(compared, 99)), 2),
            'activePixels': int(active.sum()),
            'activePixelsChangedAbove8Percent': round(float(np.mean(compared > 8) * 100), 4),
            'allPixelsChangedAbove8Percent': round(float(np.mean(per_pixel > 8) * 100), 5),
        }
    out['captures'][case] = {'stateKeySha256': hashlib.sha256(records['4x']['stateKey'].encode()).hexdigest(),
                             'dimensions': records['4x']['dimensions'],
                             'samples': {v: records[v]['samples'] for v in variants},
                             'draws': {v: records[v]['draws'] for v in variants},
                             'seams': seams,
                             'runs': {v: records[v]['run'] for v in variants},
                             'comparisonTo4x': comparisons}

montage_path = root / 'R15_CROP_MONTAGE.png'
montage.save(montage_path)
pid_pattern = re.compile(r'pid_(\d+)_')
for variant in variants:
    item = runs[(variant, 'moving-hole')]
    run = Path(item['run'])
    report = json.loads(next((run / 'metrics').glob('r14-*.json')).read_text())
    pids = {report['hostPid']} | {p['pid'] for s in report['snapshots'] for p in s['processes']}
    start = datetime.fromisoformat(report['startUtc'].replace('Z', '+00:00'))
    rows = []
    for line in (run / 'counter-samples.jsonl').read_text().splitlines():
        sample = json.loads(line)
        t = datetime.fromisoformat(sample['utc'].replace('Z', '+00:00'))
        age = (t - start).total_seconds()
        if not 10 <= age <= 40:
            continue
        dedicated = sum(c['value'] for c in sample.get('counters', [])
                        if 'dedicated usage' in c['path'].lower()
                        and (match := pid_pattern.search(c['path'])) and int(match.group(1)) in pids)
        parts = sample.get('nvidia', '').split(',')
        if len(parts) == 3:
            rows.append({'dedicatedMiB': dedicated / 1048576,
                         'adapterMiB': float(parts[0]), 'gpuPercent': float(parts[1])})
    assert len(rows) >= 10
    windows = []
    for record in report['records']:
        assert record['phase'] == 'done' and not record['contextLost'] and record['glError'] == 0
        windows.append({'display': record['display'], 'frames': record['measuredFrames'],
                        'intervalMedianMs': round(float(np.median(record['intervalsMs'])), 2),
                        'intervalP95Ms': round(float(np.percentile(record['intervalsMs'], 95)), 2),
                        'intervalP99Ms': round(float(np.percentile(record['intervalsMs'], 99)), 2)})
    out['measurements'][variant] = {'run': str(run), 'samples': len(rows),
                                    'dedicatedMedianMiB': round(float(np.median([r['dedicatedMiB'] for r in rows])), 2),
                                    'adapterMedianMiB': round(float(np.median([r['adapterMiB'] for r in rows])), 2),
                                    'gpuMedianPercent': round(float(np.median([r['gpuPercent'] for r in rows])), 2),
                                    'windows': windows}

(root / 'summary.json').write_text(json.dumps(out, indent=2))
print(root / 'summary.json')
print(montage_path)
