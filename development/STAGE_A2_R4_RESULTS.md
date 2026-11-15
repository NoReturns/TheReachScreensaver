# Stage A2-R4 virtual-coordinate bloom consistency

Decision: REVISE for Stage A2 completion. This is a developer-only diagnostic
prototype. Stage B, installation, and release work remain unauthorized.

## Root cause and correction boundary

Code-proven: `CylindricalRenderer.resize` originally sized a separate composer
and `UnrealBloomPass` from each display's padded rectangle. The installed
Three.js pass starts bright extraction at half size, rounds each of five mip
dimensions independently, and blurs with local inverse-target-size offsets.
The R3 readback showed 136x68 versus 112x61 horizontal mip 4 targets at the
left/primary seam. Equal bloom parameters do not give equal absolute-grid
samples when those rectangles have different sizes and origins.

R4 adds `alignedBloomRect` and a developer fixture `grid` option. The option
snaps each post rectangle to a common 32-physical-pixel grid, including an
aligned fringe outside the virtual desktop where its dimensions require one.
At the tested native layout, left, primary, and right mip-4 sizes are 136x68,
112x61, and 76x50; their cell origins share one absolute coordinate phase.
The native and `guard128` paths remain available as controls. The option
requires one output pixel per physical display pixel. Rectilinear rendering
does not select it.

Code-proven: a fifth-mip bloom contribution may reach approximately 1094
physical pixels horizontally or vertically from a bright source before
interpolation margins. The nominal tap supports along one axis are 5 at mip 0,
9 at mip 1, 13 at mip 2, 17 at mip 3, and 21 at mip 4. Multiplying by their
2, 4, 8, 16, and 32-pixel scales gives 10+36+104+272+672=1094 pixels.
This is a conservative support estimate, not a measured perceptual radius.
The R3 yaw -6 left-only bright band at x=2560..3327 contributes into the
shared band by vertical mip 3. A 512-pixel primary guard starts at x=3328.
The grid option cannot manufacture that missing source. A correct source
extension requires a separate coarse bright-source render and corresponding
sector coverage, or more than 512 full-resolution guard pixels. R4 did not
add that extra rendering or a full-resolution panoramic HDR atlas.

The measured grid correction was small and mixed. At 512 pixels and yaw +4,
horizontal mip-4 peak same-coordinate difference fell from 0.00125114 to
0.00074387 linear RGB, but final composite peak rose from 0.00021744 to
0.00024414. At yaw -6 the final composite peak fell only from 0.00294569 to
0.00292191. At yaw 0 it rose from 0.00214767 to 0.00226307. Thus phase
alignment alone does not resolve the complete cross-context bloom difference.
The residual source and interpolation effects are not fully isolated.

## Exact native matched-state evidence

All 18 frozen runs used the 26-radii black-hole fixture, one authoritative
state key per yaw, the 8320x2160 virtual desktop, and physical displays of
3840x2160 at (0,0), 2560x1440 at (3840,720), and 1920x1080 at (6400,1080).
Their separate 1.5/1/1 DPR WebViews reported matching physical output sizes.
The host verified the same session, state key, and frame revision across the
three windows. The capture verifier checked file hashes and dimensions.
`STAGE_A2_R4_MATRIX.csv` gives every run ID, guard, yaw, mean, p95, p99,
peak, hard-row count, raw HDR, bright, ten individual blur stages, composite,
and OutputPass same-coordinate peak difference. The per-run forensic JSON
retains the full 420-row, four-column readbacks and bright-band probes.

At 512 physical pixels, the original versus grid stage peaks were:

| Yaw | Mode | Bright | H4 | V4 | Composite | OutputPass | Mean impulse | p95 | p99 | Peak | Rows >20 |
| --- | --- | ---: | ---: | ---: | ---: | ---: | ---: | ---: | ---: | ---: | ---: |
| -6 | native | 0 | 0.010785 | 0.005398 | 0.002946 | 0.015152 | 2.798 | 14.733 | 68.815 | 99.104 | 34 |
| -6 | grid | 0 | 0.010748 | 0.005354 | 0.002922 | 0.015030 | 2.798 | 14.733 | 68.815 | 99.104 | 34 |
| 0 | native | 0 | 0.008829 | 0.003935 | 0.002148 | 0.009094 | 1.125 | 2.000 | 2.000 | 2.000 | 0 |
| 0 | grid | 0 | 0.008646 | 0.004124 | 0.002263 | 0.009583 | 1.181 | 2.000 | 2.358 | 2.464 | 0 |
| +4 | native | 0 | 0.001251 | 0.000333 | 0.000217 | 0.000977 | 0.654 | 0.892 | 1.073 | 146.106 | 5 |
| +4 | grid | 0 | 0.000744 | 0.000372 | 0.000244 | 0.000977 | 0.637 | 0.892 | 1.073 | 146.464 | 5 |

