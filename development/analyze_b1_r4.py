"""Analyze native left/primary continuous seam strips without changing captures."""
import bisect
import json
import statistics
import sys
from functools import lru_cache
from pathlib import Path

import numpy as np
from PIL import Image, ImageDraw


manifest = Path(sys.argv[1]).resolve()
root = manifest.parent.parent
data = json.loads(manifest.read_text())
assert data['captureMode'] == 'async' and data['trajectoryVersion'] == 1
frames = data['frames']
left = sorted((f for f in frames if f['crop']['x'] == 3648), key=lambda f: f['callbackMs'])
primary = sorted((f for f in frames if f['crop']['x'] == 0), key=lambda f: f['callbackMs'])
assert left and primary


@lru_cache(maxsize=16)
def image(path):
    return np.fromfile(path, dtype=np.uint8).reshape(550, 192, 4)[::-1, :, :3].copy()


def frame_image(frame):
    return image(frame['path'])


def percentile(values, q):
    return round(float(np.percentile(values, q)), 3) if values else None


# Monotonic one-to-one matching avoids reusing a display frame when one
# WebView's requestAnimationFrame cadence differs from the other.
left_times = [f['callbackMs'] for f in left]
pairs = []
next_left = 0
for p in primary:
    k = bisect.bisect_left(left_times, p['callbackMs'], lo=next_left)
    choices = [i for i in (k-1, k) if next_left <= i < len(left)]
    if not choices:
        continue
    i = min(choices, key=lambda j: abs(left[j]['callbackMs'] - p['callbackMs']))
    l = left[i]
    dt = abs(l['callbackMs'] - p['callbackMs'])
    dyaw = abs(l['actualYaw'] - p['actualYaw'])
    if dt <= 30 and dyaw <= 0.3:
        pairs.append((l, p, dt, dyaw))
        next_left = i + 1


def seam_measure(pair):
    l, p, dt, dyaw = pair
    a, b = frame_image(l).astype(np.float32), frame_image(p).astype(np.float32)
    lum = lambda x: x @ np.array([0.2126, 0.7152, 0.0722], dtype=np.float32)
    x0, x1, x2, x3 = lum(a[:, -2]), lum(a[:, -1]), lum(b[:, 0]), lum(b[:, 1])
    impulse = np.maximum(0, np.abs(x1-x2) - (np.abs(x0-x1)+np.abs(x2-x3))/2)
    hard = impulse > 20
    boundaries = np.flatnonzero(np.diff(np.r_[False, hard, False]))
    runs = boundaries[1::2] - boundaries[::2]
    return {'leftId': l['id'], 'primaryId': p['id'], 'yaw': p['actualYaw'],
            'virtualMs': p['virtualMs'], 'direction': 'forward' if p['virtualMs'] <= 2000 else 'reverse',
            'skewMs': dt, 'yawSkew': dyaw, 'meanImpulse': float(impulse.mean()),
            'peakImpulse': float(impulse.max()), 'hardRows': int(hard.sum()),
            'longestHardRun': int(runs.max()) if len(runs) else 0,
            'hardCentroidY': float(np.flatnonzero(hard).mean()+800) if hard.any() else None}, impulse


measured = [seam_measure(pair) for pair in pairs]
records = [entry[0] for entry in measured]
strips = [entry[1] for entry in measured]
tight_pairs = []
used_left = set()
for p in primary:
    k = bisect.bisect_left(left_times, p['callbackMs'])
    choices = [left[i] for i in (k-1, k) if 0 <= i < len(left)]
    if not choices:
        continue
    l = min(choices, key=lambda frame: abs(frame['callbackMs']-p['callbackMs']))
    dt = abs(l['callbackMs']-p['callbackMs'])
    dyaw = abs(l['actualYaw']-p['actualYaw'])
    if dt <= 2 and dyaw <= 0.02 and l['id'] not in used_left:
        tight_pairs.append((l, p, dt, dyaw))
        used_left.add(l['id'])
tight_measured = [seam_measure(pair) for pair in tight_pairs]
tight_records = [entry[0] for entry in tight_measured]
tight_strips = [entry[1] for entry in tight_measured]
crossings = {}
for target in (-6, 4):
    for direction in ('forward', 'reverse'):
        subset = [r for r in records if r['direction'] == direction and abs(r['yaw']-target) <= 1]
        assert subset
        closest = min(subset, key=lambda r: abs(r['yaw']-target))
        crossings[f'{target}:{direction}'] = {
            'pairs': len(subset), 'medianHardRows': percentile([r['hardRows'] for r in subset], 50),
            'maxHardRows': max(r['hardRows'] for r in subset),
            'maxLongestHardRun': max(r['longestHardRun'] for r in subset),
            'medianMeanImpulse': percentile([r['meanImpulse'] for r in subset], 50),
            'maxPeakImpulse': round(max(r['peakImpulse'] for r in subset), 3),
            'closest': closest}
        tight = [r for r in tight_records if r['direction'] == direction and abs(r['yaw']-target) <= 1]
        crossings[f'{target}:{direction}']['tightPairs'] = len(tight)
        crossings[f'{target}:{direction}']['tightMedianHardRows'] = percentile([r['hardRows'] for r in tight], 50)
        crossings[f'{target}:{direction}']['tightMaxHardRows'] = max((r['hardRows'] for r in tight), default=None)
        crossings[f'{target}:{direction}']['tightMaxPeakImpulse'] = round(max((r['peakImpulse'] for r in tight), default=0), 3)


