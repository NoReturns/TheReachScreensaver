# R14 sustained GPU performance audit

Date: 2026-10-28. Mode: isolated developer measurement. Production rendering and installed state were not changed.

## Decision

**REVISE (option B): retain the cylindrical visual direction, but revise the renderer architecture before further B1 implementation. B1 remains NO-GO.** On the recorded three-monitor layout, the current cylindrical renderer completed 180 seconds per scene at approximately 60 callbacks per second on each display in the moving black-hole case. It also used about 4.5 GiB more process-attributed dedicated GPU memory than the matched rectilinear run, with a similar adapter-wide increase. The fourth-above geometry model is materially worse. This is a resource scaling concern even though this three-monitor run did not show sustained frame collapse or context loss. R13's peripheral and black-hole visual benefit remains relevant; R14 does not justify abandoning that projection. No production budget was set before the rectilinear baselines below.

The Windows `GPU Process Memory(*)\Dedicated Usage` counter is a direct, PID-attributed OS reading, not a WebView2 working-set proxy. It is not a precise inventory of individual WebGL targets or proof that every byte was resident continuously. NVIDIA `nvidia-smi` is adapter-wide and corroborates the large change but cannot attribute it to this app alone. No trustworthy isolated GPU frame-time measurement was available.

## Configuration and exact fixture states

- GPU: NVIDIA GeForce RTX 4080 SUPER, 16376 MiB reported by `nvidia-smi`; driver 621.01. Windows WDDM, three existing physical displays. No monitor setting was changed.
- Physical rectangles: left `(-3840,-720,3840,2160)` at 144 DPI; primary `(0,0,2560,1440)` at 96 DPI; right `(2560,360,1920,1080)` at 96 DPI. Normalized desktop: 8320x2160. Each WebView2 drawing buffer matched its physical display.
- Developer host: `ReachDeveloperBuild=true` published from the current dirty Screensaver checkout. Developer payload: isolated copy of the R12 fixture bundle used by R13. Each run had a fresh `profile`, `dist`, `host`, `logs`, `captures`, `metrics`, and `temp` under its own run root. The fixture reads no owner voyage or `localStorage` and has fixed synthetic bodies, stars, galaxy, seeds, exposure, final output, native bloom, and composition shift 1216 px. Cylindrical used the accepted fixture guard of 512 physical pixels.
- Deep-space cases A/B: `galaxy-left-seam`, base yaw -78 degrees, no black-hole lens, material time 1234, no camera motion. The scene was continuously rendered, but this is a static-camera limitation.
- Black-hole cases C/D: `hole-26-left`, hole distance 26 radii and angle -38 degrees, yaw 0 initially, material time 1234 and black-hole time 0. During warmup and measurement, each window followed `round(8*sin(2*pi*t/20000))` degrees, where `t` is milliseconds since that window's loop start. This produces a repeatable 20-second, integer-degree sweep between -8 and +8 degrees. It exercises lens changes caused by camera motion. Accretion-disc shader time stayed fixed, so animated disc behavior was not measured.
- The host verified one state key across all three windows before starting each run. Camera sweep phase was local to each WebView2 page; its intended trajectory was identical but callbacks were not frame-locked across pages.

## Method and raw evidence

The host first captured each fixture, then started requestAnimationFrame rendering in all three windows. It rendered for 10 seconds of warmup followed by 180 seconds of measurement. The browser recorded callback timestamps, callback intervals, JavaScript render-call duration, frame count, context-loss state, WebGL error and motion changes. The host collected WebView2 PID and working-set snapshots every 10 seconds. A separate PowerShell sampler collected NVIDIA adapter readings, Windows GPU process dedicated/shared memory counters, GPU engine counters, and process CPU counters approximately every two seconds. Cases ran serially with no other test case active. The host requested Windows display and system wakefulness for the measurement and released that request on completion. No normal voyage ran.

