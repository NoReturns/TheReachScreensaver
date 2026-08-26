using OpenTK.Graphics.OpenGL4;
using OpenTK.Mathematics;
using TheReachScreensaver.Diagnostics;
using TheReachScreensaver.Display;
using TheReachScreensaver.Journey;

namespace TheReachScreensaver.Rendering;

internal sealed class SpaceRenderer : IDisposable
{
    private readonly StarfieldRenderer _stars;
    private readonly PlanetRenderer _planets = new();
    private readonly WarpTransitionRenderer _warp = new();
    private readonly FadeOverlay _fade = new();
    private readonly VanishingPointMarker? _vanishingPoint;
    private List<MonitorPane> _panes = [];
    private float _anchorX;
    private float _anchorY;
    private float _referenceHeight = 720f;
    private int _framebufferWidth;
    private int _framebufferHeight;
    private Vector3 _starfieldCameraPosition = Vector3.Zero;

    private const float StarfieldVisualSpeed = 3.5f;

    public Camera Camera { get; } = new();
    public float ReferenceHeight => _referenceHeight;

    private SpaceRenderer(StarfieldRenderer stars, bool showVanishingPoint)
    {
        _stars = stars;
        _vanishingPoint = showVanishingPoint ? new VanishingPointMarker() : null;
    }

    public static SpaceRenderer Create(int seed, bool showVanishingPoint)
    {
        var stars = StarfieldRenderer.Create(seed);
        return new SpaceRenderer(stars, showVanishingPoint);
    }

    public void ConfigureSurface(
        int framebufferWidth,
        int framebufferHeight,
        DisplayLayout layout,
        PixelRect windowRegion,
        bool renderMonitorPanes)
    {
        _framebufferWidth = Math.Max(framebufferWidth, 1);
        _framebufferHeight = Math.Max(framebufferHeight, 1);

        if (renderMonitorPanes)
        {
            _anchorX = layout.AnchorX;
            _anchorY = layout.AnchorY;
            _referenceHeight = Math.Max(layout.VisualAnchorMonitor.Bounds.Height, 1);
            _panes = BuildMonitorPanes(_framebufferWidth, _framebufferHeight, layout.VirtualBounds, layout.Displays);
            Log.Info($"Screensaver projection anchored at ({_anchorX:0.###},{_anchorY:0.###}), reference height {_referenceHeight}.");
        }
        else
        {
            _anchorX = windowRegion.Width / 2f;
            _anchorY = windowRegion.Height / 2f;
            _referenceHeight = Math.Max(windowRegion.Height, 1);
            _panes =
            [
                new MonitorPane(0, 0, _framebufferWidth, _framebufferHeight, windowRegion)
            ];
        }
    }

    public void AdvanceStarfield(double deltaSeconds)
    {
        if (deltaSeconds <= 0)
            return;

        var direction = Camera.Forward;
        _starfieldCameraPosition += direction * StarfieldVisualSpeed * (float)deltaSeconds;
        _starfieldCameraPosition = WrapStarfieldPosition(_starfieldCameraPosition);
    }

