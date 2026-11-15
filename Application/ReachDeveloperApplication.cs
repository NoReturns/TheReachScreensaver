#if REACH_DEVELOPER_BUILD
using System.Globalization;
using System.Text.Json;
using System.Security.Cryptography;
using System.Runtime.InteropServices;
using Microsoft.Web.WebView2.Core;
using Microsoft.Web.WebView2.WinForms;
using TheReachScreensaver.Diagnostics;
using TheReachScreensaver.Display;
using TheReachScreensaver.Native;

namespace TheReachScreensaver.Application;

// This entry point intentionally does not call ReachWebApplication.Run. That path
// resolves installed assets, settings, reset markers, and production profiles.
internal static class ReachDeveloperApplication
{
    private const string Host = "reach.example";
    private const string Url = "https://reach.example/index.html?reach=1&panorama=1&reachPreview=destination-jump&reachTestStart=departure&reachTestFrom=0&reachTestTo=1&reachTestCruise=30";
    private const string FixtureUrl = "https://reach.example/development/cylindrical-stage-a1/index.html";
    [DllImport("kernel32.dll")]
    private static extern uint SetThreadExecutionState(uint flags);
    private static readonly HashSet<string> FixtureIds = new(StringComparer.Ordinal)
    {
        "sphere-left", "sphere-seam", "earth-seam", "saturn-sector", "galaxy-sector",
        "hole-240-left", "hole-26-left", "hole-26-away", "hole-primary-right",
        "hole-26-primary-right", "hole-26-sector", "galaxy-left-seam", "roll-15", "rearward"
    };

    public static int Run(string[] args)
    {
        try
        {
            if (args.Length < 4 || args.Length > 26 || args.Length % 2 != 0
                || args[0] != "--reach-dev-projection"
                || args[1] is not ("rectilinear" or "cylindrical") || args[2] != "--sandbox-root")
                throw new ArgumentException("Use --reach-dev-projection rectilinear|cylindrical --sandbox-root <absolute run directory>.");
            var smokeSeconds = 0;
            var injectPrimaryFailure = false;
            string? fixtureId = null;
            var post = "final";
            int? guard = null;
            var bloomMips = "native";
            var motion = false;
            double? frozenYaw = null;
            var forensics = false;
            var sourceProbe = false;
            var sectorParity = false;
            var sectorMotion = false;
            var negativeProbe = false;
            var zeroTarget = false;
            var r13Measure = false;
            var r14Seconds = 0;
            string? continuous = null;
            for (var i = 4; i < args.Length; i += 2)
            {
                if (args[i] == "--reach-dev-smoke-seconds" && smokeSeconds == 0
                    && int.TryParse(args[i + 1], out var seconds) && seconds is >= 5 and <= 120)
                    smokeSeconds = seconds;
                else if (args[i] == "--reach-dev-fault" && args[i + 1] == "primary-init"
                    && !injectPrimaryFailure)
                    injectPrimaryFailure = true;
                else if (args[i] == "--reach-dev-fixture" && fixtureId is null
                    && FixtureIds.Contains(args[i + 1]))
                    fixtureId = args[i + 1];
                else if (args[i] == "--reach-dev-post" && post == "final"
                    && args[i + 1] is "raw" or "bloom" or "output")
                    post = args[i + 1];
                else if (args[i] == "--reach-dev-guard" && guard is null
                    && int.TryParse(args[i + 1], out var requestedGuard)
                    && requestedGuard is 0 or 64 or 128 or 256 or 320 or 384 or 448 or 512)
                    guard = requestedGuard;
                else if (args[i] == "--reach-dev-bloom-mips" && bloomMips == "native"
                    && args[i + 1] is "guard128" or "grid")
                    bloomMips = args[i + 1];
                else if (args[i] == "--reach-dev-motion" && args[i + 1] == "yaw"
                    && !motion)
                    motion = true;
                else if (args[i] == "--reach-dev-yaw" && frozenYaw is null
                    && double.TryParse(args[i + 1], NumberStyles.Float,
                        CultureInfo.InvariantCulture, out var yaw) && double.IsFinite(yaw)
                    && yaw is >= -12 and <= 12)
                    frozenYaw = yaw;
                else if (args[i] == "--reach-dev-forensics" && args[i + 1] == "1"
                    && !forensics)
                    forensics = true;
                else if (args[i] == "--reach-dev-source-probe" && args[i + 1] == "1"
                    && !sourceProbe)
                    sourceProbe = true;
                else if (args[i] == "--reach-dev-sector-parity" && args[i + 1] == "1"
                    && !sectorParity)
                    sectorParity = true;
                else if (args[i] == "--reach-dev-sector-motion" && args[i + 1] == "1"
                    && !sectorMotion)
                    sectorMotion = true;
                else if (args[i] == "--reach-dev-negative-probe" && args[i + 1] == "1"
                    && !negativeProbe)
                    negativeProbe = true;
                else if (args[i] == "--reach-dev-zero-target" && args[i + 1] == "1"
                    && !zeroTarget)
                    zeroTarget = true;
                else if (args[i] == "--reach-dev-r13-measure" && args[i + 1] == "1"
                    && !r13Measure)
                    r13Measure = true;
                else if (args[i] == "--reach-dev-r14-seconds" && r14Seconds == 0
                    && int.TryParse(args[i + 1], out var duration) && duration is >= 10 and <= 240)
                    r14Seconds = duration;
                else if (args[i] == "--reach-dev-continuous" && continuous is null
                    && args[i + 1] is "capture" or "baseline" or "async" or "r17")
                    continuous = args[i + 1];
                else throw new ArgumentException("Invalid developer smoke option.");
            }
            if (fixtureId is null && post != "final")
                throw new ArgumentException("Postprocessing diagnostics require a native fixture.");
            if (guard is not null && (fixtureId is null || args[1] != "cylindrical"))
                throw new ArgumentException("Bloom guard diagnostics require a cylindrical native fixture.");
            if (bloomMips != "native" && (fixtureId is null || args[1] != "cylindrical"
                || (bloomMips == "guard128" && (guard ?? 128) != 512)))
                throw new ArgumentException("Bloom mip diagnostic requires a 512-pixel cylindrical fixture.");
            if (motion && (fixtureId != "hole-26-left" || args[1] != "cylindrical"))
                throw new ArgumentException("Yaw sweep requires the cylindrical 26-radii fixture.");
            if (frozenYaw is not null && (fixtureId != "hole-26-left" || motion))
                throw new ArgumentException("Frozen yaw requires the 26-radii fixture without a motion sweep.");
            if (forensics && (fixtureId != "hole-26-left" || args[1] != "cylindrical"
                || post != "final" || motion))
                throw new ArgumentException("Forensics require a frozen final-pass cylindrical 26-radii fixture.");
            if (sourceProbe && (!forensics || frozenYaw != 6.815 || guard != 512
                || bloomMips != "native"))
                throw new ArgumentException("Source probe requires frozen yaw 6.815, native 512 guard and forensics.");
            if (sectorParity && (sourceProbe || !forensics || frozenYaw != 6.815
                || guard != 512 || bloomMips != "native" || post != "final"))
                throw new ArgumentException("Sector parity requires frozen yaw 6.815, native 512 guard and forensics.");
            if (sectorMotion && (!sectorParity || continuous is not null || motion))
                throw new ArgumentException("Sector motion requires the isolated R9 parity fixture.");
            if (negativeProbe && (sourceProbe || sectorParity || sectorMotion || !forensics
                || frozenYaw != -6 || guard != 512 || bloomMips != "native" || post != "final"))
                throw new ArgumentException("Negative probe requires the isolated -6 final fixture.");
            if (zeroTarget && (sourceProbe || sectorParity || sectorMotion || negativeProbe
                || continuous is not null || motion || !forensics || frozenYaw != 6.815
                || guard != 512 || bloomMips != "native" || post != "final"))
                throw new ArgumentException("Zero-target pilot requires the isolated 6.815 final fixture.");
            if (continuous is not null && (fixtureId is not ("hole-26-left" or "galaxy-sector" or "galaxy-left-seam")
                || (continuous != "r17" && fixtureId != "hole-26-left") || post != "final"
                || motion || frozenYaw is not null || forensics || bloomMips != "native"
                || (args[1] == "cylindrical" && guard != 512)))
                throw new ArgumentException("Continuous pilot requires the final 26-radii fixture and native 512 guard.");
            if (r13Measure && (fixtureId is null || post != "final" || forensics || sourceProbe
                || sectorParity || sectorMotion || negativeProbe || zeroTarget || continuous is not null))
                throw new ArgumentException("R13 measurement requires an unchanged final-pass native fixture.");
            if (r14Seconds > 0 && (fixtureId is not ("hole-26-left" or "galaxy-left-seam")
                || post != "final" || motion || forensics || sourceProbe || sectorParity
                || sectorMotion || negativeProbe || zeroTarget || continuous is not null || r13Measure))
                throw new ArgumentException("R14 measurement requires an isolated final-pass native fixture.");
            var root = ValidateRoot(args[3]);
            var host = CheckedPath(root, "host");
            if (!string.Equals(Path.GetFullPath(Environment.ProcessPath!),
                    Path.Combine(host, "TheReachDeveloper.exe"), StringComparison.OrdinalIgnoreCase))
                throw new InvalidOperationException("The developer executable must run from sandbox/host.");
            var dist = CheckedPath(root, "dist");
            if (!File.Exists(CheckedPath(root, "dist", "index.html")))
            {
                if (fixtureId is null || !File.Exists(CheckedPath(root, "dist", "development",
                    "cylindrical-stage-a1", "index.html")))
                    throw new FileNotFoundException("The staged developer payload is missing.");
            }
            else if (fixtureId is not null && !File.Exists(CheckedPath(root, "dist", "development",
                "cylindrical-stage-a1", "index.html")))
                throw new FileNotFoundException("The staged native fixture payload is missing.");
            var marker = CheckedPath(root, "dist", "reach-developer-payload.txt");
            if (!File.Exists(marker)
                || File.ReadAllText(marker).Trim() != "VITE_REACH_CYLINDRICAL_DEV=1;VITE_REACH_DEV_TEST=1")
                throw new InvalidOperationException("The staged payload lacks the developer build marker.");
            var profile = CheckedPath(root, "profile");
            var logs = CheckedPath(root, "logs");
            var captures = CheckedPath(root, "captures");
            var metrics = CheckedPath(root, "metrics");
            var temporary = CheckedPath(root, "temp");
            Directory.CreateDirectory(profile);
            Directory.CreateDirectory(logs);
            Directory.CreateDirectory(captures);
            Directory.CreateDirectory(metrics);
            Directory.CreateDirectory(temporary);
            using (new FileStream(Path.Combine(root, "invoked.lock"), FileMode.CreateNew,
                FileAccess.Write, FileShare.None)) { }
            Environment.SetEnvironmentVariable("TEMP", temporary);
            Environment.SetEnvironmentVariable("TMP", temporary);
            Log.Initialize(root);
            Log.Info($"Isolated developer run: {root}; executable={Environment.ProcessPath}; dist={dist}; profile={profile}.");
            Win32.SetProcessDpiAwarenessContext(Win32.DpiAwarenessContextPerMonitorAwareV2);
            System.Windows.Forms.Application.EnableVisualStyles();
            var mode = args[1];
            for (var attempt = 0; attempt < 2; attempt++)
            {
                var layout = DisplayLayout.Query();
                ValidateLayout(layout);
                var session = Guid.NewGuid().ToString("N");
                Log.Info($"Developer group mode={mode}; session={session}; monitors={layout.Displays.Count}.");
                using var group = new Group(dist, profile, captures, metrics, layout, session, mode,
                    smokeSeconds, injectPrimaryFailure && mode == "cylindrical", fixtureId, post,
                    guard ?? (fixtureId is not null && mode == "cylindrical" ? 512 : 128),
                    bloomMips, motion, frozenYaw, forensics, sourceProbe, sectorParity,
                    sectorMotion, negativeProbe, zeroTarget, continuous, r13Measure, r14Seconds);
                System.Windows.Forms.Application.Run(group);
                if (group.Result == GroupResult.Normal) return 0;
                if (fixtureId is not null) return 1;
                if (mode != "cylindrical") return 1;
                Log.Warn("Cylindrical group failed; restarting the complete group in rectilinear mode.");
                mode = "rectilinear";
            }
            return 1;
        }
        catch (Exception ex)
        {
            // No production logging initialization is attempted on preflight failure.
            Console.Error.WriteLine("Developer lane rejected launch: " + ex);
            Log.Error("Developer lane rejected launch.", ex);
            return 1;
        }
    }

