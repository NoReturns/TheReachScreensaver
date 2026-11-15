"""Read retained B1-R4 strips and measure the physical join against neighbors."""
import bisect
import json
import sys
from functools import lru_cache
from pathlib import Path

import numpy as np


def percentile(values, q):
    return round(float(np.percentile(values, q)), 3) if len(values) else None


@lru_cache(maxsize=24)
def pixels(path):
    return np.fromfile(path, dtype=np.uint8).reshape(550, 192, 4)[::-1, :, :3].astype(np.float32)


def luma(frame):
    return pixels(frame['path']) @ np.array([0.2126, 0.7152, 0.0722], dtype=np.float32)


def analyze(path):
    data = json.loads(path.read_text())
    assert data['captureMode'] == 'async' and data['trajectoryVersion'] == 1
    frames = data['frames']
    left = sorted((f for f in frames if f['crop']['x'] == 3648), key=lambda f: f['callbackMs'])
    primary = sorted((f for f in frames if f['crop']['x'] == 0), key=lambda f: f['callbackMs'])
    times = [f['callbackMs'] for f in left]
    paired = []
    used = set()
    for p in primary:
        k = bisect.bisect_left(times, p['callbackMs'])
        options = [left[i] for i in (k-1, k) if 0 <= i < len(left)]
        if not options:
            continue
        l = min(options, key=lambda f: abs(f['callbackMs']-p['callbackMs']))
        skew = abs(l['callbackMs']-p['callbackMs'])
        yaw_skew = abs(l['actualYaw']-p['actualYaw'])
        if skew <= 2 and yaw_skew <= 0.02 and l['id'] not in used:
            paired.append((l, p, skew, yaw_skew))
            used.add(l['id'])
    assert paired

    rows = []
    profiles = []
    for l, p, skew, yaw_skew in paired:
        a, b = luma(l), luma(p)
        join = np.abs(a[:, -1]-b[:, 0])
        near_l = np.abs(a[:, -1]-a[:, -2])
        near_r = np.abs(b[:, 1]-b[:, 0])
        reference = (near_l+near_r)/2
        excess = np.maximum(0, join-reference)
        local = np.concatenate((np.abs(np.diff(a[:, -9:], axis=1)),
                                np.abs(np.diff(b[:, :9], axis=1))), axis=1)
        local_median = np.median(local, axis=1)
        smooth = np.maximum(near_l, near_r) <= 2
        edge = np.maximum(near_l, near_r) > 20
        row = {'leftId': l['id'], 'primaryId': p['id'],
               'yaw': round(p['actualYaw'], 4), 'virtualMs': round(p['virtualMs'], 3),
               'direction': 'forward' if p['virtualMs'] <= 2000 else 'reverse',
               'skewMs': round(skew, 3), 'yawSkew': round(yaw_skew, 5),
               'joinMean': round(float(join.mean()), 3),
               'referenceMean': round(float(reference.mean()), 3),
               'joinP95': percentile(join, 95), 'referenceP95': percentile(reference, 95),
               'excessMean': round(float(excess.mean()), 3),
               'hardRows': int(np.sum(excess > 20)),
               'smoothRows': int(smooth.sum()),
               'smoothJoinGt10': int(np.sum(smooth & (join > 10))),
               'smoothExcessGt20': int(np.sum(smooth & (excess > 20))),
               'smoothOutlierY': (np.flatnonzero(smooth & (excess > 20))+800).tolist(),
               'edgeRows': int(edge.sum()),
               'edgeHardRows': int(np.sum(edge & (excess > 20))),
               'localMedianMean': round(float(local_median.mean()), 3),
               'hardCentroidY': round(float(np.flatnonzero(excess > 20).mean()+800), 2)
                   if np.any(excess > 20) else None}
        rows.append(row)
        profiles.append(excess)

    profiles = np.stack(profiles)
    groups = {}
    for target, radius in ((-6, 1), (4, 1), (6.8, .5)):
        for direction in ('forward', 'reverse'):
            selected = [r for r in rows if abs(r['yaw']-target) <= radius and r['direction'] == direction]
            selected_idx = [i for i, r in enumerate(rows)
                            if abs(r['yaw']-target) <= radius and r['direction'] == direction]
            assert selected
            hard_frequencies = (profiles[selected_idx] > 20).mean(axis=0)
            groups[f'{target}:{direction}'] = {
                'pairs': len(selected),
                'joinMeanMedian': percentile([r['joinMean'] for r in selected], 50),
                'referenceMeanMedian': percentile([r['referenceMean'] for r in selected], 50),
                'excessMeanMedian': percentile([r['excessMean'] for r in selected], 50),
                'hardRowsMedian': percentile([r['hardRows'] for r in selected], 50),
                'hardRowsMax': max(r['hardRows'] for r in selected),
                'smoothJoinGt10': sum(r['smoothJoinGt10'] for r in selected),
                'smoothRows': sum(r['smoothRows'] for r in selected),
                'smoothExcessGt20': sum(r['smoothExcessGt20'] for r in selected),
                'edgeHardRows': sum(r['edgeHardRows'] for r in selected),
                'hardRowsTotal': sum(r['hardRows'] for r in selected),
                'rowsHardAtLeastHalfFrames': int(np.sum(hard_frequencies >= .5)),
                'maximumHardRowFrequency': round(float(hard_frequencies.max()), 3),
                'closest': min(selected, key=lambda r: abs(r['yaw']-target))}

            # Compare the join with 16 other first differences in the same
            # narrow physical neighborhood, not with a different scene pose.
            gradients = []
            for index in selected_idx:
                l, p, _, _ = paired[index]
                stitched = np.concatenate((luma(l)[:, -9:], luma(p)[:, :9]), axis=1)
                gradients.append(np.abs(np.diff(stitched, axis=1)).mean(axis=0))
            column_means = np.stack(gradients).mean(axis=0)
            join_mean = float(column_means[8])
            controls = np.delete(column_means, 8)
            groups[f'{target}:{direction}']['columnGradient'] = {
                'joinMean': round(join_mean, 3),
                'controlMedian': round(float(np.median(controls)), 3),
                'controlP95': percentile(controls, 95),
                'joinRankOf17': int(1+np.sum(column_means > join_mean))}

    callback = {}
    for name, stream in [('left', left), ('primary', primary)]:
        gaps = []
        for before, after in zip(stream, stream[1:]):
            dt = after['callbackMs']-before['callbackMs']
            if dt > 50:
                gaps.append({'afterId': before['id'], 'beforeYaw': round(before['actualYaw'], 3),
                             'afterYaw': round(after['actualYaw'], 3), 'ms': round(dt, 3)})
        callback[name] = {'frames': len(stream), 'gapsOver50': gaps,
                          'p50Ms': percentile([b['callbackMs']-a['callbackMs'] for a,b in zip(stream,stream[1:])],50),
                          'p95Ms': percentile([b['callbackMs']-a['callbackMs'] for a,b in zip(stream,stream[1:])],95)}

    temporal = {}
    for display, stream, seam_column, control_columns in (
        ('left', left, 191, list(range(183,191))),
        ('primary', primary, 0, list(range(1,9)))):
        for target, radius in ((-6, 1), (4, 1), (6.8, .5)):
            for direction in ('forward','reverse'):
                seam_values, control_values = [], []
                triples = 0
                for a,b,c in zip(stream,stream[1:],stream[2:]):
                    if (b['virtualMs'] <= 2000) != (direction == 'forward'):
                        continue
                    if abs(b['actualYaw']-target)>radius or b['id']!=a['id']+1 or c['id']!=b['id']+1:
                        continue
                    if max(b['callbackMs']-a['callbackMs'],c['callbackMs']-b['callbackMs'])>50:
                        continue
                    weight = (b['actualYaw']-a['actualYaw'])/(c['actualYaw']-a['actualYaw'])
                    residual = np.abs(luma(b)-(luma(a)*(1-weight)+luma(c)*weight))
                    seam_values.extend(residual[:,seam_column].tolist())
                    control_values.extend(residual[:,control_columns].ravel().tolist())
                    triples += 1
                temporal[f'{display}:{target}:{direction}'] = {
                    'triples':triples,
                    'seamP95':percentile(seam_values,95),
                    'controlsP95':percentile(control_values,95),
                    'seamP99':percentile(seam_values,99),
                    'controlsP99':percentile(control_values,99),
                    'seamMax':round(max(seam_values),3) if seam_values else None,
                    'controlsMax':round(max(control_values),3) if control_values else None}

    return {'run': path.parent.parent.name, 'mode': data['mode'], 'tightPairs': len(paired),
            'geometry': {'leftLastGlobalX': 3839, 'primaryFirstGlobalX': 3840,
                         'virtualY': [800, 1349], 'pixelOrientation': 'top-down after raw row reversal'},
            'groups': groups, 'callback': callback, 'temporal':temporal,
            'rows': rows}, rows, profiles


