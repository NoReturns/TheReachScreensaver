# R13 rectilinear versus cylindrical renderer comparison

Date: 2026-10-26. Decision: **B - continue cylindrical research with strict budgets**, while keeping the accepted rectilinear panorama as the installed production renderer. R13 does not authorize implementation or promotion. B1 remains NO-GO.

## Answer

On the owner's 8320x2160 three-monitor layout, cylindrical rendering does provide a meaningful visual capability: it maps horizontal angle by a constant pixel scale, so objects and the black-hole disc do not expand dramatically as they move into the wide left display. In these matched synthetic captures, the rectilinear black-hole disc and lens occupy much more of the left monitor and have a large brightness/geometry step at the left-primary join. Cylindrical keeps the silhouette and disc closer to one coherent shape across that join. This is a projection benefit, not proof that every cylindrical pixel is more accurate or attractive. The different apparent size is inherent to the projection and must not be scored by raw cross-projection pixel differences.

The cost is material: the warmed black-hole fixture issued **87 draw calls across three rectilinear windows versus 412 cylindrical**, with 8 cylindrical perspective targets of 3244x2675, 18 cube-face scene passes when lensing is enabled, and replicated bloom/compositing. Short CPU submission samples rose from roughly 0.2 ms per window to 0.5-0.6 ms median, but their p95 and `gl.finish` readings are noisy and do not establish GPU frame time or sustained FPS. WebView2 process working-set snapshots were about 660 versus 783 MiB for the black-hole fixture; they are process RAM, **not GPU residency**. Actual GPU memory could not be measured.

## Isolation and exact evidence

The existing native `cylindrical-stage-a1` synthetic fixture was used unchanged with the R12 developer bundle. It does not read a voyage or `localStorage` (`Universe/development/cylindrical-stage-a1/view.js:9-13`). Each run used a new sandbox root and WebView2 profile. The host checked physical canvas dimensions, one shared scene-state key across three windows, projection selection and rendering readiness (`Screensaver/Application/ReachDeveloperApplication.cs:979-1034`). The same state key SHA-256 is recorded for both projections in every paired case in [capture_manifest.json](r13-runs/montage/capture_manifest.json). The manifest gives every original monitor PNG path and SHA-256, display rectangle, run root, fixture manifest and montage path. Complete original PNGs remain under each run's `captures/` directory. No capture was replaced or deleted.

The observed Windows rectangles were left `(-3840,-720,3840,2160)`, primary `(0,0,2560,1440)`, right `(2560,360,1920,1080)`. The normalized virtual canvas is 8320x2160 with rectangles `(0,0,3840,2160)`, `(3840,720,2560,1440)` and `(6400,1080,1920,1080)`. DPI values were 144, 96 and 96. Each output drawing buffer matched its native pixel rectangle. The fixture fixes camera at `(0,0,0)`, composition shift 1216 pixels, material time 1234, black-hole time 0, star and hole seeds, synthetic bodies and galaxy (`Universe/development/cylindrical-stage-a1/view.js:145-200`). Both modes used final output, native bloom, the same exposure/tone mapping configuration in `Engine`, the same physical resolution and quality settings. Cylindrical used the accepted 512-pixel guard; rectilinear has no equivalent guard. These are **synthetic scenes**, not a live voyage or an installed-payload run.

| Case | Fixture state | Rectilinear run | Cylindrical run | Paired montage |
| --- | --- | --- | --- | --- |
| Black hole yaw 0 | `hole-26-left`, yaw 0 | `run-c45ebf68e80a435e8981f933e2e2176e` | `run-95e0d4a351b14e6abab1363902c0370f` | [yaw 0](r13-runs/montage/R13_hole-yaw0.png) |
| Black hole yaw -6 | `hole-26-left`, yaw -6 | `run-c093fe40e1e240e087bde10d486c697d` | `run-7a18ce8fc7494a4494e0b3e3246f6f7d` | [yaw -6](r13-runs/montage/R13_hole-yaw-6.png) |
| Black hole yaw +6.815 | `hole-26-left`, yaw +6.815 | `run-284fd7d85a8f4c7f96c7bc38b9ac3e7b` | `run-c0c49671c82646eda5d2eddd4f8dbd5f` | [yaw +6.815](r13-runs/montage/R13_hole-yaw-plus.png) |
| Wider peripheral view | `galaxy-sector`, yaw +23 | `run-71601815bc834cdd99a447f49926bd9b` | `run-054f9f7e762243e88b19295ec389522d` | [peripheral](r13-runs/montage/R13_peripheral.png) |
| Deep-space scene, no lens | `galaxy-left-seam`, yaw -78 | `run-dd9838b74c424624a8fe46104b41638c` | `run-b72f62fa981842f78682684176c5cd7e` | [starfield](r13-runs/montage/R13_starfield.png) |