Raw unlensed and lensed HDR peaks were zero at all six 512-pixel conditions.
At yaw -6, H0 through H3 and V0 through V3 also matched in the seam strip;
H4 was the first local disagreement. At yaw +4, H1 first differed by about
0.0000001, and H4 was the first material difference. Bright source coverage
outside the seam strip remains the measured R3 yaw -6 cause.

The adjacent-pixel peak compares two different scene coordinates. It must
not be interpreted as a hard join. At yaw +4 the silhouette edge crosses the
physical seam in both contexts. The same-coordinate OutputPass difference is
about 0.000977, while the adjacent-pixel image peak is about 146 RGB units.
Yaw -6 likewise has a genuine small bloom residual amid a much larger disc
and silhouette gradient. The inspected native crops show no persistent
vertical brightness line. This is visual diagnostic evidence, not owner
acceptance.

## Guard versus quality

The mean impulse / rows above 20 at yaw -6, 0, +4 were:

| Guard | Native -6 | Grid -6 | Native 0 | Grid 0 | Native +4 | Grid +4 |
| ---: | ---: | ---: | ---: | ---: | ---: | ---: |
| 128 | 2.822 / 34 | 2.822 / 34 | 12.168 / 225 | 12.112 / 218 | 26.122 / 353 | 26.022 / 353 |
| 256 | 2.823 / 34 | 2.822 / 34 | 12.032 / 222 | 11.976 / 216 | 5.254 / 5 | 5.314 / 5 |
| 512 | 2.798 / 34 | 2.798 / 34 | 1.125 / 0 | 1.181 / 0 | 0.654 / 5 | 0.637 / 5 |

At yaw 0 and +4, 128 and 256 have materially larger equivalent-pixel
OutputPass differences, up to 0.098 and 0.234 at 128, and 0.098 and 0.050
at 256. The grid does not make either guard acceptable. At yaw -6 all guards
are similar in the seam metric because the high rows follow scene edges; the
equivalent-pixel output difference remains about 0.015 to 0.020. No guard
above 512 was tested.

## Visual and motion regression

Ten native control/grid fixture pairs were captured: 26-radii at the left
seam, 240-radii left, 26-radii primary/right, 26-radii away, galaxy at a
sector boundary, galaxy at the physical left seam, Earth atmosphere, Saturn
rings, 15-degree roll, and rearward direction. The grid 26-radii left
fixture at yaw 0 is in the frozen matrix; the other nine pairs have separate
run roots. Every pair passed fixture manifest and capture SHA-256 checks.
The 240-radii left mean impulse was 0.628 native versus 0.626 grid, with zero
hard rows in both. The 26-radii primary/right seam was 0.196 versus 0.193,
again with zero hard rows at that physical join. Galaxy physical-seam mean
was 0.004 in both. Earth and Saturn left seam means were 0.508 native versus
0.505 grid, zero hard rows. Images retain lensing, disc, shadow, galaxy,
atmosphere, and rings. Inspection did not find clipped or disconnected
objects. The two high rows in the roll fixture and four in the 26-radii
primary/right fixture are left-seam adjacent-pixel edge findings, not a
verified new physical join.

Separate 11-step native yaw sequences from -10 to +10 in 2-degree steps
passed session and state checks. The grid sequence closely followed the
control: yaw -6 was 2.798 mean / 34 high rows for both, yaw 0 was 1.125 / 0
control and 1.181 / 0 grid, yaw +4 was 0.654 / 5 control and 0.639 / 5 grid.
Inspection of frozen captures at those poses found no hard full-height line.
The sequence was paced still capture, not continuous video. It cannot certify
absence of inter-frame flicker or bloom popping at intervening yaw values.

Before/after native image pairs are retained here:

- Yaw -6: `run-a6f1252dabea48afbe2390a3c7a57d8a` native and `run-446240254cf74a8a9c16db18311de3b4` grid.
- Yaw +4: `run-1d500ea2f18343469b4ee8811b592d44` native and `run-4c30041459ae49d686f247d4d8a5a101` grid.
- 240-radii: `run-da29bee2131c41b79c55a745fc9ad594` native and `run-8ff461f5dbb542608434c43880d1a184` grid.

