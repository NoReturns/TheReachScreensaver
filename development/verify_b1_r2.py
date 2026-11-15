"""Verify ordered B1 run-local RGBA frames and summarize callback/readback cadence."""
import hashlib
import json
import statistics
import sys
from pathlib import Path

manifest = Path(sys.argv[1]).resolve()
root = manifest.parent.parent
data = json.loads(manifest.read_text())
assert data['schema'] == data['trajectoryVersion'] == 1
frames = data['frames']
assert frames
assert [f['writeOrdinal'] for f in frames] == list(range(1, len(frames) + 1))
sizes = {entry['Name']: entry['Bounds'] for entry in data['layout']}
by_display = {}
for frame in frames:
    name = frame['display']
    assert frame['mode'] == data['mode']
    by_display.setdefault(name, []).append(frame)
    assert frame['renderStartMs'] <= frame['renderEndMs']
    assert frame['callbackMs'] <= frame['renderStartMs']
    assert frame['hostReceivedUtcMs'] <= frame['hostWriteUtcMs']
    assert frame['hostReceivedTicks'] <= frame['hostWriteTicks']
    assert abs(frame['actualYaw'] - frame['intendedYaw']) < 1e-7
    if frame['payloadBytes']:
        path = Path(frame['path']).resolve()
        assert path.is_relative_to(root / 'captures')
        payload = path.read_bytes()
        assert len(payload) == frame['payloadBytes']
        assert hashlib.sha256(payload).hexdigest().upper() == frame['sha256']
        crop = frame['crop']
        assert len(payload) == crop['width'] * crop['height'] * 4
        bounds = sizes[name]
        assert crop == ({'x': bounds['Width']-192, 'y': 800, 'width': 192, 'height': 550}
                        if bounds['Width'] == 3840 else
                        {'x': 0, 'y': 80, 'width': 192, 'height': 550})
        assert frame['pixelFormat'] == 'RGBA8 bottom-up'
        assert len(set(payload)) > 1
        assert frame['readbackStartMs'] <= frame['readbackEndMs']
        assert frame['readbackEndMs'] <= frame['encodeEndMs']
for name, display_frames in by_display.items():
    ids = [f['id'] for f in display_frames]
    assert ids == sorted(set(ids))
    assert all(f['missingBefore'] == f['id'] - (display_frames[i-1]['id'] if i else 0) - 1
               for i, f in enumerate(display_frames))
    intervals = [b['callbackMs'] - a['callbackMs']
                 for a, b in zip(display_frames, display_frames[1:]) if b['id'] == a['id'] + 1]
    readbacks = [f['readbackEndMs'] - f['readbackStartMs'] for f in display_frames
                 if f['readbackStartMs'] is not None]
    crossings = {}
    for target in (-6, 4):
        matches = [f['id'] for f in display_frames if abs(f['actualYaw'] - target) <= 1]
        crossings[str(target)] = {'count': len(matches),
                                   'consecutive': any(b == a + 1 for a, b in zip(matches, matches[1:]))}
    print(json.dumps({'display': name, 'frames': len(ids), 'first': ids[0], 'last': ids[-1],
                      'callbackIntervalMedianMs': statistics.median(intervals) if intervals else None,
                      'readbackMedianMs': statistics.median(readbacks) if readbacks else None,
                      'crossings': crossings,
                      'summary': data['summaries'][name]}, separators=(',', ':')))
