# Stage B1-R11 negative-yaw join root-cause isolation

Decision: NO-GO for adding the -107.994-degree sector to primary as a
correction for the yaw -6 peak. R11 isolates the peak as genuine
adjacent-ray lens variation at a black-hole caustic, not a cross-context
source mismatch or an angular-wrap break. B1 production remains NO-GO.

## Fixture and evidence

The final isolated run is
`development/runs/run-256c1931f6ea4d50a9f8166cd82edb8a`.
It used `hole-26-left`, frozen yaw -6 degrees, native bloom, the
512-pixel guard and the recorded 8320x2160 layout. The standard fixture
manifest and `forensics-*.json` contain the initial complete final PNGs
and intermediate stages. `negative-probe-*.json` contains the source
trace, temporary sector source validation, complete before/candidate/
restored final PNGs, complete before/candidate RGBA16F lens buffers and
nearby-yaw PNGs. `metrics/negative-probe-analysis.json` is the numeric
summary. `metrics/r11_join_zoom.png` is the inspected final-frame crop.

The probe uses exact virtual pixel centers x=3836..3843 and y=930..1149
for both contexts. Its trace-only clone records original camera ray,
deflected ray, source center and UV, validity branch, cube sample,
world cube direction and sector sample. The accepted shader is unchanged.
The source probe temporarily rendered the primary -107.993582-degree
sector using the existing left projection, then disposed it. A separate
candidate test temporarily put that sector into the primary ordinary
source list, captured full lens and final output, removed it, and
captured rollback. No sector was permanently allocated.

The native fixture verifier and forensic analyzer passed. State keys
matched across all three displays. No WebGL context loss was reported.

## Reproduced final-frame failure

The physical join is left x=3839 beside primary x=3840, over virtual
y=720..2159. At yaw -6 the encoded Rec. 709 adjacent luma step has
mean 3.475, 95th percentile 11.068, 61 rows above 20 and peak
107.212 at y=1033. The left final RGB at that row is `[146,108,74]`
(luma 113.624); primary is `[13,5,1]` (luma 6.412). The result matches
the R10 -6 measurement. The captured visual crop shows a sharp scene
edge crossing the physical join.

## Exact peak-row path

The table uses top-down virtual coordinates. Camera-space ray values
were read as half floats; analytic ray angles use the accepted
`CylindricalLens.rayDir` formula and physical pixel centers.

| Quantity at y=1033 | Left x=3839 | Primary x=3840 |
| --- | ---: | ---: |
| Original ray angle | -38.01054 deg | -37.98580 deg |
| Original ray XYZ, half-float | (-0.615234, 0.020065, -0.787598) | same rounded values |
| Deflected ray XYZ | (0.009079, -0.131226, 0.991211) | (-0.048553, -0.138306, 0.988770) |
| Deflected signed angle | +179.475 deg | -177.189 deg |
| Unwrapped angle | +179.475 deg | +182.811 deg |
| Ordinary chosen sector | -107.994 deg, left index 0 | -57.994 deg, primary index 0 |
| Ordinary sector UV | (-1.769, 0.118) | (1.777, 0.748) |
| Validity and actual path | Angle invalid, cube fallback | Angle invalid, cube fallback |
| Cube world ray | (-0.094543, -0.131226, 0.986816) | (-0.151611, -0.138306, 0.978516) |
| Cube sampled linear RGB | (0, 0, 0) | (0, 0, 0) |
| Accepted lens linear RGB | (0.235474, 0.132080, 0.069641) | (0.014618, 0.006752, 0.002405) |
| Final encoded RGB | (146, 108, 74) | (13, 5, 1) |

The analytic original ray moves only 0.02474 degree between physical
pixel centers. The deflected direction moves 3.336 degrees and crosses
the signed -180/+180 representation boundary. Neighboring unwrapped
deflected angles at x=3838..3841 are 175.901, 179.475, 182.811 and
185.932 degrees, with successive steps 3.574, 3.336 and 3.121 degrees.
The trace therefore shows a steep but continuous ray mapping across
the wrap, not a discontinuity in the angular formula. The half-float
trace rounds the two original XYZ vectors to the same values; the
analytic angle calculation resolves their real subpixel separation.

At both peak pixels, the lens background lookup returns black. The
accepted black-hole fragment accumulates disc emission in `col` and
outputs `col + trans * bg`. With sampled `bg` equal to zero here, the
large lens color change is attributable to the geodesic/disc-emission
path for genuinely adjacent rays. This is proven by the source trace
and shader code, while the exact individual disc crossing responsible
for the color change was not instrumented.

## First divergent stage and neighboring columns

The first materially divergent rendered color stage is the lens:

| Stage at y=1033 | Peak RGB-channel step, left x=3839 to primary x=3840 | Same-coordinate cross-context peak |
| --- | ---: | ---: |
| Unlensed | 0 | 0 |
| Lensed, linear | 0.220856 | 0 |
| Bloom bright | 0 | 0 |
| Bloom vertical mip 3 | 0 | 0 |
| Bloom composite | 0.002922 | 0.002945 |
| Post OutputPass | 0.518433 | 0.011719 |

