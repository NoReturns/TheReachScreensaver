# R21 static source visual acceptance gate

Date: 2026-10-28. Mode: isolated developer validation. Decision: **REVISE**. B1 remains **NO-GO**. This is a technical review of rendered fixtures, not owner visual or production acceptance.

## Result

The static full-resolution 2x-MSAA left sector retains R20's large positive-yaw join correction. In the R20 frozen +6.815-degree native comparison, left-primary rows above 20 luma fell from 59 to 3. The new matched high-cadence join crop at +6.6 to +6.8 degrees has 45-52 such rows in control and 1-4 in static. The extra source is allocated at initialization and remains present throughout the motion path. No abrupt source-allocation transition appears in the captured sequence.

The candidate also changes a visible interior black-hole lens feature. At yaw -12, -6 and 0, the control contains a small curved secondary image left of the black-hole rim; the static candidate reduces it to a faint sliver. The feature moves with the scene, and the sampled static motion shows no obvious screen-fixed ghost or abrupt one-frame jump. Neither the final frames nor the branch trace establishes which RGB image is physically correct. This material, moving image change needs source-truth and owner visual review before the candidate can pass. Planet motion and dense-star motion remain untested.

## Source and fixture boundary

Read [`R20_STATIC_SOURCE_PARITY_VALIDATION.md`](R20_STATIC_SOURCE_PARITY_VALIDATION.md), its 41-yaw native frames and source traces, 60-second motion crops, static star/planet frames, and [`paired-sweep-summary.json`](r20-runs/paired-sweep-summary.json) before capturing R21. The host's `CaptureContinuous` in [`ReachDeveloperApplication.cs`](../Application/ReachDeveloperApplication.cs) captures asynchronous WebGL2 RGBA8 readbacks from left and primary; its native crop rectangles align at the left-primary virtual join. R21 used the R20 control and static developer bundles, copied by [`r21_prepare.py`](r21_prepare.py) into [`r21-variants`](r21-variants/manifest.json). The only R21 bundle changes are the developer capture trajectory, 60 Hz quantized virtual timestamps, an interior capture rectangle in the interior variant, and a 320-frame readback capacity. The R20 control/static renderer difference is unchanged. No production renderer, shader, accepted lens equation, perspective-source count, scene state, sector raster, cylindrical focal, output ray, physical monitor rectangle, native bloom or 512-pixel guard was changed.

The isolated owner-layout fixture is left 3840x2160, primary 2560x1440 and right 1920x1080 in an 8320x2160 virtual desktop. The left and primary crop positions are native physical pixels. For the join sequence, left `(3648,800,192,550)` and primary `(0,80,192,550)` both cover virtual Y 800-1349. For the interior sequence, left `(3040,900,560,280)` covers virtual Y 900-1179. Captures are native RGBA8 bottom-up; the review MP4s keep the pixels at 1:1 before lossy video encoding. The lossless `.rgba` files and frame JSON are the measurement evidence.

Each serial run follows -12 to +12 to -12 degrees in four seconds, with pose quantized to a 60 Hz virtual clock. Control and static frames were paired only when left and primary share the same virtual tick; `actualYaw` equals the fixture's intended yaw within floating-point error. Camera pose, fixture state and timestamp therefore match within each comparison, although wall-clock callback times do not. The MP4 review clips repeat each matched source frame three times at 24 fps for inspection; playback speed and missing ticks are not evidence of native smoothness.

## Owner review sequence

Review these in order. The clips label projection, source configuration, yaw and virtual timestamp on every frame. The full-context and full-join PNGs reuse exact-state R20 native frames; the MP4s are R21 motion readbacks.

