# The Reach Screensaver

Native Windows screensaver: a passive cinematic voyage **Earth → Moon → Mars → Jupiter → Saturn → Uranus → Neptune → Pluto** (30 minutes, then a fade and loop). Stars beyond Sol are not in this slice.

## Requirements

- .NET 10 SDK (this repo is pinned to `10.0.400` via `global.json`, with `rollForward: latestFeature`)
- Windows 10/11 x64 for screensaver, preview, and configuration modes
- An OpenGL 3.3 core context (any GPU from the last ~15 years, or a software renderer)
- NuGet: OpenTK 4.9.4; StbImageSharp 2.30.15 (embedded planet albedo decode; included in single-file publish)

Development mode can also run on Linux if GLFW/OpenGL are available. `/s`, `/c`, and `/p` are Windows-oriented.

## Build

From the repository root:

```powershell
dotnet build
```

Release, self-contained Windows x64 publish (also writes `TheReachScreensaver.scr` next to the `.exe`):

```powershell
dotnet publish -c Release -r win-x64 --self-contained true -p:PublishSingleFile=true -p:IncludeNativeLibrariesForSelfExtract=true -o ./publish/win-x64
```

Do not copy the `.scr` into `C:\Windows` or change the registry from this project. Install it yourself after testing.

A self-contained publish currently lands at about 77 MB (`TheReachScreensaver.exe` plus a same-sized `TheReachScreensaver.scr` copy). That size is the .NET 10 runtime plus OpenTK/GLFW natives; it does not need a separate runtime install on the target PC.

## Run

Development window (resizable, journey time in the title, Escape to close). Keys `1`–`8` jump to Earth / Moon / Mars / Jupiter / Saturn / Uranus / Neptune / Pluto approach; PageUp / PageDown skip 15 seconds. These keys do nothing in `/s`.

```powershell
dotnet run
```

Full screensaver mode (borderless, covers the virtual desktop, hidden cursor, exits on key / click / meaningful mouse movement):

```powershell
dotnet run -- /s
```

or, after building:

```powershell
.\bin\Debug\net10.0\TheReachScreensaver.exe /s
```

Configuration (no settings yet; this shows a short message and exits):

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
| `Application/` | Command-line modes, `/c` message, `/p` parenting, input/exit, process lifecycle |
| `Display/` | Monitor enumeration, virtual-desktop bounds, visual-anchor (middle) monitor |
| `Journey/` | `JourneyController`, authored spline, celestial body data, axial rotation |
| `Rendering/` | OpenGL window, camera, starfield, planets, rings, fade. Does not own journey time. |
| `Persistence/` | `%LOCALAPPDATA%\TheReachScreensaver\state.json` |

### Multi-monitor

`/s` opens **one** borderless window covering the virtual desktop. One camera, one forward, one starfield, one journey.

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

On Windows, the host parents the GLFW/OpenTK HWND into the supplied preview control (`SetParent` + `WS_CHILD`, sized to the parent client rect, closed when the parent is destroyed).

This is the correct embedding path. Remaining risk: GLFW still created a top-level window first, so some Screen Saver Settings hosts may clip, refuse focus, or tear down the child if they dislike the leftover GLFW styles. If a Windows preview pane is blank or detached, the next step is a dedicated child `HWND` + OpenGL context that is never a top-level GLFW window.

`/p` never opens a full-screen window. A missing HWND, a non-Windows OS, or a failed attach logs the reason and exits.

## Verified in this environment

This agent ran on Linux with an X11 display, so Windows-only pieces were compiled and published rather than executed as a `.scr`.

- `dotnet build` succeeds on .NET 10
- `dotnet run` opens a 1280×720 window, renders at about 60 FPS, resizes, and closes cleanly
- `dotnet run -- /s` covers the virtual desktop, exits on a key and on a mouse click
- `dotnet run -- /c` prints the no-settings message and exits 0
- `/p` parses correctly and does not open a window on Linux
- Journey state is created, loaded, saved every 30 seconds, and saved on shutdown
- `dotnet publish -r win-x64` writes `publish/win-x64/TheReachScreensaver.scr`

## What this slice does not include

Alpha Centauri, interstellar travel, the heliopause, the Oort Cloud, real ephemerides, gravity, galaxies, labels, HUD, audio, installer, updater, networking, or a settings system.
