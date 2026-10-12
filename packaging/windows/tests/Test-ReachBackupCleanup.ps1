$ErrorActionPreference = 'Stop'
$scriptPath = Join-Path (Split-Path $PSScriptRoot -Parent) 'Cleanup-ReachDeploymentBackups.ps1'
$installerScript = Get-Content -LiteralPath (Join-Path (Split-Path $PSScriptRoot -Parent) 'TheReachScreensaver.iss') -Raw
$testRoot = Join-Path $env:TEMP ('ReachBackupCleanupTest-' + [Guid]::NewGuid().ToString('N'))
$backupName = 'pre-inno-v1.0.0-20261012T120000Z-aaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaa'

function New-Case([string]$name) {
    $localData = Join-Path $testRoot $name
    $backups = Join-Path $localData 'TheReachScreensaver\DeploymentBackups'
    $backup = Join-Path $backups $backupName
    New-Item -ItemType Directory -Path $backup -Force | Out-Null
    [IO.File]::WriteAllText((Join-Path $backup 'TheReachScreensaver.scr'), 'synthetic Reach')
    [pscustomobject]@{ LocalData = $localData; Backups = $backups; Backup = $backup;
        File = (Join-Path $backup 'TheReachScreensaver.scr'); Report = (Join-Path $localData 'report.txt') }
}

function Invoke-Case([string]$shell, $case, [string]$action, [int]$expectedExit, [string]$expectedStatus) {
    & $shell -NoProfile -ExecutionPolicy Bypass -File $scriptPath -Action $action -ReportFile $case.Report -LocalDataRoot $case.LocalData | Out-Null
    if ($LASTEXITCODE -ne $expectedExit) { throw "$action exited $LASTEXITCODE; expected $expectedExit" }
    $lines = @(Get-Content -LiteralPath $case.Report)
    if ($lines[0] -ne $expectedStatus -or $lines[1] -ne $case.Backups) {
        throw "$action report mismatch: $($lines -join ' | ')"
    }
    return $lines
}

