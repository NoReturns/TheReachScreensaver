from pathlib import Path
import json
from PIL import Image, ImageDraw

base = Path(__file__).parent
controls = json.loads((base / 'r16-runs' / 'summary.json').read_text(encoding='utf-8'))['captures']
candidate = {}
for path in (base / 'r18-runs').glob('run-*/r18-manifest.json'):
    item = json.loads(path.read_text(encoding='utf-8'))
    if item['exitCode'] == 0:
        candidate[item['case']] = Path(item['run'])
review = base / 'r18-runs' / 'review'
review.mkdir(exist_ok=True)
summary = {}
for case in ('hole-yaw0', 'peripheral', 'deep-space'):
    old = Path(controls[case]['runs']['unbounded'])
    new = candidate[case]
    old_metrics = json.loads(next((old / 'metrics').glob('fixture-*.json')).read_text(encoding='utf-8'))
    new_metrics = json.loads(next((new / 'metrics').glob('fixture-*.json')).read_text(encoding='utf-8'))
    assert old_metrics['stateKey'] == new_metrics['stateKey']
    summary[case] = {'controlRun': str(old), 'candidateRun': str(new),
                     'controlState': old_metrics['stateKey'],
                     'candidateState': new_metrics['stateKey'],
                     'displays': {}}
    for id, name in ((1, 'left'), (3, 'primary'), (2, 'right')):
        a = next(old.glob(f'captures/*DISPLAY{id}.png'))
        b = next(new.glob(f'captures/*DISPLAY{id}.png'))
        ia, ib = Image.open(a).convert('RGB'), Image.open(b).convert('RGB')
        assert ia.size == ib.size
        summary[case]['displays'][name] = {'control': str(a), 'candidate': str(b),
                                           'physicalSize': ia.size}
        # Whole-display pair is downscaled only for quick navigation. Raw PNGs
        # above remain full physical resolution for actual inspection.
        scale = min(1, 1200 / ia.width)
        size = (round(ia.width * scale), round(ia.height * scale))
        pair = Image.new('RGB', (size[0] * 2, size[1] + 30))
        pair.paste(ia.resize(size), (0, 30))
        pair.paste(ib.resize(size), (size[0], 30))
        draw = ImageDraw.Draw(pair)
        draw.text((8, 8), f'{case} {name} current bounding-height', fill='white')
        draw.text((size[0] + 8, 8), 'primary-height full source', fill='white')
        pair.save(review / f'{case}-{name}-overview.png')
(review / 'capture-index.json').write_text(json.dumps(summary, indent=2), encoding='utf-8')
print(review / 'capture-index.json')
