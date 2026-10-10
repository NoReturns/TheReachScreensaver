using System.Text.Json;
using System.Security.Cryptography;
using System.Text;
using Microsoft.Web.WebView2.Core;
using Microsoft.Web.WebView2.WinForms;
using TheReachScreensaver.Diagnostics;
using TheReachScreensaver.Native;
using TheReachScreensaver.Display;
using System.Runtime.InteropServices;
using TheReachScreensaver.VisualAssets;

namespace TheReachScreensaver.Application;

internal static class ReachWebApplication
{
    private const string Host = "reach.example";
    private const string AssetHost = "reach-assets.example";
    private const string StartUrl = "https://reach.example/index.html?reach=1";

    public static int RunPreview(IntPtr parentHwnd)
    {
        if (!OperatingSystem.IsWindows())
        {
            Log.Warn("Reach preview embedding requires Windows; exiting without a window.");
            return 0;
        }
        if (parentHwnd == IntPtr.Zero || !Win32.IsWindow(parentHwnd))
        {
            Log.Warn("Reach preview parent HWND is missing or invalid; exiting without a window.");
            return 0;
        }
        return Run(fullscreen: false, previewParent: parentHwnd);
    }

    public static int Run(bool fullscreen, IntPtr previewParent = default)
    {
        if (!OperatingSystem.IsWindows())
        {
            Log.Error("The Reach WebView2 prototype requires Windows.");
            return 1;
        }

        var dist = FindDist();
        if (dist is null)
        {
            const string message = "Reach web assets were not found. Place the built Universe dist folder beside the screensaver as dist, or set REACH_WEB_DIST to its absolute path. Build TheReachUniverse-source with npm run build first.";
            Log.Error(message);
            ConfigurationDialog.ShowError(message);
            return 1;
        }
        ReachVisualConfiguration configuration;
        IReadOnlyList<ReachAsset> verifiedAssets;
        try
        {
            ReachVisualConfiguration.EnsureDefaultFile();
            configuration = ReachVisualConfiguration.Load();
            verifiedAssets = new ReachAssetCache(ReachAssetManifest.LoadPinned(), configuration).VerifiedAssets();
            Log.Info($"Reach visual mode: {configuration.Mode}; verified local assets: {verifiedAssets.Count}.");
        }
        catch (Exception ex)
        {
            Log.Warn($"Visual configuration unavailable; continuing with basic bundled maps: {ex.Message}");
            configuration = ReachVisualConfiguration.Default;
            verifiedAssets = [];
        }

        try
        {
            var version = CoreWebView2Environment.GetAvailableBrowserVersionString();
            Log.Info($"WebView2 Runtime {version}; dist={dist}");
        }
        catch (Exception ex)
        {
            var message = "Microsoft Edge WebView2 Runtime is required for The Reach screensaver. Install the Evergreen Runtime. " + ex.Message;
            Log.Error(message, ex);
            ConfigurationDialog.ShowError(message);
            return 1;
        }

        if (fullscreen || previewParent != IntPtr.Zero)
            Win32.SetProcessDpiAwarenessContext(Win32.DpiAwarenessContextPerMonitorAwareV2);
        System.Windows.Forms.Application.EnableVisualStyles();
        try
        {
            if (fullscreen)
            {
                var layout = DisplayLayout.Query();
                if (RequiresPanorama(layout))
                {
                    using var context = new ReachWebMultiContext(dist, configuration, verifiedAssets, layout);
                    System.Windows.Forms.Application.Run(context);
                    return context.ExitCode;
                }
                Log.Info("Reach fullscreen found one display; using the single-monitor path.");
            }
            using var window = new ReachWebWindow(dist, fullscreen, previewParent == IntPtr.Zero,
                configuration, verifiedAssets, previewParent: previewParent);
            if (previewParent != IntPtr.Zero && !window.TryAttachPreview())
                return 0;
            System.Windows.Forms.Application.Run(window);
            return window.ExitCode;
        }
        catch (Exception ex)
        {
            Log.Error("Could not open Reach WebView2 window.", ex);
            ConfigurationDialog.ShowError("Could not open Reach WebView2 window.\n\n" + ex.Message);
            return 1;
        }
    }

    private static string? FindDist()
    {
        var explicitPath = Environment.GetEnvironmentVariable("REACH_WEB_DIST");
        if (!string.IsNullOrWhiteSpace(explicitPath))
        {
            if (ValidDist(explicitPath)) return Path.GetFullPath(explicitPath);
            Log.Error($"REACH_WEB_DIST does not contain a built index.html: {explicitPath}");
            return null;
        }

        var executableDirectory = Path.GetDirectoryName(Environment.ProcessPath);
        if (!string.IsNullOrEmpty(executableDirectory))
        {
            var packagedDist = Path.Combine(executableDirectory, "dist");
            Log.Info($"Reach packaged assets lookup: {packagedDist}.");
            if (ValidDist(packagedDist)) return packagedDist;
        }

        var installedDist = Path.Combine(AppContext.BaseDirectory, "dist");
        if (ValidDist(installedDist)) return installedDist;

        foreach (var start in new[] { Directory.GetCurrentDirectory(), AppContext.BaseDirectory })
        {
            for (var parent = new DirectoryInfo(start); parent is not null; parent = parent.Parent)
            {
                var candidate = Path.Combine(parent.FullName, "TheReachUniverse-source", "dist");
                if (ValidDist(candidate)) return candidate;
            }
        }

        return null;
    }

