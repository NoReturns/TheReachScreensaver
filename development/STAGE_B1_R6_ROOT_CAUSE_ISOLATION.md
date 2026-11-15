# Stage B1-R6 physical-join root-cause isolation

Recommendation: REVISE before an isolated repair pilot. The first measured
divergence in bounded frozen diagnostics at yaw +6 and +7 is the native
cylindrical lens pass, before bloom. The exact continuous yaw +6.815 frame
has only final-output capture, so this stage attribution is a near-pose
inference for that frame. The strongest cause hypothesis is incomplete
per-window sector-source coverage for deflected background rays. A small
developer-only branch/source diagnostic should confirm it before changing
render calculations. B1 rendered-frame acceptance remains NO-GO.

## Exact failing frame and colour interpretation

The retained native repeat-2 run is
`development/runs/run-8e997118d7f5434ab523480d66cd400c`.
Its capture manifest is
`metrics/b1-cylindrical-115fe3339d004a92be1eb17c91d3917c.json`.
Left `DISPLAY2` frame 88 and primary `DISPLAY1` frame 89 both report
callback epoch 1792969865334.5 ms, virtual trajectory time 1681.5 ms,
intended and actual yaw +6.815 degrees, and zero callback/yaw skew.
Their SHA-256 payload hashes are
`560F7F60BD334CEE5C24FB50444D8E6B3EE7046F25ECCE5F40FFBF1348C800FB`
and
`62083825F378348AE923E6063B7057096F4D56EE25CC2F160B0C0EE7F0408827`.
The R3 verifier passed both streams: ordered frames, exact payload sizes,
matching hashes, and no queue overflow or capture error. The fixture uses
the same version-1 scene state and shader clocks in both windows.

The R3 asynchronous readback follows the final render and checks the
default framebuffer and physical drawing-buffer size. Left local crop
x=3648..3839, y=800..1349 and primary local crop x=0..191,
y=80..629 map to adjacent virtual x=3839/3840 and the same virtual
y=800..1349. The raw RGBA8 data are bottom-up; R5 reversed row order
before comparison. At virtual y=1034, the encoded output luma samples
at x=3837..3842 were `199.1, 203.3, 203.3 | 156.0, 154.9, 153.9`.
The 47.3-unit join step is measured with Rec. 709 coefficients on
8-bit output values. It is a display-referred screening metric, not
linear-light luminance or a photometric measurement. The source render
targets below are half-float linear RGB, so their numbers cannot be
compared directly with the 47.3-byte step. [The R5 seam zoom](STAGE_B1_R5_SEAM_ZOOM.png)
and `STAGE_B1_R5_SEAM_METRICS_V4.json` retain the image and per-frame
context. The repeat-1 reverse and repeat-2 reverse frames near the same
yaw show 35.2- and 42.4-unit steps at this same virtual y.

## Bounded intermediate-stage diagnostics

Existing A2 developer-only frozen forensics accept integer yaw and read
the same virtual x=3838..3841, y=800..1219 from both contexts. No
production or development source was edited. Two new isolated runs used
the accepted native guard 512 and bloom mode `native`:

| Frozen yaw | Run ID | Fixture manifest | Forensic manifest | Verified adjacent hard rows |
| ---: | --- | --- | --- | ---: |
| +6 | `run-3acfd7c8146f40ddb6807a32fc4fe07f` | `metrics/fixture-hole-26-left-cylindrical-final-e89b55049ee6423198209f6c37c22706.json` | `metrics/forensics-e89b55049ee6423198209f6c37c22706.json` | 9 |
| +7 | `run-a8d1937be9414c3585ee90aa186728a9` | `metrics/fixture-hole-26-left-cylindrical-final-06a1722422c241e482c6d6b6c2e10733.json` | `metrics/forensics-06a1722422c241e482c6d6b6c2e10733.json` | 11 |

