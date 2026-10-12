param([switch]$Apply, [switch]$Reselect)

. (Join-Path $PSScriptRoot 'ReachInstall.Common.ps1')
$paths = Get-ReachInstallPaths
$previous = @(Get-ReachSelection)
$selected = Get-ReachSelectedPath
$alreadySelected = Test-ReachFileEqual $selected $paths.Scr
if ($alreadySelected -and -not $Reselect) { throw 'Reach is already selected; no previous selection can be captured now.' }
$backupExists = Test-Path -LiteralPath $paths.Backup
$backupIsCurrent = $false
if ($backupExists) {
    if (-not (Test-Path -LiteralPath $paths.Backup -PathType Leaf)) { throw 'The previous-settings backup is not a file.' }
    $snapshot = Get-Content -LiteralPath $paths.Backup -Raw | ConvertFrom-Json
    $savedNames = @($snapshot.Values | ForEach-Object Name | Sort-Object -Unique)
    if ($snapshot.Schema -ne 1 -or -not (Test-ReachPathEqual $snapshot.InstalledScr $paths.Scr) -or
        @($snapshot.Values).Count -ne $script:ReachSettingsNames.Count -or
        $savedNames.Count -ne $script:ReachSettingsNames.Count -or
        @($savedNames | Where-Object { $script:ReachSettingsNames -notcontains $_ }).Count -ne 0) {
        throw 'The previous-settings backup does not match this installed package.'
    }
    $backupIsCurrent = Test-ReachSelectionEqual $snapshot.Values
    if (-not $backupIsCurrent -and -not $Reselect -and -not $alreadySelected) {
        throw 'The screensaver settings changed after the backup. Refusing to overwrite a newer user choice.'
    }
}

Write-Output "Current screensaver: $selected"
Write-Output "Proposed screensaver: $($paths.Scr)"
Write-Output "Backup file: $($paths.Backup)"
Write-Output 'This action asks Windows to select Reach. It does not change the existing timeout or security choice intentionally.'
if (-not $Apply) { Write-Output 'Plan only. Run again with -Apply after separate owner approval.'; return }
Get-ReachPackageFiles $paths.Target | Out-Null
if ($alreadySelected) {
    Write-Output 'Reach is already selected. The previous-selection backup was left unchanged.'
    return
}

if ($backupExists -and -not $backupIsCurrent) {
    $history = Join-Path ([IO.Path]::GetDirectoryName($paths.Backup)) ('superseded-' + [DateTime]::UtcNow.ToString('yyyyMMdd-HHmmss') + '-' + [Guid]::NewGuid().ToString('N') + '.json')
    Move-Item -LiteralPath $paths.Backup -Destination $history
    Write-Output "Earlier selection backup retained at $history"
    $backupExists = $false
}

if (-not $backupExists) {
    $backupFolder = [IO.Path]::GetDirectoryName($paths.Backup)
    New-Item -ItemType Directory -Path $backupFolder -Force | Out-Null
    $snapshot = [pscustomobject]@{
        Schema = 1
        CapturedAt = [DateTimeOffset]::Now.ToString('o')
        InstalledScr = $paths.Scr
        Values = $previous
    }
    $snapshot | ConvertTo-Json -Depth 5 | Set-Content -LiteralPath $paths.Backup -Encoding utf8 -NoNewline
}

try {
    Set-ReachSelectedPath $paths.Scr
    Send-ReachDesktopSettingsChanged

    $entry = @(Get-ReachSelection | Where-Object Name -eq 'SCRNSAVE.EXE')[0]
    if (-not $entry.Exists -or $entry.Kind -ne 'String' -or
        -not (Test-ReachFileEqual ([string]$entry.Value) $paths.Scr) -or
        -not (Test-ReachSelectionEqual @($previous | Where-Object Name -ne 'SCRNSAVE.EXE'))) {
        throw 'Windows did not retain the installed absolute path or changed another screensaver setting.'
    }
}
catch {
    $reason = $_.Exception.Message
    Restore-ReachSelection $previous
    throw "Selection failed: $reason Previous settings were restored; keep the backup for review."
}
Write-Output "Windows selected: $($paths.Scr)"
Write-Output 'Confirm preview, timeout, security choice, and idle activation in Screen Saver Settings.'
