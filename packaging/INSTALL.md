# The Reach Screensaver - manual installation

This folder is a self-contained Windows x64 screensaver package. Keep
`TheReachScreensaver.scr`, `LICENSE`, `PRIVACY.md`, `CREDITS.txt`, `ATTRIBUTION.md`, the `licenses`
directory, and the complete `dist` directory together. No .NET
SDK, Node.js, npm, source checkout, web server, or network connection is needed
for playback.

## Windows installer

The installer for the selected release installs this complete package to
`%LOCALAPPDATA%\Programs\TheReachScreensaver` for the current user. The final
page offers "Set The Reach as my Windows screensaver" and "Open The Reach
Screensaver Settings". Both are unchecked by default. If selection is checked,
Setup runs the same backup and selection procedure as
`Select-ReachScreensaver.ps1 -Apply`. It preserves the existing idle timeout,
active state, and resume security choice. If selection fails, Setup displays an
error and restores the previous settings. The WebView2 Evergreen Runtime is a
separate prerequisite.

Setup creates **Start > The Reach Screensaver > Select The Reach Screensaver**
for the current user. Opening it later explicitly selects Reach, shows a
success or error dialog, and uses the same selection verification. If the user
has switched to another saver, the earlier selection backup is retained as a
`superseded-*.json` history record before the new choice is captured. If Reach
is already selected, the shortcut leaves the backup unchanged. No administrator
rights are needed for ordinary installation or selection. Uninstall removes
the installer-created Start Menu shortcut.

Windows builds the native Screen Saver Settings list from `.scr` files in the
Windows system directory. The per-user installed `.scr` can be selected, but
does not remain in that list after choosing another saver. The final-page
selection action sets the current choice; it does not register a permanent
dropdown entry. A reliable permanent entry would need a separate elevated
system-directory launcher that forwards `/s`, `/c`, and `/p` to the complete
per-user package and handles preview lifetime, upgrades, all users, and
uninstall. No such launcher is included in this per-user release.

Before replacing an existing copy, Setup requires all Reach processes to be
closed. It copies the complete existing install to
`%LOCALAPPDATA%\TheReachScreensaver\DeploymentBackups`, verifies every file by
SHA-256, and records the backup path. Setup then extracts the new package to a
sibling staging directory and verifies its file manifest. Only then does it
move the original directory into DeploymentBackups and move the complete staged
package into place. The original directory and verified copy backup are kept.
User settings, journey progress, downloaded imagery, WebView2 profiles, and
logs are stored separately and are not part of the replacement.

If staging verification fails, the existing install stays in place. If the
directory swap fails, Setup attempts to restore the original directory. A
power interruption can stop that attempt; the verified backup and a recovery
record remain. Do not retry Setup while a pending upgrade record or staging
directory exists. Close Reach, inspect the backup, and run:

```powershell
& .\windows\Complete-ReachUpgrade.ps1 -Recover
```

Run this from the extracted package. For a completed upgrade that must
be rolled back, pass `-BackupPath` with the exact verified `pre-inno-v1.0.0-*`
directory reported by Setup under `DeploymentBackups`. Recovery copies and
hash-verifies that backup before moving the current install aside; it keeps
both the source backup and the displaced install for inspection. If recovery
fails or the pending record is missing or unreadable after an interruption, use the exact
backup path with `-BackupPath` and inspect the retained directories. This
process is not fully transactional across power loss or Windows failure.

The installer uninstall leaves user settings, saved journeys, downloaded imagery,
WebView2 profiles, and logs in place. It offers an unchecked option to remove
retained upgrade backups after the application is removed. The uninstall prompt
shows the detected folder and size and can open the folder. Removing backups
prevents restoring earlier installations. If backup inspection is unsafe or an
upgrade recovery record is pending, the option is unavailable. In owner testing,
an unrecognized historical `pending-upgrade.json.*.previous` artifact also
disabled cleanup and preserved the backups. Actual deletion was verified in
synthetic tests, not in the owner's Windows VM. Silent uninstall
keeps backups. If Reach is selected as the Windows screen saver, uninstall asks you to
select another saver first. An inaccessible or malformed selected path also
stops uninstall, with a separate diagnostic. Ribbons, Bubbles, and no selected
saver do not block uninstall.
It does not change Windows screen saver settings for you. Close Reach before
uninstalling. The installer is not digitally signed.

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
Installing the files and selecting Reach are separate actions.
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
It writes the installed `.scr`'s full absolute path to the current user's
`SCRNSAVE.EXE` registry value, broadcasts the desktop settings change, and
verifies that Windows retained the path. If selection
fails, it restores the captured values and stops. Existing timeout and resume
security choices are preserved for rollback.

The script preserves the current `ScreenSaveActive`, `ScreenSaveTimeOut`, and
`ScreenSaverIsSecure` values. It does not enable an inactive screensaver or
change the idle timeout. Preview and idle activation still need validation
with the selected release candidate.

Windows may restrict screensaver selection through organization policy. If it
does not retain the absolute per-user path, stop there. Do not copy only the
`.scr` into a Windows directory.

## Owner acceptance after approval

1. Run the install script with `-Apply`. Confirm the installed folder contains
   `.scr`, `dist`, attribution, and licenses. Confirm Windows still selects the
   previous screensaver.
2. Run the installed `.scr` with `/s` and `/c` through a direct process launch.
   Check all displays, primary-only countdown and distance display, native
   input exit, Settings, Reset Trip, and Open Credits. Relaunch `/s` to check
   voyage restoration and a scheduled reset.
3. Approve selection separately, then run the select script with `-Apply`.
   Confirm that `SCRNSAVE.EXE` equals the full installed path and the prior
   active flag, timeout, and security choices were captured.
4. Open Screen Saver Settings with `control.exe desk.cpl,screensaver,@screensaver`.
   Check that Reach is selected and its embedded preview renders stars and a
   planet without a countdown. Check that closing Settings ends the preview.
5. Leave the computer idle for the currently configured timeout, then check
   automatic `/s` activation, panoramic alignment, primary-only overlays,
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

If the user has selected a different saver after selecting Reach, manual
uninstall preserves that newer choice and retains the earlier settings backup
as an audit record. It does not overwrite the newer choice.

Uninstall does not remove `%LOCALAPPDATA%\TheReachScreensaver` or its `reach.ini`,
journey checkpoint, WebView2 profiles, visual asset cache, or logs. Removing
that user data is a separate decision.

The shared WebView2 Evergreen Runtime does not belong to this package; leave it
installed for other applications.

Diagnostics are written to `%LOCALAPPDATA%\TheReachScreensaver\logs`.
