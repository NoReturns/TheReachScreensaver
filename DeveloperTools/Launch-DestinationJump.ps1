param([switch]$ValidateOnly, [switch]$UiSelfTest)

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
$form.ClientSize = [System.Drawing.Size]::new(560, 470)
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
Add-Label 'ORDERED QUEUE' 164

$fromBox = [System.Windows.Forms.ComboBox]::new()
$toBox = [System.Windows.Forms.ComboBox]::new()
foreach ($box in @($fromBox, $toBox)) {
    $box.DropDownStyle = 'DropDownList'
    $box.SetBounds(178, $(if ($box -eq $fromBox) { 18 } else { 68 }), 360, 29)
    foreach ($stop in $stops) {
        $suffix = if ($box -eq $fromBox -and -not $stop.fromSupported) { ' (unsupported FROM)' } else { '' }
        [void]$box.Items.Add(('{0:D2}  {1}{2}' -f [int]$stop.index, $stop.name, $suffix))
    }
    $form.Controls.Add($box)
}
[void]$toBox.Items.Add('DEV  Sagittarius A*')
$fromBox.SelectedIndex = 0
$toBox.SelectedIndex = 1

$duration = [System.Windows.Forms.NumericUpDown]::new()
$duration.Minimum = 2
$duration.Maximum = 120
$duration.Value = 8
$duration.SetBounds(178, 118, 90, 28)
$form.Controls.Add($duration)

$queue = [System.Collections.ArrayList]::new()
$queueBox = [System.Windows.Forms.ListBox]::new()
$queueBox.SetBounds(178, 162, 260, 172)
$form.Controls.Add($queueBox)

function Refresh-Queue {
    $selected = $queueBox.SelectedIndex
    $queueBox.Items.Clear()
    for ($i = 0; $i -lt $queue.Count; $i++) {
        [void]$queueBox.Items.Add(('{0}. {1}' -f ($i + 1), $queue[$i].Name))
    }
    if ($selected -ge 0 -and $selected -lt $queue.Count) { $queueBox.SelectedIndex = $selected }
}
function Add-QueueButton($text, $top, $action) {
    $button = [System.Windows.Forms.Button]::new()
    $button.Text = $text
    $button.SetBounds(448, $top, 90, 30)
    $button.Add_Click($action)
    $form.Controls.Add($button)
    return $button
}
$addQueueButton = Add-QueueButton 'Add' 162 {
    if ($toBox.SelectedIndex -lt $stops.Count) {
        $stop = $stops[$toBox.SelectedIndex]
        [void]$queue.Add([pscustomobject]@{ Token = [string]$stop.index; Name = $stop.name })
    } else {
        [void]$queue.Add([pscustomobject]@{ Token = 'sgra'; Name = 'Sagittarius A*' })
    }
    Refresh-Queue
}
$removeQueueButton = Add-QueueButton 'Remove' 196 {
    if ($queueBox.SelectedIndex -ge 0) { $queue.RemoveAt($queueBox.SelectedIndex); Refresh-Queue }
}
$upQueueButton = Add-QueueButton 'Up' 230 {
    $i = $queueBox.SelectedIndex
    if ($i -gt 0) { $item = $queue[$i]; $queue[$i] = $queue[$i - 1]; $queue[$i - 1] = $item; Refresh-Queue; $queueBox.SelectedIndex = $i - 1 }
}
$downQueueButton = Add-QueueButton 'Down' 264 {
    $i = $queueBox.SelectedIndex
    if ($i -ge 0 -and $i -lt $queue.Count - 1) { $item = $queue[$i]; $queue[$i] = $queue[$i + 1]; $queue[$i + 1] = $item; Refresh-Queue; $queueBox.SelectedIndex = $i + 1 }
}
$clearQueueButton = Add-QueueButton 'Clear' 298 {
    $queue.Clear(); Refresh-Queue
}

