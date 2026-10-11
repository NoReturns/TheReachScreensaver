# The Reach Screensaver

Native Windows screensaver: a passive cinematic voyage **Earth → Moon → Mars → Jupiter → Saturn → Uranus → Neptune → Pluto** (30 minutes, then a fade and loop). Stars beyond Sol are not in this slice.

## Requirements

- .NET 10 SDK (this repo is pinned to `10.0.102` via `global.json`, with `rollForward: latestFeature`)
- Windows 10/11 x64 for this build and all window modes
- Microsoft Edge WebView2 Evergreen Runtime for `/s` playback and `/p` preview
- An OpenGL 3.3 core context for the legacy OpenTK renderer
- NuGet: Microsoft.Web.WebView2 1.0.4258.31; OpenTK 4.9.4; StbImageSharp 2.30.15 (embedded planet albedo decode; included in single-file publish)

The standard `/s` screensaver and `/p` preview use WebView2. The OpenTK renderer remains available with `--legacy-opentk`. `/c` opens the native Settings window.

## WebView2 renderer

Build `Universe` with `npm run build`. The host first uses a built `dist` folder beside its executable or `.scr`, then looks for a sibling `Universe/dist` while running from the source tree. `REACH_WEB_DIST` can select an absolute `dist` path. The host maps the folder to `https://reach.example/index.html?reach=1`; no web server is used. A missing folder or WebView2 Runtime produces an error dialog and a log entry.

This mode opens one resizable window centered on the primary monitor. It uses the Microsoft Edge WebView2 Evergreen Runtime and a separate `%LOCALAPPDATA%\TheReachScreensaver\WebView2Prototype` browser profile. The web voyage state stays in that profile's localStorage; the original `state.json` is not loaded or changed. Close the window with its normal title bar button.

Standard `/s` and `--reach-web /s` use the same WebView2 screensaver path. It detects active Windows displays at launch. With one display it opens the existing borderless, topmost primary-monitor window and uses a normal camera projection. With multiple displays it opens one window per display and renders a panorama. A native key press or mouse click closes it; deliberate mouse movement also exits. Native input is consumed before it reaches WebView2.

`--reach-web /s /all` and `/s /all` remain accepted aliases. With multiple displays, Reach opens one borderless WebView2 window at each active monitor's physical virtual-desktop bounds, using the Windows primary-display flag to choose the voyage owner and countdown window. All windows share the same WebView2 profile and verified visual configuration. The primary advances and saves the voyage; secondary windows render its broadcast camera and simulation frames without starting or saving separate journeys. Each window uses a Three.js view offset into the full virtual desktop, including gaps and negative coordinates. A native input event on any monitor closes all windows. Launch keystrokes are ignored for the first 300 ms while the windows gain focus; web input remains blocked during that interval.

The WebViews have separate render loops, so there is no hardware frame-lock guarantee at a moving seam. Post-processing such as bloom is also clipped at each window edge. Physical bezel compensation is not applied. Multi-monitor mode uses more GPU and memory than the single-window path because each display has its own WebGL renderer and post-processing buffers.

On a normal close, the host dispatches the web page's `pagehide` event and waits up to one second before disposing WebView2. The web voyage also saves periodically and on visibility changes. A renderer crash, forced termination, or timed-out script can leave the latest checkpoint older than the last visible frame. The profile remains stable across development and fullscreen launches.

The host blocks and logs external web resources. Reach mode selects only bundled maps or verified local maps and does not construct NASA GIBS or Trek tile providers. Local asset responses and failures are logged in `%LOCALAPPDATA%\TheReachScreensaver\logs\`. There is no imagery download or polling during playback.

### Offline visual assets

The host creates `%LOCALAPPDATA%\TheReachScreensaver\reach.ini` with safe defaults on the first WebView2 launch. Edit it with a text editor:

```ini
[Visuals]
Mode=basic

[Countdown]
Enabled=1
Position=auto

[Downloads]
SourceBaseUrl=
DownloadOnFirstUse=0
VerifyHashes=1

[AssetSources]
; earth-day-high=https://your-https-mirror.example/earth.jpg

