using OpenTK.Graphics.OpenGL4;

namespace TheReachScreensaver.Rendering;

internal sealed class RingMesh : IDisposable
{
    public int Vao { get; }
    public int VertexCount { get; }
    private readonly int _vbo;

    private RingMesh(int vao, int vbo, int vertexCount)
    {
        Vao = vao;
        _vbo = vbo;
        VertexCount = vertexCount;
    }

    /// <summary>
    /// Annulus in the local XZ plane, radii in units of the parent planet radius.
    /// Three stacked copies give the sheet a little thickness so a grazing
    /// view still rasterizes instead of disappearing edge-on.
    /// </summary>
    public static RingMesh Create(int segments = 96, int rings = 6)
    {
        segments = Math.Max(segments, 24);
        rings = Math.Max(rings, 2);
        const float inner = 1.05f;
        const float outer = 2.45f;
        float[] heights = [-0.045f, 0f, 0.045f];
        var vertices = new List<float>(segments * rings * heights.Length * 18);

        foreach (var height in heights)
        {
            for (var y = 0; y < rings; y++)
            {
                var v0 = y / (float)rings;
                var v1 = (y + 1) / (float)rings;
                var r0 = inner + (outer - inner) * v0;
                var r1 = inner + (outer - inner) * v1;
                for (var x = 0; x < segments; x++)
                {
                    var u0 = x / (float)segments;
                    var u1 = (x + 1) / (float)segments;
                    var a0 = u0 * MathF.PI * 2f;
                    var a1 = u1 * MathF.PI * 2f;
                    Add(vertices, r0, a0, height);
                    Add(vertices, r1, a0, height);
                    Add(vertices, r0, a1, height);
                    Add(vertices, r0, a1, height);
                    Add(vertices, r1, a0, height);
                    Add(vertices, r1, a1, height);
                }
            }
        }

        var vao = GL.GenVertexArray();
        var vbo = GL.GenBuffer();
        GL.BindVertexArray(vao);
        GL.BindBuffer(BufferTarget.ArrayBuffer, vbo);
        GL.BufferData(BufferTarget.ArrayBuffer, vertices.Count * sizeof(float), vertices.ToArray(), BufferUsageHint.StaticDraw);
        GL.EnableVertexAttribArray(0);
        GL.VertexAttribPointer(0, 3, VertexAttribPointerType.Float, false, 3 * sizeof(float), 0);
        GL.BindVertexArray(0);

        return new RingMesh(vao, vbo, vertices.Count / 3);
    }

    public void Draw()
    {
        GL.BindVertexArray(Vao);
        GL.DrawArrays(PrimitiveType.Triangles, 0, VertexCount);
        GL.BindVertexArray(0);
    }

    public void Dispose()
    {
        GL.DeleteBuffer(_vbo);
        GL.DeleteVertexArray(Vao);
    }

    private static void Add(List<float> vertices, float radius, float angle, float height)
    {
        vertices.Add(MathF.Cos(angle) * radius);
        vertices.Add(height);
        vertices.Add(MathF.Sin(angle) * radius);
    }
}