reversal = {}
for display, stream in [('left', left), ('primary', primary)]:
    forward = [f for f in stream if f['virtualMs'] <= 2000]
    reverse = [f for f in stream if f['virtualMs'] > 2000]
    for target in (-6, 4):
        errors = []
        yaw_deltas = []
        for f in forward:
            if abs(f['actualYaw']-target) > 1:
                continue
            r = min(reverse, key=lambda x: abs(x['actualYaw']-f['actualYaw']))
            delta = abs(r['actualYaw']-f['actualYaw'])
            if delta > 0.12:
                continue
            errors.append(float(np.abs(frame_image(f).astype(np.int16)-frame_image(r).astype(np.int16)).mean()))
            yaw_deltas.append(delta)
        reversal[f'{display}:{target}'] = {
            'pairs': len(errors), 'medianMeanAbsoluteByteError': percentile(errors, 50),
            'p95MeanAbsoluteByteError': percentile(errors, 95),
            'maxMeanAbsoluteByteError': round(max(errors), 3) if errors else None,
            'maxYawDifference': round(max(yaw_deltas), 3) if yaw_deltas else None}


temporal = {}
for display, stream in [('left', left), ('primary', primary)]:
    means = [float(np.mean(frame_image(f) @ np.array([0.2126, 0.7152, 0.0722]))) for f in stream]
    for target in (-6, 4):
        for direction in ('forward', 'reverse'):
            residuals = []
            for i in range(1, len(stream)-1):
                a, b, c = stream[i-1:i+2]
                if (b['virtualMs'] <= 2000) != (direction == 'forward') or abs(b['actualYaw']-target) > 1:
                    continue
                if b['id'] != a['id']+1 or c['id'] != b['id']+1:
                    continue
                if b['callbackMs']-a['callbackMs'] > 50 or c['callbackMs']-b['callbackMs'] > 50:
                    continue
                weight = (b['actualYaw']-a['actualYaw'])/(c['actualYaw']-a['actualYaw'])
                residuals.append(abs(means[i]-(means[i-1]*(1-weight)+means[i+1]*weight)))
            temporal[f'{display}:{target}:{direction}'] = {
                'triples': len(residuals), 'p95MeanLumaResidual': percentile(residuals, 95),
                'maxMeanLumaResidual': round(max(residuals), 3) if residuals else None}


# A fixed full-height line would leave persistent high impulse in the same
# physical y rows across yaw. This does not prove equivalence of two contexts.
stack = np.stack(strips)
tight_stack = np.stack(tight_strips)
hard_fraction = np.mean(tight_stack > 20, axis=0)
output = {'schema': 1, 'run': root.name, 'mode': data['mode'],
          'pairing': {'pairs': len(pairs), 'primaryFrames': len(primary), 'leftFrames': len(left),
                      'skewP50Ms': percentile([p[2] for p in pairs], 50),
                      'skewP95Ms': percentile([p[2] for p in pairs], 95),
                      'skewMaxMs': round(max(p[2] for p in pairs), 3),
                      'yawSkewMax': round(max(p[3] for p in pairs), 4)},
          'tightPairing': {'pairs': len(tight_pairs), 'maxSkewMs': round(max(p[2] for p in tight_pairs), 3),
                           'maxYawSkew': round(max(p[3] for p in tight_pairs), 4)},
          'persistentHardRowsAt50PercentFrames': int((hard_fraction >= 0.5).sum()),
          'maxHardFrameFractionAtAnyRow': round(float(hard_fraction.max()), 3),
          'tightFramesWithOver100HardRows': sum(r['hardRows'] > 100 for r in tight_records),
          'crossings': crossings, 'reversal': reversal, 'temporal': temporal,
          'seamFrames': records}
destination = root / 'metrics' / 'b1-r4-analysis-v2.json'
if destination.exists():
    raise FileExistsError(destination)
destination.write_text(json.dumps(output, indent=2), encoding='utf-8')

if data['mode'] == 'cylindrical':
    # Each contact sheet retains five consecutive paired frames in time order.
    for target in (-6, 4):
        for direction in ('forward', 'reverse'):
            candidates = [(index, r) for index, r in enumerate(records) if r['direction'] == direction]
            center = min(range(len(candidates)), key=lambda i: abs(candidates[i][1]['yaw']-target))
            selection = candidates[max(0, center-2):center+3]
            sheet = Image.new('RGB', (384*len(selection), 580), 'black')
            draw = ImageDraw.Draw(sheet)
            for col, (index, record) in enumerate(selection):
                l, p, _, _ = pairs[index]
                left_img = Image.fromarray(frame_image(l))
                primary_img = Image.fromarray(frame_image(p))
                sheet.paste(left_img, (col*384, 30))
                sheet.paste(primary_img, (col*384+192, 30))
                draw.text((col*384+4, 7),
                          f"yaw {record['yaw']:+.2f} L{l['id']} P{p['id']} skew {record['skewMs']:.1f}ms",
                          fill='white')
            label = 'minus6' if target < 0 else 'plus4'
            sheet.save(root / 'captures' / f'b1-r4-v2-{label}-{direction}.png')
    picture = Image.fromarray(np.clip(tight_stack.T * 3, 0, 255).astype(np.uint8), 'L')
    picture.resize((picture.width*2, picture.height)).save(root / 'captures' / 'b1-r4-v2-tight-seam-impulse-time.png')

print(json.dumps({key: value for key, value in output.items() if key != 'seamFrames'}, separators=(',', ':')))
