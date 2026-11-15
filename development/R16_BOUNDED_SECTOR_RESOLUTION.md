# R16 bounded sector resolution pilot

Date: 2026-10-28. Mode: isolated developer feasibility. Production rendering, installed screensaver and owner journey were not changed. B1 remains NO-GO.

## Decision

**REVISE.** Sector raster dimensions can be bounded without changing the cylindrical screen-ray mapping, sector directions or accepted lens equations. The current three-monitor 2x-MSAA fixture fell from about **4418 MiB** process-attributed dedicated GPU memory at 3244x2675 sectors to **3222 MiB** at 2433x2007, a measured **1195 MiB saving**. A 1622x1338 cap saved about **2020 MiB** but visibly enlarged/softened small stars and changed more planet detail. The 2433x2007 cap is the lowest tested resolution worth further visual validation; **no minimum acceptable resolution is established**. Black-hole stills and short motion crops remain close, but planet/star sampling and sustained motion quality are unresolved. No cap is approved for production.

A fixed sector texture cap by itself is not a general fourth-monitor solution. Under the existing fourth-above geometry, the 2433 cap would provide only about 0.375 source texels per physical output pixel near a sector center, versus 0.75 in today's layout. A quality floor would need larger targets or would need to reject that layout. This pilot did not add a fourth window.

## Phase 1: geometry and resource dependency

Source ownership: `Universe/src/reach/CylindricalGeometry.ts` defines `cylinderFocal`, sector centers, selection and padded display geometry. `Universe/src/reach/CylindricalRenderer.ts` allocates sectors, sets the sector camera projection, renders sources, and passes focal/sector angles to the warp and lens shaders. `Universe/src/reach/CylindricalLens.ts` uses the cylindrical ray and source lookup while retaining the accepted black-hole geodesic/disc mathematics. `Universe/src/core/Engine.ts` sizes the physical output and postprocessing. The R16 bundle patch did not edit those source files.

- Virtual desktop bounding height `H` fixes cylindrical focal length `f = H/(2*tan(25 degrees))` for the accepted 50-degree vertical field. This determines the angular screen ray at each physical pixel and the horizontal/vertical positions of objects. Changing `f` would change the accepted visual projection and was outside scope.
- Sector centers and selected sector identities depend on desktop width, `f`, composition shift and display rectangles. They do not depend on sector texture pixel dimensions. The same 3+3+2 identities were retained across all R16 variants.
- The current implementation computes sector target width `ceil(2*f*tan(35 degrees)*outputScale)` and height `ceil(2*f*tan(30 degrees)*outputScale)`. This makes target dimensions proportional to virtual height. The proportionality is a sampling-density choice, not a mathematical requirement of the cylindrical rays.
- The source camera keeps a 60-degree vertical field and a horizontal aspect equal to the unrestricted width/height ratio. Its pose and sector yaw are unchanged. The bounded developer bundles compute the camera aspect from the unrestricted dimensions, independent of the capped raster dimensions, so target resizing does not intentionally change angular coverage. Integer target rounding yields less than 0.1% raster-aspect mismatch.
- The warp shader's source lookup is `u = 0.5 + tan(delta)/(2*tan(35 degrees))` and `v = 0.5 + vertical/(cos(delta)*2*tan(30 degrees))`. The lens shader uses the same sector source directions. These UV/ray equations do not contain target width or height. Lower raster resolution changes interpolation, edge sampling and texture detail, not the ray assigned to a final physical pixel.
- The final output canvas and local padded bloom/crop targets remain sized to the physical monitor and 512-pixel guard. They do not shrink with source sectors. Source pixel density varies across the angular sector; near its center, the tested caps provide approximately 1.0, 0.75 and 0.5 sector texels per physical output pixel in the current layout.

The [variant manifest](r16-variants/manifest.json) records SHA-256 hashes. All variants use the R15 2x sector-MSAA developer bundle. Only the two sector-size expressions and the sector-camera-aspect expression differ in each capped copy. Reversing those expressions makes each capped bundle byte-identical to the unbounded control. No accepted shader, lens equation, angular field, sector selection or production file changed.

### Layout and policy alternatives

| Layout/policy | Sector instances and size | Approximate sector pixels | Consequence |
| --- | --- | ---: | --- |
| Current 8320x2160, unrestricted | 8 at 3244x2675 | 69.42 M | About 1 source texel per center output pixel |
| Current, fixed 2433 width cap | 8 at 2433x2007 | 39.06 M | About 0.75 center density; tested |
| Current, fixed 1622 width cap | 8 at 1622x1338 | 17.36 M | About 0.5 center density; tested |
| Fourth-above 8320x4320, unrestricted | 7 at 6487x5349 | 242.89 M | Virtual-height scaling remains severe |
| Fourth-above, fixed 2433 cap | 7 at 2433x2007 | 34.18 M | About 0.375 center density; quality untested and likely at risk |
| Fourth-above, fixed 1622 cap | 7 at 1622x1338 | 15.19 M | About 0.25 center density; quality untested and high risk |
| Fourth-above, quality floor of 0.75 center texel/output pixel | 7 at about 4866x4012 | 136.66 M | Geometry model only; doubles sector pixels versus current unrestricted |

