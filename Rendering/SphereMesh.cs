using OpenTK.Graphics.OpenGL4;

namespace TheReachScreensaver.Rendering;

internal sealed class SphereMesh : IDisposable
{
    public int Vao { get; }
    public int IndexCount { get; }
    private readonly int _vbo;
    private readonly int _ebo;

    private SphereMesh(int vao, int vbo, int ebo, int indexCount)
    {
        Vao = vao;
        _vbo = vbo;
        _ebo = ebo;
        IndexCount = indexCount;
    }

    public static SphereMesh Create(int slices, int stacks)
    {
        slices = Math.Max(slices, 8);
        stacks = Math.Max(stacks, 6);
        var vertices = new List<float>((slices + 1) * (stacks + 1) * 6);
        for (var y = 0; y <= stacks; y++)
        {
            var v = y / (float)stacks;
            var pitch = MathF.PI * (0.5f - v);
            var cy = MathF.Sin(pitch);
            var ring = MathF.Cos(pitch);
            for (var x = 0; x <= slices; x++)
            {
                var u = x / (float)slices;
                var yaw = u * MathF.PI * 2f;
                var nx = ring * MathF.Cos(yaw);
                var nz = ring * MathF.Sin(yaw);
                vertices.Add(nx);
                vertices.Add(cy);
                vertices.Add(nz);
                vertices.Add(nx);
                vertices.Add(cy);
                vertices.Add(nz);
            }
        }

        var indices = new List<uint>(slices * stacks * 6);
        for (var y = 0; y < stacks; y++)
        {
            for (var x = 0; x < slices; x++)
            {
                var a = (uint)(y * (slices + 1) + x);
                var b = (uint)((y + 1) * (slices + 1) + x);
                indices.Add(a);
                indices.Add(b);
                indices.Add(a + 1);
                indices.Add(b);
                indices.Add(b + 1);
                indices.Add(a + 1);
            }
        }

        var vao = GL.GenVertexArray();
        var vbo = GL.GenBuffer();
        var ebo = GL.GenBuffer();
        GL.BindVertexArray(vao);
        GL.BindBuffer(BufferTarget.ArrayBuffer, vbo);
        GL.BufferData(BufferTarget.ArrayBuffer, vertices.Count * sizeof(float), vertices.ToArray(), BufferUsageHint.StaticDraw);
        GL.BindBuffer(BufferTarget.ElementArrayBuffer, ebo);
        GL.BufferData(BufferTarget.ElementArrayBuffer, indices.Count * sizeof(uint), indices.ToArray(), BufferUsageHint.StaticDraw);
        const int stride = 6 * sizeof(float);
        GL.EnableVertexAttribArray(0);
        GL.VertexAttribPointer(0, 3, VertexAttribPointerType.Float, false, stride, 0);
        GL.EnableVertexAttribArray(1);
        GL.VertexAttribPointer(1, 3, VertexAttribPointerType.Float, false, stride, 3 * sizeof(float));
        GL.BindVertexArray(0);

        return new SphereMesh(vao, vbo, ebo, indices.Count);
    }

    public void Draw()
    {
        GL.BindVertexArray(Vao);
        GL.DrawElements(PrimitiveType.Triangles, IndexCount, DrawElementsType.UnsignedInt, 0);
        GL.BindVertexArray(0);
    }

    public void Dispose()
    {
        GL.DeleteBuffer(_ebo);
        GL.DeleteBuffer(_vbo);
        GL.DeleteVertexArray(Vao);
    }
}
