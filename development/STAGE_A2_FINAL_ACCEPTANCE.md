# Stage A2 final acceptance gate: native motion and resources

Decision: GO WITH DOCUMENTED LIMITATIONS for a controlled Stage B evaluation.
The candidate is the existing cylindrical 512-physical-pixel guarded renderer
with `bloomMipMode=native`. The R4 `grid` option remains diagnostic and was
not selected. No renderer, projection, black-hole, galaxy, bloom, sector,
target-size, or composition code changed in this gate. Stage B was not run.

## Motion and visual classification

The fresh disposable native run
`run-29c5dcb6fdd9401b93f9937a5b5ee45f` captured the same 26-radii
black-hole fixture at yaw -10 through +10 in 2-degree steps, including -8,
-6, 0, +4, and +8. It used three physical windows with 3840x2160,
2560x1440, and 1920x1080 output. All frames reported guard 512, native
bloom, one session ID, one serialized pose/state key across displays, and
matching frame revisions 4 through 14. The verifier checked PNG hashes,
sizes, sandbox paths, and state. Left and primary captures were written
sequentially at each frozen pose. Their file modification times span about
19:37:52.636 to 19:38:00.291 UTC for the eleven poses, about 0.7 seconds
per step. These are sampled stills, not continuous video or synchronized
capture timestamps. A second run, `run-43c3283b930f4fab9f17be704ba419cc`,
reproduced the measurements while adapter-wide memory was observed.

Selected adjacent-pixel seam impulses were yaw -6: mean 2.798, peak 99.104,
34 rows above 20; yaw 0: mean 1.125, peak 2.000, zero rows; yaw +4: mean
0.654, peak 146.106, five rows. These reproduced R4 exactly. R3/R4
same-absolute-pixel OutputPass peak disagreements at yaw -6, 0, +4 were
0.015152, 0.009094, and 0.000977. Raw HDR was continuous in the sampled
overlap. The large adjacent-pixel peaks track real black-hole silhouette
and disc gradients, as shown by each context's own neighboring pixels.
There is a smaller localized bloom disagreement, especially at yaw -6.

Inspection of the native yaw -6, 0, and +4 seam crops and sampled motion
images found no persistent vertical brightness line, missing black-hole
background, disconnected stars or galaxy, distorted disc or shadow, or
visible stepwise bloom pop. The authoritative pose and frame state stayed
synchronized. Continuous flicker between the sampled positions was not
observed directly and is not certified by this capture system. No new video
recorder was added because the existing developer host offers only still
captures and building a continuous recorder would exceed this acceptance
gate.

The focused left sphere rerun passed in both modes. The cylindrical
`sphere-left` seam mean was 0.548 with zero rows above 20, while the
rectilinear fixture was 1.514 with eight edge rows. The original R4 native
control captures were reviewed again for Earth and atmosphere, Saturn and
rings, galaxy sector and physical boundaries, 240-radii and 26-radii black
holes, 15-degree roll, and the primary/right seam. No new clipping, missing
background, geometry break, or hard brightness line was observed. The
240-radii left seam remained mean 0.628 with zero hard rows; the 26-radii
primary/right seam remained 0.196 with zero hard rows at that join.

## Native resource observations

The 512-pixel fixture's calculated sector, cube, composer, and bloom target
payload remains 1299.38 MiB across three contexts. This estimate excludes
multisample backing, other textures, driver overhead, and transient storage.
It is not GPU residency.

`nvidia-smi` identified an NVIDIA GeForce RTX 4080 SUPER with 16376 MiB
reported memory. During the separate 512-pixel motion run, adapter-wide
used memory was 7657 to 7686 MiB at sampled 4 to 12 seconds, with 8360 to
8389 MiB free. It fell to 1810 MiB used after exit. A matched-state
rectilinear 26-radii fixture, `run-a5c0d0dad568419b9f06afd4d2c39d82`,
showed 3212 to 3216 MiB adapter-wide used during sampled 4 to 6 seconds
and 1801 MiB after exit. These values include unrelated desktop/driver
use and cannot be assigned precisely to Reach. NVIDIA per-process
`used_gpu_memory` returned `[N/A]`; no per-context residency was available.
No allocation failure or WebGL context loss appeared in either fixture run.

Matched 60-second disposable live-route smoke runs used the same staged
payload, three displays, and separate profiles:

- Cylindrical: `run-57082e263a5e474797cc807d8cabb118`. Its ordinary
  developer bootstrap uses the unchanged 128-pixel default, so these live
  process and frame figures are not a sustained 512-pixel measurement.
