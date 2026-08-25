using System.Runtime.InteropServices;
using OpenTK.Graphics.OpenGL4;
using OpenTK.Mathematics;

namespace TheReachScreensaver.Rendering;

internal sealed class StarfieldRenderer : IDisposable
{
    public const int StarCount = 22000;
    public static readonly Vector3 WrapSize = new(820f, 520f, 380f);

    private readonly int _vao;
    private readonly int _vbo;
    private readonly ShaderProgram _shader;
    private readonly int _viewProjectionLocation;
    private readonly int _cameraPositionLocation;
    private readonly int _wrapSizeLocation;
    private readonly int _pixelScaleLocation;

    private StarfieldRenderer(int vao, int vbo, ShaderProgram shader)
    {
        _vao = vao;
        _vbo = vbo;
        _shader = shader;
        _viewProjectionLocation = shader.GetUniformLocation("uViewProjection");
        _cameraPositionLocation = shader.GetUniformLocation("uCameraPosition");
        _wrapSizeLocation = shader.GetUniformLocation("uWrapSize");
        _pixelScaleLocation = shader.GetUniformLocation("uPixelScale");
    }

    public static StarfieldRenderer Create(int seed)
    {
        var vertices = Generate(seed);
        var vao = GL.GenVertexArray();
        var vbo = GL.GenBuffer();

        GL.BindVertexArray(vao);
        GL.BindBuffer(BufferTarget.ArrayBuffer, vbo);
        GL.BufferData(BufferTarget.ArrayBuffer, vertices.Length * Marshal.SizeOf<StarVertex>(), vertices, BufferUsageHint.StaticDraw);

        var stride = Marshal.SizeOf<StarVertex>();
        GL.EnableVertexAttribArray(0);
        GL.VertexAttribPointer(0, 3, VertexAttribPointerType.Float, false, stride, 0);
        GL.EnableVertexAttribArray(1);
        GL.VertexAttribPointer(1, 4, VertexAttribPointerType.Float, false, stride, 3 * sizeof(float));

        GL.BindVertexArray(0);
        GL.BindBuffer(BufferTarget.ArrayBuffer, 0);

        var shader = new ShaderProgram(
            "TheReachScreensaver.Shaders.Stars.vert",
            "TheReachScreensaver.Shaders.Stars.frag");

        return new StarfieldRenderer(vao, vbo, shader);
    }

    public void Draw(Vector3 cameraPosition, Matrix4 viewProjection, float framebufferHeight)
    {
        _shader.Use();
        GL.UniformMatrix4(_viewProjectionLocation, transpose: true, ref viewProjection);
        GL.Uniform3(_cameraPositionLocation, cameraPosition);
        GL.Uniform3(_wrapSizeLocation, WrapSize);

        var pixelScale = MathF.Max(framebufferHeight * 0.085f, 42f);
        GL.Uniform1(_pixelScaleLocation, pixelScale);

        GL.BindVertexArray(_vao);
        GL.DrawArrays(PrimitiveType.Points, 0, StarCount);
        GL.BindVertexArray(0);
    }

    public void Dispose()
    {
        _shader.Dispose();
        GL.DeleteBuffer(_vbo);
        GL.DeleteVertexArray(_vao);
    }

    private static StarVertex[] Generate(int seed)
    {
        var rng = new Random(seed);
        var stars = new StarVertex[StarCount];
        for (var i = 0; i < StarCount; i++)
        {
            var position = new Vector3(
                (float)(rng.NextDouble() - 0.5) * WrapSize.X,
                (float)(rng.NextDouble() - 0.5) * WrapSize.Y,
                (float)(rng.NextDouble() - 0.5) * WrapSize.Z);

            var spectral = rng.NextDouble();
            Vector3 color;
            if (spectral < 0.12)
                color = new Vector3(1.00f, 0.78f, 0.58f);
            else if (spectral < 0.38)
                color = new Vector3(1.00f, 0.93f, 0.78f);
            else if (spectral < 0.78)
                color = new Vector3(0.95f, 0.97f, 1.00f);
            else
                color = new Vector3(0.72f, 0.82f, 1.00f);

            var brightness = MathF.Pow((float)rng.NextDouble(), 1.55f);
            var size = 0.7f + brightness * 2.8f + (float)rng.NextDouble() * 0.4f;
            stars[i] = new StarVertex(position, new Vector4(color, size));
        }

        return stars;
    }

    [StructLayout(LayoutKind.Sequential)]
    private readonly struct StarVertex(Vector3 position, Vector4 colorSize)
    {
        public readonly Vector3 Position = position;
        public readonly Vector4 ColorSize = colorSize;
    }
}
