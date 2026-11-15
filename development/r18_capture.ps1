$ErrorActionPreference = 'Stop'
$cases = @(
    @{ Name = 'hole-yaw0'; Fixture = 'hole-26-left' },
    @{ Name = 'peripheral'; Fixture = 'galaxy-sector' },
    @{ Name = 'deep-space'; Fixture = 'galaxy-left-seam' }
)
foreach ($case in $cases) {
    $id = [guid]::NewGuid().ToString('N')
    $run = Join-Path $PSScriptRoot ('r18-runs\run-' + $id)
    New-Item -ItemType Directory -Path $run | Out-Null
    Copy-Item (Join-Path $PSScriptRoot 'r18-variants\primary-height-full-source') (Join-Path $run 'dist') -Recurse
    New-Item -ItemType Directory -Path (Join-Path $run 'host') | Out-Null
    Copy-Item (Join-Path $PSScriptRoot 'r17-publish\*') (Join-Path $run 'host') -Recurse
    $exe = Join-Path $run 'host\TheReachDeveloper.exe'
    $arguments = @('--reach-dev-projection','cylindrical','--sandbox-root',$run,
        '--reach-dev-fixture',$case.Fixture,'--reach-dev-guard','512')
    $start = Get-Date
    $p = Start-Process -FilePath $exe -ArgumentList $arguments -WindowStyle Normal -PassThru
    if (-not $p.WaitForExit(120000)) { throw "R18 fixture timed out: $run" }
    @{ variant = 'primary-height-full-source'; case = $case.Name; fixture = $case.Fixture;
        run = $run; startUtc = $start.ToUniversalTime().ToString('o');
        endUtc = (Get-Date).ToUniversalTime().ToString('o'); exitCode = $p.ExitCode } |
        ConvertTo-Json | Set-Content -LiteralPath (Join-Path $run 'r18-manifest.json')
    if ($p.ExitCode -ne 0 -or -not (Get-ChildItem (Join-Path $run 'metrics') -Filter 'fixture-*.json')) {
        throw "R18 fixture did not complete: $run"
    }
    Write-Output "$($case.Name) $run"
}
