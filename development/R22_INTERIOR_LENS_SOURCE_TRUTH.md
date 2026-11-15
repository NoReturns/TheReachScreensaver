# R22 interior lens source-truth audit

Date: 2026-10-28. Mode: isolated developer diagnostic. **Decision: GO for the bounded source-truth question. B1 remains NO-GO.** This does not grant production or owner visual acceptance.

## Finding

The bright secondary curved image in the ordinary left window is a lensed image of the fixture's spiral-galaxy sprite. The object is real in the scene, but the **cube fallback overstates its brightness and angular footprint** at the three audited rays. The R20 static +42.006-degree perspective sector still renders the galaxy; it reduces the exaggerated cube image. A fresh 8-degree perspective camera, rendered directly from the same scene at the existing sector angular pixel density, agrees with the sector sample within 1.9%, 2.5% and 6.1% mean RGB at yaw -12, -6 and 0. The cube sample is 5.1, 7.6 and 10.6 times the sector's mean RGB. These results establish which representation better preserves the **current full-density scene renderer** for these directions. They do not establish absolute astrophysical radiance or correctness at every lens direction.

The difference is in background source sampling, not the accepted geodesic or disc equations. At all three traced pixels, lens emission is exactly `(0,0,0)` and transmittance is `1`. The control left context selects the 512-pixel cube after its nearest sector projects outside UV bounds. Static left and primary select the same full-resolution perspective sector and return identical source and lens RGB. Forcing cube sampling in left and primary also returns identical RGB. The original and deflected rays match across contexts and configurations.

Confidence: source selection, matched RGB, layer identity and same-density reference agreement are **proven by code and readback** for the audited rays. The precise pixel-footprint mechanism for the cube's excess brightness is a **strong code-supported inference**. Absolute physical galaxy radiance remains **unresolved** because the sprite shader changes its sampled brightness with raster density.

## Evidence boundary and method

R21's native frames and [`R21_STATIC_SOURCE_VISUAL_GATE.md`](R21_STATIC_SOURCE_VISUAL_GATE.md) identified the moving feature. R20's [`R20_STATIC_SOURCE_PARITY_VALIDATION.md`](R20_STATIC_SOURCE_PARITY_VALIDATION.md) supplies the preserved full-resolution 2x control/static bundles and target-count baseline. R22 appended [`r22_browser.js`](r22_browser.js) only to copied developer bundles listed in [`r22-variants/manifest.json`](r22-variants/manifest.json). [`r22_run.ps1`](r22_run.ps1) ran the existing `hole-26-left` frozen fixture with native final output, native bloom, the 512-pixel guard, fixed scene state and shader time, and isolated profiles. No production source or accepted lens mathematics was edited.

The disposable probe re-renders a 96x160 native-pixel lens ROI into a half-float target. A diagnostic copy of the same lens shader records the incoming ray, deflected source ray, selected source RGB, disc emission, transmittance and raw lens RGB at one representative pixel. It retains the live source textures and has its readback checked against an unmodified copy and the original lens ROI. An initial material-clone diagnostic returned black because clone-created texture objects were not the live targets; those preliminary runs are excluded from the conclusions. The corrected probe shares the live texture uniforms, and its raw lens RGB agrees with the ordinary unmodified shader at the traced pixels. The diagnostic can force either cube or sector lookup without changing the geodesic, emission or final compositing code.

An independent source reference uses a **new** 8-degree perspective camera pointed at each deflected world direction and re-renders the underlying scene. A 324x324 target has approximately 2317 pixels per radian at its center, matching the 2316-pixel cylindrical focal and the 3244x2675 sector's source scale. It does not sample either existing source texture. Additional 648, 1024 and 2048 renders probe resolution sensitivity. The 324 reference's 33x33 half-float center patch is retained in the static run trace and shown in the source comparison images.

## Exact pixel traces

Coordinates are virtual-desktop pixels in the left monitor. RGB source and raw lens values are linear half-float; final RGB is encoded 8-bit native output. Full-precision rays, sector centers/UVs, forced-path values, reference samples and file paths are in [`r22-review/traces.json`](r22-review/traces.json). Each run's `metrics/forensics-*.json` retains its full original ray trace and lossless base64 half-float ROI; `metrics/fixture-*.json` names and hashes complete native PNGs.

