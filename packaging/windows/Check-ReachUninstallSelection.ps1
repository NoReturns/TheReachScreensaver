param(
    [Parameter(Mandatory = $true)][string]$InstalledScr,
    [string]$SelectedPath
)

$ErrorActionPreference = 'Stop'

function Get-ReachCanonicalPath([string]$path) {
    if ([string]::IsNullOrWhiteSpace($path)) { return $null }
    $value = [Environment]::ExpandEnvironmentVariables($path.Trim())
    if ($value.Length -ge 2 -and $value[0] -eq '"' -and $value[$value.Length - 1] -eq '"') {
        $value = $value.Substring(1, $value.Length - 2)
    }
    if ($value -notmatch '\A[A-Za-z]:[\\/]') { throw "Unsupported screensaver path: $path" }
    $full = [IO.Path]::GetFullPath($value.Replace('/', '\'))
    if (-not (Test-Path -LiteralPath $full -PathType Leaf)) {
        throw "Screensaver path does not name an existing file: $path"
    }
    $stream = [IO.File]::Open($full, [IO.FileMode]::Open, [IO.FileAccess]::Read,
        [IO.FileShare]::ReadWrite -bor [IO.FileShare]::Delete)
    try {
        if (-not ('ReachUninstall.Native' -as [type])) {
            Add-Type -TypeDefinition @'
using System;
using System.Text;
using System.Runtime.InteropServices;
using Microsoft.Win32.SafeHandles;
namespace ReachUninstall {
    public static class Native {
        [DllImport("kernel32.dll", CharSet = CharSet.Unicode, SetLastError = true)]
        public static extern uint GetFinalPathNameByHandle(
            SafeFileHandle handle, StringBuilder path, uint length, uint flags);
    }
}
'@
        }
        $buffer = New-Object System.Text.StringBuilder 32768
        $length = [ReachUninstall.Native]::GetFinalPathNameByHandle(
            $stream.SafeFileHandle, $buffer, [uint32]$buffer.Capacity, 0)
        if ($length -eq 0 -or $length -ge $buffer.Capacity) {
            throw 'Could not resolve the screensaver file identity.'
        }
        return $buffer.ToString()
    }
    finally { $stream.Dispose() }
}

try {
    if (-not $PSBoundParameters.ContainsKey('SelectedPath')) {
        $key = [Microsoft.Win32.Registry]::CurrentUser.OpenSubKey('Control Panel\Desktop', $false)
        if ($null -eq $key) { throw 'Windows screensaver settings are unavailable.' }
        try { $SelectedPath = [string]$key.GetValue('SCRNSAVE.EXE', '') }
        finally { $key.Dispose() }
    }
    if ([string]::IsNullOrWhiteSpace($SelectedPath)) { exit 0 }
    if (-not (Test-Path -LiteralPath $InstalledScr -PathType Leaf)) {
        throw 'Installed screensaver file is unavailable.'
    }
    $installed = Get-ReachCanonicalPath $InstalledScr
    $selected = Get-ReachCanonicalPath $SelectedPath
    if ([string]::Equals($installed, $selected, [StringComparison]::OrdinalIgnoreCase)) {
        [Console]::Error.WriteLine('Reach is selected as the Windows screensaver. Choose another screensaver before uninstalling.')
        exit 1
    }
    exit 0
}
catch {
    [Console]::Error.WriteLine("Could not safely verify the selected screensaver: $($_.Exception.Message)")
    exit 1
}