Each root contains original monitor PNGs, `captures/assembled.png`, seam
crops, the fixture manifest, and visual analysis. Frozen 26-radii runs also
contain forensic readbacks and stage analysis.

## Calculated resources and timing

All values below are calculated target payloads, not measured GPU residency.
The estimate uses 12 bytes per sector color/depth pixel, 8 bytes per composer
and bloom RGBA16F pixel, six 512x512 cube faces per display, and two composer
targets. It omits driver overhead, multisample storage, other textures, and
some transient allocations.

| Mode | Guard | Sectors L/P/R | Post targets P,L,R | Sector MiB | Cube MiB | Composer MiB | Bloom MiB | Total MiB |
| --- | ---: | --- | --- | ---: | ---: | ---: | ---: | ---: |
| native | 128 | 2/3/1 | 2816x1568,3968x2160,2048x1208 | 595.85 | 54.00 | 235.91 | 108.05 | 993.81 |
| grid | 128 | 2/3/1 | 2816x1568,3968x2176,2048x1216 | 595.85 | 54.00 | 237.12 | 108.61 | 995.58 |
| native | 256 | 3/3/1 | 3072x1696,4096x2160,2176x1336 | 695.16 | 54.00 | 258.86 | 118.56 | 1126.58 |
| grid | 256 | 3/3/1 | 3072x1696,4096x2176,2176x1344 | 695.16 | 54.00 | 260.12 | 119.14 | 1128.42 |
| native | 512 | 3/3/2 | 3584x1952,4352x2160,2432x1592 | 794.47 | 54.00 | 309.27 | 141.65 | 1299.38 |
| grid | 512 | 3/3/2 | 3584x1952,4352x2176,2432x1600 | 794.47 | 54.00 | 310.62 | 142.27 | 1301.36 |

At 512, the grid costs 1.98 calculated MiB more than control. Mip-4 targets
are 112x61 primary, 136x68 left, and 76x50 right in both modes; dimensions
match but grid origins differ. At 128/256/512 the yaw-zero sector target
counts are 6/7/8 across displays. The black-hole cube adds 18 scene renders
per frame, giving 24/25/26 scene submissions before postprocessing.
No reliable native GPU timer or measured GPU residency was obtained. There
were no WebGL context losses or allocation failures in the successful native
runs. One sandbox-restricted WebView2 controller launch failed before capture;
fresh outside-sandbox, disposable runs succeeded. No performance improvement
is claimed.

## Rectilinear baseline and safety

The rectilinear `sphere-seam` fixture ran unchanged. Its visible baseline join
remains mean impulse 43.114 with 498 high rows. The previously recorded
240-radii rectilinear joins also remain separate unresolved release issues.
They were not repaired in R4.

The ordinary developer cylindrical bootstrap with no fixture passed
three-display group readiness at its unchanged 128-pixel default. Injecting
a primary initialization failure closed the cylindrical group and restarted
all three windows in rectilinear mode in a new session. The disposable native
host used staged assets, a run-specific WebView2 profile, logs, captures, and
temp directory. Source inspection found no route from this lane to the
installed app, selected screensaver, production journey/checkpoints, owner
profiles, settings, caches, reset path, or deployment backups. No owner-data
access trace or owner-data hash comparison was performed.

Universe typecheck and both developer payload builds passed. The native
developer build and publish passed with the existing NuGet advisory access,
WindowsBase reference, and WinForms DPI warnings. Native captures and
forensic manifests passed verification. `git diff --check` passed in both
repositories. After the final physical-pixel guard was added, both payloads
were rebuilt and a fresh yaw +4 grid run (`run-f5a6b788854d439da787d4f5058616dc`)
reproduced the earlier native metrics and passed readiness and capture checks.
Neither repository was committed or pushed.

## Remaining decision

The shared grid is implemented and measured, but its final effect is too
small and mixed to select it as a bloom correction. The 512-pixel guarded
control remains the better development baseline on present evidence. Its
yaw -6 coarse-source residual is small in the inspected frozen images, but
the full scene-source coverage has not been provided, and paced captures do
not close temporal acceptance. A future source-coverage prototype should
target coarse bright samples without duplicating a full-resolution HDR atlas
and measure the extra sector/render cost. Stage A2 stays open. Stage B stays
unauthorized.
