using OpenTK.Graphics.OpenGL4;
using TheReachScreensaver.Display;

namespace TheReachScreensaver.Rendering;

/// <summary>
/// Departure-only warp overlay. Separate from StarfieldRenderer / Stars shaders.
/// Radial effect is centered on the shared virtual-desktop visual anchor (middle monitor).
/// </summary>
internal sealed class WarpTransitionRenderer : IDisposable
{
    private readonly int _vao;
    private readonly int _vbo;
    private readonly ShaderProgram _shader;
    private readonly int _intensityLocation;
    private readonly int _anchorLocation;
    private readonly int _paneVirtualLocation;
    private readonly int _paneFramebufferLocation;
    private readonly int _timeLocation;

    public WarpTransitionRenderer()
    {
        _shader = new ShaderProgram(
            "TheReachScreensaver.Shaders.Warp.vert",
            "TheReachScreensaver.Shaders.Warp.frag");
        _intensityLocation = _shader.GetUniformLocation("uIntensity");
        _anchorLocation = _shader.GetUniformLocation("uAnchorVirtual");
        _paneVirtualLocation = _shader.GetUniformLocation("uPaneVirtual");
        _paneFramebufferLocation = _shader.GetUniformLocation("uPaneFramebuffer");
        _timeLocation = _shader.GetUniformLocation("uTime");

        float[] quad = [-1f, -1f, 3f, -1f, -1f, 3f];
        _vao = GL.GenVertexArray();
        _vbo = GL.GenBuffer();
        GL.BindVertexArray(_vao);
        GL.BindBuffer(BufferTarget.ArrayBuffer, _vbo);
        GL.BufferData(BufferTarget.ArrayBuffer, quad.Length * sizeof(float), quad, BufferUsageHint.StaticDraw);
        GL.EnableVertexAttribArray(0);
        GL.VertexAttribPointer(0, 2, VertexAttribPointerType.Float, false, 2 * sizeof(float), 0);
        GL.BindVertexArray(0);
    }

    public void Draw(
        float intensity,
        float journeySeconds,
        float anchorVirtualX,
        float anchorVirtualY,
        PixelRect paneVirtual,
        int paneFramebufferX,
        int paneFramebufferY,
        int paneFramebufferWidth,
        int paneFramebufferHeight)
    {
        if (intensity <= 0.001f)
            return;

        GL.Disable(EnableCap.DepthTest);
        GL.DepthMask(false);
        GL.Enable(EnableCap.Blend);
        GL.BlendFunc(BlendingFactor.SrcAlpha, BlendingFactor.One);

        _shader.Use();
        GL.Uniform1(_intensityLocation, intensity);
        GL.Uniform2(_anchorLocation, anchorVirtualX, anchorVirtualY);
        GL.Uniform4(
            _paneVirtualLocation,
            paneVirtual.Left,
            paneVirtual.Top,
            paneVirtual.Width,
            paneVirtual.Height);
        GL.Uniform4(
            _paneFramebufferLocation,
            paneFramebufferX,
            paneFramebufferY,
            paneFramebufferWidth,
            paneFramebufferHeight);
        GL.Uniform1(_timeLocation, journeySeconds);

        GL.BindVertexArray(_vao);
        GL.DrawArrays(PrimitiveType.Triangles, 0, 3);
        GL.BindVertexArray(0);
        GL.UseProgram(0);
    }

    public void Dispose()
    {
        _shader.Dispose();
        GL.DeleteBuffer(_vbo);
        GL.DeleteVertexArray(_vao);
    }
}
