param(
    [Parameter(Mandatory = $true)][ValidateSet('control-2x','control-4x','static-2x')][string]$Variant,
    [Parameter(Mandatory = $true)][ValidateSet('capture','trace','lens','motion')][string]$Mode,
    [ValidateSet('hole','stars','planet')][string]$Scene = 'hole',
    [double]$Yaw = 6.815
)
$ErrorActionPreference = 'Stop'
$fixture = @{ hole = 'hole-26-left'; stars = 'galaxy-left-seam'; planet = 'galaxy-sector' }[$Scene]
$id = [guid]::NewGuid().ToString('N')
$run = Join-Path $PSScriptRoot ('r20-runs\run-' + $id)
New-Item -ItemType Directory -Path $run | Out-Null
$source = if ($Mode -eq 'motion') { $Variant + '-motion' } else { $Variant }
Copy-Item (Join-Path $PSScriptRoot ('r20-variants\' + $source)) (Join-Path $run 'dist') -Recurse
New-Item -ItemType Directory -Path (Join-Path $run 'host') | Out-Null
Copy-Item (Join-Path $PSScriptRoot 'r17-publish\*') (Join-Path $run 'host') -Recurse
$exe = Join-Path $run 'host\TheReachDeveloper.exe'
$arguments = @('--reach-dev-projection','cylindrical','--sandbox-root',$run,
    '--reach-dev-fixture',$fixture,'--reach-dev-guard','512')
if ($Mode -eq 'capture') {
    if ($Scene -eq 'hole') { $arguments += @('--reach-dev-yaw', $Yaw.ToString('0.###',[cultureinfo]::InvariantCulture)) }
    $arguments += @('--reach-dev-r13-measure','1')
} elseif ($Mode -eq 'trace' -or $Mode -eq 'lens') {
    if ($Scene -ne 'hole') { throw 'Trace and lens modes require the hole fixture.' }
    $arguments += @('--reach-dev-yaw','6.815','--reach-dev-forensics','1',
        '--reach-dev-sector-parity','1')
    if ($Mode -eq 'trace') { $arguments += @('--reach-dev-sector-motion','1') }
} else {
    $arguments += @('--reach-dev-continuous','r17')
}
$start = Get-Date
$p = Start-Process -FilePath $exe -ArgumentList $arguments -PassThru -WindowStyle Normal
$counterPath = Join-Path $run 'counter-samples.jsonl'
if ($Mode -eq 'motion') {
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
        Start-Sleep -Seconds 1
        $p.Refresh()
    }
} else {
    if (-not $p.WaitForExit(240000)) { throw "R20 run timed out: $run" }
}
$p.Refresh()
@{ variant = $Variant; mode = $Mode; scene = $Scene; yaw = $Yaw; run = $run;
    startUtc = $start.ToUniversalTime().ToString('o');
    endUtc = (Get-Date).ToUniversalTime().ToString('o'); hostPid = $p.Id;
    exited = $p.HasExited; exitCode = $(if ($p.HasExited) { $p.ExitCode } else { $null }) } |
    ConvertTo-Json | Set-Content -LiteralPath (Join-Path $run 'r20-manifest.json')
if (-not $p.HasExited -or $p.ExitCode -ne 0) { throw "R20 run failed: $run" }
Write-Output "$Variant $Mode $Scene $run"