| Yaw, virtual pixel | Original camera ray | Deflected ray | Control left selection | Static sector UV | Cube RGB | Sector RGB | Independent 324 RGB | Lens RGB control / static | Final RGB8 control / static |
| --- | --- | --- | --- | --- | --- | --- | --- | --- | --- |
| -12, (3081,1019) | (-0.8359, 0.0261, -0.5479) | (0.4641, -0.2123, -0.8594) | Cube; nearest sector UV x=1.026 | (0.3268, 0.3063) | (0.8794, 0.7974, 0.6699) | (0.1703, 0.1555, 0.1320) | (0.1741, 0.1586, 0.1339) | (0.8701, 0.7891, 0.6626) / (0.1685, 0.1538, 0.1305) | (229,228,226) / (186,187,190) |
| -6, (3322,1023) | (-0.7744, 0.0244, -0.6318) | (0.5449, -0.1974, -0.8145) | Cube; nearest sector UV x=1.138 | (0.3968, 0.3237) | (0.8311, 0.7549, 0.6357) | (0.1074, 0.0995, 0.0862) | (0.1104, 0.1021, 0.0881) | (0.8286, 0.7529, 0.6338) / (0.1071, 0.0993, 0.0859) | (228,227,225) / (177,179,184) |
| 0, (3566,1016) | (-0.7036, 0.0274, -0.7095) | (0.6221, -0.2217, -0.7505) | Cube; nearest sector UV x=1.283 | (0.4707, 0.3029) | (0.7808, 0.7109, 0.6006) | (0.0717, 0.0673, 0.0591) | (0.0762, 0.0714, 0.0625) | (0.7783, 0.7090, 0.5986) / (0.0715, 0.0671, 0.0589) | (227,225,223) / (168,170,178) |

The table rounds the half-float vectors; the machine-readable trace retains exact values. The source-identity, UV and RGB columns are direct readbacks. The control left's nominal sector UV x values exceed one, so its actual `lookupDirection` path is cube; the static +42.006 sector UVs are in range. Lens `background` color is dimmed slightly by the existing higher-order-image term, explaining why raw lens RGB is marginally below direct source RGB. At the audited pixels there is no disc emission or attenuation.

The matched left/primary forced-source checks are explicit in each `r22` record. At -6, for example, both contexts return cube `(0.83105,0.75488,0.63574)` and sector `(0.10742,0.09955,0.08618)` when those paths are selected; the forced lens values also agree. Control left's ordinary path is cube, static left's ordinary path is sector, and primary's ordinary path is sector in both variants. These samples use the same incoming and deflected ray at the virtual pixel. This rules out a lens-equation difference or context-specific content difference at these samples.

The cube sample is directional rather than a 2D sampler UV in the shader. [`traces.json`](r22-review/traces.json) records its world direction and the inferred conventional cube-face coordinate: face `-Z`, UV approximately `(0.07489,0.64264)`, `(0.08364,0.63106)` and `(0.08556,0.64769)` at yaw -12, -6 and 0. These UVs are derived from the traced direction and fixture camera quaternion using the OpenGL cube-face mapping; the shader directly passes the world direction to `textureCube`, so the UVs are not a separate GPU readback. The sector UVs in the table are the actual shader projection formula evaluated from the traced direction.

## Underlying source and reference

The -6 disposable layer-isolation run toggled scene children only within the fixture, re-rendered, and restored every visibility flag and source target before exit. Disabling all point layers makes both source samples black. Disabling point child 10 alone makes both black; disabling child 8 (catalogue stars), child 9 (nebula points), body meshes, or body groups leaves both samples unchanged. Child 10 is `GalaxySprites` with the cylindrical galaxy vertex shader. Its nearest source is galaxy index 0, a type-2 spiral with radius 1 pc and absolute magnitude -7, at an angular offset of about 0.45, 0.65 and 0.88 degrees from the three deflected rays. The [layer-isolation trace](r22-runs/run-1a7ed6e06ded4ab087f5b24361a203fc/metrics) contains the per-layer RGB and restored baseline.

