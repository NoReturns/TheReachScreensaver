// R22 disposable source-truth diagnostic. Appended to a copied developer bundle.
const r22OriginalForensics = window.__a1Forensics;
window.__a1Forensics = () => {
  const ordinary = r22OriginalForensics();
  if (Z !== 'left' && Z !== 'primary') return { ...ordinary, r22: null };
  const yaw = X.frozenYawDegrees;
  const positions = { '-12': [3081, 1019], '-6': [3322, 1023], '0': [3566, 1016] };
  const position = positions[String(yaw)];
  if (!position) throw Error('R22 unexpected yaw');
  const probeBox = { x: position[0] - 48, y: position[1] - 70, width: 96, height: 160 };
  const e = Q.cylindrical;
  const renderer = Q.renderer;
  const gl = renderer.getContext();
  const originalMaterial = e.screenScene.children[0].material;
  const originalTarget = renderer.getRenderTarget();
  const originalTone = renderer.toneMapping;
  const originalClear = renderer.autoClear;
  const cloneOriginal = e.lensMaterial.clone();
  const probe = e.lensMaterial.clone();
  cloneOriginal.uniforms = { ...e.lensMaterial.uniforms,
    uDisplayRect: { value: e.lensMaterial.uniforms.uDisplayRect.value.clone() } };
  probe.uniforms = { ...e.lensMaterial.uniforms,
    uDisplayRect: { value: e.lensMaterial.uniforms.uDisplayRect.value.clone() } };
  const oldSource = 'vec3 background(vec3 v) { return lookupDirection(v, uCount, true); }';
  const newSource = `vec3 background(vec3 v) {
    r22Dir = v;
    r22Sample = uR22ForceCube > 0.5 ? fallbackBackground(v, vec3(0.0)) :
      lookupDirection(v, uCount, true);
    return r22Sample;
  }`;
  let shader = probe.fragmentShader;
  if (!shader.includes(oldSource) || !shader.includes('gl_FragColor = vec4(col + trans * bg, 1.0);'))
    throw Error('R22 accepted lens anchors changed');
  shader = shader.replace('uniform float uBHEnabled;', `uniform float uBHEnabled;
uniform vec2 uR22ProbeUv; uniform float uR22Mode; uniform float uR22ForceCube;
vec3 r22Dir = vec3(0.0); vec3 r22Sample = vec3(0.0);`);
  shader = shader.replace(oldSource, newSource);
  shader = shader.replace('gl_FragColor = vec4(col + trans * bg, 1.0);', `
    vec3 r22Value = col + trans * bg;
    if (uR22Mode < 1.5) r22Value = d;
    else if (uR22Mode < 2.5) r22Value = r22Dir;
    else if (uR22Mode < 4.5) r22Value = r22Sample;
    else if (uR22Mode < 5.5) r22Value = col;
    else if (uR22Mode < 6.5) r22Value = vec3(trans);
    else if (uR22Mode < 7.5) r22Value = bg;
    gl_FragColor = vec4(r22Value, 1.0);`);
  probe.fragmentShader = shader;
  probe.uniforms.uR22ProbeUv = { value: e.lensMaterial.uniforms.uDesktop.value.clone() };
  probe.uniforms.uR22Mode = { value: 1 };
  probe.uniforms.uR22ForceCube = { value: 0 };
  const rect = e.lensMaterial.uniforms.uDisplayRect.value;
  probe.uniforms.uR22ProbeUv.value.set((position[0] + .5 - rect.x) / rect.z,
    1 - (position[1] + .5 - rect.y) / rect.w);
  probe.uniforms.uDisplayRect.value.set(probeBox.x, probeBox.y,
    probeBox.width, probeBox.height);
  const target = e.targets[0].clone();
  target.setSize(probeBox.width, probeBox.height);
  target.samples = 0;
  target.depthBuffer = false;
  const half = (v) => {
    const s = (v & 32768) ? -1 : 1, exp = (v >> 10) & 31, frac = v & 1023;
    return exp === 0 ? s * Math.pow(2, -14) * frac / 1024 :
      exp === 31 ? (frac ? NaN : s * Infinity) : s * Math.pow(2, exp - 15) * (1 + frac / 1024);
  };
  const renderProbe = (mode, forceCube) => {
    probe.uniforms.uR22Mode.value = mode;
    probe.uniforms.uR22ForceCube.value = forceCube ? 1 : 0;
    e.screenScene.children[0].material = probe;
    renderer.setRenderTarget(target);
    renderer.clear();
    renderer.render(e.screenScene, e.screenCamera);
    const bits = new Uint16Array(4);
    renderer.readRenderTargetPixels(target, 48, 89, 1, 1, bits);
    const error = gl.getError();
    if (error !== gl.NO_ERROR) throw Error('R22 probe GL error ' + error);
    return Array.from(bits.slice(0, 3), half);
  };
  const renderClone = () => {
    cloneOriginal.uniforms.uDisplayRect.value.set(probeBox.x, probeBox.y,
      probeBox.width, probeBox.height);
    e.screenScene.children[0].material = cloneOriginal;
    renderer.setRenderTarget(target);
    renderer.clear();
    renderer.render(e.screenScene, e.screenCamera);
    const bits = new Uint16Array(4);
    renderer.readRenderTargetPixels(target, 48, 89, 1, 1, bits);
    if (gl.getError() !== gl.NO_ERROR) throw Error('R22 clone GL error');
    return Array.from(bits.slice(0, 3), half);
  };
  const vecCtor = e.sectorCamera.position.constructor;
  const ref = (dir, size) => {
    const world = new vecCtor(dir[0], dir[1], dir[2]).applyQuaternion(Q.camera.quaternion).normalize();
    const camera = e.sectorCamera.clone();
    camera.fov = 8;
    camera.aspect = 1;
    camera.position.copy(Q.camera.position);
    camera.quaternion.setFromUnitVectors(new vecCtor(0, 0, -1), world);
    camera.updateProjectionMatrix();
    camera.updateMatrixWorld();
    const rt = e.targets[0].clone();
    rt.setSize(size, size);
    rt.samples = 0;
    rt.depthBuffer = true;
    renderer.setRenderTarget(rt);
    renderer.clear();
    renderer.render(e.scene, camera);
    const patchSize = 33;
    const bits = new Uint16Array(patchSize * patchSize * 4);
    renderer.readRenderTargetPixels(rt, size / 2 - 16, size / 2 - 16,
      patchSize, patchSize, bits);
    const error = gl.getError();
    rt.dispose();
    if (error !== gl.NO_ERROR) throw Error('R22 reference GL error ' + error);
    let peak = 0, sum = 0, active = 0;
    for (let i = 0; i < bits.length; i += 4) {
      const value = (half(bits[i]) + half(bits[i + 1]) + half(bits[i + 2])) / 3;
      peak = Math.max(peak, value);
      sum += value;
      if (value > .01) active++;
    }
    const bytes = new Uint8Array(bits.buffer);
    let encoded = '';
    if (size === 324) {
      for (let i = 0; i < bytes.length; i += 8192)
        encoded += String.fromCharCode(...bytes.subarray(i, i + 8192));
    }
    return { size, fovDeg: 8, patchSize, pixelFormat: 'RGBA16F bottom-up',
      patchBase64: size === 324 ? btoa(encoded) : null,
      centerRGB: Array.from(bits.slice(16 * 33 * 4 + 16 * 4,
      16 * 33 * 4 + 16 * 4 + 3), half), peakMeanRGB: peak,
      sumMeanRGB: sum, activePixelsOver001: active };
  };
  const nearestGalaxy = (dir) => {
    const object = e.scene.children[10];
    const attr = object.geometry.attributes.position;
    const cam = object.material.uniforms.uCamPc.value;
    const world = new vecCtor(dir[0], dir[1], dir[2]).applyQuaternion(Q.camera.quaternion).normalize();
    const candidates = [];
    for (let i = 0; i < attr.count; i++) {
      const direction = new vecCtor(attr.getX(i) - cam.x, attr.getY(i) - cam.y,
        attr.getZ(i) - cam.z).normalize();
      const angle = Math.acos(Math.max(-1, Math.min(1, direction.dot(world))));
      candidates.push({ index: i, angleDeg: angle * 180 / Math.PI,
        radiusPc: object.geometry.attributes.radius.getX(i),
        absMag: object.geometry.attributes.absMag.getX(i),
        type: object.geometry.attributes.gtype.getX(i) });
      candidates.sort((a, b) => a.angleDeg - b.angleDeg);
      if (candidates.length > 5) candidates.pop();
    }
    return candidates;
  };
  const lensRoi = () => {
    const box = probeBox;
    const oldRect = e.lensMaterial.uniforms.uDisplayRect.value.clone();
    const rt = e.targets[0].clone();
    rt.setSize(box.width, box.height);
    rt.samples = 0;
    rt.depthBuffer = false;
    try {
      e.screenScene.children[0].material = e.lensMaterial;
      e.lensMaterial.uniforms.uDisplayRect.value.set(box.x, box.y, box.width, box.height);
      renderer.setRenderTarget(rt);
      renderer.clear();
      renderer.render(e.screenScene, e.screenCamera);
      const bits = new Uint16Array(box.width * box.height * 4);
      renderer.readRenderTargetPixels(rt, 0, 0, box.width, box.height, bits);
      if (gl.getError() !== gl.NO_ERROR) throw Error('R22 lens ROI GL error');
      const bytes = new Uint8Array(bits.buffer);
      let encoded = '';
      for (let i = 0; i < bytes.length; i += 8192)
        encoded += String.fromCharCode(...bytes.subarray(i, i + 8192));
      return { box, pixelFormat: 'RGBA16F bottom-up', base64: btoa(encoded) };
    } finally {
      e.lensMaterial.uniforms.uDisplayRect.value.copy(oldRect);
      rt.dispose();
    }
  };
  try {
    renderer.toneMapping = 0;
    renderer.autoClear = true;
    const cloneRawRGB = renderClone();
    const originalRay = renderProbe(1, false);
    const deflectedRay = renderProbe(2, false);
    const cubeRGB = renderProbe(3, true);
    const sectorRGB = renderProbe(4, false);
    const emissionRGB = renderProbe(5, false);
    const transmittance = renderProbe(6, false)[0];
    const lensSectorRGB = renderProbe(8, false);
    const lensCubeRGB = renderProbe(8, true);
    const reference1024 = Z === 'left' ? ref(deflectedRay, 1024) : null;
    const reference2048 = Z === 'left' ? ref(deflectedRay, 2048) : null;
    const reference324 = Z === 'left' ? ref(deflectedRay, 324) : null;
    const reference648 = Z === 'left' ? ref(deflectedRay, 648) : null;
    const galaxyCandidates = Z === 'left' ? nearestGalaxy(deflectedRay) : null;
    const rawLensRoi = Z === 'left' ? lensRoi() : null;
    let contributors = null;
    if (Z === 'left' && yaw === -6 && e.targets.length === 4) {
      contributors = {};
      for (const [label, indices] of Object.entries({
        points: [8, 9, 10], point8: [8], point9: [9], point10: [10],
        meshes: [1, 3, 5, 7], groups: [0, 2, 4, 6]
      })) {
        const objects = indices.map(index => e.scene.children[index]);
        const oldVisible = objects.map(object => object.visible);
        try {
          objects.forEach(object => { object.visible = false; });
          e.screenScene.children[0].material = e.lensMaterial;
          Q.render();
          contributors[label] = { cubeRGB: renderProbe(3, true),
            sectorRGB: renderProbe(4, false) };
        } finally {
          objects.forEach((object, i) => { object.visible = oldVisible[i]; });
          e.screenScene.children[0].material = e.lensMaterial;
          Q.render();
        }
      }
      contributors.restored = { cubeRGB: renderProbe(3, true),
        sectorRGB: renderProbe(4, false) };
    }
    const theta = Math.atan2(deflectedRay[0], -deflectedRay[2]);
    const vertical = deflectedRay[1] / Math.hypot(deflectedRay[0], deflectedRay[2]);
    const centers = e.centers;
    let chosen = 0, best = Infinity;
    centers.forEach((center, i) => {
      const raw = theta - center;
      const distance = Math.abs(Math.atan2(Math.sin(raw), Math.cos(raw)));
      if (distance < best) { best = distance; chosen = i; }
    });
    const raw = theta - centers[chosen];
    const delta = Math.atan2(Math.sin(raw), Math.cos(raw));
    const tanH = e.lensMaterial.uniforms.uTanHalfH.value;
    const tanV = e.lensMaterial.uniforms.uTanHalfV.value;
    const uv = [.5 + Math.tan(delta) / (2 * tanH),
                .5 + vertical / (Math.cos(delta) * 2 * tanV)];
    const path = Math.abs(delta) > .6108652382 || uv.some(x => x < 0 || x > 1)
      ? 'cube' : 'sector';
    return { ...ordinary, r22: { yaw, display: Z, pixelVirtual: position,
      originalRay, deflectedRay, theta, vertical, centers, chosen,
      sourceIdentity: path === 'cube' ? 'cube' : `sector:${centers[chosen] * 180 / Math.PI}`,
      sourceProjection: path === 'cube' ? 'cube 512 face' : 'perspective 3244x2675',
      sectorDelta: delta, sectorUV: uv, cubeRGB, sectorRGB,
      emissionRGB, transmittance, lensSectorRGB, lensCubeRGB,
      cloneRawRGB,
      reference324, reference648, reference1024, reference2048,
      galaxyCandidates,
      rawLensRoi,
      contributors,
      probeUv: [probe.uniforms.uR22ProbeUv.value.x, probe.uniforms.uR22ProbeUv.value.y],
      targetCount: e.targets.length, webglError: gl.getError(), contextLost: gl.isContextLost(),
      sceneChildren: Z === 'left' ? e.scene.children.map((object, index) => ({ index,
        type: object.type, name: object.name, children: object.children.length,
        visible: object.visible, material: object.material?.type || null,
        attributes: Object.keys(object.geometry?.attributes || {}),
        shaderStart: object.material?.vertexShader?.slice(0, 90) || null })) : null } };
  } finally {
    e.screenScene.children[0].material = originalMaterial;
    renderer.setRenderTarget(originalTarget);
    renderer.toneMapping = originalTone;
    renderer.autoClear = originalClear;
    probe.dispose();
    cloneOriginal.dispose();
    target.dispose();
  }
};
