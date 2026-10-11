# The Reach Screensaver - manual installation

This folder is a self-contained Windows x64 screensaver package. Keep
`TheReachScreensaver.scr` and the complete `dist` directory together. No .NET
SDK, Node.js, npm, source checkout, web server, or network connection is needed
for playback.

## Prerequisite

Microsoft Edge WebView2 Evergreen Runtime must be installed. If it is absent,
the screensaver logs an error and shows a message instead of opening a blank
window. Get the runtime from Microsoft's WebView2 download page:
https://developer.microsoft.com/microsoft-edge/webview2/

For an offline computer, obtain Microsoft's Evergreen Standalone Installer on
another computer and install it manually before using this package. The
runtime is shared by Windows applications and is not included here.

## Installation rehearsal

The scripts in `windows` are plan-only unless `-Apply` is supplied. Keep this
entire package in a stable staging location until the rehearsal is complete.
Installing the files and selecting Reach are separate owner-approved actions.
In PowerShell, from this package folder:

```powershell
$package = (Get-Location).Path
& .\windows\Install-ReachScreensaver.ps1 -PackagePath $package
```

After approving the file copy, rerun that command with `-Apply`. It verifies
every copied file by SHA256 and moves the complete package into
`%LOCALAPPDATA%\Programs\TheReachScreensaver`. It refuses an existing target
and never changes the current screensaver selection. If a copy fails, the
incomplete `TheReachScreensaver.pending-*` folder is left for inspection.

The installed `.scr` and its `dist` folder stay together. A key press, click,
or deliberate mouse movement exits `/s` on every display. To test playback
without selecting Reach, launch the installed `.scr` directly with `/s` from
PowerShell using `System.Diagnostics.ProcessStartInfo` and
`UseShellExecute = $false`; the `.scr` file association may substitute `/S`.

## Select Reach separately

```powershell
& .\windows\Select-ReachScreensaver.ps1
```

After separate approval to change the user's screensaver selection, rerun
with `-Apply`. This first saves `SCRNSAVE.EXE`, `ScreenSaveActive`,
`ScreenSaveTimeOut`, and `ScreenSaverIsSecure`, including missing values and
registry types, at `%LOCALAPPDATA%\TheReachScreensaver\Installation\previous-screensaver.json`.
It then calls Windows `desk.cpl,InstallScreenSaver` with the installed `.scr`'s
full absolute path and verifies that Windows retained that path. If selection
fails, it restores the captured values and stops. Existing timeout and resume
security choices are preserved for rollback.

On the Windows 10 system used for this rehearsal, `.scr` right-click **Install**
uses the same `desk.cpl,InstallScreenSaver` action with the file's long path.
Windows currently selects `C:\Windows\system32\Ribbons.scr`, with a 300-second
timeout. The actual Screen Saver Settings preview and idle activation still
require owner-approved selection and have not been exercised.

Windows may restrict screensaver selection through organization policy. If it
does not retain the absolute per-user path, stop there. Do not copy only the
`.scr` into a Windows directory.

## Owner acceptance after approval

1. Run the install script with `-Apply`. Confirm the installed folder contains
   `.scr`, `dist`, attribution, and licenses. Confirm Windows still selects the
   previous screensaver.
2. Run the installed `.scr` with `/s` and `/c` through a direct process launch.
   Check all displays, primary-only countdown, native input exit, and the
   configuration dialog. Relaunch `/s` to check voyage restoration.
3. Approve selection separately, then run the select script with `-Apply`.
   Confirm that `SCRNSAVE.EXE` equals the full installed path and the prior
   timeout and security choices were captured.
4. Open Screen Saver Settings with `control.exe desk.cpl,screensaver,@screensaver`.
   Check that Reach is selected and its embedded preview renders stars and a
   planet without a countdown. Check that closing Settings ends the preview.
5. Leave the computer idle for the currently configured timeout, then check
   automatic `/s` activation, panoramic alignment, primary-only countdown,
   input exit from a secondary display, and checkpoint restoration. Review
   `%LOCALAPPDATA%\TheReachScreensaver\logs` for missing assets or blocked
   external requests.
6. When the rehearsal is complete, review the uninstall script's plan, approve
   its `-Apply` run, and confirm the previous screensaver selection and settings
   return. Confirm Reach user data remains in place.

`/s` uses the WebView2 renderer on all active displays. `/c` opens the native
Settings window. Windows `/p` preview uses one embedded WebView2 view
with an isolated profile and does not modify the saved fullscreen voyage.

## Uninstall and rollback

Run `windows\Uninstall-ReachScreensaver.ps1` to review its plan. After approval,
rerun with `-Apply`. If Reach is active, the script requires the saved snapshot
and restores the prior selection, timeout, active flag, and resume security
choice before removing the installed package. It refuses to overwrite a newer
screensaver choice or remove Reach while it remains selected. The restored
snapshot is retained as an audit record.

Uninstall does not remove `%LOCALAPPDATA%\TheReachScreensaver` or its `reach.ini`,
journey checkpoint, WebView2 profiles, visual asset cache, or logs. Removing
that user data is a separate decision.

The shared WebView2 Evergreen Runtime does not belong to this package; leave it
installed for other applications.

Diagnostics are written to `%LOCALAPPDATA%\TheReachScreensaver\logs`.
