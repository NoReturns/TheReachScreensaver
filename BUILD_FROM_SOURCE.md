# Build The Reach Screensaver from source

This describes a Windows x64 build from the two public repositories:

- https://github.com/NoReturns/TheReachScreensaver (native host)
- https://github.com/NoReturns/youreyesinspace (Reach-integrated Universe fork)

The original Universe project is https://github.com/berkcomba/youreyesinspace.

Use the exact revisions selected for a release candidate. The commands below
assume sibling checkouts at `C:\Storage\Screensaver` and `C:\Storage\Universe`.
Record both commit IDs and any working-tree changes with the release evidence.
The v1.0.0 release candidate uses host version 1.0.0.

## Tools

Use Windows 10 or 11 x64, PowerShell, the .NET 10 SDK selected by
`Screensaver/global.json` (currently 10.0.102), Node.js 24.14.0, and npm
11.9.0. NuGet and npm packages must be available
for the first restore. The final self-contained host does not need an installed
.NET SDK or Node.js. Playback and preview require the separately installed
Microsoft Edge WebView2 Evergreen Runtime and WebGL-capable graphics hardware.

## Build both repositories

```powershell
Set-Location C:\Storage\Universe
npm ci
$env:BUILD_ID = 'reach-v1-candidate'
$env:REACH_PACKAGE = '1'
npm run build
Remove-Item Env:REACH_PACKAGE
Remove-Item Env:BUILD_ID

Set-Location C:\Storage\Screensaver
dotnet restore .\TheReachScreensaver.csproj
dotnet publish .\TheReachScreensaver.csproj -c Release -r win-x64 --self-contained true -p:PublishSingleFile=true -p:IncludeNativeLibrariesForSelfExtract=true -o .\publish\win-x64
```

Set a fixed `BUILD_ID` for a repeatable bundle reference. `npm ci` uses
Universe's lockfile. The Reach package build removes upstream logo, favicon,
and social preview files from `dist`, and removes their HTML references. It
does not delete those source files or affect an ordinary Universe build.
For the offline release package, omit `robots.txt` and `sitemap.xml`, which
refer to the upstream website. Replace the upstream product claim in the
`index.html` help credits with an accurate MIT attribution to Your Eyes In
Space by Berk Comba / BigBrains. Keep the full upstream license and notice.
The host publish produces both `.exe` and `.scr`; the `.scr` is a copy of the
published executable.
Byte-for-byte reproducibility across machines has not been established;
record the SDK, Node.js, npm, and source revisions used for each candidate.

## Assemble an offline folder

Create a new folder with this layout before testing or distribution:

```text
TheReachScreensaver.scr
TheReachScreensaver.exe
LICENSE
PRIVACY.md
CREDITS.txt
ATTRIBUTION.md
INSTALL.md
dist\...                 (entire Reach package build output)
licenses\Universe-LICENSE.txt
licenses\Universe-NOTICE.md
licenses\Three-LICENSE.txt
licenses\OpenTK-LICENSE.txt
licenses\WebView2-LICENSE.txt
licenses\WebView2-NOTICE.txt
licenses\DotNet-ThirdPartyNotices.txt
windows\...              (all scripts from packaging/windows)
PAYLOAD-SHA256.json      (SHA-256 manifest for staged installer validation)
```

For example, from PowerShell after both builds (choose a fresh destination):

