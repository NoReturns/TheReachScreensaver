param([Parameter(Mandatory = $true)][ValidateSet('4x','2x','1x')][string]$Variant)
$ErrorActionPreference = 'Stop'
$id = [guid]::NewGuid().ToString('N')
$run = Join-Path $PSScriptRoot ('r15-runs\run-' + $id)
New-Item -ItemType Directory -Path $run | Out-Null
Copy-Item (Join-Path $PSScriptRoot ('r15-variants\' + $Variant)) (Join-Path $run 'dist') -Recurse
New-Item -ItemType Directory -Path (Join-Path $run 'host') | Out-Null
Copy-Item (Join-Path $PSScriptRoot 'r14-publish\*') (Join-Path $run 'host') -Recurse
$exe = Join-Path $run 'host\TheReachDeveloper.exe'
$arguments = @('--reach-dev-projection','cylindrical','--sandbox-root',$run,
    '--reach-dev-fixture','hole-26-left','--reach-dev-guard','512',
    '--reach-dev-continuous','async')
$start = Get-Date
$p = Start-Process -FilePath $exe -ArgumentList $arguments -PassThru -WindowStyle Normal
if (-not $p.WaitForExit(120000)) { throw "Motion fixture timed out: $run" }
@{ variant = $Variant; case = 'motion-crop'; run = $run;
    startUtc = $start.ToUniversalTime().ToString('o');
    endUtc = (Get-Date).ToUniversalTime().ToString('o'); exitCode = $p.ExitCode } |
    ConvertTo-Json | Set-Content -LiteralPath (Join-Path $run 'r15-manifest.json')
if ($p.ExitCode -ne 0 -or -not (Get-ChildItem (Join-Path $run 'metrics') -Filter 'b1-*.json')) {
    throw "Motion fixture did not complete: $run"
}
Write-Output "$Variant $run"
