import json
import sys
from pathlib import Path

import numpy as np
from PIL import Image, ImageDraw

root = Path(sys.argv[1]).resolve()
reference = Path(sys.argv[2]).resolve()
manifest = json.loads(next((root / 'metrics').glob('zero-target-*.json')).read_text())
r10 = json.loads(next((reference / 'metrics').glob('sector-motion-*.json')).read_text())
weights = np.array([0.2126, 0.7152, 0.0722])


def paths(captures):
    return {('primary' if 'DISPLAY3' in x['display'] else 'left'): Path(x['frame']['path'])
            for x in captures}


def rgb(path):
    return np.asarray(Image.open(path).convert('RGB'), dtype=np.int16)


def stats(values):
    return {'mean': round(float(values.mean()), 3),
            'p95': round(float(np.percentile(values, 95)), 3),
            'max': round(float(values.max()), 3),
            'over20': int(np.count_nonzero(values > 20)),
            'over40': int(np.count_nonzero(values > 40))}


def join(left, primary):
    return np.abs(left[720:2160, -1] @ weights - primary[:, 0] @ weights)


prior = {}
result = {'schema': 1, 'run': str(root), 'referenceRun': str(reference), 'rows': []}
for record in manifest['records']:
    width = record['transitionPhysicalPixels']
    for frame in record['frames']:
        yaw = frame['yawDegrees']
        p = paths(frame['captures'])
        left, primary = rgb(p['left']), rgb(p['primary'])
        row = {'width': width, 'yaw': yaw, 'join': stats(join(left, primary)),
               'y1034': round(float(join(left, primary)[1034-720]), 3),
               'sha256': {x['display']: x['frame']['sha256'] for x in frame['captures']}}
        if width == 0:
            prior[yaw] = (p, row)
        else:
            ordinary_paths, ordinary = prior[yaw]
            ol, op = rgb(ordinary_paths['left']), rgb(ordinary_paths['primary'])
            dl = np.max(np.abs(left - ol), axis=2)
            dp = np.max(np.abs(primary - op), axis=2)
            ys, xs = np.where(dp > 1)
            row['changed'] = {'leftPixelsOver1': int(np.count_nonzero(dl > 1)),
                              'primaryPixelsOver1': int(len(xs)),
                              'primaryBounds': [int(xs.min()), int(ys.min()), int(xs.max()), int(ys.max())]
                              if len(xs) else None,
                              'primaryPixelsBeyondWidthPlus32': int(np.count_nonzero(dp[:, width+32:] > 1)),
                              'primaryMaxChannelDelta': int(dp.max())}
            lum = primary @ weights
            old = op @ weights
            gradient = np.abs(np.diff(lum[:, :width+33], axis=1))
            old_gradient = np.abs(np.diff(old[:, :width+33], axis=1))
            excess = gradient - old_gradient
            row['band'] = {'boundaryX': width,
                           'newRowsAtBoundary': int(np.count_nonzero((gradient[:, width-1] > 20)
                               & (excess[:, width-1] > 15))),
                           'newRowsWithinBandMax': int(np.max(np.count_nonzero(
                               (gradient > 20) & (excess > 15), axis=0))),
                           'newRowsWithinBandX': int(np.argmax(np.count_nonzero(
                               (gradient > 20) & (excess > 15), axis=0))),
                           'meanAbsoluteDeltaBand': round(float(dp[:, :width].mean()), 3),
                           'pixelsOver20Band': int(np.count_nonzero(dp[:, :width] > 20))}
        result['rows'].append(row)

reference_frames = {(x['phase'], x['yawDegrees']): paths(x['captures'])
                    for x in r10['records'] if x['yawDegrees'] in (0, 6.4, 6.815, 7, -6)}
comparison = []
for yaw in (0, 6.4, 6.815, 7, -6):
    if ('extra-left', yaw) not in reference_frames:
        continue
    visual = reference_frames[('extra-left', yaw)]
    rleft, rprimary = rgb(visual['left']), rgb(visual['primary'])
    reference_join = stats(join(rleft, rprimary))
    comparison.append({'yaw': yaw, 'r9ReferenceJoin': reference_join,
                       'r12': [{k: x[k] for k in ('width', 'join', 'y1034')}
                               for x in result['rows'] if x['yaw'] == yaw]})
result['referenceComparison'] = comparison

restored = paths(manifest['restored'])
ordinary = prior[6.815][0]
result['rollback'] = {display: {'sameSha256': next(x['frame']['sha256'] for x in manifest['restored']
                           if ('DISPLAY3' in x['display']) == (display == 'primary'))
                           == next(x['frame']['sha256'] for x in manifest['records'][0]['frames']
                           if x['yawDegrees'] == 6.815 for x in x['captures']
                           if ('DISPLAY3' in x['display']) == (display == 'primary')),
                           'maxChannelDelta': int(np.max(np.abs(rgb(restored[display]) - rgb(ordinary[display]))))}
                      for display in ('left', 'primary')}

result['cost'] = []
for record in manifest['records']:
    result['cost'].append({'width': record['transitionPhysicalPixels'],
                          'displays': [{**{k: d[k] for k in ('display', 'drawCalls', 'targetCount',
                              'textures', 'geometries', 'contextLost')},
                              'medianMs': round(float(np.median(d['samplesMs'])), 3),
                              'p95Ms': round(float(np.percentile(d['samplesMs'], 95)), 3)}
                              for d in (x['data'] for x in record['measurements'])]})

frames = {rec['transitionPhysicalPixels']: next(x for x in rec['frames'] if x['yawDegrees'] == 6.815)
          for rec in manifest['records']}
montage = Image.new('RGB', (4*226, 370), 'black')
draw = ImageDraw.Draw(montage)
for i, (label, frame) in enumerate([('ordinary', frames[0]), ('cube64', frames[64]),
                                    ('cube128', frames[128])]):
    p = paths(frame['captures'])
    left = Image.open(p['left']).convert('RGB').crop((3810, 900, 3840, 1240))
    primary = Image.open(p['primary']).convert('RGB').crop((0, 180, 196, 520))
    montage.paste(left, (i*226, 25))
    montage.paste(primary, (i*226+30, 25))
    draw.text((i*226+5, 5), label, fill='white')
p = reference_frames[('extra-left', 6.815)]
left = Image.open(p['left']).convert('RGB').crop((3810, 900, 3840, 1240))
primary = Image.open(p['primary']).convert('RGB').crop((0, 180, 196, 520))
montage.paste(left, (3*226, 25))
montage.paste(primary, (3*226+30, 25))
draw.text((3*226+5, 5), 'R9 reference', fill='white')
montage_path = root / 'metrics' / 'r12_join_montage.png'
montage.save(montage_path)
result['montage'] = str(montage_path)

output = root / 'metrics' / 'zero-target-analysis.json'
output.write_text(json.dumps(result, indent=2))
print(output)
for x in comparison:
    print('yaw', x['yaw'], 'R9', x['r9ReferenceJoin'],
          'R12', [(r['width'], r['join']) for r in x['r12']])
print('rollback', result['rollback'])
print('cost', result['cost'])