Option A is the current unrestricted formula: it preserves present sampling but grows sector area roughly with the square of virtual height. Option B is a fixed maximum dimension: it bounds each source target, but quality falls as virtual height grows. Option C is a source-density floor plus a maximum texture/memory limit: it can preserve a specified sampling floor while layouts fit, and must reject or separately revise layouts that exceed the limit. The 0.75 floor is an illustrative model based on the largest R16 cap, not an accepted quality threshold. No arbitrary-layout quality guarantee follows from it. The fourth-above model uses the architecture audit's assumed monitor directly above the left display; it is not an observed fourth screen.

Remaining height dependencies under either cap are focal length, physical-pixel ray angles, global sector directions/selection, sector UV sampling density and tile-camera LOD. Padded local post targets can also change when a display moves from a virtual boundary to an interior position. Cube size stays fixed, while each added WebView2 context and its browser/scene resources remain separate. A sector cap therefore bounds one allocation class, not the whole renderer.

## Phase 2: matched final frames and motion

Each variant used a fresh isolated profile and the same deterministic synthetic fixture states: black hole at yaw 0, -6 and +6.815 degrees; the +23-degree `galaxy-sector` peripheral scene; and the -78-degree `galaxy-left-seam` deep-space scene. All 15 launches captured complete final PNGs from the three physical monitors, with native bloom, 512-pixel guard, fixed camera/scene state per case, unchanged physical resolution, exposure and tone mapping. The captured state-key SHA-256 matches across the three variants for each case. [summary.json](r16-runs/summary.json) gives every run root, target status, draw count, join metric and image difference; the fixture manifests under each run root reference original PNG paths and hashes.

The [final crop montage](r16-runs/R16_CROP_MONTAGE.png) shows the black-hole left-primary join, peripheral planet and deep-space stars at native crop resolution. The [4x nearest-neighbor detail zoom](r16-runs/R16_DETAIL_ZOOM.png) exposes the planet limb and one small star without inventing pixels. At 2433, the black-hole silhouette, disc rim and caustic shape remained visually close in the sampled stills; some planet-edge texture and star core/bloom pixels changed. At 1622, the star visibly broadens and brightens and the planet limb/detail is softer. The differences are larger than R15's 2x-versus-4x MSAA changes. Native bloom masks some high-frequency source loss, so a pleasing still does not prove motion quality.

The table counts active pixels as those with any 4x-control RGB channel above 8. It reports the share of those pixels whose maximum encoded RGB-channel difference from the unbounded 2x control exceeds 8 levels. These are image changes, not a perceptual acceptability score.

| Scene | 2433 cap: active pixels changed | 1622 cap: active pixels changed |
| --- | ---: | ---: |
| Hole yaw 0 | 1.02% | 1.45% |
| Hole yaw -6 | 1.03% | 1.53% |
| Hole yaw +6.815 | 1.01% | 1.43% |
| Peripheral planet | 13.46% | 18.52% |
| Deep-space stars | 16.02% | 21.81% |

The left-primary encoded-luma adjacent-pixel mean/rows above 20 stayed near the existing order of magnitude: yaw 0 about 1.0/0; yaw -6 about 3.4-3.5/60; yaw +6.815 ranged 2.59-2.92/52-60. The lower value at 1622 is source softening, not proof that the known R8/R9 representation mismatch is fixed. Primary-right had zero rows above 20 in these frozen states. Black-hole caustic and sector/cube defects from R8-R11 remain outside this resolution pilot.

Three more isolated asynchronous four-second yaw sweeps retained left and primary seam crops at each callback: [unbounded](r16-runs/run-3f009985ef1742d48529677c0a657954), [2433 cap](r16-runs/run-b5b768f960ff4998bd7d87b4605df7db), and [1622 cap](r16-runs/run-0d15c91811334b74b889fe53b2d9ad3f). Each completed with zero reported readback errors or overflow, approximately 228-259 callbacks per display. The [motion crop montage](r16-runs/R16_MOTION_CROP_MONTAGE.png) compares sampled -6, 0 and +6.8-degree moments; [frame references](r16-runs/motion-review-frames.json) give exact times and yaw. No gross new silhouette jump was apparent at those moments. Four seconds of seam crops cannot establish flicker or shimmer during sustained travel, and the moving planet/starfield was not captured. Owner visual acceptance remains outstanding.

## Phase 3: direct resources and pacing

Three additional three-monitor moving black-hole runs used 10 seconds of warmup and 30 seconds of requestAnimationFrame rendering at 2x sector MSAA. The camera followed the same deterministic 20-second integer-degree yaw sweep. Windows `GPU Process Memory(*)\Dedicated Usage` was summed for the isolated host/WebView2 PID set; NVIDIA memory and utilization are adapter-wide. There were 13 counter samples within each measured window. These are direct counters but not a target-by-target allocation trace or proof of continuous residency. Raw `counter-samples.jsonl`, host snapshots, frame arrays and logs are in [unbounded run](r16-runs/run-17e29854201d4622918e8139439f8cec), [2433 run](r16-runs/run-3eb9274cad5c41fcb04f1a86709d8973), and [1622 run](r16-runs/run-588246bf267f4e9882f45b9a1abc52a6).

