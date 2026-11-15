param(
    [Parameter(Mandatory = $true)][ValidateSet('A','B','C','D')][string]$Case
)
$ErrorActionPreference = 'Stop'
$spec = @{
    A = @{ Mode = 'rectilinear'; Fixture = 'galaxy-left-seam' }
    B = @{ Mode = 'cylindrical'; Fixture = 'galaxy-left-seam' }
    C = @{ Mode = 'rectilinear'; Fixture = 'hole-26-left' }
    D = @{ Mode = 'cylindrical'; Fixture = 'hole-26-left' }
}[$Case]
$id = [guid]::NewGuid().ToString('N')
$run = Join-Path $PSScriptRoot ('r14-runs\run-' + $id)
New-Item -ItemType Directory -Path $run | Out-Null
Copy-Item (Join-Path $PSScriptRoot 'r12-fixture-dist-v2') (Join-Path $run 'dist') -Recurse
Set-Content -LiteralPath (Join-Path $run 'dist\reach-developer-payload.txt') -Value 'VITE_REACH_CYLINDRICAL_DEV=1;VITE_REACH_DEV_TEST=1' -NoNewline
New-Item -ItemType Directory -Path (Join-Path $run 'host') | Out-Null
Copy-Item (Join-Path $PSScriptRoot 'r14-publish\*') (Join-Path $run 'host') -Recurse
$exe = Join-Path $run 'host\TheReachDeveloper.exe'
$args = @('--reach-dev-projection', $spec.Mode, '--sandbox-root', $run,
    '--reach-dev-fixture', $spec.Fixture, '--reach-dev-r14-seconds', '180')
$p = Start-Process -FilePath $exe -ArgumentList $args -PassThru -WindowStyle Normal
$counterPath = Join-Path $run 'counter-samples.jsonl'
$start = Get-Date
$samples = 0
while (-not $p.HasExited -and (Get-Date) -lt $start.AddMinutes(6)) {
    $sample = [ordered]@{ utc = (Get-Date).ToUniversalTime().ToString('o'); hostPid = $p.Id }
    try {
        $line = & nvidia-smi --query-gpu=memory.used,utilization.gpu,utilization.memory,temperature.gpu,power.draw --format=csv,noheader,nounits
        $sample.nvidia = $line.Trim()
    } catch { $sample.nvidiaError = $_.Exception.Message }
    try {
        $c = Get-Counter '\GPU Process Memory(*)\Dedicated Usage','\GPU Process Memory(*)\Shared Usage','\GPU Engine(*)\Utilization Percentage' -SampleInterval 1 -MaxSamples 1
        $sample.counters = @($c.CounterSamples | Where-Object { $_.CookedValue -gt 0 } |
            ForEach-Object { @{ path = $_.Path; value = $_.CookedValue } })
    } catch { $sample.counterError = $_.Exception.Message }
    $sample.processes = @(Get-Process TheReachDeveloper,msedgewebview2 -ErrorAction SilentlyContinue |
        Select-Object Id,ProcessName,CPU,WorkingSet64,PrivateMemorySize64)
    ($sample | ConvertTo-Json -Compress -Depth 5) | Add-Content -LiteralPath $counterPath
    $samples++
    Start-Sleep -Seconds 1
    $p.Refresh()
}
$p.Refresh()
@{ case = $Case; mode = $spec.Mode; fixture = $spec.Fixture; run = $run;
    hostPid = $p.Id; startUtc = $start.ToUniversalTime().ToString('o');
    endUtc = (Get-Date).ToUniversalTime().ToString('o'); samples = $samples;
    exited = $p.HasExited; exitCode = $(if ($p.HasExited) { $p.ExitCode } else { $null }) } |
    ConvertTo-Json -Depth 3 | Set-Content -LiteralPath (Join-Path $run 'run-manifest.json')
Write-Output $run
