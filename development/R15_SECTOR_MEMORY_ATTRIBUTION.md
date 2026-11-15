# R15 sector memory attribution pilot

Date: 2026-10-28. Mode: isolated developer experiment. Production renderer, installed screensaver and owner journey were not changed. B1 remains NO-GO.

## Decision

**REVISE.** Reducing perspective-sector targets from 4x to 2x MSAA saved **1591 MiB** of process-attributed dedicated GPU memory in the warmed moving black-hole fixture, with no visible objectionable difference in the five matched still states or sampled black-hole motion crops. This makes 2x a credible developer candidate for a longer motion and owner visual review. The no-multisample variant saved **3423 MiB** relative to 4x, but changed more planet and star-edge pixels and has less evidence for motion quality. This pilot does not authorize selecting either setting for production. Sector dimensions still depend on virtual desktop height; a sample-count change does not solve fourth-above or arbitrary-layout scaling.

## Scope and invariant checks

The base is the same R12 isolated developer bundle used in R13 and R14. Three copies were made under [r15-variants](r15-variants/manifest.json). The only renderer expression changed between copies was the sector target's `samples` property in the compiled developer bundle. A common status-only probe adds `target.samples` to the reported sector target tuple. After normalizing that one `samples` expression, the three bundle texts compare equal. The source allocation in `Universe/src/reach/CylindricalRenderer.ts` remains `samples: Math.min(4, this.renderer.capabilities.maxSamples)`; no Universe source or accepted lens/shader mathematics was edited.

| Label | Sector `WebGLRenderTarget.samples` | Meaning |
| --- | ---: | --- |
| 4x control | 4 | Existing R12/R14 fixture behavior |
| 2x | 2 | Two-sample sector storage |
| 1x | 0 | One color/depth value per pixel, no multisample storage |

Three.js uses its multisampled render-target path when `samples > 0`, so the requested 1x case uses API value 0. All fixture manifests report 3+3+2 sector targets at 3244x2675 in the recorded 8320x2160 layout, with the expected 4, 2 or 0 sample value on every target. The camera, synthetic scene, black-hole lens equations, native bloom, 512-pixel guard, physical display rectangles and output buffers, exposure and tone mapping were otherwise identical. The five states had an identical fixture state key across variants. The developer host used a new profile and sandbox for each launch; no voyage or owner profile was read.
The WebGL output canvas still requested antialiasing in every variant, and native bloom still softened the final image. The 1x label refers only to perspective-sector storage.

## Test method and raw evidence

For each configuration, the developer host captured complete final PNGs from all three physical monitors for each state, then ran R13's 8 warmup and 48 static draw-count checks. The five states were `hole-26-left` yaw 0, -6 and +6.815 degrees, `galaxy-sector` at its fixed +23-degree peripheral pose, and `galaxy-left-seam` at its fixed -78-degree deep-space pose. Black-hole distance was 26 radii at -38 degrees; the fixture fixed material time 1234, black-hole time 0 and the synthetic random seeds. Capture manifests retain each original PNG path and SHA-256. [summary.json](r15-runs/summary.json) gives the 15 run roots, state-key hashes, target status, draw counts, join metrics and pixel comparisons. The original files remain under the run roots in [r15-runs](r15-runs).

Three separate moving black-hole runs used the R14 requestAnimationFrame loop: 10 seconds of warmup, then 30 seconds of paced rendering in all windows. Each used the same deterministic 20-second integer-degree yaw sweep from -8 to +8 degrees, fixed disc shader time and 512-pixel guard. An external sampler recorded Windows `GPU Process Memory(*)\Dedicated Usage` by isolated WebView2 PID and NVIDIA adapter-wide used memory and utilization about every two seconds. There were 13 samples in each 30-second measurement window. [r15_analyze.py](r15_analyze.py) derives the table below from [4x raw run](r15-runs/run-c5ebea6d786340549488e7c99b601f38), [2x raw run](r15-runs/run-1a037730bb794ee0a8b1ce26eb19aeeb) and [1x raw run](r15-runs/run-5d4bf4c1380945208546886b9384a4f2). Each run retains `counter-samples.jsonl`, `metrics/r14-*.json`, a fixture manifest, initial PNGs and logs.

