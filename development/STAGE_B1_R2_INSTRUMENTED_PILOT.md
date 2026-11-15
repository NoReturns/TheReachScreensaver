# Stage B1-R2 developer-only continuous motion pilot

Decision: REVISE before full B1 motion evidence collection. The pilot proves
that bounded, ordered final-output seam frames can be captured through a
continuous trajectory in both modes. Synchronous readback materially changes
frame cadence, so these frames cannot certify absence of flicker at normal
cadence. B1 motion acceptance remains NO-GO. B2 was not run.

## Scope and preflight

Screensaver `main` HEAD was `4596b719f10fa39565f7cd34094b305d128825a2`;
Universe `reach-v1-release` HEAD was
`76ab9d49c3cb6ae5d6b03f07073387b8708a47d7`. Both repositories already
had the tracked and untracked changes listed in
`development/STAGE_B1_CONTINUOUS_MOTION_EVALUATION.md`. Those changes were
preserved. No renderer, shader, bloom algorithm, 512-pixel guard, installed
screensaver, Windows display setting, owner data, or backup was changed.

## Modified source

- `../Universe/development/cylindrical-stage-a1/view.js`: developer fixture
  `__b1Start` and `__b1Drain` hooks. A version-1, four-second trajectory uses
  requestAnimationFrame time: -10 to +10 degrees over two seconds, then back
  to -10 over two seconds. It records virtual trajectory time separately from
  callback time, frame ID, intended yaw and camera-derived actual yaw. Scene
  seed and material time remain frozen; black-hole time is passed as zero on
  every frame. The original stepped hook remains.
- `Application/ReachDeveloperApplication.cs`: developer-only
  `--reach-dev-continuous capture|baseline` option, common future epoch,
  bounded host drain, run-local RGBA files and timing manifest. Only the
  final 26-radii fixture is accepted; cylindrical requires 512 guard and
  native bloom. Rectilinear uses the same trajectory as a qualitative
  control. The old fixture capture runs before the new sweep.
- `Program.cs`: the ordinary executable explicitly rejects the new developer
  flag, like the existing developer flags.
- `development/run_stage_b1_r2.ps1` and `development/verify_b1_r2.py`:
  disposable staging and manifest/hash/order verification.

The capture reads the WebGL default framebuffer immediately after
`Engine.render()`, which finishes the cylindrical crop-to-screen pass or the
rectilinear composer OutputPass. It checks Three.js render target, WebGL draw
and read framebuffer bindings, and drawing-buffer size against the physical
display. Left crop: local x=3648..3839, y=800..1349. Primary crop: local
x=0..191, y=80..629. Each is 192x550 physical RGBA8 pixels, bottom-up;
their absolute virtual y range is 800..1349 on both monitors. This is the
left/primary physical seam with nearby silhouette, disc and bloom context.
The right window records callback and gap totals but no frame payload in this
pilot. Captures are final display-output bytes, not HDR stage readbacks or
verified compositor presentations.

Each browser queue holds at most four frames. Overflow IDs and callback gaps
are explicit. The host writes frames in receipt order with `writeOrdinal`,
per-display increasing IDs, SHA-256, payload size, crop, yaw, callback,
render, readback, encoding, host receipt and write timestamps. The manifest
keeps the missing-ID count before each written frame. The short fixed sweep
bounds the overflow and gap lists. No frame is silently replaced in the
queue. The host stops after every window reports completion and its queue is
empty, or fails after 25 seconds. Existing run-local profile, temp, payload,
capture, metrics and display-layout checks remain in use.

## Build and smoke validation

`node --check` for the fixture, `npm run typecheck`, developer .NET build and
self-contained publish passed. The Vite fixture build passed after an
elevated retry: the restricted sandbox blocked its Windows child process
with `spawn EPERM`. The elevated command wrote only to a fresh B1 development
build directory. Native build retained the existing NuGet advisory access,
WindowsBase and WinForms DPI warnings. No production package was built or
installed. `git diff --check` passed in both repositories.

The final developer build and fixture bundle are under
`development/b1-publish-r2b/` and `development/b1-fixture-build-r2c/`.
Verified run manifests and raw frames are:

