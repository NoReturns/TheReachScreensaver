from pathlib import Path
from shutil import copytree
from hashlib import sha256
import json

base = Path(__file__).parent
source = base / 'r12-fixture-dist-v2'
output = base / 'r15-variants'
output.mkdir(exist_ok=True)
needle = 'samples:Math.min(4,this.renderer.capabilities.maxSamples)'
status = 'sectorTargets:this.targets.map(e=>[e.width,e.height])'
rows = []
for label, replacement in [('4x', needle), ('2x', 'samples:Math.min(2,this.renderer.capabilities.maxSamples)'),
                           ('1x', 'samples:0')]:
    target = output / label
    if target.exists():
        raise SystemExit(f'Variant exists: {target}')
    copytree(source, target)
    bundle = next((target / 'assets').glob('index-*.js'))
    content = bundle.read_text(encoding='utf-8')
    if content.count(needle) != 1 or content.count(status) != 1:
        raise SystemExit('Bundle expression is not unique')
    content = content.replace(needle, replacement).replace(
        status, 'sectorTargets:this.targets.map(e=>[e.width,e.height,e.samples])')
    bundle.write_text(content, encoding='utf-8')
    (target / 'reach-developer-payload.txt').write_text(
        'VITE_REACH_CYLINDRICAL_DEV=1;VITE_REACH_DEV_TEST=1', encoding='utf-8')
    rows.append({'label': label, 'sectorExpression': replacement, 'bundle': str(bundle),
                 'sha256': sha256(bundle.read_bytes()).hexdigest()})
(output / 'manifest.json').write_text(json.dumps(rows, indent=2), encoding='utf-8')
print(output / 'manifest.json')