The PID-attributed counter is a direct Windows dedicated-GPU-memory reading, not WebView2 working set or exact target-by-target residency. NVIDIA memory is adapter-wide and cannot be attributed to this process alone. Both counters moved by the same amount to within a few MiB in these serial runs, supporting a large sector-related allocation difference. As noted in R14, Windows GPU Process Memory counters have a documented caveat for some Windows 10 hide/restore sequences; these fixtures stayed visible. No isolated GPU frame time was measured.

## Measured memory and pacing

Medians are over the 13 samples after warmup. The process dedicated range is included to show short-run stability; 4x and 2x settled upward during the early measured samples. Differences use medians and are not claims of byte-exact target storage. All three windows completed all three 30-second runs with no reported WebGL errors, context losses or callback exceptions.

| Sector setting | Process dedicated MiB, median (range) | NVIDIA adapter used MiB | Adapter GPU % | Primary/left/right frames in 30 s | p95 callback interval ms, primary/left/right |
| --- | ---: | ---: | ---: | --- | --- |
| 4x | 6000 (5949-6047) | 7758 | 52 | 1802 / 1801 / 1800 | 16.8 / 16.8 / 16.8 |
| 2x | 4409 (4343-4409) | 6167 | 51 | 1800 / 1800 / 1800 | 16.8 / 16.8 / 16.8 |
| 1x, no MSAA | 2577 (2577-2577) | 4335 | 39 | 1801 / 1801 / 1800 | 16.7 / 16.8 / 16.8 |

The measured 4x-to-2x difference is **1591 MiB**, and 4x-to-1x is **3423 MiB**. Adapter-wide differences are 1591 and 3423 MiB respectively, a useful cross-check rather than process attribution by itself. All runs delivered about 60 callbacks per second on each window. The p99 callback interval was at most 22.2 ms in the 4x run and 16.8 ms in the 2x/1x runs; short serial runs do not establish a small pacing advantage. Callback intervals are browser scheduling observations, not isolated GPU frame time or scanout timing.

R13 static draw instrumentation reported **412 total draws** for each black-hole variant and **160** for each non-lens variant, unchanged across sample counts. The per-window draw arrays and WebGL error flags are in each capture run's `metrics/r13-*.json`. Sample count changed storage and rasterization, not scene/sector pass count.

## Final-image and motion inspection

The [final crop montage](r15-runs/R15_CROP_MONTAGE.png) shows 4x, 2x and 1x at native source pixels for the black-hole left-primary join, the peripheral planet and a deep-space star field. Complete, unresized final PNGs and hashes are referenced by the fixture manifests in the 15 capture runs. The [motion crop montage](r15-runs/R15_MOTION_CROP_MONTAGE.png) shows matched-time left and primary seam crops at approximately -6, 0 and +6.8 degrees. [motion-review-frames.json](r15-runs/motion-review-frames.json) gives the exact sampled frame IDs, times and yaw values. The three four-second asynchronous motion runs retained all left/primary crop frames: [4x](r15-runs/run-e2eca239b09f461b8c2fbeaf23282514), [2x](r15-runs/run-38da051466bd4f3f872d4328bdaf4780) and [1x](r15-runs/run-f6b27e7d09e2476f86241ee99625a453). They completed with zero readback overflow or errors; each window recorded about 228-238 callbacks. These motion captures are a visual sample, not a GPU timing benchmark.

The table counts pixels brighter than 8 in the 4x full-layout image as active. For those pixels, it reports the fraction whose maximum RGB-channel difference from 4x exceeds 8 encoded levels. This highlights edge differences without letting the large black background dominate. It does not score perceptual quality or shimmer.

| Scene | 2x changed active pixels | 1x changed active pixels | Visual reading at captured size |
| --- | ---: | ---: | --- |
| Hole yaw 0 | 0.031% | 0.067% | Silhouette, disc rim and caustic look materially unchanged in stills |
| Hole yaw -6 | 0.030% | 0.060% | Existing caustic at join remains; no new shape break seen |
| Hole yaw +6.815 | 0.025% | 0.056% | Existing sector/cube join behavior remains; sample count does not fix it |
| Peripheral planet | 0.368% | 0.626% | 1x shows the larger small edge-pixel change; no gross limb distortion seen |
| Deep-space stars | 0.349% | 0.635% | Sparse stars remain, with more per-pixel changes at 1x |