| Review | Material | Purpose |
| --- | --- | --- |
| 1 | [Full physical context at +6.815](r21-review/full-physical-context-yaw-6.815.png) | All three physical monitors; control above static. The complete native PNGs are in the frozen [control](r20-runs/run-089cfc54137e4f26afd4e075033f26ee/captures) and [static](r20-runs/run-2475f2871bd147249880eff63c3d5892/captures) runs. |
| 2 | [Positive-yaw join motion](r21-review/join-positive-control-vs-static.mp4) and [full native +6.815 join](r21-review/full-join-yaw-6.815.png) | The known +6.815 failure and the moving source boundary, virtual t=1450-1683 ms, yaw about +5.4 to +8.2. Each half of the clip joins left then primary native pixels. |
| 3 | [Full native +5.75 join](r21-review/full-join-yaw-5.750.png) | Residual bright adjacent-ray gradient despite the added source. |
| 4 | [Negative-yaw interior motion](r21-review/interior-negative-control-vs-static.mp4), [near-zero interior motion](r21-review/interior-zero-control-vs-static.mp4), and [existing native stills](r21_phase1_existing_evidence.png) | Inspect the secondary lensed feature that becomes a faint sliver in static. Motion intervals are t=0-650 and 900-1150 ms, with yaw -12 to about -4 and about -1.2 to +1.8. |
| 5 | [Negative-yaw join motion](r21-review/join-negative-control-vs-static.mp4) and [near-zero join motion](r21-review/join-zero-control-vs-static.mp4) | Check the R11 caustic control and yaw-zero join. |
| 6 | [Full physical sparse-star state](r21-review/full-physical-stars.png) and [planet state](r21-review/full-physical-planet.png) | Exact frozen control/static pairs. R20 also has 59 matched 1 Hz [2x star](r20-runs/run-84436985fb93489da7e00d4ce36f4d71) and [static star](r20-runs/run-d65eab9052314c26b5757e8dab4f6583) samples. These are sparse observations, not a native motion-smoothness test. Planet motion was not captured. |

All new raw captures and timing metadata are under these exact run directories:

| Configuration | Run directory |
| --- | --- |
| Control join | `C:\Storage\Screensaver\development\r21-runs\run-046b63e635bd4c7ab2dba88dff0b0202` |
| Static join | `C:\Storage\Screensaver\development\r21-runs\run-556e529c796741dc8041f157d306670a` |
| Control interior | `C:\Storage\Screensaver\development\r21-runs\run-d4cc47baf8864208aa9d48aa29d1f8fb` |
| Static interior | `C:\Storage\Screensaver\development\r21-runs\run-e014d3ecbb8e48fd99dc63167250740b` |

Each run has `captures/b1-cylindrical-*.rgba`, `metrics/b1-cylindrical-*.json` with exact file paths, timestamps, crop rectangles and hashes, and `metrics/fixture-*.json` with initial renderer status. [`r21-review/analysis.json`](r21-review/analysis.json) records every pose-matched pixel comparison and review clip. [`r21_review.py`](r21_review.py) is the reproducible analysis and presentation script.

## Findings by pose and time

R20's 41-yaw sweep first located the highest left-monitor changes near +5 to +6 degrees at x roughly 3736-3839 and y 915-1113, plus interior changes at yaw -12 (x3065-3587), -6 (x3306-3830), and 0 (x3542-3634). R21's positive join clip covers t=1450-1683 ms on the first forward sweep. Its negative and zero clips cover t=0-650 and 900-1150 ms. The return sweep is in the raw four-second runs and in [`analysis.json`](r21-review/analysis.json). The exact +6.815 frame is the preserved R20 frozen state; the quantized motion trajectory passes +6.8 and +7.0, not exactly +6.815.

| Yaw / region | Observed result | Classification |
| --- | --- | --- |
| -12, -6, 0, left interior | The control's secondary curved lens feature is substantially reduced in static. In the 560x280 crop, the matched forward frames have roughly 628, 991-1049 and 1068-1095 pixels with a channel difference above 20 respectively. The affected feature moves with yaw. | Material source-dependent image change; physical correctness unresolved. This is the principal visual gate failure. |
| -6, physical join | R20 static retains 60 rows above 20 luma; R21's narrower join crop has 17-61 rows in both variants over yaw -6.4 to -5.6. | R11's genuine caustic/adjacent-ray gradient remains. The extra sector does not repair it. |
| +5.75 to +6.4, physical join | R20 static has 43, 10, 21, 6, 5 and 5 rows above 20 at +5.75, +6.0, +6.1, +6.2, +6.3 and +6.4. The full native +5.75 pair shows effectively the same bright join gradient in control and static. R21's +5.8 to +6.2 crop has 6-11 control versus 4-11 static bright rows. | Residual visible high-gradient join, largely an adjacent-ray result. The low-contrast branch mismatches alone do not explain the bright rows. No new long seam was observed in these frames. |
| +6.6 to +6.8, physical join | R21 550-row crop falls from 45-52 bright rows in control to 1-4 in static. R20's exact +6.815 native result is 59 to 3. | Proven correction of the original positive-yaw source-representation failure in the observed state. |
| +7 to +7.6, physical join | In the sampled forward R21 crops, control has 0-16 bright rows and static 0-1. | Correction persists over the sampled exit interval. |
| Positive-yaw interior crop | The chosen x3040-3599 region is byte-identical in the sampled +5.8 to +7.4 states. | The positive-yaw change is concentrated nearer the left-primary join; this crop cannot clear other interior pixels. |

