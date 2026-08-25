using System.Runtime.InteropServices;
using OpenTK.Mathematics;
using OpenTK.Windowing.Common;
using OpenTK.Windowing.Desktop;
using OpenTK.Windowing.GraphicsLibraryFramework;
using TheReachScreensaver.Application;
using TheReachScreensaver.Diagnostics;
using TheReachScreensaver.Display;
using TheReachScreensaver.Journey;
using TheReachScreensaver.Native;
using TheReachScreensaver.Persistence;

namespace TheReachScreensaver.Rendering;

internal sealed class SpaceWindow : GameWindow
{
    private const double SaveIntervalSeconds = 30.0;

    private readonly ScreensaverMode _mode;
    private readonly DisplayLayout _layout;
    private readonly JourneyState _state;
    private readonly JourneyStateStore _store;
    private readonly ExitMonitor _exitMonitor = new();
    private readonly PreviewHost? _preview;
    private readonly JourneyController _journey;
    private readonly List<IDisposable> _signalRegistrations = [];

    private SpaceRenderer? _renderer;
    private double _timeSinceSave;
    private double _fpsWindow;
    private int _fpsFrames;
    private bool _initialized;

    private SpaceWindow(
        GameWindowSettings gameSettings,
        NativeWindowSettings nativeSettings,
        ScreensaverMode mode,
        DisplayLayout layout,
        JourneyState state,
        JourneyStateStore store,
        IntPtr previewParent)
        : base(gameSettings, nativeSettings)
    {
        _mode = mode;
        _layout = layout;
        _state = state;
        _store = store;
        _state.JourneySeconds = JourneyController.Normalize(_state.JourneySeconds);
        _journey = new JourneyController(_state.JourneySeconds);
        _preview = mode == ScreensaverMode.Preview ? new PreviewHost(previewParent) : null;
        HookShutdownSignals();
    }

    public static SpaceWindow Create(
        ScreensaverMode mode,
        DisplayLayout layout,
        JourneyState state,
        JourneyStateStore store,
        IntPtr previewParent)
    {
        var game = new GameWindowSettings
        {
            UpdateFrequency = 60
        };

        var native = new NativeWindowSettings
        {
            Title = mode == ScreensaverMode.Development ? "The Reach Screensaver" : "The Reach Screensaver",
            API = ContextAPI.OpenGL,
            APIVersion = new Version(3, 3),
            Profile = ContextProfile.Core,
            Flags = ContextFlags.ForwardCompatible,
            NumberOfSamples = 0,
            StencilBits = 0,
            DepthBits = 24,
            StartVisible = true,
            StartFocused = mode != ScreensaverMode.Preview,
            WindowBorder = mode == ScreensaverMode.Development ? WindowBorder.Resizable : WindowBorder.Hidden,
            WindowState = WindowState.Normal,
            Vsync = VSyncMode.Off,
            AutoIconify = false
        };

        if (mode == ScreensaverMode.Screensaver)
        {
            native.Location = new Vector2i(layout.VirtualBounds.X, layout.VirtualBounds.Y);
            native.ClientSize = new Vector2i(
                Math.Max(layout.VirtualBounds.Width, 64),
                Math.Max(layout.VirtualBounds.Height, 64));
        }
        else if (mode == ScreensaverMode.Preview)
        {
            var size = new Vector2i(320, 240);
            if (OperatingSystem.IsWindows() && previewParent != IntPtr.Zero &&
                Win32.GetClientRect(previewParent, out var rect) && rect.Width > 0 && rect.Height > 0)
            {
                size = new Vector2i(rect.Width, rect.Height);
            }

            native.ClientSize = size;
            native.StartFocused = false;
        }
        else
        {
            native.ClientSize = new Vector2i(1280, 720);
        }

        return new SpaceWindow(game, native, mode, layout, state, store, previewParent);
    }