def compare_runs(first, second):
    _, ar, ap = first
    _, br, bp = second
    comparisons = {}
    for target, radius in ((-6, 1), (4, 1), (6.8, .5)):
        for direction in ('forward','reverse'):
            left = [(r,p) for r,p in zip(ar,ap) if r['direction']==direction and abs(r['yaw']-target)<=radius]
            right = [(r,p) for r,p in zip(br,bp) if r['direction']==direction and abs(r['yaw']-target)<=radius]
            diffs = []
            yaw_diffs = []
            for r,p in left:
                match, q = min(right,key=lambda item:abs(item[0]['yaw']-r['yaw']))
                dyaw = abs(match['yaw']-r['yaw'])
                if dyaw <= .12:
                    diffs.append(float(np.mean(np.abs(p-q))))
                    yaw_diffs.append(dyaw)
            comparisons[f'{target}:{direction}'] = {'pairs':len(diffs),
                 'medianProfileMAE':percentile(diffs,50), 'p95ProfileMAE':percentile(diffs,95),
                 'maxYawDifference':round(max(yaw_diffs),3) if yaw_diffs else None}
    return comparisons


if __name__ == '__main__':
    results = [analyze(Path(arg).resolve()) for arg in sys.argv[1:]]
    summary = {'schema': 1, 'runs':[item[0] for item in results]}
    if len(results)>=2:
        summary['nativeRepeatProfileComparison'] = compare_runs(results[0],results[1])
    output = Path(__file__).with_name('STAGE_B1_R5_SEAM_METRICS_V4.json')
    if output.exists():
        raise FileExistsError(output)
    output.write_text(json.dumps(summary,indent=2),encoding='utf-8')
    print(json.dumps({'output':str(output),'runs':[(r[0]['run'],r[0]['tightPairs']) for r in results]},indent=2))
