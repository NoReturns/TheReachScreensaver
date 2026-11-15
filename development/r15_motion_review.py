import json
from pathlib import Path

from PIL import Image, ImageDraw

root = Path(__file__).parent / 'r15-runs'
variants = ['4x', '2x', '1x']
times = [400, 1000, 1680]
sheet = Image.new('RGB', (3 * 384, 3 * 580), (16, 16, 20))
draw = ImageDraw.Draw(sheet)
rows = []
for row, variant in enumerate(variants):
    run = next(p.parent for p in root.glob('run-*/r15-manifest.json')
               if (m := json.loads(p.read_text()))['variant'] == variant and m['case'] == 'motion-crop')
    data = json.loads(next((run / 'metrics').glob('b1-*.json')).read_text())
    for col, target in enumerate(times):
        chosen = {}
        for display, suffix in [('left', 'DISPLAY1'), ('primary', 'DISPLAY3')]:
            frames = [f for f in data['frames'] if f['display'].endswith(suffix) and f['path']]
            frame = min(frames, key=lambda f: abs(f['virtualMs'] - target))
            crop = frame['crop']
            assert crop['width'] == 192 and crop['height'] == 550
            im = Image.frombytes('RGBA', (192, 550), Path(frame['path']).read_bytes())
            chosen[display] = im.transpose(Image.Transpose.FLIP_TOP_BOTTOM).convert('RGB')
            rows.append({'variant': variant, 'targetMs': target, 'display': display,
                         'frameId': frame['id'], 'virtualMs': frame['virtualMs'],
                         'actualYaw': frame['actualYaw'], 'path': frame['path']})
        x = col * 384
        y = row * 580 + 24
        sheet.paste(chosen['left'], (x, y))
        sheet.paste(chosen['primary'], (x + 192, y))
        draw.text((x + 5, y - 18), f'{variant} t={target} ms left | primary', fill='white')
sheet.save(root / 'R15_MOTION_CROP_MONTAGE.png')
(root / 'motion-review-frames.json').write_text(json.dumps(rows, indent=2))
print(root / 'R15_MOTION_CROP_MONTAGE.png')
