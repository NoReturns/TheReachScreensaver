from pathlib import Path
from shutil import copytree
from hashlib import sha256
import json

base = Path(__file__).parent
source = base / 'r16-variants'
output = base / 'r17-join-variants'
if output.exists():
    raise SystemExit('R17 join variant directory already exists')
addon = (base / 'r17_browser.js').read_text(encoding='utf-8')
old_crop = "?{x:2350,y:500,width:1200,height:720}"
new_crop = "?{x:2640,y:500,width:1200,height:720}"
old_path = "base+16*(virtualMs/60000)"
new_path = "base+24*(virtualMs/60000)"
if addon.count(old_crop) != 1 or addon.count(old_path) != 1:
    raise SystemExit('R17 join patch anchor mismatch')
addon = addon.replace(old_crop, new_crop).replace(old_path, new_path)
rows = []
for label in ('unbounded', 'cap2433'):
    target = output / label
    copytree(source / label, target)
    bundle = next((target / 'assets').glob('index-*.js'))
    with bundle.open('a', encoding='utf-8') as stream:
        stream.write('\n' + addon + '\n')
    rows.append({'label': label, 'source': str(source / label),
                 'bundle': str(bundle), 'sha256': sha256(bundle.read_bytes()).hexdigest(),
                 'trajectory': 'planet yaw 23 to 47 degrees over 60 seconds',
                 'leftCrop': [2640, 500, 1200, 720]})
(output / 'manifest.json').write_text(json.dumps(rows, indent=2), encoding='utf-8')
print(output / 'manifest.json')
