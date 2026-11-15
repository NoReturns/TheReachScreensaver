# Stage B1 - continuous-motion preflight

Decision: NO-GO for motion acceptance with the existing capture tools. No B1
capture was run. This is an instrumentation blocker, not evidence of a new
rendering defect. Stage B2 was not started.

## Preflight state

Recorded 2026-10-25 before B1 edits:

- Screensaver `main` HEAD `4596b719f10fa39565f7cd34094b305d128825a2`.
  `git status --short --branch`: modified `.gitignore`, `Program.cs`,
  `TheReachScreensaver.csproj`; untracked
  `Application/ReachDeveloperApplication.cs`, `development/`.
- Universe `reach-v1-release` HEAD
  `76ab9d49c3cb6ae5d6b03f07073387b8708a47d7`. Status: deleted
  `ChatGPT Image Oct 11, 2026, 08_23_09 AM.png`; modified
  `src/core/Engine.ts`, `src/main.ts`, `src/reach/ReachScaleComparison.ts`,
  `src/render/BodyRenderer.ts`, `src/render/GalaxySprites.ts`,
  `src/render/Overlay.ts`, `src/render/TileGlobe.ts`,
  `src/render/shaders/index.ts`; untracked `Preview/`, `development/`,
  `src/reach/CylindricalGeometry.ts`, `src/reach/CylindricalLens.ts`,
  `src/reach/CylindricalRenderer.ts`,
  `src/reach/ReachDeveloperProjection.ts`, and
  `src/render/shaders/galaxy-cylindrical.vert`.

No existing modification or untracked file was removed or replaced. This
report is inside the already untracked Screensaver `development/` directory.

## Evidence reviewed

- `development/STAGE_A2_FINAL_ACCEPTANCE.md` accepts the cylindrical
  512-physical-pixel guard with `bloomMipMode=native` for controlled Stage B.
  It explicitly says the retained 11-pose sweep is sampled stills, with
  sequential captures taking about 0.7 seconds per step, and does not certify
  inter-frame flicker.
- `development/STAGE_A2_R3_RESULTS.md` identifies a small cross-context bloom
  difference at yaw -6 from missing coarse bright-source coverage. Its large
  adjacent-pixel seam impulses at yaw -6 and +4 primarily track the moving
  black-hole silhouette or disc edge. The same-virtual-pixel OutputPass peaks
  at those poses were 0.015152 and 0.000977.
- `development/STAGE_A2_R4_RESULTS.md` retains the native 512-pixel control;
  the diagnostic `grid` mode was not selected. It states the 11-step sequences
  cannot establish between-pose flicker or bloom popping.
- The original captures and metrics remain under
  `development/runs/run-29c5dcb6fdd9401b93f9937a5b5ee45f/` and
  `development/runs/run-43c3283b930f4fab9f17be704ba419cc/`. These are
  retained A2 evidence, not B1 continuous captures.

## Tooling determination

Code-proven: `Application/ReachDeveloperApplication.cs`, `Group.CaptureMotion`,
loops over eleven integer yaw values from -10 to +10 in 2-degree steps. It
calls `MoveToYaw` for each window, then `CapturePreview` sequentially for the
left and primary windows. The manifest stores yaw, state and PNG hashes, but
no per-frame presentation timestamps or frame stream. `MoveToYaw` invokes
`window.__a1MoveToYaw` once per step. In Universe
`development/cylindrical-stage-a1/view.js`, that hook sets camera yaw,
renders once and increments `frameRevision`. It does not animate between
steps. `development/run_stage_a2_r4.ps1 -Motion -BloomMode native -Guard 512`
therefore reproduces discrete still poses. Repeating it cannot answer B1.

The native fixture host rejects `--reach-dev-motion yaw` for rectilinear
mode. The available rectilinear reference captures are frozen controls or
ordinary live-route smoke; neither provides an identical, timestamped yaw
trajectory against the 512-pixel native candidate. The live route uses the
unchanged 128-pixel developer default. No existing host or fixture path
produces synchronized continuous native and reference frames. A generic
desktop recording of the current step sequence would record holds and jumps,
not genuine in-between motion, and cannot isolate transient bloom differences.

This is sufficient source evidence to stop before launch. No disposable B1
run, visual motion comparison, or frame pacing measurement was performed.
No conclusion is drawn about the presence or absence of temporal flicker or
fixed screen-space lines during genuine motion.

## Instrumentation required before capture

1. A development-only, deterministic yaw trajectory that continuously crosses
   -6 and +4 degrees in both directions, with the same black-hole state and
   the accepted native 512-pixel guard. It must render intermediate yaw poses
   on successive presentation frames rather than invoking isolated still
   renders.
2. A matching reference path driven by the same trajectory and scene state,
   with an explicit definition of what the reference is. The current fixture
   only supports the stepped cylindrical motion mode.
3. Continuous frame acquisition for both paths, with frame or presentation
   timestamps, yaw, frame revision, display identity, projection, guard and
   bloom mode. It must retain enough consecutive frames around both crossings
   to inspect the silhouette, disc edges, vertical screen-space features,
   and bloom that follows moving edges. Dropped or duplicated frames and
   capture cadence must be reported.
4. Run-local payload, profiles, logs and captures, plus a bounded stop and
   verification that the installed app, owner data, display configuration and
   backups are untouched.

After that exists, one bounded disposable run can compare synchronized
native and reference motion. B1 should report observed artifacts and pacing
before any B2 performance or residency work. This report changes no shader,
renderer, production code, installation, or owner data.