The Windows GPU PID set comes from `CoreWebView2Environment.GetProcessInfos()` for the isolated profile plus the developer host PID. The process-specific counter was summed over those PIDs. The observed nonzero dedicated allocation was predominantly in the WebView2 GPU process. All nonzero matched counters were on the same physical GPU counter instance. A single zero/absent sample in D was retained in raw evidence and not interpreted as a release of memory. Microsoft documents a known Windows 10 case where GPU Process Memory counters can overreport after an app is hidden and returns; these fixtures stayed visible, and adapter-wide memory changed by a similar amount. This still does not certify exact residency or exclude driver accounting differences. See [Microsoft's counter caveat](https://learn.microsoft.com/en-us/troubleshoot/windows-client/performance/gpu-process-memory-counters-report-wrong-value) and [GPU segment model](https://learn.microsoft.com/en-us/windows-hardware/drivers/display/gpu-segments).

| Case | Run root and raw files | Start to steady loop | Exit |
| --- | --- | ---: | --- |
| A rectilinear deep space | [run A](r14-runs/run-3202f06c0d68494c9e37d0de95f144b2) | 2.9 s | normal, code 0 |
| B cylindrical deep space | [run B](r14-runs/run-11531b80223145ae90a7690374d0127f) | 3.0 s | normal, code 0 |
| C rectilinear black hole | [run C](r14-runs/run-8da51371007145589a58119414029c18) | 6.6 s | normal, code 0 |
| D cylindrical black hole | [run D](r14-runs/run-866e0923671146118429f78b041fadbd) | 5.1 s | normal, code 0 |

Each run contains `run-manifest.json`, `counter-samples.jsonl`, `metrics/r14-*.json`, a fixture manifest, initial monitor PNGs and a host log. [summary.json](r14-runs/summary.json) is derived from those raw files by [r14_analyze.py](r14_analyze.py). Startup times are launch-to-measurement-loop start, including WebView2 initialization and initial fixture capture. The loop continued for the warmup and measured period; host shutdown followed report writing within about 0.5 to 1.5 seconds. All four processes exited and all 12 page records say `done`, with zero reported context losses, WebGL errors or callback exceptions. The initial sandbox pilot failed WebView2 controller creation with `E_UNEXPECTED`; evidence is preserved under `r14-runs/run-8331b5b5e00741d2bd65ae050f278580`. A corrected 10-second pilot succeeded under `run-9b92fa39e06f40e497a4bf5901746e46`. Neither pilot is included in the comparison.

## Direct frame pacing measurements

Intervals are requestAnimationFrame callback intervals in milliseconds, not GPU execution time or actual scanout presentation intervals. Effective FPS is measured callback count divided by 180 seconds. The left, primary and right displays can be scheduled at different cadences, so a sum across windows would be misleading. The JavaScript render-call median was 0.2 ms per window for A/C, 0.2-0.3 ms for B, and 0.4-0.5 ms for D; this is CPU-side invocation time only.

| Case | Primary FPS; median/p95/p99 ms | Left FPS; median/p95/p99 ms | Right FPS; median/p95/p99 ms |
| --- | --- | --- | --- |
| A | 97.4; 11.1/16.7/22.2 | 96.6; 11.1/16.7/22.2 | 75.0; 16.6/16.8/22.3 |
| B | 72.1; 16.6/22.2/22.3 | 60.0; 16.7/22.3/27.7 | 60.0; 16.7/27.7/27.8 |
| C | 72.5; 16.6/16.8/22.3 | 60.0; 16.7/22.2/22.3 | 60.0; 16.7/22.2/22.3 |
| D | 60.0; 16.7/16.8/22.2 | 60.0; 16.7/16.8/16.8 | 60.0; 16.7/16.8/17.1 |

The first and last measured minute had the same median callback interval to the reported 0.1 ms precision on every display in every case. D had one 33.4 ms maximum interval on the right; its other maxima were 22.6 ms. B had a 33.3 ms right maximum and 27.8-27.9 ms maxima elsewhere. These traces do not show a progressive cadence decline over three minutes. Mixed callback modes in A and scheduling can make a simple count of intervals above a median look like missed frames. Actual missed scanouts, present queue latency, tearing and display timing were unavailable without a presentation trace.

## GPU and CPU counters

Values below are medians during the measured 180 seconds. The Windows dedicated number is the sum for the isolated host/WebView2 PID group. NVIDIA values are for the entire adapter. `3D engine` is the sum of matched PID `engtype_3D` utilization counters; it is a scheduling/activity indicator, not a calibrated GPU-time measurement. CPU is summed process CPU seconds per wall second, expressed as a percent of one logical core. All cases used the same sampler and physical layout.

| Case | Process dedicated MiB | Adapter used MiB | Adapter GPU % | PID 3D engine % | CPU core equivalent % | WebView2 working set MiB |
| --- | ---: | ---: | ---: | ---: | ---: | ---: |
| A | 1507 | 3228 | 35 | 23 | 31 | 510 |
| B | 5979 | 7704 | 40 | 30 | 42 | 537 |
| C | 1514 | 3281 | 38 | 30 | 21 | 515 |
| D | 6047 | 7772 | 53 | 48 | 37 | 563 |

Paired process dedicated differences are about +4472 MiB in deep space and +4533 MiB in the black-hole case. Paired adapter-wide memory differences are +4476 and +4491 MiB. The close agreement supports the attribution at this coarse scale, but background GPU use was not held perfectly constant and these are separate serial runs. Windows process working set increased by only about 27-48 MiB, demonstrating why it cannot proxy GPU memory. The 3D engine and adapter GPU utilization rose for cylindrical, especially in the moving black-hole case, but neither reached a sustained 100%. This is evidence of greater GPU work, not proof of the exact limiting stage.

For process dedicated memory, first/last 15-sample medians were A 1513/1506 MiB, B 5923/5987 MiB, C 1522/1467 MiB, D 6015/6047 MiB. B/D shifted upward by roughly 64/32 MiB over this limited interval; the traces fluctuate and do not establish a leak. No progressive frame-rate loss accompanied them. Longer runs and a memory pressure/residency trace would be needed to exclude slow growth or eviction effects.

## Source-derived resources versus measurement

The existing [virtual-canvas architecture audit](VIRTUAL_CANVAS_ARCHITECTURE_FEASIBILITY_AUDIT.md) derives cylindrical sector/cube/composer bytes as `12*sector pixels + 12*6*512^2 per active cube + 16*padded post pixels`. This counts nominal RGBA16F color, depth, cube and a composer pair. It excludes bloom mips, multisample backing/resolves, browser surfaces, scene textures, driver overhead and process infrastructure. [A2-R4](STAGE_A2_R4_RESULTS.md) calculated 1299 MiB for the current three-monitor cylindrical black-hole target payload when bloom is added. The observed process dedicated usage of about 6047 MiB includes these and other GPU allocations; it is not a validation of the 1299 MiB as an upper bound. The roughly 4.5 GiB incremental measured cost is compatible with substantial multisample and browser/driver backing, but the exact split is unmeasured.

| Layout | Source-derived cylindrical sectors and target size | Principal nominal MiB; 5x-sector sensitivity | Runtime evidence |
| --- | --- | --- | --- |
| Single 3840x2160 4K | 2 at 3244x2675 | 343; 1138 | No R14 launch; model only |
| Two mixed-resolution, 3840x2160 plus 2560x1440 | 3+2 at 3244x2675 | 768; 2754 | No R14 launch; model only |
| Current three, 8320x2160 | 3+3+2 at 3244x2675 | 1158; 4336 | About 6.0 GiB process dedicated in B/D |
| Hypothetical fourth 3840x2160 above left, 8320x4320 | 2+2+1+2 at 6487x5349 | 3372; 14491 | Geometry and allocation model only; no fourth window |

The fourth-above case doubles bounding height, which doubles focal length and approximately doubles each target dimension, even though only one physical monitor is added. The 14.5 GiB sensitivity value is not a measured upper bound and omits other allocations. This model alone is enough to reject assuming that the present architecture scales safely to a vertical extension of the owner's 16 GiB GPU. It does not predict a fourth-display FPS.

R13 directly counted static final-frame draws in the same fixtures: deep space 84 rectilinear versus 160 cylindrical across three windows; black hole 87 versus 412. R14 did not re-count moving-frame draws. Source flow in `Universe/src/core/Engine.ts` renders one rectilinear scene per page, followed by optional black-hole, bloom and output passes. `Universe/src/reach/CylindricalRenderer.ts` renders 8 selected perspective sectors across the three pages and, with the lens active, 18 cube faces, then repeats lens/warp, bloom, output and crop per page. Rectilinear also retains three independent WebView2 contexts and local postprocessing. Draw counts do not by themselves identify a GPU bottleneck.

## Bottleneck ranking and limits

1. **GPU memory allocation and perspective target storage: strongest evidence.** The paired PID counter and adapter memory both rise by about 4.5 GiB, while the source shows 8 large RGBA16F sector targets with depth and up to four samples. This is a measured footprint increase, but no target-by-target accounting or definitive residency trace was captured. The fourth-above model amplifies this cost.
2. **Repeated sector scene rendering and padded postprocessing: strong architectural evidence, moderate runtime evidence.** Source shows 8 scene sectors plus per-window padded native bloom/crop. B has lower callback throughput than A on all displays; D needs more GPU engine activity than C. R14 cannot split sector rasterization from lens, bloom or crop time. The no-lens B/A difference shows the sector/post path is expensive without the six cube faces.
3. **Six-face cube rendering: plausible extra black-hole cost.** D/C has the largest GPU utilization difference and 18 cube-face scene passes are source-proven. However the black-hole shader and scene also differ from deep space, so the cube's isolated cost was not measured. B/D process dedicated readings are similar, but the cube's allocation and residency cannot be isolated from these aggregate counters.
4. **Separate WebView2 contexts, CPU submission and scheduling: present but not dominant on this run.** All modes use three contexts. JS render-call medians remain below 1 ms and summed process CPU is below half one core on median, while GPU activity and dedicated allocation increase. The callback trace may still include browser or compositor scheduling effects; no GPU timestamp queries or ETW present trace were captured.
5. **GPU saturation: not demonstrated.** D's adapter GPU median was 53% and matched 3D engine sum 48%, while all three displays sustained about 60 callbacks per second. These counters do not rule out short per-engine bursts, blocked presents, or GPU latency. They do rule out claiming a continuously saturated adapter from this evidence.

## Uncertainty and smallest next measurement

The audit measures a synthetic, fixed-content developer fixture, not a full voyage. Deep space has no camera motion; the black-hole camera sweeps in integer-degree steps, and disc shader time remains frozen. The apparent 60-callback ceiling in B/C/D may be display scheduling rather than a hard renderer limit. The runs were serial and unreplicated, so small percentage differences are noisy. No actual GPU per-frame timestamp, per-resource residency trace, present-to-scanout timestamp, or memory-pressure behavior was captured. Windows GPU Process Memory is a useful direct process counter here, with the Microsoft caveat above, but cannot be equated to guaranteed continuous residency. The adapter-wide NVIDIA values cannot be called process-specific by themselves.

The smallest additional measurement before any production decision is one isolated repeat of C/D with a Windows Performance Recorder GPU/video-memory and presentation trace, retaining the same profiles and fixture states, plus a developer-only smoothly animated black-hole time and camera path. That would separate GPU execution, residency/eviction, present pacing and background adapter use. It should be specified against the observed rectilinear baseline rather than an arbitrary new budget. The current R14 evidence already supports revising the memory-scaling architecture before further B1 implementation.

## Validation and scope

Focused developer `dotnet publish` passed, with existing NuGet vulnerability-index, WindowsBase and high-DPI warnings. A 10-second R14 pilot passed after the known sandbox WebView2 controller failure. Four fresh 180-second serial runs completed, with 10-second warmup each, matched fixture state checks, 12 complete page records, no reported WebGL errors/context losses, and normal process exit. `git diff --check` passed. No full suite, installation, Windows selection change, owner-journey access, cleanup, commit, push, or production promotion occurred. Changed files for R14: `Application/ReachDeveloperApplication.cs` (developer-only bounded sampler), `development/r14_run.ps1`, `development/r14_analyze.py`, this report, and new `development/r14-runs/` evidence plus `development/r14-publish/`. Existing R8-R13 evidence and dirty worktrees were preserved.
