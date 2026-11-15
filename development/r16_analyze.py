import hashlib
import json
import re
from datetime import datetime
from pathlib import Path

import numpy as np
from PIL import Image, ImageDraw

root = Path(__file__).parent / 'r16-runs'
runs = {}
for path in root.glob('run-*/r16-manifest.json'):
    item = json.loads(path.read_text())
    runs[(item['variant'], item['case'])] = item
variants = ['unbounded', 'cap2433', 'cap1622']
cases = ['hole-yaw0', 'hole-yaw-6', 'hole-yaw-plus', 'peripheral', 'deep-space']
summary = {'captures': {}, 'measurements': {}, 'motion': {}}
montage = Image.new('RGB', (1800, 3120), (20, 20, 24))
draw = ImageDraw.Draw(montage)
for row, case in enumerate(cases):
    records = {}
    for variant in variants:
        run = Path(runs[(variant, case)]['run'])
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
        target_rows = [t for s in fixture['statuses'] for t in s['fixture']['cylindrical']['sectorTargets']]
        records[variant] = {'image': full, 'stateKey': fixture['stateKey'],
                            'targetDimensions': sorted({tuple(t[:2]) for t in target_rows}),
                            'sampleCounts': sorted({t[2] for t in target_rows}),
                            'targetInstances': len(target_rows),
                            'centers': [s['fixture']['cylindrical']['sectorCentersDeg']
                                        for s in fixture['statuses']],
                            'postTargets': [s['fixture']['cylindrical']['postTarget']
                                            for s in fixture['statuses']],
                            'outputBuffers': [s['fixture']['drawingBuffer']
                                              for s in fixture['statuses']],
                            'draws': sum(r['measurement']['drawCalls'][0] for r in r13['records']),
                            'run': str(run)}
    assert len({r['stateKey'] for r in records.values()}) == 1
    assert all(r['targetInstances'] == 8 and r['sampleCounts'] == [2] for r in records.values())
    assert all(records[v]['centers'] == records['unbounded']['centers']
               and records[v]['postTargets'] == records['unbounded']['postTargets']
               and records[v]['outputBuffers'] == records['unbounded']['outputBuffers']
               for v in variants)
    box = ((3540, 850, 4140, 1450) if case.startswith('hole') else
           (2650, 570, 3250, 1170) if case == 'peripheral' else
           (800, 400, 1400, 1000))
    for col, variant in enumerate(variants):
        montage.paste(records[variant]['image'].crop(box), (col * 600, row * 624 + 24))
        draw.text((col * 600 + 6, row * 624 + 5), f'{case} {variant}', fill='white')
    baseline = np.asarray(records['unbounded']['image'])
    active = np.max(baseline, axis=2) > 8
    comparisons = {}
    for variant in variants[1:]:
        image = np.asarray(records[variant]['image'])
        difference = np.max(np.abs(baseline.astype(np.int16) - image.astype(np.int16)), axis=2)
        compared = difference[active]
        comparisons[variant] = {'meanMaxRgbActive': round(float(np.mean(compared)), 4),
                                'p99MaxRgbActive': round(float(np.percentile(compared, 99)), 2),
                                'activePixelsChangedAbove8Percent': round(float(np.mean(compared > 8) * 100), 4),
                                'activePixelsChangedAbove20Percent': round(float(np.mean(compared > 20) * 100), 4)}
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
    summary['captures'][case] = {
        'stateKeySha256': hashlib.sha256(records['unbounded']['stateKey'].encode()).hexdigest(),
        'runs': {v: records[v]['run'] for v in variants},
        'targets': {v: records[v]['targetDimensions'] for v in variants},
        'draws': {v: records[v]['draws'] for v in variants},
        'comparisonToUnbounded': comparisons, 'seams': seams}

montage.save(root / 'R16_CROP_MONTAGE.png')
pid_pattern = re.compile(r'pid_(\d+)_')
for variant in variants:
    run = Path(runs[(variant, 'moving-hole')]['run'])
    report = json.loads(next((run / 'metrics').glob('r14-*.json')).read_text())
    pids = {report['hostPid']} | {p['pid'] for s in report['snapshots'] for p in s['processes']}
    start = datetime.fromisoformat(report['startUtc'].replace('Z', '+00:00'))
    values = []
    for line in (run / 'counter-samples.jsonl').read_text().splitlines():
        sample = json.loads(line)
        age = (datetime.fromisoformat(sample['utc'].replace('Z', '+00:00')) - start).total_seconds()
        if not 10 <= age <= 40:
            continue
        dedicated = sum(c['value'] for c in sample.get('counters', [])
                        if 'dedicated usage' in c['path'].lower()
                        and (match := pid_pattern.search(c['path'])) and int(match.group(1)) in pids)
        parts = sample.get('nvidia', '').split(',')
        if len(parts) == 3:
            values.append((dedicated / 1048576, float(parts[0]), float(parts[1])))
    assert len(values) >= 10
    windows = []
    for record in report['records']:
        assert record['phase'] == 'done' and not record['contextLost'] and record['glError'] == 0
        windows.append({'display': record['display'], 'frames': record['measuredFrames'],
                        'intervalMedianMs': round(float(np.median(record['intervalsMs'])), 2),
                        'intervalP95Ms': round(float(np.percentile(record['intervalsMs'], 95)), 2),
                        'intervalP99Ms': round(float(np.percentile(record['intervalsMs'], 99)), 2)})
    summary['measurements'][variant] = {
        'run': str(run), 'sampleCount': len(values),
        'processDedicatedMedianMiB': round(float(np.median([v[0] for v in values])), 2),
        'processDedicatedRangeMiB': [round(min(v[0] for v in values), 2), round(max(v[0] for v in values), 2)],
        'adapterUsedMedianMiB': round(float(np.median([v[1] for v in values])), 2),
        'adapterGpuMedianPercent': round(float(np.median([v[2] for v in values])), 2),
        'windows': windows}
    motion_run = Path(runs[(variant, 'motion-crop')]['run'])
    motion = json.loads(next((motion_run / 'metrics').glob('b1-*.json')).read_text())
    summary['motion'][variant] = {'run': str(motion_run), 'frames': len(motion['frames']),
                                  'summaries': {k: {'phase': v['phase'], 'callbacks': v['callbacks'],
                                                    'errors': len(v['errors']), 'overflow': len(v['overflow'])}
                                                for k, v in motion['summaries'].items()}}

(root / 'summary.json').write_text(json.dumps(summary, indent=2))
print(root / 'summary.json')
