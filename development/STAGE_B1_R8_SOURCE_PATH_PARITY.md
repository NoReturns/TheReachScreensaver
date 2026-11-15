# Stage B1-R8 cross-context lens source-path parity

Decision: representation mismatch is the likely cause of the exact-yaw B1
join discontinuity. R7's NO-GO for adding the -107.994-degree sector stands.
B1 rendered-frame acceptance remains NO-GO. No production repair was made.

## Evidence and controls

The final isolated run is `development/runs/run-059c4fdea4d64ea3b733324ef5b897a1`.
Its primary evidence is
`metrics/source-probe-94ec7ff23e6e4674ae8b7425231b6f52.json`.
The fixture and forensic manifests in that root retain the physical captures,
state and control hashes. The same fixture as R7 fixes yaw +6.815 degrees,
camera quaternion `[0,0.05943704235540271,0,0.9982320561853552]`,
composition shift 1216, material shader time 1234, black-hole time 0,
the `hole-26-left` scene, native bloom, the 512-pixel guard and the
8320x2160 virtual layout. Both windows have the same fixture state key.
The probe compares absolute virtual pixel centers x=3838..3841,
y=970..1069. This is a deterministic frozen reconstruction, not the
historical continuous frame.

The developer-only hook in `../Universe/development/cylindrical-stage-a1/view.js`
uses a 4x100 offscreen RGBA16F lens target. It forces the background lookup
to the cube for both contexts, then binds only the +42.0064175-degree
perspective sector for both contexts. The left sector is rendered temporarily
with the same scene and cloned projection camera; primary uses its existing
sector. The accepted lens ray, geodesic, emission, transmittance and ordinary
renderer path are unchanged. The hook restores uniforms, screen material,
tone mapping, auto-clear and render target in `finally`, and disposes the
temporary target and diagnostic materials. Neither context was lost.

The ordinary default-framebuffer SHA-256 before and after the probe matched
in each context: left
`B7BCD1D8599BBB18F6FB7B7D902CF45DA9A0B00D690BB17404DF24BA76043012`,
primary
`67A5CADA268F2326F6960A106CF1497541443F222140B8833A7D8C902D2D13E9`.
These are the same hashes reported by R7. The left repeated-render control
still differs, as R7 documented; before/after the bounded probe matches.
At physical join y=1034, the ordinary encoded output is left RGBA
`[204,203,204,255]` versus primary `[152,156,168,255]`, a 47.2688
Rec. 709 weighted 8-bit step. The R6/R7 evidence established the first
large divergence at the lens stage; this run's forensic analyzer reports
raw lens peak 0.72039795 and output peak 0.25195312.

## Forced-path result

The following hashes cover the complete 4x100 half-float lens RGB strip.

| Context | Ordinary selection | Forced cube | Forced +42 sector |
| --- | --- | --- | --- |
| Left | `F6F3CEC2...E872457B2E` | `F6F3CEC2...E872457B2E` | `09757D02...C50F1E78C212` |
| Primary | `09757D02...C50F1E78C212` | `F6F3CEC2...E872457B2E` | `09757D02...C50F1E78C212` |

The full hashes are in the source-probe JSON. Across 400 matched virtual
pixels, forced cube differs between contexts by exactly zero half-float
units, as does forced sector. Ordinary left equals forced cube and ordinary
primary equals forced sector byte for byte. Cube versus sector within either
context has mean per-pixel maximum RGB-channel difference 0.1808486255 and
peak 0.7203979492. The ordinary cross-context difference has exactly the
same mean and peak, and all 400 pixels differ. This is direct evidence that
the lens-stage context difference is entirely accounted for by source
representation in the probed rectangle.

At x=3839, y=1034, both contexts trace the same camera-space deflected
direction `(0.728516,-0.160034,-0.665527)` and cube lookup world direction
`(0.644531,-0.160034,-0.747070)`. Each traces cube sample linear RGB
`(0.319092,0.295410,0.255127)`. With the +42 sector forced, both use UV
`(0.569824,0.358887)` with valid angle and UV, and both sample linear RGB
`(0.002333,0.002211,0.001968)`. The complete lens RGB is
`(0.316162,0.292725,0.252686)` with cube and
`(0.002310,0.002190,0.001950)` with sector in either context.
At adjacent x=3840, y=1034, both contexts trace deflected direction
`(0.739258,-0.161011,-0.653320)`, world cube direction
`(0.656738,-0.161011,-0.736328)`, cube sample
`(0.241455,0.223755,0.193359)` and sector UV
`(0.581543,0.357666)`. The sector sample is
`(0.001153,0.001094,0.000976)`. The same-coordinate direction,
world cube direction, cube sample, sector UV, sector sample and lens RGB
have maximum cross-context difference zero at these pixels; their trace
maximum is zero over all 400 pixels. Adjacent x=3839/3840 rays naturally
differ because they are different physical pixel centers.