The [owner-review montage](r13-runs/montage/R13_OWNER_REVIEW_MONTAGE.png) combines all five pairs. Each case shows both full virtual-layout presentations, including black gaps where there is no monitor, plus equal-coordinate crops around left-primary x=3840 and primary-right x=6400. Full presentations are downscaled for the sheet; crop panels use nearest-neighbor enlargement. The original monitor PNGs in [capture_manifest.json](r13-runs/montage/capture_manifest.json) are the lossless review source.

## Visual inspection

| Case | Rectilinear observation | Cylindrical observation | Interpretation |
| --- | --- | --- | --- |
| Yaw 0 black hole | Left peripheral black-hole disc and accretion region expand strongly, with an obvious left-primary shape/brightness break in the full layout. | Disc and lens remain more compact; the silhouette traverses the left-primary join with much less apparent scale jump. | Inherent rectilinear peripheral stretching plus a real physical-join presentation problem. Cylindrical's mapping is visibly useful here. |
| Yaw -6 black hole | The large left image and join break persist. | Compact disc and better overall shape, but a sharp caustic sits at the join in the crop. | The R11 steep adjacent-ray caustic remains; it is not cured by the projection choice. |
| Yaw +6.815 black hole | Peripheral disc remains greatly enlarged and the left-primary join cuts through high-contrast material. | Better global shape, but the known B1 sector-versus-cube join mismatch remains visible in the final crop. | R8/R9 defect; the cylindrical renderer was unchanged for this test. |
| Peripheral view | A bright object near the far-left field is stretched into an ellipse; the central Earth/planet group appears larger than in cylindrical. | Far-left object is much closer to circular and the overall field feels less pulled sideways. | Main non-lens benefit of cylindrical mapping. Differences in apparent object size are projection differences, not defects by themselves. |
| Deep-space, no lens | Very dark starfield with sparse visible stars and a small planet; no strong physical join feature in the measured strip. | Different object positions and apparent sizes, with sparse stars; no obvious bright join line in this capture. | This fixture is too sparse and dark to establish starfield superiority. It does show that cylindrical's extra source passes are incurred even without a black hole. |

The full-layout montage makes geometric differences clear. It is less sensitive to faint stars and small bloom residuals, so original full-resolution captures should be used for owner review. This frozen set does not prove how either projection feels during continuous voyage motion, nor does it establish a universal visual preference. No new screen-fixed blend/ghosting policy was enabled. R12's rejected ghosting artifact is therefore absent by design; the current cylindrical B1 representation mismatch and native bloom residual remain possible.

### Within-projection physical-join check

The table compares **adjacent final encoded pixels within each renderer**, never rectilinear pixels against cylindrical pixels. Rec. 709 encoded-luma difference is measured between x=3839 and x=3840 over y=720..2159. The full [analysis.json](r13-runs/montage/analysis.json) also records primary-right x=6399/6400 over y=1080..2159. A high adjacent step alone does not prove a cross-context defect; an intrinsic caustic or a steep object boundary can land exactly on a join. Source-parity evidence is needed for attribution.

| Black-hole yaw | Rectilinear mean / rows above 20 | Cylindrical mean / rows above 20 | Cylindrical interpretation |
| ---: | ---: | ---: | --- |
| 0 | 14.38 / 240 | 1.03 / 0 | Large rectilinear join difference is reduced in this state. |
| -6 | 16.61 / 276 | 3.48 / 61 | R11 traced the cylindrical peak to intrinsic adjacent-ray caustic variation. |
| +6.815 | 20.87 / 654 | 2.92 / 59 | R8/R9 traced a subset of cylindrical failure to inconsistent source representation. |

