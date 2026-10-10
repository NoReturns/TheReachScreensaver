# Package attribution

This package includes the production web build of Your Eyes In Space and the
native The Reach Screensaver host. See `licenses/Universe-LICENSE.txt` and
`licenses/Universe-NOTICE.md` for the universe source license and bundled data,
model, and imagery credits. The bundled HYG-derived star catalogue is credited
there and uses CC BY-SA 4.0 (https://creativecommons.org/licenses/by-sa/4.0/).
The bundled planetary imagery credits are also recorded in
`dist/textures/reach-assets.manifest.json`.

Bundled runtime dependencies and their notices:

- three.js, MIT: `licenses/Three-LICENSE.txt`.
- Microsoft WebView2 SDK loader, Microsoft license: `licenses/WebView2-LICENSE.txt`.
- .NET runtime third-party components: `licenses/DotNet-ThirdPartyNotices.txt`.
- OpenTK 4.9.4, MIT, Copyright (c) 2006-2020 Stefanos Apostolopoulos for the
  Open Toolkit library: https://github.com/opentk/opentk/blob/master/LICENSE.md
- StbImageSharp 2.30.15, public domain: https://github.com/StbSharp/StbImageSharp

WebView2 Evergreen Runtime itself is a separate prerequisite and is not
redistributed in this package. Vite and TypeScript are build tools only.
