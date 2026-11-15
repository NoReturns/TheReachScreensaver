# Stage B1-R12 zero-additional-target join feasibility

Decision: NO-GO for the tested primary-side cube-to-sector join policy.
It lowers part of the positive-yaw physical-join error, but it does not
match the R9 visual reference and introduces new errors at yaw 0 and
negative yaw. B1 production remains NO-GO. This is a developer-only
result, not a production selector or shader change.

## Fixture and method

The valid isolated 8320x2160 run is
`development/runs/run-2f7c546d25e14ed68fbeba1aa816607c`.
`metrics/zero-target-*.json` records all complete final PNG captures,
states, policy settings and 48-sample cost measurements. The derived
`metrics/zero-target-analysis.json` records paired full-frame differences.
`metrics/r12_join_montage.png` shows the final join at yaw +6.815 for
ordinary rendering, 64 pixels, 128 pixels and the R9 reference. The
R9/R10 reference is the frozen extra-left-sector run
`development/runs/run-98efc64b8cfb4ac88e7ddd00a432d6ad`.

The host used the frozen `hole-26-left` state, native bloom, the accepted
512-pixel guard and ordinary source lists. The developer fixture cloned
only the primary final lens material. At virtual x=3840 it samples the
already rendered cube; `smoothstep` transitions to the existing primary
sector at x=3904 or x=3968. Accepted ray equations and source projections
were unchanged. The left and right contexts were unchanged. No scene
render pass or perspective target was added. The material clone shares
the live black-hole uniforms with the accepted material; this is needed
to preserve yaw-dependent lens state. Width 0 restores the ordinary
material. The fixture captures complete final frames after each yaw
update and after rollback.

An initial run, `run-ffe4da5588c34cc294ede05ae03a56a5`, exposed a
fixture error: a plain material clone froze the black-hole uniform values.
Its large cross-frame differences are invalid as policy evidence. The
corrected run above shares those uniform objects. The invalid run was
retained as evidence and was not used for the decision.

## Full-frame physical join

The join statistic is absolute luma difference between the left final
pixel x=3839 and primary final pixel x=3840 over y=720..2159. Counts
show rows above 20 luma units. Both widths have the same join result
because the boundary weight is the same.

| Yaw | Ordinary mean / rows | 64 or 128 mean / rows | R9 reference mean / rows | Candidate peak |
| --- | ---: | ---: | ---: | ---: |
| -6 | 3.475 / 61 | 4.181 / 78 | 3.475 / 61 | 107.212 |
| 0 | 1.027 / 0 | 1.637 / 7 | 1.027 / 0 | 36.150 |
| +6.4 | 2.176 / 24 | 2.611 / 18 | 1.220 / 5 | 167.801 |
| +6.815 | 2.917 / 59 | 1.739 / 17 | 0.967 / 3 | 67.261 |
| +7.0 | 1.925 / 18 | 1.123 / 5 | 0.869 / 2 | 64.193 |

At the original y=1034 failure, yaw +6.815 falls from 47.269 to 7.856;
R9's extra-left-sector reference had 1.928 in the R9 frozen run. The
candidate removes 42 of 59 rows above 20 at that yaw, but retains 17,
including 10 above 40. At +6.4 the maximum rises from 46.988 to 167.801,
despite a smaller count above 20. At yaw 0 it creates seven above-20 rows
where ordinary rendering and R9 have zero. The intrinsic yaw -6 caustic
peak remains 107.212, while the candidate adds 17 above-20 rows. These
are final-output measurements, not lens-strip agreement.

## Motion and displaced artifacts

The sweep captured -7, -6.5, -6, -5.5, -4, 0, 2, 4, 5.5, 6, 6.2, 6.4,
6.6, 6.815, 7, 7.2 and 7.5 degrees for each policy. All three contexts
agreed on the fixture state key at each yaw. The 64-pixel candidate has
more above-20 join rows than ordinary at 10 of the 17 positions. It
crosses from worsening at +6.2 (8 to 20 rows), to improvement at +6.4
(24 to 18), then worsens again at +7.2 (3 to 7). This changing edge
error is a visible risk for popping during continuous camera motion.
The captures are discrete stepped motion, so they do not establish a
frame-rate flicker frequency.

The corrected candidate's above-1-channel differences from ordinary
rendering are confined to the primary-edge region: maximum changed
local x=60 for width 64 and x=122 for width 128 across the tested yaws.
No changed pixel was found beyond width+32. At +6.815, width 64 changes
9,500 primary pixels, including 1,206 pixels within the 64-pixel strip
by more than 20 RGB units. Width 128 changes 14,716 pixels, including
1,438 above 20 in its strip. The full-frame and montage images show a
second displaced narrow arc beside the black-hole rim in the blend
region. This is a screen-fixed blending/ghosting artifact: the affected
x range stays attached to the primary boundary as the camera yaw moves.
No new high-contrast vertical edge was measured exactly at x=3904 or
x=3968, but the error is spread through the transition strip. Widening
from 64 to 128 pixels does not improve physical-join statistics and
alters more of the black-hole image.

## GPU work, allocation and rollback

All policies recorded 142 draw calls, three perspective targets, 17
renderer-reported textures and seven geometries on the primary display.
The left display also stayed at 142 calls and three targets. Additional
perspective-target allocation is 0 MiB, compared with R9's 99.31 MiB
estimate for one 3244x2675 RGBA16F multisample target. The candidate
does add a compiled lens material/program and texture sampling inside
the strip; the browser does not expose its exact driver memory. Median
primary `engine.render` plus `gl.finish` samples were 0.60 ms ordinary,
0.50 ms at 64 and 0.55 ms at 128. The ordering is within measurement
noise and is not evidence of a rendering speedup. The first eight renders
were warmups; each reported median uses 48 samples.

After width 128 the fixture restored width 0 and yaw +6.815. Restored
frames differ from the initial ordinary captures by at most one RGB
unit per channel; PNG hashes therefore differ. The initial ordinary
frames also match R10 ordinary frames at these five key yaws to at most
one channel unit. Source lists, target counts and installed state were
not changed.

## Recommendation and limits

NO-GO. Do not advance this cube-to-sector blend as a bounded dynamic
selection implementation. It improves one original row but produces
new join errors at yaw 0 and -6, raises peaks at nearby positive yaws,
and ghosts the black-hole rim inside a screen-fixed strip. The 128-pixel
width spends more image area without improving the boundary result.
R9's matching sector representation remains the stronger visual
reference, though its resource cost and R10 motion limits remain open.
No production selector, allocation policy, shader or renderer was changed.

Validation: developer host `dotnet publish` passed; Universe `npm run
build` and isolated Vite fixture build passed; the isolated host run
completed without reported context loss; all 17 state keys matched
across displays; `analyze_b1_r12.py` produced the paired final-frame
metrics. The browser capture and discrete yaw sequence do not establish
owner visual acceptance or long-duration continuous-motion behavior.
