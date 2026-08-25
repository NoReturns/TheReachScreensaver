using OpenTK.Graphics.OpenGL4;
using OpenTK.Mathematics;

namespace TheReachScreensaver.Rendering;

/// <summary>
/// Development-only marker at the shared camera's true forward / vanishing direction.
/// Slice 1.1 diagnostic — delete this type (and its Draw call) when it is no longer needed.
/// </summary>
internal sealed class VanishingPointMarker : IDisposable
{
    public const bool Enabled = true;

    private readonly int _vao;
    private readonly int _vbo;
    private readonly int _program;
    private readonly int _colorLocation;
    private readonly float[] _lineVertices = new float[8];
    private bool _disposed;

    public VanishingPointMarker()
    {
        _program = Compile(
            """
            #version 330 core
            layout (location = 0) in vec2 aNdc;
            void main()
            {
                gl_Position = vec4(aNdc, 0.0, 1.0);
            }
            """,
            """
            #version 330 core
            uniform vec3 uColor;
            out vec4 FragColor;
            void main()
            {
                FragColor = vec4(uColor, 1.0);
            }
            """);
        _colorLocation = GL.GetUniformLocation(_program, "uColor");

        _vao = GL.GenVertexArray();
        _vbo = GL.GenBuffer();
        GL.BindVertexArray(_vao);
        GL.BindBuffer(BufferTarget.ArrayBuffer, _vbo);
        GL.BufferData(BufferTarget.ArrayBuffer, 8 * sizeof(float), IntPtr.Zero, BufferUsageHint.DynamicDraw);
        GL.EnableVertexAttribArray(0);
        GL.VertexAttribPointer(0, 2, VertexAttribPointerType.Float, false, 2 * sizeof(float), 0);
        GL.BindVertexArray(0);
        GL.BindBuffer(BufferTarget.ArrayBuffer, 0);
    }

    public void Draw(Camera camera, Matrix4 view, Matrix4 projection, int viewportWidth, int viewportHeight)
    {
        if (!Enabled || viewportWidth <= 0 || viewportHeight <= 0)
            return;

        var world = new Vector4(camera.Position + camera.Forward * 12f, 1f);
        var clip = world * view * projection;
        if (clip.W <= 0.0001f)
            return;

        var cx = clip.X / clip.W;
        var cy = clip.Y / clip.W;
        if (cx is < -1.2f or > 1.2f || cy is < -1.2f or > 1.2f)
            return;

        var armX = 14f / viewportWidth * 2f;
        var armY = 14f / viewportHeight * 2f;
        _lineVertices[0] = cx - armX;
        _lineVertices[1] = cy;
        _lineVertices[2] = cx + armX;
        _lineVertices[3] = cy;
        _lineVertices[4] = cx;
        _lineVertices[5] = cy - armY;
        _lineVertices[6] = cx;
        _lineVertices[7] = cy + armY;

        GL.Disable(EnableCap.Blend);
        GL.Disable(EnableCap.ProgramPointSize);
        GL.UseProgram(_program);
        GL.Uniform3(_colorLocation, 0.82f, 0.90f, 1.00f);
        GL.BindVertexArray(_vao);
        GL.BindBuffer(BufferTarget.ArrayBuffer, _vbo);
        GL.BufferSubData(BufferTarget.ArrayBuffer, IntPtr.Zero, _lineVertices.Length * sizeof(float), _lineVertices);
        GL.DrawArrays(PrimitiveType.Lines, 0, 4);
        GL.BindVertexArray(0);
        GL.UseProgram(0);
    }

    public void Dispose()
    {
        if (_disposed)
            return;
        _disposed = true;
        GL.DeleteBuffer(_vbo);
        GL.DeleteVertexArray(_vao);
        GL.DeleteProgram(_program);
    }

    private static int Compile(string vertexSource, string fragmentSource)
    {
        var vertex = GL.CreateShader(ShaderType.VertexShader);
        GL.ShaderSource(vertex, vertexSource);
        GL.CompileShader(vertex);
        GL.GetShader(vertex, ShaderParameter.CompileStatus, out var vertexOk);
        if (vertexOk == 0)
            throw new InvalidOperationException("Vanishing-point vertex shader failed: " + GL.GetShaderInfoLog(vertex));

        var fragment = GL.CreateShader(ShaderType.FragmentShader);
        GL.ShaderSource(fragment, fragmentSource);
        GL.CompileShader(fragment);
        GL.GetShader(fragment, ShaderParameter.CompileStatus, out var fragmentOk);
        if (fragmentOk == 0)
            throw new InvalidOperationException("Vanishing-point fragment shader failed: " + GL.GetShaderInfoLog(fragment));

        var program = GL.CreateProgram();
        GL.AttachShader(program, vertex);
        GL.AttachShader(program, fragment);
        GL.LinkProgram(program);
        GL.GetProgram(program, GetProgramParameterName.LinkStatus, out var linked);
        GL.DeleteShader(vertex);
        GL.DeleteShader(fragment);
        if (linked == 0)
            throw new InvalidOperationException("Vanishing-point program failed to link: " + GL.GetProgramInfoLog(program));

        return program;
    }
}