[Storage]
AssetDirectory=%LOCALAPPDATA%\TheReachScreensaver\Assets
```

`[Countdown]` controls the small yellow destination timer in the primary WebView2 window only. Set `Enabled=0` to hide it. `Position=auto` rotates through top-left, top-centre, top-right, bottom-left, bottom-centre, and bottom-right as the destination changes; any of those names can be used as a fixed position. Existing INI files without this section use the enabled, automatic defaults. The timer reads the active voyage phase and saved duration, so a resumed or paused voyage retains its time. The interactive explorer and legacy OpenTK screensaver do not show it.

`basic` uses bundled Earth, Moon, Mars, Jupiter, and selected observed moon maps; other surfaces remain procedural. `procedural` ignores all imagery. `enhanced` uses verified local high-resolution assets, then bundled maps, then procedural surfaces. Existing `cached` and `download` INI values are read as `enhanced` without rewriting the user's file. The `/c` Settings window offers Download High-Quality Images when pinned files are missing or invalid. It shows the required size and publisher before asking for consent, reports progress, and allows cancellation. A complete verified download enables `enhanced` through persistent settings for the next launch. The separate `--reach-assets-setup` command remains available. Playback and opening `/c` do not start downloads. `VerifyHashes=0` is ignored: cached files must match pinned hashes.

Each pinned asset has its own HTTPS source URL. The optional Moon 4K image is hosted on this project's GitHub Release; its single redirect to GitHub's HTTPS release-asset host is checked explicitly. Other redirects are not followed. `SourceBaseUrl` optionally overrides pinned URLs with an HTTPS mirror root using each asset's `DownloadPath`. An `AssetSources` entry overrides one asset's source and takes precedence over the shared mirror. All sources retain the application's pinned size and SHA-256. A local asset pack can be placed in `AssetDirectory` and will work without internet access if its files match the manifest. Invalid INI values fall back to safe defaults.

The embedded `VisualAssets/reach-assets.manifest.json` pins an optional 5400x2700 NASA Blue Marble Earth map and a 4096x2048 NASA SVS Moon map. Bundled map records and credits are in the universe repository's `public/textures/reach-assets.manifest.json`. Run `--reach-assets-status` to print cache counts and required bytes without downloading. Each approved setup invocation retries missing or corrupt assets, continues after an individual failure, skips verified files, and appends timestamped results to `%LOCALAPPDATA%\TheReachScreensaver\asset-acquisition.log`. Re-run setup after a failed publisher request; no configuration reset is needed. The local `--reach-assets-self-test` fixture exercises mode selection, verification, consent, caching, unreachable and corrupt source recovery, direct and override sources, restricted redirects, and path validation without opening a window or accessing the network.

The WebView2 host injects `window.__reachVisualAssets` before page scripts run: `{ schema: 1, mode: "basic|procedural|enhanced", assets: [{ assetId, destinationBodyId, url, sourceAttribution, width, height }] }`. Only verified enhanced assets are listed. Their URLs use `https://reach-assets.example/` mapped to the configured local asset directory. Reach uses bundled maps from Vite's public directory as fallback and never constructs remote tile providers. The interactive explorer retains its existing providers. The host rejects external resource requests.

For a local asset check, set `REACH_WEB_VERIFY_ASSETS=1` when launching `--reach-web`. This sends HEAD requests through the virtual host for the built JS, WASM, catalogue, model, and image files and logs each response. It reads local files only.

For a read-only persistence check, set `REACH_WEB_VERIFY_CHECKPOINT=1`. The log records a fingerprint and route progress from the saved web checkpoint at document start and after `pagehide`. It does not read or write the original C# `state.json`.

## Build

From the repository root:

```powershell
dotnet build
```

Release, self-contained Windows x64 publish (also writes `TheReachScreensaver.scr` next to the `.exe`):

```powershell
dotnet publish -c Release -r win-x64 --self-contained true -p:PublishSingleFile=true -p:IncludeNativeLibrariesForSelfExtract=true -o ./publish/win-x64
```

To use the published `.scr` from Screen Saver Settings, keep the built Universe `dist` folder beside `TheReachScreensaver.scr` and install or register the `.scr` so Windows can select it. The WebView2 Evergreen Runtime must also be installed. This phase does not package or install either dependency. Alternatively, set `REACH_WEB_DIST` to an absolute built `dist` path available to the screensaver process. Do not copy the `.scr` into `C:\Windows` or change the registry from this project.

A self-contained publish includes the .NET runtime and OpenTK/GLFW natives. WebView2 Evergreen Runtime and the built Universe `dist` folder are separate requirements.

## Run

The default development window uses WebView2. `--reach-web` keeps the same development behavior. For the old OpenTK development window (resizable, journey time in the title, Escape to close), use `--legacy-opentk`. Its keys `1`–`8` jump to Earth / Moon / Mars / Jupiter / Saturn / Uranus / Neptune / Pluto approach; PageUp / PageDown skip 15 seconds. These keys do nothing in `/s`.

```powershell
dotnet run
```

Full WebView2 screensaver mode (one borderless window per display, hidden cursor, exits on key / click / meaningful mouse movement):

```powershell
dotnet run -- /s
```

or, after building:

```powershell
.\bin\Debug\net10.0\TheReachScreensaver.exe /s
```

`dotnet run -- --legacy-opentk /s` runs the old OpenTK fullscreen renderer for development. `/s /all` remains accepted for compatibility.

Configuration (shows imagery status, About, and Credits; download and reset actions are disabled):

```powershell
dotnet run -- /c
```

Windows preview (parent HWND from Screen Saver Settings):

```powershell
.\TheReachScreensaver.exe /p <HWND>
.\TheReachScreensaver.exe /p:<HWND>
```

## Architecture

