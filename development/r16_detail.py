import json
from pathlib import Path
from PIL import Image, ImageDraw

root = Path(__file__).parent / 'r16-runs'
variants = ['unbounded', 'cap2433', 'cap1622']
boxes = [('peripheral', (2970, 790, 3090, 910), 'planet limb'),
         ('deep-space', (1257, 716, 1337, 796), 'small stars')]
sheet = Image.new('RGB', (3 * 480, 2 * 520), (16, 16, 20))
draw = ImageDraw.Draw(sheet)
for row, (case, box, title) in enumerate(boxes):
    for col, variant in enumerate(variants):
        run = next(p.parent for p in root.glob('run-*/r16-manifest.json')
                   if (m := json.loads(p.read_text()))['variant'] == variant and m['case'] == case)
        manifest = json.loads(next((run / 'metrics').glob('fixture-*.json')).read_text())
        path = next(Path(c['path']) for c in manifest['captures'] if c['display'].endswith('DISPLAY1'))
        with Image.open(path) as im:
            crop = im.convert('RGB').crop(box).resize((480, 480), Image.Resampling.NEAREST)
        sheet.paste(crop, (col * 480, row * 520 + 24))
        draw.text((col * 480 + 6, row * 520 + 5), f'{title} {variant} 4x nearest', fill='white')
sheet.save(root / 'R16_DETAIL_ZOOM.png')
print(root / 'R16_DETAIL_ZOOM.png')
