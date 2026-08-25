using System.Reflection;
using OpenTK.Graphics.OpenGL4;
using TheReachScreensaver.Diagnostics;

namespace TheReachScreensaver.Rendering;

internal sealed class ShaderProgram : IDisposable
{
    public int Handle { get; }

    public ShaderProgram(string vertexResourceName, string fragmentResourceName)
    {
        var vertexSource = ReadEmbedded(vertexResourceName);
        var fragmentSource = ReadEmbedded(fragmentResourceName);

        var vertex = Compile(ShaderType.VertexShader, vertexSource);
        var fragment = Compile(ShaderType.FragmentShader, fragmentSource);

        Handle = GL.CreateProgram();
        GL.AttachShader(Handle, vertex);
        GL.AttachShader(Handle, fragment);
        GL.LinkProgram(Handle);

        GL.GetProgram(Handle, GetProgramParameterName.LinkStatus, out var status);
        if (status == 0)
        {
            var log = GL.GetProgramInfoLog(Handle);
            GL.DeleteShader(vertex);
            GL.DeleteShader(fragment);
            GL.DeleteProgram(Handle);
            throw new InvalidOperationException("Shader program failed to link: " + log);
        }

        GL.DetachShader(Handle, vertex);
        GL.DetachShader(Handle, fragment);
        GL.DeleteShader(vertex);
        GL.DeleteShader(fragment);
        Log.Info("Shader program linked.");
    }

    public void Use() => GL.UseProgram(Handle);

    public int GetUniformLocation(string name)
    {
        var location = GL.GetUniformLocation(Handle, name);
        if (location < 0)
            Log.Warn($"Uniform '{name}' was not found.");
        return location;
    }

    public void Dispose()
    {
        if (Handle != 0)
            GL.DeleteProgram(Handle);
    }

    private static int Compile(ShaderType type, string source)
    {
        var shader = GL.CreateShader(type);
        GL.ShaderSource(shader, source);
        GL.CompileShader(shader);
        GL.GetShader(shader, ShaderParameter.CompileStatus, out var status);
        if (status == 0)
        {
            var log = GL.GetShaderInfoLog(shader);
            GL.DeleteShader(shader);
            throw new InvalidOperationException($"{type} compile failed: {log}");
        }

        return shader;
    }

    private static string ReadEmbedded(string resourceName)
    {
        var assembly = Assembly.GetExecutingAssembly();
        using var stream = assembly.GetManifestResourceStream(resourceName)
            ?? throw new FileNotFoundException($"Embedded shader '{resourceName}' was not found. Resources: {string.Join(", ", assembly.GetManifestResourceNames())}");
        using var reader = new StreamReader(stream);
        return reader.ReadToEnd();
    }
}
