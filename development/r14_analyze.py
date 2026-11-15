import json
import math
import re
import statistics
from datetime import datetime, timedelta
from pathlib import Path

ROOT = Path(__file__).parent / 'r14-runs'
PID = re.compile(r'pid_(\d+)_')


def percentile(values, q):
    values = sorted(values)
    if not values:
        return None
    pos = (len(values) - 1) * q
    low = math.floor(pos)
    high = math.ceil(pos)
    return values[low] + (values[high] - values[low]) * (pos - low)


def describe(values):
    return {key: round(value, 3) if value is not None else None for key, value in {
        'median': percentile(values, .5), 'p95': percentile(values, .95),
        'p99': percentile(values, .99), 'min': min(values) if values else None,
        'max': max(values) if values else None,
    }.items()}


def summarize(run):
    manifest = json.loads((run / 'run-manifest.json').read_text())
    report = json.loads(next((run / 'metrics').glob('r14-*.json')).read_text())
    pids = {report['hostPid']}
    for snapshot in report['snapshots']:
        pids.update(p['pid'] for p in snapshot['processes'])
    memory = []
    adapter = []
    engine = []
    cpu = []
    samples = [json.loads(line) for line in (run / 'counter-samples.jsonl').read_text().splitlines()]
    measure_start = datetime.fromisoformat(report['startUtc'].replace('Z', '+00:00')) + timedelta(seconds=10)
    measure_end = measure_start + timedelta(seconds=180)
    for sample in samples:
        t = datetime.fromisoformat(sample['utc'].replace('Z', '+00:00'))
        if not measure_start <= t <= measure_end:
            continue
        parts = sample.get('nvidia', '').split(',')
        if len(parts) == 5:
            try:
                adapter.append({'utc': sample['utc'], 'memoryMiB': float(parts[0]),
                                'gpuPercent': float(parts[1]), 'memoryPercent': float(parts[2]),
                                'temperatureC': float(parts[3]), 'powerW': float(parts[4])})
            except ValueError:
                pass
        dedicated = 0
        three_d = 0
        for counter in sample.get('counters', []):
            path = counter['path'].lower()
            match = PID.search(path)
            if not match or int(match.group(1)) not in pids:
                continue
            if 'dedicated usage' in path:
                dedicated += counter['value']
            if 'engtype_3d' in path and 'utilization percentage' in path:
                three_d += counter['value']
        memory.append({'utc': sample['utc'], 'dedicatedMiB': dedicated / 1048576})
        engine.append({'utc': sample['utc'], 'threeDPercentSum': three_d})
        cpu.append({'utc': sample['utc'], 'values': {p['Id']: p['CPU'] for p in sample['processes']
                    if p['Id'] in pids and p['CPU'] is not None}})
    windows = []
    for record in report['records']:
        intervals = record['intervalsMs']
        times = record['callbackTimesMs']
        begin = times[0] if times else 0
        first = [v for v, t in zip(intervals, times[1:]) if t - begin < 60000]
        last = [v for v, t in zip(intervals, times[1:]) if t - begin >= 120000]
        median = percentile(intervals, .5)
        windows.append({'display': record['display'], 'phase': record['phase'],
                        'warmupFrames': record['warmupFrames'],
                        'frames': record['measuredFrames'],
                        'effectiveFps': round(record['measuredFrames'] / 180, 3),
                        'intervalMs': describe(intervals),
                        'renderCpuMs': describe(record['renderMs']),
                        'firstMinuteMedianMs': round(percentile(first, .5), 3),
                        'lastMinuteMedianMs': round(percentile(last, .5), 3),
                        'over1p5Median': sum(x > 1.5 * median for x in intervals),
                        'over2Median': sum(x > 2 * median for x in intervals),
                        'motionChanges': record['motionChanges'],
                        'contextLost': record['contextLost'],
                        'glError': record['glError'], 'errors': record['errors']})
    snapshots = report['snapshots']
    ws = [sum(p['workingSet'] for p in s['processes'] if p['workingSet'] >= 0) / 1048576
          for s in snapshots]
    cpu_deltas = []
    for first, last in zip(cpu, cpu[1:]):
        elapsed = (datetime.fromisoformat(last['utc']) - datetime.fromisoformat(first['utc'])).total_seconds()
        if elapsed <= 0:
            continue
        common = first['values'].keys() & last['values'].keys()
        cpu_deltas.append(100 * sum(max(0, last['values'][pid] - first['values'][pid])
                                    for pid in common) / elapsed)
    def end_medians(items, key):
        if not items:
            return None
        count = min(15, len(items) // 2)
        return [round(percentile([x[key] for x in items[:count]], .5), 2),
                round(percentile([x[key] for x in items[-count:]], .5), 2)]
    return {'case': manifest['case'], 'run': str(run), 'fixture': manifest['fixture'],
            'mode': manifest['mode'], 'durationSeconds': report['durationSeconds'],
            'startUtc': report['startUtc'], 'endUtc': report['endUtc'],
            'counterSamples': len(samples), 'inMeasurementSamples': len(memory),
            'webviewPids': sorted(pids - {report['hostPid']}), 'windows': windows,
            'adapterGpuPercent': describe([s['gpuPercent'] for s in adapter]),
            'adapterMemoryMiB': describe([s['memoryMiB'] for s in adapter]),
            'adapterPowerW': describe([s['powerW'] for s in adapter]),
            'webviewDedicatedMiB': describe([s['dedicatedMiB'] for s in memory]),
            'webviewDedicatedFirstLastMedianMiB': end_medians(memory, 'dedicatedMiB'),
            'webview3dEnginePercentSum': describe([s['threeDPercentSum'] for s in engine]),
            'cpuCoreEquivalentPercent': describe(cpu_deltas),
            'webviewWorkingSetMiB': describe(ws),
            'webviewWorkingSetFirstLastMiB': [round(ws[0], 2), round(ws[-1], 2)]}


if __name__ == '__main__':
    output = [summarize(run) for run in ROOT.glob('run-*') if (run / 'run-manifest.json').exists()]
    output.sort(key=lambda x: x['case'])
    path = ROOT / 'summary.json'
    path.write_text(json.dumps(output, indent=2))
    print(path)
