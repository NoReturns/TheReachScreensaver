using System.Reflection;
using OpenTK.Graphics.OpenGL4;
using StbImageSharp;
using TheReachScreensaver.Diagnostics;

namespace TheReachScreensaver.Rendering;

internal sealed class GlTexture : IDisposable
{
    public int Handle { get; private set; }
    public int Width { get; }
    public int Height { get; }

    private GlTexture(int handle, int width, int height)
    {
        Handle = handle;
        Width = width;
        Height = height;
    }

    public static GlTexture? TryLoadEmbedded(string resourceName)
    {
        try
        {
            var assembly = Assembly.GetExecutingAssembly();
            using var stream = assembly.GetManifestResourceStream(resourceName);
            if (stream is null)
            {
                Log.Warn($"Embedded texture '{resourceName}' was not found.");
                return null;
            }

            using var memory = new MemoryStream();
            stream.CopyTo(memory);
            var bytes = memory.ToArray();

            StbImage.stbi_set_flip_vertically_on_load(0);
            var image = ImageResult.FromMemory(bytes, ColorComponents.RedGreenBlueAlpha);
            return Upload(image.Data, image.Width, image.Height, resourceName);
        }
        catch (Exception ex)
        {
            Log.Warn($"Failed to load texture '{resourceName}': {ex.Message}");
            return null;
        }
    }

    private static GlTexture Upload(byte[] rgba, int width, int height, string label)
    {
        var handle = GL.GenTexture();
        GL.BindTexture(TextureTarget.Texture2D, handle);
        GL.TexImage2D(
            TextureTarget.Texture2D,
            0,
            PixelInternalFormat.Rgba8,
            width,
            height,
            0,
            PixelFormat.Rgba,
            PixelType.UnsignedByte,
            rgba);

        // Longitude wraps; latitude clamps at the poles.
        GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureWrapS, (int)TextureWrapMode.Repeat);
        GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureWrapT, (int)TextureWrapMode.ClampToEdge);
        GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureMinFilter, (int)TextureMinFilter.LinearMipmapLinear);
        GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureMagFilter, (int)TextureMagFilter.Linear);
        GL.GenerateMipmap(GenerateMipmapTarget.Texture2D);
        GL.BindTexture(TextureTarget.Texture2D, 0);

        Log.Info($"Loaded texture '{label}' {width}x{height}.");
        return new GlTexture(handle, width, height);
    }

    public void Bind(TextureUnit unit)
    {
        GL.ActiveTexture(unit);
        GL.BindTexture(TextureTarget.Texture2D, Handle);
    }

    public void Dispose()
    {
        if (Handle != 0)
        {
            GL.DeleteTexture(Handle);
            Handle = 0;
        }
    }
}