    private static string ValidateRoot(string value)
    {
        if (!Path.IsPathFullyQualified(value)) throw new ArgumentException("Sandbox root must be absolute.");
        var root = Path.GetFullPath(value).TrimEnd(Path.DirectorySeparatorChar);
        if (!Guid.TryParseExact(Path.GetFileName(root).Replace("run-", ""), "N", out _)
            || !Path.GetFileName(root).StartsWith("run-", StringComparison.Ordinal))
            throw new ArgumentException("Sandbox root must be a unique run-<32 hex digits> directory.");
        if (!Directory.Exists(root)) throw new DirectoryNotFoundException(root);
        CheckNoLinks(root);
        return root;
    }

    private static string CheckedPath(string root, params string[] parts)
    {
        var path = Path.GetFullPath(Path.Combine([root, .. parts]));
        if (!path.StartsWith(root + Path.DirectorySeparatorChar, StringComparison.OrdinalIgnoreCase))
            throw new InvalidOperationException("Sandbox path escaped its root.");
        CheckNoLinks(path);
        return path;
    }

    private static void CheckNoLinks(string path)
    {
        for (var item = new DirectoryInfo(path) as FileSystemInfo; item is not null;
             item = item is DirectoryInfo directory ? directory.Parent : null)
        {
            if (item.Exists && (item.Attributes & FileAttributes.ReparsePoint) != 0)
                throw new InvalidOperationException("Sandbox path crosses a reparse point: " + item.FullName);
        }
        if (!Directory.Exists(path)) return;
        var pending = new Stack<string>();
        pending.Push(path);
        while (pending.TryPop(out var directory))
            foreach (var entry in Directory.EnumerateFileSystemEntries(directory))
            {
                var attributes = File.GetAttributes(entry);
                if ((attributes & FileAttributes.ReparsePoint) != 0)
                    throw new InvalidOperationException("Sandbox contains a reparse point: " + entry);
                if ((attributes & FileAttributes.Directory) != 0) pending.Push(entry);
            }
    }

    private static void ValidateLayout(DisplayLayout layout)
    {
        if (layout.Displays.Count is < 1 or > 8 || layout.Displays.Any(d => d.Name == "fallback")
            || layout.Displays.Count(d => d.IsPrimary) != 1)
            throw new InvalidOperationException("Physical monitor enumeration failed.");
        foreach (var display in layout.Displays)
            if (display.Bounds.Width <= 0 || display.Bounds.Height <= 0
                || display.Bounds.Left < layout.VirtualBounds.Left || display.Bounds.Top < layout.VirtualBounds.Top
                || display.Bounds.Right > layout.VirtualBounds.Right || display.Bounds.Bottom > layout.VirtualBounds.Bottom
                || display.DpiX <= 0)
                throw new InvalidOperationException("Invalid physical monitor geometry.");
    }

    private enum GroupResult { Failed, Normal }

    private sealed class Group : System.Windows.Forms.ApplicationContext
    {
        private readonly string _dist, _profile, _captures, _metrics, _session, _mode;
        private readonly DisplayLayout _layout;
        private readonly int _smokeSeconds;
        private readonly bool _injectPrimaryFailure;
        private readonly string? _fixtureId;
        private readonly string _post;
        private readonly int _guard;
        private readonly string _bloomMips;
        private readonly bool _motion;
        private readonly double? _frozenYaw;
        private readonly bool _forensics;
        private readonly bool _sourceProbe;
        private readonly bool _sectorParity;
        private readonly bool _sectorMotion;
        private readonly bool _negativeProbe;
        private readonly bool _zeroTarget;
        private readonly string? _continuous;
        private readonly bool _r13Measure;
        private readonly int _r14Seconds;
        private readonly List<Window> _windows = [];
        private readonly System.Windows.Forms.Timer _watch = new() { Interval = 2000 };
        private CoreWebView2Environment? _environment;
        private ReachWebInputMonitor? _input;
        private bool _closing;
        private DateTime _deadlineUtc;
        public GroupResult Result { get; private set; } = GroupResult.Failed;

        public Group(string dist, string profile, string captures, string metrics,
            DisplayLayout layout, string session, string mode, int smokeSeconds, bool injectPrimaryFailure,
            string? fixtureId, string post, int guard, string bloomMips, bool motion,
            double? frozenYaw, bool forensics, bool sourceProbe, bool sectorParity,
            bool sectorMotion, bool negativeProbe, bool zeroTarget, string? continuous,
            bool r13Measure, int r14Seconds)
        {
            _dist = dist; _profile = profile; _captures = captures; _metrics = metrics;
            _layout = layout; _session = session; _mode = mode;
            _smokeSeconds = smokeSeconds; _injectPrimaryFailure = injectPrimaryFailure;
            _fixtureId = fixtureId; _post = post;
            _guard = guard;
            _bloomMips = bloomMips;
            _motion = motion;
            _frozenYaw = frozenYaw;
            _forensics = forensics;
            _sourceProbe = sourceProbe;
            _sectorParity = sectorParity;
            _sectorMotion = sectorMotion;
            _negativeProbe = negativeProbe;
            _zeroTarget = zeroTarget;
            _continuous = continuous;
            _r13Measure = r13Measure;
            _r14Seconds = r14Seconds;
            System.Windows.Forms.Application.Idle += Start;
            _watch.Tick += Watch;
        }

        private async void Start(object? sender, EventArgs e)
        {
            System.Windows.Forms.Application.Idle -= Start;
            try
            {
                _environment = await CoreWebView2Environment.CreateAsync(userDataFolder: _profile,
                    options: new CoreWebView2EnvironmentOptions("--no-proxy-server"));
                foreach (var display in _layout.Displays.OrderByDescending(d => d.IsPrimary))
                {
                    var window = new Window(display, _layout.VirtualBounds, _session, _mode, _dist, Fail,
                        _injectPrimaryFailure && display.IsPrimary, _fixtureId, _post, _guard,
                        _bloomMips, _frozenYaw, _forensics, _sourceProbe,
                        _sectorParity, _sectorMotion, _negativeProbe, _zeroTarget);
                    _windows.Add(window);
                    window.Show();
                }
                await Task.WhenAll(_windows.Select(w => w.Initialize(_environment)));
                if (_closing) return;
                foreach (var window in _windows) await window.WaitReady();
                if (_closing) return;
                if (_fixtureId is not null && _windows.Any(w => w.StateKey != _windows[0].StateKey))
                    throw new InvalidOperationException("Fixture state differs between displays.");
                foreach (var window in _windows) window.Reveal();
                Log.Info("Developer group readiness passed; all windows revealed.");
                if (_fixtureId is null)
                {
                    _input = new ReachWebInputMonitor(reason =>
                    {
                        if (_windows.Count > 0 && !_windows[0].IsDisposed)
                            _windows[0].BeginInvoke((Action)(() => Stop(reason, true)));
                    });
                    _input.Start();
                }
                if (_smokeSeconds > 0) _deadlineUtc = DateTime.UtcNow.AddSeconds(_smokeSeconds);
                _watch.Start();
                await Capture();
                if (_r13Measure && !_closing) await CaptureR13Measurement();
                if (_r14Seconds > 0 && !_closing) await CaptureR14Measurement();
                if (_forensics && !_closing) await CaptureForensics();
                if (_sourceProbe && !_closing) await CaptureSourceProbe();
                if (_negativeProbe && !_closing) await CaptureNegativeProbe();
                if (_sectorMotion && !_closing) await CaptureSectorMotion();
                else if (_sectorParity && !_closing) await CaptureSectorParity();
                if (_zeroTarget && !_closing) await CaptureZeroTarget();
                if (_motion && !_closing) await CaptureMotion();
                if (_continuous == "r17" && !_closing) await CaptureR17();
                else if (_continuous is not null && !_closing) await CaptureContinuous();
                if (_fixtureId is not null && !_closing) Stop("Matched fixture capture completed.", true);
            }
            catch (Exception ex) { Fail("Group startup failed: " + ex); }
        }

