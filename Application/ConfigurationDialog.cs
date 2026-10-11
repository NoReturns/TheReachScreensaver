using System.Diagnostics;
using System.Reflection;
using TheReachScreensaver.Diagnostics;
using TheReachScreensaver.Native;
using TheReachScreensaver.Persistence;
using TheReachScreensaver.VisualAssets;

namespace TheReachScreensaver.Application;

internal static class ConfigurationDialog
{
    private const string GitHubUrl = "https://github.com/NoReturns/TheReachScreensaver";

    public static void Show(IntPtr ownerHwnd)
    {
        if (!OperatingSystem.IsWindows()) return;
        System.Windows.Forms.Application.EnableVisualStyles();
        using var form = CreateForm();
        if (ownerHwnd != IntPtr.Zero && Win32.IsWindow(ownerHwnd))
            form.ShowDialog(new WindowOwner(ownerHwnd));
        else
            form.ShowDialog();
    }

    private static Form CreateForm()
    {
        var form = new Form
        {
            Text = "The Reach Screensaver Settings",
            AutoScaleMode = AutoScaleMode.Dpi,
            ClientSize = new Size(620, 640),
            MinimumSize = new Size(560, 540),
            StartPosition = FormStartPosition.CenterParent,
            FormBorderStyle = FormBorderStyle.Sizable,
            MaximizeBox = false,
            AutoScroll = true
        };
        var root = new TableLayoutPanel
        {
            Dock = DockStyle.Top,
            AutoSize = true,
            AutoSizeMode = AutoSizeMode.GrowAndShrink,
            Padding = new Padding(16),
            ColumnCount = 1,
            RowCount = 5
        };
        root.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
        for (var row = 0; row < 5; row++) root.RowStyles.Add(new RowStyle(SizeType.AutoSize));
        form.Controls.Add(root);

        var imagery = Section("Planetary imagery");
        var imageryRows = Rows();
        var modeValue = AddRow(imageryRows, "Current visual mode", "Loading...");
        var availableValue = AddRow(imageryRows, "Available images", "Loading...");
        var verifiedValue = AddRow(imageryRows, "Already verified", "Loading...");
        var missingValue = AddRow(imageryRows, "Missing downloads", "Loading...");
        var sizeValue = AddRow(imageryRows, "Required download", "Loading...");
        var progressValue = AddRow(imageryRows, "Progress", "Idle");
        var statusValue = AddRow(imageryRows, "Status", "Ready");
        var download = AddButtonRow(imageryRows, "Download High-Quality Images", enabled: false);
        var cancelDownload = AddButtonRow(imageryRows, "Cancel Download", enabled: false);
        CancellationTokenSource? downloadCancellation = null;
        bool busy = false;
        void RefreshImagery()
        {
            try
            {
                var configuration = ReachVisualConfiguration.Load();
                var manifest = ReachAssetManifest.LoadPinned();
                var cache = new ReachAssetCache(manifest, configuration);
                var missingAssets = manifest.Assets.Where(asset => !cache.IsVerified(asset)).ToArray();
                var verified = manifest.Assets.Length - missingAssets.Length;
                var missingBytes = missingAssets.Sum(asset => asset.ExpectedFileSize);
                modeValue.Text = configuration.Mode.ToString();
                availableValue.Text = manifest.Assets.Length.ToString();
                verifiedValue.Text = verified.ToString();
                missingValue.Text = missingAssets.Length.ToString();
                sizeValue.Text = $"{missingBytes:N0} bytes ({missingBytes / 1048576.0:0.0} MiB)";
                download.Enabled = !busy && missingAssets.Length > 0;
            }
            catch (Exception ex)
            {
                Log.Warn($"Could not show imagery status: {ex.Message}");
                statusValue.Text = "Imagery status unavailable; see the application log.";
                download.Enabled = false;
            }
        }
        cancelDownload.Click += (_, _) => downloadCancellation?.Cancel();
        download.Click += async (_, _) =>
        {
            if (busy) return;
            try
            {
                var configuration = ReachVisualConfiguration.Load();
                var manifest = ReachAssetManifest.LoadPinned();
                var cache = new ReachAssetCache(manifest, configuration with { Mode = ReachVisualMode.Enhanced });
                var pending = manifest.Assets.Where(asset => !cache.IsVerified(asset)).ToArray();
                if (pending.Length == 0) { RefreshImagery(); return; }
                var total = pending.Sum(asset => asset.ExpectedFileSize);
                var publishers = string.Join("\n", pending.Select(asset =>
                    $"{asset.SourceAttribution} - {configuration.SourceFor(asset).Host}").Distinct());
                var question = $"Download {pending.Length} high-quality image(s), {total:N0} bytes "
                    + $"({total / 1048576.0:0.0} MiB)?\n\nPublisher / source:\n{publishers}\n\n"
                    + "Verified files will be skipped. Bundled imagery remains available.";
                if (MessageBox.Show(form, question, "Confirm imagery download", MessageBoxButtons.YesNo,
                        MessageBoxIcon.Question) != DialogResult.Yes)
                {
                    statusValue.Text = "Download declined.";
                    return;
                }
                busy = true;
                download.Enabled = false;
                cancelDownload.Enabled = true;
                statusValue.Text = "Downloading...";
                progressValue.Text = $"0 / {total:N0} bytes";
                downloadCancellation = new CancellationTokenSource();
                var token = downloadCancellation.Token;
                var progress = new Progress<ReachAssetCache.AcquisitionProgress>(update =>
                {
                    if (!form.IsDisposed && busy)
                        progressValue.Text = $"{update.AssetId}: {update.BytesProcessed:N0} / {update.TotalBytes:N0} bytes";
                });
                var (result, logWarning) = await Task.Run(async () =>
                {
                    var acquired = await cache.AcquireApprovedDetailedAsync(true, cancellationToken: token,
                        progress: progress);
                    string? warning = null;
                    try { ReachVisualSetup.WriteAcquisitionLog(ReachVisualSetup.AcquisitionLogPath, manifest, cache, acquired); }
                    catch (Exception ex)
                    {
                        Log.Warn($"Could not write asset acquisition log: {ex.Message}");
                        warning = " Acquisition log could not be written.";
                    }
                    if (acquired.Failures.Count == 0 && manifest.Assets.All(cache.IsVerified))
                        ReachVisualConfiguration.SavePreferences(ReachVisualConfiguration.Load() with
                            { Mode = ReachVisualMode.Enhanced });
                    return (acquired, warning);
                }, token);
                statusValue.Text = result.Failures.Count == 0
                    ? $"Success: {result.Installed} installed, {result.Skipped} already verified. Enhanced mode enabled for next launch."
                    : $"Failed: {result.Failures.Count} image(s). Retry is available; see acquisition log.";
                statusValue.Text += logWarning;
            }
            catch (OperationCanceledException)
            {
                statusValue.Text = "Download cancelled. Verified images are retained.";
                Log.Info("Reach imagery download cancelled in Settings.");
            }
            catch (Exception ex)
            {
                Log.Error("Reach imagery download failed in Settings.", ex);
                statusValue.Text = "Download failed; see the application log. Retry is available.";
            }
            finally
            {
                busy = false;
                cancelDownload.Enabled = false;
                downloadCancellation?.Dispose();
                downloadCancellation = null;
                RefreshImagery();
            }
        };
        form.FormClosing += (_, e) =>
        {
            if (!busy) return;
            e.Cancel = true;
            downloadCancellation?.Cancel();
            statusValue.Text = "Cancelling download...";
        };
        RefreshImagery();
        imagery.Controls.Add(imageryRows);
        root.Controls.Add(imagery, 0, 0);

        var journey = Section("Journey progress");
        var journeyRows = Rows();
        AddRow(journeyRows, "Trip", "Your current journey is kept between launches.");
        var resetStatus = AddRow(journeyRows, "Reset status", "No reset pending.");
        try
        {
            if (JourneyReset.PendingToken() is not null)
                resetStatus.Text = "Reset scheduled for the next fullscreen launch.";
        }
        catch (Exception ex)
        {
            Log.Error("Could not inspect journey reset request.", ex);
            resetStatus.Text = "Reset status unavailable; see the application log.";
        }
        AddButtonRow(journeyRows, "Reset Trip", enabled: true, () =>
        {
            const string question = "Reset your journey?\n\n"
                + "This will return your voyage to its starting point and reset your total distance travelled and completed round trips.\n\n"
                + "Downloaded images and application settings will be preserved.\n\n"
                + "This cannot be undone.";
            if (MessageBox.Show(form, question, "Reset Trip", MessageBoxButtons.YesNo,
                    MessageBoxIcon.Warning, MessageBoxDefaultButton.Button2) != DialogResult.Yes) return;
            try
            {
                JourneyReset.Schedule();
                resetStatus.Text = "Reset scheduled for the next fullscreen launch.";
                MessageBox.Show(form, "Journey reset scheduled for the next fullscreen launch.",
                    "Reset Trip", MessageBoxButtons.OK, MessageBoxIcon.Information);
            }
            catch (Exception ex)
            {
                Log.Error("Could not schedule Reach journey reset.", ex);
                resetStatus.Text = "Reset failed. Check the details before trying again.";
                MessageBox.Show(form, "Journey reset failed. " + ex.Message,
                    "Reset Trip", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        });
        journey.Controls.Add(journeyRows);
        root.Controls.Add(journey, 0, 1);

        var about = Section("About");
        var aboutRows = Rows();
        var version = Assembly.GetExecutingAssembly().GetName().Version?.ToString(3) ?? "unknown";
        AddRow(aboutRows, "Application", $"The Reach Screensaver {version}");
        AddRow(aboutRows, "Origin", "Based on Your Eyes In Space by Berk Comba.");
        AddButtonRow(aboutRows, "GitHub", enabled: true, () => OpenGitHub(form));
        about.Controls.Add(aboutRows);
        root.Controls.Add(about, 0, 2);

        var credits = Section("Credits");
        var creditsRows = Rows();
        AddRow(creditsRows, "Attribution", "Project, data, imagery, and dependency credits.");
        AddButtonRow(creditsRows, "Open Credits", enabled: true, () => OpenCredits(form));
        credits.Controls.Add(creditsRows);
        root.Controls.Add(credits, 0, 3);

        var close = new Button { Text = "Close", AutoSize = true, Anchor = AnchorStyles.Right };
        close.Click += (_, _) => form.Close();
        root.Controls.Add(close, 0, 4);
        form.AcceptButton = close;
        form.CancelButton = close;
        return form;
    }

    private static GroupBox Section(string title) => new()
    {
        Text = title,
        AutoSize = true,
        AutoSizeMode = AutoSizeMode.GrowAndShrink,
        Dock = DockStyle.Top,
        Padding = new Padding(12),
        Margin = new Padding(0, 0, 0, 10)
    };

    private static TableLayoutPanel Rows()
    {
        var table = new TableLayoutPanel { AutoSize = true, AutoSizeMode = AutoSizeMode.GrowAndShrink,
            Dock = DockStyle.Top, ColumnCount = 2 };
        table.ColumnStyles.Add(new ColumnStyle(SizeType.AutoSize));
        table.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
        return table;
    }

    private static Label AddRow(TableLayoutPanel table, string label, string value)
    {
        var row = table.RowCount++;
        table.RowStyles.Add(new RowStyle(SizeType.AutoSize));
        table.Controls.Add(new Label { Text = label, AutoSize = true, Margin = new Padding(0, 4, 16, 4) }, 0, row);
        var valueLabel = new Label { Text = value, AutoSize = true, MaximumSize = new Size(410, 0),
            Margin = new Padding(0, 4, 0, 4) };
        table.Controls.Add(valueLabel, 1, row);
        return valueLabel;
    }

    private static Button AddButtonRow(TableLayoutPanel table, string label, bool enabled, Action? action = null)
    {
        var row = table.RowCount++;
        table.RowStyles.Add(new RowStyle(SizeType.AutoSize));
        var button = new Button { Text = label, AutoSize = true, Enabled = enabled,
            Margin = new Padding(0, 10, 0, 2) };
        if (action is not null) button.Click += (_, _) => action();
        table.Controls.Add(button, 0, row);
        table.SetColumnSpan(button, 2);
        return button;
    }

    private static void OpenGitHub(Form owner)
    {
        try { Process.Start(new ProcessStartInfo(GitHubUrl) { UseShellExecute = true }); }
        catch (Exception ex) { MessageBox.Show(owner, ex.Message, "Could not open GitHub", MessageBoxButtons.OK, MessageBoxIcon.Error); }
    }

    private static void OpenCredits(Form owner)
    {
        var executableDirectory = Path.GetDirectoryName(Environment.ProcessPath);
        var path = Path.Combine(executableDirectory ?? AppContext.BaseDirectory, "CREDITS.txt");
        if (!File.Exists(path)) path = Path.Combine(AppContext.BaseDirectory, "CREDITS.txt");
        if (!File.Exists(path))
        {
            MessageBox.Show(owner, "The packaged credits file is missing.", "Credits", MessageBoxButtons.OK, MessageBoxIcon.Error);
            return;
        }
        try
        {
            var start = new ProcessStartInfo("notepad.exe") { UseShellExecute = false };
            start.ArgumentList.Add(path);
            Process.Start(start);
        }
        catch (Exception ex) { MessageBox.Show(owner, ex.Message, "Could not open Credits", MessageBoxButtons.OK, MessageBoxIcon.Error); }
    }

    private sealed class WindowOwner(IntPtr handle) : IWin32Window
    {
        public IntPtr Handle => handle;
    }

    public static void ShowError(string message) => ShowMessage(message, iconError: true);
    public static void ShowInfo(string message) => ShowMessage(message, iconError: false);

    private static void ShowMessage(string message, bool iconError)
    {
        Log.Info($"Configuration/message: {message}");
        if (OperatingSystem.IsWindows())
        {
            var icon = iconError ? Win32.MbIconError : Win32.MbIconInformation;
            Win32.MessageBoxW(IntPtr.Zero, message, "The Reach Screensaver", Win32.MbOk | icon);
            return;
        }
        try { Console.WriteLine(message); }
        catch { /* WinExe without a console. */ }
    }
}