$resolvedTemp = [IO.Path]::GetFullPath($env:TEMP).TrimEnd('\') + '\'
$resolvedTest = [IO.Path]::GetFullPath($testRoot)
if (-not $resolvedTest.StartsWith($resolvedTemp, [StringComparison]::OrdinalIgnoreCase) -or
    [IO.Path]::GetFileName($resolvedTest) -notmatch '^ReachBackupCleanupTest-[0-9a-f]{32}$') {
    throw 'Unsafe synthetic test root.'
}
New-Item -ItemType Directory -Path $testRoot | Out-Null
try {
    foreach ($shell in @((Join-Path $env:SystemRoot 'System32\WindowsPowerShell\v1.0\powershell.exe'),
            (Join-Path $env:SystemRoot 'SysWOW64\WindowsPowerShell\v1.0\powershell.exe'))) {
        if (-not (Test-Path -LiteralPath $shell -PathType Leaf)) { throw "PowerShell missing: $shell" }
        $suffix = if ($shell -match 'SysWOW64') { 'x86' } else { 'x64' }

        $unchecked = New-Case "unchecked-$suffix"
        Invoke-Case $shell $unchecked 'Inspect' 0 'READY' | Out-Null
        if (-not (Test-Path -LiteralPath $unchecked.File)) { throw 'Unchecked inspection deleted a backup.' }

        $missing = [pscustomobject]@{ LocalData = (Join-Path $testRoot "missing-$suffix");
            Backups = (Join-Path $testRoot "missing-$suffix\TheReachScreensaver\DeploymentBackups");
            Report = (Join-Path $testRoot "missing-$suffix-report.txt") }
        $missingReport = Invoke-Case $shell $missing 'Inspect' 0 'MISSING'
        if ($missingReport[3] -notmatch 'does not exist') { throw 'Missing folder lacks a diagnostic.' }

        $empty = Join-Path $testRoot "empty-$suffix"
        $emptyBackups = Join-Path $empty 'TheReachScreensaver\DeploymentBackups'
        New-Item -ItemType Directory -Path $emptyBackups -Force | Out-Null
        $emptyCase = [pscustomobject]@{ LocalData = $empty; Backups = $emptyBackups;
            Report = (Join-Path $empty 'report.txt') }
        $emptyReport = Invoke-Case $shell $emptyCase 'Inspect' 0 'EMPTY'
        if ($emptyReport[2] -ne '0.00' -or $emptyReport[3] -notmatch 'exists but is empty') {
            throw 'Existing empty backup folder was not distinguished from a missing folder.'
        }

        $cancelled = New-Case "cancelled-$suffix"
        Invoke-Case $shell $cancelled 'Inspect' 0 'READY' | Out-Null
        if (-not (Test-Path -LiteralPath $cancelled.File)) { throw 'Cancelled uninstall deleted a backup.' }

        $refused = New-Case "refused-$suffix"
        $installed = Join-Path $refused.LocalData 'Programs\TheReachScreensaver'
        New-Item -ItemType Directory -Path $installed -Force | Out-Null
        Invoke-Case $shell $refused 'Remove' 2 'UNSAFE' | Out-Null
        if (-not (Test-Path -LiteralPath $refused.File)) { throw 'Refused uninstall deleted a backup.' }

        $checked = New-Case "checked-$suffix"
        $original = $checked.Backup + '.original'
        $originalNested = Join-Path $original 'dist\textures'
        New-Item -ItemType Directory -Path $originalNested -Force | Out-Null
        [IO.File]::WriteAllBytes((Join-Path $original 'TheReachScreensaver.scr'), (New-Object byte[] (2MB)))
        [IO.File]::WriteAllBytes((Join-Path $originalNested 'reach-earth-bmng-2k.jpg'), (New-Object byte[] (1MB)))
        $manifest = $checked.Backup + '.manifest.json'
        [IO.File]::WriteAllText($manifest, '{"Schema":1,"Files":[]}')
        $completed = Join-Path $checked.Backups ('completed-' + ('b' * 32) + '.json')
        [IO.File]::WriteAllText($completed, '{"State":"completed"}')
        foreach ($sibling in @('Installation', 'Images', 'WebView2', 'Logs', 'Journey')) {
            $folder = Join-Path $checked.LocalData (Join-Path 'TheReachScreensaver' $sibling)
            New-Item -ItemType Directory -Path $folder -Force | Out-Null
            [IO.File]::WriteAllText((Join-Path $folder 'keep.txt'), 'user data')
        }
        $inspected = Invoke-Case $shell $checked 'Inspect' 0 'READY'
        $expectedBytes = (Get-Item -LiteralPath $checked.File).Length +
            (Get-Item -LiteralPath (Join-Path $original 'TheReachScreensaver.scr')).Length +
            (Get-Item -LiteralPath (Join-Path $originalNested 'reach-earth-bmng-2k.jpg')).Length +
            (Get-Item -LiteralPath $manifest).Length + (Get-Item -LiteralPath $completed).Length
        $expectedSize = ([double]$expectedBytes / 1MB).ToString('F2', [Globalization.CultureInfo]::InvariantCulture)
        if ($inspected[2] -ne $expectedSize -or $expectedSize -ne '3.00') {
            throw "Inspection size did not include the full pre-inno and .original backup set: $($inspected[2])"
        }
        $removed = Invoke-Case $shell $checked 'Remove' 0 'REMOVED'
        if ($removed[2] -ne $inspected[2]) { throw 'Removal size differed from inspected validated backup set.' }
        if ((Get-ChildItem -LiteralPath $checked.Backups -Force | Measure-Object).Count -ne 0) {
            throw 'Checked cleanup left backup contents.'
        }
        foreach ($sibling in @('Installation', 'Images', 'WebView2', 'Logs', 'Journey')) {
            if (-not (Test-Path -LiteralPath (Join-Path $checked.LocalData (Join-Path (Join-Path 'TheReachScreensaver' $sibling) 'keep.txt')))) {
                throw "Checked cleanup removed unrelated $sibling data."
            }
        }

        $lifecycle = New-Case "lifecycle-$suffix"
        $installedScripts = Join-Path $lifecycle.LocalData 'Programs\TheReachScreensaver\windows'
        New-Item -ItemType Directory -Path $installedScripts -Force | Out-Null
        $installedHelper = Join-Path $installedScripts 'Cleanup-ReachDeploymentBackups.ps1'
        Copy-Item -LiteralPath $scriptPath -Destination $installedHelper
        $temporaryHelper = Join-Path $testRoot "Cleanup-ReachDeploymentBackups-$suffix.ps1"
        Copy-Item -LiteralPath $installedHelper -Destination $temporaryHelper
        Invoke-Case $shell $lifecycle 'Inspect' 0 'READY' | Out-Null
        Remove-Item -LiteralPath (Split-Path $installedScripts -Parent) -Recurse -Force
        if (Test-Path -LiteralPath $installedHelper) { throw 'Synthetic uninstall left the installed cleanup helper.' }
        & $shell -NoProfile -ExecutionPolicy Bypass -File $temporaryHelper -Action Remove -ReportFile $lifecycle.Report -LocalDataRoot $lifecycle.LocalData | Out-Null
        if ($LASTEXITCODE -ne 0 -or (Get-Content -LiteralPath $lifecycle.Report -First 1) -ne 'REMOVED' -or
            (Test-Path -LiteralPath $lifecycle.File)) {
            throw 'Temporary cleanup helper failed after synthetic application removal.'
        }

        $pending = New-Case "pending-$suffix"
        [IO.File]::WriteAllText((Join-Path $pending.Backups 'pending-upgrade.json'), '{}')
        $pendingReport = Invoke-Case $shell $pending 'Remove' 2 'UNSAFE'
        if ($pendingReport[3] -notmatch 'unfinished upgrade recovery record') {
            throw 'Pending recovery refusal lacks a diagnostic.'
        }
        if (-not (Test-Path -LiteralPath $pending.File)) { throw 'Pending recovery backup was removed.' }

        $unknown = New-Case "unknown-$suffix"
        [IO.File]::WriteAllText((Join-Path $unknown.Backups 'unrelated.txt'), 'keep')
        Invoke-Case $shell $unknown 'Remove' 2 'UNSAFE' | Out-Null
        if (-not (Test-Path -LiteralPath $unknown.File) -or
            -not (Test-Path -LiteralPath (Join-Path $unknown.Backups 'unrelated.txt'))) {
            throw 'Unknown backup-root content was removed.'
        }

        $linked = New-Case "junction-$suffix"
        $outside = Join-Path $testRoot "outside-$suffix"
        New-Item -ItemType Directory -Path $outside | Out-Null
        [IO.File]::WriteAllText((Join-Path $outside 'keep.txt'), 'unrelated')
        $junction = Join-Path $linked.Backup 'linked'
        New-Item -ItemType Junction -Path $junction -Target $outside | Out-Null
        Invoke-Case $shell $linked 'Remove' 2 'UNSAFE' | Out-Null
        if (-not (Test-Path -LiteralPath (Join-Path $outside 'keep.txt'))) { throw 'Junction escaped backup root.' }
        if (((Get-Item -LiteralPath $junction -Force).Attributes -band [IO.FileAttributes]::ReparsePoint) -eq 0) {
            throw 'Expected a synthetic junction.'
        }
        [IO.Directory]::Delete($junction, $false)

        $parentLink = New-Case "root-junction-$suffix"
        $externalRoot = Join-Path $testRoot "external-backups-$suffix"
        foreach ($movePath in @($parentLink.Backups, $externalRoot)) {
            if (-not [IO.Path]::GetFullPath($movePath).StartsWith($resolvedTest + '\', [StringComparison]::OrdinalIgnoreCase)) {
                throw 'Unsafe synthetic backup move path.'
            }
        }
        [IO.Directory]::Move($parentLink.Backups, $externalRoot)
        New-Item -ItemType Junction -Path $parentLink.Backups -Target $externalRoot | Out-Null
        Invoke-Case $shell $parentLink 'Remove' 2 'UNSAFE' | Out-Null
        if (-not (Test-Path -LiteralPath (Join-Path $externalRoot (Join-Path $backupName 'TheReachScreensaver.scr')))) {
            throw 'Backup-root junction was followed.'
        }
        [IO.Directory]::Delete($parentLink.Backups, $false)

        $locked = New-Case "locked-$suffix"
        $stream = New-Object IO.FileStream($locked.File, [IO.FileMode]::Open, [IO.FileAccess]::ReadWrite, [IO.FileShare]::None)
        try {
        $lockedReport = Invoke-Case $shell $locked 'Remove' 3 'PARTIAL'
        if ($lockedReport[3] -notmatch [regex]::Escape($locked.File)) {
            throw 'Locked backup report omitted the untouched file path.'
        }
            if (-not (Test-Path -LiteralPath $locked.File)) { throw 'Locked backup file was removed.' }
        }
        finally { $stream.Dispose() }
    }
    if ($installerScript -notmatch 'CurUninstallStep <> usDone\) or not BackupCleanupRequested' -or
        $installerScript -notmatch 'DirExists\(ExpandConstant' -or
        $installerScript -notmatch 'CleanupCheck\.Checked := False' -or
        $installerScript -notmatch 'if UninstallSilent then' -or
        $installerScript -notmatch 'LocalDataRoot' -or
        $installerScript -notmatch 'OpenButton\.Enabled := FolderExists') {
        throw 'Installer cleanup lifecycle or unchecked default is missing.'
    }
    Write-Output 'Backup cleanup and copied-helper uninstall lifecycle cases passed in 64-bit and 32-bit Windows PowerShell.'
}
finally {
    $resolvedTest = [IO.Path]::GetFullPath($testRoot)
    if (-not $resolvedTest.StartsWith($resolvedTemp, [StringComparison]::OrdinalIgnoreCase) -or
        [IO.Path]::GetFileName($resolvedTest) -notmatch '^ReachBackupCleanupTest-[0-9a-f]{32}$') {
        throw 'Unsafe test cleanup target.'
    }
    Remove-Item -LiteralPath $resolvedTest -Recurse -Force
}
