param([switch]$Apply)

. (Join-Path $PSScriptRoot 'ReachInstall.Common.ps1')
$paths = Get-ReachInstallPaths
if (-not (Test-Path -LiteralPath $paths.Target)) {
    if (-not $Apply) { Write-Output "No installed package at $($paths.Target). No uninstall action is available."; return }
    throw "Reach install folder does not exist: $($paths.Target)"
}
if (Test-Path -LiteralPath $paths.Programs) {
    $programsItem = Get-Item -LiteralPath $paths.Programs -Force
    if (($programsItem.Attributes -band [IO.FileAttributes]::ReparsePoint) -ne 0) {
        throw 'The per-user Programs folder is a reparse point.'
    }
}
Assert-NoReparsePoints $paths.Target
$selected = Get-ReachSelectedPath
$reachSelected = Test-ReachFileEqual $selected $paths.Scr
$backupExists = Test-Path -LiteralPath $paths.Backup -PathType Leaf
if ($reachSelected -and -not $backupExists) {
    throw 'Reach is selected but the previous-settings backup is missing. Do not remove the active package.'
}

$snapshot = $null
$alreadyRestored = $false
if ($backupExists) {
    $snapshot = Get-Content -LiteralPath $paths.Backup -Raw | ConvertFrom-Json
    $savedNames = @($snapshot.Values | ForEach-Object Name | Sort-Object -Unique)
    if ($snapshot.Schema -ne 1 -or -not (Test-ReachPathEqual $snapshot.InstalledScr $paths.Scr) -or
        @($snapshot.Values).Count -ne $script:ReachSettingsNames.Count -or
        $savedNames.Count -ne $script:ReachSettingsNames.Count -or
        @($savedNames | Where-Object { $script:ReachSettingsNames -notcontains $_ }).Count -ne 0) {
        throw 'The previous-settings backup does not match this installed package.'
    }
    $alreadyRestored = Test-ReachSelectionEqual $snapshot.Values
}

Write-Output "Installed package: $($paths.Target)"
Write-Output "Current screensaver: $selected"
Write-Output $(if ($reachSelected) { 'Will restore the previous selection and settings before removing Reach.' }
    elseif ($alreadyRestored) { 'Previous settings are already restored.' }
    else { 'Reach is not selected; the newer user choice will be preserved.' })
Write-Output 'reach.ini, visual assets, WebView2 profiles, saved journey, and logs will be kept.'
if (-not $Apply) { Write-Output 'Plan only. Run again with -Apply after owner approval.'; return }

if ($reachSelected) { Restore-ReachSelection $snapshot.Values }
if (Test-ReachFileEqual (Get-ReachSelectedPath) $paths.Scr) {
    throw 'Reach remains selected; refusing to remove its executable.'
}
if ([IO.Path]::GetFullPath($paths.Target) -cne
    [IO.Path]::GetFullPath((Join-Path $paths.Programs 'TheReachScreensaver'))) {
    throw 'Unsafe uninstall target.'
}
Remove-Item -LiteralPath $paths.Target -Recurse -Force
if ($backupExists) {
    $history = Join-Path ([IO.Path]::GetDirectoryName($paths.Backup)) ('retained-' + [DateTime]::Now.ToString('yyyyMMdd-HHmmss') + '.json')
    Move-Item -LiteralPath $paths.Backup -Destination $history
    Write-Output "Previous-settings record retained at $history"
}
Write-Output 'Reach package removed. User configuration and journey data were preserved.'
