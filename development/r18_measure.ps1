$ErrorActionPreference = 'Stop'
$id = [guid]::NewGuid().ToString('N')
$run = Join-Path $PSScriptRoot ('r18-runs\run-' + $id)
New-Item -ItemType Directory -Path $run | Out-Null
Copy-Item (Join-Path $PSScriptRoot 'r18-variants\primary-height-full-source') (Join-Path $run 'dist') -Recurse
New-Item -ItemType Directory -Path (Join-Path $run 'host') | Out-Null
Copy-Item (Join-Path $PSScriptRoot 'r17-publish\*') (Join-Path $run 'host') -Recurse
$exe = Join-Path $run 'host\TheReachDeveloper.exe'
$arguments = @('--reach-dev-projection','cylindrical','--sandbox-root',$run,
    '--reach-dev-fixture','hole-26-left','--reach-dev-guard','512',
    '--reach-dev-r14-seconds','30')
$start = Get-Date
$p = Start-Process -FilePath $exe -ArgumentList $arguments -PassThru -WindowStyle Normal
$counterPath = Join-Path $run 'counter-samples.jsonl'
while (-not $p.HasExited -and (Get-Date) -lt $start.AddMinutes(3)) {
    $sample = [ordered]@{ utc = (Get-Date).ToUniversalTime().ToString('o'); hostPid = $p.Id }
    try { $sample.nvidia = (& nvidia-smi --query-gpu=memory.used,utilization.gpu,utilization.memory --format=csv,noheader,nounits).Trim() }
    catch { $sample.nvidiaError = $_.Exception.Message }
    try {
        $c = Get-Counter '\GPU Process Memory(*)\Dedicated Usage','\GPU Engine(*)\Utilization Percentage' -SampleInterval 1 -MaxSamples 1
        $sample.counters = @($c.CounterSamples | Where-Object { $_.CookedValue -gt 0 } |
            ForEach-Object { @{ path = $_.Path; value = $_.CookedValue } })
    } catch { $sample.counterError = $_.Exception.Message }
    ($sample | ConvertTo-Json -Compress -Depth 5) | Add-Content -LiteralPath $counterPath
    Start-Sleep -Seconds 1
    $p.Refresh()
}
$p.Refresh()
@{ variant = 'primary-height-full-source'; case = 'moving-hole'; run = $run;
    hostPid = $p.Id; startUtc = $start.ToUniversalTime().ToString('o');
    endUtc = (Get-Date).ToUniversalTime().ToString('o'); exited = $p.HasExited;
    exitCode = $(if ($p.HasExited) { $p.ExitCode } else { $null }) } |
    ConvertTo-Json | Set-Content -LiteralPath (Join-Path $run 'r18-manifest.json')
if (-not (Get-ChildItem (Join-Path $run 'metrics') -Filter 'r14-*.json')) {
    throw "R18 measurement did not complete: $run"
}
Write-Output "primary-height-full-source $run"