        private async Task Capture()
        {
            var files = new List<object>();
            var expectedState = _fixtureId is null ? null : _windows[0].StateKey;
            foreach (var window in _windows)
            {
                if (_fixtureId is not null)
                {
                    await window.CheckReady();
                    if (window.StateKey != expectedState)
                        throw new InvalidOperationException("Fixture state changed before capture.");
                }
                var name = Path.Combine(_captures, $"{_mode}-{_session}-{window.Display.Name.Replace('\\', '_').Replace('.', '_')}.png");
                await using (var file = new FileStream(name, FileMode.CreateNew, FileAccess.Write, FileShare.None))
                {
                    await window.CapturePreview(file);
                    await file.FlushAsync();
                }
                if (_fixtureId is not null)
                {
                    await window.CheckReady();
                    if (window.StateKey != expectedState)
                        throw new InvalidOperationException("Fixture state changed during capture.");
                    files.Add(new { frame = 1, display = window.Display.Name, path = name,
                        sha256 = Convert.ToHexString(SHA256.HashData(File.ReadAllBytes(name))) });
                }
                Log.Info("Captured developer display: " + name);
            }
            if (_fixtureId is not null)
            {
                if (_windows.Any(w => w.StateKey != expectedState))
                    throw new InvalidOperationException("Fixture group state changed after capture.");
                var repeats = new List<object>();
                if (_fixtureId == "hole-26-left" && _post == "final" && !_forensics)
                {
                    for (var frame = 2; frame <= 3; frame++)
                    {
                        foreach (var window in _windows) await window.RenderAgain(frame);
                        foreach (var window in _windows)
                        {
                            await window.CheckReady();
                            if (window.StateKey != expectedState)
                                throw new InvalidOperationException("Fixture state changed on repeated render.");
                            var name = Path.Combine(_captures,
                                $"{_mode}-{_session}-{window.Display.Name.Replace('\\', '_').Replace('.', '_')}-frame{frame}.png");
                            await using (var file = new FileStream(name, FileMode.CreateNew, FileAccess.Write, FileShare.None))
                            {
                                await window.CapturePreview(file);
                                await file.FlushAsync();
                            }
                            await window.CheckReady();
                            repeats.Add(new { frame, display = window.Display.Name, path = name,
                                sha256 = Convert.ToHexString(SHA256.HashData(File.ReadAllBytes(name))) });
                        }
                    }
                }
                var manifest = new { schema = 1, fixtureId = _fixtureId, stateRevision = 1,
                    mode = _mode, post = _post, guardPhysicalPixels = _guard,
                    bloomMipMode = _bloomMips, frozenYawDegrees = _frozenYaw,
                    forensics = _forensics, sessionId = _session,
                    stateKey = expectedState, layout = new { desktop = _layout.VirtualBounds,
                        displays = _layout.Displays.Select(d => new { d.Name, d.Bounds, d.DpiX, d.IsPrimary }) },
                    captures = files, repeatCaptures = repeats,
                    statuses = _windows.Select(w => w.LastStatus).ToArray() };
                File.WriteAllText(Path.Combine(_metrics, $"fixture-{_fixtureId}-{_mode}-{_post}-{_session}.json"),
                    JsonSerializer.Serialize(manifest));
                return;
            }
            await Task.Delay(4000);
            if (_closing) return;
            var process = System.Diagnostics.Process.GetCurrentProcess();
            var children = _environment?.GetProcessInfos().Select(info =>
            {
                try
                {
                    using var child = System.Diagnostics.Process.GetProcessById(info.ProcessId);
                    return new { pid = info.ProcessId, kind = info.Kind.ToString(),
                        privateBytes = child.PrivateMemorySize64, workingSet = child.WorkingSet64 };
                }
                catch { return new { pid = info.ProcessId, kind = info.Kind.ToString(),
                    privateBytes = -1L, workingSet = -1L }; }
            }).ToArray();
            var frames = new List<object>();
            foreach (var window in _windows) frames.Add(await window.FrameIntervalReport());
            var report = new { mode = _mode, sessionId = _session,
                processId = process.Id, privateBytes = process.PrivateMemorySize64,
                workingSet = process.WorkingSet64, webView2Processes = children,
                frameIntervals = frames, displays = _windows.Select(w => w.LastStatus).ToArray() };
            File.WriteAllText(Path.Combine(_metrics, $"{_mode}-{_session}.json"), JsonSerializer.Serialize(report));
        }

        private async Task CaptureR13Measurement()
        {
            var records = new List<object>();
            foreach (var window in _windows)
                records.Add(await window.MeasureR13());
            var process = System.Diagnostics.Process.GetCurrentProcess();
            var children = _environment?.GetProcessInfos().Select(info =>
            {
                try
                {
                    using var child = System.Diagnostics.Process.GetProcessById(info.ProcessId);
                    return new { pid = info.ProcessId, kind = info.Kind.ToString(),
                        privateBytes = child.PrivateMemorySize64, workingSet = child.WorkingSet64 };
                }
                catch { return new { pid = info.ProcessId, kind = info.Kind.ToString(),
                    privateBytes = -1L, workingSet = -1L }; }
            }).ToArray();
            var report = new { schema = 1, fixtureId = _fixtureId, mode = _mode,
                stateKey = _windows[0].StateKey, hostPrivateBytes = process.PrivateMemorySize64,
                hostWorkingSet = process.WorkingSet64, webView2Processes = children, records };
            File.WriteAllText(Path.Combine(_metrics, $"r13-{_mode}-{_session}.json"),
                JsonSerializer.Serialize(report));
        }

        private async Task CaptureR14Measurement()
        {
            const uint continuous = 0x80000000;
            const uint systemRequired = 0x00000001;
            const uint displayRequired = 0x00000002;
            if (SetThreadExecutionState(continuous | systemRequired | displayRequired) == 0)
                throw new InvalidOperationException("Could not inhibit display sleep for R14.");
            try
            {
                var startUtc = DateTimeOffset.UtcNow;
                await Task.WhenAll(_windows.Select(w => w.StartR14(_r14Seconds,
                    _fixtureId == "hole-26-left")));
                var snapshots = new List<object>();
                for (var elapsed = 0; elapsed <= _r14Seconds + 12; elapsed += 10)
                {
                    if (_closing) throw new InvalidOperationException("R14 group closed during measurement.");
                    var processes = _environment?.GetProcessInfos().Select(info =>
                    {
                        try
                        {
                            using var p = System.Diagnostics.Process.GetProcessById(info.ProcessId);
                            return new { pid = info.ProcessId, kind = info.Kind.ToString(),
                                privateBytes = p.PrivateMemorySize64, workingSet = p.WorkingSet64 };
                        }
                        catch { return new { pid = info.ProcessId, kind = info.Kind.ToString(),
                            privateBytes = -1L, workingSet = -1L }; }
                    }).ToArray();
                    snapshots.Add(new { utc = DateTimeOffset.UtcNow, processes });
                    if (elapsed < _r14Seconds + 12) await Task.Delay(10000);
                }
                var records = await Task.WhenAll(_windows.Select(w => w.ReadR14()));
                var report = new { schema = 1, fixtureId = _fixtureId, mode = _mode,
                    durationSeconds = _r14Seconds, warmupSeconds = 10,
                    startUtc, endUtc = DateTimeOffset.UtcNow,
                    stateKey = _windows[0].StateKey, hostPid = Environment.ProcessId,
                    snapshots, records };
                File.WriteAllText(Path.Combine(_metrics, $"r14-{_mode}-{_session}.json"),
                    JsonSerializer.Serialize(report));
                if (records.Any(r => r.GetProperty("phase").GetString() != "done"
                    || r.GetProperty("contextLost").GetBoolean()
                    || r.GetProperty("errors").GetArrayLength() != 0))
                    throw new InvalidOperationException("R14 fixture reported incomplete or failed rendering.");
            }
            finally { SetThreadExecutionState(continuous); }
        }

        private async Task CaptureMotion()
        {
            var frames = new List<object>();
            foreach (var yaw in new[] { -10, -8, -6, -4, -2, 0, 2, 4, 6, 8, 10 })
            {
                await Task.WhenAll(_windows.Select(w => w.MoveToYaw(yaw)));
                if (_windows.Any(w => w.StateKey != _windows[0].StateKey))
                    throw new InvalidOperationException("Motion group state differs between displays.");
                var captures = new List<object>();
                foreach (var window in _windows.Where(w => w.Display.Bounds.Right <= _layout.VirtualBounds.Left + 6400))
                {
                    await window.CheckReady();
                    var path = Path.Combine(_captures,
                        $"motion-yaw{yaw}-{_session}-{window.Display.Name.Replace('\\', '_').Replace('.', '_')}.png");
                    await using (var file = new FileStream(path, FileMode.CreateNew, FileAccess.Write, FileShare.None))
                    {
                        await window.CapturePreview(file);
                        await file.FlushAsync();
                    }
                    await window.CheckReady();
                    captures.Add(new { display = window.Display.Name, path,
                        sha256 = Convert.ToHexString(SHA256.HashData(File.ReadAllBytes(path))) });
                }
                frames.Add(new { yawDegrees = yaw, stateKey = _windows[0].StateKey,
                    statuses = _windows.Select(w => w.LastStatus).ToArray(), captures });
            }
            File.WriteAllText(Path.Combine(_metrics, $"motion-{_session}.json"),
                JsonSerializer.Serialize(new { schema = 1, sessionId = _session,
                    fixtureId = _fixtureId, guardPhysicalPixels = _guard, frames }));
        }