R20 source traces report 5-9 branch-mismatch rows at +5.75 to +6.3 and 2 at +6.4; +6.815 has none. The branch-mismatch rows around +5.75 to +6.4 have no overlap with rows above 20 final-output join luma in the prior bounded trace analysis. The +5.75 trace also has 13 differing sector-identity rows, two of which exceed 20 final-output luma. Thus some isolated high-gradient source difference remains, but the large bright +5.75 gradient is present in both final images. A source branch trace is not RGB source parity. The exact trace records are in the static [R20 41-yaw run](r20-runs/run-5b106ef9b7d44c57ac6963d801e63d43/metrics), with control records in the paired [control run](r20-runs/run-ffd989e6bee24dc7ba8cd1f0d3b46a5f/metrics).

The primary-right regression control is the R20 exact-state frozen +6.815 PNG pair and sparse-star/planet pairs in the full-physical review images. Both frozen sparse scenes are byte-identical on all three monitors and have zero primary-right join rows above 20 in R20's analysis. The R21 continuous host captures only left and primary, so primary-right motion was **not** newly certified. No inference about other layouts follows from this one fixture.

## Temporal and resource check

The four R21 runs recorded 228-237 left or primary callbacks per four-second path. Join pair: 228/230 control and 231/232 static callbacks for left/primary. Interior pair: 228/229 control and 228/237 static. There were 212 exact common join ticks and 225 exact common interior-left ticks. Median callback interval was 16.7 ms in each recorded left/primary stream. Each had two gaps above 50 ms, with maxima of 138.9-194.5 ms. All four async capture summaries report zero readback overflow and zero WebGL errors; fixture initial statuses have no renderer error. The sampled clips do not show a screen-fixed blend or obvious abrupt allocation pop. The gaps, serial runs, lossy review encoding and 4-second duration prevent a claim of native scanout smoothness or flicker-free operation.

The initial fixture status in both R21 pairs reports control sector targets `3/3/2` and static `4/3/2` for left/primary/right, each 3244x2675 at 2x MSAA. The candidate's extra left target exists before the path starts. No R21 code toggles its allocation with yaw. No new lens or shader mathematics was introduced. This matches R20's stable target count through its 41-yaw trace and 60-second motion runs. The R21 readback increases capture workload and buffer use, so it was deliberately kept separate from ordinary performance measurement. R20 remains the cost baseline: approximately 333 MiB additional process-attributed dedicated GPU memory in the black-hole motion pair, 14 additional left scene draws, and no reported WebGL error or context loss. No new GPU residency, utilization or throughput number is inferred from these capture runs. The high-cadence callback intervals do not establish GPU frame time.

Native final output includes native bloom and the 512-pixel guard. The bright join correction persists after that post path. R20's star and planet frozen final PNGs are byte-identical across control/static, and its 4x-versus-2x sparse-star check was favorable. R21 did not produce a new dense-star shimmer or moving-planet comparison, and did not separately trace bloom intermediates.

## Decision and smallest follow-up

**REVISE.** The static correction is technically stable in this bounded capture and substantially repairs the original +6.815 physical seam at its measured 333 MiB and 14-draw baseline cost. It is not yet suitable to call visually acceptable: the control/static interior lens difference at negative and zero yaw is plainly visible, and the remaining +5.75 join gradient and occasional differing sector identity have not been fully classified at source RGB level. The next isolated review should compare the deflected-ray source RGB and source identity at the disappearing interior feature, then show the existing short clips to the owner at native display scale. A short high-cadence planet and dense-star pass, plus primary-right motion, would close the specific untested states. Do not add sources or alter the projection to perform that follow-up. No production acceptance or B1 GO is implied.

Validation performed: reviewed R20 native evidence first; checked R21 copied-bundle substitutions and hashes; ran four isolated WebView2 developer fixtures; paired raw native pixels by exact virtual pose; made and inspected the listed clips, full-context images and full-height join stills; compared R20 source traces and measured cost baseline; checked callback, overflow, error and initial target status. No installed screensaver, display settings, owner journey, production renderer/shader, R8-R20 evidence, existing dirty worktree, Git history or remote was changed.
