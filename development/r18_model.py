"""Read-only geometry model for the isolated R18 architecture audit."""
from pathlib import Path
import json
import math

DEG = math.pi / 180
TAN25 = math.tan(25 * DEG)
TAN30 = math.tan(30 * DEG)
TAN35 = math.tan(35 * DEG)
GUARD = 512

layouts = {
    'single-4k': [
        dict(name='primary-4k', x=0, y=0, w=3840, h=2160, primary=True),
    ],
    'current-three': [
        dict(name='left-4k', x=0, y=0, w=3840, h=2160, primary=False),
        dict(name='Windows-primary', x=3840, y=720, w=2560, h=1440, primary=True),
        dict(name='right-1080p', x=6400, y=1080, w=1920, h=1080, primary=False),
    ],
    'fourth-above': [
        dict(name='new-above-left', x=0, y=0, w=3840, h=2160, primary=False),
        dict(name='left-4k', x=0, y=2160, w=3840, h=2160, primary=False),
        dict(name='Windows-primary', x=3840, y=2880, w=2560, h=1440, primary=True),
        dict(name='right-1080p', x=6400, y=3240, w=1920, h=1080, primary=False),
    ],
    'four-horizontal': [
        dict(name='left-4k', x=0, y=0, w=3840, h=2160, primary=False),
        dict(name='Windows-primary', x=3840, y=720, w=2560, h=1440, primary=True),
        dict(name='right-1080p', x=6400, y=1080, w=1920, h=1080, primary=False),
        dict(name='new-right-1080p', x=8320, y=1080, w=1920, h=1080, primary=False),
    ],
    'mixed-asymmetric': [
        dict(name='left-1440p', x=0, y=2160, w=2560, h=1440, primary=False),
        dict(name='Windows-primary-1080p', x=2560, y=2520, w=1920, h=1080, primary=True),
        dict(name='upper-right-4k', x=4480, y=0, w=3840, h=2160, primary=False),
        dict(name='lower-right', x=8320, y=2160, w=1280, h=1024, primary=False),
    ],
}


def angles(a, b, center, focal):
    return [round(math.atan((a - center) / focal) / DEG, 3),
            round(math.atan((b - center) / focal) / DEG, 3)]


def evaluate(displays, mode):
    width = max(d['x'] + d['w'] for d in displays)
    height = max(d['y'] + d['h'] for d in displays)
    primary, = [d for d in displays if d['primary']]
    if mode == 'A-bounding':
        focal = height / (2 * TAN25)
        anchor_y = height / 2
    elif mode == 'B-primary-height':
        focal = primary['h'] / (2 * TAN25)
        anchor_y = height / 2
    else:
        focal = 1440 / (2 * TAN25)
        anchor_y = primary['y'] + primary['h'] / 2
    # The fixture's +0.1-primary-width composition offset is kept in the
    # horizontal anchor. It changes rays but cancels from sector selection.
    anchor_x = primary['x'] + primary['w'] * .6
    left = -anchor_x / focal
    right = (width - anchor_x) / focal
    centers = []
    c = left + 25 * DEG
    for _ in range(9):
        centers.append(c)
        if c + 35 * DEG >= right:
            break
        c += 50 * DEG
    complete = centers[-1] + 35 * DEG >= right
    monitors = []
    total_instances = 0
    required_tan = TAN30
    for d in displays:
        x0 = max(0, d['x'] - GUARD)
        x1 = min(width, d['x'] + d['w'] + GUARD)
        y0 = max(0, d['y'] - GUARD)
        y1 = min(height, d['y'] + d['h'] + GUARD)
        first = (x0 - anchor_x) / focal
        last = (x1 - anchor_x) / focal
        selected = []
        for i, center in enumerate(centers):
            a = (centers[i - 1] + center) / 2 if i else -math.inf
            b = (center + centers[i + 1]) / 2 if i + 1 < len(centers) else math.inf
            if b >= first - 1e-8 and a <= last + 1e-8:
                selected.append(i)
        total_instances += len(selected)
        # Nearest-center offset reaches its maximum at a display edge or at
        # any sector midpoint within the padded X interval.
        points = [first, last]
        points += [(centers[i] + centers[i + 1]) / 2 for i in range(len(centers) - 1)
                   if first <= (centers[i] + centers[i + 1]) / 2 <= last]
        delta = max(min(abs(point - centers[i]) for i in selected) for point in points)
        tan_needed = max(abs(anchor_y - y0), abs(anchor_y - y1)) / focal / math.cos(delta)
        required_tan = max(required_tan, tan_needed)
        monitors.append(dict(name=d['name'], primary=d['primary'],
                             rect=[d['x'], d['y'], d['w'], d['h']],
                             horizontalDegrees=angles(d['x'], d['x'] + d['w'], anchor_x, focal),
                             verticalDegrees=angles(anchor_y - d['y'] - d['h'], anchor_y - d['y'], 0, focal),
                             sectorIndices=selected,
                             selectedCentersDegrees=[round(centers[i] / DEG, 3) for i in selected],
                             paddedRect=[x0, y0, x1 - x0, y1 - y0],
                             maxDeltaDegrees=round(delta / DEG, 3),
                             requiredSourceHalfFovDegrees=round(math.atan(tan_needed) / DEG, 3)))
    width_px = math.ceil(2 * focal * TAN35)
    current_height_px = math.ceil(2 * focal * TAN30)
    full_height_px = math.ceil(2 * focal * required_tan)
    source_fov = 2 * math.atan(required_tan) / DEG
    physical = sum(d['w'] * d['h'] for d in displays)
    pixels_full = total_instances * width_px * full_height_px
    return dict(desktop=[width, height], primary=primary['name'], mode=mode,
                opticalAnchor=[round(anchor_x, 3), round(anchor_y, 3)],
                focal=round(focal, 3), globalSectorCount=len(centers),
                globalCentersDegrees=[round(c / DEG, 3) for c in centers],
                capacityValid=complete and len(centers) <= 8,
                sectorInstances=total_instances,
                fixed60SourceCoversOutput=required_tan <= TAN30 + 1e-10,
                requiredSourceVerticalFovDegrees=round(source_fov, 3),
                fixed60Sector=[width_px, current_height_px],
                fullQualitySector=[width_px, full_height_px],
                physicalPixels=physical, sectorPixelsFull=pixels_full,
                sectorToPhysicalPixelRatio=round(pixels_full / physical, 3),
                sector2xMiB=round(pixels_full * 36 / 1048576, 1),
                monitors=monitors)


result = {layout: {mode: evaluate(displays, mode) for mode in
                   ('A-bounding', 'B-primary-height', 'C-fixed1440-primary-anchor')}
          for layout, displays in layouts.items()}
output = Path(__file__).parent / 'r18-model.json'
output.write_text(json.dumps(result, indent=2), encoding='utf-8')
print(output)
