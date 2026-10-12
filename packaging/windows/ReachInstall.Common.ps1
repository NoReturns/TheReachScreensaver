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
            'windows\Uninstall-ReachScreensaver.ps1', 'windows\Prepare-ReachUpgrade.ps1',
            'windows\Complete-ReachUpgrade.ps1', 'windows\Check-ReachUninstallSelection.ps1',
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

function Test-ReachPathEqual([string]$left, [string]$right) {
    if ([string]::IsNullOrWhiteSpace($left) -or [string]::IsNullOrWhiteSpace($right)) { return $false }
    try { return [IO.Path]::GetFullPath($left) -ieq [IO.Path]::GetFullPath($right) }
    catch { return $false }
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
