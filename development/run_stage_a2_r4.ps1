param(
    [ValidateSet('rectilinear', 'cylindrical')] [string] $Projection = 'cylindrical',
    [ValidateSet('native', 'grid')] [string] $BloomMode = 'grid',
    [ValidateSet(128, 256, 512)] [int] $Guard = 512,
    [ValidateRange(-12, 12)] [int] $Yaw = 0,
    [string] $Fixture = 'hole-26-left',
    [switch] $Forensics,
    [switch] $Motion,
    [switch] $Fault
)

$ErrorActionPreference = 'Stop'
$repo = Split-Path $PSScriptRoot -Parent
$universe = (Resolve-Path (Join-Path $repo '../Universe')).Path
$root = Join-Path $PSScriptRoot ('runs/run-' + [guid]::NewGuid().ToString('N'))
$hostDir = Join-Path $root 'host'
$distDir = Join-Path $root 'dist'
New-Item -ItemType Directory -Path $hostDir, $distDir | Out-Null
Copy-Item (Join-Path $PSScriptRoot 'r4-publish/*') $hostDir -Recurse
Copy-Item (Join-Path $universe 'dist/*') $distDir -Recurse
$fixtureDir = Join-Path $distDir 'development/cylindrical-stage-a1'
New-Item -ItemType Directory -Path $fixtureDir | Out-Null
Copy-Item (Join-Path $PSScriptRoot 'r4-fixture-dist/index.html') $fixtureDir
Copy-Item (Join-Path $PSScriptRoot 'r4-fixture-dist/assets/*') (Join-Path $distDir 'assets') -Force
Set-Content (Join-Path $distDir 'reach-developer-payload.txt') 'VITE_REACH_CYLINDRICAL_DEV=1;VITE_REACH_DEV_TEST=1' -NoNewline

$runArgs = @('--reach-dev-projection', $Projection, '--sandbox-root', $root)
if ($Fixture) { $runArgs += @('--reach-dev-fixture', $Fixture) }
if ($Projection -eq 'cylindrical' -and $Fixture) {
    $runArgs += @('--reach-dev-guard', "$Guard")
    if ($BloomMode -eq 'grid') { $runArgs += @('--reach-dev-bloom-mips', 'grid') }
}
if ($Fixture -eq 'hole-26-left' -and !$Motion) { $runArgs += @('--reach-dev-yaw', "$Yaw") }
if ($Forensics) { $runArgs += @('--reach-dev-forensics', '1') }
if ($Motion) { $runArgs += @('--reach-dev-motion', 'yaw') }
if ($Fault) { $runArgs += @('--reach-dev-fault', 'primary-init') }
if (!$Fixture) { $runArgs += @('--reach-dev-smoke-seconds', '5') }
$process = Start-Process -FilePath (Join-Path $hostDir 'TheReachDeveloper.exe') -ArgumentList $runArgs -WorkingDirectory $root -PassThru -Wait -WindowStyle Hidden
if ($process.ExitCode -ne 0) { throw "Developer fixture failed: $root" }
if ($Fixture) {
    $manifest = Get-ChildItem (Join-Path $root 'metrics/fixture-*.json') | Select-Object -First 1
    if ($null -eq $manifest) { throw "Developer fixture has no capture manifest: $root" }
    python (Join-Path $PSScriptRoot 'analyze_native_fixtures.py') $manifest.FullName
    if ($LASTEXITCODE -ne 0) { throw "Fixture verification failed: $root" }
}
if ($Forensics) {
    $stages = Get-ChildItem (Join-Path $root 'metrics/forensics-*.json') | Select-Object -First 1
    python (Join-Path $PSScriptRoot 'analyze_native_forensics.py') $stages.FullName
    if ($LASTEXITCODE -ne 0) { throw "Stage verification failed: $root" }
}
if ($Motion) {
    $motionFile = Get-ChildItem (Join-Path $root 'metrics/motion-*.json') | Select-Object -First 1
    python (Join-Path $PSScriptRoot 'analyze_native_motion.py') $motionFile.FullName
    if ($LASTEXITCODE -ne 0) { throw "Motion verification failed: $root" }
}
Write-Output $root
