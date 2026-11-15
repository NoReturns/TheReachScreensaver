param(
    [Parameter(Mandatory = $true)][ValidateSet('unbounded','cap2433')][string]$Variant,
    [Parameter(Mandatory = $true)][ValidateSet('planet','planetjoin','planetjoinaligned','stars','hole')][string]$Scenario
)
$ErrorActionPreference = 'Stop'
$fixture = @{ planet = 'galaxy-sector'; planetjoin = 'galaxy-sector'; planetjoinaligned = 'galaxy-sector'; stars = 'galaxy-left-seam'; hole = 'hole-26-left' }[$Scenario]
$id = [guid]::NewGuid().ToString('N')
$run = Join-Path $PSScriptRoot ('r17-runs\run-' + $id)
New-Item -ItemType Directory -Path $run | Out-Null
$source = if ($Scenario -eq 'planetjoinaligned') { 'r17-join-aligned-variants' }
    elseif ($Scenario -eq 'planetjoin') { 'r17-join-variants' } else { 'r17-variants' }
Copy-Item (Join-Path $PSScriptRoot ($source + '\' + $Variant)) (Join-Path $run 'dist') -Recurse
New-Item -ItemType Directory -Path (Join-Path $run 'host') | Out-Null
Copy-Item (Join-Path $PSScriptRoot 'r17-publish\*') (Join-Path $run 'host') -Recurse
$exe = Join-Path $run 'host\TheReachDeveloper.exe'
$arguments = @('--reach-dev-projection','cylindrical','--sandbox-root',$run,
    '--reach-dev-fixture',$fixture,'--reach-dev-guard','512',
    '--reach-dev-continuous','r17')
$start = Get-Date
$p = Start-Process -FilePath $exe -ArgumentList $arguments -PassThru -WindowStyle Normal
$counterPath = Join-Path $run 'counter-samples.jsonl'
while (-not $p.HasExited -and (Get-Date) -lt $start.AddMinutes(8)) {
    $sample = [ordered]@{ utc = (Get-Date).ToUniversalTime().ToString('o'); hostPid = $p.Id }
    try { $sample.nvidia = (& nvidia-smi --query-gpu=memory.used,utilization.gpu,utilization.memory --format=csv,noheader,nounits).Trim() }
    catch { $sample.nvidiaError = $_.Exception.Message }
    try {
        $c = Get-Counter '\GPU Process Memory(*)\Dedicated Usage','\GPU Engine(*)\Utilization Percentage' -SampleInterval 1 -MaxSamples 1
        $sample.counters = @($c.CounterSamples | Where-Object { $_.CookedValue -gt 0 } |
            ForEach-Object { @{ path = $_.Path; value = $_.CookedValue } })
    } catch { $sample.counterError = $_.Exception.Message }
    ($sample | ConvertTo-Json -Compress -Depth 5) | Add-Content -LiteralPath $counterPath
    Start-Sleep -Seconds 2
    $p.Refresh()
}
$p.Refresh()
@{ variant = $Variant; scenario = $Scenario; fixture = $fixture; run = $run;
    hostPid = $p.Id; startUtc = $start.ToUniversalTime().ToString('o');
    endUtc = (Get-Date).ToUniversalTime().ToString('o'); exited = $p.HasExited;
    exitCode = $(if ($p.HasExited) { $p.ExitCode } else { $null }) } |
    ConvertTo-Json | Set-Content -LiteralPath (Join-Path $run 'r17-manifest.json')
Write-Output "$Variant $Scenario $run exit=$($p.ExitCode)"