For both non-lens cases the left-primary mean was 0.00 rectilinear; cylindrical was 0.00 peripheral and 0.18 starfield, with no rows above 20. The measured primary-right join had no rows above 20 in any of these five static cases. This does not prove all rows or motion states are clean. The cylindrical +6.815 measurements reproduce the pre-R9 order of magnitude, since no additional sector was installed. R9's extra target improved the frozen join but cost 99.31 MiB nominal color/depth and 14 draws; R10 did not establish a safe dynamic selector (`Screensaver/development/STAGE_B1_R9_FULL_FRAME_SECTOR_PARITY_PILOT.md:30-82`, `STAGE_B1_R10_MOTION_SECTOR_COVERAGE.md:149-171`). R11's caustic and R12's rejected blend are distinct failure modes (`STAGE_B1_R11_NEGATIVE_YAW_ROOT_CAUSE.md:35-84`, `STAGE_B1_R12_ZERO_TARGET_FEASIBILITY.md:45-83`).

## Performance and resource evidence

Four additional isolated runs measured yaw-zero black hole and no-lens starfield in both modes. They used the same R12 fixture payload and a newly published **developer-only** host. After capture, each monitor rendered 8 warmups and 48 timed static frames. Developer instrumentation calls `window.__a1RenderAgain()`, wraps WebGL draw entry points to count calls, and calls `gl.finish()` after each render (`Screensaver/Application/ReachDeveloperApplication.cs`, `MeasureR13`). The three windows were sampled sequentially, not as a simultaneous 60-Hz animation. Raw arrays, context-loss flags and process snapshots are in the `r13-*.json` report under each measurement run's `metrics/`; their exact paths are included in [analysis.json](r13-runs/montage/analysis.json).

| State / mode | Draws primary, left, right; total | CPU submission median ms primary, left, right | CPU submission p95 ms primary, left, right | Host + WebView2 working-set snapshot |
| --- | --- | --- | --- | ---: |
| Black hole / rectilinear | 29, 29, 29; 87 | 0.20, 0.20, 0.20 | 0.67, 0.50, 0.50 | 660 MiB |
| Black hole / cylindrical | 142, 142, 128; 412 | 0.50, 0.50, 0.60 | 1.40, 10.81, 6.17 | 783 MiB |
| No lens / rectilinear | 28, 28, 28; 84 | 0.20, 0.20, 0.20 | 0.40, 0.40, 0.37 | 641 MiB |
| No lens / cylindrical | 58, 58, 44; 160 | 0.30, 0.30, 0.20 | 0.60, 0.73, 0.50 | 744 MiB |

The cylinder uses 3/3/2 perspective targets in the existing layout, each 3244x2675 RGBA16F with depth and up to four samples; rectilinear uses no perspective sector targets. Cylindrical black-hole frames submit 8 sector scene renders plus 18 cube-face renders, versus one rectilinear scene pass per window. Without the lens, cylinder still submits 8 sector scene renders, versus three rectilinear scene passes total (`Universe/src/reach/CylindricalRenderer.ts:210-269`; `Universe/src/core/Engine.ts:169-176`). The source-derived **principal** sector/cube/composer estimate for the three-display 512-guard cylinder is 1158 MiB, and A2-R4 estimated 1299 MiB including bloom; those are allocated-payload calculations, **not measured GPU residency** (`Screensaver/development/STAGE_A2_R4_RESULTS.md:138-158`). Rectilinear composer and bloom target bytes were not measured in this R13 run, so no exact GPU-memory ratio is claimed.

The CPU values have coarse timer resolution and are sensitive to WebView2 scheduling, draw-wrapper overhead and run order. The `gl.finish()` completed-render samples were nearly identical to CPU submission values; they did **not** yield a credible isolated GPU time. The large black-hole cylindrical p95 outliers reinforce that short sequential sampling is insufficient for pacing claims. FPS, sustained p95 frame interval, native CPU utilization, GPU frame time and **actual GPU memory/residency are unavailable** under this fixture. No context loss or WebGL error was reported in the measured runs. WebView2 private bytes are virtual committed/reserved process memory and are not GPU residency; their sums are intentionally not used as a graphics-memory estimate. Working-set snapshots are one-time process RAM observations, not steady-state peaks.

## Layout and production implications