$launch = [System.Windows.Forms.Button]::new()
$launch.Text = 'Launch single'
$launch.SetBounds(178, 350, 140, 34)
$form.Controls.Add($launch)
$launchQueue = [System.Windows.Forms.Button]::new()
$launchQueue.Text = 'Start journey'
$launchQueue.SetBounds(330, 350, 140, 34)
$form.Controls.Add($launchQueue)

$status = [System.Windows.Forms.Label]::new()
$status.Text = 'Local developer build only. Fullscreen exits on input.'
$status.SetBounds(20, 405, 520, 45)
$form.Controls.Add($status)

function Start-DestinationTest($isQueue) {
    $launch.Enabled = $false
    $launchQueue.Enabled = $false
    try {
        $from = $stops[$fromBox.SelectedIndex]
        if (-not $from.fromSupported) {
            throw "$($from.name) cannot be a FROM stop. Its photographic landmark has no initial body camera pose."
        }
        if ($isQueue) {
            if ($queue.Count -eq 0) { throw 'Add at least one destination to the queue.' }
            $previousToken = [string]$from.index
            for ($i = 0; $i -lt $queue.Count; $i++) {
                if ($queue[$i].Token -eq $previousToken) {
                    throw "Queue destination $($i + 1) repeats the previous stop."
                }
                $previousToken = $queue[$i].Token
            }
            $routeArgs = '--reach-dev-queue {0} {1} {2} /s' -f [int]$from.index,
                (($queue | ForEach-Object { $_.Token }) -join ','), [int]$duration.Value
        } else {
            if ($toBox.SelectedIndex -ge $stops.Count) { throw 'Add Sagittarius A* to the queue and select Start journey.' }
            $to = $stops[$toBox.SelectedIndex]
            if ($from.index -eq $to.index) { throw 'FROM and TO must be different itinerary stops.' }
            $routeArgs = '--reach-dev-route {0} {1} {2} /s' -f [int]$from.index,
                [int]$to.index, [int]$duration.Value
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
        $start.Arguments = $routeArgs
        $start.EnvironmentVariables['REACH_WEB_DIST'] = $devDist
        $process = [Diagnostics.Process]::Start($start)
        if ($null -eq $process) { throw 'Windows did not start the debug screensaver.' }
        $status.Text = "Launched test process $($process.Id). Move the mouse or press a key to exit."
    } catch {
        $status.Text = 'Launch failed.'
        [System.Windows.Forms.MessageBox]::Show($_.Exception.Message, 'Destination Jump Testing') | Out-Null
    } finally { $launch.Enabled = $true; $launchQueue.Enabled = $true }
}
$launch.Add_Click({ Start-DestinationTest $false })
$launchQueue.Add_Click({ Start-DestinationTest $true })

if ($UiSelfTest) {
    $form.ShowInTaskbar = $false
    $form.Opacity = 0
    $form.Show()
    try {
        $toBox.SelectedIndex = 5
        $addQueueButton.PerformClick()
        $toBox.SelectedIndex = 4
        $addQueueButton.PerformClick()
        if ($queue.Count -ne 2 -or $queueBox.Items.Count -ne 2) { throw 'Add did not update the queue.' }
        $queueBox.SelectedIndex = 1
        $upQueueButton.PerformClick()
        if ($queue[0].Token -ne '4') { throw 'Up did not reorder the queue.' }
        $downQueueButton.PerformClick()
        if ($queue[1].Token -ne '4') { throw 'Down did not reorder the queue.' }
        $removeQueueButton.PerformClick()
        if ($queue.Count -ne 1) { throw 'Remove did not update the queue.' }
        $clearQueueButton.PerformClick()
        if ($queue.Count -ne 0 -or $queueBox.Items.Count -ne 0) { throw 'Clear did not empty the queue.' }
        Write-Output 'PASS destination queue UI Add, Remove, Up, Down, Clear'
    } finally { $form.Close() }
    return
}

[System.Windows.Forms.Application]::Run($form)
