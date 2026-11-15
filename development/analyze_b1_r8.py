import glob
import json
import sys

root = sys.argv[1]
path = glob.glob(root + '/metrics/source-probe-*.json')[0]
data = json.load(open(path, encoding='utf-8'))
by_display = {r['display']: r for r in data['records']}
left, primary = by_display['left'], by_display['primary']
assert left['stateKey'] == primary['stateKey']
assert left['region'] == primary['region'] == {'x': 3838, 'y': 970, 'width': 4, 'height': 100}

def pixel(display, mode, channel, x, y):
    rec = next(r for r in by_display[display]['records'] if r['mode'] == mode)
    rows = rec['linearRgb'] if channel == 0 else rec['traces'][channel - 1]['rows']
    return rows[y - 970]['pixels'][x - 3838]

def delta(a, b):
    return max(abs(x - y) for x, y in zip(a[:3], b[:3]))

print('evidence', path)
for display in ('left', 'primary'):
    d = by_display[display]
    print('display', display, 'hashes', d['ordinaryOutputBefore']['sha256'], d['ordinaryOutputAfter']['sha256'], 'match', d['ordinaryOutputMatch'], 'contextLost', d['contextLost'])
    print('source', d['addedSource'])
    for r in d['records']:
        print('branch', display, r['mode'], r['linearRgbHash'])
for mode in ('normal', 'cube', 'sector'):
    ds = [delta(pixel('left', mode, 0, x, y), pixel('primary', mode, 0, x, y))
          for y in range(970, 1070) for x in range(3838, 3842)]
    print('cross_context', mode, 'mean', sum(ds) / len(ds), 'max', max(ds), 'nonzero', sum(x != 0 for x in ds))
for display in ('left', 'primary'):
    ds = [delta(pixel(display, 'cube', 0, x, y), pixel(display, 'sector', 0, x, y))
          for y in range(970, 1070) for x in range(3838, 3842)]
    print('cube_sector', display, 'mean', sum(ds) / len(ds), 'max', max(ds))
for y in (990, 1008, 1012, 1020, 1030, 1033, 1034, 1035, 1040, 1050, 1060):
    print('row', y)
    for x in (3839, 3840):
        print('pixel', x, 'dir', pixel('left', 'normal', 1, x, y), pixel('primary', 'normal', 1, x, y),
              'cube_xyz', pixel('left', 'normal', 4, x, y), pixel('primary', 'normal', 4, x, y),
              'sector_uv', pixel('left', 'sector', 2, x, y), pixel('primary', 'sector', 2, x, y),
              'cube_sample', pixel('left', 'sector', 3, x, y), pixel('primary', 'sector', 3, x, y),
              'sector_sample', pixel('left', 'sector', 5, x, y), pixel('primary', 'sector', 5, x, y),
              'lens', {mode: (pixel('left', mode, 0, x, y), pixel('primary', mode, 0, x, y)) for mode in ('normal', 'cube', 'sector')})
for channel in (1, 3, 4, 5):
    ds = [delta(pixel('left', 'sector', channel, x, y), pixel('primary', 'sector', channel, x, y))
          for y in range(970, 1070) for x in range(3838, 3842)]
    print('trace_cross', channel, 'max', max(ds), 'nonzero', sum(x != 0 for x in ds))
