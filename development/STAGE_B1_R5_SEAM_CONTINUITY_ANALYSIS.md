# Stage B1-R5 native seam continuity analysis

Recommendation: NO-GO for B1 rendered-frame seam continuity. Retained R4
final-output frames show a reproducible localized discontinuity at the
physical left/primary join near yaw +6.5 to +6.8. It recurs in both native
repeats and sweep directions at matched yaw with 0-0.1 ms cross-window
callback skew. The discontinuity is anchored to x=3839/3840 while its
affected y range follows moving scene content. The data do not identify
which render stage causes it. They also do not measure compositor or monitor
presentation. This finding does not itself change the prior A2 decision.

## Source and evidence boundary

Preflight Screensaver `main` HEAD:
`4596b719f10fa39565f7cd34094b305d128825a2`. Universe
`reach-v1-release` HEAD:
`76ab9d49c3cb6ae5d6b03f07073387b8708a47d7`. Both repositories had
their existing dirty status; no capture or renderer code was changed in R5.
The analysis read [the R4 report](STAGE_B1_R4_FULL_MOTION_EVIDENCE.md),
the three R4 manifests and raw RGBA frames, the R4 analysis and image
strips, and `analyze_b1_r4.py`. The retained manifests are:

- Native repeat 1: `runs/run-cd2f2784d3ea46a6bddade97a4d3133c/metrics/b1-cylindrical-643b09a55a25433f96292a97535b603c.json`.
- Native repeat 2: `runs/run-8e997118d7f5434ab523480d66cd400c/metrics/b1-cylindrical-115fe3339d004a92be1eb17c91d3917c.json`.
- Rectilinear control: `runs/run-799b5156367140f5aa3ea074eb739603/metrics/b1-rectilinear-8a9e59b4d41b424bb2750f821a3302ff.json`.

`view.js` maps the left display to virtual x=0..3839, y=0..2159 and
the primary to x=3840..6399, y=720..2159. `__b1Start` captures the
actual final default framebuffer after `engine.render()`, checks its
physical dimensions and framebuffer bindings, and reads left local
x=3648..3839, y=800..1349 and primary local x=0..191, y=80..629.
Both crops therefore contain virtual y=800..1349 and adjacent physical
pixels x=3839 and x=3840. In Windows desktop coordinates both sampled
y ranges are 80..629. Raw RGBA8 rows are bottom-up; the analysis reverses
them before comparing equal virtual y. The 512-pixel native render guard
provides internal virtual coverage, but the captured final-output strips
contain no duplicated virtual x pixel. Identical neighboring pixel values
are not expected or required for a smooth image.

The R3 verifier was rerun on all three R4 manifests. It passed every
stream: 239/227, 228/228, and 236/228 primary/left frames written for
native repeat 1, native repeat 2, and rectilinear; no missing captured
IDs, hash mismatch, queue overflow, capture error, or write reorder.
The identical fixture state key and version-1 yaw trajectory documented
in R4 support matched scene state. Pairing by callback epoch and actual
yaw retained 80 and 165 native pairs at <=2 ms and <=0.02 degrees;
observed maximum skew was 0.1 ms and 0.001 degrees. These are rendered
frame alignments, not verified presentation alignments.

## Boundary method and crossing results

`analyze_b1_r5.py` reads the R4 files without modifying them. It compares
the absolute luma step from x=3839 to 3840 with the mean of the adjacent
within-display steps x=3838 to 3839 and x=3840 to 3841. Positive excess
above 20 on the 8-bit luma scale marks a screening outlier. It also ranks
the join gradient against 16 other adjacent-column gradients within eight
pixels of the join and checks rows where both immediate internal steps
are <=2. This local comparison reduces false alarms at ordinary image
edges, but a feature exactly on the boundary can still be ambiguous.
`STAGE_B1_R5_SEAM_METRICS_V4.json` contains the per-frame values and
timing gaps. Earlier R5 metrics V1-V3 are retained development outputs;
V4 is authoritative.

| Native run and crossing | Tight pairs forward / reverse | Median join luma step forward / reverse | Median internal step forward / reverse | Median hard rows forward / reverse |
| --- | ---: | ---: | ---: | ---: |
| Repeat 1, yaw -6 +/-1 | 6 / 5 | 4.149 / 4.156 | 3.705 / 4.177 | 11.5 / 12 |
| Repeat 2, yaw -6 +/-1 | 4 / 9 | 3.638 / 3.529 | 2.617 / 3.468 | 10 / 6 |
| Repeat 1, yaw +4 +/-1 | 7 / 5 | 1.649 / 1.423 | 1.544 / 1.407 | 3 / 2 |
| Repeat 2, yaw +4 +/-1 | 9 / 9 | 1.406 / 1.209 | 1.481 / 1.236 | 3 / 2 |

At -6, the join gradient can rank first or second of the 17 local
gradients, but most hard rows coincide with a large within-display
gradient at the silhouette/disc edge. Across both native runs and both
directions within the -6 and +4 windows, 281 of 384 hard-row events
coincided with an immediate internal edge over 20. Only six of 25,356
smooth-neighbor row samples had excess over 20. The frame strips show
these crossing impulses moving with the black-hole silhouette and disc.
At +4, the join gradient ranked 12th to 15th of the 17 local gradients
in the four native run/direction groups. These crossing windows alone
would support continuity with A2's documented localized edge differences.