    protected override void OnLoad()
    {
        base.OnLoad();

        try
        {
            if (_mode == ScreensaverMode.Screensaver)
            {
                AlwaysOnTop = true;
                ApplyScreensaverChrome();
            }

            if (_preview is not null && !_preview.TryAttach(this))
            {
                Log.Warn("Preview attachment failed; closing without showing a full-screen window.");
                Close();
                return;
            }

            if (_mode == ScreensaverMode.Screensaver)
                CursorState = CursorState.Hidden;

            _renderer = SpaceRenderer.Create(
                _state.Seed,
                showVanishingPoint: _mode == ScreensaverMode.Development);
            ApplyRendererResize();
            _initialized = true;
            Log.Info($"OpenGL renderer ready. Framebuffer {FramebufferSize.X}x{FramebufferSize.Y}.");
        }
        catch (Exception ex)
        {
            Log.Error("Renderer initialization failed.", ex);
            if (_mode == ScreensaverMode.Development)
                ConfigurationDialog.ShowError("OpenGL initialization failed.\n\n" + ex.Message);
            Close();
        }
    }

    protected override void OnUnload()
    {
        Console.CancelKeyPress -= OnCancelKeyPress;
        foreach (var registration in _signalRegistrations)
            registration.Dispose();
        _signalRegistrations.Clear();
        Persist("shutdown");
        _renderer?.Dispose();
        _renderer = null;
        base.OnUnload();
    }

    protected override void OnResize(ResizeEventArgs e)
    {
        base.OnResize(e);
        ApplyRendererResize();
    }

    protected override void OnUpdateFrame(FrameEventArgs args)
    {
        base.OnUpdateFrame(args);
        if (!_initialized)
            return;

        if (_preview is not null)
        {
            if (!_preview.IsParentAlive())
            {
                Close();
                return;
            }

            _preview.SyncSize(this);
        }

        var dt = Math.Clamp(args.Time, 0.0, 0.1);
        _journey.Advance(dt);
        _journey.Apply(_renderer!.Camera, dt, smoothLook: true);
        _state.JourneySeconds = _journey.Time;
        _timeSinceSave += dt;
        if (_timeSinceSave >= SaveIntervalSeconds)
        {
            Persist("periodic");
            _timeSinceSave = 0;
        }

        if (_mode == ScreensaverMode.Screensaver)
        {
            _exitMonitor.Observe(MousePosition, MouseState, KeyboardState, dt);
            if (_exitMonitor.ShouldExit)
            {
                Log.Info($"Screensaver exit: {_exitMonitor.Reason}.");
                Close();
                return;
            }
        }
        else if (_mode == ScreensaverMode.Development && KeyboardState.IsKeyDown(Keys.Escape))
        {
            Close();
            return;
        }

        if (_mode == ScreensaverMode.Development)
        {
            _fpsWindow += dt;
            _fpsFrames++;
            if (_fpsWindow >= 0.5)
            {
                var fps = _fpsFrames / _fpsWindow;
                var diag = _journey.BuildDevDiagnostics(_renderer!.Camera, _renderer.ReferenceHeight);
                Title = $"The Reach Screensaver — {diag} — {fps:0} FPS";
                _fpsWindow = 0;
                _fpsFrames = 0;
            }
        }
    }

    protected override void OnRenderFrame(FrameEventArgs args)
    {
        base.OnRenderFrame(args);
        if (!_initialized || _renderer is null)
            return;

        _renderer.Draw(_journey);
        SwapBuffers();
    }

    protected override void OnKeyDown(KeyboardKeyEventArgs e)
    {
        base.OnKeyDown(e);
        if (_mode == ScreensaverMode.Screensaver)
        {
            Log.Info($"Screensaver exit: key {e.Key}.");
            Close();
            return;
        }

        if (_mode != ScreensaverMode.Development)
            return;

        if (e.Key == Keys.Escape)
        {
            Close();
            return;
        }

        if (HandleDevelopmentSeek(e.Key))
        {
            _journey.Apply(_renderer!.Camera, 0, smoothLook: false);
            _state.JourneySeconds = _journey.Time;
        }
    }

    protected override void OnMouseDown(MouseButtonEventArgs e)
    {
        base.OnMouseDown(e);
        if (_mode == ScreensaverMode.Screensaver)
        {
            Log.Info($"Screensaver exit: mouse {e.Button}.");
            Close();
        }
    }