- Rectilinear: `run-5d36aaeb10914fb780db4d9923c92f7f`.

Both ran to their planned stop with group readiness and no logged context
loss or allocation failure. Each reported seven WebView2 subprocesses:
three renderers, one GPU, one browser, and two utilities. At the host's
capture point, cylindrical WebView2 private bytes totaled 6882.6 million
(about 6563.9 MiB) and working sets 1512.1 million (about 1442.1 MiB);
rectilinear totals were 3292.0 million (about 3139.5 MiB) private and
1617.5 million (about 1542.6 MiB) working set. The GPU subprocess private
bytes were about 5568.4 MiB versus 2103.2 MiB. Private bytes and working
sets are process accounting, not GPU residency. The native host itself was
about 17.5/59.7 MiB private/working for cylindrical and 18.5/59.7 MiB for
rectilinear at that point.

`requestAnimationFrame` interval p50/p95/p99 in milliseconds over the host's
recent three-second window were:

| Display | Cylindrical | Rectilinear |
| --- | --- | --- |
| Primary | 16.7 / 16.8 / 33.4 | 5.6 / 5.7 / 5.7 |
| Left | 11.1 / 33.3 / 33.4 | 5.6 / 5.7 / 5.7 |
| Right | 5.6 / 11.2 / 16.7 | 5.6 / 5.7 / 5.7 |

The host samples callback cadence shortly after startup. They are not GPU
timer measurements and do not establish stable animation throughput for
the 512-pixel candidate. Across the 60-second smoke, adapter-wide used
memory sampled near 7.0 GiB cylindrical and 3.7 GiB rectilinear, returning
near 1.8 GiB after each run. This is a coarse whole-adapter observation.

## Rectilinear default and isolation

The normal projection remains rectilinear when the frozen developer
projection bootstrap is absent. A fresh rectilinear 26-radii fixture with
the same serialized scene state captured successfully. The known visible
rectilinear `sphere-seam` and 240-radii joins from R1/R4 remain separate
release-acceptance issues; this gate did not repair them.

A fresh injected primary cylindrical initialization failure in
`run-e9b92e89d85440be8f6240fefe0ae921` closed that group, restarted a
complete three-window rectilinear group under a new session ID, passed
readiness, and exited normally. The current hardware has three connected
monitors. A true single-monitor native run was not performed because this
gate forbids changing Windows display configuration. The single-monitor
perspective fallback is supported by the default code path but remains an
environment-specific validation obligation.

All launches used `TheReachDeveloper.exe` staged under unique
`development/runs/run-*` roots with run-local payload, profile, temp, logs,
captures, and metrics. Source and run logs show no installed executable,
production profile, journey, checkpoint, reset, settings, cache, or backup
path selected. There was no independent owner-data access trace or hash
comparison. No installed screensaver, Windows display setting, deployment
backup, or repository source file was changed.

## Transfer to Stage B/C

Before release acceptance, obtain continuous or substantially denser
512-pixel motion observation, including between-step flicker and bloom
popping, and collect sustained 512-pixel GPU timing and residency on target
hardware. Test a genuine single-monitor native environment without changing
the owner's current display setup. Reassess the known rectilinear joins,
additional monitor layouts/adapters, and owner visual acceptance. These
open gates do not reveal a new material defect that blocks a controlled
Stage B evaluation; they are not a claim of production readiness.

This gate added only this report and disposable ignored run evidence. Both
repositories retained their pre-existing working-tree changes. No commit,
push, installation, Stage B, or release work was performed.

Final read-only `git status --short --branch` was unchanged from preflight.
Screensaver on `main`: modified `.gitignore`, `Program.cs`, and
`TheReachScreensaver.csproj`; untracked `Application/ReachDeveloperApplication.cs`
and `development/`. Universe on `reach-v1-release`: the pre-existing deleted
`ChatGPT Image Oct 11, 2026, 08_23_09 AM.png`; modified `src/core/Engine.ts`,
`src/main.ts`, `src/reach/ReachScaleComparison.ts`, `src/render/BodyRenderer.ts`,
`src/render/GalaxySprites.ts`, `src/render/Overlay.ts`, `src/render/TileGlobe.ts`,
and `src/render/shaders/index.ts`; untracked `Preview/`, `development/`,
`src/reach/CylindricalGeometry.ts`, `src/reach/CylindricalLens.ts`,
`src/reach/CylindricalRenderer.ts`, `src/reach/ReachDeveloperProjection.ts`,
and `src/render/shaders/galaxy-cylindrical.vert`. `git diff --check` passed
in both repositories. This report is within the already untracked
Screensaver `development/` directory.
