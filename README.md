# The Reach Screensaver

Native Windows screensaver: a passive 41-stop Solar System voyage that starts at Earth, visits planets, moons, dwarf planets, spacecraft, Halley's Comet and the Sun, and then repeats. Interstellar destinations are outside the production route.

## Requirements

- .NET 10 SDK for source builds only (this repo is pinned to `10.0.102` via `global.json`, with `rollForward: latestFeature`)
- Windows 10/11 x64 for this build and all window modes
- Microsoft Edge WebView2 Evergreen Runtime for `/s` playback and `/p` preview
- An OpenGL 3.3 core context for the legacy OpenTK renderer
- NuGet: Microsoft.Web.WebView2 1.0.4258.31; OpenTK 4.9.4; StbImageSharp 2.30.15 (embedded planet albedo decode; included in single-file publish)

The standard `/s` screensaver and `/p` preview use WebView2. The OpenTK renderer remains available with `--legacy-opentk`. `/c` opens the native Settings window.

## WebView2 renderer

Build `Universe` with `npm run build`. The host first uses a built `dist` folder beside its executable or `.scr`, then looks for a sibling `Universe/dist` while running from the source tree. `REACH_WEB_DIST` can select an absolute `dist` path. The host maps the folder to `https://reach.example/index.html?reach=1`; no web server is used. A missing folder or WebView2 Runtime produces an error dialog and a log entry.

Development mode opens one resizable WebView2 window. Fullscreen `/s` uses the active displays. The voyage state is stored in the `%LOCALAPPDATA%\TheReachScreensaver\WebView2Prototype` browser profile's localStorage; the legacy `state.json` is not loaded or changed.

Standard `/s` and `--reach-web /s` use the same WebView2 screensaver path. It detects active Windows displays at launch. With one display it opens the existing borderless, topmost primary-monitor window and uses a normal camera projection. With multiple displays it opens one window per display and renders a panorama. A native key press or mouse click closes it; deliberate mouse movement also exits. Native input is consumed before it reaches WebView2.

`--reach-web /s /all` and `/s /all` remain accepted aliases. With multiple displays, Reach opens one borderless WebView2 window at each active monitor's physical virtual-desktop bounds. The primary advances and saves the voyage; secondary windows render its broadcast camera and simulation frames without starting or saving separate journeys. A native input event on any monitor closes all windows. The native input monitor uses a 750 ms startup grace period and a 16-pixel mouse movement threshold.

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

`basic` uses the 15 bundled Reach maps where available; other surfaces remain procedural. `procedural` ignores imagery. `enhanced` uses verified local high-resolution assets, then bundled maps, then procedural surfaces. The public download offer has 26 files for 14 destinations: Earth and 25 files in the optional batch. The source manifest retains two Ceres and two Mercury records for provenance, but the app excludes them because Ceres redistribution terms and the exact Mercury JPEG provenance are unresolved. Existing local files are retained but these four records are not injected into playback. The web renderer selects 2K at physical viewport widths up to 2048 pixels and 4K above that, using an available verified tier if the preferred tier is absent. Moon has a 4K enhanced file only. Existing `cached` and `download` INI values are read as `enhanced`. The `/c` Settings window offers Download High-Quality Images for missing offered files, with consent, progress, cancellation, and retry. A complete verified acquisition enables `enhanced` for the next launch. Playback and opening `/c` do not start downloads. Cached files must pass size and SHA-256 checks, even if `VerifyHashes=0` is set.

Each offered asset has its own HTTPS source URL. The 25 offered batch JPEGs are pinned to the project's `reach-imagery-v1` GitHub Release; their listed names, sizes, and GitHub SHA-256 digest metadata matched the manifest on 2026-10-11. Full download and playback remain release-candidate checks. A single redirect to GitHub's HTTPS release-asset host is allowed; other redirects are not followed. `SourceBaseUrl` can select an HTTPS mirror root, and an `AssetSources` entry can override one source. All sources retain pinned size and SHA-256 checks. A matching local asset pack in `AssetDirectory` works without internet access. Ceres and Mercury retain their bundled or procedural fallback paths.

The embedded `VisualAssets/reach-assets.manifest.json` contains Earth and a 15-destination optional batch. The public loader excludes Ceres and Mercury, leaving 14 offered destinations. The maps include enhanced colour, grayscale, near-infrared, and radar-derived products; `VisualAssets/REACH_IMAGERY_V1.md` records their sources and limitations. Bundled map records are in Universe's `public/textures/reach-assets.manifest.json`. Run `--reach-assets-status` to inspect the cache without downloading. An approved setup retries missing or corrupt offered assets and records results in `%LOCALAPPDATA%\TheReachScreensaver\asset-acquisition.log`.

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

For manual distribution, keep the published `.scr`, built Universe `dist`, `LICENSE`, `PRIVACY.md`, `CREDITS.txt`, attribution, third-party licenses, and `packaging/windows` scripts together. [BUILD_FROM_SOURCE.md](BUILD_FROM_SOURCE.md) covers the two-repository build and package layout. `packaging/INSTALL.md` describes separate per-user installation, selection, and uninstall steps. Selection preserves the existing idle timeout, active flag, and resume security setting; it does not enable an inactive screensaver. WebView2 Evergreen must already be installed. Do not copy only the `.scr` into `C:\Windows`.

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

Configuration (imagery status and consent-based download, Reset Trip, About, Privacy, Credits, Licenses, and Source Code):

```powershell
dotnet run -- /c
```

Windows preview (parent HWND from Screen Saver Settings):

```powershell
.\TheReachScreensaver.exe /p <HWND>
.\TheReachScreensaver.exe /p:<HWND>
```

## Journey and persistence

The production route contains 41 authored Solar System stops and repeats in the same forward order. The primary WebView2 window saves `the-reach:journey:v1` in its localStorage on phase changes, periodically, on visibility changes, and on normal close. Secondary displays follow the primary without separate journey saves. `/p` preview uses a separate WebView2 profile and does not change the fullscreen voyage.

The distance display accumulates geometric centre-to-centre legs sampled at departure; it is not the camera's travelled path. Reset Trip in `/c` schedules deletion of the web checkpoint at the next fullscreen launch, returning the voyage and distance to the start. The legacy OpenTK renderer and its `state.json` are separate.

## Credits

Settings opens the packaged [Credits](CREDITS.txt), [Privacy](PRIVACY.md), and [MIT host license](LICENSE), and links to the [source code](https://github.com/NoReturns/TheReachScreensaver). Keep them with `ATTRIBUTION.md`, third-party notices, and the complete Reach `dist` folder when assembling a release candidate. The version remains 0.4.0 until release candidate validation.