    private bool HandleDevelopmentSeek(Keys key)
    {
        switch (key)
        {
            case Keys.D1:
            case Keys.KeyPad1:
                _journey.Seek(1.5);
                return true;
            case Keys.D2:
            case Keys.KeyPad2:
                _journey.Seek(SolarSystem.MoonEncounter);
                return true;
            case Keys.D3:
            case Keys.KeyPad3:
                _journey.Seek(SolarSystem.MarsEncounter);
                return true;
            case Keys.D4:
            case Keys.KeyPad4:
                _journey.Seek(592.0);
                return true;
            case Keys.D5:
            case Keys.KeyPad5:
                _journey.Seek(804.0);
                return true;
            case Keys.D6:
            case Keys.KeyPad6:
                _journey.Seek(1132.0);
                return true;
            case Keys.D7:
            case Keys.KeyPad7:
                _journey.Seek(1492.0);
                return true;
            case Keys.D8:
            case Keys.KeyPad8:
                _journey.Seek(1792.0);
                return true;
            case Keys.PageUp:
                _journey.Seek(_journey.Time + 15.0);
                return true;
            case Keys.PageDown:
                _journey.Seek(_journey.Time - 15.0);
                return true;
            default:
                return false;
        }
    }

    private PixelRect VisibleRegion()
    {
        if (_mode == ScreensaverMode.Screensaver)
            return _layout.VirtualBounds;

        var size = FramebufferSize;
        if (size.X <= 0 || size.Y <= 0)
            size = ClientSize;
        return new PixelRect(0, 0, Math.Max(size.X, 1), Math.Max(size.Y, 1));
    }

    private void ApplyRendererResize()
    {
        _renderer?.ConfigureSurface(
            FramebufferSize.X,
            FramebufferSize.Y,
            _layout,
            VisibleRegion(),
            renderMonitorPanes: _mode == ScreensaverMode.Screensaver);
    }

    private void HookShutdownSignals()
    {
        Console.CancelKeyPress += OnCancelKeyPress;
        if (OperatingSystem.IsWindows())
            return;

        try
        {
            _signalRegistrations.Add(PosixSignalRegistration.Create(PosixSignal.SIGINT, OnPosixStop));
            _signalRegistrations.Add(PosixSignalRegistration.Create(PosixSignal.SIGTERM, OnPosixStop));
        }
        catch (Exception ex)
        {
            Log.Warn($"Could not register POSIX shutdown signals: {ex.Message}");
        }
    }

    private void OnCancelKeyPress(object? sender, ConsoleCancelEventArgs e)
    {
        e.Cancel = true;
        Close();
    }

    private void OnPosixStop(PosixSignalContext context)
    {
        context.Cancel = true;
        Close();
    }

    private void ApplyScreensaverChrome()
    {
        Location = new Vector2i(_layout.VirtualBounds.X, _layout.VirtualBounds.Y);
        ClientSize = new Vector2i(
            Math.Max(_layout.VirtualBounds.Width, 64),
            Math.Max(_layout.VirtualBounds.Height, 64));

        if (!OperatingSystem.IsWindows())
            return;

        unsafe
        {
            var hwnd = GLFW.GetWin32Window(WindowPtr);
            if (hwnd == IntPtr.Zero)
                return;

            var style = Win32.GetWindowLongPtr(hwnd, Win32.GwlStyle).ToInt64();
            style &= ~(Win32.WsCaption | Win32.WsThickFrame | Win32.WsSysMenu |
                       Win32.WsMinimizeBox | Win32.WsMaximizeBox | Win32.WsBorder);
            Win32.SetWindowLongPtr(hwnd, Win32.GwlStyle, new IntPtr(unchecked((int)style)));

            var ex = Win32.GetWindowLongPtr(hwnd, Win32.GwlExStyle).ToInt64();
            ex |= Win32.WsExToolWindow | Win32.WsExTopmost;
            ex &= ~Win32.WsExAppWindow;
            Win32.SetWindowLongPtr(hwnd, Win32.GwlExStyle, new IntPtr(unchecked((int)ex)));

            Win32.SetWindowPos(
                hwnd,
                Win32.HwndTopmost,
                _layout.VirtualBounds.X,
                _layout.VirtualBounds.Y,
                Math.Max(_layout.VirtualBounds.Width, 64),
                Math.Max(_layout.VirtualBounds.Height, 64),
                Win32.SwpFrameChanged | Win32.SwpShowWindow);
        }
    }

    private void Persist(string reason)
    {
        try
        {
            _store.Save(_state);
            Log.Info($"Saved journey state ({reason}): seconds={_state.JourneySeconds:0.###}.");
        }
        catch (Exception ex)
        {
            Log.Warn($"Save during {reason} failed: {ex.Message}");
        }
    }
}
