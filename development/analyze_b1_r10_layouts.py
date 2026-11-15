"""Source-geometry fixture for representative supported virtual desktops."""

import json
import math
from pathlib import Path

focal = 2160 / (2 * math.tan(math.radians(25)))
shift = 1216
half = math.radians(35)
step = math.radians(50)

layouts = {
    'single-3840x2160': (3840, [(0, 0, 3840, 2160)]),
    'dual-6400x2160': (6400, [(0, 0, 3840, 2160), (3840, 720, 2560, 1440)]),
    'recorded-8320x2160': (8320, [(0, 0, 3840, 2160),
                                   (3840, 720, 2560, 1440),
                                   (6400, 1080, 1920, 1080)]),
}

def centers(width):
    first = (-width / 2 - shift) / focal
    right = (width / 2 - shift) / focal
    values = []
    center = first + math.radians(25)
    while len(values) <= 8:
        values.append(center)
        if center + half >= right:
            return values
        center += step
    raise ValueError('capacity exceeded')

def exact_selected(values, desktop, display):
    x, y, width, height = display
    start = max(0, x - 512)
    end = min(desktop, x + width + 512)
    first = (start - desktop / 2 - shift) / focal
    last = (end - desktop / 2 - shift) / focal
    result = []
    for i, center in enumerate(values):
        a = (values[i-1] + center)/2 if i else -math.inf
        b = (center + values[i+1])/2 if i+1 < len(values) else math.inf
        if b >= first - 1e-8 and a <= last + 1e-8:
            result.append(center)
    return result

report = {}
for name, (width, displays) in layouts.items():
    values = centers(width)
    selected_by_display = [exact_selected(values, width, d) for d in displays]
    report[name] = {
        'virtual': [width, 2160], 'displays': displays,
        'globalCentersDeg': [round(math.degrees(x), 6) for x in values],
        'selectedCentersDeg': [[round(math.degrees(x), 6) for x in group]
                               for group in selected_by_display],
        'joins': [{'x': displays[i][0] + displays[i][2],
                   'sharedCentersDeg': [round(math.degrees(x), 6)
                                        for x in set(selected_by_display[i]).intersection(
                                            selected_by_display[i+1])],
                   'leftOnlyDeg': [round(math.degrees(x), 6)
                                   for x in set(selected_by_display[i]).difference(
                                       selected_by_display[i+1])],
                   'rightOnlyDeg': [round(math.degrees(x), 6)
                                    for x in set(selected_by_display[i+1]).difference(
                                        selected_by_display[i])]}
                  for i in range(len(displays)-1)]}

out = Path('development/STAGE_B1_R10_LAYOUT_GEOMETRY.json')
out.write_text(json.dumps(report, indent=2))
print(json.dumps(report, indent=2))
