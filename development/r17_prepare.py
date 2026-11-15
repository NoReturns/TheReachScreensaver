from pathlib import Path
from shutil import copytree
from hashlib import sha256
import json

base = Path(__file__).parent
source = base / 'r16-variants'
output = base / 'r17-variants'
if output.exists():
    raise SystemExit('R17 variant directory already exists')
addon = (base / 'r17_browser.js').read_text(encoding='utf-8')
rows = []
for label in ('unbounded', 'cap2433'):
    target = output / label
    copytree(source / label, target)
    bundle = next((target / 'assets').glob('index-*.js'))
    with bundle.open('a', encoding='utf-8') as stream:
        stream.write('\n' + addon + '\n')
    rows.append({'label': label, 'source': str(source / label),
                 'bundle': str(bundle), 'sha256': sha256(bundle.read_bytes()).hexdigest()})
(output / 'manifest.json').write_text(json.dumps(rows, indent=2), encoding='utf-8')
print(output / 'manifest.json')
