# Stage B1-R4 full continuous-motion evidence

## Preflight and validity criteria (recorded before R4 capture)

Source state: Screensaver `main` HEAD
`4596b719f10fa39565f7cd34094b305d128825a2`; Universe
`reach-v1-release` HEAD `76ab9d49c3cb6ae5d6b03f07073387b8708a47d7`.
Pre-existing dirty trees match `STAGE_B1_R3_ASYNC_CAPTURE_RESULTS.md` and
are to be preserved. This R4 uses the R3 developer host and fixture builds
under `development/b1-publish-r3b/` and
`development/b1-fixture-build-r3c/`, with no source or renderer changes.

The native fixture contract must report `hole-26-left`, final pass,
`bloomMipMode=native`, guard 512, the same session/scene state and physical
display layout. The source path ends with the cylindrical composer and its
crop-to-default-framebuffer render. The fixture rejects a non-default draw
or read framebuffer and a drawing-buffer size that differs from the physical
display. The pixel-pack command follows that final render. The left crop is
local x=3648..3839, y=800..1349; the primary crop is x=0..191, y=80..629.
Both represent virtual y=800..1349 on opposite sides of virtual x=3840.
They contain the moving black-hole edge, disc and nearby bloom in the R3
previews. They do not duplicate an absolute virtual pixel across contexts.
Any same-virtual-pixel overlap claim must use the retained frozen R3/R4
forensics, not these continuous final-output strips.

Evidence is valid for rendered-motion analysis only if every captured
display reports completed sweep and drain, zero errors and overflow, one
written frame per callback, ordered IDs and writes, valid hashes/crop sizes,
and both directions of yaw -6 and +4 represented by consecutive frames.
Report callback p50/p95/max and each gap above 50 ms; exclude a crossing
window from seam comparison if a gap or left/primary timing/yaw skew prevents
pairing within 30 ms and 0.3 degrees. Compare paired native edge pixels only
as adjacent virtual positions. Attribute a seam to the renderer only when a
fixed-coordinate contrast persists across multiple frames and is not
explained by the moving silhouette or disc in the neighbouring pixels.
Use yaw-matched forward/reverse views and frame sequences to assess temporal
popping; do not interpret a single large edge impulse as flicker. If these
conditions or the limited crop prevent a defensible conclusion, decide
REVISE. Rectilinear is a motion control, not a pixel-identical reference.
No captured frame is a verified compositor presentation timestamp.

## Collection and analysis

Recommendation: REVISE for B1 rendered-motion acceptance. The ordered
final-output strips support a narrower finding: no persistent fixed-row seam
or obvious frame-to-frame pop was found in the sampled seam region during
these runs. They do not supply duplicate absolute physical pixels across
the native contexts, and the 192-pixel-wide crops do not cover the full
bloom field. The outstanding overlap and localized-flicker questions remain
open. This is not evidence of compositor presentation synchronization.

### Runs and integrity

All runs used the unchanged R3 developer host and fixture builds. The host
executable SHA-256 was
`655A15B439C37962A045665CC951FD031090D8A77F150306E7A578750B589C5B`;
the fixture JS SHA-256 was
`A1EC3525460297C7BE327D4C3B76B2BB3A2087A3522A104137C7E6D28C0DF030`.
Each run has a `metrics/b1-*.json` capture manifest, a
`metrics/fixture-*.json` fixture record, raw ordered
`captures/b1-*.rgba` files, logs, and the analysis named below. The paths
are relative to `development/runs/`.

Exact capture manifests, within the run IDs in the next table, are:

- Native repeat 1: `metrics/b1-cylindrical-643b09a55a25433f96292a97535b603c.json` and `metrics/fixture-hole-26-left-cylindrical-final-643b09a55a25433f96292a97535b603c.json`.
- Native repeat 2: `metrics/b1-cylindrical-115fe3339d004a92be1eb17c91d3917c.json` and `metrics/fixture-hole-26-left-cylindrical-final-115fe3339d004a92be1eb17c91d3917c.json`.
- Rectilinear control: `metrics/b1-rectilinear-8a9e59b4d41b424bb2750f821a3302ff.json` and `metrics/fixture-hole-26-left-rectilinear-final-8a9e59b4d41b424bb2750f821a3302ff.json`.

| Mode | Run ID | Frames primary / left | Callback p50 ms | Callback p95 ms | Worst gap ms | Gaps >50 ms | Peak PBOs primary / left |
| --- | --- | ---: | ---: | ---: | ---: | ---: | ---: |
| Native guard 512, repeat 1 | `run-cd2f2784d3ea46a6bddade97a4d3133c` | 239 / 227 | 16.7 / 16.7 | 22.3 / 22.3 | 166.7 / 166.7 | 3 / 3 | 239 / 227 |
| Native guard 512, repeat 2 | `run-8e997118d7f5434ab523480d66cd400c` | 228 / 228 | 16.7 / 16.7 | 22.3 / 27.8 | 127.8 / 127.8 | 3 / 3 | 228 / 228 |
| Rectilinear motion control | `run-799b5156367140f5aa3ea074eb739603` | 236 / 228 | 16.7 / 16.7 | 27.8 / 27.8 | 88.8 / 88.9 | 2 / 1 | 236 / 228 |

