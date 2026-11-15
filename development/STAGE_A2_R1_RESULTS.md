# Stage A2-R1 native fixture result

Status: REVISE. Captures are diagnostic native WebView2 evidence, not owner
visual acceptance or Stage B approval.

## Contract and execution

The source fixture is `C:\Storage\Universe\development\cylindrical-stage-a1\native-fixtures.js`.
Both projection modes use the same static camera, Reach-material scene,
synthetic time, random seeds, object positions, 1216-pixel shift, and display
layout. The 240 and 26 black-hole values are distance in multiples of the
synthetic body radius 1. The host validates the exact serialized state across
all three windows before revealing or capturing. Each run has a separate
session and disposable root. A script verified 11 rectilinear/cylindrical
state-key pairs, 66 original captures, three ready displays per run, and no
fixture errors. Captures were hashed and checked against their manifests.

Windows reported virtual desktop origin (-3840,-720), size 8320x2160. The
normalized left, primary, and right rectangles were (0,0,3840,2160),
(3840,720,2560,1440), and (6400,1080,1920,1080). DPR was 1.5, 1, 1.
The cylindrical sector counts were 2, 3, 1, with 128 physical-pixel guards.

Each run below is under `C:\Storage\Screensaver\development\runs\` and
contains its manifest, three original PNGs, assembled desktop, and seam crops.
The seam metric is mean 8-bit luminance impulse at x=3840 over absolute
y=800..1499; the next number is the count of rows above 20.

| Fixture | Rectilinear run / impulse / rows | Cylindrical run / impulse / rows |
| --- | --- | --- |
| sphere-left | run-d1c9de972a5d4d4c9469ad4352453686 / 1.514 / 8 | run-dce762397f96426d8b4e206ba52bc5f8 / 0.802 / 0 |
| sphere-seam | run-8724625b1ad4497399d43871a443466e / 43.114 / 498 | run-114bc2f557244a609ece88e1b2aa7ffc / 0.290 / 0 |
| earth-seam | run-d974ce88d6c64b0c997fb1730dbae73b / 1.514 / 8 | run-6af5001ec3ee4e7e9424b8962929185c / 0.654 / 0 |
| saturn-sector | run-d31518ecb4b0432b94557b81ff695464 / 1.514 / 8 | run-24eb1d3c7a474f90be76e72a40098998 / 0.654 / 0 |
| galaxy-sector | run-e2381c36f24c4588bae8b83ce00071e9 / 0 / 0 | run-b0d22191f1bc4e5b9f622fd86b5e662d / 0 / 0 |
| hole-240-left | run-891f132ffb7e4d66b9e74362b8c82fc5 / 109.291 / 525 | run-68958a8b5ab644e79a013bb43806bfee / 0.700 / 0 |
| hole-26-left | run-67a83cb886b64a7ca3d924842e17ebb7 / 0.380 / 3 | run-c8b1416405964f2fb0b036249fef4101 / 12.168 / 225 |
| hole-26-away | run-2000a2b089574ceebd16ee3189f79f87 / 0.209 / 0 | run-f0176eb4b82341e6a5570a908d72cbbd / 0.670 / 5 |
| hole-primary-right | run-99241c20f7ea4761afee1f1db53cc3b9 / 69.553 / 286 | run-8ca2a53c531e44a89b7ad08c52060267 / 0.646 / 0 |
| roll-15 | run-75974c32d2f4457daf7fa2e40d6e20e0 / 0.362 / 4 | run-b136fb2503f9435fa6469be13f7d4d47 / 11.533 / 223 |
| rearward | run-553366cf34554fd8af14c22a94b4e724 / 0 / 0 | run-b806cfdac7034b0c98ae8886928aae18 / 0 / 0 |

At x=6400, the cylindrical primary/right black-hole case had mean impulse
0.188 and one row above 20 over absolute y=1080..1599. The left/primary
26-radii case is a stable hard boundary in the final image, while the away
case is largely continuous. The 240-radii cylindrical image is continuous
through the measured seam and retains the disc, shadow, Earth lensing, and
bloom. Rectilinear sphere-seam, 240-radii, and primary/right images show
large joins. Their cause and baseline acceptability remain unresolved.

The reference sphere is visibly round in the cylindrical image and stretched
at the outer angle in rectilinear. Earth and its atmosphere, Saturn and its
rings, the spiral galaxy, stars, and the black-hole disc/shadow appear in the
native captures. These image inspections are qualitative. Automated metrics
cover physical monitor seams, not internal sector boundaries or silhouette
geometry. The rearward capture is mostly dark, so it proves initialization
and coverage without proving rich rearward scene content.

## 26-radii pass isolation

All pass variants used identical geometry state, then toggled the developer
diagnostic pass setting. Half-float left/primary readback was taken at equal
absolute y coordinates, x=3839/3840, before readback-to-display conversion.
The complete comparison is in
`run-24e0aa8145174ae9853d100f29748b71/metrics/pass-comparison.json`.

| Cylindrical pass | Mean final 8-bit impulse | Rows above 20 | Mean half-float impulse |
| --- | ---: | ---: | ---: |
| Raw lens, no bloom or OutputPass | 0.068 | 0 | 0.00004 |
| OutputPass, no bloom | 0.089 | 0 | 0.00005 |
| Bloom, no OutputPass | 3.679 | 0 | 0.01719 |
| Bloom plus OutputPass | 12.168 | 225 | 0.05885 |

At y=1200, the raw half-float RGB edge was about
(0.041,0.041,0.045)/(0.040,0.039,0.043). With bloom but without
OutputPass it was (0.058,0.059,0.065)/(0.040,0.039,0.043). With both
passes it was (0.222,0.223,0.242)/(0.159,0.158,0.170). At y=1050, bloom
lit the left edge while the primary edge stayed black. This identifies bloom
as the introduced discontinuity and OutputPass as an amplifier. A specific
sampling or guard mechanism is not yet demonstrated.

The final 26-radii pair captured frames 1, 2, and 3 with unchanged state key.
All rectilinear repeat images were pixel identical. Across cylindrical repeat
images, only 23 total pixels differed from frame 1 and each difference was
at most one 8-bit level. The seam impulse remained 12.168.

## Focused validation and limits

`npm run typecheck`, JavaScript syntax checks, developer native build,
local release build, Python syntax checks, and `git diff --check` passed.
A locally built ordinary release executable rejected `--reach-dev-fixture`
with exit code 1 before normal startup. Native fixture runs exited 0.
The existing A2 group readiness/fallback lane was retained; no new injected
fallback run was needed for the static capture contract.

The developer entry point bypasses `ReachWebApplication.Run`, production
journey store construction, reset processing, settings, and installed payload
resolution. Its run-root checks reject reparse points and reused roots before
profile/log creation. WebView2 profile, logs, temp files, metrics, and captures
are scoped to the run root. This is source and observed-path evidence; no
external filesystem access monitor was used. No installed executable, monitor
configuration API, installer, or Windows screensaver selector was invoked.
No before/after owner-data hash comparison was performed.

The outstanding native 26-radii bloom seam blocks completion of A2 visual
acceptance. The rectilinear joins also need baseline classification. A moving
camera sequence and owner visual acceptance remain open. No Stage B work was
started.
