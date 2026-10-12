Set-StrictMode -Version Latest
$ErrorActionPreference = 'Stop'

$script:ReachSettingsNames = @('SCRNSAVE.EXE', 'ScreenSaveActive', 'ScreenSaveTimeOut', 'ScreenSaverIsSecure')

function Get-ReachInstallPaths {
    $localData = [Environment]::GetFolderPath('LocalApplicationData')
    if ([string]::IsNullOrWhiteSpace($localData)) { throw 'LocalAppData is unavailable.' }
    $programs = [IO.Path]::GetFullPath((Join-Path $localData 'Programs'))
    $target = [IO.Path]::GetFullPath((Join-Path $programs 'TheReachScreensaver'))
    $backup = Join-Path $localData 'TheReachScreensaver\Installation\previous-screensaver.json'
    if ([IO.Path]::GetDirectoryName($target).TrimEnd('\') -ine $programs.TrimEnd('\')) {
        throw 'The install target is outside the per-user Programs folder.'
    }
    [pscustomobject]@{ Programs = $programs; Target = $target; Scr = Join-Path $target 'TheReachScreensaver.scr'; Backup = $backup }
}

function Assert-NoReparsePoints([string]$path) {
    $item = Get-Item -LiteralPath $path -Force
    if (($item.Attributes -band [IO.FileAttributes]::ReparsePoint) -ne 0) {
        throw "Reparse point is not allowed in package path: $path"
    }
    foreach ($child in Get-ChildItem -LiteralPath $path -Recurse -Force) {
        if (($child.Attributes -band [IO.FileAttributes]::ReparsePoint) -ne 0) {
            throw "Reparse point is not allowed in package: $($child.FullName)"
        }
    }
}

function Get-ReachPackageFiles([string]$path) {
    $root = [IO.Path]::GetFullPath((Resolve-Path -LiteralPath $path).Path)
    Assert-NoReparsePoints $root
    foreach ($required in @('TheReachScreensaver.scr', 'dist\index.html', 'ATTRIBUTION.md', 'INSTALL.md',
            'CREDITS.txt', 'LICENSE', 'PRIVACY.md',
            'licenses\Universe-LICENSE.txt', 'licenses\Universe-NOTICE.md',
            'licenses\Three-LICENSE.txt', 'licenses\OpenTK-LICENSE.txt',
            'licenses\WebView2-LICENSE.txt', 'licenses\WebView2-NOTICE.txt',
            'licenses\DotNet-ThirdPartyNotices.txt', 'windows\ReachInstall.Common.ps1',
            'windows\Install-ReachScreensaver.ps1', 'windows\Select-ReachScreensaver.ps1',
            'windows\Select-ReachFromSetup.ps1',
            'windows\Uninstall-ReachScreensaver.ps1', 'windows\Prepare-ReachUpgrade.ps1',
            'windows\Complete-ReachUpgrade.ps1', 'windows\Check-ReachUninstallSelection.ps1',
            'windows\Cleanup-ReachDeploymentBackups.ps1',
            'PAYLOAD-SHA256.json')) {
        if (-not (Test-Path -LiteralPath (Join-Path $root $required) -PathType Leaf)) {
            throw "Incomplete package: missing $required"
        }
    }
    foreach ($brandFile in @('logo.png', 'logo-white.png', 'favicon.png', 'apple-touch-icon.png', 'og-image.jpg')) {
        if (Test-Path -LiteralPath (Join-Path $root "dist\$brandFile") -PathType Leaf) {
            throw "Package retains unused upstream branding: $brandFile"
        }
    }
    $index = Get-Content -LiteralPath (Join-Path $root 'dist\index.html') -Raw
    if ($index -cmatch 'logo(?:-white)?\.png|favicon\.png|apple-touch-icon\.png|og-image\.jpg|<span class="brand-name">YOUR EYES IN SPACE|<title>Your Eyes In Space') {
        throw 'Package HTML retains upstream branding references.'
    }
    $files = @(Get-ChildItem -LiteralPath $root -File -Recurse -Force)
    if ($files.Count -lt 10) { throw 'The package contains too few files.' }
    [pscustomobject]@{ Root = $root; Files = $files }
}

function Get-ReachSelection {
    $key = [Microsoft.Win32.Registry]::CurrentUser.OpenSubKey('Control Panel\Desktop', $false)
    if ($null -eq $key) { throw 'Current-user desktop settings are unavailable.' }
    try {
        $values = foreach ($name in $script:ReachSettingsNames) {
            $exists = $key.GetValueNames() -contains $name
            [pscustomobject]@{
                Name = $name
                Exists = $exists
                Kind = if ($exists) { $key.GetValueKind($name).ToString() } else { $null }
                Value = if ($exists) { $key.GetValue($name) } else { $null }
            }
        }
        return @($values)
    }
    finally { $key.Dispose() }
}

function Get-ReachSelectedPath {
    $entry = @(Get-ReachSelection | Where-Object Name -eq 'SCRNSAVE.EXE')[0]
    if (-not $entry.Exists) { return $null }
    return [string]$entry.Value
}

function Set-ReachSelectedPath([string]$path) {
    $key = [Microsoft.Win32.Registry]::CurrentUser.OpenSubKey('Control Panel\Desktop', $true)
    if ($null -eq $key) { throw 'Current-user desktop settings are not writable.' }
    try { $key.SetValue('SCRNSAVE.EXE', $path, [Microsoft.Win32.RegistryValueKind]::String) }
    finally { $key.Dispose() }
}

function Test-ReachPathEqual([string]$left, [string]$right) {
    if ([string]::IsNullOrWhiteSpace($left) -or [string]::IsNullOrWhiteSpace($right)) { return $false }
    try { return [IO.Path]::GetFullPath($left) -ieq [IO.Path]::GetFullPath($right) }
    catch { return $false }
}

function Get-ReachFileIdentity([string]$path) {
    if ([string]::IsNullOrWhiteSpace($path)) { throw 'Screensaver path is empty.' }
    $value = $path.Trim()
    if ($value.StartsWith('"') -or $value.EndsWith('"')) {
        if ($value.Length -lt 2 -or -not ($value.StartsWith('"') -and $value.EndsWith('"'))) {
            throw "Malformed screensaver path: $path"
        }
        $value = $value.Substring(1, $value.Length - 2)
    }
    $value = [Environment]::ExpandEnvironmentVariables($value)
    if ($value -notmatch '\A[A-Za-z]:[\\/]' -or $value.Substring(2) -match '[:*?"<>|]') {
        throw "Unsupported screensaver path: $path"
    }
    $full = [IO.Path]::GetFullPath($value.Replace('/', '\'))
    # A 32-bit PowerShell process otherwise redirects System32 to SysWOW64.
    if (-not [Environment]::Is64BitProcess -and [Environment]::Is64BitOperatingSystem) {
        $windows = [Environment]::GetEnvironmentVariable('SystemRoot')
        $system32 = [IO.Path]::Combine($windows, 'System32')
        if ($full.StartsWith($system32 + '\', [StringComparison]::OrdinalIgnoreCase)) {
            $full = [IO.Path]::Combine($windows, 'Sysnative',
                $full.Substring($system32.Length + 1))
        }
    }
    $stream = [IO.File]::Open($full, [IO.FileMode]::Open, [IO.FileAccess]::Read,
        [IO.FileShare]::ReadWrite -bor [IO.FileShare]::Delete)
    try {
        if (-not ('ReachInstall.FileNative' -as [type])) {
            Add-Type -TypeDefinition @'
using System;
using System.Text;
using System.Runtime.InteropServices;
using Microsoft.Win32.SafeHandles;
namespace ReachInstall {
    public static class FileNative {
        [DllImport("kernel32.dll", CharSet = CharSet.Unicode, SetLastError = true)]
        public static extern uint GetFinalPathNameByHandle(
            SafeFileHandle handle, StringBuilder path, uint length, uint flags);
        [StructLayout(LayoutKind.Sequential)]
        public struct FileInformation {
            public uint Attributes;
            public System.Runtime.InteropServices.ComTypes.FILETIME CreationTime;
            public System.Runtime.InteropServices.ComTypes.FILETIME AccessTime;
            public System.Runtime.InteropServices.ComTypes.FILETIME WriteTime;
            public uint VolumeSerialNumber;
            public uint FileSizeHigh;
            public uint FileSizeLow;
            public uint NumberOfLinks;
            public uint FileIndexHigh;
            public uint FileIndexLow;
        }
        [DllImport("kernel32.dll", SetLastError = true)]
        public static extern bool GetFileInformationByHandle(
            SafeFileHandle handle, out FileInformation info);
    }
}
'@
        }
        $buffer = New-Object System.Text.StringBuilder 32768
        $length = [ReachInstall.FileNative]::GetFinalPathNameByHandle(
            $stream.SafeFileHandle, $buffer, [uint32]$buffer.Capacity, 0)
        if ($length -eq 0 -or $length -ge $buffer.Capacity) {
            throw "Could not resolve screensaver file identity: $path"
        }
        $info = New-Object ReachInstall.FileNative+FileInformation
        if (-not [ReachInstall.FileNative]::GetFileInformationByHandle($stream.SafeFileHandle, [ref]$info)) {
            throw "Could not read screensaver file identity: $path"
        }
        return ('{0:X8}:{1:X8}:{2:X8}' -f $info.VolumeSerialNumber, $info.FileIndexHigh, $info.FileIndexLow)
    }
    finally { $stream.Dispose() }
}

function Test-ReachFileEqual([string]$left, [string]$right) {
    if ([string]::IsNullOrWhiteSpace($left) -or [string]::IsNullOrWhiteSpace($right)) { return $false }
    $leftIdentity = Get-ReachFileIdentity $left
    $rightIdentity = Get-ReachFileIdentity $right
    return [string]::Equals($leftIdentity, $rightIdentity, [StringComparison]::OrdinalIgnoreCase)
}

function Test-ReachSelectionEqual($expected) {
    $actual = @(Get-ReachSelection)
    foreach ($saved in $expected) {
        $current = @($actual | Where-Object Name -eq $saved.Name)[0]
        if ($current.Exists -ne $saved.Exists) { return $false }
        if ($saved.Exists -and ($current.Kind -ne $saved.Kind -or [string]$current.Value -cne [string]$saved.Value)) {
            return $false
        }
    }
    return $true
}

function Restore-ReachSelection($saved) {
    $key = [Microsoft.Win32.Registry]::CurrentUser.OpenSubKey('Control Panel\Desktop', $true)
    if ($null -eq $key) { throw 'Current-user desktop settings are not writable.' }
    try {
        foreach ($entry in $saved) {
            if ($script:ReachSettingsNames -notcontains $entry.Name) { throw "Unexpected settings name: $($entry.Name)" }
            if ($entry.Exists) {
                $kind = [Enum]::Parse([Microsoft.Win32.RegistryValueKind], [string]$entry.Kind)
                $key.SetValue([string]$entry.Name, $entry.Value, $kind)
            }
            else { $key.DeleteValue([string]$entry.Name, $false) }
        }
    }
    finally { $key.Dispose() }
    if (-not (Test-ReachSelectionEqual $saved)) { throw 'Windows did not retain the restored screensaver settings.' }
    Send-ReachDesktopSettingsChanged
}

function Send-ReachDesktopSettingsChanged {
    if (-not ('ReachInstall.Native' -as [type])) {
        Add-Type -TypeDefinition @'
using System;
using System.Runtime.InteropServices;
namespace ReachInstall {
    public static class Native {
        [DllImport("user32.dll", CharSet = CharSet.Unicode)]
        public static extern IntPtr SendMessageTimeout(IntPtr window, uint message,
            UIntPtr wParam, string lParam, uint flags, uint timeout, out UIntPtr result);
    }
}
'@
    }
    [UIntPtr]$result = [UIntPtr]::Zero
    [void][ReachInstall.Native]::SendMessageTimeout([IntPtr]0xffff, 0x1a, [UIntPtr]::Zero,
        'Control Panel\Desktop', 0x2, 2000, [ref]$result)
}
