# Stage A2-R2 native bloom seam diagnosis

Decision: REVISE for completing Stage A2. The developer-only 512-pixel guard
repairs the fixed yaw-zero 26-radii hard join, but the controlled yaw sweep
still has localized spikes. Stage B and production integration are not approved.

## Source cause

Three.js `UnrealBloomPass` extracts bright pixels at half resolution, applies
five progressively smaller horizontal and vertical Gaussian blurs, composites
the five mips, and adds the result to the HDR input. Its blur kernels contain
6, 10, 14, 18 and 22 taps before bilinear tap merging. The composited blur
can therefore draw from a much wider source region than 128 physical pixels.
The left and primary windows own independent post targets of different sizes
and offsets. Their bloom mips also have different dimensions and sampling
phases.

At absolute virtual x=3840, y=1200 in the 26-radii native fixture, the
128-guard bright pass and mips 0 to 2 were zero in both contexts. Mip 3 was
approximately RGB (0.000557, 0.000592, 0.000708) on the left and zero on
primary. Mip 4 was (0.02826, 0.02945, 0.03375) on the left and zero on
primary. The composite was (0.01703, 0.01775, 0.02034) against zero.
This is the first directly measured divergence in the bloom chain. The raw
lens input remained nearly continuous at the same physical seam.

With 512-pixel guards, mip 3 matched across the two contexts to half-float
readback precision. Mip 4 was (0.02826, 0.02945, 0.03375) against
(0.02927, 0.03041, 0.03455); both contexts now received the distant bright
contribution. Holding the 512-pixel source region but forcing the 128-guard
bloom mip dimensions increased final seam impulse from 1.125 to 3.691, while
remaining below the hard-row threshold. Thus insufficient source coverage is
the cause of the hard 128-guard join, and differing mip dimensions/sampling
contribute a smaller residual. There is no evidence of a crop-coordinate,
black-hole physics, or OutputPass transfer-function error. OutputPass
amplified the bloom difference but did not introduce it.

The native stage-probe manifests are under
`run-431c37425a454e8fb3907c53d1a1bff7` (128) and
`run-bc017509a3b14e1cbc088b43a4e5905f` (512).

## Physical guard sweep

All rows use the same frozen fixture, scene revision, physical layout, pass
settings, and projection scale. Each run used its own disposable sandbox and
session. The RGB metric is excess seam luminance impulse on absolute x=3840,
y=800..1499. Half-float source checks included raw lens, bloom without
OutputPass, and final output. The full sweep and target sizes are recorded in
`run-783fadc84a1143c8b8ba449a9905d42d/metrics/guard-sweep.json`.

| Guard px | Mean | p95 | p99 | Peak | Rows >20 | Calculated principal MiB |
| ---: | ---: | ---: | ---: | ---: | ---: | ---: |
| 0 | 13.569 | 24.076 | 24.076 | 24.076 | 331 | 864.30 |
| 64 | 12.178 | 22.004 | 22.004 | 22.004 | 225 | 874.84 |
| 128 | 12.168 | 22.004 | 22.004 | 22.004 | 225 | 885.76 |
| 256 | 12.032 | 22.004 | 22.004 | 22.004 | 222 | 1008.02 |
| 320 | 8.372 | 15.860 | 15.860 | 15.860 | 0 | 1020.1 |
| 384 | 2.763 | 5.002 | 5.002 | 5.144 | 0 | 1131.8 |
| 448 | 2.320 | 4.072 | 4.072 | 4.430 | 0 | 1144.6 |
| 512 | 1.125 | 2.000 | 2.000 | 2.000 | 0 | 1157.73 |

At all five required widths, raw final-image impulse stayed 0.068 and raw
half-float impulse stayed about 0.000043. Bloom-only mean RGB impulse fell
from 3.679 at 128 to 0.387 at 512. The 512 result was visually smooth in
the native seam crop; 320 still showed a subtle gradient, and 384/448 were
intermediate. The developer fixture default is now 512, while the ordinary
release and rectilinear renderer remain unchanged. A diagnostic flag retains
all tested widths. This is a fixture-lane correction, not a production setting.

