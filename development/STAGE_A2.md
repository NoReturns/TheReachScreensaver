# Stage A2 disposable native lane

The developer host is compiled only with `-p:ReachDeveloperBuild=true`. Its
assembly is `TheReachDeveloper.exe`; the build does not produce a `.scr`.
The ordinary assembly rejects all Stage A2 flags before normal startup.
The developer assembly rejects ordinary `/s`, `/p`, and `/c` entry paths.

Create a fresh `development/runs/run-<32 hex digits>` directory for each
invocation. Stage the developer executable in `host/` and the Universe build
in `dist/`. Build Universe with `VITE_REACH_CYLINDRICAL_DEV=1` and
`VITE_REACH_DEV_TEST=1`, then write
`VITE_REACH_CYLINDRICAL_DEV=1;VITE_REACH_DEV_TEST=1` to
`dist/reach-developer-payload.txt`. The marker records the staging contract;
it is not a cryptographic attestation of the bundle. Do not reuse a run root.

Launch syntax:

```text
host/TheReachDeveloper.exe --reach-dev-projection cylindrical --sandbox-root <absolute run root>
host/TheReachDeveloper.exe --reach-dev-projection rectilinear --sandbox-root <absolute run root>
```

Optional development test flags are `--reach-dev-smoke-seconds 5..120` for a
graceful timed exit and `--reach-dev-fault primary-init` for an injected
cylindrical primary initialization failure. The latter tests group-wide
rectilinear fallback with a new session ID. The run root is never deleted.

Stage A2-R1 native visual fixtures use
`--reach-dev-fixture <fixture-id>` and optional
`--reach-dev-post raw|bloom|output` (default `final`). The fixture route loads
only `dist/development/cylindrical-stage-a1/index.html`. Valid IDs are
`sphere-left`, `sphere-seam`, `earth-seam`, `saturn-sector`, `galaxy-sector`,
`hole-240-left`, `hole-26-left`, `hole-26-away`, `hole-primary-right`,
`hole-26-primary-right`, `hole-26-sector`, `galaxy-left-seam`, `roll-15`, and
`rearward`. Use a distinct run root for each mode and pass.
The host checks the three windows' exact serialized fixture state, session,
mode, frame revision, drawing buffer, and requested cylindrical guard before
capturing. It hashes each full-resolution PNG and records the contract and
state in `metrics/fixture-*.json`. The 26-radii final fixture also renders
and captures two further frozen frames. A fixture failure terminates the
developer run without starting the ordinary host.

Stage A2-R2 uses a 512 physical-pixel guard by default for cylindrical native
fixtures only. The non-fixture developer smoke lane and ordinary release keep
their previous settings. `--reach-dev-guard 0|64|128|256|320|384|448|512`
selects a diagnostic width. `--reach-dev-bloom-mips guard128` is a 512-guard
experiment that shrinks only bloom targets to their 128-guard dimensions.
`--reach-dev-motion yaw` runs the 26-radii left-seam fixture through eleven
authoritative yaw steps in the same three-window group and saves hashed left
and primary captures at each step. Fixture capture does not arm the native
mouse-exit monitor, which could otherwise interrupt evidence capture.
See `STAGE_A2_R2_RESULTS.md` for the guard sweep, mip diagnosis and motion
limitations.

Stage A2-R3 frozen forensic fixtures add `--reach-dev-yaw -12..12` for the
26-radii left-seam fixture and `--reach-dev-forensics 1` for its cylindrical,
final-pass, 512-pixel-guard capture. All three windows must report the same
fixture state and first-frame revision before capture. The forensic flag saves
matching absolute-virtual-coordinate half-float stage strips in
`metrics/forensics-*.json`; `python development/analyze_native_forensics.py
<forensics manifest>` verifies the state and compares overlapping pixels.
The probe is developer-only and does not alter the rendering pipeline.

`python development/analyze_native_fixtures.py <fixture manifest>` verifies
capture hashes and state, assembles the normalized virtual desktop, writes
monitor seam and black-hole crops, and records seam impulses under the same
run root. `python development/compare_native_passes.py <eight 26-radii
rectilinear/cylindrical pass manifests>` compares matching half-float
left/primary boundary samples. These scripts only read capture files under
the supplied disposable roots; their generated evidence stays there.

The host uses only `dist/`, `profile/`, `logs/`, `captures/`, `metrics/`, and
`temp/` beneath the run root. It rejects reparse points, missing payloads,
and reused roots. The selected developer route is a synthetic two-stop
Earth-to-ISS preview, so the Universe journey store is not constructed.
The native path does not call settings, reset, or installed-payload lookup.

The A2-R1 fixture captures cover the 240-radii and 26-radii poses and a
15-degree roll. The native 26-radii cylindrical final capture has a visible
left/primary bloom seam; the 240-radii cylindrical seam scan did not show it.
Rectilinear reference sphere and 240-radii captures also show large monitor
brightness joins, so this fixture set is diagnostic evidence, not visual
acceptance. The 26-radii seam is absent in raw and OutputPass-only samples,
appears with bloom, and is amplified by OutputPass. A short frozen-frame
repeat does not replace a moving-camera run or owner visual acceptance.
Frame interval samples are recent
`requestAnimationFrame` callback intervals, not GPU cost. GPU process private
bytes are not measured GPU residency.
