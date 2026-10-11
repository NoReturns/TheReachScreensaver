# Reach imagery v1 provenance

The optional enhanced batch covers Moon, Mars, Io, Tethys, Dione, Iapetus,
Titan, Enceladus, Europa, Mercury, Rhea, Venus, Ceres, Vesta, and Pluto.
Earth was pinned earlier. The source manifest records 29 batch JPEGs. The 25
eligible batch JPEGs are hosted at
https://github.com/NoReturns/TheReachScreensaver/releases/tag/reach-imagery-v1 .
`reach-assets.manifest.json` records exact release URLs, attribution, usage
notes, byte sizes, dimensions, and SHA-256 hashes. The staged preparation
records are in `C:\Storage\ReachImageryStaging` and its `phase4gd` and
`phase4ge` subfolders.

The public app loader excludes the two Ceres and two Mercury records until
Ceres redistribution rights and Mercury JPEG provenance are established. It
offers 26 files across 14 destinations. Bundled or procedural rendering remains
available for both bodies; existing local files are not deleted.
On 2026-10-11, GitHub Release metadata matched the names, sizes, and SHA-256
digests of all 25 offered batch files. This does not establish redistribution
rights or prove a complete user download. On 2026-10-11, the Release asset
list no longer contained either Ceres or Mercury file; uncached direct HEAD
requests for all four returned HTTP 404. The source manifest retains their
records for provenance, and the public loader excludes them.
The Release also hosts six Phobos, Deimos, and Mimas JPEGs outside the v1.0
app manifest. They are not included in the offline package or the app's public
download offer. Their Phase 4G-F source and approved output records are in
`C:\Storage\ReachImageryStaging\phase4gf\phase4gf-manifest.json` and
`AUDIT.md`. On 2026-10-11, the six GitHub Release names, sizes, and SHA-256
digests matched the approved outputs; all three local source files also
matched their recorded hashes. This verifies file identity through Release
metadata, not a full remote download or in-app arrival test.

| Body | Staged source file | Original download URL | Credit and limitations |
| --- | --- | --- | --- |
| Phobos | `phobos-pds-stooke-viking-mro-cyl.jpg` | https://sbnarchive.psi.edu/pds3/multi_mission/MULTI_SA_MULTI_6_STOOKEMAPS_V3_0/document/m1phobos/phobos_cyl_cor_control.jpg | Philip Stooke, NASA PDS Stooke Small Bodies Maps V3.0. Grayscale photomosaic reprojected from Cornell to DLR control; the match is approximate and photometric joins remain visible. |
| Deimos | `deimos-pds-stooke-viking-mro-cyl.jpg` | https://sbnarchive.psi.edu/pds3/multi_mission/MULTI_SA_MULTI_6_STOOKEMAPS_V3_0/document/m2deimos/deimos_cyl_viking_mro.jpg | Philip Stooke, NASA PDS Stooke Small Bodies Maps V3.0. Viking and Mariner 9 mosaic with selected MRO HiRISE detail; genuine sharpness varies by region. |
| Mimas | `mimas-cassini-2017-global.jpg` | https://assets.science.nasa.gov/dynamicimage/assets/science/psd/photojournal/pia/pia17/pia17214/PIA17214.tif?w=5760&h=2880&fit=clip | NASA/JPL-Caltech/Space Science Institute, Cassini Global Map - June 2017, PIA17214. The requested TIFF endpoint returned a 5760 x 2880 JPEG; mosaic joins and variable sharpness remain. |