At 128 the calculated principal targets total 885.76 MiB, plus 108.05 MiB
for the bloom bright and five horizontal/vertical mip targets. At 512 those
figures are 1157.73 and 141.65 MiB. The limited target estimate rises by
305.57 MiB to 1299.38 MiB; driver overhead, other textures and cube detail
are not included. Guard expansion changes sector counts from 2/3/1 to 3/3/2.
Before the sweep the RTX 4080 SUPER reported at least 13,903 MiB free, and
all five required widths allocated and exited cleanly. No reliable GPU timer
or measured residency result was obtained; these byte counts are calculated,
not GPU residency measurements.

## Matched native regression

Ten same-state rectilinear/cylindrical fixture pairs passed manifest and SHA-256
checks, for 60 original physical-monitor captures. These include the 26-radii
left, away, primary/right and internal-sector cases; 240-radii left; galaxy
at the left seam; Earth and atmosphere; Saturn and rings; 15-degree roll; and
the reference sphere. Each pair had equal state keys and separate sessions.
The corrected galaxy seam pose used yaw -78 degrees; an earlier yaw +78
diagnostic placed the galaxy elsewhere and was not counted as seam evidence.

At 512, the 240-radii left seam mean impulse was 0.628, zero rows above 20,
compared with the R1 accepted 0.700 and zero rows. The 26-radii left seam
fell from 12.168/225 rows to 1.125/zero rows. The 26-radii primary/right seam
was 0.196 with zero rows above 20. Earth, Saturn, sphere, and galaxy did not
show a new physical-monitor hard join. The galaxy seam scan at y=1400..1799
had mean impulse 0.085, peak 1.215 and zero rows above 20. Disc, shadow,
planet atmosphere and Saturn rings remain visible in the captures. Internal
sector and silhouette inspection was visual, not a comprehensive numeric
geometry proof.

The rectilinear path was not changed. It retains large joins in the synthetic
reference sphere (mean 43.114, 498 rows above 20), 240-radii left seam
(109.291, 525 rows), and earlier 240-radii primary/right pose (69.553,
286 rows at the left join). These are baseline findings, not new cylindrical
regressions. Their acceptability and cause remain open.

## Same-session yaw sweep

The corrected developer lane ran eleven authoritative yaw steps, -10 to
+10 degrees in increments of 2, in one three-window session. Each step
updated the same synthetic camera and black-hole targeting, then checked
session, fixture, frame revision and state key across all three windows.
Left and primary captures were hashed per step. This was a paced same-session
step sweep, not a continuous frame-rate video. Its manifest and metrics are
under `run-1cf835a89e074d5f8fe30846c4b99d84/metrics/`.

The yaw-zero seam was smooth. The sweep nevertheless found localized spikes:
yaw -6 had mean impulse 2.798, peak 99.104 and 34 rows above 20; yaw +4
had mean 0.654, peak 146.106 and five rows above 20. Native seam crops retain
the black-hole disc and shadow but show small edge discontinuities at these
poses. There was no observed missing whole monitor or sustained full-height
bloom line. A denser or continuous motion recording and the cause of these
localized spikes remain open, so the motion acceptance criterion is not met.

## Isolation and validation

The updated developer host stages only its explicit fixture payload and
uses disposable profile, log, capture, metric and temp paths. The production
journey store, reset path, installed payload lookup and selected Windows
screensaver were not invoked. Source checks are the isolation evidence;
there was no external owner-data access trace or owner-data hash comparison.
The ordinary release build rejected a developer motion flag with exit code 1.
An injected primary cylindrical initialization failure closed the entire
group and restarted rectilinear in a new session within the same sandbox;
that run exited 0. Universe typecheck, native developer and release builds,
JavaScript/Python syntax checks, matched manifest verification and
`git diff --check` passed. Build warnings about NuGet advisory access,
WindowsBase references and WinForms DPI configuration remain.

No installation, display configuration, production profile, journey, backup,
Git history or remote was changed. No Stage B or sustained performance work
was started. The native motion residual and rectilinear baseline joins block
a GO decision for Stage A2 completion.
