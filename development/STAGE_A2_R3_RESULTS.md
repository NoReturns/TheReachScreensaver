# Stage A2-R3 native motion-seam forensics

Decision: GO for a focused R4 investigation/correction. Stage A2 is not yet
closed. No R3 renderer correction, production installation, Stage B work, Git
write, or monitor configuration change was performed.

## Exact frozen native state

The disposable developer host gained `--reach-dev-yaw -12..12` and
`--reach-dev-forensics 1`. Forensic mode is accepted only for the cylindrical
`hole-26-left` final-pass fixture with a 512-physical-pixel guard. Its three
windows must report one session ID, fixture ID, serialized scene-state key,
projection mode, and frame revision 1 before each capture. The host writes
hashed monitor PNGs and a separate stage-readback manifest beneath the run
root. `analyze_native_forensics.py` rechecks the manifest against all three
captured display statuses. The GPU completes the fixture render before the
readiness flag is published.

Frozen yaw -6, 0, and +4 runs passed this contract, and each display reported
the expected 3840x2160 / 2560x1440 / 1920x1080 layout with relative origins
(0,0), (3840,720), (6400,1080). The left/primary overlapping sector at
virtual x=3839.5, y=1033.5 used the same global sector centre (-57.994
degrees), source UV (0.75966, 0.51850), focal length 2316.067 pixels, and
composition shift 1216. Local sector indices differed (left 1, primary 0)
because each window holds a selected subset of the same global sectors.
Raw unlensed and lensed half-float RGB agreed at all 4 x 420 sampled
overlapping virtual pixels for all three poses. This checks the resulting
source/lens colour, but does not independently record each deflected ray's
fallback branch. No evidence points to a ray, sector, or crop-coordinate
failure in these sampled pixels.

The yaw -6 diagnostic rerun produced byte-identical PNG hashes on all three
displays with the earlier yaw -6 run. The yaw +4 rerun differed by only one
8-bit value in two pixels on the primary and one pixel on the left. The
serialized scene state and seam statistics were unchanged. Stage readback did
not materially perturb the rendered image.

## First differing stage and bloom coordinates

At yaw -6, raw unlensed, raw lensed, bright pass, and horizontal/vertical mips
0 through 3 agreed across contexts at the seam strip. Horizontal mip 4 was
the first locally different stage: peak linear half-float channel difference
0.010785. It fell to 0.005398 at vertical mip 4 and 0.002946 at composite;
the OutputPass target's peak encoded difference was 0.015152. At yaw +4,
horizontal mip 1 first differed by only about 0.0000001. The first material
difference was again horizontal mip 4 (0.001251), then composite (0.000217)
and OutputPass target (0.000977). The yaw-zero horizontal mip 4 difference
was 0.008829. Comparisons always use corresponding absolute virtual pixel
centres, not local monitor y coordinates or neighbouring final pixels.

At yaw -6, the left bright pass had 14872.09 summed linear luminance units in
virtual x=2560..3327, y=800..1219, a region the primary 512-guard target
cannot contain. In their shared x=3328..3839 band, bright sums matched
(255.35 left, 255.26 primary). By vertical mip 3, the shared-band sums had
diverged (24.41 left, 5.62 primary): the left-only bright source had already
spread into the overlap. Its local seam-strip mip 3 values were still equal;
the broad horizontal mip 4 kernel then carried the difference to the seam.
This directly demonstrates insufficient neighbouring source at yaw -6.

At yaw +4, the bright emitter was in the shared x=3328..3839 band: sums were
14878.88 in both contexts, with zero in the left-only band. The small
remaining mip difference is consistent with different mip dimensions and
sampling phases. The mip-4 targets are 136x68 on left and 112x61 on primary.
At absolute y=907, their nearest mip-4 centre lines are y=905.294 and
y=896.0; earlier mips align at that coordinate. Three.js UnrealBloomPass
repeatedly rounds mip sizes and uses each local target's inverse size for the
Gaussian offsets. This establishes a coarse-level virtual-grid mismatch.
It does not establish a general smaller guard that would preserve all bloom
source contributions.

## Final seam classification

