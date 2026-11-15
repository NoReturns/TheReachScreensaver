param()

$ErrorActionPreference = 'Stop'
$repo = Split-Path $PSScriptRoot -Parent
$universe = (Resolve-Path (Join-Path $repo '../Universe')).Path
$root = Join-Path $PSScriptRoot ('runs/run-' + [guid]::NewGuid().ToString('N'))
$hostDir = Join-Path $root 'host'
$distDir = Join-Path $root 'dist'
New-Item -ItemType Directory -Path $hostDir, $distDir | Out-Null
Copy-Item (Join-Path $PSScriptRoot 'r9-publish/*') $hostDir -Recurse
Copy-Item (Join-Path $universe 'dist/*') $distDir -Recurse
$fixtureDir = Join-Path $distDir 'development/cylindrical-stage-a1'
New-Item -ItemType Directory -Path $fixtureDir | Out-Null
Copy-Item (Join-Path $PSScriptRoot 'r9-fixture-dist/development/cylindrical-stage-a1/index.html') $fixtureDir
Copy-Item (Join-Path $PSScriptRoot 'r9-fixture-dist/assets/*') (Join-Path $distDir 'assets') -Force
Set-Content (Join-Path $distDir 'reach-developer-payload.txt') 'VITE_REACH_CYLINDRICAL_DEV=1;VITE_REACH_DEV_TEST=1' -NoNewline

$runArgs = @('--reach-dev-projection','cylindrical','--sandbox-root',$root,
    '--reach-dev-fixture','hole-26-left','--reach-dev-guard','512',
    '--reach-dev-yaw','6.815','--reach-dev-forensics','1',
    '--reach-dev-sector-parity','1')
$process = Start-Process -FilePath (Join-Path $hostDir 'TheReachDeveloper.exe') -ArgumentList $runArgs -WorkingDirectory $root -PassThru -Wait -WindowStyle Hidden
Write-Output $root
if ($process.ExitCode -ne 0) { throw "R9 developer fixture failed: $root" }