| 2x sector target | Process dedicated MiB, median (range) | Adapter used MiB | Adapter GPU % | Primary/left/right callbacks in 30 s | Median callback interval ms |
| --- | ---: | ---: | ---: | --- | ---: |
| 3244x2675 unbounded | 4418 (4171-4418) | 6167 | 51 | 1800 / 1800 / 1800 | 16.7 |
| 2433x2007 cap | 3222 (3184-3238) | 4975 | 40 | 1801 / 1800 / 1800 | 16.7 |
| 1622x1338 cap | 2398 (2358-2447) | 4141 | 39 | 2322 / 1800 / 1800 | 16.6-16.7 |

The measured PID-attributed savings relative to unbounded are **1195 MiB** at 2433 and **2020 MiB** at 1622. Adapter-wide median changes were about 1192 and 2026 MiB. R15's comparable unbounded 2x reading was 4409 MiB, close to this pilot's 4418 MiB. The 1622 primary callback count rose in this one short run, but mixed display/browser scheduling prevents treating that as an isolated GPU-speed result. Left/right remained near 60 callbacks per second. The 1622 p95/p99 intervals reached about 22.3 ms on left/right, whereas unbounded and 2433 p95/p99 were about 16.8 ms; this does not establish a sustained pacing regression from one short sample. No 30-second run reported WebGL errors or context loss. Browser callback intervals are not isolated GPU frame time or scanout timing.

R13 draw instrumentation in each captured case reported 412 total draws for black-hole final frames and 160 for the two non-lens scenes across all three resolution variants. Target counts stayed 3+3+2 and 2x MSAA. Reducing texture dimensions lowered allocation and raster demand; it did not reduce scene pass count.

Source-derived sector color/depth plus 2x multisample storage is about 36 bytes per sector pixel, excluding browser/driver backing. That yields approximately 2383 MiB at current unbounded size, 1341 MiB at 2433, and 596 MiB at 1622 for the eight sectors. The model predicts savings of 1042 and 1787 MiB, below the observed process-counter changes of 1195 and 2020 MiB. The gap may include resolve textures, driver allocation granularity or other target-related resources; the experiment cannot apportion it. Bloom, cube, scene assets and WebView2 contexts remain. Even at 1622, measured cylindrical process dedicated use was about 884 MiB above R14's rectilinear black-hole median of 1514 MiB; this cross-run remainder is approximate and not solely attributable to sectors.

For the hypothetical fourth-above geometry, the same source-derived 36-byte rule gives about 8339 MiB for seven unrestricted 6487x5349 sectors, versus 1174 MiB at the 2433 cap or 522 MiB at 1622. A 0.75 center-density floor would require about 4866x4012 and 4692 MiB of nominal 2x sector storage. These figures exclude postprocessing, fourth-context surfaces, assets, driver overhead and memory pressure. They are **geometry/resource models only**, not measured fourth-window memory, and none is a safe upper bound. A fixed cap can keep target allocation predictable only by accepting falling source density or rejecting layouts beyond a quality floor.

## Remaining risk and next smallest validation

The minimum accepted source resolution remains unknown. The 2433x2007 current-layout cap is a candidate because black-hole geometry stayed close and it saved about 1.2 GiB, but planet and star samples changed substantially and full-motion perception is untested. The 1622x1338 cap is **NO-GO as a visual candidate in this fixture** because small stars visibly broaden and the larger pixel differences signal a material sampling loss. The overall R16 decision is REVISE, since mathematical decoupling is valid but quality and layout policy need work.

The smallest next validation is an isolated, longer matched motion capture of the peripheral planet and starfield at unbounded and 2433, with owner full-resolution review and a predetermined quality criterion. Then model a quality-floor rule for the fourth-above geometry and reject it if source density, memory or maximum texture size cannot fit. A GPU allocation/residency trace would refine the measured saving but is not needed to see the visual risk. Do not change focal length, lens equations, sector identities or physical output to force a cap to pass.

Validation performed: read-only source and architecture trace; byte-normalized variant comparison; 15 successful three-monitor final-frame fixture launches with state-key, PNG hash, target-size/sample and draw checks; three warmed 30-second GPU-counter runs; three four-second asynchronous motion-crop runs; full-layout pixel and join analysis; visual inspection of final, detail and motion montages; Python/PowerShell script execution; and `git diff --check`. No full suite, Windows display change, installation, owner-data access, cleanup, deletion, commit, push or production promotion occurred. New files are this report, `development/r16_prepare.py`, `development/r16_capture.ps1`, `development/r16_measure.ps1`, `development/r16_motion.ps1`, `development/r16_analyze.py`, `development/r16_motion_review.py`, `development/r16_detail.py`, and new variants/evidence under `development/r16-variants/` and `development/r16-runs/`. R8-R15 evidence and both existing dirty worktrees were preserved.
