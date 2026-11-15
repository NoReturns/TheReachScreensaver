# R20 static source parity validation

Date: 2026-10-28. Mode: isolated developer experiment. Recommendation: **REVISE**. B1 remains NO-GO. No production renderer, shader, lens equation, installed screensaver, Windows display setting, or owner journey was changed.

## Answer

A full-resolution 2x-MSAA source added to the left context at renderer initialization reproduces R9's frozen +6.815-degree correction. The left-primary final-frame join falls from 59 to 3 rows above 20 encoded-luma units, and the exact failure row at virtual y=1034 falls from 47.269 to 1.928. The left and primary lens traces then choose the same +42.006-degree source and UV at that row. The extra source identity is selected as the next center from the global sector registry; candidate allocation does not use a literal +42.006-degree value. The copied bundle retains an unused R9 diagnostic assertion containing that literal.

The static choice held four left, three primary and two right targets through the 41-yaw sampled trace and the continuous three-cycle -12 to +12 degree motion. It eliminated the large positive-yaw branch mismatch from about +6.5 to +7.6 degrees, but left small branch mismatches around +5.75 to +6.4 degrees and the known negative-yaw mismatch. It also changed interior left pixels at other yaws, up to roughly 500 pixels inside the left monitor. Sampled crops show no obvious screen-fixed ghost; moving source-dependent detail and low-rate readback are not sufficient to certify absence of flicker. Static source parity is a credible developer candidate for the proven positive-yaw defect, but it is not a complete B1 source-coverage solution or an owner-accepted motion result.

In serial 60-second black-hole runs, process-attributed dedicated GPU memory medians were 4384 MiB control and 4717 MiB static, an approximate **333 MiB incremental cost**. One 3244x2675 2x target has a nominal resolved color/depth plus multisample model of about 298 MiB; the counter is process-level, not exact target residency. The left gained 14 scene draws per frozen frame. GPU utilization median rose from 53% to 67.5% in these adapter-wide samples; no isolated GPU frame time was measured. A separate matched full-resolution sparse-star 4x-versus-2x run saved about 1572 MiB process-attributed dedicated GPU memory, so the static target consumed roughly 21% of that measured sample-count saving in a cross-scene comparison. R15's moving-hole 4x-to-2x saving was 1591 MiB. These comparisons do not establish a production memory budget or sustained GPU throughput margin.

## Isolation and implementation boundary

The two repositories were already dirty and were preserved. The production source path remains `Universe/src/reach/CylindricalGeometry.ts` for `sectorCenters` and `displaySectorCenters`, `Universe/src/reach/CylindricalRenderer.ts` for target allocation and scene renders, and `Universe/src/reach/CylindricalLens.ts` for output and deflected-ray lookup. `Screensaver/Application/ReachDeveloperApplication.cs` is the existing isolated developer host. `Screensaver/BUILD_FROM_SOURCE.md` and both repository-local `AGENTS.md` files were read before the experiment. No source file in Universe or host application source was edited for R20.

R20 preserves R13's focal, cylindrical output-ray projection, composition, object scale and full source raster dimensions. It does **not** preserve every lens pixel: changing a deflected ray from cube to sector sampling intentionally changes some interior imagery. Whether that appearance is acceptable is part of the unresolved visual gate.

[`r20_prepare.py`](r20_prepare.py) copied the preserved R15 full-resolution 2x and 4x fixture bundles into [`r20-variants`](r20-variants/manifest.json). The only candidate renderer change is in its copied developer bundle: after normal `displaySectorCenters` selection, on the recorded 8320x2160 layout and its left viewport, choose `all[all.indexOf(localLast)+1]` from the same global center array and append it before first target allocation. The bundle also neutralizes the legacy R9 toggle for the static candidate, because the existing R10 host invokes it; both R10 phases must leave targets unchanged. The 2x source dimensions, MSAA count, sector camera, cylindrical focal, output rays, accepted lens mathematics, exposure, native bloom, 512-pixel guard, post crop, scene state and physical output buffers are unchanged. The 4x and 2x motion fixtures use the same smooth yaw envelope; only sector sample count differs. The candidate is test-layout-gated and is **not** a proposed production patch.

