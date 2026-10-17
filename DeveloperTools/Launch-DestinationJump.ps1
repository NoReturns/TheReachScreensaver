param([switch]$ValidateOnly)

$ErrorActionPreference = 'Stop'
Add-Type -AssemblyName System.Windows.Forms
Add-Type -AssemblyName System.Drawing

try {
    $hostRepo = (Resolve-Path -LiteralPath (Join-Path $PSScriptRoot '..')).Path
    $universeRepo = Join-Path (Split-Path $hostRepo -Parent) 'Universe'
    $itineraryScript = Join-Path $universeRepo 'scripts\print-reach-itinerary.mjs'
    if (-not (Test-Path -LiteralPath $itineraryScript)) {
        throw "Universe itinerary source is missing: $itineraryScript"
    }
    $node = (Get-Command node.exe -ErrorAction Stop).Source
    Push-Location $universeRepo
    try {
        $itineraryJson = & $node $itineraryScript
        if ($LASTEXITCODE -ne 0) { throw 'Could not read the production itinerary from Universe.' }
    } finally { Pop-Location }
    $itinerary = $itineraryJson | ConvertFrom-Json
    $stops = @($itinerary.stops)
    if ($stops.Count -lt 2) { throw 'The production itinerary has fewer than two stops.' }
} catch {
    [System.Windows.Forms.MessageBox]::Show($_.Exception.Message, 'Destination Jump Testing') | Out-Null
    throw
}

if ($ValidateOnly) {
    Write-Output "Loaded $($stops.Count) itinerary stops, version $($itinerary.version)."
    return
}

[System.Windows.Forms.Application]::EnableVisualStyles()
$form = [System.Windows.Forms.Form]::new()
$form.Text = 'The Reach - Destination Jump Testing'
$form.ClientSize = [System.Drawing.Size]::new(500, 260)
$form.StartPosition = 'CenterScreen'
$form.FormBorderStyle = 'FixedDialog'
$form.MaximizeBox = $false

function Add-Label($text, $top) {
    $label = [System.Windows.Forms.Label]::new()
    $label.Text = $text
    $label.SetBounds(20, $top, 155, 25)
    $form.Controls.Add($label)
}
Add-Label 'FROM' 22
Add-Label 'TO' 72
Add-Label 'CRUISE DURATION (s)' 122

$fromBox = [System.Windows.Forms.ComboBox]::new()
$toBox = [System.Windows.Forms.ComboBox]::new()
foreach ($box in @($fromBox, $toBox)) {
    $box.DropDownStyle = 'DropDownList'
    $box.SetBounds(178, $(if ($box -eq $fromBox) { 18 } else { 68 }), 300, 29)
    foreach ($stop in $stops) {
        $suffix = if ($box -eq $fromBox -and -not $stop.fromSupported) { ' (unsupported FROM)' } else { '' }
        [void]$box.Items.Add(('{0:D2}  {1}{2}' -f [int]$stop.index, $stop.name, $suffix))
    }
    $form.Controls.Add($box)
}
$fromBox.SelectedIndex = 0
$toBox.SelectedIndex = 1

$duration = [System.Windows.Forms.NumericUpDown]::new()
$duration.Minimum = 2
$duration.Maximum = 120
$duration.Value = 8
$duration.SetBounds(178, 118, 90, 28)
$form.Controls.Add($duration)

$launch = [System.Windows.Forms.Button]::new()
$launch.Text = 'LAUNCH TEST'
$launch.SetBounds(178, 163, 140, 34)
$form.Controls.Add($launch)

$status = [System.Windows.Forms.Label]::new()
$status.Text = 'Local developer build only. Fullscreen exits on input.'
$status.SetBounds(20, 215, 460, 35)
$form.Controls.Add($status)

$launch.Add_Click({
    $launch.Enabled = $false
    try {
        $from = $stops[$fromBox.SelectedIndex]
        $to = $stops[$toBox.SelectedIndex]
        if ($from.index -eq $to.index) { throw 'FROM and TO must be different itinerary stops.' }
        if (-not $from.fromSupported) {
            throw "$($from.name) cannot be a FROM stop. Its photographic landmark has no initial body camera pose."
        }

        $devDist = Join-Path $universeRepo 'dist\destination-test'
        $status.Text = 'Building isolated developer web bundle...'
        [System.Windows.Forms.Application]::DoEvents()
        $previousDevFlag = [Environment]::GetEnvironmentVariable('VITE_REACH_DEV_TEST', 'Process')
        $previousErrorPreference = $ErrorActionPreference
        Push-Location $universeRepo
        try {
            $env:VITE_REACH_DEV_TEST = '1'
            $ErrorActionPreference = 'Continue'
            $webOutput = (& npm.cmd run build -- --outDir $devDist 2>&1 | Out-String)
            if ($LASTEXITCODE -ne 0) { throw "Universe build failed: $webOutput" }
        } finally {
            $ErrorActionPreference = $previousErrorPreference
            [Environment]::SetEnvironmentVariable('VITE_REACH_DEV_TEST', $previousDevFlag, 'Process')
            Pop-Location
        }
        if (-not (Test-Path -LiteralPath (Join-Path $devDist 'index.html'))) {
            throw 'The developer web bundle was not created.'
        }

        $status.Text = 'Building local debug screensaver...'
        [System.Windows.Forms.Application]::DoEvents()
        Push-Location $hostRepo
        try {
            $ErrorActionPreference = 'Continue'
            $hostOutput = (& dotnet build .\TheReachScreensaver.csproj -c Debug --no-restore -v:q 2>&1 | Out-String)
            if ($LASTEXITCODE -ne 0) { throw "Screensaver build failed: $hostOutput" }
        } finally {
            $ErrorActionPreference = $previousErrorPreference
            Pop-Location
        }
        $debugDir = Join-Path $hostRepo 'bin\Debug\net10.0-windows'
        $exe = Join-Path $debugDir 'TheReachScreensaver.exe'
        $scr = Join-Path $debugDir 'TheReachScreensaver.scr'
        if (-not (Test-Path -LiteralPath $exe)) { throw "Debug screensaver executable is missing: $exe" }
        Copy-Item -LiteralPath $exe -Destination $scr -Force

        $start = [Diagnostics.ProcessStartInfo]::new($scr)
        $start.UseShellExecute = $false
        $start.WorkingDirectory = $debugDir
        $start.Arguments = '--reach-dev-route {0} {1} {2} /s' -f [int]$from.index,
            [int]$to.index, [int]$duration.Value
        $start.EnvironmentVariables['REACH_WEB_DIST'] = $devDist
        $process = [Diagnostics.Process]::Start($start)
        if ($null -eq $process) { throw 'Windows did not start the debug screensaver.' }
        $status.Text = "Launched test process $($process.Id). Move the mouse or press a key to exit."
    } catch {
        $status.Text = 'Launch failed.'
        [System.Windows.Forms.MessageBox]::Show($_.Exception.Message, 'Destination Jump Testing') | Out-Null
    } finally { $launch.Enabled = $true }
}.GetNewClosure())

[System.Windows.Forms.Application]::Run($form)