The tiny bloom composite mismatch is downstream and cannot account for
the lens step. Output encoding and crop presentation make the lens
contrast prominent in the final PNG, but they do not introduce the
first large difference. The matched-coordinate lens strip is identical
between windows at the peak. The same lens shader evaluated offscreen
inside either context produces the same 0.220856 adjacent step for
x=3839 to x=3840.

In the actual final frame at y=1033, luma at x=3836..3843 is
117.246, 128.161, 123.429, 113.624, 6.412, 0, 0, 0. The
within-left adjacent steps immediately before the join are 10.915,
4.732 and 9.805; the first within-primary step is 6.412. Thus the
107.212 step is locally concentrated at the join in this frame, but
the identical offscreen lens result proves it does not require a
context boundary. Elsewhere in the nearby x=3700..3979,
y=800..1199 bands, ordinary internal adjacent luma steps exceed 100:
left peak 175.04 and primary peak 217.59. The caustic produces sharp
edges within individual windows as well.

## Controlled source-path parity

The existing left -107.994 source and temporary primary clone have
matching 3244x2675 RGBA16F projections, four samples and depth.
Their 4x4 grid of 128x128 color readbacks matches byte for byte:
`856D7C73B8EA33CA091D6504F4CFDE504918DF357A427E59F3708FA329CA1536`.
All 262,144 sampled alpha values are nonzero and 32 sampled pixels have
nonzero RGB. This verifies a rendered sector target, not an empty one;
it does not prove every unsampled source texel.

Across all 1,760 same-coordinate strip pixels, forced cube versus cube
has zero lens RGB difference between contexts. Forced -107.994 sector
versus sector also has zero difference; 294 pixels take a valid sector
branch in both contexts. Ordinary selection differs at only two
same-coordinate pixels in this strip, `(3842,973)` and `(3843,975)`,
with peak linear RGB-channel delta 0.036255. Those pixels involve a
cube-versus-sector star sample, away from the y=1033 peak. For every
sampled join row in y=930..1149, ordinary, forced cube and forced sector
give the same adjacent lens RGB step. At y=1033 the forced -107.994
sector is angle-invalid for both rays and takes the cube fallback, so
its exact sampled output equals forced cube and ordinary output.

The actual temporary primary-sector candidate left final join metrics
unchanged: 61 rows above 20 and the same 107.212 peak at y=1033.
It changed 169 primary frame pixels by more than one channel unit in
local x=2..65, y=191..422 (virtual x=3842..3905, y=911..1142),
including 104 pixels above 20 and a peak channel change of 228.
Visual review shows bright details removed near the primary edge.
Full lens buffers show changes away from the peak; the y=1033 lens
sample is unchanged. This candidate is not a safe correction. After
disabling it, complete left and primary frames each returned to within
one channel unit of their pre-candidate captures.

## Nearby negative yaw and remaining limits

The same disposable fixture captured complete ordinary final frames
at nearby negative yaws:

| Yaw | Rows above 20 | Peak luma | Peak virtual y |
| ---: | ---: | ---: | ---: |
| -8 | 1 | 20.34 | 1939 |
| -7 | 6 | 179.46 | 1024 |
| -6.5 | 28 | 142.97 | 911 |
| -6.25 | 34 | 138.57 | 951 |
| -6 | 61 | 107.21 | 1033 |
| -5.75 | 38 | 168.19 | 1094 |
| -5.5 | 21 | 166.05 | 1147 |
| -5 | 10 | 126.77 | 957 |
| -4.5 | 8 | 143.85 | 929 |
| -4 | 4 | 87.28 | 907 |

The sharp features move and change as yaw changes; the 61-row extent
is concentrated near -6, while high isolated peaks occur at adjacent
poses. This is a stepped frozen-yaw result, not a continuous-cadence
flicker test.

The detailed source probe covers 58 of the 61 rows above 20. Rows
y=1150 and 1151 are just outside it; the R10 full-height trace shows
no background lookup at those join pixels, consistent with disc-only
color, but their exact disc crossings were not traced. Row y=1981 is
far from the caustic strip; R10 shows both sides selecting the same
-57.994-degree sector with valid UV, but its exact source texels and
downstream path were not isolated. The two ordinary same-coordinate
star-sample differences at y=973 and 975 and small postprocess
same-coordinate differences are real secondary effects. They do not
explain the y=1033 peak. These bounded exceptions remain unverified.

## Recommendation and validation

Do not add the -107.994 sector to primary as a repair for the -6 peak.
The peak is an intrinsic high-gradient caustic at adjacent ray centers
under the accepted lens. A bounded next diagnostic, if smoother output
is required, is to compare subpixel lens sampling around this caustic
in an isolated render while retaining the accepted ray and emission
equations; it must measure final-frame output and resource cost. This
is a diagnostic proposal, not a production shader or guard change.
Separately, the negative-yaw source mismatch on some other rows can be
assessed only against complete final frames before selecting any sector
repair. B1 production remains NO-GO.

The developer .NET build and publish, isolated Vite build, `node --check`,
Universe typecheck, native fixture verifier, forensic analyzer, R11
analysis script and `git diff --check` passed. The production renderer
and accepted shader were not modified. No installation, owner-data
change, cleanup, commit, push or release was performed.
