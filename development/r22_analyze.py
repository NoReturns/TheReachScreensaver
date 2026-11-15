"""Condense R22 trace JSON and render native final/source diagnostic comparisons."""
from pathlib import Path
import base64
import json
import math
import numpy as np
from PIL import Image, ImageDraw

root = Path(__file__).resolve().parent
out = root / 'r22-review'
out.mkdir(exist_ok=True)
runs = {
    -12: ('1fd4dc8ebf9d4810b3fab5294c83a76c', '723f52c451cc4f80b0b3719bb536c5ee'),
    -6: ('daf4e212600348f78d58407d1bb4cdf9', '1a7ed6e06ded4ab087f5b24361a203fc'),
    0: ('5a7a36159d234d6cbee5a30e481894d6', '8f1c091bcab94820b3d1a4949d72bd52'),
}

def load(run_id):
    folder = root / 'r22-runs' / ('run-' + run_id)
    forensic = json.loads(next((folder / 'metrics').glob('forensics-*.json')).read_text())
    fixture = json.loads(next((folder / 'metrics').glob('fixture-*.json')).read_text())
    traces = {r['r22']['display']: r['r22'] for r in forensic['records'] if r.get('r22')}
    frame = next(x['path'] for x in fixture['captures'] if x['display'].endswith('DISPLAY1'))
    return forensic, fixture, traces, frame

def raw_roi(trace):
    record = trace['rawLensRoi']
    b = record['box']
    data = np.frombuffer(base64.b64decode(record['base64']), dtype=np.float16)
    return data.reshape(b['height'], b['width'], 4)[::-1, :, :3].astype(np.float32)

def show_linear(data):
    v = np.maximum(data, 0)
    v = np.power(1 - np.exp(-v), 1 / 2.2)
    return Image.fromarray(np.uint8(np.clip(v * 255, 0, 255)), 'RGB')

def cube_coordinate(direction, quaternion):
    x,y,z = direction
    qx,qy,qz,qw = quaternion
    tx = 2*(qy*z-qz*y)
    ty = 2*(qz*x-qx*z)
    tz = 2*(qx*y-qy*x)
    wx = x + qw*tx + (qy*tz-qz*ty)
    wy = y + qw*ty + (qz*tx-qx*tz)
    wz = z + qw*tz + (qx*ty-qy*tx)
    if abs(wz) >= max(abs(wx), abs(wy)) and wz < 0:
        return [wx,wy,wz], '-Z', [(1-wx/abs(wz))/2,(1-wy/abs(wz))/2]
    raise ValueError('Audited ray did not land on expected cube -Z face')

