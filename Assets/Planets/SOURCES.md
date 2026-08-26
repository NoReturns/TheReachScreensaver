# Planet Texture Sources

Observational / data-derived imagery used by The Reach Screensaver.
NASA logos are not rendered in the screensaver.

## Earth_BlueMarble_2048.jpg

- **Object:** Earth
- **Source title:** Blue Marble: Land Surface, Shallow Water, and Shaded Topography
- **Credit:** NASA Goddard Space Flight Center / Reto Stöckli (land surface, shallow water, clouds); Robert Simmon (ocean color, compositing); MODIS teams; USGS EROS (topography); NOAA AVHRR (Antarctica); DMSP (city lights)
- **PIA / catalog:** Visible Earth / Earth Observatory Blue Marble (2002); file `land_shallow_topo_2048.jpg`
- **Type:** Observational mosaic (MODIS true-color composite)
- **Source page:** NASA Earth Observatory — The Blue Marble (2002); download via eoimages.gsfc.nasa.gov
- **Modifications:** None (native 2048×1024 JPEG)

## Mars_Viking.jpg

- **Object:** Mars
- **Source title:** Mars Image Texture
- **Credit:** NASA/Jet Propulsion Laboratory & Caltech; Viking images processed at USGS
- **PIA / catalog:** NASA Science 3D Resources — Mars
- **Type:** Observational / data-derived (Viking image mosaic processed for 3D texturing)
- **Source page:** https://science.nasa.gov/3d-resources/mars/ (also mirrored in nasa/NASA-3D-Resources)
- **Modifications:** None (1440×720 JPEG as published)

## Jupiter_PIA07782_2048.jpg

- **Object:** Jupiter
- **Source title:** Cassini's Best Maps of Jupiter (Cylindrical Map) — without grid
- **Credit:** NASA/JPL/Space Science Institute
- **PIA:** PIA07782
- **Type:** Observational mosaic (Cassini ISS cylindrical color map)
- **Source page:** https://photojournal.jpl.nasa.gov/catalog/PIA07782 ; full JPEG from PDS Atmospheres Node (NMSU)
- **Modifications:** Resized from 3601×1801 to 2048×1024 JPEG (high-quality bicubic)

## Pluto_PIA11707_2048.jpg

- **Object:** Pluto
- **Source title:** Pluto Global Color Map / Pluto Color Map
- **Credit:** NASA/Johns Hopkins University Applied Physics Laboratory/Southwest Research Institute
- **PIA:** PIA11707
- **Type:** Observational mosaic (New Horizons Ralph/MVIC global color map)
- **Source page:** https://photojournal.jpl.nasa.gov/catalog/PIA11707 ; NASA images CDN `PIA11707~orig.jpg`
- **Modifications:** Resized from 5926×2963 to 2048×1024 JPEG (high-quality bicubic)

## Bodies without imported maps (this pass)

| Body | Reason |
| --- | --- |
| Moon | No compact official equirectangular global mosaic was obtained cleanly at a sensible size; LRO WAC global mosaic is multi-GB. Retained procedural rocky lunar shader rather than wrapping a hemisphere photo. |
| Saturn | NASA 3D Resources Saturn texture is labeled fictional. No complete legitimate cylindrical observational map used. Procedural bands tuned against Cassini natural-color appearance; rings remain separate geometry. |
| Uranus | Voyager 2 true-color (PIA00032) shows a nearly featureless pale cyan sphere — restrained procedural match preferred over fabricating detail. |
| Neptune | NASA 3D Resources Neptune texture is labeled fictional. Procedural atmosphere tuned from Voyager 2 color imagery. |
| Io, Europa, Ganymede, Callisto, Titan, Charon | Out of scope for this pass; remain procedural. |