        private async Task CaptureContinuous()
        {
            var capture = _continuous != "baseline";
            var asyncCapture = _continuous == "async";
            var epochMs = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds() + (asyncCapture ? 4000 : 2000);
            await Task.WhenAll(_windows.Select(w => w.StartContinuous(epochMs,
                capture && w.Display.Bounds.Right <= _layout.VirtualBounds.Left + 6400,
                asyncCapture)));
            if (asyncCapture)
            {
                var remainingMs = epochMs + 4200 - DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();
                if (remainingMs > 0) await Task.Delay((int)remainingMs);
            }
            var drainStartUtcMs = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();
            var frames = new List<object>();
            var lastIds = _windows.ToDictionary(w => w.Display.Name, _ => 0);
            var summaries = new Dictionary<string, object>();
            var deadline = DateTime.UtcNow.AddSeconds(25);
            while (DateTime.UtcNow < deadline)
            {
                var results = await Task.WhenAll(_windows.Select(w => w.DrainContinuous()));
                var allDone = true;
                for (var i = 0; i < _windows.Count; i++)
                {
                    var window = _windows[i];
                    var root = results[i];
                    var phase = root.GetProperty("phase").GetString();
                    if (phase == "failed") throw new InvalidOperationException("Continuous fixture failed: " + root);
                    if (phase != "done" || root.GetProperty("queued").GetInt32() != 0) allDone = false;
                    summaries[window.Display.Name] = new { phase,
                        callbacks = root.GetProperty("callbacks").GetInt32(),
                        capacity = root.GetProperty("capacity").GetInt32(),
                        peakPending = root.GetProperty("peakPending").GetInt32(),
                        asyncSupport = root.GetProperty("asyncSupport").Clone(),
                        intervalsMs = root.GetProperty("intervalsMs").Clone(),
                        overflow = root.GetProperty("overflow").Clone(),
                        errors = root.GetProperty("errors").Clone(),
                        gaps = root.GetProperty("gaps").Clone() };
                    var item = root.GetProperty("frame");
                    if (item.ValueKind != JsonValueKind.Object) continue;
                    var id = item.GetProperty("id").GetInt32();
                    if (id <= lastIds[window.Display.Name])
                        throw new InvalidOperationException("Continuous frame order regressed.");
                    var previousId = lastIds[window.Display.Name];
                    lastIds[window.Display.Name] = id;
                    var receivedUtcMs = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();
                    var receivedTicks = System.Diagnostics.Stopwatch.GetTimestamp();
                    var data = item.TryGetProperty("base64", out var encoded)
                        ? Convert.FromBase64String(encoded.GetString()!) : [];
                    var crop = item.GetProperty("crop");
                    if (data.Length > 0 && data.Length != crop.GetProperty("width").GetInt32()
                        * crop.GetProperty("height").GetInt32() * 4)
                        throw new InvalidOperationException("Continuous crop payload size is wrong.");
                    string? path = null;
                    if (data.Length > 0)
                    {
                        path = Path.Combine(_captures, $"b1-{_mode}-{window.Display.Name.Replace('\\', '_').Replace('.', '_')}-{id:D5}.rgba");
                        await using var file = new FileStream(path, FileMode.CreateNew, FileAccess.Write, FileShare.None);
                        await file.WriteAsync(data);
                        await file.FlushAsync();
                    }
                    frames.Add(new { display = window.Display.Name, mode = _mode, id,
                        writeOrdinal = frames.Count + 1,
                        missingBefore = id - previousId - 1,
                        callbackMs = item.GetProperty("callbackMs").GetDouble(),
                        virtualMs = item.GetProperty("virtualMs").GetDouble(),
                        intendedYaw = item.GetProperty("intendedYaw").GetDouble(),
                        actualYaw = item.GetProperty("actualYaw").GetDouble(),
                        renderStartMs = item.GetProperty("renderStartMs").GetDouble(),
                        renderEndMs = item.GetProperty("renderEndMs").GetDouble(),
                        readbackStartMs = item.GetProperty("readbackStartMs").Clone(),
                        readbackEndMs = item.GetProperty("readbackEndMs").Clone(),
                        submitStartMs = item.TryGetProperty("submitStartMs", out var submitStart)
                            ? submitStart.GetDouble() : (double?)null,
                        submitEndMs = item.TryGetProperty("submitEndMs", out var submitEnd)
                            ? submitEnd.GetDouble() : (double?)null,
                        syncPolls = item.TryGetProperty("syncPolls", out var syncPolls)
                            ? syncPolls.GetInt32() : (int?)null,
                        syncPollMs = item.TryGetProperty("syncPollMs", out var syncPollMs)
                            ? syncPollMs.GetDouble() : (double?)null,
                        transferStartMs = item.TryGetProperty("transferStartMs", out var transferStart)
                            ? transferStart.GetDouble() : (double?)null,
                        transferEndMs = item.TryGetProperty("transferEndMs", out var transferEnd)
                            ? transferEnd.GetDouble() : (double?)null,
                        encodeEndMs = item.TryGetProperty("encodeEndMs", out var encodeEnd)
                            ? encodeEnd.GetDouble() : (double?)null,
                        crop = crop.Clone(), pixelFormat = item.GetProperty("pixelFormat").Clone(),
                        payloadBytes = data.Length, path,
                        sha256 = data.Length == 0 ? null : Convert.ToHexString(SHA256.HashData(data)),
                        hostReceivedUtcMs = receivedUtcMs, hostReceivedTicks = receivedTicks,
                        hostWriteUtcMs = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds(),
                        hostWriteTicks = System.Diagnostics.Stopwatch.GetTimestamp() });
                }
                if (allDone) break;
                await Task.Delay(5);
            }
            if (summaries.Count != _windows.Count || summaries.Values.Any(v =>
                JsonSerializer.Serialize(v).Contains("\"phase\":\"done\"") == false))
                throw new TimeoutException("Continuous fixture did not stop within its bound.");
            File.WriteAllText(Path.Combine(_metrics, $"b1-{_mode}-{_session}.json"),
                JsonSerializer.Serialize(new { schema = 1, trajectoryVersion = 1,
                    durationMs = 4000, epochMs, drainStartUtcMs, mode = _mode,
                    guardPhysicalPixels = _mode == "cylindrical" ? 512 : (int?)null,
                    bloomMipMode = "native", capture, sessionId = _session,
                    captureMode = _continuous,
                    layout = _layout.Displays.Select(d => new { d.Name, d.Bounds, d.DpiX }),
                    summaries, frames }));
        }

        private async Task CaptureR17()
        {
            const uint continuous = 0x80000000, systemRequired = 1, displayRequired = 2;
            if (SetThreadExecutionState(continuous | systemRequired | displayRequired) == 0)
                throw new InvalidOperationException("Could not inhibit display sleep for R17.");
            try
            {
                var epoch = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds() + 3000;
                await Task.WhenAll(_windows.Select(w => w.StartR17(epoch,
                    w.Display.Bounds.Left < _layout.VirtualBounds.Left + 6400)));
                var delay = epoch + 61000 - DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();
                if (delay > 0) await Task.Delay((int)delay);
                var frames = new List<object>();
                var summaries = new Dictionary<string, object>();
                var deadline = DateTime.UtcNow.AddMinutes(4);
                while (DateTime.UtcNow < deadline)
                {
                    var results = await Task.WhenAll(_windows.Select(w => w.DrainR17()));
                    var allDone = true;
                    for (var i = 0; i < _windows.Count; i++)
                    {
                        var w = _windows[i];
                        var root = results[i];
                        var phase = root.GetProperty("phase").GetString();
                        if (phase == "failed") throw new InvalidOperationException("R17 fixture failed: " + root);
                        if (phase != "done" || root.GetProperty("queued").GetInt32() != 0) allDone = false;
                        if (root.GetProperty("intervals").ValueKind == JsonValueKind.Array)
                            summaries[w.Display.Name] = new { phase,
                                callbacks = root.GetProperty("callbacks").GetInt32(),
                                captured = root.GetProperty("captured").GetInt32(),
                                overflow = root.GetProperty("overflow").GetInt32(),
                                contextLost = root.GetProperty("contextLost").GetBoolean(),
                                errors = root.GetProperty("errors").Clone(),
                                intervals = root.GetProperty("intervals").Clone() };
                        var item = root.GetProperty("frame");
                        if (item.ValueKind != JsonValueKind.Object) continue;
                        var crop = item.GetProperty("crop");
                        var data = Convert.FromBase64String(item.GetProperty("base64").GetString()!);
                        if (data.Length != crop.GetProperty("width").GetInt32()
                            * crop.GetProperty("height").GetInt32() * 4)
                            throw new InvalidOperationException("R17 crop length disagrees.");
                        var second = item.GetProperty("second").GetInt32();
                        var path = Path.Combine(_captures,
                            $"r17-{w.Display.Name.Replace('\\', '_').Replace('.', '_')}-{second:D2}.rgba");
                        await File.WriteAllBytesAsync(path, data);
                        frames.Add(new { display = w.Display.Name, second,
                            virtualMs = item.GetProperty("virtualMs").GetDouble(),
                            yawDegrees = item.GetProperty("yaw").GetDouble(),
                            callbackMs = item.GetProperty("callbackMs").GetDouble(),
                            readStartMs = item.GetProperty("readStart").GetDouble(),
                            readEndMs = item.GetProperty("readEnd").GetDouble(),
                            crop = crop.Clone(), path,
                            sha256 = Convert.ToHexString(SHA256.HashData(data)) });
                    }
                    if (allDone) break;
                    await Task.Delay(5);
                }
                if (summaries.Count != _windows.Count)
                    throw new TimeoutException("R17 drain did not finish.");
                File.WriteAllText(Path.Combine(_metrics, $"r17-{_session}.json"),
                    JsonSerializer.Serialize(new { schema = 1, fixtureId = _fixtureId,
                        epoch, durationMs = 60000, stateKey = _windows[0].StateKey,
                        hostPid = Environment.ProcessId,
                        processes = _environment?.GetProcessInfos().Select(p => new
                        { pid = p.ProcessId, kind = p.Kind.ToString() }).ToArray(),
                        statuses = _windows.Select(w => w.LastStatus).ToArray(),
                        summaries, frames }));
                if (summaries.Values.Any(v => JsonSerializer.Serialize(v).Contains("\"overflow\":0") == false))
                    throw new InvalidOperationException("R17 capture overflowed.");
            }
            finally { SetThreadExecutionState(continuous); }
        }

        private async Task CaptureForensics()
        {
            var records = new List<object>();
            var expected = _windows[0].StateKey;
            foreach (var window in _windows)
            {
                await window.CheckReady();
                if (window.StateKey != expected)
                    throw new InvalidOperationException("Forensic group state changed.");
                records.Add(await window.Forensics());
            }
            File.WriteAllText(Path.Combine(_metrics, $"forensics-{_session}.json"),
                JsonSerializer.Serialize(new { schema = 1, sessionId = _session,
                    fixtureId = _fixtureId, frozenYawDegrees = _frozenYaw ?? 0,
                    stateKey = expected, records }));
        }

        private async Task CaptureSourceProbe()
        {
            var records = new List<object>();
            var expected = _windows[0].StateKey;
            foreach (var window in _windows.Where(w => w.Display.IsPrimary || w.Display.Bounds.X == _layout.VirtualBounds.X))
            {
                await window.CheckReady();
                if (window.StateKey != expected)
                    throw new InvalidOperationException("Source probe group state changed.");
                records.Add(await window.SourceProbe());
            }
            File.WriteAllText(Path.Combine(_metrics, $"source-probe-{_session}.json"),
                JsonSerializer.Serialize(new { schema = 1, sessionId = _session,
                    fixtureId = _fixtureId, frozenYawDegrees = _frozenYaw,
                    stateKey = expected, records }));
        }

