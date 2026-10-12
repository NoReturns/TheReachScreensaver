param([switch]$Backup, [string]$LocalDataRoot)

$ErrorActionPreference = 'Stop'

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

$localData = if ($LocalDataRoot) { [IO.Path]::GetFullPath($LocalDataRoot) }
    else { [Environment]::GetFolderPath('LocalApplicationData') }
$programs = Join-Path $localData 'Programs'
$target = Join-Path $programs 'TheReachScreensaver'
$stage = Join-Path $programs 'TheReachScreensaver.pending-v1.0.0'
$dataRoot = Join-Path $localData 'TheReachScreensaver'
$running = @(Get-Process -Name 'TheReachScreensaver' -ErrorAction SilentlyContinue)
if ($running.Count -gt 0) {
    throw 'Close all Reach playback, preview, and Settings processes before continuing.'
}
if (Test-Path -LiteralPath $stage) {
    throw "An unfinished Reach staging directory exists: $stage. Recover it before retrying."
}
if ($Backup -and (Test-Path -LiteralPath (Join-Path $localData 'TheReachScreensaver\DeploymentBackups\pending-upgrade.json'))) {
    throw 'An unfinished Reach upgrade record exists. Recover it before retrying.'
}
if (-not (Test-Path -LiteralPath $target)) { return }
if (-not (Test-Path -LiteralPath $target -PathType Container)) {
    throw 'The existing Reach install path is not a directory.'
}
foreach ($path in @($programs, $target)) {
    if ((Get-Item -LiteralPath $path -Force).Attributes -band [IO.FileAttributes]::ReparsePoint) {
        throw "Refusing a reparse point in the install path: $path"
    }
}
$items = @(Get-ChildItem -LiteralPath $target -Recurse -Force)
if (@($items | Where-Object { $_.Attributes -band [IO.FileAttributes]::ReparsePoint }).Count -gt 0) {
    throw 'The existing install contains a reparse point. Review it before upgrading.'
}
if (-not $Backup) { return }

if (Test-Path -LiteralPath $dataRoot) {
    if ((Get-Item -LiteralPath $dataRoot -Force).Attributes -band [IO.FileAttributes]::ReparsePoint) {
        throw 'The Reach user-data root is a reparse point.'
    }
}
$backupRoot = Join-Path $dataRoot 'DeploymentBackups'
if (Test-Path -LiteralPath $backupRoot) {
    if ((Get-Item -LiteralPath $backupRoot -Force).Attributes -band [IO.FileAttributes]::ReparsePoint) {
        throw 'The backup directory is a reparse point.'
    }
}
else { New-Item -ItemType Directory -Path $backupRoot | Out-Null }
$backupPath = Join-Path $backupRoot ('pre-inno-v1.0.0-' + [DateTime]::UtcNow.ToString('yyyyMMddTHHmmssZ') + '-' + [Guid]::NewGuid().ToString('N'))
$manifestPath = $backupPath + '.manifest.json'
$recordPath = Join-Path $backupRoot 'pending-upgrade.json'
$movedPath = $backupPath + '.original'
New-Item -ItemType Directory -Path $backupPath | Out-Null
foreach ($item in $items | Where-Object { $_.PSIsContainer }) {
    $relative = Get-ReachRelativePath $target $item.FullName
    New-Item -ItemType Directory -Path (Join-Path $backupPath $relative) -Force | Out-Null
}
$files = @($items | Where-Object { -not $_.PSIsContainer })
$hashes = @()
foreach ($file in $files) {
    $relative = Get-ReachRelativePath $target $file.FullName
    $copy = Join-Path $backupPath $relative
    New-Item -ItemType Directory -Path ([IO.Path]::GetDirectoryName($copy)) -Force | Out-Null
    Copy-Item -LiteralPath $file.FullName -Destination $copy
    $sourceHash = (Get-FileHash -LiteralPath $file.FullName -Algorithm SHA256).Hash
    if ($sourceHash -cne (Get-FileHash -LiteralPath $copy -Algorithm SHA256).Hash) {
        throw "Backup verification failed: $relative"
    }
    $hashes += [pscustomobject]@{ Path = $relative; SHA256 = $sourceHash }
}
if (@(Get-ChildItem -LiteralPath $backupPath -Recurse -File -Force).Count -ne $files.Count) {
    throw 'Backup file count verification failed.'
}
$manifest = [pscustomobject]@{ Schema = 1; Files = $hashes }
$manifest | ConvertTo-Json -Depth 5 | Set-Content -LiteralPath $manifestPath -Encoding UTF8
$record = [pscustomobject]@{
    Schema = 1; Target = $target; Stage = $stage; Backup = $backupPath
    Manifest = $manifestPath; MovedOriginal = $movedPath; State = 'prepared'
}
$recordTemp = $recordPath + '.' + [Guid]::NewGuid().ToString('N') + '.tmp'
[IO.File]::WriteAllText($recordTemp, ($record | ConvertTo-Json -Depth 5),
    (New-Object System.Text.UTF8Encoding($false)))
[IO.File]::Move($recordTemp, $recordPath)
Write-Output "Verified prior install backup: $backupPath"
Write-Output "Recovery record: $recordPath"
