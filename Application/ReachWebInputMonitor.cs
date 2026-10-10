using System.ComponentModel;
using System.Diagnostics;
using System.Runtime.InteropServices;
using TheReachScreensaver.Diagnostics;

namespace TheReachScreensaver.Application;

/// <summary>Consumes native input while the WebView2 screensaver is active.</summary>
internal sealed class ReachWebInputMonitor : IDisposable
{
    private const int WhKeyboardLl = 13;
    private const int WhMouseLl = 14;
    private const int WmKeyDown = 0x0100;
    private const int WmSysKeyDown = 0x0104;
    private const int WmMouseMove = 0x0200;
    private const int WmLButtonDown = 0x0201;
    private const int WmRButtonDown = 0x0204;
    private const int WmMButtonDown = 0x0207;
    private const int WmXButtonDown = 0x020B;
    private const int StartupGraceMs = 750;
    private const int MoveThresholdSquared = 16 * 16;

    private readonly Action<string> _exit;
    private readonly HookProc _keyboardProc;
    private readonly HookProc _mouseProc;
    private readonly Stopwatch _clock = new();
    private IntPtr _keyboardHook;
    private IntPtr _mouseHook;
    private Point _anchor;
    private int _ignoredStartupMoves;
    private bool _exitRequested;

    public ReachWebInputMonitor(Action<string> exit)
    {
        _exit = exit;
        _keyboardProc = OnKeyboard;
        _mouseProc = OnMouse;
    }

    public void Start()
    {
        if (!GetCursorPos(out _anchor))
            throw new Win32Exception(Marshal.GetLastWin32Error(), "Could not read the cursor position.");

        var module = GetModuleHandle(null);
        _keyboardHook = SetWindowsHookEx(WhKeyboardLl, _keyboardProc, module, 0);
        if (_keyboardHook == IntPtr.Zero)
            throw new Win32Exception(Marshal.GetLastWin32Error(), "Could not install the keyboard exit hook.");

        _mouseHook = SetWindowsHookEx(WhMouseLl, _mouseProc, module, 0);
        if (_mouseHook == IntPtr.Zero)
        {
            var error = Marshal.GetLastWin32Error();
            Dispose();
            throw new Win32Exception(error, "Could not install the mouse exit hook.");
        }

        _clock.Start();
        Log.Info($"Reach WebView2 native input armed after {StartupGraceMs} ms and {Math.Sqrt(MoveThresholdSquared):0} px of movement.");
    }

    private IntPtr OnKeyboard(int code, IntPtr message, IntPtr data)
    {
        if (code < 0) return CallNextHookEx(_keyboardHook, code, message, data);
        if (message == new IntPtr(WmKeyDown) || message == new IntPtr(WmSysKeyDown))
            RequestExit("keyboard");
        return new IntPtr(1);
    }

    private IntPtr OnMouse(int code, IntPtr message, IntPtr data)
    {
        if (code < 0) return CallNextHookEx(_mouseHook, code, message, data);
        if (message == new IntPtr(WmMouseMove))
        {
            var point = Marshal.PtrToStructure<MouseHookData>(data).Position;
            if (_clock.ElapsedMilliseconds < StartupGraceMs)
            {
                _anchor = point;
                _ignoredStartupMoves++;
            }
            else
            {
                var dx = (long)point.X - _anchor.X;
                var dy = (long)point.Y - _anchor.Y;
                if (dx * dx + dy * dy >= MoveThresholdSquared)
                    RequestExit("mouse movement");
            }
        }
        else if (message == new IntPtr(WmLButtonDown) || message == new IntPtr(WmRButtonDown) ||
                 message == new IntPtr(WmMButtonDown) || message == new IntPtr(WmXButtonDown))
        {
            RequestExit("mouse button");
        }
        return new IntPtr(1);
    }

    private void RequestExit(string reason)
    {
        if (_exitRequested) return;
        _exitRequested = true;
        _exit(reason);
    }

    public void Dispose()
    {
        if (_mouseHook != IntPtr.Zero) UnhookWindowsHookEx(_mouseHook);
        if (_keyboardHook != IntPtr.Zero) UnhookWindowsHookEx(_keyboardHook);
        _mouseHook = IntPtr.Zero;
        _keyboardHook = IntPtr.Zero;
        if (_clock.IsRunning)
            Log.Info($"Reach WebView2 native input stopped; ignored {_ignoredStartupMoves} startup mouse moves.");
        _clock.Stop();
    }

    private delegate IntPtr HookProc(int code, IntPtr message, IntPtr data);

    [StructLayout(LayoutKind.Sequential)]
    private struct Point { public int X; public int Y; }

    [StructLayout(LayoutKind.Sequential)]
    private struct MouseHookData
    {
        public Point Position;
        public uint MouseData;
        public uint Flags;
        public uint Time;
        public IntPtr ExtraInfo;
    }

    [DllImport("user32.dll", SetLastError = true)]
    private static extern bool GetCursorPos(out Point point);

    [DllImport("user32.dll", SetLastError = true)]
    private static extern IntPtr SetWindowsHookEx(int hookId, HookProc callback, IntPtr module, uint threadId);

    [DllImport("user32.dll", SetLastError = true)]
    private static extern bool UnhookWindowsHookEx(IntPtr hook);

    [DllImport("user32.dll")]
    private static extern IntPtr CallNextHookEx(IntPtr hook, int code, IntPtr message, IntPtr data);

    [DllImport("kernel32.dll", CharSet = CharSet.Unicode)]
    private static extern IntPtr GetModuleHandle(string? moduleName);
}
