param(
    [switch]$Commit,
    [switch]$Recover,
    [string]$BackupPath,
    [string]$LocalDataRoot
)

$ErrorActionPreference = 'Stop'
if ($Commit.IsPresent -eq $Recover.IsPresent) { throw 'Specify exactly one of -Commit or -Recover.' }

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

function Assert-ReachTree([string]$path) {
    if (-not (Test-Path -LiteralPath $path -PathType Container)) { throw "Missing directory: $path" }
    foreach ($item in @(Get-Item -LiteralPath $path -Force) +
        @(Get-ChildItem -LiteralPath $path -Recurse -Force)) {
        if ($item.Attributes -band [IO.FileAttributes]::ReparsePoint) {
            throw "Reparse point is not allowed: $($item.FullName)"
        }
    }
}

function Assert-ReachDirectory([string]$path) {
    $item = Get-Item -LiteralPath $path -Force
    if (-not $item.PSIsContainer -or $item.Attributes -band [IO.FileAttributes]::ReparsePoint) {
        throw "Unsafe directory: $path"
    }
}

function Write-ReachRecord([string]$path, $record) {
    $temp = $path + '.' + [Guid]::NewGuid().ToString('N') + '.tmp'
    [IO.File]::WriteAllText($temp, ($record | ConvertTo-Json -Depth 5),
        (New-Object System.Text.UTF8Encoding($false)))
    if (Test-Path -LiteralPath $path) {
        $previous = $path + '.' + [Guid]::NewGuid().ToString('N') + '.previous'
        Move-Item -LiteralPath $path -Destination $previous
        try { Move-Item -LiteralPath $temp -Destination $path }
        catch {
            Move-Item -LiteralPath $previous -Destination $path
            throw
        }
    }
    else { [IO.File]::Move($temp, $path) }
}

