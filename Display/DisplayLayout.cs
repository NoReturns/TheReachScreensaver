using System.Runtime.InteropServices;
using OpenTK.Windowing.Desktop;
using TheReachScreensaver.Diagnostics;
using TheReachScreensaver.Native;

namespace TheReachScreensaver.Display;

internal sealed class DisplayLayout
{
    public PixelRect VirtualBounds { get; }
    public IReadOnlyList<DisplayRegion> Displays { get; }
    public IReadOnlyList<DisplayRegion> DisplaysLeftToRight { get; }
    public DisplayRegion VisualAnchorMonitor { get; }
    public float AnchorX { get; }
    public float AnchorY { get; }

    public DisplayLayout(PixelRect virtualBounds, IReadOnlyList<DisplayRegion> displays)
    {
        VirtualBounds = virtualBounds;
        Displays = displays;
        DisplaysLeftToRight = displays
            .OrderBy(d => d.Bounds.X + d.Bounds.Width / 2.0)
            .ThenBy(d => d.Bounds.Y)
            .ToList();
        VisualAnchorMonitor = SelectVisualAnchor(DisplaysLeftToRight);
        AnchorX = VisualAnchorMonitor.Bounds.Left + VisualAnchorMonitor.Bounds.Width / 2f;
        AnchorY = VisualAnchorMonitor.Bounds.Top + VisualAnchorMonitor.Bounds.Height / 2f;
        LogLayout();
    }

    private static DisplayRegion SelectVisualAnchor(IReadOnlyList<DisplayRegion> leftToRight)
    {
        if (leftToRight.Count == 0)
        {
            return new DisplayRegion
            {
                Bounds = new PixelRect(0, 0, 1280, 720),
                Name = "fallback",
                IsPrimary = true
            };
        }

        return leftToRight[(leftToRight.Count - 1) / 2];
    }

    private void LogLayout()
    {
        Log.Info($"Virtual desktop {VirtualBounds.Width}x{VirtualBounds.Height} at ({VirtualBounds.X},{VirtualBounds.Y}); {Displays.Count} display(s).");
        var index = 1;
        foreach (var display in DisplaysLeftToRight)
        {
            var cx = display.Bounds.Left + display.Bounds.Width / 2f;
            var cy = display.Bounds.Top + display.Bounds.Height / 2f;
            var role = ReferenceEquals(display, VisualAnchorMonitor) ? "ANCHOR" : "";
            Log.Info($"  L→R {index}: {display.Name} {display.Bounds.Width}x{display.Bounds.Height} at ({display.Bounds.X},{display.Bounds.Y}) center=({cx:0.#},{cy:0.#}) dpi={display.DpiX:0}/{display.DpiY:0} primary={display.IsPrimary} {role}");
            index++;
        }

        Log.Info($"Visual anchor monitor '{VisualAnchorMonitor.Name}' center=({AnchorX:0.###},{AnchorY:0.###}) bounds {VisualAnchorMonitor.Bounds.Width}x{VisualAnchorMonitor.Bounds.Height} at ({VisualAnchorMonitor.Bounds.X},{VisualAnchorMonitor.Bounds.Y}).");
    }

    public static DisplayLayout Query()
    {
        try
        {
            if (OperatingSystem.IsWindows())
            {
                var windows = QueryWindows();
                if (windows.Displays.Count > 0)
                    return windows;
            }

            return QueryOpenTk();
        }
        catch (Exception ex)
        {
            Log.Warn($"Display enumeration failed ({ex.Message}); using a 1280x720 fallback.");
            return Fallback();
        }
    }

    public static DisplayLayout Fallback()
    {
        var bounds = new PixelRect(0, 0, 1280, 720);
        return new DisplayLayout(bounds, [new DisplayRegion
        {
            Bounds = bounds,
            Name = "fallback",
            IsPrimary = true
        }]);
    }

    private static DisplayLayout QueryWindows()
    {
        var displays = new List<DisplayRegion>();
        Win32.MonitorEnumProc callback = (IntPtr hMonitor, IntPtr hdc, ref Win32.Rect rect, IntPtr data) =>
        {
            var info = new Win32.MonitorInfoEx
            {
                Size = Marshal.SizeOf<Win32.MonitorInfoEx>(),
                DeviceName = string.Empty
            };

            if (!Win32.GetMonitorInfoW(hMonitor, ref info))
                return true;

            float dpiX = 96f;
            float dpiY = 96f;
            try
            {
                if (Win32.GetDpiForMonitor(hMonitor, Win32.MdtEffectiveDpi, out var x, out var y) == 0)
                {
                    dpiX = x;
                    dpiY = y;
                }
            }
            catch (DllNotFoundException)
            {
                // Windows 7 / missing shcore: keep 96.
            }

            displays.Add(new DisplayRegion
            {
                Bounds = new PixelRect(info.Monitor.Left, info.Monitor.Top, info.Monitor.Width, info.Monitor.Height),
                Name = string.IsNullOrWhiteSpace(info.DeviceName) ? $"monitor-{displays.Count + 1}" : info.DeviceName,
                DpiX = dpiX,
                DpiY = dpiY,
                IsPrimary = (info.Flags & Win32.MonitorInfoPrimary) != 0
            });
            return true;
        };

        Win32.EnumDisplayMonitors(IntPtr.Zero, IntPtr.Zero, callback, IntPtr.Zero);

        var virtualBounds = new PixelRect(
            Win32.GetSystemMetrics(Win32.SmXVirtualScreen),
            Win32.GetSystemMetrics(Win32.SmYVirtualScreen),
            Win32.GetSystemMetrics(Win32.SmCxVirtualScreen),
            Win32.GetSystemMetrics(Win32.SmCyVirtualScreen));

        if (virtualBounds.Width <= 0 || virtualBounds.Height <= 0)
        {
            if (displays.Count == 0)
                return Fallback();

            int left = displays.Min(d => d.Bounds.Left);
            int top = displays.Min(d => d.Bounds.Top);
            int right = displays.Max(d => d.Bounds.Right);
            int bottom = displays.Max(d => d.Bounds.Bottom);
            virtualBounds = new PixelRect(left, top, right - left, bottom - top);
        }

        return new DisplayLayout(virtualBounds, displays);
    }

    private static DisplayLayout QueryOpenTk()
    {
        GLFWProvider.EnsureInitialized();
        var monitors = Monitors.GetMonitors();
        if (monitors.Count == 0)
            return Fallback();

        var displays = new List<DisplayRegion>(monitors.Count);
        int left = int.MaxValue, top = int.MaxValue, right = int.MinValue, bottom = int.MinValue;

        foreach (var monitor in monitors)
        {
            var area = monitor.ClientArea;
            var bounds = new PixelRect(area.Min.X, area.Min.Y, area.Size.X, area.Size.Y);
            left = Math.Min(left, bounds.Left);
            top = Math.Min(top, bounds.Top);
            right = Math.Max(right, bounds.Right);
            bottom = Math.Max(bottom, bounds.Bottom);

            displays.Add(new DisplayRegion
            {
                Bounds = bounds,
                Name = monitor.Name ?? $"monitor-{displays.Count + 1}",
                DpiX = monitor.HorizontalDpi,
                DpiY = monitor.VerticalDpi,
                IsPrimary = displays.Count == 0
            });
        }

        var virtualBounds = new PixelRect(left, top, right - left, bottom - top);
        return new DisplayLayout(virtualBounds, displays);
    }
}
