"""Append R22 diagnostics only to fresh copies of the R20 developer bundles."""
from pathlib import Path
from shutil import copytree
from hashlib import sha256
import json

root = Path(__file__).resolve().parent
source = root / 'r22_browser.js'
out = root / 'r22-variants'
if out.exists():
    raise SystemExit(f'Refusing to overwrite {out}')
rows = []
for name in ('control-2x', 'static-2x'):
    target = out / name
    copytree(root / 'r20-variants' / name, target)
    bundle = next((target / 'assets').glob('index-*.js'))
    with bundle.open('a', encoding='utf-8') as stream:
        stream.write('\n' + source.read_text(encoding='utf-8') + '\n')
    rows.append({'variant': name, 'bundle': str(bundle), 'sha256': sha256(bundle.read_bytes()).hexdigest()})
out.mkdir(exist_ok=True)
(out / 'manifest.json').write_text(json.dumps(rows, indent=2), encoding='utf-8')
