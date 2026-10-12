param(
    [Parameter(Mandatory = $true)][string]$InstalledScr,
    [string]$SelectedPath,
    [string]$DiagnosticFile
)

$ErrorActionPreference = 'Stop'
. (Join-Path $PSScriptRoot 'ReachInstall.Common.ps1')

function Write-ReachDiagnostic([string]$message) {
    [Console]::Error.WriteLine($message)
    if (-not [string]::IsNullOrWhiteSpace($DiagnosticFile)) {
        [IO.File]::WriteAllText($DiagnosticFile, $message)
    }
}

try {
    if (-not $PSBoundParameters.ContainsKey('SelectedPath')) {
        $key = [Microsoft.Win32.Registry]::CurrentUser.OpenSubKey('Control Panel\Desktop', $false)
        if ($null -eq $key) { throw 'Windows screensaver settings are unavailable.' }
        try { $SelectedPath = [string]$key.GetValue('SCRNSAVE.EXE', '') }
        finally { $key.Dispose() }
    }
    if ([string]::IsNullOrWhiteSpace($SelectedPath)) { exit 0 }
    if (Test-ReachFileEqual $InstalledScr $SelectedPath) {
        Write-ReachDiagnostic 'Reach is selected as the Windows screensaver. Choose another screensaver before uninstalling.'
        exit 1
    }
    exit 0
}
catch {
    Write-ReachDiagnostic "Could not safely verify the selected screensaver: $($_.Exception.Message)"
    exit 2
}
