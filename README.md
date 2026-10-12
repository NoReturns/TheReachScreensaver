# The Reach Screensaver

**A free, open-source cinematic space screensaver for Windows 10/11.**

Let your idle desktop take you through the Solar System. The Reach follows a persistent **41-stop voyage**, visiting planets, moons, dwarf planets, Halley's Comet, and historic spacecraft. Watch close-up encounters and cinematic journeys between destinations, then pick up from your saved progress the next time it starts.

Created by **NoReturns**, The Reach Screensaver is an independent project based on *Your Eyes In Space* by **Berk Comba / BigBrains**.

**Current version: 1.0.0.** Download the Windows installer or manual ZIP from [GitHub Releases](https://github.com/NoReturns/TheReachScreensaver/releases).

## Why does this exist?

First, an important note: **The Reach is a NON-INTERACTIVE screensaver.** It's designed to play automatically while you're away from your computer. You don't fly the ship, click planets, or control the journey. You just watch it travel through space, and it picks up where it left off next time.

And yes, I realize screensavers are passé.

Unfortunately, my monitor and Windows seem incapable of performing a proper display handshake when waking from sleep. Every time I'd walk away for more than 20 minutes, I'd come back to find my carefully arranged desktop icons all grouped together in a corner.

After spending far too much time trying to solve a problem that shouldn't exist in the first place, I gave up fighting it. My monitors no longer go to sleep.

So... screensaver it is. Windows Ribbons? Meh.

So I built a basic space-travel screensaver myself. It worked, but frankly, it sucked. I wanted something better: a cinematic journey through space that could run across my monitors, remember its progress, and continue exploring whenever I walked away.

I put up with my homemade version until I came across [**Your Eyes In Space**](https://github.com/berkcomba/youreyesinspace) by **Berk Comba / BigBrains**.

With his permission and under the project's MIT license, I forked it and started building the screensaver I'd wanted all along. His interactive universe explorer provided a fantastic foundation for turning that idea into something much more substantial.

And that's it.

Just a stubborn monitor, a Windows sleep/wake handshake problem I couldn't solve, and a desire to watch something more interesting than Ribbons.

**Screw the sleep-mode handshake nonsense. I'll take a journey through space instead.**

Hopefully others enjoy it as much as I do.

**41 destinations in v1.0. Another 200+ are planned.**

Enjoy the journey.

## Screenshots

Screenshots are planned for:

- **Three-monitor panoramic voyage** - one view of space spanning three displays.
- **Close-up planetary encounter** - a destination during observation.
- **Native Screensaver Settings** - imagery downloads, journey reset, and project information.

Screenshots are not yet included in this repository.

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

Get the v1.0.0 packages and `SHA256SUMS.txt` from [GitHub Releases](https://github.com/NoReturns/TheReachScreensaver/releases). Check the downloaded file's SHA-256 against the checksum file.

| Option | What to expect |
| --- | --- |
| Windows installer | `TheReachScreensaver-1.0.0-Setup-backup-fix.exe`. Recommended for most Windows users. Installs for the current user without administrator rights. |
| Manual ZIP | `TheReachScreensaver-1.0.0-win-x64-backup-fix.zip`. Complete package with screensaver, local visual assets, installation scripts, and license notices for manual installation. |
| Build from source | Build the native host and Universe fork yourself using the public repositories. Follow the source-build guide. |

**Installing the files and selecting the screensaver are separate actions.** The installer does not automatically select or enable Reach, and it leaves the existing idle timeout and resume security setting unchanged. For the normal installer:

1. Install Microsoft Edge WebView2 Evergreen Runtime if it is not already available.
2. Run `TheReachScreensaver-1.0.0-Setup-backup-fix.exe` and complete Setup. Close any running Reach windows before upgrading an older copy.
3. On Setup's final page, optionally check **Set The Reach as my Windows screensaver**. It is unchecked by default. You can select Reach later through **Start > The Reach Screensaver > Select The Reach Screensaver**.
4. Open Windows **Change screen saver** settings to review the preview, idle timeout, and resume security options. Use Reach's Settings action to open its native configuration window.

For the manual ZIP:

1. Extract the entire package and keep its files and folders together, including the complete `dist` folder and license notices.
2. Ensure Microsoft Edge WebView2 Evergreen Runtime is installed.
3. Follow the [Windows installation guide](packaging/INSTALL.md) to review and run the per-user installation script, then the separate selection script if you choose to select Reach.
4. Open Windows **Change screen saver** settings to check the selection, preview it, and review your idle timeout and resume security options. Use **Settings** to open Reach's native configuration window.

The manual selection script preserves your existing timeout, active flag, and resume security settings. If screensavers are currently disabled, selecting Reach does not enable them automatically. Organization policy may restrict selection.

Reach's per-user installation does not add a permanent entry to the native Windows screensaver dropdown. After switching to another saver, use the Start Menu shortcut to select Reach again. The shortcut preserves the current Windows settings and retains earlier selection backups in local history. Uninstall refuses while Reach is selected; choose another saver or no saver in Windows settings first. Installation and ordinary selection do not require administrator rights.

Keep the `.scr` with its companion files. **Do not copy only the `.scr` into System32 or another Windows directory.** The [installation guide](packaging/INSTALL.md) also covers upgrades, recovery, and uninstalling. Uninstall leaves local journey data and downloaded imagery in place. Its optional, unchecked cleanup removes retained upgrade backups only after the application is removed; deleting them prevents restoration of earlier installations. The installer is currently unsigned, so Windows may show an unknown publisher or SmartScreen warning.

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
- The v1.0.0 installer passed owner acceptance on Windows 11 Home in VM-Chewy. Backup cleanup was disabled there when inspection found an unrecognized historical `pending-upgrade.json.*.previous` artifact. The uninstaller reported the issue and preserved the backups. Actual backup deletion was verified in synthetic tests, not in VM-Chewy.

## Credits and licensing

The Reach Screensaver is developed by **NoReturns**, building on **Your Eyes In Space by Berk Comba / BigBrains**.

Astronomical imagery and maps draw on **NASA, JPL, USGS**, and contributing mission and processing teams, including LROC/Arizona State University, LOLA, the Space Science Institute, and Philip Stooke's NASA Planetary Data System maps. Individual credits and usage limits are recorded with the imagery. These credits do not imply endorsement.

The star catalogue is derived from the **HYG Database by David Nash / Astronexus**, using the Hipparcos, Yale Bright Star, and Gliese catalogues. The derived data retains its **CC BY-SA 4.0** license.

Third-party software includes **three.js**, **OpenTK**, **StbImageSharp**, the **Microsoft WebView2 SDK loader**, and the **.NET runtime**. Their respective licenses and notices accompany the package.

The original native host code is **MIT licensed**; the Universe source retains its **BigBrains MIT license**. **Those source-code licenses do not relicense third-party imagery, astronomical data, trademarks, or dependencies.** Each retains its own terms and attribution requirements.

Read the [host license](LICENSE), [full credits](CREDITS.txt), [imagery provenance](VisualAssets/REACH_IMAGERY_V1.md), [pinned enhanced imagery manifest](VisualAssets/reach-assets.manifest.json), [native map sources](Assets/Planets/SOURCES.md), and [privacy policy](PRIVACY.md) for the applicable details.
