param(
    [Parameter(Mandatory = $true)][ValidateSet('control-2x','static-2x')][string]$Variant,
    [Parameter(Mandatory = $true)][ValidateSet(-12,-6,0)][int]$Yaw
)
$ErrorActionPreference = 'Stop'
$id = [guid]::NewGuid().ToString('N')
$run = Join-Path $PSScriptRoot ('r22-runs\run-' + $id)
New-Item -ItemType Directory -Path $run | Out-Null
Copy-Item (Join-Path $PSScriptRoot ('r22-variants\' + $Variant)) (Join-Path $run 'dist') -Recurse
New-Item -ItemType Directory -Path (Join-Path $run 'host') | Out-Null
Copy-Item (Join-Path $PSScriptRoot 'r17-publish\*') (Join-Path $run 'host') -Recurse
$exe = Join-Path $run 'host\TheReachDeveloper.exe'
$arguments = @('--reach-dev-projection','cylindrical','--sandbox-root',$run,
    '--reach-dev-fixture','hole-26-left','--reach-dev-guard','512',
    '--reach-dev-yaw',$Yaw.ToString([cultureinfo]::InvariantCulture),
    '--reach-dev-forensics','1')
$p = Start-Process -FilePath $exe -ArgumentList $arguments -PassThru -WindowStyle Normal
if (-not $p.WaitForExit(240000)) { throw "R22 run timed out: $run" }
$p.Refresh()
@{ variant = $Variant; yaw = $Yaw; run = $run; exitCode = $p.ExitCode } |
    ConvertTo-Json | Set-Content -LiteralPath (Join-Path $run 'r22-manifest.json')
if ($p.ExitCode -ne 0) { throw "R22 run failed: $run" }
Write-Output "$Variant yaw $Yaw $run"
