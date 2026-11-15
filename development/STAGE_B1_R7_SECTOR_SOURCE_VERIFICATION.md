# Stage B1-R7 exact-yaw lens source verification

Recommendation: NO-GO for a repair that merely adds the -107.994-degree
sector to the primary source list. B1 rendered-frame acceptance remains
NO-GO. At the exact +6.815-degree frozen pose, that added source was
rendered with colour and depth, but the primary lens selected its existing
+42.006-degree sector for all 400 probed pixels. Primary A and B lens
outputs were byte-identical. The left lens instead took the cube fallback
for those same rays. This identifies a source-selection/fallback mismatch,
but the proposed missing leftmost sector is not its cause in this region.

## Preflight and reproduction

Screensaver `main` HEAD was
`4596b719f10fa39565f7cd34094b305d128825a2`; Universe
`reach-v1-release` HEAD was
`76ab9d49c3cb6ae5d6b03f07073387b8708a47d7`. Both repositories
already contained tracked and untracked development work, which was
preserved. R5 and R6 evidence were read before edits. The accepted shader,
renderer, bloom code, and 512-pixel guard were not changed.

The successful isolated run is
`development/runs/run-2d337d51a14948cc80fcd9f5a5c9059e`.
Its primary evidence is
`metrics/source-probe-d165c6ba525e420ba2dc0a8732dad8d1.json`.
The same root retains the fixture manifest, forensic manifest, PNG captures,
logs, and the existing analyzer outputs. The fixture reports yaw 6.815,
camera quaternion `[0,0.05943704235540271,0,0.9982320561853552]`,
composition shift 1216, material shader time 1234, black-hole time 0,
the `hole-26-left` 26-radii scene, native bloom, guard 512, and the
recorded 8320x2160 virtual layout. Its state key is identical between
windows. The R4 continuous failing pair reported the same yaw and fixture
clock; this is a deterministic frozen reconstruction, not the historical
frame itself.

The frozen physical output reproduces the R5 event: at virtual y=1034,
left x=3839 has encoded Rec. 709 luma 203.285 and primary x=3840 has
156.016, a 47.269-unit adjacent-pixel step. R5 measured 47.3 at the
continuous failing frame. These encoded luma values are separate from
the linear half-float lens comparisons below.

## Probe and integrity

`Application/ReachDeveloperApplication.cs` now accepts fractional frozen
yaw for the existing developer fixture and gates `--reach-dev-source-probe 1`
to yaw 6.815, native guard 512, and forensics. It sends the result through
a bounded WebView2 polling slot. `Program.cs` rejects the new flag in an
ordinary build. The developer-only
`../Universe/development/cylindrical-stage-a1/view.js` renders a 4x100
offscreen lens rectangle at absolute virtual x=3838..3841 and
y=970..1069 in both windows. It retains linear RGBA and source-trace
values for every pixel. The primary runs A with its original three sources
and B with a newly rendered fourth source at -107.993582 degrees. Camera,
scene, pixel centres, source target size/format, lens material, and all
non-source uniforms are held constant. A trace-only clone of the accepted
lens shader records the deflected direction, selected index and centre,
UV, fallback branch, and cube sample. The production shader file and
ordinary final-output path are untouched.

The added sector target was 3244x2675 RGBA16F with depth and four samples,
matching the existing sector target. The scene render issued 14 draw calls.
Its centre 64x64 readback had nonzero alpha, and a 4x4 grid of 128x128
readbacks found 27 pixels with nonzero RGB and nonzero alpha in all
262,144 sampled pixels. The grid hash exactly matched the corresponding
existing left-window -107.994-degree source grid:
`1785E6B6AD2084070558E8C8550C52E9A59A1AD77A0359D7E064C595CEB3F8D8`.
This confirms that B bound a rendered source, not an uninitialized target;
the grid does not assert colour at every unsampled texel. The temporary
target's calculated colour-plus-depth footprint was 104,132,400 bytes
(99.31 MiB). The probe reported 106,232,752 bytes of temporary target
and validation-array allocation on primary, excluding driver overhead and
MSAA storage. Its `finally` path calls `dispose()` on the added target,
trace material, and strip target. No GPU residency measurement was made.

For each window, the default-framebuffer hash before and after the
offscreen probe was identical. Primary repeated-render control also
matched. The left intermediate repeat differed, but its before/after
hash matched exactly; that isolated repeated render was not used as the
probe comparison. The original final output was not left modified.
Neither WebGL context reported loss. Fixture and forensic analyzers passed.

## Numerical and branch findings

The A and B 4x100 half-float RGB payload hashes both equal
`09757D02C6E820D8CC5A02683F26823681A9E06402566A9372DFC50F1E78C212`.
Their maximum per-channel delta is 0 across all 400 pixels. Each differs
from the left same-coordinate strip, whose hash is
`F6F3CEC263CA74D4D1805C3D1FCC9CB9DECEFC66749A2F7B36311CE872457B2E`.
The A/left and B/left mean of each pixel's maximum RGB-channel difference
is 0.180849; the peak is 0.720398. These same-coordinate comparisons
test lens inputs, not adjacent physical-pixel continuity.

The deflected-direction trace hash is identical for A, B, and left.
All 400 A and B pixels use a valid sector lookup, branch 0. Adding the
source changes the index from 2 to 3 but not its +42.006-degree centre,
UV, branch, or output. All 400 left pixels use fallback branch 1 because
their nearest -7.994-degree sector exceeds the angular validity limit.
Their cube sample equals the primary's cube sample at every probed pixel
with maximum channel delta 0. This means the contexts agree on the
deflected ray and fallback cube sample, but one context samples a local
sector while the other falls back to the cube.

At x=3839, y=1034, the deflected direction is approximately
`(0.728516,-0.160034,-0.665527)`. Primary A and B select the
+42.006-degree sector at UV `(0.569824,0.358887)`, branch 0, yielding
linear lens RGB `(0.002310,0.002190,0.001950)`. Left selects its
-7.994-degree sector at out-of-range UV x=1.542, branch 1, then samples
the cube at approximately `(0.319092,0.295410,0.255127)` and yields
`(0.316162,0.292725,0.252686)`. The small cube/lens difference is
consistent with the lens's own transmittance and emission calculation.

The leftmost added sector is about 108 degrees left of forward, whereas
the traced background rays in this strip are around 32 to 48 degrees
right. It is therefore not the selected source for these pixels. A
potentially more relevant difference is that the primary has its
+42.006-degree sector while the left does not. This is a code-supported
inference, not a validated repair: replacing that sector with a cube
sample or changing lookup policy was not tested.

## Validation, failures, and decision

`node --check`, Universe typecheck, developer .NET build and publish, and
the isolated Vite fixture build passed. The first Vite build attempt was
blocked by sandbox `spawn EPERM`; its approved retry succeeded. Three
earlier fresh run roots were retained after a WebView2 controller failure,
an async result handoff error, and an overstrict first-render hash check.
The final run completed normally. `git diff --check` found no whitespace
errors; it emitted only the pre-existing Screensaver line-ending warnings.
The two repository HEADs and top-level dirty status remained unchanged.

The result meets the NO-GO condition for sector-expansion repair: valid
added coverage did not alter the branch or reduce the lens discrepancy.
The next narrowly scoped design question is whether a globally consistent
fallback/source-selection rule can make the same deflected ray sample the
same representation in both windows without introducing another boundary.
Do not promote a repair from this probe alone. B1 rendered-frame acceptance
remains NO-GO. This diagnostic did not measure motion, compositor
presentation, B2 residency, or B3 display behaviour.
