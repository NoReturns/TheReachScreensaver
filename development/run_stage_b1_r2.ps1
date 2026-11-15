param(
    [ValidateSet('rectilinear', 'cylindrical')] [string] $Projection = 'cylindrical',
    [switch] $Baseline,
    [switch] $Async,
    [switch] $Stepped,
    [Parameter(Mandatory)] [string] $HostPublish,
    [Parameter(Mandatory)] [string] $FixtureBuild
)
$ErrorActionPreference = 'Stop'
if (($Baseline -and $Stepped) -or ($Async -and ($Baseline -or $Stepped))) {
    throw 'Select exactly one capture mode.'
}
$repo = Split-Path $PSScriptRoot -Parent
$universe = (Resolve-Path (Join-Path $repo '../Universe')).Path
$root = Join-Path $PSScriptRoot ('runs/run-' + [guid]::NewGuid().ToString('N'))
$hostDir = Join-Path $root 'host'
$distDir = Join-Path $root 'dist'
New-Item -ItemType Directory -Path $hostDir, $distDir | Out-Null
Copy-Item (Join-Path $HostPublish '*') $hostDir -Recurse
Copy-Item (Join-Path $universe 'dist/*') $distDir -Recurse
$fixtureDir = Join-Path $distDir 'development/cylindrical-stage-a1'
New-Item -ItemType Directory -Path $fixtureDir | Out-Null
Copy-Item (Join-Path $FixtureBuild 'development/cylindrical-stage-a1/index.html') $fixtureDir
Copy-Item (Join-Path $FixtureBuild 'assets/*') (Join-Path $distDir 'assets') -Force
Set-Content (Join-Path $distDir 'reach-developer-payload.txt') 'VITE_REACH_CYLINDRICAL_DEV=1;VITE_REACH_DEV_TEST=1' -NoNewline
$runArgs = @('--reach-dev-projection', $Projection, '--sandbox-root', $root,
    '--reach-dev-fixture', 'hole-26-left')
if ($Stepped) { $runArgs += @('--reach-dev-motion', 'yaw') }
else { $runArgs += @('--reach-dev-continuous',
    $(if ($Baseline) { 'baseline' } elseif ($Async) { 'async' } else { 'capture' })) }
if ($Projection -eq 'cylindrical') { $runArgs += @('--reach-dev-guard', '512') }
$process = Start-Process -FilePath (Join-Path $hostDir 'TheReachDeveloper.exe') -ArgumentList $runArgs -WorkingDirectory $root -PassThru -Wait -WindowStyle Hidden
Write-Output $root
if ($process.ExitCode -ne 0) { throw "B1 developer fixture failed with exit $($process.ExitCode): $root" }