| Criterion | Rectilinear | Cylindrical developer renderer |
| --- | --- | --- |
| Visual quality | Accepted production look; visibly stretched periphery in these wide-layout fixtures. | Better peripheral object shape and black-hole composition in these fixtures; current B1 artifacts remain. |
| Physical joins | Significant black-hole left-primary step in this fixture; little visible starfield join here. | Much smaller yaw-zero step, but R8/R9 positive-yaw mismatch and R11 caustic remain. |
| Peripheral distortion | Gnomonic/rectilinear stretch grows away from the focal view. | Angular horizontal mapping limits that stretch, with a different accepted projection aesthetic. |
| Black-hole presentation | Enlarged left peripheral disc; existing lens uses window projection. | More coherent panoramic disc shape; expensive cube/sector rendering and known B1 defects. |
| Starfield presentation | Sparse fixture gives no clear quality disadvantage. | Sparse fixture gives no clear quality win; source-sector work remains. |
| GPU resource efficiency | Fewer measured draws and no sector targets. | More draws and large target-payload estimates; actual residency unknown. |
| Multi-monitor scalability | One WebView2 context per monitor; local render targets. | Same contexts plus selected sectors, per-context cube and padded bloom targets. |
| Layout independence | Host coordinate contract supports mixed/negative rectangles, but projection appearance varies with virtual extent. | Geometry accepts rectangles, yet focal and target size scale with virtual bounding height; arbitrary layouts not visually accepted. |
| Production readiness | Currently installed/accepted direction. | Developer-gated and B1 NO-GO. |
| Maintenance complexity | Existing production renderer and host contract. | Additional source-selection, cube fallback, lens shader adaptation, sector/LOD and bloom-grid behavior. |

Single monitor: cylindrical could reduce edge stretch within a wide panel, but there is no cross-window join benefit and the audit modeled two large perspective targets at 3840x2160. A single-monitor performance regression has not been tested here. Dual monitor: cylindrical's benefit grows with horizontal extent, while the virtual-canvas audit modeled five sector instances across a 6400x2160 layout and retained two independent contexts. Existing three monitors: this R13 run verifies the visual gain and 8-target cost for the recorded rectangles. Fourth monitor above: **geometry model only**; no fourth window or Windows setting was changed. The audit assumed a 3840x2160 monitor directly above the left display, producing an 8320x4320 bounding extent. Under current cylindrical formulas, source targets become 6487x5349 and the model selected seven target instances, about 3.37 GiB principal nominal target payload before bloom, multisample, assets and driver overhead (`Screensaver/development/VIRTUAL_CANVAS_ARCHITECTURE_FEASIBILITY_AUDIT.md:52-70`). Rectilinear also gains a WebView2 context and local postprocessing work, but does not allocate cylindrical perspective sectors; no fourth-monitor runtime comparison exists.

## Recommendation and decision boundary

Choose **B: continue cylindrical research with strict budgets**, limited to isolated developer fixtures. The R13 captures show a real and visible wide-panorama capability that would be lost by abandoning cylindrical now: substantially less peripheral stretching and a more coherent black-hole disc/silhouette across the left-primary field. A rectilinear-only direction would retain the established production path, lower measured draw count, and simpler maintenance, but would keep the pronounced wide-field deformation in these fixtures. Prior development effort is not a reason for this choice; the paired imagery is.

Further cylindrical work should be contingent on explicit measured GPU residency and sustained frame-time budgets, not on the nominal target estimate or this noisy timing sample. It must also solve R8/R9 source parity without R10 popping or R12 ghosting, classify R11 caustic correctly, and preserve native bloom appearance. A different bounded projection architecture (option C) becomes preferable if source consistency cannot be achieved within those budgets, especially for vertical layouts. No renderer switch, shader/lens edit, production promotion, display change or owner-state operation is authorized by R13. Owner visual review of the original PNGs and montage remains outstanding.

## Validation and files changed

Performed: ten successful isolated visual fixture launches (five paired cases), four additional warmed performance launches, matched-state and PNG-hash checks, full-layout and join-crop montage generation, within-projection join analysis, read-only source review, and developer host `dotnet publish` (passed with existing NuGet vulnerability-index and WindowsBase/high-DPI warnings). A first sandboxed WebView2 controller launch failed with `E_UNEXPECTED`; its run and log were retained at `development/r13-runs/run-891c1aa640c6496984c48a7726d3f9dc`. Subsequent isolated launches completed through the permitted unsandboxed path. No full suite was run.

Files changed for this task: `Application/ReachDeveloperApplication.cs` (developer-only `--reach-dev-r13-measure` instrumentation), `development/r13_montage.py`, `development/r13_analyze.py`, this report, and generated evidence under `development/r13-runs/` plus `development/r13-publish/`. No Universe source or production renderer/shader was edited. Existing dirty work in both repositories was preserved. Remaining uncertainty: owner visual preference, sustained motion and FPS, actual GPU residency, true GPU time, fourth-monitor runtime behavior, and whether a bounded source policy can close B1.