    private static bool ValidDist(string path) =>
        Directory.Exists(path) && File.Exists(Path.Combine(path, "index.html"));

    private static bool RequiresPanorama(DisplayLayout layout)
    {
        if (layout.Displays.Count == 1 && layout.Displays[0].Name == "fallback")
            throw new InvalidOperationException("Windows display enumeration failed; Reach fullscreen cannot use a fabricated display.");
        if (layout.Displays.Count == 0 || layout.Displays.Count(display => display.IsPrimary) != 1)
            throw new InvalidOperationException("Windows did not identify exactly one primary display.");
        foreach (var display in layout.Displays)
        {
            var bounds = display.Bounds;
            var desktop = layout.VirtualBounds;
            if (bounds.Width <= 0 || bounds.Height <= 0 || bounds.Left < desktop.Left || bounds.Top < desktop.Top
                || bounds.Right > desktop.Right || bounds.Bottom > desktop.Bottom)
                throw new InvalidOperationException($"Display {display.Name} is outside the virtual desktop.");
        }
        return layout.Displays.Count > 1;
    }

    private sealed record PanoramaDisplay(DisplayRegion Monitor, PixelRect VirtualBounds, string SessionId)
    {
        public int ViewportOffsetX => Monitor.Bounds.X - VirtualBounds.X;
        public int ViewportOffsetY => Monitor.Bounds.Y - VirtualBounds.Y;
    }

    public static int RunDisplayFixtures()
    {
        try
        {
            var fixtures = new[]
            {
                new[] { ("primary", new PixelRect(0, 0, 1920, 1080), true, 96f) },
                new[] { ("left", new PixelRect(-2560, 0, 2560, 1440), false, 144f),
                    ("primary", new PixelRect(0, 0, 1920, 1080), true, 96f) },
                new[] { ("left", new PixelRect(-3840, -720, 3840, 2160), false, 144f),
                    ("primary", new PixelRect(0, 0, 2560, 1440), true, 96f),
                    ("right", new PixelRect(2560, 360, 1920, 1080), false, 96f) },
                new[] { ("left", new PixelRect(-1920, 200, 1920, 1080), false, 96f),
                    ("primary", new PixelRect(0, 0, 2560, 1440), true, 120f),
                    ("above", new PixelRect(0, -1080, 1920, 1080), false, 96f),
                    ("right", new PixelRect(2560, 300, 1280, 1024), false, 144f) }
            };
            for (var index = 0; index < fixtures.Length; index++)
            {
                var monitors = fixtures[index].Select(item => new DisplayRegion
                {
                    Name = item.Item1, Bounds = item.Item2, IsPrimary = item.Item3,
                    DpiX = item.Item4, DpiY = item.Item4
                }).ToArray();
                var left = monitors.Min(display => display.Bounds.Left);
                var top = monitors.Min(display => display.Bounds.Top);
                var right = monitors.Max(display => display.Bounds.Right);
                var bottom = monitors.Max(display => display.Bounds.Bottom);
                var desktop = new PixelRect(left, top, right - left, bottom - top);
                var layout = new DisplayLayout(desktop, monitors);
                if (RequiresPanorama(layout) != (monitors.Length > 1))
                    throw new InvalidOperationException($"Fixture {index + 1} chose the wrong window mode.");
                var slices = monitors.Select(display => new PanoramaDisplay(display, desktop, "fixture")).ToArray();
                if (slices.Any(slice => slice.ViewportOffsetX < 0 || slice.ViewportOffsetY < 0
                    || slice.ViewportOffsetX + slice.Monitor.Bounds.Width > desktop.Width
                    || slice.ViewportOffsetY + slice.Monitor.Bounds.Height > desktop.Height)
                    || slices.Select(slice => (slice.ViewportOffsetX, slice.ViewportOffsetY,
                        slice.Monitor.Bounds.Width, slice.Monitor.Bounds.Height)).Distinct().Count() != monitors.Length)
                    throw new InvalidOperationException($"Fixture {index + 1} has an invalid or duplicate viewport.");
                Console.WriteLine($"PASS {monitors.Length}-display native layout: {(monitors.Length == 1 ? "normal" : "panorama")}");
            }
            try { RequiresPanorama(DisplayLayout.Fallback()); throw new InvalidOperationException("Fabricated display was accepted."); }
            catch (InvalidOperationException ex) when (ex.Message.Contains("display enumeration failed"))
            {
                Console.WriteLine("PASS failed enumeration does not silently select one display");
            }
            return 0;
        }
        catch (Exception ex)
        {
            Console.Error.WriteLine($"FAIL native display fixture: {ex.Message}");
            return 1;
        }
    }

