# Stage B1-R10 motion and sector coverage validation

Recommendation for a bounded dynamic selector: REVISE. R9 frozen-frame
parity remains accepted, but the current evidence does not justify a
runtime sector entry/exit rule. B1 production acceptance remains NO-GO.

## Evidence and controls

The final isolated run is
`development/runs/run-98efc64b8cfb4ac88e7ddd00a432d6ad`.
Its `metrics/sector-motion-*.json` manifest identifies 82 paired yaw
records, complete final PNG frames for the left and primary windows, and
per-frame trace JSON. `metrics/sector-motion-analysis.json` has the
full-join and full-frame comparisons. `metrics/r10_join_6815_zoom.png` is
the inspected physical-join crop. The run used the R9 `hole-26-left`
state at frozen yaw +6.815 for startup and rollback, 512-pixel guard,
native bloom, and the recorded 8320x2160 three-monitor layout. The
capture sequence then swept from -12 to +12 degrees, with 0.1-degree
steps across most of +6.0..+8.1 and an exact +6.815 sample. Every frame
was rendered with the accepted lens shader and ordinary source choice.

The two phases were ordinary rendering and rendering with the R9 +42.006418
degree sector appended to the left renderer instance. It remained present
for every yaw in that phase. Primary and right source lists were unchanged.
The same state key was recorded for all three windows at each yaw, no
WebGL context loss was reported, and left target count was 3 ordinary or
4 with the extra sector. Primary stayed at 3; right stayed at 2.

The initial disabled frame and the final disabled frame at +6.815 each
differed by at most one channel unit from the original startup capture
on left and primary. The R9 frozen-frame result therefore remains
reproducible after this sweep. These one-unit changes are the existing
repeat-render variation, not a changed baseline. The R9 full lens buffers
and allocation result remain in `STAGE_B1_R9_FULL_FRAME_SECTOR_PARITY_PILOT.md`.

## Join measurements through motion

For each yaw, the analyzer compared encoded Rec. 709 luma at left x=3839
and primary x=3840 over all 1,440 overlapping virtual rows y=720..2159.
The table gives mean step, rows above 20, and rows with different valid
sector-versus-cube branches in the two active lens traces. It separates
trace pixels that never called background lookup.

| Yaw | Mean ordinary | Mean extra-left | Rows >20 ordinary | Rows >20 extra-left | Branch mismatch ordinary | Branch mismatch extra-left |
| ---: | ---: | ---: | ---: | ---: | ---: | ---: |
| -8 | 1.281 | 1.281 | 1 | 1 | 52 | 52 |
| -6 | 3.475 | 3.475 | 61 | 61 | 35 | 35 |
| 0 | 1.027 | 1.027 | 0 | 0 | 0 | 0 |
| +5.75 | 2.747 | 2.739 | 43 | 43 | 7 | 5 |
| +6.0 | 1.635 | 1.463 | 11 | 10 | 56 | 5 |
| +6.4 | 2.176 | 1.220 | 24 | 5 | 167 | 2 |
| +6.5 | 2.187 | 1.085 | 19 | 2 | 179 | 0 |
| +6.7 | 3.024 | 0.964 | 56 | 0 | 177 | 0 |
| +6.815 | 2.917 | 0.967 | 59 | 3 | 167 | 0 |
| +7.0 | 1.925 | 0.869 | 18 | 2 | 146 | 0 |
| +7.5 | 0.859 | 0.740 | 2 | 1 | 72 | 0 |
| +7.6 | 0.809 | 0.759 | 0 | 0 | 48 | 0 |
| +7.7 | 0.783 | 0.783 | 1 | 1 | 0 | 0 |
| +8.0 | 0.727 | 0.727 | 0 | 0 | 0 | 0 |

At +6.815, the original row y=1034 step again fell from 47.269 to
1.928. The ordinary mismatch reaches the join by the sampled +5.75
position, is materially reduced by +6.0, and is absent from the traced
join by +7.7. The fixed sector makes the branch choice equal on all
active join rows from +6.5 through +7.6. These are sampled boundaries,
not exact continuous thresholds. Between +6.0 and +6.4, residual branch
mismatch remains, and the final-frame improvement is incomplete.

At yaw -8, the left lens selects its -107.994-degree source while primary
falls back to the cube on 52 active join rows. At -6, that mismatch remains
on 35 rows, and the final join has 61 rows above 20 in both phases. At
y=1033, the final step is 107.21 luma units. The sampled ray angles
cross the -180/+180 wrap there; one traced row has sector/cube mismatch,
while the peak row uses cube fallback on both sides. Thus the large -6
defect is not explained entirely by the counted branch mismatch. Adding
only +42 to left does not address it. The R7 test of adding -107.994 to
primary at +6.815 does not test its effect at negative yaw.

## Sector entry, exit, and frame continuity

