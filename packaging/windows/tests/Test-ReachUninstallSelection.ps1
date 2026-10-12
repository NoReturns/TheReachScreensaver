$ErrorActionPreference = 'Stop'
$windowsScripts = Split-Path $PSScriptRoot -Parent
$check = Join-Path $windowsScripts 'Check-ReachUninstallSelection.ps1'
$common = Join-Path $windowsScripts 'ReachInstall.Common.ps1'
$x64 = Join-Path $env:SystemRoot 'System32\WindowsPowerShell\v1.0\powershell.exe'
$x86 = Join-Path $env:SystemRoot 'SysWOW64\WindowsPowerShell\v1.0\powershell.exe'
$tempRoot = Join-Path $env:TEMP ('ReachSelectionTest-' + [Guid]::NewGuid().ToString('N'))
New-Item -ItemType Directory -Path $tempRoot | Out-Null
try {
    $installed = Join-Path $tempRoot 'The Reach Screensaver.scr'
    $other = Join-Path $tempRoot 'Other Screensaver.scr'
    [IO.File]::WriteAllText($installed, 'Reach test file')
    [IO.File]::WriteAllText($other, 'Other test file')
    $shortType = Add-Type -MemberDefinition '[DllImport("kernel32.dll", CharSet=CharSet.Unicode, SetLastError=true)] public static extern uint GetShortPathName(string longPath, System.Text.StringBuilder shortPath, uint bufferLength);' -Name ShortPath -Namespace ReachSelectionTest -PassThru
    $shortBuffer = New-Object System.Text.StringBuilder 32768
    $length = $shortType::GetShortPathName($installed, $shortBuffer, [uint32]$shortBuffer.Capacity)
    if ($length -eq 0 -or $length -ge $shortBuffer.Capacity -or
        $shortBuffer.ToString() -ieq $installed) { throw '8.3 paths are unavailable for the short-path regression.' }
    $short = $shortBuffer.ToString()
    $alias = Join-Path $tempRoot 'ReachAlias.scr'
    New-Item -ItemType HardLink -Path $alias -Target $installed | Out-Null
    $cases = @(
        @{ Name = 'other selected'; Path = $other; Exit = 0 },
        @{ Name = 'Ribbons selected'; Path = (Join-Path $env:SystemRoot 'System32\Ribbons.scr'); Exit = 0 },
        @{ Name = 'Bubbles selected'; Path = (Join-Path $env:SystemRoot 'System32\Bubbles.scr'); Exit = 0 },
        @{ Name = 'Reach selected'; Path = $installed; Exit = 1 },
        @{ Name = '8.3 Reach selected'; Path = $short; Exit = 1 },
        @{ Name = 'hard-link Reach selected'; Path = $alias; Exit = 1 },
        @{ Name = 'blank selection'; Path = ' '; Exit = 0 },
        @{ Name = 'missing selection'; Path = (Join-Path $tempRoot 'Missing.scr'); Exit = 2 },
        @{ Name = 'malformed selection'; Path = '"C:\broken.scr'; Exit = 2 }
    )
    foreach ($shell in @($x64, $x86)) {
        if (-not (Test-Path -LiteralPath $shell -PathType Leaf)) { throw "Missing PowerShell: $shell" }
        foreach ($case in $cases) {
            $ErrorActionPreference = 'Continue'
            $output = & $shell -NoProfile -ExecutionPolicy Bypass -File $check -InstalledScr $installed -SelectedPath $case.Path 2>&1
            $ErrorActionPreference = 'Stop'
            if ($LASTEXITCODE -ne $case.Exit) {
                throw "$($case.Name) failed in $shell (exit $LASTEXITCODE): $output"
            }
        }
    }
    . $common
    if (-not (Test-ReachFileEqual $installed $short)) { throw 'Short path identity mismatch.' }
    if (-not (Test-ReachFileEqual $installed $alias)) { throw 'Hard-link path identity mismatch.' }
    Write-Output 'Uninstall selection checks passed in 64-bit and 32-bit Windows PowerShell.'
}
finally { Remove-Item -LiteralPath $tempRoot -Recurse -Force }
