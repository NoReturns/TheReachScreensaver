"""Make inspectable joined strips from retained R4 final-output frames."""
import json
from pathlib import Path

import numpy as np
from PIL import Image, ImageDraw


BASE = Path(__file__).parent / 'runs'
CASES = [
    ('native repeat 1 reverse +6.70', 'run-cd2f2784d3ea46a6bddade97a4d3133c',
     'b1-cylindrical-643b09a55a25433f96292a97535b603c.json', 126, 132),
    ('native repeat 2 forward +6.82', 'run-8e997118d7f5434ab523480d66cd400c',
     'b1-cylindrical-115fe3339d004a92be1eb17c91d3917c.json', 88, 89),
    ('native repeat 2 reverse +6.80', 'run-8e997118d7f5434ab523480d66cd400c',
     'b1-cylindrical-115fe3339d004a92be1eb17c91d3917c.json', 127, 127),
    ('native repeat 2 forward -6.02', 'run-8e997118d7f5434ab523480d66cd400c',
     'b1-cylindrical-115fe3339d004a92be1eb17c91d3917c.json', 11, 11),
]


full = Image.new('RGB', (384*len(CASES), 580), 'black')
zoom = Image.new('RGB', (256*len(CASES), 740), 'black')
draw_full = ImageDraw.Draw(full)
draw_zoom = ImageDraw.Draw(zoom)
for i, (label, run, manifest, left_id, primary_id) in enumerate(CASES):
    data = json.loads((BASE/run/'metrics'/manifest).read_text())
    left = next(f for f in data['frames'] if f['crop']['x'] == 3648 and f['id'] == left_id)
    primary = next(f for f in data['frames'] if f['crop']['x'] == 0 and f['id'] == primary_id)
    def picture(frame):
        raw = np.fromfile(frame['path'],dtype=np.uint8).reshape(550,192,4)[::-1,:,:3]
        return Image.fromarray(raw)
    joined = Image.new('RGB',(384,550))
    joined.paste(picture(left),(0,0))
    joined.paste(picture(primary),(192,0))
    full.paste(joined,(384*i,30))
    draw_full.text((384*i+3,7),label,fill='white')
    # x 176..207 and virtual y 940..1119, displayed at 4x.
    crop = joined.crop((176,140,208,320)).resize((128,720),Image.Resampling.NEAREST)
    zoom.paste(crop,(256*i+64,20))
    draw_zoom.text((256*i+3,3),label,fill='white')
full.save(Path(__file__).with_name('STAGE_B1_R5_FULL_STRIPS.png'))
zoom.save(Path(__file__).with_name('STAGE_B1_R5_SEAM_ZOOM.png'))
