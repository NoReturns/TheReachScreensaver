"""Compare R3 async crops with retained R2 synchronous crops at nearby yaw."""
import json
import sys
from pathlib import Path

import numpy as np


sync = json.loads(Path(sys.argv[1]).read_text())
async_run = json.loads(Path(sys.argv[2]).read_text())
assert sync['mode'] == async_run['mode']
assert sync['capture'] and async_run['captureMode'] == 'async'
cache = {}


def pixels(frame):
    path = frame['path']
    if path not in cache:
        cache[path] = np.fromfile(path, dtype=np.uint8).astype(np.int16)
    return cache[path]


for display in {f['display'] for f in async_run['frames']}:
    sf = [f for f in sync['frames'] if f['display'] == display and f['payloadBytes']
          and f['virtualMs'] <= 2000]
    af = [f for f in async_run['frames'] if f['display'] == display and f['payloadBytes']
          and f['virtualMs'] <= 2000]
    for target in (-6, 4):
        sample = min(af, key=lambda f: abs(f['actualYaw'] - target))
        nearby = [f for f in sf if abs(f['actualYaw'] - sample['actualYaw']) <= 1.5]
        wrong = min(sf, key=lambda f: abs(f['actualYaw'] - (target + 8 if target < 0 else target - 8)))
        assert nearby
        source = pixels(sample)
        def error(frame):
            return float(np.abs(source - pixels(frame)).mean())
        best = min(nearby, key=error)
        print(json.dumps({'display': display, 'targetYaw': target,
                          'asyncFrameId': sample['id'], 'asyncYaw': sample['actualYaw'],
                          'closestSyncYaw': best['actualYaw'],
                          'yawDifference': abs(best['actualYaw'] - sample['actualYaw']),
                          'nearbyMeanAbsoluteByteError': error(best),
                          'wrongYaw': wrong['actualYaw'],
                          'wrongYawMeanAbsoluteByteError': error(wrong)}))
