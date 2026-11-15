import json
import sys
from pathlib import Path

import numpy as np
from PIL import Image

root = Path(sys.argv[1])
manifest = json.loads(next((root / 'metrics').glob('sector-parity-*.json')).read_text())
records = {r['baselineLens']['metadata']['display']: r for r in manifest['records']}
left = records['left']
primary = records['primary']

def frame(record, phase):
    return np.asarray(Image.open(record[phase + 'Frame']['path']).convert('RGB'), dtype=np.int16)

def lens(record, phase):
    meta = record[phase + 'Lens']['metadata']
    data = np.memmap(record[phase + 'Lens']['path'], dtype='<f2', mode='r',
                     shape=(meta['height'], meta['width'], 4))
    return data, meta

lb, la, lr = (frame(left, name) for name in ('baseline', 'pilot', 'restored'))
pb, pa, pr = (frame(primary, name) for name in ('baseline', 'pilot', 'restored'))
weights = np.array([0.2126, 0.7152, 0.0722])

def luma(rgb):
    return rgb @ weights

def summary(values):
    return {'mean': float(np.mean(values)), 'median': float(np.median(values)),
            'p95': float(np.percentile(values, 95)), 'max': float(np.max(values))}

join = {}
for name, l, p in [('before', lb, pb), ('after', la, pa), ('restored', lr, pr)]:
    left_px = l[720:2160, -1, :]
    primary_px = p[:, 0, :]
    diff = np.abs(luma(left_px) - luma(primary_px))
    join[name] = {'luma': summary(diff),
                  'over20': int(np.count_nonzero(diff > 20)),
                  'over40': int(np.count_nonzero(diff > 40)),
                  'y1034': float(diff[1034 - 720]),
                  'peakY': int(np.argmax(diff) + 720)}

image_diff = np.max(np.abs(la - lb), axis=2)
restore_diff = np.max(np.abs(lr - lb), axis=2)
changed = np.argwhere(image_diff > 1)
unexpected = np.argwhere((image_diff > 5) & (restore_diff <= 1))
output_change = {'changedPixelsOver1': int(changed.shape[0]),
                 'pixelsOver5BeyondRepeatControl': int(unexpected.shape[0]),
                 'primaryBeforeAfterEqual': bool(np.array_equal(pb, pa)),
                 'leftRestoredEqual': bool(np.array_equal(lb, lr)),
                 'restoredDifference': summary(restore_diff),
                 'changeBoundingBox': [int(changed[:, 1].min()), int(changed[:, 0].min()),
                                       int(changed[:, 1].max()), int(changed[:, 0].max())]
                 if changed.size else None}

# Compare vertical candidate boundaries throughout the changed frame. Ordinary
# scene edges appear in both phases; report new strong steps by column.
before_step = np.abs(np.diff(luma(lb[720:2160, 2800:3840]), axis=1))
after_step = np.abs(np.diff(luma(la[720:2160, 2800:3840]), axis=1))
new_step = (after_step > 20) & (after_step - before_step > 15)
counts = np.count_nonzero(new_step, axis=0)
top = np.argsort(counts)[-15:][::-1]
boundaries = [{'xLeft': int(2800 + i), 'newRows': int(counts[i])}
              for i in top if counts[i] > 0]

lens_summary = {}
for record in (left, primary):
    before, meta = lens(record, 'baseline')
    after, _ = lens(record, 'pilot')
    x0, y0, width, height = meta['rect']
    # Files are bottom-up, as returned by WebGL readRenderTargetPixels.
    row = height - 1 - (1034 - y0)
    if x0 <= 3839 < x0 + width:
        x = 3839 - x0
    else:
        x = 3840 - x0
    b = np.asarray(before[row, x, :3], dtype=np.float32)
    a = np.asarray(after[row, x, :3], dtype=np.float32)
    delta = np.abs(np.asarray(after[:, :, :3], dtype=np.float32)
                   - np.asarray(before[:, :, :3], dtype=np.float32))
    lens_summary[meta['display']] = {
        'dimensions': [width, height], 'differentRgbValues': int(np.count_nonzero(delta)),
        'maxRgbDelta': float(np.max(delta)), 'meanPixelPeakDelta': float(np.mean(np.max(delta, axis=2))),
        'y1034NearJoinBefore': b.tolist(), 'y1034NearJoinAfter': a.tolist()}

report = {'schema': 1, 'run': str(root), 'joinRows': [720, 2159],
          'join': join, 'frameChange': output_change, 'newBoundaryCandidates': boundaries,
          'lens': lens_summary,
          'timing': {r['baselineLens']['metadata']['display']: {
              'beforeMs': summary(r['baselineTiming']['samplesMs']),
              'afterMs': summary(r['pilotTiming']['samplesMs'])} for r in manifest['records']}}
path = root / 'metrics' / 'sector-parity-analysis.json'
path.write_text(json.dumps(report, indent=2))
print(json.dumps(report, indent=2))
