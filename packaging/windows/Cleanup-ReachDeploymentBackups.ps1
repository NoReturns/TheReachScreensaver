param(
    [Parameter(Mandatory = $true)][ValidateSet('Inspect', 'Remove')][string]$Action,
    [Parameter(Mandatory = $true)][string]$ReportFile,
    [string]$LocalDataRoot
)

Set-StrictMode -Version Latest
$ErrorActionPreference = 'Stop'

function Write-Report([string]$status, [string]$path, [string]$size, [string]$message) {
    $lines = @($status, $path, $size, $message)
    [IO.File]::WriteAllLines($ReportFile, $lines, [Text.Encoding]::Unicode)
}

function Assert-PlainDirectory([string]$path) {
    $item = Get-Item -LiteralPath $path -Force -ErrorAction Stop
    if (-not $item.PSIsContainer -or ($item.Attributes -band [IO.FileAttributes]::ReparsePoint) -ne 0) {
        throw "Not a normal directory: $path"
    }
}

function Assert-PlainPathToRoot([string]$path) {
    $current = $path
    while ($current) {
        Assert-PlainDirectory $current
        $parent = [IO.Path]::GetDirectoryName($current.TrimEnd('\'))
        if (-not $parent -or $parent.Equals($current, [StringComparison]::OrdinalIgnoreCase)) { return }
        $current = $parent
    }
}

function Assert-WithinBackups([string]$path, [string]$root) {
    $full = [IO.Path]::GetFullPath($path)
    $prefix = $root.TrimEnd('\') + '\'
    if (-not $full.StartsWith($prefix, [StringComparison]::OrdinalIgnoreCase)) {
        throw "Backup item escapes the expected directory: $path"
    }
}

function Assert-PlainAncestors([string]$path, [string]$root) {
    $current = [IO.Path]::GetDirectoryName($path)
    while ($current -and $current.Length -ge $root.Length) {
        Assert-PlainDirectory $current
        if ($current.Equals($root, [StringComparison]::OrdinalIgnoreCase)) { return }
        $current = [IO.Path]::GetDirectoryName($current)
    }
    throw "Backup item has an unexpected parent: $path"
}

function Get-BackupItems([string]$root) {
    $result = New-Object 'System.Collections.Generic.List[System.IO.FileSystemInfo]'
    $pending = New-Object 'System.Collections.Generic.Stack[string]'
    $pending.Push($root)
    while ($pending.Count -gt 0) {
        $directory = $pending.Pop()
        Assert-PlainDirectory $directory
        foreach ($item in Get-ChildItem -LiteralPath $directory -Force -ErrorAction Stop) {
            Assert-WithinBackups $item.FullName $root
            if (($item.Attributes -band [IO.FileAttributes]::ReparsePoint) -ne 0) {
                throw "Backup contains a junction or symbolic link: $($item.FullName)"
            }
            $result.Add($item)
            if ($item.PSIsContainer) { $pending.Push($item.FullName) }
        }
    }
    return $result.ToArray()
}

function Assert-ReachBackupNames([System.IO.FileSystemInfo[]]$items, [string]$root) {
    foreach ($item in $items) {
        if (-not [IO.Path]::GetDirectoryName($item.FullName).Equals($root, [StringComparison]::OrdinalIgnoreCase)) { continue }
        $name = $item.Name
        if ($name -eq 'pending-upgrade.json') {
            throw 'An unfinished upgrade recovery record exists. Keep backups and complete recovery first.'
        }
        $snapshot = $name -match '^pre-inno-v1\.0\.0-\d{8}T\d{6}Z-[0-9a-f]{32}(\.original|\.manifest\.json)?$'
        $otherDirectory = $name -match '^(failed-payload|replaced-install|abandoned-stage)-[0-9a-f]{32}$'
        $record = $name -match '^(completed|recovered|unreadable-record)-[0-9a-f]{32}\.json$'
        if (-not ($snapshot -or $otherDirectory -or $record)) {
            throw "Unrecognized item in DeploymentBackups; leaving all backups untouched: $name"
        }
        if ($item.PSIsContainer -and -not ($otherDirectory -or ($snapshot -and $name -notmatch '\.json$'))) {
            throw "Unexpected backup directory: $name"
        }
        if (-not $item.PSIsContainer -and -not ($record -or ($snapshot -and $name -match '\.manifest\.json$'))) {
            throw "Unexpected backup file: $name"
        }
        if ($item.PSIsContainer -and -not (Test-Path -LiteralPath (Join-Path $item.FullName 'TheReachScreensaver.scr') -PathType Leaf)) {
            throw "Backup directory has no Reach screensaver file: $name"
        }
    }
}

$root = ''
try {
    $localData = if ($LocalDataRoot) { $LocalDataRoot } else { [Environment]::GetFolderPath('LocalApplicationData') }
    if ([string]::IsNullOrWhiteSpace($localData) -or $localData -notmatch '^[A-Za-z]:[\\/]' -or
        $localData -match '[*?"<>|]' -or $localData.Substring(2) -match ':') {
        throw 'LocalAppData is not an absolute local drive path.'
    }
    $localData = [IO.Path]::GetFullPath($localData)
    $dataRoot = [IO.Path]::GetFullPath((Join-Path $localData 'TheReachScreensaver'))
    $root = [IO.Path]::GetFullPath((Join-Path $dataRoot 'DeploymentBackups'))
    if (-not [IO.Path]::GetDirectoryName($root).Equals($dataRoot, [StringComparison]::OrdinalIgnoreCase)) {
        throw 'The backup path is not the expected Reach child directory.'
    }
    try { $rootItem = Get-Item -LiteralPath $root -Force -ErrorAction Stop }
    catch [System.Management.Automation.ItemNotFoundException] {
        Write-Report 'MISSING' $root '0.00' 'The backup folder does not exist.'
        exit 0
    }
    Assert-PlainPathToRoot $localData
    Assert-PlainDirectory $dataRoot
    if (-not $rootItem.PSIsContainer -or ($rootItem.Attributes -band [IO.FileAttributes]::ReparsePoint) -ne 0) {
        throw "Not a normal backup directory: $root"
    }
    $items = @(Get-BackupItems $root)
    Assert-ReachBackupNames $items $root
    if ($items.Count -eq 0) {
        Write-Report 'EMPTY' $root '0.00' 'The backup folder exists but is empty.'
        exit 0
    }
    $bytes = [long]0
    foreach ($item in $items) {
        if (-not $item.PSIsContainer) { $bytes += $item.Length }
    }
    $size = ([double]$bytes / 1MB).ToString('F2', [Globalization.CultureInfo]::InvariantCulture)
    if ($Action -eq 'Inspect') {
        Write-Report 'READY' $root $size 'Removing these backups prevents restoration of earlier installations.'
        exit 0
    }
    if (Test-Path -LiteralPath (Join-Path $localData 'Programs\TheReachScreensaver') -ErrorAction Stop) {
        throw 'Reach is still installed. Backups can be removed only after application uninstall.'
    }
    if (Test-Path -LiteralPath (Join-Path $localData 'Programs\TheReachScreensaver.pending-v1.0.0') -ErrorAction Stop) {
        throw 'A staged Reach installation exists. Keep backups until the installation is resolved.'
    }
    $failures = New-Object 'System.Collections.Generic.List[string]'
    foreach ($item in @($items | Where-Object { -not $_.PSIsContainer })) {
        try {
            Assert-WithinBackups $item.FullName $root
            Assert-PlainAncestors $item.FullName $root
            $current = Get-Item -LiteralPath $item.FullName -Force -ErrorAction Stop
            if ($current.PSIsContainer -or ($current.Attributes -band [IO.FileAttributes]::ReparsePoint) -ne 0) {
                throw 'Item changed type or became a reparse point.'
            }
            Remove-Item -LiteralPath $item.FullName -Force -ErrorAction Stop
        }
        catch { $failures.Add("$($item.FullName): $($_.Exception.Message)") }
    }
    foreach ($item in @($items | Where-Object { $_.PSIsContainer } | Sort-Object { $_.FullName.Length } -Descending)) {
        try {
            Assert-WithinBackups $item.FullName $root
            Assert-PlainAncestors $item.FullName $root
            Assert-PlainDirectory $item.FullName
            Remove-Item -LiteralPath $item.FullName -Force -ErrorAction Stop
        }
        catch { $failures.Add("$($item.FullName): $($_.Exception.Message)") }
    }
    if ($failures.Count -gt 0) {
        Write-Report 'PARTIAL' $root $size ("Some backup files could not be removed. Details: " + ($failures -join ' | '))
        exit 3
    }
    Write-Report 'REMOVED' $root $size 'Retained Reach upgrade backups were removed.'
    exit 0
}
catch {
    Write-Report 'UNSAFE' $root '0.00' $_.Exception.Message
    exit 2
}
