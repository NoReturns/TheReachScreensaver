"""Analyze the isolated -6 degree full-frame and source-path probe."""

import json
import math
import sys
from pathlib import Path

import numpy as np
from PIL import Image

root = Path(sys.argv[1]).resolve()
evidence = json.loads(next((root / 'metrics').glob('negative-probe-*.json')).read_text())
forensics = json.loads(next((root / 'metrics').glob('forensics-*.json')).read_text())
by_display = {r['sourceProbe']['display']: r for r in evidence['records']}
source = {name: record['sourceProbe'] for name, record in by_display.items()}
stages = {r['display']: r['stages'] for r in forensics['records']}
weights = np.array([0.2126, 0.7152, 0.0722])

def mode(display, name):
    return next(r for r in source[display]['records'] if r['mode'] == name)

def rgb(record):
    return np.asarray([row['pixels'] for row in record['linearRgb']], dtype=float)

def trace(record, channel):
    return np.asarray([row['pixels'] for row in next(t for t in record['traces']
                       if t['channel'] == channel)['rows']], dtype=float)

def frame(display, phase):
    return np.asarray(Image.open(by_display[display][phase + 'Frame']['path']).convert('RGB'),
                      dtype=np.int16)

def join(left, primary):
    d = np.abs(left[720:2160, -1] @ weights - primary[:, 0] @ weights)
    return {'mean': float(d.mean()), 'p95': float(np.percentile(d, 95)),
            'peak': float(d.max()), 'peakY': int(d.argmax() + 720),
            'rowsOver20': int(np.count_nonzero(d > 20)),
            'y1033': float(d[1033 - 720])}

output = {}
frames = {}
for phase in ('baseline', 'pilot', 'restored'):
    left, primary = frame('left', phase), frame('primary', phase)
    frames[phase] = {'left': left, 'primary': primary}
    output[phase] = join(left, primary)

primary_change = np.max(np.abs(frames['baseline']['primary']
                               - frames['pilot']['primary']), axis=2)
changed = np.argwhere(primary_change > 1)
output['primaryCandidateChange'] = {
    'pixelsOver1': int(changed.shape[0]),
    'pixelsOver20': int(np.count_nonzero(primary_change > 20)),
    'peakChannel': int(primary_change.max()),
    'localBoundingBox': [int(changed[:, 1].min()), int(changed[:, 0].min()),
                         int(changed[:, 1].max()), int(changed[:, 0].max())]
    if changed.size else None}
output['restoreMaxChannel'] = {
    name: int(np.max(np.abs(frames['baseline'][name] - frames['restored'][name])))
    for name in ('left', 'primary')}

parity = {}
for name in ('normal', 'cube', 'sector'):
    left, primary = rgb(mode('left', name)), rgb(mode('primary', name))
    delta = np.max(np.abs(left[:, :, :3] - primary[:, :, :3]), axis=2)
    adjacent = np.max(np.abs(left[:, 3, :3] - primary[:, 4, :3]), axis=1)
    parity[name] = {'sameCoordinatePeak': float(delta.max()),
                    'sameCoordinateDifferent': int(np.count_nonzero(delta > 1e-6)),
                    'adjacentMean': float(adjacent.mean()),
                    'adjacentPeak': float(adjacent.max())}

sector_left = trace(mode('left', 'sector'), 3)
sector_primary = trace(mode('primary', 'sector'), 3)
parity['validSectorPixelsBoth'] = int(np.count_nonzero(
    (sector_left[:, :, 0] == 0) & (sector_primary[:, :, 0] == 0)))
parity['sampledPixels'] = int(sector_left.shape[0] * sector_left.shape[1])
parity['sectorGridHashEqual'] = (source['left']['sectorSource']['sampleGrid']['sha256']
                                 == source['primary']['sectorSource']['sampleGrid']['sha256'])

stage_result = {}
for name in ('unlensed', 'lensed', 'bright', 'vertical3', 'composite', 'postOutput'):
    left = np.asarray(stages['left'][name]['rows'][1033 - 800], dtype=float)
    primary = np.asarray(stages['primary'][name]['rows'][1033 - 800], dtype=float)
    stage_result[name] = {'sameCoordinatePeak': float(np.max(np.abs(left - primary))),
                          'adjacentPeak': float(np.max(np.abs(left[1] - primary[2]))),
                          'leftX3839': left[1].tolist(),
                          'primaryX3840': primary[2].tolist()}

row = {}
for display in ('left', 'primary'):
    normal = mode(display, 'normal')
    row[display] = {}
    for x in (3838, 3839, 3840, 3841):
        y_index, x_index = 1033 - 930, x - 3836
        row[display][str(x)] = {
            'originalRay': trace(normal, 6)[y_index, x_index].tolist(),
            'deflectedRay': trace(normal, 1)[y_index, x_index].tolist(),
            'choice': trace(normal, 2)[y_index, x_index].tolist(),
            'branchAndCubeRgb': trace(normal, 3)[y_index, x_index].tolist(),
            'cubeWorldRay': trace(normal, 4)[y_index, x_index].tolist(),
            'lensRgb': rgb(normal)[y_index, x_index, :3].tolist()}

angles = [math.degrees(math.atan2(row['left'][str(x)]['deflectedRay'][0],
                                  -row['left'][str(x)]['deflectedRay'][2]))
          for x in (3838, 3839, 3840, 3841)]
unwrapped = np.rad2deg(np.unwrap(np.deg2rad(angles)))
ray_angles = {'deflectedSignedDeg': angles, 'deflectedUnwrappedDeg': unwrapped.tolist(),
              'unwrappedStepsDeg': np.diff(unwrapped).tolist()}

nearby = []
for record in evidence['nearby']:
    captures = {('left' if 'DISPLAY1' in c['display'] else 'primary'): c
                for c in record['captures']}
    left = np.asarray(Image.open(captures['left']['frame']['path']).convert('RGB'),
                      dtype=np.int16)
    primary = np.asarray(Image.open(captures['primary']['frame']['path']).convert('RGB'),
                         dtype=np.int16)
    nearby.append({'yaw': record['yawDegrees'], **join(left, primary)})

report = {'schema': 1, 'run': str(root), 'yawDegrees': -6,
          'sourceParity': parity, 'stagesAtY1033': stage_result,
          'traceAtY1033': row, 'rayAngles': ray_angles,
          'finalOutput': output, 'nearbyYaw': nearby}
path = root / 'metrics' / 'negative-probe-analysis.json'
path.write_text(json.dumps(report, indent=2))
print('analysis', path)
print('join', output['baseline'], output['pilot'])
print('sourceParity', parity)
print('stagesAtY1033', {k: v['adjacentPeak'] for k, v in stage_result.items()})
print('nearby', [(x['yaw'], x['rowsOver20'], round(x['peak'], 2)) for x in nearby])
