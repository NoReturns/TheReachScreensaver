import json
import sys
from pathlib import Path

import numpy as np
from PIL import Image

root = Path(sys.argv[1]).resolve()
manifest = json.loads(next((root / 'metrics').glob('sector-motion-*.json')).read_text())
weights = np.array([0.2126, 0.7152, 0.0722])
rows = []
previous = {}

for record in manifest['records']:
    captures = {}
    for item in record['captures']:
        trace = json.loads(Path(item['tracePath']).read_text())
        image = np.asarray(Image.open(item['frame']['path']).convert('RGB'), dtype=np.int16)
        captures[trace['display']] = (item, trace, image)
    left, lt, li = captures['left']
    primary, pt, pi = captures['primary']
    join = np.abs(li[720:2160, -1] @ weights - pi[:, 0] @ weights)
    near_l = np.asarray(lt['choiceRows'], dtype=float)[:, 1, :]
    near_p = np.asarray(pt['choiceRows'], dtype=float)[:, 2, :]
    active_l = np.asarray(lt['rayRows'], dtype=float)[:, 1, 3] > 0.5
    active_p = np.asarray(pt['rayRows'], dtype=float)[:, 2, 3] > 0.5
    active_both = active_l & active_p
    # Branch 0 samples a sector; branches 1..3 fall back to the cube.
    mismatch = active_both & ((near_l[:, 2] == 0) != (near_p[:, 2] == 0))
    sector_mismatch = active_both & (near_l[:, 2] == 0) & (near_p[:, 2] == 0) & (
        np.abs(near_l[:, 1] - near_p[:, 1]) > 0.01)
    key = record['phase']
    temporal = None
    if key in previous:
        last_yaw, last_image = previous[key]
        delta = np.abs((li[720:2160, 3800:3840] - last_image[720:2160, 3800:3840]))
        temporal = {'fromYaw': last_yaw, 'meanRgb': float(delta.mean()),
                    'peakRgb': int(delta.max()), 'pixelsOver40': int(np.count_nonzero(np.max(delta, axis=2) > 40))}
    previous[key] = (record['yawDegrees'], li)
    entry = {'phase': key, 'yaw': record['yawDegrees'],
             'join': {'mean': float(join.mean()), 'p95': float(np.percentile(join, 95)),
                      'max': float(join.max()), 'over20': int(np.count_nonzero(join > 20)),
                      'over40': int(np.count_nonzero(join > 40)),
                      'y1034': float(join[1034-720])},
             'branchMismatchRows': int(np.count_nonzero(mismatch)),
             'activeBothRows': int(np.count_nonzero(active_both)),
             'activeOnlyOneRows': int(np.count_nonzero(active_l != active_p)),
             'branchMismatchYRange': [int(np.min(np.flatnonzero(mismatch))+720),
                                      int(np.max(np.flatnonzero(mismatch))+720)] if mismatch.any() else None,
             'differentSectorRows': int(np.count_nonzero(sector_mismatch)),
             'leftJoinSectorRows': int(np.count_nonzero(active_l & (near_l[:, 2] == 0))),
             'primaryJoinSectorRows': int(np.count_nonzero(active_p & (near_p[:, 2] == 0))),
             'leftJoinCubeRows': int(np.count_nonzero(active_l & (near_l[:, 2] != 0))),
             'primaryJoinCubeRows': int(np.count_nonzero(active_p & (near_p[:, 2] != 0))),
             'leftCenters': lt['centersRadians'], 'primaryCenters': pt['centersRadians'],
             'leftSampledEnvelope': lt['sampledEnvelope'],
             'primarySampledEnvelope': pt['sampledEnvelope'],
             'temporal': temporal}
    rows.append(entry)

by_yaw = {}
for row in rows:
    by_yaw.setdefault(row['yaw'], {})[row['phase']] = row
comparison = []
for yaw, phases in by_yaw.items():
    a, b = phases['ordinary'], phases['extra-left']
    comparison.append({'yaw': yaw, 'beforeMean': a['join']['mean'],
                       'afterMean': b['join']['mean'],
                       'beforeOver20': a['join']['over20'],
                       'afterOver20': b['join']['over20'],
                       'beforeBranchMismatch': a['branchMismatchRows'],
                       'afterBranchMismatch': b['branchMismatchRows'],
                       'beforeY1034': a['join']['y1034'],
                       'afterY1034': b['join']['y1034']})

# Compare paired complete frames and local edge impulses. A new vertical
# boundary would produce many rows at one x; isolated scene detail does not.
paired = []
for ordinary, pilot in zip(manifest['records'][:len(manifest['yawDegrees'])],
                           manifest['records'][len(manifest['yawDegrees']):]):
    assert ordinary['yawDegrees'] == pilot['yawDegrees']
    images = []
    for item in (ordinary, pilot):
        left_item = next(c for c in item['captures'] if 'DISPLAY1' in c['display'])
        images.append(np.asarray(Image.open(left_item['frame']['path']).convert('RGB'), dtype=np.int16))
    before, after = images
    difference = np.max(np.abs(after - before), axis=2)
    changed = np.argwhere(difference > 1)
    before_luma = before @ weights
    after_luma = after @ weights
    a = np.abs(np.diff(before_luma[720:2160, 3700:3840], axis=1))
    b = np.abs(np.diff(after_luma[720:2160, 3700:3840], axis=1))
    new = (b > 20) & ((b - a) > 15)
    counts = np.count_nonzero(new, axis=0)
    paired.append({'yaw': ordinary['yawDegrees'],
                   'changedPixelsOver1': int(changed.shape[0]),
                   'boundingBox': [int(changed[:, 1].min()), int(changed[:, 0].min()),
                                   int(changed[:, 1].max()), int(changed[:, 0].max())]
                   if changed.size else None,
                   'maxNewBoundaryRows': int(counts.max()),
                   'maxNewBoundaryX': int(3700 + counts.argmax())})

report = {'schema': 1, 'run': str(root), 'samples': rows,
          'comparison': comparison, 'pairedFullFrames': paired}
path = root / 'metrics' / 'sector-motion-analysis.json'
path.write_text(json.dumps(report, indent=2))
print('yaw beforeMean afterMean beforeOver20 afterOver20 beforeBranchMismatch afterBranchMismatch')
for row in comparison:
    print(row['yaw'], round(row['beforeMean'], 3), round(row['afterMean'], 3),
          row['beforeOver20'], row['afterOver20'], row['beforeBranchMismatch'],
          row['afterBranchMismatch'])