The six 2K and 4K exports were made with Pillow Lanczos resizing, without
recoloring, inpainting, or synthesized detail. They use JPEG quality 94,
4:4:4 sampling, and an embedded sRGB profile. The [PDS source guide](https://sbnarchive.psi.edu/pds3/multi_mission/MULTI_SA_MULTI_6_STOOKEMAPS_V3_0/document/00_map_guide.html)
states that the Stooke maps are public domain with proper credit and intended
for visualization, not photometric analysis. Required citation: Stooke, P.,
Stooke Small Bodies Maps V3.0, MULTI-SA-MULTI-6-STOOKEMAPS-V3.0, NASA
Planetary Data System, 2015. The [NASA Mimas source page](https://science.nasa.gov/photojournal/mimas-global-map-june-2017/)
gives the credit above. Reuse is subject to [NASA media guidance](https://www.nasa.gov/nasa-brand-center/images-and-media/)
and [JPL image policy](https://www.jpl.nasa.gov/jpl-image-use-policy/),
including source credit and no implied endorsement.

| Body | Source or mission product | Interpretation and known limits |
| --- | --- | --- |
| Moon | https://svs.gsfc.nasa.gov/4720/ | LROC observed colour with lower resolution LOLA polar fill; 4K only. |
| Mars | https://astrogeology.usgs.gov/search/map/mars_viking_colorized_global_mosaic_232m | Viking colourized global MDIM 2.1; colour detail is coarser than the monochrome base. |
| Io | https://astrogeology.usgs.gov/search/map/io_galileo_ssi_voyager_color_merged_global_mosaic_1km | Galileo/Voyager colour merge; poles are interpolated and regional resolution varies. |
| Tethys | https://science.nasa.gov/photojournal/color-maps-of-tethys-2014/ | Cassini UV/visible/IR enhanced colour, not natural visible colour. |
| Dione | https://science.nasa.gov/photojournal/color-maps-of-dione-2014/ | Cassini UV/visible/IR enhanced colour, not natural visible colour. |
| Iapetus | https://science.nasa.gov/photojournal/color-maps-of-iapetus-2014/ | Cassini UV/visible/IR enhanced colour, not natural visible colour. |
| Titan | https://science.nasa.gov/resource/titan-mosaic-the-surface-under-the-haze/ | Cassini 938 nm near-infrared surface reflectance in grayscale RGB; not a visible-colour view. |
| Enceladus | https://science.nasa.gov/photojournal/color-maps-of-enceladus-2014/ | Cassini enhanced colour; visible longitudinal seam. Owner approved with this limitation. |
| Europa | https://astrogeology.usgs.gov/search/map/europa_voyager_galileo_ssi_global_mosaic_500m | Galileo/Voyager grayscale; south-polar no-data and abrupt mosaic boundaries. Owner approved with these limitations. |
| Mercury | https://astrogeology.usgs.gov/search/map/mercury_messenger_mdis_basemap_enhanced_color_global_mosaic_665m | MESSENGER enhanced colour, not natural colour; north-polar no-data. The USGS catalogue names Applied Coherent Technology Corporation as primary author, NASA/JHUAPL/Arizona State University/Carnegie Science as originators, and asks for author citation. The staged 11520 x 5760 JPEG has a recorded hash, but its exact acquisition URL and derivation from the catalogue's 23040 x 11520 TIFF remain unverified. Owner approved the visual limitation. |
| Rhea | https://science.nasa.gov/photojournal/color-maps-of-rhea-2014/ | Cassini enhanced colour; visible longitudinal seam. Owner approved with this limitation. |
| Venus | https://astrogeology.usgs.gov/search/map/venus_magellan_global_c3_mdir_synthetic_color_mosaic_4641m | Magellan radar-derived surface, fully simulated colour. It is neither visible atmospheric imagery nor natural observed surface colour. |
| Ceres | https://dawngis.dlr.de/data/Ceres/mosaic_ceres.php | Dawn HAMO controlled clear-filter grayscale mosaic; dark or missing southern polar coverage and varying illumination. DLR redistribution terms were not explicit on the data page. |
| Vesta | https://astrogeology.usgs.gov/search/map/vesta_dawn_fc_hamo_global_mosaic_60m | Dawn FC HAMO grayscale mosaic; northern polar gap and minor documented gap filling. |
| Pluto | https://science.nasa.gov/resource/pluto-global-color-map/ | New Horizons MVIC three-filter processed colour mosaic; large unobserved southern region remains black. Owner accepted this limitation. |

The source and export recipes, native dimensions, colour processing, coverage
checks, and exact original download URLs are in the named staging JSON files
and `phase4gd/reach-imagery-batch-4gd-audit.md` and
`phase4ge/phase4ge-audit.md`. The Mercury source JPEG
provenance remains the exception noted above. No polar data or seam repair was
invented during export. The 2K and 4K files are separately pinned so the web
renderer can choose a tier based on physical viewport width, with a verified
alternate tier when the preferred one is absent. The host never downloads
imagery during playback.

NASA image reuse guidance: https://www.nasa.gov/nasa-brand-center/images-and-media/ .
Credit NASA and the named mission and processing teams; no endorsement is
implied. The cited USGS catalog records mark their products public domain or
without use constraints and request source citation where stated. Review
third-party contributor rights separately for commercial redistribution.
