using OpenTK.Mathematics;
using OpenTK.Windowing.GraphicsLibraryFramework;

namespace TheReachScreensaver.Application;

/// <summary>
/// Screensaver-mode input: ignore startup jitter, then exit on a real key, click, or move.
/// </summary>
internal sealed class ExitMonitor
{
    private const double ArmDelaySeconds = 0.75;
    private const float MoveThresholdPixels = 16f;
    private const float WarpResetPixels = 240f;

    private Vector2 _anchor;
    private bool _hasAnchor;
    private bool _armed;
    private double _elapsed;

    public bool ShouldExit { get; private set; }
    public string? Reason { get; private set; }

    public void Observe(Vector2 mousePosition, MouseState mouse, KeyboardState keyboard, double deltaSeconds)
    {
        if (ShouldExit)
            return;

        _elapsed += deltaSeconds;
        if (!_hasAnchor)
        {
            _anchor = mousePosition;
            _hasAnchor = true;
        }

        if (!_armed)
        {
            _anchor = mousePosition;
            if (_elapsed >= ArmDelaySeconds)
                _armed = true;
            return;
        }

        if (keyboard.IsAnyKeyDown)
        {
            Signal("keyboard");
            return;
        }

        for (var button = MouseButton.Button1; button <= MouseButton.Button8; button++)
        {
            if (mouse.IsButtonDown(button))
            {
                Signal("mouse button");
                return;
            }
        }

        var delta = mousePosition - _anchor;
        var distance = delta.Length;
        if (distance >= WarpResetPixels)
        {
            _anchor = mousePosition;
            return;
        }

        if (distance >= MoveThresholdPixels)
            Signal("mouse movement");
    }

    private void Signal(string reason)
    {
        ShouldExit = true;
        Reason = reason;
    }
}