[`r20_run.ps1`](r20_run.ps1) staged a fresh GUID-named run root, copied the existing developer host and one fixture payload, and launched the host against its own `dist`, WebView2 `profile`, `temp`, `logs`, `captures` and `metrics`. The fixture's synthetic scene does not read the owner voyage. All runs were serial. Two initial sandboxed launches failed at WebView2 controller creation with `E_UNEXPECTED`, before rendering: [first](r20-runs/run-688a320dee1a45948a75fc098782f7f7), [second](r20-runs/run-0d4f4791b1b341b092ccdf32fc7731e8). Their logs were retained. Subsequent runs succeeded outside the filesystem sandbox with separate disposable roots. No failed run is included in the measurements.

### Raw evidence index

| Purpose | Control | Static or 2x comparison |
| --- | --- | --- |
| Frozen +6.815 native final PNGs and R13 draw metrics | [2x control](r20-runs/run-089cfc54137e4f26afd4e075033f26ee) | [2x static](r20-runs/run-2475f2871bd147249880eff63c3d5892) |
| 41-yaw native final PNG and lens-source traces, with derived analysis in each `metrics` folder | [2x control sweep](r20-runs/run-ffd989e6bee24dc7ba8cd1f0d3b46a5f) | [2x static sweep](r20-runs/run-5b106ef9b7d44c57ac6963d801e63d43) |
| Frozen half-float lens buffers and R9-style parity analysis | [2x control lens](r20-runs/run-82e70d6c700843c2b413fd742e8cbe2e) | [2x static lens](r20-runs/run-fc609f7b85954b27bd6501c0d40d9d3d) |
| Smooth black-hole motion, 59 native crops per left and primary display, GPU counters | [2x control motion](r20-runs/run-dc261c73bde74b2bab75c614380b92ac) | [2x static motion](r20-runs/run-653cb5a9992246aeb340a6011689d181) |
| Static small-star final PNGs | [2x control stars](r20-runs/run-bd16bb7652c646c3876662c413aab40f) | [2x static stars](r20-runs/run-81065718dec94354850ca595c6f5ec85) |
| Static peripheral-planet final PNGs | [2x control planet](r20-runs/run-e1fa7426000a4ae8b00f03f0033e9599) | [2x static planet](r20-runs/run-03ed1caa097142f39fc39a6b56af581e) |
| Full-resolution 60-second sparse-star 4x/2x motion and GPU counters | [4x stars](r20-runs/run-9e8badc3792f4352a6de8c48d8b4a2bc) | [2x stars](r20-runs/run-84436985fb93489da7e00d4ce36f4d71); [static 2x stars](r20-runs/run-d65eab9052314c26b5757e8dab4f6583) |

The fixture JSON in each run's `metrics` folder names and hashes every complete native monitor PNG. The sweep manifest names each per-yaw PNG and source trace. The [direct paired sweep summary](r20-runs/paired-sweep-summary.json) gives the left and primary changed-pixel counts and bounding boxes for all 41 paired 2x states. Representative native-pixel crop views are [black-hole yaw 0](r20-runs/hole-control-vs-static-second10.png), [black-hole near +7 degrees](r20-runs/hole-control-vs-static-second02.png), and [sparse star at second 30, enlarged by nearest neighbor](r20-runs/stars-4x-vs-2x-second30.png). These images are aids; the original PNGs and `.rgba` crops are the lossless evidence.

## Frozen +6.815 result and source trace

The matched fixture state keys are equal. The observed normalized physical rectangles are left `(0,0,3840,2160)`, Windows primary `(3840,720,2560,1440)` and right `(6400,1080,1920,1080)`. The cylindrical focal is 2316.067 physical pixels, with the R13 composition shift of 1216. All sources are 3244x2675. Initial fixture status reports control target counts `3/3/2` and static `4/3/2` for left/primary/right; the static left centers are -107.994, -57.994, -7.994 and +42.006 degrees, while the control left ends at -7.994. The target count difference is present in the **first captured frame**, before any yaw sweep.

| Final encoded join at +6.815 | 2x control | 2x static |
| --- | ---: | ---: |
| Left-primary mean adjacent luma | 2.917 | 0.967 |
| Left-primary rows above 20 / 1440 | 59 | 3 |
| Left-primary peak | 59.269 | 38.132 |
| Left-primary y=1034 | 47.269 | 1.928 |
| Primary-right rows above 20 / 1080 | 0 | 0 |

At y=1034, the control left trace chooses its -7.994-degree center but reports invalid-angle branch 1, hence cube fallback. Primary chooses +42.006 degrees with valid branch 0 and UV y about 0.359. The static left chooses the same +42.006 center and the same UV y as primary; the local source index differs only because the left source list has one extra entry. In the frozen complete frames, 3399 left pixels changed by more than one RGB channel level, all within x=3806..3839 and y=949..1113; peak channel difference is 63. The primary final PNG is byte-identical, and the right differs by at most one channel level from repeated-render noise. Source-representation mismatch at the original join is directly corrected. The three remaining high-step rows are not proven to be a source-path defect.