`verify_b1_r3.py` passed each R4 manifest: one written frame per captured
callback, monotonically ordered IDs and host writes, valid crop and payload
sizes, SHA-256 matches, nonconstant pixels, no missing IDs, no queue
overflow, and no capture errors. The right window completed each sweep; all
three host groups stopped normally. The 256-slot ring stayed within its
206.25 MiB nominal two-display allocation. No allocation failure or
context-loss message appeared in the run logs. Available system memory was
48,081 MiB before and 47,996 MiB after the runs; this coarse observation
is not a GPU-residency result.

The fixture state key was identical in all three runs:
`73b47dddf091292cc9b68e671c1c717d7107da84eb8df2b7bfa20ec7db29d09a`.
Native records report `hole-26-left`, the final output pass,
`bloomMipMode=native`, guard 512, and frozen material and black-hole shader
times. The developer-only trajectory is version 1, four seconds, yaw -10
to +10 and back, with virtual time recorded separately from callback time.
The final-output readback check requires default draw/read framebuffer,
physical drawing-buffer dimensions, and the crop specified above. The
seam is at virtual x=3840; the two captured rectangles cover its opposite
sides at the same virtual y, including the moving silhouette, disc edge,
and surrounding bloom shown in the contact sheets. The two contexts do not
capture an overlapping absolute virtual x coordinate.

### Cadence and frame validity

Both directions have consecutive captured IDs within one degree of yaw -6
and +4 on both displays. The >50 ms callback gaps occur near initial yaw
-10 to about -7.4, before the selected crossings; one first-run gap leads
into the approach to -6. There is no >50 ms gap inside either crossing
window. Median callback spacing met the 16.7 ms R3 no-readback baseline,
but native p95 reached 22.3-27.8 ms, and isolated worst gaps reached
127.8-166.7 ms. Thus a median alone overstates continuity. The raw
manifests retain every callback, render, readback submission, host receipt,
and file-write timestamp for inspection. Render spans measure CPU command
submission; they are not GPU execution or presentation timestamps.

Pixel-pack submission p50 was 0.0 ms and p95 0.1 ms at browser clock
resolution. Completed-buffer transfer after the sweep was about 0.2 ms
p50 and 0.3-0.4 ms p95; encoding was about 5.9-6.1 ms p50 and 6.6-7.1 ms
p95. Fence polling and bulk transfer were deferred until after the motion
interval. The first zero-timeout poll found every retained buffer signaled,
which does not reveal when each GPU operation completed. Host drain and
file writing preserved order. The R3 native no-readback and rectilinear
no-readback manifests remain the trajectory/cadence controls; this R4
performed no B2 timing or residency stress test.

One-to-one left/primary callback pairing within 30 ms and 0.3 degrees
found 227, 228, and 226 pairs in the three runs. Native pairing skew was
p50/p95/max 11.1/22.3/27.8 ms in repeat 1 and 5.6/16.7/16.8 ms in
repeat 2; maximum yaw skews were 0.278 and 0.168 degrees. Differences
at a fast-moving high-contrast edge are sensitive to even this skew.
The stricter comparison at <=2 ms and <=0.02 degrees retained 80 and
165 native pairs, with observed maximum skew 0.1 ms and 0.001 degrees.

### Temporal image findings

`development/analyze_b1_r4.py` produced authoritative
`metrics/b1-r4-analysis-v2.json` and
`captures/b1-r4-v2-*.png` inside each run. Its seam measure is the
positive excess luma jump between adjacent left/primary pixels, after
subtracting the mean of the neighboring within-display pixel jumps. A
"hard row" exceeds 20 on the 8-bit luma scale. This is a screening
metric, not a same-pixel equality test. An earlier
`metrics/b1-r4-analysis.json` output had a hard-run calculation error;
it was retained without cleanup and must not be used for conclusions.

| Native run | Tight -6 forward / reverse: pairs; median, max hard rows | Tight +4 forward / reverse: pairs; median, max hard rows | Fixed-row test |
| --- | --- | --- | --- |
| Repeat 1 | 6; 11.5, 22 / 5; 12, 46 | 7; 3, 5 / 5; 2, 4 | 0 rows hard in >=50% of tight frames; largest row frequency 6.2% |
| Repeat 2 | 4; 10, 34 / 9; 6, 19 | 9; 3, 5 / 9; 2, 4 | 0 rows hard in >=50% of tight frames; largest row frequency 4.8% |

