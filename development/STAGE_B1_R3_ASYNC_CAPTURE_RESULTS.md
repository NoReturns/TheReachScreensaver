# Stage B1-R3 asynchronous continuous capture pilot

Recommendation: GO for controlled full B1 motion evidence collection on the
current three-display development layout, with the limitations below. This is
an instrumentation decision. B1 motion acceptance remains NO-GO; no claim of
flicker absence or compositor presentation synchronization is made. B2 was
not run.

## Preflight and source boundary

Read `STAGE_B1_R2_INSTRUMENTED_PILOT.md` and its baseline, synchronous
capture, stepped, and preview evidence before editing. Screensaver remained
on `main` at `4596b719f10fa39565f7cd34094b305d128825a2`; Universe
remained on `reach-v1-release` at
`76ab9d49c3cb6ae5d6b03f07073387b8708a47d7`. Both had the same dirty
status described in R2. All existing tracked and untracked work was kept.

The only implementation edits were to
`../Universe/development/cylindrical-stage-a1/view.js`, the developer-only
`Application/ReachDeveloperApplication.cs`, and the disposable launcher
`development/run_stage_b1_r2.ps1`. New verification scripts are
`development/verify_b1_r3.py` and `development/compare_b1_r3_pose.py`.
The accepted cylindrical renderer, 512-physical-pixel guard, shaders, bloom
passes, and production display behavior were not changed. The synchronous
continuous and stepped controls remain selectable.

## WebGL2 audit and implementation

Runtime capability checks on the developer fixture confirmed WebGL2,
`PIXEL_PACK_BUFFER` transfer through `getBufferSubData`, and
`fenceSync`/`clientWaitSync` in all final async runs. The fixture validates
that Three.js has no render target bound, the WebGL draw and read framebuffer
bindings are the default framebuffer, and the drawing buffer equals the
physical display size. It then issues `readPixels` into a pixel-pack buffer
immediately after the final display render. The pixel command and fence
retain the rendered frame's ID, timestamp, yaw, and physical crop. No bulk
CPU pixel transfer or GPU fence wait occurs in the render callback.

The capture crop remains the R2 final-output 192x550 RGBA8 region on each
side of the left/primary seam: left local x=3648..3839, y=800..1349;
primary local x=0..191, y=80..629. Pixel rows are bottom-up. The right
window reports callback cadence without a seam crop. The version-1 yaw
trajectory and frozen scene/shader clock are unchanged from R2.

The final implementation preallocates a fixed 256-buffer ring per captured
display, each buffer 422400 bytes. That is 103.13 MiB of pixel-pack storage
per display, or 206.25 MiB across the two capture displays, before driver
overhead. An exhausted ring records the dropped frame ID; no frame is
silently replaced. The host waits until the four-second trajectory has ended
before calling the drain hook. The hook polls the oldest fence with zero
timeout, transfers completed buffers, base64-encodes them, and returns each
frame to the host in order. The host writes run-local RGBA files and SHA-256
metadata. The ring is released after the last completed frame is drained.
The host's 25-second drain bound and existing group stop/failure path remain.

Two smaller-ring diagnostics were retained, not deleted. An eight-buffer
run (`run-342f824eb9ed4ba5b03f2c3a5eb6faf6`) reduced median callback
spacing but overflowed 131-132 frame IDs per captured display and left gaps
near +4 degrees. A 192-buffer run drained during motion
(`run-8aa780f14c9341e98baaef3549b10101`) had zero overflow but median
spacing of 22.2/27.7 ms. Fence polls and transfer were short; encoding and
host transfer during motion were the disturbance. Deferring drain removed
that competing work from the measured motion interval.

## Final bounded evidence

All four final runs used `development/b1-publish-r3b/` and
`development/b1-fixture-build-r3c/` staged into separate disposable roots.
The first and second numbers in each cell are primary and left displays.

