# Reach imagery v1 provenance

The optional enhanced batch covers Moon, Mars, Io, Tethys, Dione, Iapetus,
Titan, Enceladus, Europa, Mercury, Rhea, and Venus. Earth was pinned earlier.
The 23 batch JPEGs are hosted at
https://github.com/NoReturns/TheReachScreensaver/releases/tag/reach-imagery-v1 .
`reach-assets.manifest.json` records exact release URLs, attribution, usage
notes, byte sizes, dimensions, and SHA-256 hashes. The staged preparation
records are in `C:\Storage\ReachImageryStaging` and its `phase4gd` subfolder.

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
| Mercury | https://astrogeology.usgs.gov/search/map/mercury_messenger_mdis_basemap_enhanced_color_global_mosaic_665m | MESSENGER enhanced colour, not natural colour; north-polar no-data. The exact local source JPEG URL was not verified. Owner approved with these limitations. |
| Rhea | https://science.nasa.gov/photojournal/color-maps-of-rhea-2014/ | Cassini enhanced colour; visible longitudinal seam. Owner approved with this limitation. |
| Venus | https://astrogeology.usgs.gov/search/map/venus_magellan_global_c3_mdir_synthetic_color_mosaic_4641m | Magellan radar-derived surface, fully simulated colour. It is neither visible atmospheric imagery nor natural observed surface colour. |

The source and export recipes, native dimensions, colour processing, coverage
checks, and exact original download URLs are in the named staging JSON files
and `phase4gd/reach-imagery-batch-4gd-audit.md`. The Mercury source JPEG
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
