param([Parameter(Mandatory = $true)][ValidateSet('4x','2x','1x')][string]$Variant)
$ErrorActionPreference = 'Stop'
$root = Join-Path $PSScriptRoot 'r15-runs'
New-Item -ItemType Directory -Path $root -Force | Out-Null
$cases = @(
    @{ Name = 'hole-yaw0'; Fixture = 'hole-26-left'; Yaw = '0' },
    @{ Name = 'hole-yaw-6'; Fixture = 'hole-26-left'; Yaw = '-6' },
    @{ Name = 'hole-yaw-plus'; Fixture = 'hole-26-left'; Yaw = '6.815' },
    @{ Name = 'peripheral'; Fixture = 'galaxy-sector'; Yaw = $null },
    @{ Name = 'deep-space'; Fixture = 'galaxy-left-seam'; Yaw = $null }
)
foreach ($case in $cases) {
    $id = [guid]::NewGuid().ToString('N')
    $run = Join-Path $root ('run-' + $id)
    New-Item -ItemType Directory -Path $run | Out-Null
    Copy-Item (Join-Path $PSScriptRoot ('r15-variants\' + $Variant)) (Join-Path $run 'dist') -Recurse
    New-Item -ItemType Directory -Path (Join-Path $run 'host') | Out-Null
    Copy-Item (Join-Path $PSScriptRoot 'r14-publish\*') (Join-Path $run 'host') -Recurse
    $exe = Join-Path $run 'host\TheReachDeveloper.exe'
    $arguments = @('--reach-dev-projection','cylindrical','--sandbox-root',$run,
        '--reach-dev-fixture',$case.Fixture,'--reach-dev-guard','512',
        '--reach-dev-r13-measure','1')
    if ($null -ne $case.Yaw) { $arguments += @('--reach-dev-yaw',$case.Yaw) }
    $start = Get-Date
    $p = Start-Process -FilePath $exe -ArgumentList $arguments -WindowStyle Normal -PassThru
    if (-not $p.WaitForExit(120000)) { throw "Fixture timed out: $run" }
    @{ variant = $Variant; case = $case.Name; fixture = $case.Fixture;
        yaw = $case.Yaw; run = $run; startUtc = $start.ToUniversalTime().ToString('o');
        endUtc = (Get-Date).ToUniversalTime().ToString('o'); exitCode = $p.ExitCode } |
        ConvertTo-Json | Set-Content -LiteralPath (Join-Path $run 'r15-manifest.json')
    if ($p.ExitCode -ne 0 -or -not (Get-ChildItem (Join-Path $run 'metrics') -Filter 'r13-*.json')) {
        throw "Fixture did not complete: $run"
    }
    Write-Output "$Variant $($case.Name) $run"
}