Each root also contains `metrics/forensic-analysis.json`,
`metrics/visual-analysis.json`, hashed physical monitor PNGs, an assembled
image, and a seam crop. The existing fixture and forensic analyzers
passed state, session, frame revision, physical layout, hashes, source
sector overlap, and stage dimensions. The frozen captures are diagnostic
poses, not synchronized video. Their +6/+7 final-output defect occupies
moving rows around the +6.815 event but is not the identical frame.

Peak absolute same-virtual-pixel RGB differences between left and primary
in the 4x420 half-float forensic strips were:

| Stage | Yaw +6 | Yaw +7 | Finding |
| --- | ---: | ---: | --- |
| Unlensed cylindrical warp | 0 | 0 | Direct scene/source projection agrees in the sampled overlap. |
| Cylindrical black-hole lens | 0.363037 | 0.204346 | First large cross-context divergence. |
| Bright threshold | 0 | 0.000549 | No matching large bright-source divergence at the join. |
| Horizontal mip 0 | 0.001259 | 0.001324 | Small local bloom difference. |
| Horizontal mip 4 | 0.001255 | 0.001420 | Separate known coarse-mip residual. |
| Bloom composite texture | 0.000488 | 0.000916 | Nearly equal at matched virtual pixels. |
| Encoded OutputPass target | 0.265137 | 0.123535 | Large lens difference survives output conversion. |

At yaw +7 and virtual x=3839, y=1024, both unlensed contexts returned
`(0,0,0)`. The lens returned approximately
`(0.14575,0.15698,0.18591)` on the left and `(0,0,0)` on the primary.
The separate bloom composite was approximately
`(0.27838,0.29742,0.35699)` versus
`(0.27809,0.29700,0.35669)`, while the encoded OutputPass target was
approximately `(0.73682,0.75049,0.78564)` versus
`(0.62646,0.64258,0.68701)`. At yaw +6, x=3839, y=979, the same
pattern is stronger: unlensed zero in both, lensed left
`(0.23108,0.28027,0.36304)` versus primary zero, bloom composite
within 0.00025 per channel, and encoded output left
`(0.72168,0.74951,0.79346)` versus primary
`(0.47070,0.48511,0.52832)`. These are same-coordinate comparisons,
not the R5 adjacent-pixel metric.

The forensic lens strip is rendered after the normal frame into a small
offscreen target using the same material and uniforms, with
`uDisplayRect` temporarily set to the same virtual four-column rectangle
in both contexts. This controls pixel-center mapping for the source/lens
comparison. The hook restores the material and render target afterward.
It does not independently record the lens shader's chosen source index,
fallback branch, cube colour, or deflected direction.

## Render-path audit and cause ranking

`src/core/Engine.ts` constructs one authoritative scene/camera per
developer window, fixes `ACESFilmicToneMapping`, exposure 1, sRGB output,
and bloom settings 0.35/0.4/0.92. The fixture fixes scene geometry,
material time 1234, black-hole time 0, seed and camera yaw; the B1 tick
updates camera quaternion and `BlackHolePass.target` before each render.
`BlackHolePass.target` derives the hole position, disk normal, ray region,
and projection uniforms from that pose. The local panorama FOV formula
algebraically yields the same pixels-per-radian across the unequal
display heights. Equal per-window inputs are supported by fixture state,
but individual shader uniforms were not logged at +6.815.

`CylindricalRenderer.renderFrame` first renders selected perspective
sector textures, then a fallback cube, then runs the cylindrical lens
as the `RenderPass` in a per-window half-float `EffectComposer`. The
`UnrealBloomPass` extracts bright regions, builds five horizontal/vertical
mips, and additively composites bloom onto the lens result. `OutputPass`
applies tone mapping/output conversion, then `cropMaterial` samples the
padded target to the physical default framebuffer. The +6/+7 forensic
order proves the large divergence exists before bright extraction and
before final crop or output conversion. Bloom has its own small residual
but is not the first or dominant cause of this failure.