| Mode | Run root | Frames | Callback p50 ms | Callback p95 ms | Max ms | Gaps over 50 ms | Peak ring use |
| --- | --- | --- | --- | --- | --- | --- | --- |
| Native baseline | `run-20d7866eb2284a67bd9ffce99ea9b93f` | 235 / 236 | 16.7 / 16.7 | 16.8 / 22.3 | 66.7 / 66.7 | 2 / 3 | 0 |
| Native async | `run-1973d14d08804e5689c046766436f623` | 233 / 227 | 16.7 / 16.7 | 22.2 / 27.8 | 105.5 / 100.0 | 3 / 3 | 233 / 227 of 256 |
| Rectilinear baseline | `run-d5d6962c5dd549a499a2ad09b7b94479` | 235 / 235 | 16.7 / 16.7 | 16.8 / 27.8 | 44.4 / 50.0 | 0 / 0 | 0 |
| Rectilinear async | `run-0bf70f2432184c9289fbeea4d16a653f` | 230 / 233 | 16.7 / 16.7 | 27.8 / 27.7 | 94.4 / 99.9 | 1 / 1 | 230 / 233 of 256 |

Each captured display wrote exactly one verified file per callback in the
final async runs. All four streams had zero overflow, zero capture errors,
strictly increasing frame IDs, ordered host writes, valid physical crops,
nonconstant pixels, and matching SHA-256 hashes. Both -6 and +4 crossings
had consecutive frame IDs and no callback gap above 50 ms within plus or
minus one degree. The final native streams retained 22/22 frames near -6
and 26/24 near +4; rectilinear retained 23/24 and 22/24. These counts
include both sweep directions. The right window completed and the host
stopped normally in every run. Rectilinear is a qualitative projection
control, not a pixel-identical cylindrical reference.

The post-sweep fence poll took 0 ms at reported median and p95 resolution;
every retained buffer was signaled on its first zero-timeout poll. This does
not measure when within the sweep the GPU completed its work. Pixel-pack
submission took 0.0 ms median and 0.1 ms p95 as measured by the browser's
rounded clock. Completed-buffer transfer was 0.2 ms median and 0.3-0.4 ms
p95. Encoding after transfer was about 5.9 ms median and 6.5-7.0 ms p95.
Host draining took 7.30 seconds native and 7.27 seconds rectilinear after
the sweep; thus collection of completed buffers caused no in-sweep callback
stall in these final runs. The render-start/end span is CPU submission time,
not GPU execution timing. No residency measurement was made.

`verify_b1_r3.py` checked callback counts against written frames plus
explicit overflow IDs, queue bounds, crossing coverage, metadata timing,
frame order, crop geometry, paths, byte sizes, nonconstant output and hashes.
`compare_b1_r3_pose.py` compared async crops near -6 and +4 with retained R2
synchronous crops from the same projection. Across eight display/pose pairs,
the best nearby reference yaw differed by 0.056-0.280 degrees and mean
absolute byte error was 1.17-8.11. A deliberately wrong yaw about eight
degrees away had error 36.4-96.0. This supports frame/yaw correlation and
rules out a gross stale-buffer or wrong-pose capture in these samples; it is
not a per-pixel proof for every frame. Native previews at
`run-1973d14d08804e5689c046766436f623/captures/async-preview-yaw-minus6.png`
and `async-preview-yaw-plus4.png` visibly contain the moving black-hole,
disc and bloom around the physical seam.

The current build also passed the synchronous native control
`run-b047aab90a1149e4a81fa813ed3cbbdc` (55.5 ms median, no overflow)
and the stepped control `run-a9351177c32c4a74ba9f8e7ce4b8dcab`.
The stepped yaw -6 and +4 metrics reproduced A2: 2.798 mean / 34 high rows
and 0.654 mean / five high rows. Fixture and stepped manifests passed the
existing analyzers. `node --check`, Universe typecheck, developer .NET build
and publish, Vite fixture build, Python verifier syntax, and `git diff --check`
passed. Native build retained the known NuGet advisory access, WindowsBase,
and WinForms DPI warnings.

## Limits and next gate

The crop does not cover the full black hole or bloom field. The 256-buffer
ring has only 23 free slots at its observed peak, so faster displays,
longer sweeps, or different hardware may overflow. Its 206.25 MiB nominal
GPU buffer payload is substantial and is not measured residency. P95 callback
spacing increased in three of four comparisons, and isolated 94-106 ms
outliers remain. These are documented pilot limits, not evidence of no
flicker. A full B1 review should inspect ordered motion frames, test repeat
captures, and decide whether this crop and cadence answer the visual question
on the target layout. The timestamps describe browser callbacks, render
submission, GPU fence polling, transfer, host receipt and file writes. They
do not identify WebView compositor presentation or synchronize displays at
scan-out.

Final read-only Git status matched preflight in both repositories. No
installation, display change, cleanup, staging, commit, push, production
promotion, or B2 work occurred. No file or run root was removed.