The separate half-float lens-buffer capture confirms the correction in actual sampled color. At the same virtual pixel `(3839,1034)`, control left lens RGB is `(0.316162,0.292725,0.252686)`, while primary is `(0.002310,0.002190,0.001950)`. Static left equals that primary RGB exactly at half-float precision. Across a matched 40x100 same-coordinate strip, x=3800..3839 and y=1000..1099, the mean per-pixel maximum linear-RGB difference between left and primary falls from 0.038075 control to 0.00000212 static. Pixels with any RGB difference above 0.01 fall from 2701/4000 to 0/4000. The static peak difference is 0.001343, consistent with small repeat-render variation. This strip is a bounded lens-parity result, not a full-frame or all-yaw RGB proof.

R13's static draw measurement reported primary 142, left 142, right 128 calls in control, versus 142, **156**, 128 in static. Each value was stable through the 48 measured frames per window, and no WebGL error or context loss was reported. One additional scene render per left window explains the 14 extra draws in this scene; it does not imply 14 draws for every possible scene.

## Motion, interior changes, and other scenes

The sampled R10 trace includes 41 yaws from -12 to +12 degrees, with 0.1-degree sampling around the positive join transition. The static candidate's two legacy host phases are identical by construction: the old toggle is a no-op. In all 82 candidate records, the source identities and target counts stay `4/3/2`; no target enters or leaves. The paired phases have zero >1-level changed pixels and zero new boundary rows. This verifies the fixture's static allocation behavior, rather than a dynamic policy. The independently launched 2x control's ordinary phase provides the matched baseline. Selected join results:

| Yaw deg | Control rows >20 / branch mismatch rows | Static rows >20 / branch mismatch rows |
| ---: | ---: | ---: |
| -8 | 1 / 52 | 1 / 52 |
| -6 | 60 / 35 | 60 / 35 |
| 0 | 0 / 0 | 0 / 0 |
| +5.75 | 43 / 7 | 43 / 5 |
| +6.0 | 11 / 56 | 10 / 5 |
| +6.4 | 24 / 167 | 5 / 2 |
| +6.5 | 19 / 179 | 2 / 0 |
| +6.7 | 56 / 177 | 0 / 0 |
| +6.815 | 59 / 167 | 3 / 0 |
| +7.0 | 18 / 146 | 2 / 0 |
| +7.2 | 3 / 122 | 4 / 0 |
| +7.6 | 0 / 48 | 0 / 0 |
| +12 | 3 / 0 | 3 / 0 |

The remaining negative-yaw mismatch is consistent with R11's finding that the large -6 caustic is an intrinsic adjacent-ray gradient and was not corrected by adding a -107.994 primary source. No such source was added here. The small positive residuals around +5.75 to +6.4 mean this one extra left source does not supply complete source parity throughout motion. The trace classifies branch and sector identity; it is not a full RGB equality proof at every pixel.

The direct control-versus-static full-frame sweep shows nontrivial **interior** changes: at -12, 3722 left pixels over one level changed within x=3065..3587; at -6, 5019 within x=3306..3830; at 0, 6280 within x=3542..3634; at +5.75, 7639 within x=3764..3839. At +6.815, the difference is the smaller join region described above. Primary had zero pixels over one level changed at every sampled yaw, with at most one channel level of repeat variation. These moving footprints do not show a fixed vertical screen blend, but they show the static source is sampled far inside the left frame at some lens poses. The extra source replaces cube/other-source samples, so a material image difference is expected; its perceptual quality requires owner review.

The R17-style continuous developer loop ran 60 seconds with `yaw = 12 sin(2 pi t / 20000)` degrees, three smooth cycles spanning -12 to +12. It retained 59 once-per-second native RGBA crops on left and primary for both control and static runs, plus all callback intervals. The candidate/control crops used identical synthetic state and exact virtual-second yaw at every paired sample. In 38 of 59 left samples, some pixels differed by more than eight channel levels; the largest changed patch had 2865 pixels and peak channel difference 91. The changed x range moved with yaw (for example x=26..93 at yaw 0 versus x=300..319 near +7 degrees in the 320-pixel left seam crop). Visual inspection of matched native crop pairs at yaw 0, near +7 and negative yaw found no obvious screen-fixed ghost. The 1 Hz stored samples cannot rule out short-lived temporal flicker, while R10's dense stepped trace is not itself continuous video. The R17 capture crops have different absolute vertical positions on left and primary, so they cannot independently measure a physical join; the R10 full-frame captures do that.

