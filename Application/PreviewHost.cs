using OpenTK.Windowing.Desktop;
using OpenTK.Windowing.GraphicsLibraryFramework;
using TheReachScreensaver.Diagnostics;
using TheReachScreensaver.Native;
using TheReachScreensaver.Rendering;

namespace TheReachScreensaver.Application;

/// <summary>
/// Parents the OpenTK/GLFW window into the Windows Screen Saver Settings preview HWND.
/// </summary>
internal sealed class PreviewHost
{
    private readonly IntPtr _parentHwnd;
    private IntPtr _childHwnd;

    public PreviewHost(IntPtr parentHwnd)
    {
        _parentHwnd = parentHwnd;
    }

    public bool TryAttach(SpaceWindow window)
    {
        if (!OperatingSystem.IsWindows())
        {
            Log.Warn("Preview embedding is only implemented on Windows.");
            return false;
        }

        if (_parentHwnd == IntPtr.Zero || !Win32.IsWindow(_parentHwnd))
        {
            Log.Warn("Preview parent HWND is missing or already destroyed.");
            return false;
        }

        _childHwnd = GetWin32Hwnd(window);
        if (_childHwnd == IntPtr.Zero)
        {
            Log.Warn("Could not retrieve the GLFW Win32 HWND for preview parenting.");
            return false;
        }

        var style = Win32.GetWindowLongPtr(_childHwnd, Win32.GwlStyle).ToInt64();
        style &= ~(Win32.WsPopup | Win32.WsCaption | Win32.WsThickFrame | Win32.WsSysMenu |
                   Win32.WsMinimizeBox | Win32.WsMaximizeBox | Win32.WsBorder | Win32.WsDlgFrame);
        style |= Win32.WsChild | Win32.WsVisible;
        Win32.SetWindowLongPtr(_childHwnd, Win32.GwlStyle, new IntPtr(unchecked((int)style)));

        var exStyle = Win32.GetWindowLongPtr(_childHwnd, Win32.GwlExStyle).ToInt64();
        exStyle &= ~(Win32.WsExAppWindow | Win32.WsExTopmost);
        exStyle |= Win32.WsExToolWindow;
        Win32.SetWindowLongPtr(_childHwnd, Win32.GwlExStyle, new IntPtr(unchecked((int)exStyle)));

        Win32.SetParent(_childHwnd, _parentHwnd);
        SyncSize(window);
        Log.Info($"Preview window {_childHwnd.ToInt64()} parented to {_parentHwnd.ToInt64()}.");
        return true;
    }

    public bool IsParentAlive()
    {
        return OperatingSystem.IsWindows() && _parentHwnd != IntPtr.Zero && Win32.IsWindow(_parentHwnd);
    }

    public void SyncSize(SpaceWindow window)
    {
        if (!OperatingSystem.IsWindows() || _childHwnd == IntPtr.Zero)
            return;
        if (!Win32.GetClientRect(_parentHwnd, out var rect))
            return;

        Win32.SetWindowPos(
            _childHwnd,
            IntPtr.Zero,
            0,
            0,
            Math.Max(rect.Width, 1),
            Math.Max(rect.Height, 1),
            Win32.SwpNoZOrder | Win32.SwpNoActivate | Win32.SwpFrameChanged | Win32.SwpShowWindow);

        try
        {
            window.ClientSize = new OpenTK.Mathematics.Vector2i(Math.Max(rect.Width, 1), Math.Max(rect.Height, 1));
        }
        catch
        {
            // GLFW may already match the size.
        }
    }

    private static unsafe IntPtr GetWin32Hwnd(SpaceWindow window)
    {
        return GLFW.GetWin32Window(window.WindowPtr);
    }
}
