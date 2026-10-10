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

## Install (manual, per user)

1. Copy this entire folder to a permanent location you can write, such as
   `%LOCALAPPDATA%\Programs\TheReachScreensaver`. Do not move only the `.scr`.
2. In PowerShell, test it with:
   `& "$env:LOCALAPPDATA\Programs\TheReachScreensaver\TheReachScreensaver.scr" /s`
   A key press, click, or deliberate mouse movement exits every display.
3. In File Explorer, right-click `TheReachScreensaver.scr` in that permanent
   folder and choose **Install** (use **Show more options** if needed). This is
   the user-initiated Windows action that selects the screensaver. Open Screen
   Saver Settings to confirm the selection and set the wait time. Do not move
   the package after selecting it.

Windows may restrict screensaver selection through organization policy. If the
Install action is unavailable or Windows does not retain a screensaver outside
its system directories, stop there rather than copying files to Windows or
changing registry values. That Windows installation path has not been tested
for this package.

`/s` uses the WebView2 renderer on all active displays. `/c` keeps the existing
configuration behavior. Windows `/p` preview uses one embedded WebView2 view
with an isolated profile and does not modify the saved fullscreen voyage.

## Uninstall (manual)

1. In Screen Saver Settings, select **None** or another screensaver and apply.
2. Delete this package folder from its permanent location.
3. Leave `%LOCALAPPDATA%\TheReachScreensaver` in place to retain `reach.ini`,
   the journey checkpoint, cached imagery, and logs. Delete that user-data
   folder only if you intentionally want to erase those settings and data.

The shared WebView2 Evergreen Runtime does not belong to this package; leave it
installed for other applications.

Diagnostics are written to `%LOCALAPPDATA%\TheReachScreensaver\logs`.