`CylindricalGeometry.paddedDisplayRect` with guard 512 gives left
post rectangle x=0..4351, y=0..2159 and primary x=3328..6911,
y=208..2159. Both contain the join and 512 pixels of common source
region. `displaySectorCenters` chooses sector targets from these direct
padded display angles. The +6/+7 forensic manifests record left centres
-107.994, -57.994, -7.994 degrees and primary centres -57.994,
-7.994, +42.006 degrees. The direct ray at the join is in shared
coverage, consistent with the zero unlensed difference. The lens shader
can sample a deflected background direction through
`CylindricalLens.lookupDirection`; it chooses among each window's
selected `uSources`, and an invalid angle/UV falls back to
`uFallbackCube`. The selection contract covers the direct padded
rectangle, not necessarily the deflected ray envelope.

Ranked hypotheses for the lens-stage divergence:

1. High: the deflected background direction at these pixels needs a
   sector available on the left but omitted from the primary's selected
   set. The selected lists differ by the leftmost -107.994-degree
   sector, and left lens RGB is nonzero where primary lens RGB is zero.
   Source inspection proves this path can diverge; the exact branch and
   sampled sector were not recorded.
2. Medium: both contexts enter a fallback lookup, but cube background
   content or sampling differs. The cube is separately rendered in each
   WebView and was not read back at the failing ray direction.
3. Low: a context-specific lens uniform or numerical geodesic branch
   differs despite the matched pose and identical diagnostic pixel
   rectangle. Existing state checks do not log every uniform or branch.
4. Low for this major step: bloom source reach or mip-grid phase, already
   measured at -6/+4. The +6/+7 lens divergence precedes bloom, and
   the bloom-only composite is nearly equal at the sampled failing pixels.

The accepted guard was inspected, not changed. A 512-pixel direct
post rectangle does not guarantee lens source rays remain inside the
same sector subset. Increasing bloom guard or changing bloom parameters
without a traced source direction would be speculative. Neither is
recommended as an R6 experiment.

The older yaw -6 and +4 forensic runs sampled equal unlensed and lensed
RGB in both contexts. At -6 their first material local difference was
horizontal bloom mip 4 after missing distant bright-source coverage;
at +4 the first difference was tiny at horizontal mip 1, with the
largest still small at mip 4. Their large adjacent-pixel peaks follow
the black-hole silhouette/disc in both contexts. The +6/+7 lens-stage
failure is therefore a separate mechanism on present evidence, even
though all can appear near a moving black hole.

## Smallest next experiment and decision

The smallest diagnostic extension is developer-only exact-yaw stage
capture at +6.815, retaining the R4 final-output pair and its frame
metadata. In a bounded offscreen 4-column by roughly 160-row probe,
record the lens shader's deflected direction, chosen sector centre and
UV, invalid/fallback branch, cube sample, and RGB from each context.
Capture the unlensed and lensed values at the same absolute pixel
centres and verify the final physical pixels in the same run. Keep the
accepted renderer and 512-pixel path unchanged, store probe results
under a fresh disposable root, and reject the diagnostic if it changes
the ordinary final-output hashes at a matched frozen control pose.

If that probe confirms an omitted sector/fallback transition, the
smallest isolated corrective experiment is an offscreen developer-only
primary lens probe supplied with the missing sector texture, rendered
once at the failing pose. Compare same-coordinate lens and final-output
values before considering a production source-coverage design. A
repair pilot is REVISE until this branch/source trace confirms the
causal mechanism at the exact failing yaw. No speculative renderer fix,
guard increase, bloom retuning, B2/B3 work, installation, display
change, cleanup, commit, or push was performed in R6.

## Workspace status and validation

Preflight Screensaver `main` HEAD was
`4596b719f10fa39565f7cd34094b305d128825a2`; Universe
`reach-v1-release` HEAD was
`76ab9d49c3cb6ae5d6b03f07073387b8708a47d7`. Both dirty trees
were preserved. R6 added only this report and two disposable forensic
run roots under Screensaver `development/runs/`; no source file changed.
Both developer launches exited normally and both existing forensic
analyzers passed. Final read-only Git status matched preflight in both
repositories. No source build was needed because no code was edited.
