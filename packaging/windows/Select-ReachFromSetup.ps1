param([switch]$Shortcut)

$ErrorActionPreference = 'Stop'
try {
    & (Join-Path $PSScriptRoot 'Select-ReachScreensaver.ps1') -Apply -Reselect | Out-Null
    if ($Shortcut) {
        Add-Type -AssemblyName System.Windows.Forms
        [void][System.Windows.Forms.MessageBox]::Show('The Reach is now selected as your Windows screensaver. Your existing idle timeout, active state, and resume security settings were preserved.',
            'The Reach Screensaver', [System.Windows.Forms.MessageBoxButtons]::OK, [System.Windows.Forms.MessageBoxIcon]::Information)
    }
}
catch {
    $message = "Windows could not select The Reach. $($_.Exception.Message)"
    Add-Type -AssemblyName System.Windows.Forms
    [void][System.Windows.Forms.MessageBox]::Show($message, 'The Reach Screensaver',
        [System.Windows.Forms.MessageBoxButtons]::OK, [System.Windows.Forms.MessageBoxIcon]::Error)
    exit 1
}