The static 2x and control 2x frozen small-star and planet PNGs are byte-identical on all three monitors. Their primary-right joins have zero rows above 20; the star left-primary join has zero and the planet join has zero. In the 59-sample matched small-star motion, 52/59 left and 43/59 primary crop hashes match exactly; all other paired frames differ by at most two channel levels, with one single channel value over one level on left and none on primary. No star sampling or bloom change attributable to the static sector was seen in this sparse trajectory. Planet **motion** was not captured. Native bloom and the 512 guard were active in all final output, but there was no separate bloom-intermediate parity trace. No owner visual acceptance is claimed.

The bounded 4x-versus-2x full-resolution sparse-star run used identical state and 59 exact-yaw native samples per captured display. Across all 59 left crops, no active control pixel differed by more than eight RGB channel levels; peak channel difference was six. In a fixed 45x25 tracked-star patch, median summed RGB was 4630 at 4x and 4646 at 2x, and median pixels with mean RGB above 10 were 17 versus 18. The nearest-neighbor second-30 view shows no obvious broadening at that one star. This is a useful bounded check of R15's outstanding small-star uncertainty, **not** a dense-field, faster-shimmer or owner acceptance test.

## Measured resources and pacing

The continuous runs used isolated host/WebView2 PID sets from their `r17-*.json` reports. Windows `GPU Process Memory(*)\\Dedicated Usage` samples were summed only for those PIDs over virtual seconds 10..59; 22 usable samples per run. NVIDIA utilization is adapter-wide. These are process-attributed usage observations, not a target allocation trace or proof of exact residency. The process counter's Windows caveat and the R14/R15 attribution limits still apply.

| 60-second case | PID dedicated median MiB | Adapter GPU median % | Left / primary / right callbacks | Left / primary / right p95 callback interval ms | Errors / context losses |
| --- | ---: | ---: | --- | --- | --- |
| 2x black-hole control | 4384 | 53 | 3496 / 3584 / 3712 | 22.2 / 16.8 / 16.8 | 0 / 0 |
| 2x black-hole static | 4717 | 67.5 | 4170 / 4184 / 4332 | 16.8 / 16.8 / 22.2 | 0 / 0 |
| 4x sparse stars | 5953 | 40 | 3408 / 4389 / 3732 | 22.3 / 22.2 / 27.8 | 0 / 0 |
| 2x sparse stars | 4381 | 35 | 3418 / 3761 / 3716 | 22.2 / 27.8 / 33.3 | 0 / 0 |
| 2x sparse stars, static | 4707 | 37.5 | 3452 / 3673 / 3738 | 22.2 / 27.8 / 27.9 | 0 / 0 |

The black-hole static-versus-control increment is 333 MiB; the sparse-star static-versus-control increment is 326 MiB. Both are near the approximately 298 MiB nominal 2x extra-target model, with driver/resolve and counter variation. The sparse-star full-resolution 4x-to-2x difference is 1572 MiB. R15 measured 1591 MiB for a 4x-to-2x moving-hole comparison. Pacing was browser callback scheduling, not isolated GPU frame or scanout time. The static black-hole run had *more* callbacks despite higher measured adapter utilization, which demonstrates why callback count cannot be used as a GPU-work speedup claim. No WebGL errors or context losses were reported in the successful runs.

## Layout-derived policy model and hard gates

The proposed rule is to build one global sector registry from the accepted `sectorCenters(W,f,shift)` for the whole display layout, then derive each display's padded direct-output set using `displaySectorCenters`. For each horizontal physical join, inspect the neighboring windows' registry IDs. If the right neighbor owns a center immediately beyond the left window's rightmost local ID, the left may reserve that same global ID **statically at initialization** for a developer source-parity test. The current fixture meets that condition and imports +42.006 from primary into left. The rule does not import a left-neighbor ID into primary, consistent with R11's rejected -107.994 correction. This is a directional candidate, not a proof that any neighboring ID is needed by lens-deflected rays. A production policy would additionally need a conservative deflected-ray coverage envelope and same-representation evidence for both sides of every join, including vertical joins and wrap. It must never infer necessity from a momentary join failure or allocate on yaw.