- Native no-readback baseline:
  `development/runs/run-5d24967dceda45779d70d33adbd2e6d8/metrics/`.
  This used the same trajectory implementation before the right-window
  summary-only correction; left and primary remained unchanged. Their median
  callback interval was 16.7 ms, with 236 and 231 recorded frames. Both
  crossings had consecutive IDs. Three initial gaps above 50 ms occurred.
- Rectilinear no-readback baseline:
  `development/runs/run-84078c1fed174b2bbd8383efff8876e5/metrics/`.
  Left and primary each recorded 235 frames at median 16.7 ms, zero overflow,
  and consecutive frames at both crossings.
- Final native capture:
  `development/runs/run-97e2728dcd2f4e8f8666f2bf44a4ab12/metrics/` and
  `captures/`. Left and primary wrote 75 and 76 frames with increasing IDs,
  zero overflow and zero errors. Median callback interval was 55.5 ms for
  both; median synchronous readback was 39.3 ms. Within plus or minus one
  degree of -6, each had eight frames with consecutive IDs. At +4, each had
  seven, also consecutive. The right window completed 294 callbacks with
  zero queue overflow. The host completed its bounded stop normally.
- Final rectilinear capture:
  `development/runs/run-5906d81cd0c0473eaa7d1ee0fe8f3148/metrics/` and
  `captures/`. Left and primary each wrote 80 frames, with zero overflow and
  errors. Median callback interval was 50.0 ms; median readback was 35.9 ms
  and 39.1 ms. Each crossing had eight consecutive frames. The right window
  completed 296 callbacks. This is a projection control, not a pixel-identical
  reference for the cylindrical output.
- Existing stepped path on the final build:
  `development/runs/run-044b8acc3e9e4576a9ff587c3d190d6f/` passed
  `analyze_native_fixtures.py` and `analyze_native_motion.py`. Its yaw -6 and
  +4 seam metrics reproduced A2: 2.798 mean / 34 high rows and 0.654 mean /
  five high rows.

`verify_b1_r2.py` checked manifest schema, frame order, yaw calculation,
timing order, crop geometry, sandbox paths, RGBA size, nonconstant captured
pixels, and SHA-256 for every retained frame. An initial native pilot run
`run-db2682286e06432bb6fc3c4fee254731` exposed right-window metadata
overflow while the host transferred left and primary pixels. The fixture now
keeps only right-window callback/gap totals; the final runs have no overflow.
Its diagnostic image `captures/b1-preview-yaw-minus6.png` visibly contains
the black-hole silhouette, accretion disc and surrounding bloom, confirming
the readback is not an empty buffer. It is an instrumentation preview, not
visual acceptance.

## Timing and evidence limits

The 192-pixel strips do not cover the whole black hole or entire bloom field.
Readback is synchronous and raises median callback spacing from 16.7 ms to
50-55.5 ms, roughly 18-20 captured frames per second. The gap counter uses a
50 ms threshold, so the capture runs report many gaps even with ordered IDs.
Those are real callback delays, not necessarily dropped compositor frames.
No OS presentation timestamp or WebView compositor swap result was captured.
Separate native and rectilinear runs share a trajectory function, but do not
share presentation instants. The common host start epoch only coordinates
the three windows within a run approximately; actual yaw and callback time
must be used for comparison. Rendering, readback, host receipt, and file write
times are distinguished in the manifest. GPU timing and residency were not
measured.

For full B1 evidence, reduce or decouple per-frame readback cost while
retaining output-frame identity, then repeat capture and no-readback baseline
at useful cadence. Add validated presentation timing only if claiming
simultaneous display presentation. Inspect ordered frames for stationary
screen-space lines, edge-following bloom, and flicker before any motion
acceptance decision. This pilot does not infer flicker absence.

Final read-only Git status matched preflight in both repositories. Screensaver
remains on `main` at `4596b719f10fa39565f7cd34094b305d128825a2`, with
modified `.gitignore`, `Program.cs`, `TheReachScreensaver.csproj`, and
untracked `Application/ReachDeveloperApplication.cs` and `development/`.
Universe remains on `reach-v1-release` at
`76ab9d49c3cb6ae5d6b03f07073387b8708a47d7`, with its pre-existing
deleted image, modified Engine/main/reach/render sources, and untracked
Preview, development, and cylindrical source files. No staging, commit,
push, installation, cleanup, or B2 action occurred.