```powershell
$hostRepo = 'C:\Storage\Screensaver'
$webRepo = 'C:\Storage\Universe'
$packageRoot = 'C:\Storage\ReachCandidate'
New-Item -ItemType Directory -Path $packageRoot, "$packageRoot\licenses" | Out-Null
Copy-Item "$hostRepo\publish\win-x64\TheReachScreensaver.scr", "$hostRepo\publish\win-x64\TheReachScreensaver.exe", "$hostRepo\publish\win-x64\LICENSE", "$hostRepo\publish\win-x64\PRIVACY.md", "$hostRepo\publish\win-x64\CREDITS.txt" -Destination $packageRoot
Copy-Item "$webRepo\dist" -Destination $packageRoot -Recurse
Copy-Item "$hostRepo\packaging\ATTRIBUTION.md", "$hostRepo\packaging\INSTALL.md" -Destination $packageRoot
Copy-Item "$hostRepo\packaging\windows" -Destination $packageRoot -Recurse
Copy-Item "$webRepo\LICENSE" "$packageRoot\licenses\Universe-LICENSE.txt"
Copy-Item "$webRepo\NOTICE.md" "$packageRoot\licenses\Universe-NOTICE.md"
Copy-Item "$webRepo\node_modules\three\LICENSE" "$packageRoot\licenses\Three-LICENSE.txt"
Copy-Item "$hostRepo\packaging\OpenTK-LICENSE.txt" "$packageRoot\licenses\OpenTK-LICENSE.txt"
$webView2 = Join-Path $env:USERPROFILE '.nuget\packages\microsoft.web.webview2\1.0.4258.31'
Copy-Item "$webView2\LICENSE.txt" "$packageRoot\licenses\WebView2-LICENSE.txt"
Copy-Item "$webView2\NOTICE.txt" "$packageRoot\licenses\WebView2-NOTICE.txt"
$dotnetRoot = Split-Path (Get-Command dotnet).Source
Copy-Item "$dotnetRoot\ThirdPartyNotices.txt" "$packageRoot\licenses\DotNet-ThirdPartyNotices.txt"
$licenseSources = @{
  'Universe-LICENSE.txt' = "$webRepo\LICENSE"
  'Universe-NOTICE.md' = "$webRepo\NOTICE.md"
  'Three-LICENSE.txt' = "$webRepo\node_modules\three\LICENSE"
  'OpenTK-LICENSE.txt' = "$hostRepo\packaging\OpenTK-LICENSE.txt"
  'WebView2-LICENSE.txt' = "$webView2\LICENSE.txt"
  'WebView2-NOTICE.txt' = "$webView2\NOTICE.txt"
  'DotNet-ThirdPartyNotices.txt' = "$dotnetRoot\ThirdPartyNotices.txt"
}
foreach ($name in $licenseSources.Keys) {
  if ((Get-FileHash -LiteralPath $licenseSources[$name] -Algorithm SHA256).Hash -cne
      (Get-FileHash -LiteralPath "$packageRoot\licenses\$name" -Algorithm SHA256).Hash) {
    throw "License copy mismatch: $name"
  }
}
if ((Get-Content "$packageRoot\CREDITS.txt" -Raw) -notmatch 'CC BY-SA 4\.0' -or
    (Get-Content "$packageRoot\licenses\Universe-NOTICE.md" -Raw) -notmatch 'CC BY-SA 4\.0') {
  throw 'HYG attribution is missing.'
}
```

The notice source is the .NET 10 runtime used for publish.
StbImageSharp is public domain; its source and status are linked in
`CREDITS.txt`. Do not use an old packaged Universe notice in place of the
current checkout's notice.

Check that `dist/index.html` exists, the `dist` folder has no upstream logo,
favicon, or social preview image files, and every license file above is
present. Run `windows/Install-ReachScreensaver.ps1 -PackagePath <folder>`
without `-Apply` for its completeness check. This is a rehearsal only.

## Run without changing Windows settings

From the assembled folder, launch the `.exe` directly:

```powershell
.\TheReachScreensaver.exe /c
.\TheReachScreensaver.exe /s
```

`/c` opens Settings; `/s` starts playback and exits on input. Use the `.exe`
for direct launching because Windows may substitute the `.scr` file
association. To test the exact `.scr` without selecting it, use a direct
process launch:

```powershell
$p = [Diagnostics.ProcessStartInfo]::new((Resolve-Path .\TheReachScreensaver.scr).Path)
$p.UseShellExecute = $false
$p.ArgumentList.Add('/s')
[Diagnostics.Process]::Start($p)
```

Windows Screen Saver Settings provides `/p` preview after selection. The
manual per-user install and separate selection path remains documented in
`packaging/INSTALL.md`.