        private async Task CaptureSectorParity()
        {
            var selected = _windows.Where(w => w.Display.IsPrimary
                || w.Display.Bounds.X == _layout.VirtualBounds.X).ToArray();
            var expected = selected[0].StateKey;
            var records = new List<object>();
            foreach (var window in selected)
            {
                await window.CheckReady();
                if (window.StateKey != expected)
                    throw new InvalidOperationException("R9 fixture state differs between displays.");
                var baselineTiming = await window.SectorParityMeasure();
                var baselineLens = await window.CaptureSectorParityLens(_captures, _session, "before");
                var baselineFrame = await CaptureSectorParityFrame(window, "before");
                var enabled = await window.SectorParityToggle(true);
                var pilotTiming = await window.SectorParityMeasure();
                var pilotLens = await window.CaptureSectorParityLens(_captures, _session, "after");
                var pilotFrame = await CaptureSectorParityFrame(window, "after");
                var disabled = await window.SectorParityToggle(false);
                var restoredTiming = await window.SectorParityMeasure();
                var restoredFrame = await CaptureSectorParityFrame(window, "restored");
                records.Add(new { display = window.Display.Name, baselineTiming, baselineLens,
                    baselineFrame, enabled, pilotTiming, pilotLens, pilotFrame, disabled,
                    restoredTiming, restoredFrame });
                await window.CheckReady();
            }
            File.WriteAllText(Path.Combine(_metrics, $"sector-parity-{_session}.json"),
                JsonSerializer.Serialize(new { schema = 1, sessionId = _session,
                    fixtureId = _fixtureId, frozenYawDegrees = _frozenYaw,
                    guardPhysicalPixels = _guard, stateKey = expected, records }));
        }