The temporary left +42 source and existing primary +42 source each use
3244x2675 RGBA16F, four samples and a depth attachment. Their 16-element
projection matrices are numerically identical; the source-center angles
match at 0.733150292618528 radians. The 4x4 grid of 128x128 source colour
readbacks matches byte for byte, SHA-256
`DDA8038220FBADCCFC8CCF114FE43C9955A5DA0BCAE22DA6BA55D66CDC709964`.
All 262,144 sampled texels have nonzero alpha and 23 have nonzero RGB.
The left source render issued 14 draw calls. All 400 forced-sector trace
lookups have valid angle and UV and matching sampled linear RGBA. Depth
attachment configuration was verified, but individual depth texels were
not read back; this limits claims about depth contents. The exact sampled
sector colour and resulting lens RGB nevertheless match across contexts.

## Failing rows and nearby controls

Values below are peak absolute linear RGB-channel differences between the
adjacent physical join pixels x=3839 left and x=3840 primary. These are
not comparable directly with the encoded 47.2688 output step.

| Virtual y | Ordinary | Both cube | Both sector | Interpretation |
| ---: | ---: | ---: | ---: | --- |
| 990 | 0.043121 | 0.005585 | 0 | Nearby lower-intensity control still follows representation choice. |
| 1020 | 0.389282 | 0.259033 | 0.062714 | Strong local source variation remains even with a matched path. |
| 1030 | 0.518021 | 0.023926 | 0.002579 | Representation dominates. |
| 1033 | 0.361286 | 0.078857 | 0.001587 | Representation dominates. |
| 1034 | 0.315016 | 0.076050 | 0.001163 | Failing join row. |
| 1035 | 0.274109 | 0.069580 | 0.000851 | Same pattern below join row. |
| 1040 | 0.164551 | 0.011719 | 0 | Nearby control still follows representation choice. |
| 1060 | 0.051086 | 0.007843 | 0 | Lower-intensity control. |

At y=1034, the same-ray cube-versus-sector difference on left x=3839 is
0.313852 in the peak RGB channel, versus the observed 0.315016 adjacent
lens step. Thus the representation difference accounts for about 99.6%
of that channel's adjacent step; the remaining 0.001163 is the adjacent
sector-path variation. Across all 100 rows, mean adjacent peak-channel
difference is 0.178076 ordinarily, 0.055176 with both cube and 0.006869
with both sector. This row calculation is descriptive, not a claim that
the encoded output would have those values after a production change.

## Cause and minimal repair design

Proven by the bounded probe: both contexts calculate equivalent deflected
rays and identical results for each matching lookup branch. The normal
left lens lacks the +42 sector and chooses its cube fallback; primary has
that sector and uses it. The cube and perspective sector contain materially
different linear RGB for the same ray. The first divergent operation is
source selection at `CylindricalLens.lookupDirection`, followed by sampling
`uFallbackCube` versus `uSources`. This is a representation mismatch, not
inconsistent data within an identical sampling path.

One minimal design for a later isolated repair pilot is to make
`CylindricalRenderer.ensureSectors` account for the deflected ray envelope
of each window's padded lens rectangle and add only the +42 sector to the
left window when that envelope needs it. The sector itself and projection
already reproduce primary byte for byte in this probe. The pilot would
need to bound the extra 99.31 MiB colour-plus-depth target estimate,
validate all envelope edges and motion, and check that sector selection
does not move a discontinuity elsewhere. This is a proposal, not an
implemented repair or B1 acceptance.

## Validation and scope

`node --check` and Universe typecheck passed. The isolated Vite fixture
build passed after an initial sandbox `spawn EPERM` retry. The first
sandboxed developer launches failed during WebView2 controller creation;
the isolated escalated launch completed. A preliminary successful probe
revealed stale uniforms in the forced cube clone, which were corrected
before the final run. Fixture and forensic analyzers then passed;
`git diff --check` found no whitespace errors (only pre-existing
Screensaver line-ending warnings). Both repositories retained their
pre-existing dirty top-level status. The only source change is the
developer fixture hook; the production renderer, accepted shader,
512-pixel guard, installation and owner data were not changed. No cleanup,
commit, push, B2 or B3 work was performed.
