param(
    [Parameter(Mandatory = $true)][string]$PackagePath,
    [switch]$Apply
)

function Get-ReachRelativePath([string]$root, [string]$path) {
    if ($root -notmatch '\A[A-Za-z]:[\\/]' -or $path -notmatch '\A[A-Za-z]:[\\/]' -or
        $root -match '[*?"<>|]' -or $path -match '[*?"<>|]' -or
        $root.Substring(2) -match ':' -or $path.Substring(2) -match ':') {
        throw 'Expected absolute local drive paths without invalid characters.'
    }
    $fullRoot = [IO.Path]::GetFullPath($root.Replace('/', '\'))
    $fullPath = [IO.Path]::GetFullPath($path.Replace('/', '\'))
    $prefix = $fullRoot.TrimEnd('\') + '\'
    if (-not $fullPath.StartsWith($prefix, [StringComparison]::OrdinalIgnoreCase)) {
        throw "Path is outside the source directory: $path"
    }
    return $fullPath.Substring($prefix.Length)
}

. (Join-Path $PSScriptRoot 'ReachInstall.Common.ps1')
$paths = Get-ReachInstallPaths
$package = Get-ReachPackageFiles $PackagePath
if (Test-ReachPathEqual $package.Root $paths.Target) { throw 'The source is already the install folder.' }
if (Test-Path -LiteralPath $paths.Target) { throw "Install folder already exists: $($paths.Target)" }
if (Test-Path -LiteralPath $paths.Programs) {
    $programsItem = Get-Item -LiteralPath $paths.Programs -Force
    if (($programsItem.Attributes -band [IO.FileAttributes]::ReparsePoint) -ne 0) {
        throw 'The per-user Programs folder is a reparse point.'
    }
}

Write-Output "Source: $($package.Root)"
Write-Output "Destination: $($paths.Target)"
Write-Output "Package files: $($package.Files.Count)"
Write-Output 'This action copies files only. It does not select or activate a screensaver.'
if (-not $Apply) { Write-Output 'Plan only. Run again with -Apply after owner approval.'; return }

New-Item -ItemType Directory -Path $paths.Programs -Force | Out-Null
$pending = Join-Path $paths.Programs ('TheReachScreensaver.pending-' + [Guid]::NewGuid().ToString('N'))
New-Item -ItemType Directory -Path $pending | Out-Null
try {
    foreach ($file in $package.Files) {
        $relative = Get-ReachRelativePath $package.Root $file.FullName
        $copy = Join-Path $pending $relative
        New-Item -ItemType Directory -Path ([IO.Path]::GetDirectoryName($copy)) -Force | Out-Null
        Copy-Item -LiteralPath $file.FullName -Destination $copy
        if ((Get-FileHash -LiteralPath $file.FullName -Algorithm SHA256).Hash -cne
            (Get-FileHash -LiteralPath $copy -Algorithm SHA256).Hash) {
            throw "Copied file failed SHA256 verification: $relative"
        }
    }
    if (Test-Path -LiteralPath $paths.Target) { throw 'Install folder appeared during copy; refusing to replace it.' }
    Move-Item -LiteralPath $pending -Destination $paths.Target
    Write-Output "Installed package: $($paths.Target)"
    Write-Output 'Reach is not selected. Run Select-ReachScreensaver.ps1 separately if approved.'
}
catch {
    Write-Warning "Install stopped. Any incomplete copy remains at $pending for inspection."
    throw
}
