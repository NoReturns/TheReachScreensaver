param(
    [Parameter(Mandatory = $true)][ValidateSet('control-2x','static-2x')][string]$Variant,
    [Parameter(Mandatory = $true)][ValidateSet('join','interior')][string]$Region
)
$ErrorActionPreference = 'Stop'
$id = [guid]::NewGuid().ToString('N')
$run = Join-Path $PSScriptRoot ('r21-runs\run-' + $id)
New-Item -ItemType Directory -Path $run | Out-Null
Copy-Item (Join-Path $PSScriptRoot ('r21-variants\' + $Variant + '-' + $Region)) (Join-Path $run 'dist') -Recurse
New-Item -ItemType Directory -Path (Join-Path $run 'host') | Out-Null
Copy-Item (Join-Path $PSScriptRoot 'r17-publish\*') (Join-Path $run 'host') -Recurse
$exe = Join-Path $run 'host\TheReachDeveloper.exe'
$arguments = @('--reach-dev-projection','cylindrical','--sandbox-root',$run,
    '--reach-dev-fixture','hole-26-left','--reach-dev-guard','512',
    '--reach-dev-continuous','async')
$start = Get-Date
$p = Start-Process -FilePath $exe -ArgumentList $arguments -PassThru -WindowStyle Normal
if (-not $p.WaitForExit(240000)) { throw "R21 run timed out: $run" }
$p.Refresh()
@{ variant = $Variant; region = $Region; run = $run;
    startUtc = $start.ToUniversalTime().ToString('o');
    endUtc = (Get-Date).ToUniversalTime().ToString('o'); hostPid = $p.Id;
    exitCode = $p.ExitCode } | ConvertTo-Json |
    Set-Content -LiteralPath (Join-Path $run 'r21-manifest.json')
if ($p.ExitCode -ne 0) { throw "R21 run failed: $run" }
Write-Output "$Variant $Region $run"
