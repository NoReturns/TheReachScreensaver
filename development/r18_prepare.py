"""Prepare an isolated, current-layout primary-height focal feasibility bundle."""
from pathlib import Path
from shutil import copytree
from hashlib import sha256
import json
import math

base = Path(__file__).parent
source = base / 'r16-variants' / 'unbounded'
output = base / 'r18-variants' / 'primary-height-full-source'
if output.exists():
    raise SystemExit('R18 candidate already exists')
copytree(source, output)
bundle = next((output / 'assets').glob('index-*.js'))
content = bundle.read_text(encoding='utf-8')
full_height = 2160
primary_height = 1440
focal = primary_height / (2 * math.tan(math.radians(25)))
model = json.loads((base / 'r18-model.json').read_text(encoding='utf-8'))
source_half_tan = math.tan(math.radians(
    (model['current-three']['B-primary-height']['requiredSourceVerticalFovDegrees'] + 0.05) / 2))
source_fov = math.degrees(2 * math.atan(source_half_tan))
fov_literal = format(source_fov, '.15g')
replacements = {
    'this.focal=nd(r.totalPanoramaHeight)': 'this.focal=nd(1440)',
    'this.focal=nd(this.view.totalPanoramaHeight,e)': 'this.focal=nd(1440,e)',
    'yd=Math.tan(60*Math.PI/360)': f'yd=Math.tan({fov_literal}*Math.PI/360)',
    'uTanHalfV:{value:Math.tan(30*Math.PI/180)}':
        f'uTanHalfV:{{value:Math.tan({fov_literal}*Math.PI/360)}}',
    'sectorCamera=new wo(60,1,zu,Bu)': f'sectorCamera=new wo({fov_literal},1,zu,Bu)',
    'new wo(60,this.sectorCamera.aspect,zu,Bu)':
        f'new wo({fov_literal},this.sectorCamera.aspect,zu,Bu)',
    't.fov=60,t.aspect=this.sectorCamera.aspect':
        f't.fov={fov_literal},t.aspect=this.sectorCamera.aspect',
}
for old, new in replacements.items():
    if content.count(old) != 1:
        raise SystemExit(f'R18 anchor is not unique: {old} ({content.count(old)})')
    content = content.replace(old, new)
bundle.write_text(content, encoding='utf-8')
manifest = {
    'scope': 'current physical three-monitor layout only',
    'source': str(source), 'sourceSha256': sha256(next((source/'assets').glob('index-*.js')).read_bytes()).hexdigest(),
    'bundle': str(bundle), 'bundleSha256': sha256(bundle.read_bytes()).hexdigest(),
    'reference': {'primaryHeight': primary_height, 'virtualHeight': full_height,
                  'focal': focal, 'sourceVerticalFovDeg': source_fov,
                  'sourceHalfTangent': source_half_tan,
                  'sectorWidth': math.ceil(2 * focal * math.tan(math.radians(35))),
                  'sectorHeight': math.ceil(2 * focal * source_half_tan)},
    'replacements': replacements,
}
(output.parent / 'manifest.json').write_text(json.dumps(manifest, indent=2), encoding='utf-8')
print(output.parent / 'manifest.json')
