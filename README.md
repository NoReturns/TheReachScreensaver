# The Reach Screensaver

**A free, open-source cinematic space screensaver for Windows 10/11.**

Let your idle desktop take you through the Solar System. The Reach follows a persistent **41-stop voyage**, visiting planets, moons, dwarf planets, Halley's Comet, and historic spacecraft. Watch close-up encounters and cinematic journeys between destinations, then pick up from your saved progress the next time it starts.

Created by **NoReturns**, The Reach Screensaver is an independent project based on *Your Eyes In Space* by **Berk Comba / BigBrains**.

**Current version: 0.4.0.** The v1.0 installer and final ZIP are not published yet; the version will remain 0.4.0 until release validation is complete.

## Screenshots

Screenshots will be added here before release:

- **Three-monitor panoramic voyage** - one view of space spanning three displays.
- **Close-up planetary encounter** - a destination during observation.
- **Native Screensaver Settings** - imagery downloads, journey reset, and project information.

No suitable screenshots are currently included in this repository. The owner can add the three captures to a repository screenshot folder and embed them here when ready.

## Features

- **41 Solar System destinations.** Explore a route that starts at Earth and includes the Moon, giant planets and their moons, dwarf planets, the Sun, Halley's Comet, and the Pioneer and Voyager spacecraft.
- **Cinematic travel and observation.** Automated departures, travel, planetary observation, and moon flybys make the voyage a passive experience.
- **One screen or a panorama.** The screensaver detects active monitors at launch. Multiple displays share the same voyage and camera view, including a three-monitor panoramic layout.
- **Windows screensaver integration.** Fullscreen playback, a native Settings window, and an embedded preview in Windows Screen Saver Settings. Preview uses a separate profile and leaves your fullscreen journey untouched.
- **Persistent journey progress.** Saved checkpoints let subsequent sessions resume the voyage instead of starting over. Secondary monitors follow the primary journey.
- **Astronomical distance odometer.** A cumulative distance display tracks progress across destination-to-destination legs. It measures geometric separation sampled at departure, rather than the camera's exact flight path.
- **Destination countdown.** See the time remaining for the current observation, departure, or travel phase. Countdown and distance overlays appear only on the primary monitor.
- **Imagery with a fallback.** Bundled planetary textures work offline. Procedural surfaces cover bodies without maps, while optional verified high-resolution imagery adds detail where available.
- **Verified downloads.** Optional images must match their expected file size and SHA-256 fingerprint before they can be used.
- **Settings and Reset Trip.** Review imagery status, approve downloads, restart your journey, and open privacy, credits, licensing, and source information.
- **Exit on input.** A key press, mouse click, or deliberate mouse movement closes playback across all displays.

## Download and installation

**The normal Windows installer and final v1.0 ZIP are not available yet.** There are no application download links to use at this stage. The planned distribution options are:

| Option | What to expect |
| --- | --- |
| Windows installer | A normal installer built with Inno Setup, planned for release. |
| Manual ZIP | A complete portable package containing the screensaver, local visual assets, installation scripts, and license notices. |
| Build from source | Build the native host and Universe fork yourself using the public repositories. Available now through the source-build guide. |

**Installing the files and selecting the screensaver are separate actions.** For the manual package:

1. Extract the entire package and keep its files and folders together, including the complete `dist` folder and license notices.
2. Ensure Microsoft Edge WebView2 Evergreen Runtime is installed.
3. Follow the [manual installation guide](packaging/INSTALL.md) to review and run the per-user installation script, then the separate selection script.
4. Open Windows **Change screen saver** settings to check the selection, preview it, and review your idle timeout and resume security options. Use **Settings** to open Reach's native configuration window.

The manual selection script preserves your existing timeout, active flag, and resume security settings. If screensavers are currently disabled, selecting Reach does not enable them automatically. Organization policy may restrict selection.

Keep the `.scr` with its companion files. **Do not copy only the `.scr` into System32 or another Windows directory.** The [installation guide](packaging/INSTALL.md) also covers uninstalling and restoring the previous selection. Uninstall leaves local journey data and downloaded imagery in place.

To build your own package, follow [Build from source](BUILD_FROM_SOURCE.md).

## Requirements

- **Windows 10 or Windows 11, x64.**
- **Microsoft Edge WebView2 Evergreen Runtime**, installed separately.
- **WebGL-capable graphics hardware** for playback and preview.
- One monitor; additional monitors are optional. Rendering on multiple monitors uses more graphics resources.
- An internet connection only if you choose to download optional high-resolution imagery. Playback works offline with the complete local package and installed runtime.

Packaged builds include the .NET runtime and require **no .NET SDK, Node.js, npm, or web server**. Developer prerequisites belong in [BUILD_FROM_SOURCE.md](BUILD_FROM_SOURCE.md).

## Settings and imagery

Open Reach's **Settings** from Windows Screen Saver Settings. The native window shows the current visual mode, verified and missing images, required download size, and download progress. It also provides **Reset Trip**, credits, privacy, licenses, and source links.

### Visual modes

| Mode | Appearance |
| --- | --- |
| **Basic** | The default. Uses bundled planetary maps where available and procedural surfaces elsewhere. |
| **Procedural** | Uses generated surfaces instead of imagery maps. |
| **Enhanced** | Uses verified local high-resolution maps, falling back to bundled maps and then procedural surfaces when needed. |

