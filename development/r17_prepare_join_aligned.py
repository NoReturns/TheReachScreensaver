from pathlib import Path
from shutil import copytree
from hashlib import sha256
import json

base = Path(__file__).parent
source = base / 'r16-variants'
output = base / 'r17-join-aligned-variants'
if output.exists():
    raise SystemExit('R17 aligned join variants already exist')
addon = (base / 'r17_browser.js').read_text(encoding='utf-8')
replacements = {
    "?{x:2350,y:500,width:1200,height:720}": "?{x:2640,y:720,width:1200,height:720}",
    "base+16*(virtualMs/60000)": "base+24*(virtualMs/60000)",
    "{x:0,y:700,width:320,height:700}": "{x:0,y:0,width:320,height:720}",
}
for old, new in replacements.items():
    if addon.count(old) != 1:
        raise SystemExit(f'R17 aligned join anchor mismatch: {old}')
    addon = addon.replace(old, new)
rows = []
for label in ('unbounded', 'cap2433'):
    target = output / label
    copytree(source / label, target)
    bundle = next((target / 'assets').glob('index-*.js'))
    with bundle.open('a', encoding='utf-8') as stream:
        stream.write('\n' + addon + '\n')
    rows.append({'label': label, 'bundle': str(bundle),
                 'sha256': sha256(bundle.read_bytes()).hexdigest(),
                 'trajectory': 'planet yaw 23 to 47 degrees over 60 seconds',
                 'leftCrop': [2640, 720, 1200, 720],
                 'primaryCrop': [0, 0, 320, 720],
                 'virtualYOverlap': [720, 1440]})
(output / 'manifest.json').write_text(json.dumps(rows, indent=2), encoding='utf-8')
print(output / 'manifest.json')