There were no tight native frames with more than 100 hard rows. The
repeat-2 exact-yaw -6 forward pair, left and primary ID 11, had zero
timestamp/yaw skew, 34 hard rows, mean impulse 3.964, and peak 118.163.
Its large peak sits where the moving silhouette/disc edge meets the seam.
The tight-pair time strips show curved moving peaks that track the
black-hole boundary; they do not show a constant-y high contrast band.
The five-frame contact sheets at both crossings and sweep directions
show continuous motion of the silhouette, disc, and nearby bloom, with
the strongest boundary changes following those features. Broad pairs
can falsely inflate hard-row counts: their yaw skew reaches 0.278
degrees. They were not used to declare a renderer defect.

At matching yaw across the forward and reverse passes (up to 0.12 degree
difference), median full-crop RGB mean absolute byte error in the two
native runs was 1.59-2.69 at -6 and 0.20-2.92 at +4, depending on
display and run. The maximum within these comparisons was 6.21. This
supports repeatable rendered scene state, while yaw mismatch and
screen-space sampling prevent exact equality. A three-frame interpolation
test on full-crop mean luma found maximum residual 0.308 in native repeat
1 and 0.338 in repeat 2, in 8-bit units, near the crossings. The
rectilinear control maximum was 0.358. Such full-crop averages can hide
small localized flicker. Visual inspection of the ordered native sheets
did not reveal a new fixed-coordinate vertical bar or obvious abrupt
bloom pop in the sampled strips; it cannot rule out flicker elsewhere or
between retained callbacks.

Representative evidence, relative to `development/runs/`:

- Repeat 2, -6 forward: `run-8e997118d7f5434ab523480d66cd400c/captures/b1-r4-v2-minus6-forward.png` (includes exact-yaw ID 11).
- Repeat 1, -6 reverse: `run-cd2f2784d3ea46a6bddade97a4d3133c/captures/b1-r4-v2-minus6-reverse.png`.
- Repeat 2, +4 forward and reverse: `run-8e997118d7f5434ab523480d66cd400c/captures/b1-r4-v2-plus4-forward.png` and `b1-r4-v2-plus4-reverse.png`.
- Repeat 1 and 2 temporal seam maps: `captures/b1-r4-v2-tight-seam-impulse-time.png` in each native run.

Each contact-sheet tile joins the final-output left crop to the primary
crop at its middle. Its label gives actual yaw, left/primary IDs, and
callback skew. The seam map uses time horizontally and virtual physical y
vertically; brightness is the seam-impulse screen metric. Its bright arcs
move with scene geometry. Rectilinear output follows the same trajectory
and timing contract and passed capture integrity, but is only a motion
control because its projection and bloom positions differ.

### Native overlap and remaining limits

The native screen layout has 512 physical pixels of rendering overlap
available in the accepted implementation, but this R3/R4 final-output
capture records only the nonoverlapping 192-pixel strips on either side
of the display join. Final display pixels at the same absolute x are not
present in both streams. The retained A2 frozen-pose forensics can compare
same-virtual-pixel samples at yaw -6 and +4; they cannot establish their
temporal behavior during this continuous run. The observed small,
geometry-following boundary differences are compatible with A2's
documented localized bloom differences, but this dataset cannot quantify
their continuous same-pixel overlap or determine whether they amplify at
unobserved positions. The full black-hole and bloom field is also outside
the narrow strips.

Captured frame order is verified, but independent WebViews are not
presented in lockstep. The 0-28 ms cross-window skew, p95 callback
slowdown, and startup outliers limit transient comparisons. Browser
callback, CPU render submission, deferred GPU readback completion, host
receipt, and file-write times are distinct. No compositor, scan-out, or
monitor presentation timing was measured. No absence-of-flicker or
presentation claim follows from these captures.

For a B1 acceptance decision, retain the R3 async ring and add a bounded
developer-only final-output overlap crop at identical absolute virtual
coordinates on both native windows, with enough margin to include the
silhouette/disc and bloom around the join. Keep the ring allocation at or
below 206.25 MiB by trading strip dimensions or frame count rather than
increasing it silently. Repeat paired four-second sweeps and inspect
ordered tight-skew frames plus localized temporal changes. This is a
recommendation for a later authorized step, not an implementation in R4.
No production renderer, shader, bloom, guard, installation, display
configuration, backup, or owner data was changed. B2 and B3 were not run.

## Final source and repository status

R4 added only this report and `development/analyze_b1_r4.py` in the
Screensaver development directory, plus disposable run-local evidence.
The Universe source tree was not edited in R4. Screensaver remains at
`4596b719f10fa39565f7cd34094b305d128825a2`, with the same top-level
dirty status as preflight: modified `.gitignore`, `Program.cs`, and
`TheReachScreensaver.csproj`; untracked developer host and `development/`.
Universe remains at `76ab9d49c3cb6ae5d6b03f07073387b8708a47d7`,
with the same pre-existing modified, deleted, and untracked paths as
preflight. `git diff --check` in Screensaver returned no whitespace errors;
it emitted only line-ending warnings for three pre-existing modified
files. No Git write or remote command was run.