The source-density explanation is supported by code and by the independent renders. [`CylindricalRenderer.ts`](../../Universe/src/reach/CylindricalRenderer.ts) creates 3244x2675 perspective targets and a 512-pixel cube face, rendering the same scene into both. [`GalaxySprites.ts`](../../Universe/src/render/GalaxySprites.ts) sets `uPxPerRad` and `uPixelRatio` for the point layer; [`galaxy-cylindrical.vert`](../../Universe/src/render/shaders/galaxy-cylindrical.vert) computes `gl_PointSize` in raster pixels. The cube's much coarser angular raster gives that point sprite a larger angular footprint. This explains the bright curved cube image and is supported by the independent 324-pixel reference agreeing with the sector. The reference at 648, 1024 and 2048 pixels becomes progressively dimmer at the exact ray because the sprite's pixel-sized footprint changes with raster density. The renderer therefore does not provide a resolution-invariant physical radiance reference. The 324 result is the relevant reference for the accepted full-density sector policy; cube overbrightness is a strong scene-renderer conclusion, not an astrophysical calibration claim.

## Native visual comparisons

Each final comparison uses the original native PNG pixels in a 96x160 crop, displayed at 3x nearest neighbor only for inspection. The red box marks the traced virtual pixel. Raw-lens images show the same native-area half-float lens outputs mapped identically into display RGB. The independent reference panel is a 33x33 **source direction** patch, not a simulated full lensed frame. Its source sampling and display mapping are labeled; the lossless half-float values remain in the run JSON.

| Yaw | Final native output | Raw lens and independent source reference |
| --- | --- | --- |
| -12 | [control versus static](r22-review/native-final-yaw--12.png) | [source diagnostic](r22-review/source-diagnostic-yaw--12.png) |
| -6 | [control versus static](r22-review/native-final-yaw--6.png) | [source diagnostic](r22-review/source-diagnostic-yaw--6.png) |
| 0 | [control versus static](r22-review/native-final-yaw-+0.png) | [source diagnostic](r22-review/source-diagnostic-yaw-+0.png) |

Exact final evidence directories (each contains native `captures/cylindrical-*.png`, `metrics/fixture-*.json` and `metrics/forensics-*.json`):

| Yaw | Control | Static with independent patch |
| --- | --- | --- |
| -12 | `C:\Storage\Screensaver\development\r22-runs\run-1fd4dc8ebf9d4810b3fab5294c83a76c` | `C:\Storage\Screensaver\development\r22-runs\run-723f52c451cc4f80b0b3719bb536c5ee` |
| -6 | `C:\Storage\Screensaver\development\r22-runs\run-daf4e212600348f78d58407d1bb4cdf9` | `C:\Storage\Screensaver\development\r22-runs\run-1a7ed6e06ded4ab087f5b24361a203fc` |
| 0 | `C:\Storage\Screensaver\development\r22-runs\run-5a7a36159d234d6cbee5a30e481894d6` | `C:\Storage\Screensaver\development\r22-runs\run-8f1c091bcab94820b3d1a4949d72bd52` |

The fixture state keys match for each control/static pair. [`r22_analyze.py`](r22_analyze.py) asserts ray/path parity and generates the condensed trace and review images. The final six selected runs report no WebGL error, no context loss and no fixture renderer error. Earlier probe-development runs remain preserved under `r22-runs` but are not used for the conclusion.

## Decision and next smallest action

**GO for source truth at these three poses:** the static perspective source is the faithful representation of the current full-density scene renderer, and the control's brighter secondary image is a cube-resolution point-sprite exaggeration. Static sampling does not remove the underlying galaxy, lens emission, or an actual disc feature at the audited rays. The positive-yaw correction and R21 visual acceptance gate remain separate; B1 stays NO-GO.

The smallest next action is owner review of the three native comparisons and the short R21 interior motion clips, with the specific question whether the reduced secondary galaxy image is visually acceptable. If a renderer change is later proposed, test a consistent source-density policy for all lens directions and layouts rather than treating this local result as universal cube-versus-sector parity. No further architecture or motion campaign was performed in R22.

Validation performed: reused R20/R21 evidence; checked the Universe lens, sector and galaxy-sprite code; traced exact matched rays and source paths at three frozen poses; checked raw-lens probe parity; isolated the contributing scene layer; re-rendered independent narrow-camera references at four densities; made and inspected native final and half-float diagnostic comparisons; verified selected runs' fixture state, GL errors and context-loss status. No production renderer/shader, accepted lens mathematics, permanent perspective target, installed screensaver, journey, monitor configuration, prior evidence, Git state or remote was changed.