| Frozen yaw | Mean impulse | p95 | p99 | Peak | Rows >20 | Peak same-virtual-pixel OutputPass difference |
| ---: | ---: | ---: | ---: | ---: | ---: | ---: |
| -6 | 2.798 | 14.733 | 68.815 | 99.104 | 34 | 0.015152 |
| 0 | 1.125 | 2.000 | 2.000 | 2.000 | 0 | 0.009094 |
| +4 | 0.654 | 0.892 | 1.073 | 146.106 | 5 | 0.000977 |

The peak final-image impulse compares x=3839 with x=3840, which are adjacent
different scene pixels. At the yaw -6 hard rows, the mean same-context
OutputPass adjacent-channel jump was 0.2757, versus 0.0098 mean disagreement
for identical virtual pixels rendered in both contexts. At yaw +4 these were
0.3773 and 0.00049. For example, yaw +4, y=907, raw lensed RGB switches
from about (0.533,0.575,0.708) at x=3839 to black at x=3840 in BOTH
contexts. The black-hole silhouette crosses the physical seam at that row.
The native-resolution crops show no persistent hard vertical brightness line.
Classification: primarily C, a legitimate high-contrast scene edge that the
four-column impulse metric mistakes for a seam. There is also a small B
cross-context bloom difference at yaw -6 from missing source coverage. The
large impulse values do not measure that residual bloom difference.

The accepted 240-radii 512-guard result remains the R2 baseline (mean 0.700,
zero hard rows). No 240-radii fixture was rerun because R3 changed only the
synthetic fixture/host diagnostics, and its renderer was not edited.

## Guard cost and R4 recommendation

The current 512-guard principal-plus-bloom estimate is 1299.38 MiB versus
993.81 MiB at 128, an increase of 305.57 MiB. Sector-target count changes
2/3/1 to 3/3/2 and accounts for approximately 198.62 MiB of the increase.
The two composer targets account for 73.36 MiB and bloom bright plus five
horizontal/vertical mip pairs for 33.60 MiB. These are calculated allocations,
not measured GPU residency. The retained yaw-zero sweep showed that 128 and
256 kept hard joins, 320 removed rows above 20 but retained a subtle
gradient, and 384/448 improved it further. It did not certify any smaller
guard at moving poses. The yaw -6 probe shows even 512 lacks some distant
bright source for exact bloom agreement, so simply raising the guard is a
poor next step.

The smallest justified R4 scope is to align bloom sampling to one virtual
desktop mip grid, account explicitly for the coarse kernel's source support,
and test 384/448/512 at the frozen failing poses before selecting a storage
trade-off. Recompute the seam metric using duplicated absolute virtual pixels
to separate cross-context differences from real scene edges. Do not alter
black-hole physics, global brightness, cylindrical geometry, or rectilinear
rendering. R3 implements none of this correction.

## Separate rectilinear baseline

Retained R1 rectilinear `sphere-seam` mean impulse 43.114 (498 rows above 20)
and `hole-240-left` mean 109.291 (525 rows above 20) correspond to visible
object/background joins in their native seam crops. These are pre-existing
rectilinear presentation concerns, not cylindrical regressions. They remain
an independent release-acceptance risk; no rectilinear repair was attempted.

## Evidence and validation

Frozen yaw -6: `run-c8d62a5fbd47482a9b6ff8cea2fb0479`; yaw 0:
`run-482a35ac3070449db6d3e463a19c883f`; yaw +4:
`run-c934c27154544f7a9dbba47aaee8c6e4`. Bright-region probes:
`run-a0ea43f8c8074208bd972f14a66c6c19` (-6) and
`run-bd11c13cc2224f53a282d59a5c8bae00` (+4). Each contains original
monitor PNGs, reassembled image, native-resolution seam crop, hashed fixture
manifest, forensic stage readbacks, and analysis JSON under its own root.
The first sandbox-restricted native launch failed during WebView2 controller
creation and was not reused; outside-sandbox reruns succeeded from fresh
roots. No run directory was deleted.

`npm run typecheck`, `node --check development/cylindrical-stage-a1/view.js`,
developer native build/publish, local release build, Python analyzer syntax,
capture/hash/state analysis and native three-window readiness all passed.
The local release executable rejected `--reach-dev-forensics` before ordinary
startup. Existing group readiness/fallback code was not changed by R3 and
was not rerun with injected failure. Source review confirms the R3 fixture
route still uses only sandbox payload, logs and WebView2 profile; no journey,
settings, reset or installed payload path was selected. Visual assessment is
diagnostic and is not owner visual acceptance.
