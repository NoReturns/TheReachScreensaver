# Stage B1-R9 isolated full-frame sector parity pilot

Decision: the added +42.0064175-degree left source reduces the original
physical join discontinuity in the final encoded frame at frozen yaw +6.815.
This is evidence for the exact frozen fixture only. B1 is not accepted.

## Fixture and implementation

The final run is `runs/run-67a13889e71249fd9f9d797341a0bbb5`.
`metrics/sector-parity-*.json` identifies the complete before, pilot and
restored PNG frames for left and primary, plus complete raw RGBA16F lens
buffers before and during the pilot. `metrics/sector-parity-analysis.json`
contains the measured comparisons. The lens buffers are bottom-up WebGL
readbacks; their dimensions and absolute virtual rectangles are in the
manifest. The initial standard fixture captured all three displays.

The disposable host fixed the `hole-26-left` state at yaw +6.815, a
8320x2160 virtual desktop, native bloom and 512-pixel guard. State keys
matched across windows. The R9 switch modified only the left fixture
renderer instance: after ordinary sector selection, it appended a
3244x2675 RGBA16F, four-sample color and depth target at center
0.733150292618528 radians. That is the R8 +42 source projection. The
ordinary render loop filled it, and the accepted lens shader made its own
source choice. The primary renderer, lens mathematics, shader source and
production renderer were unchanged. The switch was removed and the target
disposed after the pilot capture.

## Final-frame result

The physical join is left x=3839 beside primary x=3840, for virtual
y=720..2159. Rec. 709 encoded luma differences across all 1,440 rows:

| Metric | Before | Pilot | Restored |
| --- | ---: | ---: | ---: |
| Mean adjacent step | 2.917 | 0.967 | 2.917 |
| 95th percentile | 12.111 | 3.072 | 12.111 |
| Peak | 59.269 | 38.132 | 59.269 |
| Rows above 20 | 59 | 3 | 59 |
| Rows above 40 | 25 | 0 | 25 |
| Row y=1034 | 47.269 | 1.928 | 47.269 |

The original row y=1034 step fell by 95.9% in final output. No join row
increased by more than 1.93 luma units. The primary before/after PNGs are
byte identical. In the left PNG, changes greater than one channel unit
are confined to x=3806..3839, y=949..1113 (3,400 pixels). The restored
left frame differs from baseline by at most one channel unit, consistent
with the previously observed repeated-render variation. The disabled
initial frame also matches the R8 fixture with the same state key to
within one channel unit: 6 left pixels, 14 primary pixels and 2 right
pixels differ across the complete frames.

The full lens buffers confirm that the left x=3839, y=1034 RGB changed
from `(0.316162, 0.292725, 0.252686)` to
`(0.002310, 0.002190, 0.001950)`, matching primary at that location.
Primary lens buffers have seven differing RGB half-float values, peak
0.000061, while its final PNG is unchanged. This is small repeated-render
variation, not a changed primary source list.

The full-frame search for a displaced vertical seam found only isolated
new steps above 20 near x=3825..3829 on one or two rows around y=1048.
Visual inspection of the enlarged join region found no new extended line.
Those small local steps and the three remaining physical join rows above
20 mean that absence of every visible artifact is not established. The
captured still frame is not owner visual acceptance or a motion test.

## Resource cost

The added target is 3244x2675 with RGBA16F color, depth and four samples.
Its color plus depth estimate is 104,132,400 bytes, or 99.31 MiB, exactly
the R8 estimate. This excludes driver allocation and multisample storage;
actual GPU residency was not queried. The left full-frame draw count rose
from 142 to 156 and returned to 142 when disabled. Primary remained 142.

Forty-eight warmed full-frame samples per phase used `performance.now()`
around `engine.render()` followed by `gl.finish()`. Left mean wall time was
0.631 ms before, 0.692 ms with the added sector, and 0.556 ms restored.
The pilot minus initial mean is 0.060 ms, and pilot minus the mean of both
disabled phases is 0.098 ms. Primary control means were 0.623, 0.637 and
0.646 ms. This is an approximate CPU-plus-GPU completed-render measure,
not an isolated GPU timer result; sub-millisecond ordering and clock noise
limit cost precision. The reliable work increase is one sector target and
14 scene draws per left frame.

## Validation and limits

`node --check`, Universe typecheck, the isolated Vite fixture build,
developer .NET build and publish, the native fixture verifier, and
`git diff --check` passed. The successful native run used an isolated
WebView2 profile and run root. No production installation, owner journey,
Windows selection, cleanup, commit or remote write was performed.

This pilot does not test changing yaw, other scene states, alternate
monitor layouts, long-run residency or owner visual acceptance. It does
not implement dynamic sector-envelope discovery. B1 remains open.