        private async Task CaptureSectorMotion()
        {
            var selected = _windows.Where(w => w.Display.IsPrimary
                || w.Display.Bounds.X == _layout.VirtualBounds.X).ToArray();
            var yaws = new[] { -12.0, -10.0, -8.0, -6.0, -4.0, -2.0,
                0.0, 2.0, 4.0, 5.0, 5.75, 6.0, 6.1, 6.2,
                6.3, 6.4, 6.5, 6.6, 6.7, 6.75, 6.8, 6.815, 6.85, 6.9,
                6.95, 7.0, 7.1, 7.2, 7.25, 7.3, 7.4, 7.5, 7.6, 7.7,
                7.8, 7.9, 8.0, 8.1, 9.0, 10.0, 12.0 };
            var records = new List<object>();
            foreach (var phase in new[] { "ordinary", "extra-left" })
            {
                if (phase == "extra-left")
                    foreach (var window in selected) await window.SectorParityToggle(true);
                foreach (var yaw in yaws)
                {
                    var states = await Task.WhenAll(_windows.Select(w => w.SectorMotionYaw(yaw)));
                    var stateKey = states[0].GetProperty("stateKey").GetString();
                    if (states.Any(s => s.GetProperty("stateKey").GetString() != stateKey))
                        throw new InvalidOperationException("R10 yaw state differs between displays.");
                    var captures = new List<object>();
                    foreach (var window in selected)
                    {
                        var frame = await CaptureSectorParityFrame(window, $"r10-{phase}-yaw{yaw.ToString("0.###", CultureInfo.InvariantCulture)}");
                        var trace = await window.SectorMotionTrace();
                        var tracePath = Path.Combine(_metrics,
                            $"r10-trace-{phase}-yaw{yaw.ToString("0.###", CultureInfo.InvariantCulture)}-{window.Display.Name.Replace('\\', '_').Replace('.', '_')}.json");
                        File.WriteAllText(tracePath, trace.GetRawText());
                        captures.Add(new { display = window.Display.Name, frame, tracePath,
                            centersRadians = trace.GetProperty("centersRadians").Clone(),
                            sampledEnvelope = trace.GetProperty("sampledEnvelope").Clone() });
                    }
                    records.Add(new { phase, yawDegrees = yaw, stateKey, states, captures });
                }
            }
            foreach (var window in selected) await window.SectorParityToggle(false);
            foreach (var window in _windows) await window.SectorMotionYaw(6.815);
            var restored = new List<object>();
            foreach (var window in selected)
                restored.Add(new { display = window.Display.Name,
                    frame = await CaptureSectorParityFrame(window, "r10-restored-yaw6.815") });
            File.WriteAllText(Path.Combine(_metrics, $"sector-motion-{_session}.json"),
                JsonSerializer.Serialize(new { schema = 1, sessionId = _session,
                    fixtureId = _fixtureId, guardPhysicalPixels = _guard,
                    yawDegrees = yaws, records, restored }));
        }

        private async Task CaptureNegativeProbe()
        {
            var selected = _windows.Where(w => w.Display.IsPrimary
                || w.Display.Bounds.X == _layout.VirtualBounds.X).ToArray();
            var expected = selected[0].StateKey;
            var records = new List<object>();
            foreach (var window in selected)
            {
                await window.CheckReady();
                if (window.StateKey != expected)
                    throw new InvalidOperationException("R11 state differs between displays.");
                var sourceProbe = await window.SourceProbe();
                var baselineLens = await window.CaptureSectorParityLens(_captures, _session, "r11-before");
                var baselineFrame = await CaptureSectorParityFrame(window, "r11-before");
                var enabled = await window.NegativeSectorToggle(true);
                var pilotLens = await window.CaptureSectorParityLens(_captures, _session, "r11-after");
                var pilotFrame = await CaptureSectorParityFrame(window, "r11-after");
                var disabled = await window.NegativeSectorToggle(false);
                var restoredFrame = await CaptureSectorParityFrame(window, "r11-restored");
                records.Add(new { display = window.Display.Name, sourceProbe,
                    baselineLens, baselineFrame, enabled, pilotLens, pilotFrame,
                    disabled, restoredFrame });
            }
            var nearby = new List<object>();
            foreach (var yaw in new[] { -8.0, -7.0, -6.5, -6.25, -6.0,
                -5.75, -5.5, -5.0, -4.5, -4.0 })
            {
                var states = await Task.WhenAll(_windows.Select(w => w.SectorMotionYaw(yaw)));
                if (states.Any(s => s.GetProperty("stateKey").GetString()
                    != states[0].GetProperty("stateKey").GetString()))
                    throw new InvalidOperationException("R11 nearby yaw state differs.");
                var captures = new List<object>();
                foreach (var window in selected)
                    captures.Add(new { display = window.Display.Name,
                        frame = await CaptureSectorParityFrame(window,
                            $"r11-nearby-yaw{yaw.ToString("0.##", CultureInfo.InvariantCulture)}") });
                nearby.Add(new { yawDegrees = yaw, states, captures });
            }
            File.WriteAllText(Path.Combine(_metrics, $"negative-probe-{_session}.json"),
                JsonSerializer.Serialize(new { schema = 1, sessionId = _session,
                    fixtureId = _fixtureId, frozenYawDegrees = _frozenYaw,
                    guardPhysicalPixels = _guard, stateKey = expected, records, nearby }));
        }

        private async Task CaptureZeroTarget()
        {
            var selected = _windows.Where(w => w.Display.IsPrimary
                || w.Display.Bounds.X == _layout.VirtualBounds.X).ToArray();
            var records = new List<object>();
            var yaws = new[] { -7.0, -6.5, -6.0, -5.5, -4.0, 0.0,
                2.0, 4.0, 5.5, 6.0, 6.2, 6.4, 6.6, 6.815, 7.0, 7.2, 7.5 };
            foreach (var width in new[] { 0, 64, 128 })
            {
                var policy = await Task.WhenAll(selected.Select(w => w.ZeroTargetPolicy(width)));
                var frames = new List<object>();
                foreach (var yaw in yaws)
                {
                    var states = await Task.WhenAll(_windows.Select(w => w.SectorMotionYaw(yaw)));
                    if (states.Any(s => s.GetProperty("stateKey").GetString()
                        != states[0].GetProperty("stateKey").GetString()))
                        throw new InvalidOperationException("R12 yaw state differs between displays.");
                    var captures = new List<object>();
                    foreach (var window in selected)
                        captures.Add(new { display = window.Display.Name,
                            frame = await CaptureSectorParityFrame(window,
                                $"r12-width{width}-yaw{yaw.ToString("0.###", CultureInfo.InvariantCulture)}") });
                    frames.Add(new { yawDegrees = yaw, states, captures });
                }
                foreach (var window in selected) await window.SectorMotionYaw(6.815);
                var measurements = new List<object>();
                foreach (var window in selected)
                    measurements.Add(new { display = window.Display.Name,
                        data = await window.ZeroTargetMeasure() });
                records.Add(new { transitionPhysicalPixels = width, policy, frames, measurements });
            }
            var rollback = await Task.WhenAll(selected.Select(w => w.ZeroTargetPolicy(0)));
            foreach (var window in _windows) await window.SectorMotionYaw(6.815);
            var restored = new List<object>();
            foreach (var window in selected)
                restored.Add(new { display = window.Display.Name,
                    frame = await CaptureSectorParityFrame(window, "r12-restored-yaw6.815") });
            File.WriteAllText(Path.Combine(_metrics, $"zero-target-{_session}.json"),
                JsonSerializer.Serialize(new { schema = 1, sessionId = _session,
                    fixtureId = _fixtureId, frozenYawDegrees = _frozenYaw,
                    guardPhysicalPixels = _guard, yaws, records, rollback, restored }));
        }

        private async Task<object> CaptureSectorParityFrame(Window window, string phase)
        {
            var path = Path.Combine(_captures,
                $"r9-{phase}-{_session}-{window.Display.Name.Replace('\\', '_').Replace('.', '_')}.png");
            await using (var file = new FileStream(path, FileMode.CreateNew, FileAccess.Write, FileShare.None))
            {
                await window.CapturePreview(file);
                await file.FlushAsync();
            }
            return new { path, sha256 = Convert.ToHexString(SHA256.HashData(File.ReadAllBytes(path))) };
        }

        private async void Watch(object? sender, EventArgs e)
        {
            _watch.Stop();
            try
            {
                if (_deadlineUtc != default && DateTime.UtcNow >= _deadlineUtc)
                {
                    Stop("Developer smoke interval completed.", true);
                    return;
                }
                var fresh = DisplayLayout.Query();
                ValidateLayout(fresh);
                if (fresh.VirtualBounds != _layout.VirtualBounds || fresh.Displays.Count != _layout.Displays.Count
                    || _layout.Displays.Any(old => !fresh.Displays.Any(now => now.Name == old.Name
                        && now.Bounds == old.Bounds && now.DpiX == old.DpiX)))
                    throw new InvalidOperationException("Display layout changed.");
                if (_continuous is null && !_sectorMotion && !_negativeProbe && !_zeroTarget
                    && _r14Seconds == 0)
                    foreach (var window in _windows) await window.CheckReady();
                if (!_closing) _watch.Start();
            }
            catch (Exception ex) { Fail("Developer group lost readiness: " + ex); }
        }

        private void Fail(string reason) => Stop(reason, false);
        private void Stop(string reason, bool normal)
        {
            if (_closing) return;
            _closing = true;
            Result = normal ? GroupResult.Normal : GroupResult.Failed;
            Log.Info("Developer group closing: " + reason);
            _watch.Stop();
            _input?.Dispose();
            foreach (var window in _windows) window.Close();
            ExitThread();
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                System.Windows.Forms.Application.Idle -= Start;
                _watch.Dispose();
                _input?.Dispose();
                foreach (var window in _windows) window.Dispose();
            }
            base.Dispose(disposing);
        }
    }

    private sealed class Window : System.Windows.Forms.Form
    {
        private readonly PixelRect _desktop;
        private readonly string _session, _mode, _dist;
        private readonly bool _injectFailure;
        private readonly string? _fixtureId;
        private readonly string _post;
        private readonly int _guard;
        private readonly string _bloomMips;
        private readonly double? _frozenYaw;
        private readonly bool _forensics;
        private readonly bool _sourceProbe;
        private readonly bool _sectorParity;
        private readonly bool _sectorMotion;
        private readonly bool _negativeProbe;
        private readonly bool _zeroTarget;
        private readonly Action<string> _fail;
        private readonly WebView2 _view = new() { Dock = System.Windows.Forms.DockStyle.Fill };
        private readonly System.Windows.Forms.Panel _cover = new()
            { Dock = System.Windows.Forms.DockStyle.Fill, BackColor = System.Drawing.Color.Black };
        public DisplayRegion Display { get; }
        public object? LastStatus { get; private set; }
        public string? StateKey { get; private set; }
        private int _expectedFrameRevision = 1;

        public Window(DisplayRegion display, PixelRect desktop, string session, string mode,
            string dist, Action<string> fail, bool injectFailure, string? fixtureId, string post,
            int guard, string bloomMips, double? frozenYaw, bool forensics, bool sourceProbe,
            bool sectorParity, bool sectorMotion, bool negativeProbe, bool zeroTarget)
        {
            Display = display; _desktop = desktop; _session = session; _mode = mode;
            _dist = dist; _fail = fail; _injectFailure = injectFailure;
            _fixtureId = fixtureId; _post = post; _guard = guard; _bloomMips = bloomMips;
            _frozenYaw = frozenYaw;
            _forensics = forensics;
            _sourceProbe = sourceProbe;
            _sectorParity = sectorParity;
            _sectorMotion = sectorMotion;
            _negativeProbe = negativeProbe;
            _zeroTarget = zeroTarget;
            Text = "Reach disposable developer display";
            BackColor = System.Drawing.Color.Black;
            FormBorderStyle = System.Windows.Forms.FormBorderStyle.None;
            StartPosition = System.Windows.Forms.FormStartPosition.Manual;
            Bounds = new System.Drawing.Rectangle(display.Bounds.X, display.Bounds.Y,
                display.Bounds.Width, display.Bounds.Height);
            Controls.Add(_view);
            Controls.Add(_cover);
            _cover.BringToFront();
            FormClosed += (_, _) => _fail("A developer window closed.");
        }

        public async Task Initialize(CoreWebView2Environment environment)
        {
            await _view.EnsureCoreWebView2Async(environment);
            var core = _view.CoreWebView2;
            core.Settings.AreDefaultContextMenusEnabled = false;
            core.Settings.AreDevToolsEnabled = false;
            core.Settings.AreHostObjectsAllowed = false;
            core.Settings.IsWebMessageEnabled = false;
            core.NewWindowRequested += (_, args) => args.Handled = true;
            core.ProcessFailed += (_, args) => _fail("WebView2 process failed: " + args.ProcessFailedKind);
            core.NavigationStarting += (_, args) =>
            {
                if (!args.Uri.StartsWith("https://" + Host + "/", StringComparison.OrdinalIgnoreCase))
                    args.Cancel = true;
            };
            core.AddWebResourceRequestedFilter("*", CoreWebView2WebResourceContext.All);
            core.WebResourceRequested += (_, args) =>
            {
                if (args.Request.Uri.StartsWith("https://" + Host + "/", StringComparison.OrdinalIgnoreCase)) return;
                args.Response = environment.CreateWebResourceResponse(null, 403, "Blocked", "Content-Type: text/plain");
            };
            var m = Display.Bounds; var d = _desktop;
            var contract = new { schema = 1, sessionId = _session, isPrimaryDisplay = Display.IsPrimary,
                monitorBounds = new { x = m.X, y = m.Y, width = m.Width, height = m.Height },
                virtualDesktopBounds = new { x = d.X, y = d.Y, width = d.Width, height = d.Height },
                viewportOffsetX = m.X - d.X, viewportOffsetY = m.Y - d.Y,
                viewportWidth = m.Width, viewportHeight = m.Height,
                totalPanoramaWidth = d.Width, totalPanoramaHeight = d.Height,
                devicePixelRatio = Display.DpiX / 96f };
            await core.AddScriptToExecuteOnDocumentCreatedAsync(
                "Object.defineProperty(window,'__reachPanorama',{value:Object.freeze(" +
                JsonSerializer.Serialize(contract) + ")});" +
                (_mode == "cylindrical" ? "Object.defineProperty(window,'__reachDeveloperProjection',{value:Object.freeze(" +
                JsonSerializer.Serialize(new { schema = 1, mode = _mode, sessionId = _session }) + ")});" : "") +
                (_fixtureId is null ? "" : "Object.defineProperty(window,'__reachNativeFixture',{value:Object.freeze(" +
                JsonSerializer.Serialize(new { schema = 1, fixtureId = _fixtureId, stateRevision = 1,
                    mode = _mode, sessionId = _session, post = _post,
                    guardPhysicalPixels = _guard, bloomMipMode = _bloomMips,
                    frozenYawDegrees = _frozenYaw, forensics = _forensics,
                    sourceProbe = _sourceProbe, sectorParity = _sectorParity,
                    sectorMotion = _sectorMotion, negativeProbe = _negativeProbe,
                    zeroTarget = _zeroTarget }) + ")});"));
            await core.AddScriptToExecuteOnDocumentCreatedAsync("""
                (() => {
                  const intervals = []; let previous = 0;
                  const tick = t => {
                    if (previous) intervals.push({ t, dt: t - previous });
                    if (intervals.length > 1200) intervals.shift();
                    previous = t; requestAnimationFrame(tick);
                  };
                  requestAnimationFrame(tick);
                  Object.defineProperty(window, '__reachDevFrameIntervals', {
                    value: () => intervals.filter(x => x.t >= performance.now() - 3000).map(x => x.dt)
                  });
                })();
                """);
            core.SetVirtualHostNameToFolderMapping(Host, _dist, CoreWebView2HostResourceAccessKind.DenyCors);
            var navigation = new TaskCompletionSource<bool>(TaskCreationOptions.RunContinuationsAsynchronously);
            core.NavigationCompleted += (_, args) => navigation.TrySetResult(args.IsSuccess);
            core.Navigate(_fixtureId is null ? Url : FixtureUrl);
            if (!await navigation.Task.WaitAsync(TimeSpan.FromSeconds(30)))
                throw new InvalidOperationException("Developer payload navigation failed.");
        }

        public async Task WaitReady()
        {
            if (_injectFailure) throw new InvalidOperationException("Injected primary renderer initialization failure.");
            for (var i = 0; i < 60; i++)
            {
                try { await CheckReady(); return; }
                catch when (i < 59) { await Task.Delay(500); }
            }
            throw new InvalidOperationException("Developer display did not become ready.");
        }

        public async Task CheckReady()
        {
            var json = await _view.CoreWebView2.ExecuteScriptAsync(
                _fixtureId is null
                    ? "JSON.stringify({state:document.readyState,css:[innerWidth,innerHeight],dpr:devicePixelRatio,panorama:window.__reachPanoramaStatus?.()??null})"
                    : "JSON.stringify({state:document.readyState,css:[innerWidth,innerHeight],dpr:devicePixelRatio,fixture:window.__a1Status?.()??null})");
            using var outer = JsonDocument.Parse(json);
            using var state = JsonDocument.Parse(outer.RootElement.GetString() ?? "null");
            var root = state.RootElement;
            if (root.GetProperty("state").GetString() != "complete") throw new InvalidOperationException("Document incomplete.");
            var css = root.GetProperty("css");
            var dpr = root.GetProperty("dpr").GetDouble();
            if (Math.Abs(dpr - Display.DpiX / 96.0) > 0.02
                || Math.Abs(css[0].GetDouble() * dpr - Display.Bounds.Width) > 2
                || Math.Abs(css[1].GetDouble() * dpr - Display.Bounds.Height) > 2)
                throw new InvalidOperationException("Physical and CSS geometry disagree.");
            if (_fixtureId is not null)
            {
                var fixture = root.GetProperty("fixture");
                if (fixture.ValueKind != JsonValueKind.Object
                    || fixture.GetProperty("error").ValueKind != JsonValueKind.Null
                    || fixture.GetProperty("mode").GetString() != _mode
                    || fixture.GetProperty("contract").GetProperty("sessionId").GetString() != _session)
                    throw new InvalidOperationException("Native fixture failed or selected the wrong projection.");
                var fixtureState = fixture.GetProperty("nativeFixture");
                if (fixtureState.GetProperty("sessionId").GetString() != _session
                    || fixtureState.GetProperty("fixtureId").GetString() != _fixtureId
                    || fixtureState.GetProperty("stateRevision").GetInt32() != 1
                    || fixtureState.GetProperty("post").GetString() != _post
                    || fixtureState.GetProperty("guardPhysicalPixels").GetInt32() != _guard
                    || fixtureState.GetProperty("bloomMipMode").GetString() != _bloomMips
                    || (_frozenYaw is not null && fixtureState.GetProperty("state")
                        .GetProperty("yawDegrees").GetDouble() != _frozenYaw)
                    || !fixtureState.GetProperty("renderReady").GetBoolean()
                    || fixtureState.GetProperty("frameRevision").GetInt32() != _expectedFrameRevision)
                    throw new InvalidOperationException("Native fixture state is not ready.");
                var buffer = fixture.GetProperty("drawingBuffer");
                if (buffer[0].GetInt32() != Display.Bounds.Width
                    || buffer[1].GetInt32() != Display.Bounds.Height)
                    throw new InvalidOperationException("Native fixture drawing buffer has the wrong size.");
                var cylinder = fixture.GetProperty("cylindrical");
                if (_mode == "cylindrical")
                {
                    if (cylinder.ValueKind != JsonValueKind.Object
                        || cylinder.GetProperty("mode").GetString() != "cylindrical"
                        || cylinder.GetProperty("guardPhysicalPixels").GetInt32() != _guard
                        || cylinder.GetProperty("bloomMipMode").GetString() != _bloomMips)
                        throw new InvalidOperationException("Native fixture cylinder is not ready.");
                }
                else if (cylinder.ValueKind != JsonValueKind.Null)
                    throw new InvalidOperationException("Rectilinear fixture initialized a cylinder.");
                StateKey = fixtureState.GetProperty("stateKey").GetString();
                if (string.IsNullOrEmpty(StateKey))
                    throw new InvalidOperationException("Native fixture state key is missing.");
                LastStatus = JsonSerializer.Deserialize<object>(root.GetRawText());
                return;
            }
            var status = root.GetProperty("panorama");
            if (status.ValueKind != JsonValueKind.Object
                || status.GetProperty("contract").GetProperty("sessionId").GetString() != _session
                || status.GetProperty("primary").GetBoolean() != Display.IsPrimary
                || status.GetProperty("sequence").GetInt32() < 0)
                throw new InvalidOperationException("Panorama session or frame is not ready.");
            var contract = status.GetProperty("contract");
            var monitor = contract.GetProperty("monitorBounds");
            var desktop = contract.GetProperty("virtualDesktopBounds");
            if (monitor.GetProperty("x").GetInt32() != Display.Bounds.X
                || monitor.GetProperty("y").GetInt32() != Display.Bounds.Y
                || monitor.GetProperty("width").GetInt32() != Display.Bounds.Width
                || monitor.GetProperty("height").GetInt32() != Display.Bounds.Height
                || desktop.GetProperty("x").GetInt32() != _desktop.X
                || desktop.GetProperty("y").GetInt32() != _desktop.Y
                || desktop.GetProperty("width").GetInt32() != _desktop.Width
                || desktop.GetProperty("height").GetInt32() != _desktop.Height
                || Math.Abs(contract.GetProperty("devicePixelRatio").GetDouble() - dpr) > 0.02
                || !double.IsFinite(status.GetProperty("compositionShiftX").GetDouble()))
                throw new InvalidOperationException("Panorama geometry or composition contract changed.");
            var canvas = status.GetProperty("canvas");
            if (canvas[0].GetInt32() != Display.Bounds.Width || canvas[1].GetInt32() != Display.Bounds.Height)
                throw new InvalidOperationException("WebGL drawing buffer does not match the physical monitor.");
            var projection = status.GetProperty("developerProjection");
            if (_mode == "cylindrical")
            {
                if (projection.ValueKind != JsonValueKind.Object
                    || projection.GetProperty("mode").GetString() != "cylindrical"
                    || projection.TryGetProperty("failed", out _)
                    || projection.GetProperty("sectors").GetInt32() < 1
                    || projection.GetProperty("guardPhysicalPixels").GetInt32() != 128
                    || projection.GetProperty("sectorTargets").GetArrayLength()
                        != projection.GetProperty("sectors").GetInt32()
                    || projection.GetProperty("postTarget")[0].GetInt32() < Display.Bounds.Width
                    || projection.GetProperty("postTarget")[1].GetInt32() < Display.Bounds.Height
                    || projection.GetProperty("output")[0].GetInt32() != Display.Bounds.Width
                    || projection.GetProperty("output")[1].GetInt32() != Display.Bounds.Height)
                    throw new InvalidOperationException("Cylindrical renderer readiness failed.");
            }
            else if (projection.ValueKind != JsonValueKind.Null)
                throw new InvalidOperationException("Rectilinear fallback initialized a cylindrical renderer.");
            LastStatus = JsonSerializer.Deserialize<object>(root.GetRawText());
        }

        public void Reveal() => _cover.Visible = false;
        public async Task RenderAgain(int frame)
        {
            if (_fixtureId is null || frame != _expectedFrameRevision + 1)
                throw new InvalidOperationException("Invalid fixture frame advance.");
            var result = await _view.CoreWebView2.ExecuteScriptAsync("window.__a1RenderAgain?.()");
            if (!int.TryParse(result, out var actual) || actual != frame)
                throw new InvalidOperationException("Fixture render did not advance the expected frame.");
            _expectedFrameRevision = frame;
        }
        public async Task MoveToYaw(int yaw)
        {
            if (_fixtureId != "hole-26-left" || _mode != "cylindrical")
                throw new InvalidOperationException("Motion unavailable for this fixture.");
            var result = await _view.CoreWebView2.ExecuteScriptAsync(
                "JSON.stringify(window.__a1MoveToYaw(" + yaw + "))");
            using var outer = JsonDocument.Parse(result);
            using var record = JsonDocument.Parse(outer.RootElement.GetString() ?? "null");
            var state = record.RootElement;
            if (state.GetProperty("sessionId").GetString() != _session
                || state.GetProperty("fixtureId").GetString() != _fixtureId
                || state.GetProperty("yawDegrees").GetInt32() != yaw
                || state.GetProperty("mode").GetString() != _mode
                || state.GetProperty("frameRevision").GetInt32() != _expectedFrameRevision + 1)
                throw new InvalidOperationException("Motion step failed its state contract.");
            _expectedFrameRevision++;
            await CheckReady();
        }
        public async Task<object> Forensics()
        {
            if (!_forensics || _fixtureId != "hole-26-left")
                throw new InvalidOperationException("Forensic readback was not requested.");
            var json = await _view.CoreWebView2.ExecuteScriptAsync(
                "JSON.stringify(window.__a1Forensics?.())");
            using var outer = JsonDocument.Parse(json);
            using var record = JsonDocument.Parse(outer.RootElement.GetString() ?? "null");
            var root = record.RootElement;
            if (root.GetProperty("sessionId").GetString() != _session
                || root.GetProperty("fixtureId").GetString() != _fixtureId
                || root.GetProperty("stateKey").GetString() != StateKey
                || root.GetProperty("frameRevision").GetInt32() != 1)
                throw new InvalidOperationException("Forensic readback disagrees with captured state.");
            return root.Clone();
        }

        public async Task<object> SourceProbe()
        {
            if ((!_sourceProbe && !_negativeProbe) || _fixtureId != "hole-26-left")
                throw new InvalidOperationException("Source probe was not requested.");
            await _view.CoreWebView2.ExecuteScriptAsync(
                "window.__a1SourceProbeResult=null;window.__a1SourceProbe().then(x=>window.__a1SourceProbeResult=x).catch(e=>window.__a1SourceProbeResult={error:String(e)});true");
            string json = "null";
            var deadline = DateTime.UtcNow.AddSeconds(30);
            while (DateTime.UtcNow < deadline)
            {
                await Task.Delay(100);
                json = await _view.CoreWebView2.ExecuteScriptAsync(
                    "JSON.stringify(window.__a1SourceProbeResult)");
                if (json != "null") break;
            }
            using var outer = JsonDocument.Parse(json);
            using var record = JsonDocument.Parse(outer.RootElement.ValueKind == JsonValueKind.String
                ? outer.RootElement.GetString() ?? "null" : json);
            var root = record.RootElement;
            if (root.ValueKind != JsonValueKind.Object || !root.TryGetProperty("sessionId", out _))
                throw new InvalidOperationException("Source probe script returned: "
                    + json[..Math.Min(json.Length, 2000)]);
            if (root.GetProperty("sessionId").GetString() != _session
                || root.GetProperty("fixtureId").GetString() != _fixtureId
                || root.GetProperty("stateKey").GetString() != StateKey
                || root.GetProperty("frameRevision").GetInt32() != 1)
                throw new InvalidOperationException("Source probe disagrees with captured state.");
            return root.Clone();
        }
        public async Task<JsonElement> SectorParityToggle(bool enabled) =>
            await SectorParityCall($"window.__a1R9Toggle({enabled.ToString().ToLowerInvariant()})");

        public async Task<JsonElement> NegativeSectorToggle(bool enabled) =>
            await SectorParityCall($"window.__a1R11Toggle({enabled.ToString().ToLowerInvariant()})");

        public async Task<JsonElement> SectorParityMeasure() =>
            await SectorParityCall("window.__a1R9Measure()");

        public async Task<JsonElement> ZeroTargetPolicy(int width)
        {
            if (!_zeroTarget || width is not (0 or 64 or 128))
                throw new InvalidOperationException("R12 policy was not requested.");
            return await SectorParityCall($"window.__a1R12Policy({width})");
        }

        public async Task<JsonElement> ZeroTargetMeasure()
        {
            if (!_zeroTarget) throw new InvalidOperationException("R12 measure was not requested.");
            return await SectorParityCall("window.__a1R12Measure()");
        }

        public async Task<JsonElement> SectorMotionYaw(double yaw)
        {
            if (!_sectorMotion && !_negativeProbe && !_zeroTarget)
                throw new InvalidOperationException("Motion was not requested.");
            return await SectorParityCall("window.__a1R10Yaw("
                + yaw.ToString("0.###", CultureInfo.InvariantCulture) + ")");
        }

        public async Task<JsonElement> SectorMotionTrace()
        {
            if (!_sectorMotion) throw new InvalidOperationException("R10 trace was not requested.");
            return await SectorParityCall("window.__a1R10Trace()");
        }

        private async Task<JsonElement> SectorParityCall(string expression)
        {
            if ((!_sectorParity && !_negativeProbe && !_zeroTarget) || _fixtureId != "hole-26-left")
                throw new InvalidOperationException("R9 pilot was not requested.");
            var json = await _view.CoreWebView2.ExecuteScriptAsync($"JSON.stringify({expression})");
            using var outer = JsonDocument.Parse(json);
            using var inner = JsonDocument.Parse(outer.RootElement.GetString() ?? "null");
            if (inner.RootElement.ValueKind != JsonValueKind.Object)
                throw new InvalidOperationException("R9 fixture call failed: " + json);
            return inner.RootElement.Clone();
        }

        public async Task<object> CaptureSectorParityLens(string captures, string session, string phase)
        {
            var metadata = await SectorParityCall("window.__a1R9LensBegin()");
            var width = metadata.GetProperty("width").GetInt32();
            var height = metadata.GetProperty("height").GetInt32();
            var path = Path.Combine(captures,
                $"r9-lens-{phase}-{session}-{Display.Name.Replace('\\', '_').Replace('.', '_')}.rgba16f");
            try
            {
                await using var file = new FileStream(path, FileMode.CreateNew, FileAccess.Write, FileShare.None);
                for (var row = 0; row < height; row += 16)
                {
                    var count = Math.Min(16, height - row);
                    var json = await _view.CoreWebView2.ExecuteScriptAsync(
                        $"window.__a1R9LensChunk({row},{count})");
                    var encoded = JsonSerializer.Deserialize<string>(json)
                        ?? throw new InvalidOperationException("R9 lens chunk is empty.");
                    var bytes = Convert.FromBase64String(encoded);
                    if (bytes.Length != width * count * 8)
                        throw new InvalidOperationException("R9 lens chunk length differs.");
                    await file.WriteAsync(bytes);
                }
                await file.FlushAsync();
            }
            finally { await _view.CoreWebView2.ExecuteScriptAsync("window.__a1R9LensEnd()"); }
            return new { metadata, path, bytes = new FileInfo(path).Length,
                sha256 = Convert.ToHexString(SHA256.HashData(File.ReadAllBytes(path))) };
        }
        public Task CapturePreview(Stream stream) => _view.CoreWebView2.CapturePreviewAsync(
            CoreWebView2CapturePreviewImageFormat.Png, stream);

        public async Task StartR17(long epochMs, bool capture)
        {
            var json = await _view.CoreWebView2.ExecuteScriptAsync(
                $"JSON.stringify(window.__r17Start({epochMs}, {capture.ToString().ToLowerInvariant()}))");
            using var outer = JsonDocument.Parse(json);
            using var inner = JsonDocument.Parse(outer.RootElement.GetString() ?? "null");
            if (inner.RootElement.ValueKind != JsonValueKind.Object)
                throw new InvalidOperationException("R17 fixture start failed.");
        }

        public async Task<JsonElement> DrainR17()
        {
            var json = await _view.CoreWebView2.ExecuteScriptAsync("JSON.stringify(window.__r17Drain())");
            using var outer = JsonDocument.Parse(json);
            using var inner = JsonDocument.Parse(outer.RootElement.GetString() ?? "null");
            return inner.RootElement.Clone();
        }

        public async Task StartContinuous(long epochMs, bool capture, bool asyncCapture)
        {
            var mode = asyncCapture ? "\"async\"" : capture.ToString().ToLowerInvariant();
            var json = await _view.CoreWebView2.ExecuteScriptAsync(
                $"JSON.stringify(window.__b1Start({epochMs}, {mode}))");
            using var outer = JsonDocument.Parse(json);
            using var inner = JsonDocument.Parse(outer.RootElement.GetString() ?? "null");
            if (inner.RootElement.GetProperty("version").GetInt32() != 1)
                throw new InvalidOperationException("Continuous trajectory version disagrees.");
            if (asyncCapture && !inner.RootElement.GetProperty("asyncSupport")
                    .EnumerateObject().All(property => property.Value.GetBoolean()))
                throw new InvalidOperationException("WebGL2 asynchronous readback is unavailable.");
        }

        public async Task<JsonElement> DrainContinuous()
        {
            var json = await _view.CoreWebView2.ExecuteScriptAsync("JSON.stringify(window.__b1Drain())");
            using var outer = JsonDocument.Parse(json);
            using var inner = JsonDocument.Parse(outer.RootElement.GetString() ?? "null");
            return inner.RootElement.Clone();
        }

        public async Task<object> FrameIntervalReport()
        {
            var json = await _view.CoreWebView2.ExecuteScriptAsync("window.__reachDevFrameIntervals?.() ?? []");
            var values = JsonSerializer.Deserialize<double[]>(json) ?? [];
            var sorted = values.Where(v => double.IsFinite(v) && v > 0).Order().ToArray();
            if (sorted.Length == 0) return new { display = Display.Name, count = 0 };
            double At(double percentile) => sorted[Math.Min(sorted.Length - 1,
                (int)Math.Ceiling(percentile * sorted.Length) - 1)];
            return new { display = Display.Name, count = sorted.Length,
                averageMs = sorted.Average(), p50Ms = At(0.5), p95Ms = At(0.95), p99Ms = At(0.99) };
        }

        public async Task<object> MeasureR13()
        {
            var script = """
                (() => {
                  const canvas = document.querySelector('#scene');
                  const gl = canvas?.getContext('webgl2');
                  if (!gl || !window.__a1RenderAgain || !window.__a1Status)
                    throw new Error('R13 fixture instrumentation unavailable');
                  const names = ['drawArrays', 'drawElements', 'drawArraysInstanced', 'drawElementsInstanced'];
                  const original = new Map();
                  let draws = 0;
                  try {
                    for (const name of names) {
                      const fn = gl[name];
                      original.set(name, fn);
                      gl[name] = (...args) => { draws++; return fn.apply(gl, args); };
                    }
                    for (let i = 0; i < 8; i++) { window.__a1RenderAgain(); gl.finish(); }
                    const cpu = [], completed = [], calls = [];
                    for (let i = 0; i < 48; i++) {
                      draws = 0;
                      const start = performance.now();
                      window.__a1RenderAgain();
                      const submitted = performance.now();
                      gl.finish();
                      const ended = performance.now();
                      cpu.push(submitted - start);
                      completed.push(ended - start);
                      calls.push(draws);
                    }
                    const status = window.__a1Status();
                    return { cpuSubmissionMs: cpu, completedRenderMs: completed,
                      drawCalls: calls, contextLost: gl.isContextLost(),
                      glError: gl.getError(), stateKey: status.nativeFixture.stateKey,
                      frameRevision: status.nativeFixture.frameRevision,
                      drawingBuffer: status.drawingBuffer, cylindrical: status.cylindrical };
                  } finally {
                    for (const [name, fn] of original) gl[name] = fn;
                  }
                })()
                """;
            var json = await _view.CoreWebView2.ExecuteScriptAsync("JSON.stringify(" + script + ")");
            using var outer = JsonDocument.Parse(json);
            using var inner = JsonDocument.Parse(outer.RootElement.GetString() ?? "null");
            var result = inner.RootElement;
            if (result.GetProperty("contextLost").GetBoolean()
                || result.GetProperty("glError").GetInt32() != 0
                || result.GetProperty("stateKey").GetString() != StateKey
                || result.GetProperty("cpuSubmissionMs").GetArrayLength() != 48
                || result.GetProperty("completedRenderMs").GetArrayLength() != 48)
                throw new InvalidOperationException("R13 measured fixture became invalid.");
            return new { display = Display.Name, measurement = result.Clone() };
        }

        public async Task StartR14(int seconds, bool moveCamera)
        {
            var script = """
                (() => {
                  if (!window.__a1RenderAgain || !window.__a1Status || window.__r14)
                    throw new Error('R14 fixture instrumentation unavailable');
                  const gl = document.querySelector('#scene')?.getContext('webgl2');
                  if (!gl) throw new Error('R14 WebGL2 unavailable');
                  const result = {phase:'warmup', intervalsMs:[], renderMs:[],
                    callbackTimesMs:[], errors:[], contextLost:false, glError:0,
                    warmupFrames:0, measuredFrames:0, motionChanges:0};
                  window.__r14 = result;
                  const started = performance.now();
                  let last = null, previousYaw = null;
                  const tick = time => {
                    if (result.phase === 'failed' || result.phase === 'done') return;
                    const age = time - started;
                    const measuring = age >= 10000;
                    if (age >= 10000 + SECONDS * 1000) {
                      result.phase = 'done';
                      result.contextLost = gl.isContextLost();
                      result.glError = gl.getError();
                      result.status = window.__a1Status();
                      return;
                    }
                    try {
                      if (measuring && result.phase !== 'measuring') {
                        result.phase = 'measuring'; last = null;
                      }
                      const begin = performance.now();
                      if (MOVE) {
                        const yaw = Math.round(8 * Math.sin(age * 2 * Math.PI / 20000));
                        if (yaw !== previousYaw) {
                          window.__a1MoveToYaw(yaw); previousYaw = yaw;
                          result.motionChanges++;
                        } else window.__a1RenderAgain();
                      } else window.__a1RenderAgain();
                      const end = performance.now();
                      if (measuring) {
                        if (last !== null) result.intervalsMs.push(time - last);
                        result.callbackTimesMs.push(time);
                        result.renderMs.push(end - begin);
                        result.measuredFrames++;
                      } else result.warmupFrames++;
                      last = time;
                      if (gl.isContextLost()) throw new Error('WebGL context lost');
                      requestAnimationFrame(tick);
                    } catch (error) {
                      result.errors.push(String(error)); result.phase = 'failed';
                      result.contextLost = gl.isContextLost();
                      result.glError = gl.getError();
                    }
                  };
                  requestAnimationFrame(tick);
                  return true;
                })()
                """.Replace("SECONDS", seconds.ToString(CultureInfo.InvariantCulture))
                    .Replace("MOVE", moveCamera ? "true" : "false");
            var value = await _view.CoreWebView2.ExecuteScriptAsync(script);
            if (value != "true") throw new InvalidOperationException("R14 loop did not start.");
        }

        public async Task<JsonElement> ReadR14()
        {
            var json = await _view.CoreWebView2.ExecuteScriptAsync(
                "JSON.stringify({display:window.__a1Status?.().display,...window.__r14})");
            using var outer = JsonDocument.Parse(json);
            using var inner = JsonDocument.Parse(outer.RootElement.GetString() ?? "null");
            return inner.RootElement.Clone();
        }
    }
}
#endif
