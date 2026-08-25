using OpenTK.Graphics.OpenGL4;

namespace TheReachScreensaver.Rendering;

internal sealed class FadeOverlay : IDisposable
{
    private readonly int _vao;
    private readonly int _vbo;
    private readonly ShaderProgram _shader;
    private readonly int _alphaLocation;

    public FadeOverlay()
    {
        _shader = new ShaderProgram(
            "TheReachScreensaver.Shaders.Fade.vert",
            "TheReachScreensaver.Shaders.Fade.frag");
        _alphaLocation = _shader.GetUniformLocation("uAlpha");

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

    public void Draw(float visibility)
    {
        var alpha = 1f - Math.Clamp(visibility, 0f, 1f);
        if (alpha <= 0.001f)
            return;

        GL.Disable(EnableCap.DepthTest);
        GL.Enable(EnableCap.Blend);
        GL.BlendFunc(BlendingFactor.SrcAlpha, BlendingFactor.OneMinusSrcAlpha);
        _shader.Use();
        GL.Uniform1(_alphaLocation, alpha);
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
