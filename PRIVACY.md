# Privacy

The Reach Screensaver does not include analytics or user tracking. Its code does
not send settings, journey progress, or personal data to NoReturns. The source
contains no NoReturns data-collection endpoint.

Settings (`reach.ini` and saved Settings preferences), the journey checkpoint,
WebView2 profiles, optional verified image files, and diagnostic logs stay on
your computer under `%LOCALAPPDATA%\TheReachScreensaver`. The preview uses a
separate WebView2 profile. Logs can include local paths, runtime errors, and
the host names of blocked web requests. Uninstall leaves this user data in
place; you can remove it separately after closing the screensaver.

Playback uses a local Universe bundle through WebView2 and does not download
imagery. Web resource requests to external hosts are blocked by the app. If
you explicitly approve optional enhanced imagery, the native host requests
files from the pinned GitHub Release URLs or an HTTPS mirror you configure.
Those services can receive standard connection information such as your IP
address. Files are checked against pinned sizes and SHA-256 hashes before use.
The separately installed Microsoft Edge WebView2 Evergreen Runtime may make
its own update or diagnostic connections under Microsoft's terms and settings.

Privacy questions: [noreturnsgames@gmail.com](mailto:noreturnsgames@gmail.com)