The full -10 to +10 sweep changes the result. Within yaw +6.3 to +7.3,
the join was the largest of the 17 local column gradients in every native
run/direction group. Its mean step was 3.53-6.64 versus a 1.43-1.64
median among nearby internal columns. At yaw +6.696 in native repeat 1
reverse, 49 of 550 rows had excess over 20; 9 of those were smooth
neighbors. At yaw +6.815 in repeat 2 forward, 51 rows were hard, with
8 smooth-neighbor outliers. At yaw +6.796 in repeat 2 reverse, 46 rows
were hard, with 9 smooth-neighbor outliers. The affected region is
localized around virtual y about 970..1050, rather than a full-height
vertical bar. Its y position changes with the image feature, while the
large step occurs at the same physical x boundary. A fixed-y frequency
test across all frames, used in R4, therefore missed this fixed-x
join clipping.

At virtual y=1034, repeat 1 reverse yaw +6.696 has luma samples
`181.1, 189.9, 186.1 | 150.9, 149.9, 148.9` at virtual x=3837..3842.
The bar marks the physical join; the 35.2-unit join step contrasts with
the 3.8-unit immediate left and 1.0-unit immediate right steps. Repeat 2
forward yaw +6.815 yields
`199.1, 203.3, 203.3 | 156.0, 154.9, 153.9`, a 47.3-unit join step
with 0.0 and 1.0 immediate internal steps. Repeat 2 reverse yaw +6.795
has about a 42.4-unit join step at the same y. The compared frames have
identical or <=0.001-degree yaw and 0-0.1 ms callback skew within each
left/primary pair. [Joined final-output strips](STAGE_B1_R5_FULL_STRIPS.png)
and [4x seam zoom](STAGE_B1_R5_SEAM_ZOOM.png) make the local clipping
inspectable. These values demonstrate a rendered adjacent-pixel
discontinuity; they do not prove that the bloom pass alone caused it.

## Motion and temporal limits

The +6.8 event grows and recedes through consecutive frames: repeat 2
forward tight pairs at yaw +6.315, +6.538, +6.815, and +6.982 have
12, 23, 51, and 10 hard rows. Reverse pairs at +7.018, +6.796,
+6.518, and +6.352 have 12, 46, 17, and 13. This order and the
forward/reverse recurrence favor a geometry-dependent effect at a fixed
display join over a random dropped-frame artifact. Matched-yaw seam
excess profiles between native repeats had median absolute differences
of 0.858 luma units for three forward pairs near +6.8 and 2.675 for
one reverse pair; the latter is sparse and had 0.1-degree yaw mismatch.
At -6 and +4 the corresponding cross-repeat medians ranged 0.888-2.626.

Three-frame yaw-interpolation residuals at the seam pixel and eight
nearby columns were of comparable scale at -6 and +4. For example,
native repeat 2 left seam/control p95 residuals were 64.388/65.365
at -6 forward and 10.601/11.974 at +4 forward; primary values were
64.015/60.386 and 12.028/12.193. These large -6 residuals reflect
nonlinear motion of high-contrast geometry across fixed pixels; they
cannot be interpreted as flicker. Near +6.8, the repeated spatial
step is the decisive evidence, not a temporal-residual peak. Rectilinear
followed the same trajectory but has different projection content and
much larger join differences near +6.8. It is a motion/instrumentation
control, not a pixel-identical native reference and cannot establish
which native pass causes the defect.

Native callback p50 was 16.7 ms and p95 22.3-27.8 ms. Repeat 1 had a
166.7 ms gap from yaw -9.919 to -8.252, then 77.7-77.8 ms from -8.252
to about -7.475, and 66.7-72.2 ms from -7.363 to about -6.7.
Repeat 2 had 88.8-88.9 ms from about -9.908 to -9.019, 127.8 ms
from -9.019 to -7.741, and 77.8 ms from -7.741 to -6.963.
Those early intervals lack continuous rendered-frame evidence. No
>50 ms gap occurred inside the -6, +4, or +6.8 windows; each has
consecutive frame IDs in both directions. The p95 spacing still means
some temporal changes could occur between samples. The PBO ring
completed and wrote ordered files after the sweep; its fence/transfer
times do not measure monitor presentation or GPU residency.

## Decision boundary and next diagnostic

Duplicate absolute virtual pixels across contexts are not required to
test the displayed physical seam: x=3839 and x=3840 are the actual
adjacent output pixels, and within-display gradients give the relevant
continuity control. A same-pixel comparison would answer a different
question: whether the two guarded intermediate render/composite buffers
agree at identical virtual coordinates and which stage introduces the
observed step. If pursued later, a developer-only hook could read a
small common virtual x/y rectangle from each existing padded composer
target after composition and before the final crop, using synchronized
frame/yaw metadata. It should record exact target pixel-center mapping
and stage, keep an explicit bounded queue and allocation, and leave
production rendering untouched. R5 did not implement or run that test.

The new +6.8 finding meets the requested NO-GO condition: a repeatable,
seam-specific adjacent-pixel defect in rendered final output. Its
visibility on actual monitors and its precise upstream cause remain
unverified. B1 rendered-motion acceptance remains NO-GO. No B2 or B3
test, capture rerun, installation, display change, cleanup, Git write,
or production change was performed.

## Final workspace status

R5 added this report, `analyze_b1_r5.py`,
`render_b1_r5_examples.py`, the versioned R5 metric JSON files, and two
derived PNGs under `development/` in Screensaver. The R4 raw captures
and both repositories' pre-existing tracked/untracked work were preserved.
Final Git status matched preflight at the top level in each repository;
both HEADs are unchanged. `git diff --check` returned no whitespace
errors, only line-ending warnings on three pre-existing Screensaver
modifications. The analysis scripts passed Python syntax checks. No
native or Universe build was run because no rendering source changed.