    private sealed class ReachWebMultiContext : System.Windows.Forms.ApplicationContext
    {
        private readonly List<ReachWebWindow> _windows = [];
        private readonly string _dist;
        private readonly ReachVisualConfiguration _configuration;
        private readonly IReadOnlyList<ReachAsset> _assets;
        private readonly DisplayLayout _layout;
        private readonly string _sessionId = Guid.NewGuid().ToString("N");
        private Task<CoreWebView2Environment>? _environment;
        private ReachWebInputMonitor? _inputMonitor;
        private readonly System.Windows.Forms.Timer _armTimer = new() { Interval = 300 };
        private int _remaining;
        private bool _closing;
        private bool _cursorHidden;

        public int ExitCode { get; private set; }

        public ReachWebMultiContext(string dist, ReachVisualConfiguration configuration,
            IReadOnlyList<ReachAsset> assets, DisplayLayout layout)
        {
            _dist = dist;
            _configuration = configuration;
            _assets = assets;
            _layout = layout;
            System.Windows.Forms.Application.Idle += Start;
            _armTimer.Tick += ArmInput;
        }

        private Task<CoreWebView2Environment> SharedEnvironment()
        {
            if (_environment is not null) return _environment;
            var profile = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
                "TheReachScreensaver", "WebView2Prototype");
            Directory.CreateDirectory(profile);
            return _environment = CoreWebView2Environment.CreateAsync(userDataFolder: profile,
                options: new CoreWebView2EnvironmentOptions("--no-proxy-server"));
        }

        private void Start(object? sender, EventArgs e)
        {
            System.Windows.Forms.Application.Idle -= Start;
            try
            {
                foreach (var display in _layout.Displays.OrderByDescending(display => display.IsPrimary))
                {
                    var window = new ReachWebWindow(_dist, true, display.IsPrimary, _configuration, _assets,
                        new PanoramaDisplay(display, _layout.VirtualBounds, _sessionId), SharedEnvironment, CloseAll);
                    window.FormClosed += WindowClosed;
                    _windows.Add(window);
                }
                _remaining = _windows.Count;
                foreach (var window in _windows) window.Show();
                System.Windows.Forms.Cursor.Hide();
                _cursorHidden = true;
                _windows[0].Activate();
                // Ignore the launch keystroke that can arrive just after the new windows gain focus.
                _armTimer.Start();
                Log.Info($"Reach panorama started {_windows.Count} windows with session {_sessionId}.");
            }
            catch (Exception ex)
            {
                ExitCode = 1;
                Log.Error("Could not start Reach panorama.", ex);
                CloseAll("startup failure");
                if (_remaining == 0) ExitThread();
            }
        }

        private void ArmInput(object? sender, EventArgs e)
        {
            _armTimer.Stop();
            if (_closing) return;
            try
            {
                _inputMonitor = new ReachWebInputMonitor(reason =>
                {
                    var window = _windows[0];
                    if (!window.IsDisposed)
                        window.BeginInvoke((Action)(() => CloseAll(reason)));
                });
                _inputMonitor.Start();
            }
            catch (Exception ex)
            {
                ExitCode = 1;
                Log.Error("Could not arm Reach panorama native input.", ex);
                CloseAll("input hook failure");
            }
        }

        private void CloseAll(string reason)
        {
            if (_closing) return;
            _closing = true;
            _armTimer.Stop();
            Log.Info($"Reach panorama closing all windows: {reason}.");
            _inputMonitor?.Dispose();
            foreach (var window in _windows)
                if (!window.IsDisposed) window.Close();
        }