Across the paired complete left PNGs, the extra source changes pixels
greater than one channel unit even at yaw -12. At -12 the change bounds
are x=3065..3587, y=937..1135, with 628 pixels changing more than 20
channel units and peak channel change 73. At yaw 0, the change bounds are
x=3542..3634, y=917..1113, with 1,097 pixels above 20 and peak 91.
At +6.815 they are x=3806..3839, y=949..1113. The affected region
moves right with yaw and shrinks after +5.75; at +7.6 it is only
x=3836..3839 with 90 pixels above one unit, and at +7.7 there are none
above one unit. This is a source entry and exit across the left frame,
not just a join event.

A selector that first allocates +42 when the physical join begins to fail
would switch between visibly different complete frames. The extra source
still changes visible interior pixels at the -12 limit of this fixture,
so this experiment did not locate a pop-free entry angle. The fixed
sector has no allocation transition during the sweep. A dense stepped
comparison found no newly extended vertical line: the largest count of
new strong adjacent-column steps at any one x was five rows. In the
original failing region, the changed bounds progress monotonically with
yaw. These captures do not prove absence of temporal flicker at native
frame cadence, particularly near caustics or a future allocation switch.
The unchanged -6 discontinuity and some isolated +5.75/+6.1 join rows
remain visible risks.

## Deflected-ray envelope and other layouts

The trace hook sampled a 64x64 grid across each padded lens rectangle
and every row of a four-pixel strip at the left/primary join. Across the
sweep, sampled left deflected angles reached -179.61 to +179.72 degrees.
The lens can wrap around the camera near the black hole. A simple minimum
and maximum angle over the padded rectangle would demand nearly full
angular coverage and is not a useful bounded proof for adding only +42.
The grid is a sample, not a conservative mathematical envelope: thin
caustics and inter-frame motion can exceed it. No safe numerical yaw or
pixel margin can be certified from this run alone.

`development/STAGE_B1_R10_LAYOUT_GEOMETRY.json` applies the current
`sectorCenters`, `displaySectorCenters`, focal and 512-pixel padding
formulas to three representative contracts:

| Virtual layout | Selected centers by display | Join asymmetry |
| --- | --- | --- |
| Single 3840x2160 | -52.580, -2.580 | No physical join |
| Dual 6400x2160 | Left: -84.245, -34.245, +15.755; primary: -34.245, +15.755 | Left-only -84.245 |
| Recorded 8320x2160 | Left: -107.994, -57.994, -7.994; primary: -57.994, -7.994, +42.006; right: -7.994, +42.006 | Left/primary has one exclusive sector on each side |

The geometry result for the recorded layout matches the actual source
lists captured during R10. Single and dual layouts were not rendered:
the isolated native fixture verifies the owner's recorded physical layout,
and this task did not change Windows display settings. Their source lists
are code-equivalent geometry checks, not visual or GPU validation. A
different desktop width changes the global sector centers, so the R9
absolute +42 sector must not be hardcoded for other layouts.

## Cost and proposed strategy

The fixed left source adds one 3244x2675 RGBA16F color target with depth
and four samples. Color plus depth alone is 104,132,400 bytes, or
99.31 MiB, excluding driver and multisample storage. R9 measured 14
additional full-frame draw calls on left (142 to 156) and about 0.06 ms
additional completed-render wall time in one frozen run. R10 confirms
the target count across yaw but did not remeasure GPU residency or time.
Adding the corresponding -107.994 source to primary would have the same
99.31 MiB nominal footprint at the recorded layout. Both additions
would total 198.62 MiB nominal and about 28 extra scene draws if each
rendered 14 calls. That second cost is an estimate, not an R10 measurement.

The minimal deterministic candidate is to derive each neighbor's global
sector identities from the display contract, then close each physical
join over the union of sources needed by deflected rays on both sides.
That union must be computed before a sector becomes visibly sampled,
held through a bounded motion lookahead and hysteresis interval, and
limited by explicit per-window target count and GPU residency budgets.
The source list must keep the same global ordering on every display so
ordinary nearest-sector selection is deterministic. A cap must produce
an explicit unsupported-layout result in development validation rather
than silently reverting one side to the cube. Static join closure at
initialization is a simpler candidate while entry/exit remains unproved.
This is a design recommendation only; no selector was implemented.

Before implementing a bounded dynamic selector, establish a conservative
ray envelope or a proved safe overapproximation for the supported lens
state range, including angle wrap, UV validity, motion velocity, frame
lookahead and a measured safety margin. The entry rule must allocate
before any changed pixel appears, including interior pixels, and the exit
rule must retain a target until all such rays leave. The current -12 to
+12 sweep cannot provide that entry guarantee because changed pixels
remain at -12. Release conditions also include rendered single and dual
layout fixtures, both physical joins, a negative-yaw source parity test,
continuous-cadence capture across allocation transitions, no new visible
discontinuity, bounded measured GPU residency, and owner visual review.

## Validation and scope

The developer .NET build and publish, isolated Vite build, `node --check`,
Universe typecheck, native fixture verifier and `git diff --check` passed.
The first R10 launch was interrupted by the developer readiness watcher
while frame revisions advanced; the watcher was scoped away from the
bounded motion sweep, and both later runs completed. No production
renderer or shader, accepted lens mathematics, 512-pixel guard, primary
source list, installation, owner data or Windows selection was changed.
No cleanup, commit, push or release was performed.