summary = {}
for yaw, (control_id, static_id) in runs.items():
    cforensic, cfixture, ctraces, cpath = load(control_id)
    sforensic, sfixture, straces, spath = load(static_id)
    c = ctraces['left']
    s = straces['left']
    primary = straces['primary']
    assert cfixture['stateKey'] == sfixture['stateKey']
    assert c['pixelVirtual'] == s['pixelVirtual']
    assert c['originalRay'] == s['originalRay'] == primary['originalRay']
    assert c['deflectedRay'] == s['deflectedRay'] == primary['deflectedRay']
    assert c['cubeRGB'] == s['cubeRGB'] == primary['cubeRGB']
    assert s['sectorRGB'] == primary['sectorRGB']
    assert c['sourceIdentity'] == 'cube'
    assert s['sourceIdentity'].startswith('sector:')
    assert c['emissionRGB'] == s['emissionRGB'] == [0,0,0]
    assert c['transmittance'] == s['transmittance'] == 1
    state = json.loads(cfixture['stateKey'])
    cube_world, cube_face, cube_uv = cube_coordinate(c['deflectedRay'], state['cameraQuaternion'])
    box = c['rawLensRoi']['box']
    cx, cy = c['pixelVirtual']
    cp = Image.open(cpath).convert('RGB').crop((box['x'], box['y'], box['x']+box['width'], box['y']+box['height']))
    sp = Image.open(spath).convert('RGB').crop((box['x'], box['y'], box['x']+box['width'], box['y']+box['height']))
    canvas = Image.new('RGB', (576, 535), 'black')
    canvas.paste(cp.resize((288,480),Image.Resampling.NEAREST), (0,55))
    canvas.paste(sp.resize((288,480),Image.Resampling.NEAREST),(288,55))
    draw=ImageDraw.Draw(canvas)
    draw.text((5,4),f'CONTROL cube | yaw {yaw:+d} | R13 cylindrical',fill='white')
    draw.text((293,4),'STATIC +42.006 sector | full 2x',fill='white')
    draw.text((5,22),f'virtual pixel ({cx},{cy}); native 96x160 crop at 3x nearest',fill='white')
    draw.text((5,38),'final encoded output; native pixels preserved',fill='white')
    rx=3*(cx-box['x']);ry=55+3*(cy-box['y'])
    draw.rectangle((rx-4,ry-4,rx+4,ry+4),outline='red')
    draw.rectangle((rx+288-4,ry-4,rx+288+4,ry+4),outline='red')
    final_path = out / f'native-final-yaw-{yaw:+d}.png'
    canvas.save(final_path)
    rawc=raw_roi(c);raws=raw_roi(s)
    ref=s['reference324'];assert ref['patchSize']==33 and ref['patchBase64']
    ref_patch=np.frombuffer(base64.b64decode(ref['patchBase64']),dtype=np.float16).reshape(33,33,4)[::-1,:,:3].astype(np.float32)
    diagnostic=Image.new('RGB',(650,370),'black');dd=ImageDraw.Draw(diagnostic)
    diagnostic.paste(show_linear(rawc).resize((192,320),Image.Resampling.NEAREST),(0,35))
    diagnostic.paste(show_linear(raws).resize((192,320),Image.Resampling.NEAREST),(192,35))
    diagnostic.paste(show_linear(ref_patch).resize((198,198),Image.Resampling.NEAREST),(420,95))
    dd.text((5,3),f'RAW LENS cube | yaw {yaw:+d}',fill='white')
    dd.text((198,3),'RAW LENS sector',fill='white')
    dd.text((420,3),'Independent source camera',fill='white')
    dd.text((420,20),'8deg FOV, 324px, sector density',fill='white')
    dd.text((420,78),'33x33 source patch at 6x',fill='white')
    dd.text((5,357),'Linear RGB display mapping; original half-float data in run JSON.',fill='white')
    diagnostic_path=out/f'source-diagnostic-yaw-{yaw:+d}.png'
    diagnostic.save(diagnostic_path)
    crgb=cp.getpixel((cx-box['x'],cy-box['y']))
    srgb=sp.getpixel((cx-box['x'],cy-box['y']))
    summary[str(yaw)]={
        'controlRun':str(root/'r22-runs'/('run-'+control_id)),
        'staticRun':str(root/'r22-runs'/('run-'+static_id)),
        'pixelVirtual':[cx,cy],
        'stateKeyEqual':True,
        'originalRay':c['originalRay'],
        'deflectedRay':c['deflectedRay'],
        'cubeWorldDirection':cube_world,
        'cubeFace':cube_face,
        'cubeUVInferred':cube_uv,
        'controlSource':c['sourceIdentity'],
        'staticSource':s['sourceIdentity'],
        'sectorUV':s['sectorUV'],
        'cubeRGB':s['cubeRGB'],
        'sectorRGB':s['sectorRGB'],
        'reference324RGB':ref['centerRGB'],
        'reference648RGB':s['reference648']['centerRGB'],
        'reference1024RGB':s['reference1024']['centerRGB'],
        'reference2048RGB':s['reference2048']['centerRGB'],
        'galaxyCandidate':s['galaxyCandidates'][0],
        'emissionRGB':s['emissionRGB'],
        'transmittance':s['transmittance'],
        'lensControlRGB':c['cloneRawRGB'],
        'lensStaticRGB':s['cloneRawRGB'],
        'finalControlRGB8':crgb,
        'finalStaticRGB8':srgb,
        'rawLensControlRGB':rawc[cy-box['y'],cx-box['x']].tolist(),
        'rawLensStaticRGB':raws[cy-box['y'],cx-box['x']].tolist(),
        'nativeComparison':str(final_path),
        'sourceDiagnostic':str(diagnostic_path),
        'cubeToSectorMeanRatio':float(np.mean(s['cubeRGB'])/np.mean(s['sectorRGB'])),
        'reference324SectorMeanRelativeError':float(abs(np.mean(ref['centerRGB'])-np.mean(s['sectorRGB']))/np.mean(s['sectorRGB'])),
    }

(out/'traces.json').write_text(json.dumps(summary,indent=2),encoding='utf-8')
print({yaw:{'pixel':d['pixelVirtual'],'cubeToSector':round(d['cubeToSectorMeanRatio'],2),
            'refError':round(d['reference324SectorMeanRelativeError'],3),
            'final':(d['finalControlRGB8'],d['finalStaticRGB8'])} for yaw,d in summary.items()})
