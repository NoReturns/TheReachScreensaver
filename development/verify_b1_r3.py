"""Verify bounded B1 capture frames, timing, order, and crossing coverage."""
import hashlib
import json
import statistics
import sys
from pathlib import Path


def quantile(values, fraction):
    if not values:
        return None
    ordered = sorted(values)
    return ordered[min(len(ordered) - 1, max(0, int((len(ordered) - 1) * fraction + 0.5)))]


for argument in sys.argv[1:]:
    manifest = Path(argument).resolve()
    root = manifest.parent.parent
    data = json.loads(manifest.read_text())
    assert data['schema'] == data['trajectoryVersion'] == 1
    mode = data.get('captureMode', 'capture' if data['capture'] else 'baseline')
    assert mode in ('baseline', 'capture', 'async')
    frames = data['frames']
    assert frames and [f['writeOrdinal'] for f in frames] == list(range(1, len(frames) + 1))
    bounds = {d['Name']: d['Bounds'] for d in data['layout']}
    for display, summary in data['summaries'].items():
        assert summary['phase'] == 'done' and not summary['errors']
        assert summary['capacity'] == (256 if mode == 'async' else 4)
        assert summary['peakPending'] <= summary['capacity']
        if mode == 'async':
            assert all(summary['asyncSupport'].values())
        samples = [f for f in frames if f['display'] == display]
        if bounds[display]['Width'] == 1920:
            assert not samples
            continue
        ids = [f['id'] for f in samples]
        assert ids == sorted(set(ids))
        assert summary['callbacks'] == len(ids) + len(summary['overflow'])
        assert len(summary['intervalsMs']) == summary['callbacks'] - 1
        assert [f['missingBefore'] for f in samples] == [
            f['id'] - (samples[i-1]['id'] if i else 0) - 1
            for i, f in enumerate(samples)]
        submit = []
        transfer = []
        polling = []
        encoding = []
        readback = []
        for frame in samples:
            assert frame['mode'] == data['mode']
            assert abs(frame['actualYaw'] - frame['intendedYaw']) < 1e-7
            # rAF and performance.now are separately rounded browser clocks.
            assert frame['callbackMs'] <= frame['renderStartMs'] + 1
            assert frame['renderStartMs'] <= frame['renderEndMs']
            assert frame['hostReceivedTicks'] <= frame['hostWriteTicks']
            if mode == 'async':
                assert frame['submitStartMs'] >= frame['renderEndMs']
                assert frame['submitStartMs'] <= frame['submitEndMs']
                assert frame['submitEndMs'] <= frame['transferStartMs']
                assert frame['transferStartMs'] <= frame['transferEndMs'] <= frame['encodeEndMs']
                assert frame['syncPolls'] >= 1 and frame['syncPollMs'] >= 0
                submit.append(frame['submitEndMs'] - frame['submitStartMs'])
                transfer.append(frame['transferEndMs'] - frame['transferStartMs'])
                polling.append(frame['syncPollMs'])
                encoding.append(frame['encodeEndMs'] - frame['transferEndMs'])
            if frame['readbackStartMs'] is not None:
                assert frame['readbackStartMs'] <= frame['readbackEndMs']
                readback.append(frame['readbackEndMs'] - frame['readbackStartMs'])
            if mode == 'baseline':
                assert frame['payloadBytes'] == 0 and frame['path'] is None
                continue
            crop = frame['crop']
            expected = ({'x': 3648, 'y': 800, 'width': 192, 'height': 550}
                        if bounds[display]['Width'] == 3840 else
                        {'x': 0, 'y': 80, 'width': 192, 'height': 550})
            assert crop == expected and frame['pixelFormat'] == 'RGBA8 bottom-up'
            path = Path(frame['path']).resolve()
            assert path.is_relative_to(root / 'captures')
            pixels = path.read_bytes()
            assert len(pixels) == frame['payloadBytes'] == 192 * 550 * 4
            assert len(set(pixels)) > 1
            assert hashlib.sha256(pixels).hexdigest().upper() == frame['sha256']
        crossing = {}
        for yaw in (-6, 4):
            near = [f['id'] for f in samples if abs(f['actualYaw'] - yaw) <= 1]
            crossing[str(yaw)] = {'frames': len(near),
                                   'consecutive': any(b == a + 1 for a, b in zip(near, near[1:]))}
        intervals = summary['intervalsMs']
        print(json.dumps({'run': root.name, 'mode': data['mode'], 'captureMode': mode,
                          'display': display, 'callbacks': summary['callbacks'],
                          'written': len(samples), 'overflow': len(summary['overflow']),
                          'gapsOver50Ms': len(summary['gaps']),
                          'callbackP50Ms': quantile(intervals, 0.5),
                          'callbackP95Ms': quantile(intervals, 0.95),
                          'callbackMaxMs': max(intervals),
                          'submitP50Ms': quantile(submit, 0.5),
                          'submitP95Ms': quantile(submit, 0.95),
                          'transferP50Ms': quantile(transfer, 0.5),
                          'transferP95Ms': quantile(transfer, 0.95),
                          'fencePollP50Ms': quantile(polling, 0.5),
                          'fencePollP95Ms': quantile(polling, 0.95),
                          'encodeP50Ms': quantile(encoding, 0.5),
                          'encodeP95Ms': quantile(encoding, 0.95),
                          'readbackP50Ms': quantile(readback, 0.5),
                          'peakPending': summary['peakPending'],
                          'crossings': crossing}, separators=(',', ':')))