    public void Draw(JourneyController journey)
    {
        GL.Viewport(0, 0, _framebufferWidth, _framebufferHeight);
        GL.ClearColor(0.0035f, 0.0045f, 0.011f, 1f);
        GL.Clear(ClearBufferMask.ColorBufferBit | ClearBufferMask.DepthBufferBit);

        var view = Camera.ViewMatrix;
        var starView = Matrix4.LookAt(
            _starfieldCameraPosition,
            _starfieldCameraPosition + Camera.Forward,
            Camera.WorldUp);

        foreach (var pane in _panes)
        {
            GL.Viewport(pane.X, pane.Y, pane.Width, pane.Height);
            GL.Enable(EnableCap.ScissorTest);
            GL.Scissor(pane.X, pane.Y, pane.Width, pane.Height);

            var projection = Camera.ProjectionForRegion(pane.VirtualRect, _anchorX, _anchorY, _referenceHeight);
            var viewProjection = view * projection;
            var starViewProjection = starView * projection;

            GL.Disable(EnableCap.DepthTest);
            GL.DepthMask(false);
            GL.Enable(EnableCap.Blend);
            GL.BlendFunc(BlendingFactor.SrcAlpha, BlendingFactor.One);
            GL.Enable(EnableCap.ProgramPointSize);
            _stars.Draw(_starfieldCameraPosition, starViewProjection, _referenceHeight);

            _warp.Draw(
                journey.WarpIntensity,
                (float)journey.Time,
                _anchorX,
                _anchorY,
                pane.VirtualRect,
                pane.X,
                pane.Y,
                pane.Width,
                pane.Height);

            _planets.Draw(journey.Bodies, Camera, view, projection, journey.Time, _referenceHeight);

            _vanishingPoint?.Draw(Camera, view, projection, pane.Width, pane.Height);

            GL.Disable(EnableCap.ScissorTest);
        }

        GL.Viewport(0, 0, _framebufferWidth, _framebufferHeight);
        _fade.Draw(journey.Visibility);
        GL.BindVertexArray(0);
        GL.UseProgram(0);
    }

    public void Dispose()
    {
        _vanishingPoint?.Dispose();
        _fade.Dispose();
        _warp.Dispose();
        _planets.Dispose();
        _stars.Dispose();
    }

    private static List<MonitorPane> BuildMonitorPanes(
        int framebufferWidth,
        int framebufferHeight,
        PixelRect virtualDesktop,
        IReadOnlyList<DisplayRegion> displays)
    {
        var panes = new List<MonitorPane>(displays.Count);
        if (displays.Count == 0 || virtualDesktop.Width <= 0 || virtualDesktop.Height <= 0)
        {
            panes.Add(new MonitorPane(0, 0, framebufferWidth, framebufferHeight, virtualDesktop));
            return panes;
        }

        double scaleX = framebufferWidth / (double)virtualDesktop.Width;
        double scaleY = framebufferHeight / (double)virtualDesktop.Height;

        foreach (var display in displays)
        {
            var rect = display.Bounds;
            int x0 = (int)Math.Round((rect.Left - virtualDesktop.Left) * scaleX);
            int x1 = (int)Math.Round((rect.Right - virtualDesktop.Left) * scaleX);
            int yTop0 = (int)Math.Round((rect.Top - virtualDesktop.Top) * scaleY);
            int yTop1 = (int)Math.Round((rect.Bottom - virtualDesktop.Top) * scaleY);

            int x = Math.Clamp(Math.Min(x0, x1), 0, framebufferWidth);
            int width = Math.Clamp(Math.Abs(x1 - x0), 0, framebufferWidth - x);
            int yTop = Math.Min(yTop0, yTop1);
            int yBottom = Math.Max(yTop0, yTop1);
            int glY = framebufferHeight - yBottom;
            int height = yBottom - yTop;

            glY = Math.Clamp(glY, 0, framebufferHeight);
            height = Math.Clamp(height, 0, framebufferHeight - glY);
            if (width <= 0 || height <= 0)
                continue;

            panes.Add(new MonitorPane(x, glY, width, height, rect));
            Log.Info($"Monitor pane {display.Name}: viewport ({x},{glY}) {width}x{height} virtual {rect.Width}x{rect.Height} at ({rect.X},{rect.Y}).");
        }

        if (panes.Count == 0)
            panes.Add(new MonitorPane(0, 0, framebufferWidth, framebufferHeight, virtualDesktop));

        return panes;
    }

    private readonly record struct MonitorPane(int X, int Y, int Width, int Height, PixelRect VirtualRect);

    private static Vector3 WrapStarfieldPosition(Vector3 position)
    {
        var wrap = StarfieldRenderer.WrapSize;
        return new Vector3(
            WrapComponent(position.X, wrap.X),
            WrapComponent(position.Y, wrap.Y),
            WrapComponent(position.Z, wrap.Z));
    }

    private static float WrapComponent(float value, float size) =>
        size <= 0f ? value : value - size * MathF.Floor(value / size + 0.5f);
}