        private void WindowClosed(object? sender, System.Windows.Forms.FormClosedEventArgs e)
        {
            if (sender is ReachWebWindow window && window.ExitCode != 0) ExitCode = window.ExitCode;
            if (!_closing) CloseAll("a monitor window closed");
            if (--_remaining == 0) ExitThread();
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                System.Windows.Forms.Application.Idle -= Start;
                _armTimer.Dispose();
                _inputMonitor?.Dispose();
                if (_cursorHidden) System.Windows.Forms.Cursor.Show();
                foreach (var window in _windows) window.Dispose();
            }
            base.Dispose(disposing);
        }
    }

    private sealed class ReachWebWindow : System.Windows.Forms.Form
    {
        private readonly string _dist;
        private readonly bool _fullscreen;
        private readonly bool _isPrimaryDisplay;
        private readonly IntPtr _previewParent;
        private readonly PanoramaDisplay? _panorama;
        private readonly Func<Task<CoreWebView2Environment>>? _sharedEnvironment;
        private readonly Action<string>? _closeGroup;
        private readonly ReachVisualConfiguration _visualConfiguration;
        private readonly IReadOnlyList<ReachAsset> _verifiedAssets;
        private readonly HashSet<string> _assetUrls = new(StringComparer.OrdinalIgnoreCase);
        private readonly WebView2 _webView = new() { Dock = System.Windows.Forms.DockStyle.Fill };
        private readonly HashSet<string> _blockedHosts = new(StringComparer.OrdinalIgnoreCase);
        private ReachWebInputMonitor? _inputMonitor;
        private readonly System.Windows.Forms.Timer? _previewTimer;
        private bool _closingAfterCheckpoint;
        private bool _shutdownRequested;
        private bool _cursorHidden;
        private bool _previewHandleDestroyedLogged;

        public int ExitCode { get; private set; }

        public ReachWebWindow(string dist, bool fullscreen, bool isPrimaryDisplay, ReachVisualConfiguration visualConfiguration,
            IReadOnlyList<ReachAsset> verifiedAssets, PanoramaDisplay? panorama = null,
            Func<Task<CoreWebView2Environment>>? sharedEnvironment = null, Action<string>? closeGroup = null,
            IntPtr previewParent = default)
        {
            _dist = dist;
            _fullscreen = fullscreen;
            _isPrimaryDisplay = isPrimaryDisplay;
            _previewParent = previewParent;
            _panorama = panorama;
            _sharedEnvironment = sharedEnvironment;
            _closeGroup = closeGroup;
            _visualConfiguration = visualConfiguration;
            _verifiedAssets = verifiedAssets;
            foreach (var asset in verifiedAssets)
                _assetUrls.Add($"https://{AssetHost}/{asset.LocalRelativePath}");
            Text = "The Reach - WebView2 prototype";
            BackColor = System.Drawing.Color.Black;
            StartPosition = System.Windows.Forms.FormStartPosition.Manual;
            if (fullscreen)
            {
                var bounds = panorama?.Monitor.Bounds;
                if (bounds is null)
                {
                    var monitor = Win32.MonitorFromPoint(new Win32.Point(0, 0), Win32.MonitorDefaultToPrimary);
                    var info = new Win32.MonitorInfoEx { Size = Marshal.SizeOf<Win32.MonitorInfoEx>(), DeviceName = string.Empty };
                    if (monitor == IntPtr.Zero || !Win32.GetMonitorInfoW(monitor, ref info))
                        throw new InvalidOperationException("Could not determine the primary monitor bounds.");
                    bounds = new PixelRect(info.Monitor.Left, info.Monitor.Top, info.Monitor.Width, info.Monitor.Height);
                }
                FormBorderStyle = System.Windows.Forms.FormBorderStyle.None;
                ShowInTaskbar = false;
                TopMost = true;
                Bounds = new System.Drawing.Rectangle(bounds.Value.X, bounds.Value.Y,
                    bounds.Value.Width, bounds.Value.Height);
                Log.Info($"Reach WebView2 fullscreen bounds: {Bounds.X},{Bounds.Y} {Bounds.Width}x{Bounds.Height}; primary={_isPrimaryDisplay}.");
            }
            else if (_previewParent != IntPtr.Zero)
            {
                FormBorderStyle = System.Windows.Forms.FormBorderStyle.None;
                ShowInTaskbar = false;
                ClientSize = new System.Drawing.Size(1, 1);
                _previewTimer = new System.Windows.Forms.Timer { Interval = 100 };
                _previewTimer.Tick += (_, _) => SyncPreview();
            }
            else
            {
                ClientSize = new System.Drawing.Size(1280, 720);
                var area = System.Windows.Forms.Screen.PrimaryScreen?.WorkingArea;
                if (area is { } bounds)
                    Location = new System.Drawing.Point(bounds.Left + (bounds.Width - Width) / 2,
                        bounds.Top + (bounds.Height - Height) / 2);
            }
            Controls.Add(_webView);
        }

        public bool TryAttachPreview()
        {
            if (_previewParent == IntPtr.Zero || !Win32.IsWindow(_previewParent)
                || !Win32.GetClientRect(_previewParent, out _))
            {
                Log.Warn("Reach preview parent HWND was destroyed before attachment.");
                return false;
            }

            var child = Handle;
            var style = Win32.GetWindowLongPtr(child, Win32.GwlStyle).ToInt64();
            style &= ~(Win32.WsPopup | Win32.WsCaption | Win32.WsThickFrame | Win32.WsSysMenu |
                       Win32.WsMinimizeBox | Win32.WsMaximizeBox | Win32.WsBorder | Win32.WsDlgFrame);
            style |= Win32.WsChild | Win32.WsVisible;
            Win32.SetWindowLongPtr(child, Win32.GwlStyle, new IntPtr(style));

            var exStyle = Win32.GetWindowLongPtr(child, Win32.GwlExStyle).ToInt64();
            exStyle &= ~(Win32.WsExAppWindow | Win32.WsExTopmost);
            exStyle |= Win32.WsExToolWindow;
            Win32.SetWindowLongPtr(child, Win32.GwlExStyle, new IntPtr(exStyle));
            Win32.SetParent(child, _previewParent);
            if (Win32.GetParent(child) != _previewParent)
            {
                Log.Error($"Reach preview could not attach child HWND {child} to parent {_previewParent}.");
                return false;
            }
            SyncPreview();
            Log.Info($"Reach WebView2 preview child HWND {child} attached to parent {_previewParent}.");
            return true;
        }

        private void SyncPreview()
        {
            if (_previewParent == IntPtr.Zero || IsDisposed) return;
            if (!Win32.IsWindow(_previewParent) || !Win32.GetClientRect(_previewParent, out var rect))
            {
                Log.Info("Reach preview parent closed; stopping WebView2.");
                Close();
                return;
            }
            var width = Math.Max(rect.Width, 1);
            var height = Math.Max(rect.Height, 1);
            if (Width != width || Height != height)
            {
                Win32.SetWindowPos(Handle, IntPtr.Zero, 0, 0, width, height,
                    Win32.SwpNoZOrder | Win32.SwpNoActivate | Win32.SwpFrameChanged);
                Log.Info($"Reach preview resized to {width}x{height}.");
            }
        }

        protected override async void OnShown(EventArgs e)
        {
            base.OnShown(e);
            try
            {
                if (_previewParent != IntPtr.Zero)
                {
                    if (!Win32.IsWindow(_previewParent)) { Close(); return; }
                    _previewTimer!.Start();
                    SyncPreview();
                }
                if (_fullscreen && _panorama is null)
                {
                    _inputMonitor = new ReachWebInputMonitor(reason =>
                    {
                        if (!IsDisposed)
                            BeginInvoke((Action)(() => { Log.Info($"Reach WebView2 exit: {reason}."); Close(); }));
                    });
                    _inputMonitor.Start();
                    System.Windows.Forms.Cursor.Hide();
                    _cursorHidden = true;
                    Activate();
                }
                var profile = Path.Combine(
                    Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
                    "TheReachScreensaver", _previewParent == IntPtr.Zero ? "WebView2Prototype" : "WebView2Preview");
                if (_previewParent != IntPtr.Zero)
                    Log.Info($"Reach preview uses isolated WebView2 profile {profile}.");
                Directory.CreateDirectory(profile);
                var options = new CoreWebView2EnvironmentOptions("--no-proxy-server");
                var environment = _sharedEnvironment is null
                    ? await CoreWebView2Environment.CreateAsync(userDataFolder: profile, options: options)
                    : await _sharedEnvironment();
                if (_shutdownRequested || IsDisposed) return;
                await _webView.EnsureCoreWebView2Async(environment);
                if (_shutdownRequested || IsDisposed) return;

                var core = _webView.CoreWebView2;
                core.Settings.AreDefaultContextMenusEnabled = false;
                core.Settings.AreDevToolsEnabled = false;
                core.Settings.AreBrowserAcceleratorKeysEnabled = false;
                core.Settings.IsZoomControlEnabled = false;
                core.Settings.IsStatusBarEnabled = false;
                core.Settings.AreHostObjectsAllowed = false;
                core.Settings.IsWebMessageEnabled = false;
                core.NewWindowRequested += (_, args) => args.Handled = true;
                core.NavigationStarting += (_, args) =>
                {
                    Log.Info($"Web navigation starting: {args.Uri}");
                    if (!IsLocalUrl(args.Uri))
                    {
                        Log.Warn($"Blocked external navigation: {args.Uri}");
                        args.Cancel = true;
                    }
                };
                core.WebResourceResponseReceived += (_, args) =>
                {
                    if (args.Response.StatusCode >= 400)
                        Log.Error($"Web asset HTTP {args.Response.StatusCode}: {args.Request.Uri}");
                    else if (IsLocalUrl(args.Request.Uri) || _assetUrls.Contains(args.Request.Uri))
                        Log.Info($"Local asset HTTP {args.Response.StatusCode}: {args.Request.Uri}");
                };
                core.NavigationCompleted += async (_, args) =>
                {
                    if (args.IsSuccess) Log.Info("Reach web navigation completed.");
                    else
                    {
                        Log.Error($"Reach web navigation failed: {args.WebErrorStatus}");
                        _closeGroup?.Invoke("web navigation failed");
                    }
                    if (args.IsSuccess && _panorama is not null)
                    {
                        try
                        {
                            var received = await core.ExecuteScriptAsync("window.__reachPanorama ?? null");
                            Log.Info($"Reach panorama {_panorama.Monitor.Name} received configuration: {received}");
                            if (received == "null") throw new InvalidOperationException("Panorama configuration was not injected.");
                            await Task.Delay(5000);
                            if (!_shutdownRequested)
                            {
                                var status = await core.ExecuteScriptAsync("window.__reachPanoramaStatus?.() ?? null");
                                Log.Info($"Reach panorama {_panorama.Monitor.Name} status: {status}");
                                using var state = JsonDocument.Parse(status);
                                var view = state.RootElement.GetProperty("viewOffset");
                                var monitor = _panorama.Monitor.Bounds;
                                var desktop = _panorama.VirtualBounds;
                                if (!view.GetProperty("enabled").GetBoolean()
                                    || view.GetProperty("fullWidth").GetInt32() != desktop.Width
                                    || view.GetProperty("fullHeight").GetInt32() != desktop.Height
                                    || view.GetProperty("offsetX").GetInt32() != monitor.X - desktop.X
                                    || view.GetProperty("offsetY").GetInt32() != monitor.Y - desktop.Y
                                    || view.GetProperty("width").GetInt32() != monitor.Width
                                    || view.GetProperty("height").GetInt32() != monitor.Height
                                    || state.RootElement.GetProperty("primary").GetBoolean() != _isPrimaryDisplay
                                    || state.RootElement.GetProperty("countdown").GetBoolean() != (_isPrimaryDisplay && _visualConfiguration.CountdownEnabled))
                                    throw new InvalidOperationException("The web camera did not apply its assigned panorama slice.");
                                if (Environment.GetEnvironmentVariable("REACH_WEB_VERIFY_PANORAMA") == "1")
                                {
                                    var captures = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
                                        "TheReachScreensaver", "logs", "panorama-captures");
                                    Directory.CreateDirectory(captures);
                                    var name = _panorama.Monitor.Name.Replace('\\', '_').Replace('.', '_');
                                    var file = Path.Combine(captures, $"{_panorama.SessionId}-{name}.png");
                                    await using var image = File.Create(file);
                                    await core.CapturePreviewAsync(CoreWebView2CapturePreviewImageFormat.Png, image);
                                    Log.Info($"Reach panorama {_panorama.Monitor.Name} captured: {file}.");
                                }
                            }
                        }
                        catch (Exception) when (_shutdownRequested || IsDisposed)
                        {
                            Log.Info("Reach panorama status probe stopped during shutdown.");
                        }
                        catch (Exception ex)
                        {
                            ExitCode = 1;
                            Log.Error($"Reach panorama {_panorama.Monitor.Name} validation failed.", ex);
                            _closeGroup?.Invoke("panorama validation failed");
                        }
                    }
                    if (args.IsSuccess && _previewParent != IntPtr.Zero
                        && Environment.GetEnvironmentVariable("REACH_WEB_VERIFY_PREVIEW") == "1")
                    {
                        try
                        {
                            await Task.Delay(3000);
                            if (!_shutdownRequested && !IsDisposed)
                            {
                                var status = await core.ExecuteScriptAsync("JSON.stringify({canvas: [document.querySelector('canvas')?.width, document.querySelector('canvas')?.height], countdown: !!document.getElementById('reach-countdown'), panorama: !!window.__reachPanorama, journey: !!localStorage.getItem('the-reach:journey:v1')})");
                                Log.Info($"Reach preview rendered status: {status}");
                                var captures = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
                                    "TheReachScreensaver", "logs", "preview-captures");
                                Directory.CreateDirectory(captures);
                                var file = Path.Combine(captures, $"preview-{DateTime.Now:yyyyMMdd-HHmmss}.png");
                                await using var image = File.Create(file);
                                await core.CapturePreviewAsync(CoreWebView2CapturePreviewImageFormat.Png, image);
                                Log.Info($"Reach preview captured: {file}.");
                            }
                        }
                        catch (Exception ex) { Log.Warn($"Reach preview validation unavailable: {ex.Message}"); }
                    }
                    if (args.IsSuccess && Environment.GetEnvironmentVariable("REACH_WEB_VERIFY_CHECKPOINT") == "1")
                    {
                        try { await LogCheckpointFingerprint(core, "at document start", "window.__reachCheckpointAtStart"); }
                        catch (Exception ex) { Log.Warn($"Could not inspect saved Reach checkpoint: {ex.Message}"); }
                    }
                    if (args.IsSuccess && Environment.GetEnvironmentVariable("REACH_WEB_VERIFY_ASSETS") == "1")
                    {
                        try { await ProbeLocalAssets(core); }
                        catch (Exception ex) { Log.Error("Local asset probe failed.", ex); }
                    }
                };
                core.ProcessFailed += (_, args) =>
                {
                    Log.Error($"WebView2 process failed: {args.ProcessFailedKind}");
                    _closeGroup?.Invoke("WebView2 process failed");
                };
                core.AddWebResourceRequestedFilter("*", CoreWebView2WebResourceContext.All);
                core.WebResourceRequested += (_, args) =>
                {
                    if (IsLocalUrl(args.Request.Uri) || _assetUrls.Contains(args.Request.Uri)) return;
                    var uri = new Uri(args.Request.Uri);
                    if (_blockedHosts.Add(uri.Host))
                        Log.Warn($"Blocked external web resource host: {uri.Host}");
                    args.Response = environment.CreateWebResourceResponse(null, 403, "External resources disabled", "Content-Type: text/plain");
                };

                await core.AddScriptToExecuteOnDocumentCreatedAsync("""
                    (() => {
                      for (const type of ['pointerdown', 'pointerup', 'pointermove', 'click', 'dblclick',
                                          'wheel', 'touchstart', 'touchmove', 'touchend', 'keydown', 'keyup']) {
                        window.addEventListener(type, e => { e.preventDefault(); e.stopImmediatePropagation(); },
                          { capture: true, passive: false });
                      }
                      const hideCursor = () => {
                        const style = document.createElement('style');
                        style.textContent = '* { cursor: none !important; }';
                        (document.head || document.documentElement).appendChild(style);
                      };
                      if (document.readyState === 'loading')
                        document.addEventListener('DOMContentLoaded', hideCursor, { once: true });
                      else hideCursor();
                    })();
                    """);
                if (Environment.GetEnvironmentVariable("REACH_WEB_VERIFY_CHECKPOINT") == "1")
                    await core.AddScriptToExecuteOnDocumentCreatedAsync("window.__reachCheckpointAtStart = localStorage.getItem('the-reach:journey:v1');");
                if (_verifiedAssets.Count > 0)
                    core.SetVirtualHostNameToFolderMapping(AssetHost, _visualConfiguration.AssetDirectory,
                        CoreWebView2HostResourceAccessKind.Allow);
                var visualContract = new
                {
                    schema = 1,
                    mode = _visualConfiguration.Mode.ToString().ToLowerInvariant(),
                    assets = _verifiedAssets.Select(asset => new
                    {
                        assetId = asset.AssetId,
                        destinationBodyId = asset.DestinationBodyId,
                        url = $"https://{AssetHost}/{asset.LocalRelativePath}",
                        sourceAttribution = asset.SourceAttribution,
                        width = asset.Width,
                        height = asset.Height
                    }).ToArray()
                };
                await core.AddScriptToExecuteOnDocumentCreatedAsync(
                    "Object.defineProperty(window, '__reachVisualAssets', { value: Object.freeze(" +
                    JsonSerializer.Serialize(visualContract) + "), configurable: false });");
                if (_shutdownRequested || IsDisposed) return;
                var countdownContract = new
                {
                    enabled = _visualConfiguration.CountdownEnabled,
                    primaryDisplay = _isPrimaryDisplay,
                    position = _visualConfiguration.CountdownPosition
                };
                await core.AddScriptToExecuteOnDocumentCreatedAsync(
                    "Object.defineProperty(window, '__reachCountdown', { value: Object.freeze(" +
                    JsonSerializer.Serialize(countdownContract) + "), configurable: false });");
                if (_shutdownRequested || IsDisposed) return;
                if (_panorama is { } panorama)
                {
                    var monitor = panorama.Monitor.Bounds;
                    var desktop = panorama.VirtualBounds;
                    var renderContract = new
                    {
                        schema = 1,
                        sessionId = panorama.SessionId,
                        isPrimaryDisplay = _isPrimaryDisplay,
                        monitorBounds = new { x = monitor.X, y = monitor.Y, width = monitor.Width, height = monitor.Height },
                        virtualDesktopBounds = new { x = desktop.X, y = desktop.Y, width = desktop.Width, height = desktop.Height },
                        viewportOffsetX = panorama.ViewportOffsetX,
                        viewportOffsetY = panorama.ViewportOffsetY,
                        viewportWidth = monitor.Width,
                        viewportHeight = monitor.Height,
                        totalPanoramaWidth = desktop.Width,
                        totalPanoramaHeight = desktop.Height,
                        devicePixelRatio = panorama.Monitor.DpiX / 96f
                    };
                    await core.AddScriptToExecuteOnDocumentCreatedAsync(
                        "Object.defineProperty(window, '__reachPanorama', { value: Object.freeze(" +
                        JsonSerializer.Serialize(renderContract) + "), configurable: false }); " +
                        "const markPanorama = () => document.documentElement.classList.add('reach-panorama'); " +
                        "if (document.documentElement) markPanorama(); " +
                        "else document.addEventListener('DOMContentLoaded', markPanorama, { once: true });");
                    if (_shutdownRequested || IsDisposed) return;
                }
                core.SetVirtualHostNameToFolderMapping(Host, _dist, CoreWebView2HostResourceAccessKind.DenyCors);
                var url = _panorama is null ? StartUrl : StartUrl + "&panorama=1";
                core.Navigate(url);
                Log.Info($"Navigating to {url} using local dist {_dist}.");
            }
            catch (Exception) when (_shutdownRequested || IsDisposed)
            {
                Log.Info("Reach WebView2 initialization stopped during shutdown.");
            }
            catch (Exception ex)
            {
                ExitCode = 1;
                Log.Error("Could not start Reach WebView2 prototype.", ex);
                ConfigurationDialog.ShowError("Could not start Reach WebView2 prototype.\n\n" + ex.Message);
                Close();
            }
        }

        private static bool IsLocalUrl(string value) =>
            Uri.TryCreate(value, UriKind.Absolute, out var uri) &&
            uri.Scheme == Uri.UriSchemeHttps &&
            string.Equals(uri.Host, Host, StringComparison.OrdinalIgnoreCase);

        private async Task ProbeLocalAssets(CoreWebView2 core)
        {
            var extensions = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
                { ".js", ".wasm", ".bin", ".json", ".glb", ".jpg", ".png" };
            var paths = Directory.EnumerateFiles(_dist, "*", SearchOption.AllDirectories)
                .Where(file => extensions.Contains(Path.GetExtension(file)))
                .Select(file => "/" + Path.GetRelativePath(_dist, file).Replace('\\', '/'))
                .ToArray();
            var script = "for (const path of " + JsonSerializer.Serialize(paths) +
                ") fetch(path, {method: 'HEAD'}).catch(e => console.error('Local asset probe:', path, e));";
            await core.ExecuteScriptAsync(script);
            Log.Info($"Started {paths.Length} local HTTP HEAD asset probes; responses are logged individually.");
        }

        private static async Task LogCheckpointFingerprint(CoreWebView2 core, string stage, string expression)
        {
            var raw = JsonSerializer.Deserialize<string>(await core.ExecuteScriptAsync(expression));
            if (raw is null)
            {
                Log.Info($"Reach checkpoint {stage}: absent.");
                return;
            }
            var fingerprint = Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(raw)))[..16];
            using var document = JsonDocument.Parse(raw);
            var state = document.RootElement;
            var phase = state.GetProperty("phase");
            Log.Info($"Reach checkpoint {stage}: {raw.Length} characters, SHA256 prefix {fingerprint}, " +
                $"destination={state.GetProperty("destinationId").GetString()}, route={state.GetProperty("routeIndex").GetInt32()}, " +
                $"phase={phase.GetProperty("kind").GetString()}, elapsed={phase.GetProperty("elapsed").GetDouble():0.###} s.");
        }

        protected override async void OnFormClosing(System.Windows.Forms.FormClosingEventArgs e)
        {
            _shutdownRequested = true;
            if (_isPrimaryDisplay && !_closingAfterCheckpoint && _webView.CoreWebView2 is { } core)
            {
                e.Cancel = true;
                _closingAfterCheckpoint = true;
                try
                {
                    var checkpoint = core.ExecuteScriptAsync("window.dispatchEvent(new Event('pagehide'))");
                    if (await Task.WhenAny(checkpoint, Task.Delay(1000)) == checkpoint)
                    {
                        await checkpoint;
                        Log.Info("Reach pagehide checkpoint event delivered before close.");
                        if (Environment.GetEnvironmentVariable("REACH_WEB_VERIFY_CHECKPOINT") == "1")
                            await LogCheckpointFingerprint(core, "after pagehide", "localStorage.getItem('the-reach:journey:v1')");
                    }
                    else Log.Warn("Reach pagehide checkpoint event timed out during close.");
                }
                catch (Exception ex) { Log.Warn($"Could not deliver Reach pagehide checkpoint: {ex.Message}"); }
                Close();
                return;
            }
            base.OnFormClosing(e);
        }

        protected override void OnFormClosed(System.Windows.Forms.FormClosedEventArgs e)
        {
            _previewTimer?.Stop();
            _previewTimer?.Dispose();
            _inputMonitor?.Dispose();
            if (_cursorHidden) System.Windows.Forms.Cursor.Show();
            _webView.Dispose();
            Log.Info("Reach WebView2 window closed.");
            base.OnFormClosed(e);
        }

        protected override void OnHandleDestroyed(EventArgs e)
        {
            if (_previewParent != IntPtr.Zero && !RecreatingHandle)
            {
                _shutdownRequested = true;
                _previewTimer?.Stop();
                if (!_previewHandleDestroyedLogged)
                {
                    _previewHandleDestroyedLogged = true;
                    Log.Info("Reach WebView2 preview handle destroyed.");
                }
            }
            base.OnHandleDestroyed(e);
        }
    }
}