function Assert-ReachFiles([string]$path, [string]$manifestPath, [bool]$payload) {
    Assert-ReachTree $path
    $manifest = Get-Content -LiteralPath $manifestPath -Raw | ConvertFrom-Json
    if ($manifest.Schema -ne 1) { throw "Unsupported file manifest: $manifestPath" }
    $entries = @($manifest.Files)
    if ($entries.Count -lt 1) { throw 'Empty file manifest.' }
    $seen = @{}
    foreach ($entry in $entries) {
        $relative = [string]$entry.Path
        if ([string]::IsNullOrWhiteSpace($relative) -or
            [IO.Path]::IsPathRooted($relative) -or $relative -match '[:*?"<>|]' -or
            $relative -match '(^|[\\/])\.\.?([\\/]|$)') {
            throw "Unsafe manifest path: $relative"
        }
        $file = Join-Path $path $relative
        if ((Get-ReachRelativePath $path $file) -cne $relative.Replace('/', '\') -or
            $seen.ContainsKey($relative)) {
            throw "Ambiguous manifest path: $relative"
        }
        $seen[$relative] = $true
        if (-not (Test-Path -LiteralPath $file -PathType Leaf) -or
            [string]$entry.SHA256 -notmatch '\A[0-9a-fA-F]{64}\z' -or
            (Get-FileHash -LiteralPath $file -Algorithm SHA256).Hash -cne [string]$entry.SHA256) {
            throw "File hash mismatch: $relative"
        }
    }
    $actual = @(Get-ChildItem -LiteralPath $path -File -Recurse -Force)
    $expectedCount = $entries.Count + [int]$payload
    if ($actual.Count -ne $expectedCount) { throw 'File manifest count mismatch.' }
    if ($payload -and -not (Test-Path -LiteralPath (Join-Path $path 'PAYLOAD-SHA256.json') -PathType Leaf)) {
        throw 'Payload manifest is missing.'
    }
}

function Read-ReachRecord([string]$recordPath, [string]$target, [string]$stage, [string]$backupRoot) {
    if ((Get-Item -LiteralPath $recordPath -Force).Attributes -band [IO.FileAttributes]::ReparsePoint) {
        throw 'Upgrade recovery record is a reparse point.'
    }
    $record = Get-Content -LiteralPath $recordPath -Raw | ConvertFrom-Json
    $relativeBackup = Get-ReachRelativePath $backupRoot ([string]$record.Backup)
    if ($record.Schema -ne 1 -or $record.Target -ine $target -or $record.Stage -ine $stage -or
        $relativeBackup -match '[\\/]' -or -not $relativeBackup.StartsWith('pre-inno-v1.0.0-', [StringComparison]::OrdinalIgnoreCase) -or
        $record.Manifest -ine ($record.Backup + '.manifest.json') -or
        $record.MovedOriginal -ine ($record.Backup + '.original')) {
        throw 'Upgrade recovery record has unexpected paths.'
    }
    return $record
}

function Assert-NoReachProcesses {
    if (@(Get-Process -Name 'TheReachScreensaver' -ErrorAction SilentlyContinue).Count -gt 0) {
        throw 'Close all Reach playback, preview, and Settings processes before continuing.'
    }
}

$localData = if ($LocalDataRoot) { [IO.Path]::GetFullPath($LocalDataRoot) }
    else { [Environment]::GetFolderPath('LocalApplicationData') }
if ($localData -notmatch '\A[A-Za-z]:[\\/]') { throw 'LocalAppData must be on a local drive.' }
$programs = Join-Path $localData 'Programs'
$target = Join-Path $programs 'TheReachScreensaver'
$stage = Join-Path $programs 'TheReachScreensaver.pending-v1.0.0'
$dataRoot = Join-Path $localData 'TheReachScreensaver'
$backupRoot = Join-Path $dataRoot 'DeploymentBackups'
$recordPath = Join-Path $backupRoot 'pending-upgrade.json'
Assert-NoReachProcesses
if (Test-Path -LiteralPath $programs) { Assert-ReachDirectory $programs }
if (Test-Path -LiteralPath $dataRoot) { Assert-ReachDirectory $dataRoot }
if (Test-Path -LiteralPath $backupRoot) { Assert-ReachDirectory $backupRoot }

if ($Commit) {
    Assert-ReachFiles $stage (Join-Path $stage 'PAYLOAD-SHA256.json') $true
    $record = $null
    if (Test-Path -LiteralPath $recordPath) {
        $record = Read-ReachRecord $recordPath $target $stage $backupRoot
        if ($record.State -ne 'prepared' -or -not (Test-Path -LiteralPath $target -PathType Container) -or
            (Test-Path -LiteralPath $record.MovedOriginal)) { throw 'Upgrade state changed after backup.' }
        Assert-ReachFiles $record.Backup $record.Manifest $false
        Assert-ReachFiles $target $record.Manifest $false
    }
    elseif (Test-Path -LiteralPath $target) {
        Assert-ReachTree $target
        if (@(Get-ChildItem -LiteralPath $target -Force).Count -ne 0) {
            throw 'Existing installation has no verified recovery record.'
        }
    }
    try {
        if ($record) {
            $record.State = 'swapping'
            Write-ReachRecord $recordPath $record
            Move-Item -LiteralPath $target -Destination $record.MovedOriginal
        }
        elseif (Test-Path -LiteralPath $target) {
            Remove-Item -LiteralPath $target
        }
        Move-Item -LiteralPath $stage -Destination $target
        Assert-ReachFiles $target (Join-Path $target 'PAYLOAD-SHA256.json') $true
        if ($record) {
            $record.State = 'completed'
            Write-ReachRecord $recordPath $record
            $history = Join-Path $backupRoot ('completed-' + [Guid]::NewGuid().ToString('N') + '.json')
            Move-Item -LiteralPath $recordPath -Destination $history
        }
        Write-Output "Verified Reach payload installed: $target"
    }
    catch {
        $cause = $_.Exception.Message
        if ($record -and (Test-Path -LiteralPath $record.MovedOriginal -PathType Container)) {
            try {
                if (Test-Path -LiteralPath $target) {
                    $failed = Join-Path $backupRoot ('failed-payload-' + [Guid]::NewGuid().ToString('N'))
                    Move-Item -LiteralPath $target -Destination $failed
                }
                Move-Item -LiteralPath $record.MovedOriginal -Destination $target
                Assert-ReachFiles $target $record.Manifest $false
            }
            catch { throw "Upgrade failed: $cause. Automatic restoration also failed: $($_.Exception.Message). Use -Recover." }
        }
        throw "Upgrade failed: $cause. Previous files remain or were restored; use -Recover if needed."
    }
    return
}

if (-not (Test-Path -LiteralPath $backupRoot -PathType Container)) {
    throw 'No rollback backups exist.'
}
$record = $null
$unreadableRecord = $false
if (Test-Path -LiteralPath $recordPath) {
    try {
        $record = Read-ReachRecord $recordPath $target $stage $backupRoot
        $BackupPath = [string]$record.Backup
    }
    catch {
        if (-not $BackupPath -or
            (Get-Item -LiteralPath $recordPath -Force).Attributes -band [IO.FileAttributes]::ReparsePoint) {
            throw
        }
        $unreadableRecord = $true
    }
}
if (-not $BackupPath) { throw 'Specify -BackupPath for a completed upgrade.' }
$backupRelative = Get-ReachRelativePath $backupRoot $BackupPath
if ($backupRelative -match '[\\/]' -or $backupRelative -notlike 'pre-inno-v1.0.0-*') {
    throw 'Backup path must name a direct child of DeploymentBackups.'
}
$manifestPath = $BackupPath + '.manifest.json'
Assert-ReachFiles $BackupPath $manifestPath $false
$restoreStage = Join-Path $programs ('TheReachScreensaver.recovery-' + [Guid]::NewGuid().ToString('N'))
Copy-Item -LiteralPath $BackupPath -Destination $restoreStage -Recurse
Assert-ReachFiles $restoreStage $manifestPath $false
$failedPath = Join-Path $backupRoot ('replaced-install-' + [Guid]::NewGuid().ToString('N'))
try {
    if (Test-Path -LiteralPath $target) {
        Assert-ReachTree $target
        Move-Item -LiteralPath $target -Destination $failedPath
    }
    Move-Item -LiteralPath $restoreStage -Destination $target
    Assert-ReachFiles $target $manifestPath $false
}
catch {
    if (-not (Test-Path -LiteralPath $target) -and (Test-Path -LiteralPath $failedPath)) {
        Move-Item -LiteralPath $failedPath -Destination $target
    }
    throw
}
if (Test-Path -LiteralPath $stage) {
    Assert-ReachTree $stage
    Move-Item -LiteralPath $stage -Destination (Join-Path $backupRoot ('abandoned-stage-' + [Guid]::NewGuid().ToString('N')))
}
if ($record) {
    $record.State = 'recovered'
    Write-ReachRecord $recordPath $record
    Move-Item -LiteralPath $recordPath -Destination (Join-Path $backupRoot ('recovered-' + [Guid]::NewGuid().ToString('N') + '.json'))
}
elseif ($unreadableRecord) {
    Move-Item -LiteralPath $recordPath -Destination (Join-Path $backupRoot ('unreadable-record-' + [Guid]::NewGuid().ToString('N') + '.json'))
}
Write-Output "Verified prior Reach files restored: $target"