At left-primary, the encoded-luma adjacent-pixel mean and rows above 20 were effectively unchanged across variants: yaw 0 about 1.027/0, yaw -6 about 3.47/60-61, and yaw +6.815 about 2.917/59. Primary-right had no rows above 20 in these five frozen states. This does not prove a join is correct under all motion; R8-R11's known source-parity and caustic problems remain. The sampled motion crops did not show an obvious new edge break at 2x or 1x, but three selected times from a four-second sweep cannot rule out shimmer. Planet-edge and starfield motion were not captured. Owner visual acceptance is outstanding.

## Attribution and layout scaling

The current layout has eight 3244x2675 sector instances, about 69.42 million sector pixels. A sector uses RGBA16F color and depth; the source-derived resolved color-plus-depth footprint is about 12 bytes per pixel before multisample backing. The 4x-to-2x saving is about 24 bytes per sector pixel, consistent with two fewer color/depth samples at 12 bytes each. The 4x-to-no-MSAA saving is about 52 bytes per sector pixel, close to but above the simple 48-byte four-sample backing model. The difference can involve resolves, driver backing and counter variation; R15 did not inspect individual GPU allocations.

R14's moving black-hole comparison found about 4533 MiB more PID-attributed dedicated GPU memory in cylindrical than rectilinear mode. R15's 3423 MiB no-MSAA saving accounts for roughly **three quarters** of that incremental footprint under comparable three-monitor fixtures. The no-MSAA cylindrical process still used about **1063 MiB more** than R14's rectilinear black-hole process median (2577 versus 1514 MiB). That remainder is not attributable by this experiment among resolved sector textures, cube, padded bloom/composer, WebView2 contexts, scene assets and driver storage. The 2x result retains about 2.9 GiB over the R14 rectilinear process baseline. Comparing separate runs is approximate; the R15 4x process median of 6000 MiB was within 47 MiB of R14 D's 6047 MiB.

The existing [virtual-canvas audit](VIRTUAL_CANVAS_ARCHITECTURE_FEASIBILITY_AUDIT.md) models a hypothetical 3840x2160 fourth monitor directly above the left display, yielding an 8320x4320 virtual bound and seven much larger 6487x5349 sectors. Their total is about 242.9 million pixels, 3.5 times today's sector-pixel count. Applying the same nominal per-sample sector storage gives the following source model before bloom mips, browser surfaces, scene assets and driver overhead:

| Hypothetical fourth-above setting | Principal nominal targets plus modeled multisample backing, MiB |
| --- | ---: |
| 4x | about 14491 |
| 2x | about 8932 |
| 1x, no MSAA | about 3372 |

These are **not fourth-window measurements or safe upper bounds**. The current three-monitor empirical savings should not be multiplied into a precise fourth-monitor residency forecast. A fourth context, different sector selection, driver allocations and memory pressure could alter actual usage. Reducing samples leaves the dependency on virtual desktop height intact and does not establish arbitrary-layout viability.

## Validation and remaining uncertainty

Validation performed: exact bundle-difference normalization; 15 successful three-monitor final-frame captures and state-key checks; target dimension/sample verification; 48 static timed frames per window per capture case for draw counts and WebGL errors; three fresh 10-second-warmup plus 30-second paced memory runs; three four-second asynchronous motion-crop runs; original PNG SHA-256 checks; full-layout pixel and join comparisons; visual inspection of both montages; Python and PowerShell script execution; and `git diff --check`. No full suite was run. The R14 developer host executable was reused; no dependency upgrade or production build occurred.

Limits: one short memory run per sample setting, no GPU timestamp or target-level allocation trace, no long-term residency/eviction test, no moving planet or starfield capture, no video-based shimmer scoring, and no owner visual review. Windows counter caveats and adapter-wide attribution limits remain as in R14. The pilot identifies a large multisample contribution; it does not make the current cylindrical renderer production-ready. New R15 files are this report, `development/r15_prepare.py`, `development/r15_capture.ps1`, `development/r15_measure.ps1`, `development/r15_motion.ps1`, `development/r15_analyze.py`, `development/r15_motion_review.py`, variant bundles and evidence under `development/r15-runs/`. Existing dirty worktrees and R8-R14 evidence were preserved. No cleanup, deletion, installation, monitor change, commit or push occurred.
