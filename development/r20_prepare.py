"""Build disposable R20 bundles from the preserved full-resolution fixtures."""
from hashlib import sha256
from pathlib import Path
from shutil import copytree
import json

base = Path(__file__).parent
out = base / 'r20-variants'
if out.exists():
    raise SystemExit(f'R20 variants already exist: {out}')

sources = {
    'control-2x': base / 'r15-variants' / '2x',
    'control-4x': base / 'r15-variants' / '4x',
    'static-2x': base / 'r15-variants' / '2x',
}
anchor = ('a=ad(e,{...this.view,viewportOffsetX:t.x,viewportOffsetY:t.y,'
          'viewportWidth:t.width,viewportHeight:t.height},this.focal,this.shift);'
          'if(a.length<1')
replacement = ('a=ad(e,{...this.view,viewportOffsetX:t.x,viewportOffsetY:t.y,'
               'viewportWidth:t.width,viewportHeight:t.height},this.focal,this.shift);'
               'if(this.view.totalPanoramaWidth===8320&&this.view.totalPanoramaHeight===2160'
               '&&this.view.viewportOffsetX===0&&this.view.viewportWidth===3840){'
               'let next=e[e.indexOf(a.at(-1))+1];'
               'if(next!==undefined&&!a.includes(next))a.push(next)}'
               'if(a.length<1')

rows = []
for name, source in sources.items():
    target = out / name
    copytree(source, target)
    bundle = next((target / 'assets').glob('index-*.js'))
    content = bundle.read_text(encoding='utf-8')
    if name == 'static-2x':
        if content.count(anchor) != 1:
            raise SystemExit('R20 static ensureSectors anchor is not unique')
        content = content.replace(anchor, replacement)
        # The legacy R9 toggle is still called by the R10 host. It must not
        # change allocations or source identities in the static candidate.
        hook = 'lh=X?.sectorParity&&Z===`left`?Q.cylindrical:null;'
        if content.count(hook) != 1:
            raise SystemExit('R9 hook anchor is not unique')
        content = content.replace(hook, 'lh=null;')
        start = content.index('window.__a1R9Toggle=e=>{')
        end = content.index(',window.__a1R9Measure=', start)
        content = (content[:start] +
                   'window.__a1R9Toggle=e=>{uh();Q.render();return{display:Z,'
                   'enabled:Z===`left`,centersRadians:[...Q.cylindrical.centers],'
                   'targets:Q.cylindrical.targets.map(t=>[t.width,t.height,t.samples]),'
                   'contextLost:Q.renderer.getContext().isContextLost()}}' +
                   content[end:])
    bundle.write_text(content, encoding='utf-8')
    # R17's paced continuous capture reads the same full-resolution source.
    motion = out / (name + '-motion')
    copytree(target, motion)
    motion_bundle = next((motion / 'assets').glob('index-*.js'))
    addon = (base / 'r17_browser.js').read_text(encoding='utf-8')
    # R20 must cover the full requested yaw interval continuously.
    addon = addon.replace('base+8*Math.sin(', 'base+12*Math.sin(')
    with motion_bundle.open('a', encoding='utf-8') as stream:
        stream.write('\n' + addon + '\n')
    for label, path in [(name, bundle), (name + '-motion', motion_bundle)]:
        rows.append({'variant': label, 'bundle': str(path),
                     'sha256': sha256(path.read_bytes()).hexdigest()})

out.mkdir(exist_ok=True)
(out / 'manifest.json').write_text(json.dumps(rows, indent=2), encoding='utf-8')
print(out / 'manifest.json')