The public enhanced imagery offer contains **26 files across 14 destinations**. Available maps include 2K and 4K versions where supplied; the renderer chooses an available verified resolution for the display. The Moon's enhanced map is 4K only.

**Ceres and Mercury enhanced downloads are excluded** while Ceres redistribution terms and the exact Mercury JPEG provenance remain unresolved. Both destinations remain in the voyage with fallback imagery. Separate enhanced maps for **Phobos, Deimos, and Mimas** exist, but are not offered through the application.

### Optional downloads

Choose **Download High-Quality Images** to review the files, size, and sources before approving a download. Verified files are skipped. Downloads show progress, support cancellation, and can be retried after failure. A complete verified acquisition enables Enhanced mode for the next launch.

Downloads are user-initiated. Opening Settings or starting playback does not download images. Cached images are checked against pinned sizes and SHA-256 fingerprints; missing or invalid images fall back to the available local rendering. Once acquired, enhanced imagery also works offline.

### Countdown preferences and Reset Trip

The countdown is enabled by default. It can use a fixed position or automatically move among six positions as destinations change. These preferences and the visual mode are stored locally: `reach.ini` supplies defaults, and saved preferences in `settings.json` take precedence. The current native Settings window reports the mode and manages downloads; it does not include visual-mode or countdown selectors.

**Reset Trip** asks for confirmation and schedules a return to the starting point, with the distance odometer reset, on the next fullscreen launch. Close any running fullscreen voyage before requesting a reset. Downloaded images and application settings are preserved.

For individual image sources, processing details, and coverage limitations, see the [imagery provenance documentation](VisualAssets/REACH_IMAGERY_V1.md).

## Privacy and transparency

The Reach Screensaver contains **no advertising, application analytics, or user tracking**. Its source contains no NoReturns collection endpoint, and it does not send your preferences or voyage progress to NoReturns.

Journey saves, preferences, WebView2 profiles, cached images, and diagnostic logs stay locally under `%LOCALAPPDATA%\TheReachScreensaver`. Logs may contain local paths, runtime errors, and blocked request host names.

Normal playback uses local resources, makes no external resource requests, and does not fetch imagery. The application blocks external web resources. Optional, user-approved imagery downloads use pinned GitHub sources by default; those services receive ordinary connection information such as your IP address. Image size and SHA-256 verification protect file integrity.

Playback depends on Microsoft's separately installed **WebView2 Evergreen Runtime**, which may make its own update or diagnostic connections under Microsoft's terms and settings.

Both the native host and the Universe fork have public source repositories, so you can inspect the code and build the application yourself. Read the [privacy policy](PRIVACY.md) for details.

## Source code

- [Native Windows host - NoReturns/TheReachScreensaver](https://github.com/NoReturns/TheReachScreensaver)
- [Universe fork - NoReturns/youreyesinspace](https://github.com/NoReturns/youreyesinspace)
- [Original Your Eyes In Space - berkcomba/youreyesinspace](https://github.com/berkcomba/youreyesinspace)

The [source-build guide](BUILD_FROM_SOURCE.md) explains how to build both components and assemble a complete offline package.

## Known limitations

- The current **41-stop route repeats in the same forward order**. Interstellar travel is not part of v1.0. Reverse traversal and round-trip counting are future features.
- Astronomical imagery has uneven coverage and resolution. Some maps contain seams or missing polar regions; others use processed colour, grayscale, near-infrared, or radar-derived data rather than natural visible colour. See [image sources and limitations](VisualAssets/REACH_IMAGERY_V1.md).
- Multi-monitor rendering is **not hardware frame-locked**. Displays render independently, so moving seams may differ between frames. Bloom is clipped at window edges, and physical bezel compensation is not applied.
- Journey progress is checkpointed rather than saved every frame. A crash or forced termination can resume from an older checkpoint.
- Final v1.0 package, installation, full imagery-download, and playback acceptance checks remain pending. This README describes current implementation, not a claim of completed release certification.

## Credits and licensing

The Reach Screensaver is developed by **NoReturns**, building on **Your Eyes In Space by Berk Comba / BigBrains**.

Astronomical imagery and maps draw on **NASA, JPL, USGS**, and contributing mission and processing teams, including LROC/Arizona State University, LOLA, the Space Science Institute, and Philip Stooke's NASA Planetary Data System maps. Individual credits and usage limits are recorded with the imagery. These credits do not imply endorsement.

The star catalogue is derived from the **HYG Database by David Nash / Astronexus**, using the Hipparcos, Yale Bright Star, and Gliese catalogues. The derived data retains its **CC BY-SA 4.0** license.

Third-party software includes **three.js**, **OpenTK**, **StbImageSharp**, the **Microsoft WebView2 SDK loader**, and the **.NET runtime**. Their respective licenses and notices accompany the package.

The original native host code is **MIT licensed**; the Universe source retains its **BigBrains MIT license**. **Those source-code licenses do not relicense third-party imagery, astronomical data, trademarks, or dependencies.** Each retains its own terms and attribution requirements.

Read the [host license](LICENSE), [full credits](CREDITS.txt), [imagery provenance](VisualAssets/REACH_IMAGERY_V1.md), [pinned enhanced imagery manifest](VisualAssets/reach-assets.manifest.json), [native map sources](Assets/Planets/SOURCES.md), and [privacy policy](PRIVACY.md) for the applicable details.