The model below uses the **existing** virtual-height focal `f=Hvirtual/(2 tan 25 deg)`, 50-degree horizontal center pitch, 35-degree half-width, full-density source dimensions and the 512-pixel guard. It does not apply R18's primary-height or fixed-reference focal. The dual mixed case is an illustrative 3840x2160 left display and 2560x1440 Windows primary to its right at `(3840,720)`; it is not a measured dual setup. The fourth-above case uses R18's hypothetical 3840x2160 monitor directly above the left. `local` lists global registry IDs in display order, and `static` applies only the simple right-neighbor import rule where applicable.

| Layout; virtual bounds | f px; global IDs | Local IDs control; static | Source target px; nominal 2x sectors control/static MiB | Direct scene passes control/static |
| --- | --- | --- | --- | --- |
| Single 4K; 3840x2160 | 2316; 2 | `[0,1]`; same | 3244x2675; 596 / 596 | 2 / 2 |
| Dual mixed; 6400x2160 | 2316; 3 | left `[0,1,2]`, primary `[1,2]`; same | 3244x2675; 1490 / 1490 | 5 / 5 |
| Observed three; 8320x2160 | 2316; 4 | left `[0,1,2]` to `[0,1,2,3]`, primary `[1,2,3]`, right `[2,3]` | 3244x2675; 2383 / 2681 | 8 / 9 |
| Fourth above; 8320x4320 | 4632; 2 | upper-left `[0,1]`, lower-left `[0,1]`, primary `[0,1]`, right `[1]`; same | 6487x5349; 8339 / 8339 | 7 / 7 |

With a black hole, add six cube-face scene renders **per context** to the direct-pass counts: single 8, dual 17, three 26/27, fourth 31. The fourth-above physical output adds 59% pixels over the observed three displays, yet nominal sector storage rises by about 211% relative to the three-display static variant. Existing focal policy also doubles object scale/changes composition when virtual height doubles; R18 already identified that architecture problem. No R20 fourth-window rendering was performed.

Explicit conservative R20 developer gates: require WebGL2, `EXT_color_buffer_float`, working 2x sector MSAA, target dimensions no greater than the observed full-density 3244x2675, at most eight global and eight local sampler IDs, no more than nine total sector instances, no more than approximately 2.7 GiB nominal 2x sector storage, and a warmed process-attributed dedicated median below 5 GiB on this measured owner setup. The target, instance and nominal limits preserve the largest **tested** R20 configuration; they are research bounds, not universal hardware limits or production acceptance thresholds. Reject a layout that exceeds a gate, rather than shrink targets, change object scale, silently fall back to a mismatched cube, or skip stars. Single and modeled dual fit the source/resource model but remain visually untested in R20. The observed three fits and has the measured results above. Fourth-above fails target dimension and nominal storage limits; it is unsupported by this policy. Other layouts and GPUs require their own model and runtime capability checks.

## Decision and remaining work

**REVISE.** The targeted positive-yaw source mismatch is corrected at 2x with one stable, global-identity sector and an approximately 333 MiB measured process-memory cost. The small-star 2x sample check is favorable. This is not a GO for B1 because positive-yaw residual branch mismatches remain, negative-yaw source differences and intrinsic caustic remain, interior lens imagery changes substantially at some poses, the 60-second crops sample only once per second, planet motion and owner visual review are missing, and the simple neighboring-sector rule is not a general lens coverage proof. The additional adapter utilization and 14 draws need a measured GPU-work budget before a wider implementation candidate.

The smallest next developer proof would use an explicit globally shared source-ID map and a conservative lens-ray envelope for the current layout, then capture synchronized full native join strips and source RGB in continuous motion at higher cadence. It should inspect the interior moving difference at negative and zero yaw, slow planet motion, dense stars and native bloom, and obtain owner review of the complete motion before any production proposal. A different layout should be modeled and simulated within the resource gates; the fourth-above layout requires a separate architecture.

Validation performed: copied-bundle anchor uniqueness checks and hashes; syntax checks; matched static captures for black hole, planet and stars; 41-yaw control and candidate native final-frame/source-trace sweeps; half-float lens-buffer parity capture; 60-second continuous black-hole control/static motion; 60-second full-resolution sparse-star 4x, 2x and static-2x motion; source identity and target-count checks; direct join and interior pixel comparisons; visual inspection of representative native crops; PID-attributed GPU counters; R13 draw instrumentation; error and context-loss checks; report whitespace check. No production source change, display change, journey access, installation, cleanup, deletion, Git write, or remote operation occurred. All R8-R19 evidence and existing dirty worktrees remain.
