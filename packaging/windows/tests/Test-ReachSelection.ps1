$ErrorActionPreference = 'Stop'
$source = Join-Path $PSScriptRoot '..\Select-ReachScreensaver.ps1'
$temporary = Join-Path ([IO.Path]::GetTempPath()) ('ReachSelectionTest-' + [Guid]::NewGuid().ToString('N'))
New-Item -ItemType Directory -Path $temporary | Out-Null
try {
    Copy-Item -LiteralPath $source -Destination (Join-Path $temporary 'Select-ReachScreensaver.ps1')
    @'
$script:ReachSettingsNames = @('SCRNSAVE.EXE', 'ScreenSaveActive', 'ScreenSaveTimeOut', 'ScreenSaverIsSecure')
function Get-ReachInstallPaths { return $global:ReachTestPaths }
function Get-ReachSelection {
    return @($global:ReachTestValues | ForEach-Object {
        [pscustomobject]@{ Name = $_.Name; Exists = $_.Exists; Kind = $_.Kind; Value = $_.Value }
    })
}
function Get-ReachSelectedPath { return [string](@(Get-ReachSelection | Where-Object Name -eq 'SCRNSAVE.EXE')[0].Value) }
function Test-ReachPathEqual([string]$left, [string]$right) { return $left -ieq $right }
function Test-ReachFileEqual([string]$left, [string]$right) { return $left -ieq $right }
function Test-ReachSelectionEqual($expected) {
    $actual = @(Get-ReachSelection)
    foreach ($saved in $expected) {
        $current = @($actual | Where-Object Name -eq $saved.Name)[0]
        if ($current.Exists -ne $saved.Exists -or $current.Kind -cne $saved.Kind -or
            [string]$current.Value -cne [string]$saved.Value) { return $false }
    }
    return $true
}
function Get-ReachPackageFiles([string]$path) { return $path }
function Set-ReachSelectedPath([string]$path) {
    if ($global:ReachTestFailWrite) { throw 'Synthetic write failure.' }
    $global:ReachTestValues[0].Value = $path
}
function Send-ReachDesktopSettingsChanged { }
function Restore-ReachSelection($saved) { $global:ReachTestValues = @($saved) }
'@ | Set-Content -LiteralPath (Join-Path $temporary 'ReachInstall.Common.ps1') -Encoding utf8

    $global:ReachTestPaths = [pscustomobject]@{
        Target = $temporary
        Scr = 'C:\Test Reach\TheReachScreensaver.scr'
        Backup = Join-Path $temporary 'history\previous-screensaver.json'
    }
    $global:ReachTestValues = @(
        [pscustomobject]@{ Name = 'SCRNSAVE.EXE'; Exists = $true; Kind = 'String'; Value = 'C:\Windows\System32\Ribbons.scr' },
        [pscustomobject]@{ Name = 'ScreenSaveActive'; Exists = $true; Kind = 'String'; Value = '0' },
        [pscustomobject]@{ Name = 'ScreenSaveTimeOut'; Exists = $true; Kind = 'String'; Value = '900' },
        [pscustomobject]@{ Name = 'ScreenSaverIsSecure'; Exists = $true; Kind = 'String'; Value = '1' }
    )
    $global:ReachTestFailWrite = $false
    $selector = Join-Path $temporary 'Select-ReachScreensaver.ps1'

    & $selector -Reselect | Out-Null
    if (Test-Path -LiteralPath $global:ReachTestPaths.Backup) { throw 'Plan-only selection wrote a backup.' }
    if ($global:ReachTestValues[0].Value -ne 'C:\Windows\System32\Ribbons.scr') { throw 'Plan-only selection changed the current choice.' }

    & $selector -Apply -Reselect | Out-Null
    $first = Get-Content -LiteralPath $global:ReachTestPaths.Backup -Raw | ConvertFrom-Json
    if ($first.Values[0].Value -ne 'C:\Windows\System32\Ribbons.scr') { throw 'Initial backup did not capture Ribbons.' }
    if ($global:ReachTestValues[0].Value -ne $global:ReachTestPaths.Scr) { throw 'Explicit selection did not activate Reach.' }

    & $selector -Apply -Reselect | Out-Null
    if (@(Get-ChildItem -LiteralPath (Split-Path $global:ReachTestPaths.Backup) -Filter 'superseded-*.json').Count -ne 0) {
        throw 'Already-selected Reach overwrote the backup.'
    }

    $global:ReachTestValues[0].Value = 'C:\Windows\System32\Bubbles.scr'
    & $selector -Apply -Reselect | Out-Null
    $prior = @(Get-ChildItem -LiteralPath (Split-Path $global:ReachTestPaths.Backup) -Filter 'superseded-*.json')
    if ($prior.Count -ne 1) { throw 'Earlier selection backup was not retained.' }
    $retained = Get-Content -LiteralPath $prior[0].FullName -Raw | ConvertFrom-Json
    $current = Get-Content -LiteralPath $global:ReachTestPaths.Backup -Raw | ConvertFrom-Json
    if ($retained.Values[0].Value -ne 'C:\Windows\System32\Ribbons.scr' -or
        $current.Values[0].Value -ne 'C:\Windows\System32\Bubbles.scr') {
        throw 'Reselection backups do not preserve the old and new choices.'
    }
    if (@($global:ReachTestValues | Where-Object Name -ne 'SCRNSAVE.EXE' | Where-Object { $_.Value -notin @('0', '900', '1') }).Count -ne 0) {
        throw 'Selection changed timeout, active state, or security.'
    }

    $global:ReachTestValues[0].Value = 'C:\Windows\System32\Ribbons.scr'
    $global:ReachTestFailWrite = $true
    try { & $selector -Apply -Reselect | Out-Null; throw 'Expected a synthetic selection failure.' }
    catch { if ($_.Exception.Message -notmatch 'Selection failed: Synthetic write failure') { throw } }
    if ($global:ReachTestValues[0].Value -ne 'C:\Windows\System32\Ribbons.scr') { throw 'Failed selection did not restore the previous choice.' }
    Write-Output 'Synthetic selection, reselection, backup history, no-op, and failure restoration passed.'
}
finally {
    Remove-Item -LiteralPath $temporary -Recurse -Force
    Remove-Variable ReachTestPaths, ReachTestValues, ReachTestFailWrite -Scope Global -ErrorAction SilentlyContinue
}
