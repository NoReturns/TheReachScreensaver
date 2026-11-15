from pathlib import Path
from shutil import copytree
from hashlib import sha256
import json

base = Path(__file__).parent
source = base / 'r15-variants' / '2x'
output = base / 'r16-variants'
output.mkdir(exist_ok=True)
old = ('r=Math.ceil(2*this.focal*yd*n),i=Math.ceil(2*this.focal*vd*n),a=')
old_aspect = 'this.sectorCamera.aspect=i/r'
rows = []
for label, cap in [('unbounded', None), ('cap2433', 2433), ('cap1622', 1622)]:
    target = output / label
    if target.exists():
        raise SystemExit(f'Variant exists: {target}')
    copytree(source, target)
    bundle = next((target / 'assets').glob('index-*.js'))
    content = bundle.read_text(encoding='utf-8')
    if content.count(old) != 1 or content.count(old_aspect) != 1:
        raise SystemExit('Source size or camera aspect expression is not unique')
    if cap is not None:
        scale = f'Math.min(1,{cap}/(2*this.focal*vd*n))'
        new = (f'r=Math.ceil(2*this.focal*yd*n*{scale}),'
               f'i=Math.ceil(2*this.focal*vd*n*{scale}),a=')
        content = content.replace(old, new)
        content = content.replace(old_aspect,
            'this.sectorCamera.aspect=Math.ceil(2*this.focal*vd*n)/Math.ceil(2*this.focal*yd*n)')
    bundle.write_text(content, encoding='utf-8')
    rows.append({'label': label, 'maxSectorWidth': cap, 'bundle': str(bundle),
                 'sha256': sha256(bundle.read_bytes()).hexdigest()})
(output / 'manifest.json').write_text(json.dumps(rows, indent=2), encoding='utf-8')
print(output / 'manifest.json')