| Piece | Role |
| --- | --- |
| `Application/` | Command-line modes, `/c` Settings window, `/p` parenting, input/exit, process lifecycle |
| `Display/` | Monitor enumeration, virtual-desktop bounds, visual-anchor (middle) monitor |
| `Journey/` | `JourneyController`, authored spline, celestial body data, axial rotation |
| `Rendering/` | OpenGL window, camera, starfield, planets, rings, fade. Does not own journey time. |
| `Persistence/` | `%LOCALAPPDATA%\TheReachScreensaver\state.json` |

### Multi-monitor

The legacy OpenTK `/s` route (`--legacy-opentk /s`) opens **one** borderless window covering the virtual desktop. One camera, one forward, one starfield, one journey.

Displays are sorted by horizontal center. The **median** monitor is the visual anchor. Camera forward is the ray through that monitor's physical pixel center, even when the virtual-desktop rectangle is asymmetric. Each monitor is a `glViewport` pane with an off-axis slice of that master frustum. No toe-in, no cloned scenes.

Development mode draws a small crosshair at the projected forward direction (the middle-monitor view). It is not shown in `/s` or `/p`.

### Journey

1800-second authored Catmull-Rom path:

| Time | Encounter |
| --- | --- |
| 0:00 | Earth departure |
| 1:15 | Moon |
| 4:00 | Mars |
| 10:00 | Jupiter (continues; no reset) |
| 13:30 | Saturn |
| 19:00 | Uranus |
| 25:00 | Neptune |
| 30:00 | Pluto closest approach / fade |

`journeySeconds` is the authority and is stored in `[0, 1800)`. At 1796–1800s the scene fades to black, time wraps to 0, then fades in on Earth. Existing Slice 2 saves (for example `402.5`) resume at the same time.

Planets spin around a per-body axis (`RotationAxis`, `RotationRate`, `InitialRotation`) with recognizable axial tilt. Procedural surfaces are evaluated in body-local coordinates so continents, bands, and the Great Red Spot travel with the sphere. Lighting is computed from a cinematic Sol position, not a camera headlight.

### Solar-System scale (Slice 3.1)

Authoritative positions and radii use **astronomical units** (`1 AU = 149,597,870.7 km`) in `double` precision (`Vector3d`). Heliocentric distances follow broadly realistic orbital separations; planets are placed at **authored ecliptic longitudes** around Sol (not a single forward queue). The camera path still uses cinematic timing, so effective travel speed varies wildly between legs.

Rendering uses a **floating origin**: each frame, `renderPosition = bodyWorldPosition - cameraWorldPosition` (computed in double, converted to float for OpenGL). Distant bodies are drawn by **projected angular size**:

| Projected radius | Mode |
| --- | --- |
| ≥ 2 px | Full sphere |
| 0.5–2 px | Smooth crossfade point ↔ sphere |
| &lt; 0.5 px, naked-eye | Star-like point (Mars, Jupiter, Saturn, faint Uranus) |
| Hidden | Neptune, Pluto from Earth; bodies too faint/small |

Saturn's rings appear only when the planet disk is sufficiently resolved (`SphereBlend > 0.35`).

Development mode title shows journey diagnostics, for example: `04:31 | Mars | cam 1.05 AU | 0.52 AU | 12.4 px | SPHERE — 60 FPS`. These do not appear in `/s`.

Saturn uses a single annulus mesh with procedural radial bands, tilted with the planet. Titan and Charon use the shared satellite path. Uranus shows its sideways pole through axial tilt rather than rings.

### Persistence

File: `%LOCALAPPDATA%\TheReachScreensaver\state.json`

```json
{
  "version": 1,
  "journeySeconds": 0.0,
  "seed": 18472931
}
```

Missing or corrupt files recover to defaults. Saves are atomic (temp file + replace). `journeySeconds` resumes the voyage on the next run. Valid range is `[0, 1800)`.

Logs: `%LOCALAPPDATA%\TheReachScreensaver\logs\`

### Preview (`/p`)

On Windows, the host creates one WebView2 child HWND within the supplied preview control. It tracks the parent's client size and closes when the parent is destroyed. Preview uses a normal single-camera projection, hides the countdown, and stores its web state in the separate `WebView2Preview` profile. The fullscreen voyage profile and legacy `state.json` are untouched.

`/p` never opens a full-screen window. A missing HWND, a non-Windows OS, or a failed attach logs the reason and exits.

## Historical OpenTK validation

The following results describe an earlier OpenTK-only slice. They do not validate the current WebView2 `/s` route.

- `dotnet build` succeeds on .NET 10
- `dotnet run` opens a 1280×720 window, renders at about 60 FPS, resizes, and closes cleanly
- `dotnet run -- /s` covers the virtual desktop, exits on a key and on a mouse click
- `dotnet run -- /c` showed the earlier no-settings message and exited 0
- `/p` parses correctly and does not open a window on Linux
- Journey state is created, loaded, saved every 30 seconds, and saved on shutdown
- `dotnet publish -r win-x64` writes `publish/win-x64/TheReachScreensaver.scr`

## What this slice does not include

Alpha Centauri, interstellar travel, the heliopause, the Oort Cloud, real ephemerides, gravity, galaxies, labels, HUD, audio, installer, updater, networking, or editable Settings controls.
