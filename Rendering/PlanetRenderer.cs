using System.Runtime.InteropServices;
using OpenTK.Graphics.OpenGL4;
using OpenTK.Mathematics;
using TheReachScreensaver.Journey;
using Vector3d = TheReachScreensaver.Journey.Coordinates.Vector3d;

namespace TheReachScreensaver.Rendering;

internal sealed class PlanetRenderer : IDisposable
{
    private readonly SphereMesh _mesh;
    private readonly RingMesh _rings;
    private readonly ShaderProgram _shader;
    private readonly ShaderProgram _ringShader;
    private readonly ShaderProgram _pointShader;
    private readonly int _viewProjectionLocation;
    private readonly int _centerLocation;
    private readonly int _radiusLocation;
    private readonly int _rotationLocation;
    private readonly int _albedoLocation;
    private readonly int _lightLocation;
    private readonly int _cameraLocation;
    private readonly int _styleLocation;
    private readonly int _alphaLocation;
    private readonly int _ringViewProjectionLocation;
    private readonly int _ringCenterLocation;
    private readonly int _ringPoleLocation;
    private readonly int _ringPlanetRadiusLocation;
    private readonly int _ringLightLocation;
    private readonly int _ringCameraLocation;
    private readonly int _ringColorLocation;
    private readonly int _ringInnerLocation;
    private readonly int _ringOuterLocation;
    private readonly int _ringOpacityLocation;
    private readonly int _pointViewProjectionLocation;
    private readonly int _pointPixelScaleLocation;
    private readonly int _pointGlobalAlphaLocation;
    private readonly int _pointVao;
    private readonly int _pointVbo;
    private readonly PointVertex[] _pointScratch = new PointVertex[32];

    public PlanetRenderer()
    {
        _mesh = SphereMesh.Create(48, 28);
        _rings = RingMesh.Create();
        _shader = new ShaderProgram(
            "TheReachScreensaver.Shaders.Planet.vert",
            "TheReachScreensaver.Shaders.Planet.frag");
        _ringShader = new ShaderProgram(
            "TheReachScreensaver.Shaders.Ring.vert",
            "TheReachScreensaver.Shaders.Ring.frag");
        _pointShader = new ShaderProgram(
            "TheReachScreensaver.Shaders.BodyPoint.vert",
            "TheReachScreensaver.Shaders.BodyPoint.frag");
        _viewProjectionLocation = _shader.GetUniformLocation("uViewProjection");
        _centerLocation = _shader.GetUniformLocation("uCenter");
        _radiusLocation = _shader.GetUniformLocation("uRadius");
        _rotationLocation = _shader.GetUniformLocation("uRotation");
        _albedoLocation = _shader.GetUniformLocation("uAlbedo");
        _lightLocation = _shader.GetUniformLocation("uLightDir");
        _cameraLocation = _shader.GetUniformLocation("uCameraPos");
        _styleLocation = _shader.GetUniformLocation("uStyle");
        _alphaLocation = _shader.GetUniformLocation("uAlpha");
        _ringViewProjectionLocation = _ringShader.GetUniformLocation("uViewProjection");
        _ringCenterLocation = _ringShader.GetUniformLocation("uCenter");
        _ringPoleLocation = _ringShader.GetUniformLocation("uPole");
        _ringPlanetRadiusLocation = _ringShader.GetUniformLocation("uPlanetRadius");
        _ringLightLocation = _ringShader.GetUniformLocation("uLightDir");
        _ringCameraLocation = _ringShader.GetUniformLocation("uCameraPos");
        _ringColorLocation = _ringShader.GetUniformLocation("uRingColor");
        _ringInnerLocation = _ringShader.GetUniformLocation("uInner");
        _ringOuterLocation = _ringShader.GetUniformLocation("uOuter");
        _ringOpacityLocation = _ringShader.GetUniformLocation("uOpacity");
        _pointViewProjectionLocation = _pointShader.GetUniformLocation("uViewProjection");
        _pointPixelScaleLocation = _pointShader.GetUniformLocation("uPixelScale");
        _pointGlobalAlphaLocation = _pointShader.GetUniformLocation("uGlobalAlpha");

        _pointVao = GL.GenVertexArray();
        _pointVbo = GL.GenBuffer();
        GL.BindVertexArray(_pointVao);
        GL.BindBuffer(BufferTarget.ArrayBuffer, _pointVbo);
        GL.BufferData(BufferTarget.ArrayBuffer, _pointScratch.Length * Marshal.SizeOf<PointVertex>(), IntPtr.Zero, BufferUsageHint.DynamicDraw);
        var stride = Marshal.SizeOf<PointVertex>();
        GL.EnableVertexAttribArray(0);
        GL.VertexAttribPointer(0, 3, VertexAttribPointerType.Float, false, stride, 0);
        GL.EnableVertexAttribArray(1);
        GL.VertexAttribPointer(1, 4, VertexAttribPointerType.Float, false, stride, 3 * sizeof(float));
        GL.BindVertexArray(0);
    }

    public void Draw(
        IReadOnlyList<CelestialBody> bodies,
        Camera camera,
        Matrix4 view,
        Matrix4 projection,
        double journeySeconds,
        float referenceHeightPixels)
    {
        GL.Enable(EnableCap.DepthTest);
        GL.DepthMask(true);

        var viewProjection = view * projection;
        var cameraWorld = camera.WorldPosition;
        var fovY = camera.FieldOfViewY;
        var pointCount = 0;

        foreach (var body in bodies)
        {
            var appearance = BodyAppearance.Evaluate(body, cameraWorld, referenceHeightPixels, fovY);
            if (appearance.Mode is BodyRenderMode.Point or BodyRenderMode.Transition && appearance.PointAlpha > 0.01)
            {
                if (pointCount >= _pointScratch.Length)
                    continue;

                var renderPos = body.RenderPosition(cameraWorld).ToVector3();
                var brightness = body.UnresolvedBrightness * (float)appearance.PointAlpha;
                _pointScratch[pointCount++] = new PointVertex(
                    renderPos,
                    new Vector4(body.UnresolvedColor * brightness, brightness));
            }
        }

        DrawPoints(viewProjection, referenceHeightPixels, pointCount);
        DrawSpheres(bodies, camera, viewProjection, journeySeconds, cameraWorld, referenceHeightPixels, fovY);
        DrawRings(bodies, camera, viewProjection, cameraWorld, referenceHeightPixels, fovY);
    }

    public void Dispose()
    {
        GL.DeleteBuffer(_pointVbo);
        GL.DeleteVertexArray(_pointVao);
        _pointShader.Dispose();
        _ringShader.Dispose();
        _shader.Dispose();
        _rings.Dispose();
        _mesh.Dispose();
    }

    private void DrawSpheres(
        IReadOnlyList<CelestialBody> bodies,
        Camera camera,
        Matrix4 viewProjection,
        double journeySeconds,
        Vector3d cameraWorld,
        float referenceHeightPixels,
        float fovY)
    {
        _shader.Use();
        GL.UniformMatrix4(_viewProjectionLocation, transpose: true, ref viewProjection);
        GL.Uniform3(_cameraLocation, camera.Position);

        foreach (var body in bodies)
        {
            var appearance = BodyAppearance.Evaluate(body, cameraWorld, referenceHeightPixels, fovY);
            if (appearance.Mode is not BodyRenderMode.Sphere and not BodyRenderMode.Transition)
                continue;
            if (appearance.SphereBlend <= 0.01)
                continue;

            DrawSphere(body, cameraWorld, journeySeconds, (float)appearance.SphereBlend);
        }
    }

    private void DrawSphere(CelestialBody body, Vector3d cameraWorld, double journeySeconds, float alpha)
    {
        if (alpha < 0.999f)
        {
            GL.Enable(EnableCap.Blend);
            GL.BlendFunc(BlendingFactor.SrcAlpha, BlendingFactor.OneMinusSrcAlpha);
            GL.DepthMask(false);
        }
        else
        {
            GL.Disable(EnableCap.Blend);
            GL.DepthMask(true);
        }

        var renderPos = body.RenderPosition(cameraWorld).ToVector3();
        var rotation = body.LocalToWorld(journeySeconds);
        var light = SolarSystem.LightDirectionAt(body.WorldPosition, cameraWorld);
        var radius = (float)body.Radius;

        GL.Uniform3(_centerLocation, renderPos);
        GL.Uniform1(_radiusLocation, radius);
        UniformMatrix3(_rotationLocation, rotation);
        GL.Uniform3(_albedoLocation, body.Albedo);
        GL.Uniform3(_lightLocation, light);
        GL.Uniform1(_styleLocation, (int)body.Style);
        GL.Uniform1(_alphaLocation, alpha);
        _mesh.Draw();

        if (alpha < 0.999f)
        {
            GL.DepthMask(true);
            GL.Disable(EnableCap.Blend);
        }
    }

    private void DrawRings(
        IReadOnlyList<CelestialBody> bodies,
        Camera camera,
        Matrix4 viewProjection,
        Vector3d cameraWorld,
        float referenceHeightPixels,
        float fovY)
    {
        var any = false;
        foreach (var body in bodies)
        {
            var appearance = BodyAppearance.Evaluate(body, cameraWorld, referenceHeightPixels, fovY);
            if (appearance.ShouldDrawRings(body))
            {
                any = true;
                break;
            }
        }

        if (!any)
            return;

        GL.Enable(EnableCap.Blend);
        GL.BlendFunc(BlendingFactor.SrcAlpha, BlendingFactor.OneMinusSrcAlpha);
        GL.DepthMask(false);
        GL.Disable(EnableCap.CullFace);

        _ringShader.Use();
        GL.UniformMatrix4(_ringViewProjectionLocation, transpose: true, ref viewProjection);
        GL.Uniform3(_ringCameraLocation, camera.Position);

        foreach (var body in bodies)
        {
            var appearance = BodyAppearance.Evaluate(body, cameraWorld, referenceHeightPixels, fovY);
            if (!appearance.ShouldDrawRings(body))
                continue;

            var renderPos = body.RenderPosition(cameraWorld).ToVector3();
            var pole = body.PoleVector();
            var light = SolarSystem.LightDirectionAt(body.WorldPosition, cameraWorld);
            var ringOpacity = body.RingOpacity * (float)appearance.SphereBlend;

            GL.Uniform3(_ringCenterLocation, renderPos);
            GL.Uniform3(_ringPoleLocation, pole);
            GL.Uniform1(_ringPlanetRadiusLocation, (float)body.Radius);
            GL.Uniform3(_ringLightLocation, light);
            GL.Uniform3(_ringColorLocation, body.RingColor);
            GL.Uniform1(_ringInnerLocation, (float)body.RingInnerRadiusAu);
            GL.Uniform1(_ringOuterLocation, (float)body.RingOuterRadiusAu);
            GL.Uniform1(_ringOpacityLocation, ringOpacity);
            _rings.Draw();
        }

        GL.DepthMask(true);
        GL.Disable(EnableCap.Blend);
    }

    private void DrawPoints(Matrix4 viewProjection, float referenceHeightPixels, int pointCount)
    {
        if (pointCount <= 0)
            return;

        GL.Enable(EnableCap.Blend);
        GL.BlendFunc(BlendingFactor.SrcAlpha, BlendingFactor.One);
        GL.Enable(EnableCap.ProgramPointSize);
        GL.DepthMask(false);

        _pointShader.Use();
        GL.UniformMatrix4(_pointViewProjectionLocation, transpose: true, ref viewProjection);
        var pixelScale = MathF.Max(referenceHeightPixels * 0.055f, 28f);
        GL.Uniform1(_pointPixelScaleLocation, pixelScale);
        GL.Uniform1(_pointGlobalAlphaLocation, 1f);

        GL.BindVertexArray(_pointVao);
        GL.BindBuffer(BufferTarget.ArrayBuffer, _pointVbo);
        GL.BufferSubData(BufferTarget.ArrayBuffer, IntPtr.Zero, pointCount * Marshal.SizeOf<PointVertex>(), _pointScratch);
        GL.DrawArrays(PrimitiveType.Points, 0, pointCount);
        GL.BindVertexArray(0);

        GL.DepthMask(true);
        GL.Disable(EnableCap.ProgramPointSize);
        GL.Disable(EnableCap.Blend);
    }

    private static void UniformMatrix3(int location, Matrix3 matrix)
    {
        GL.UniformMatrix3(location, transpose: true, ref matrix);
    }

    [StructLayout(LayoutKind.Sequential)]
    private readonly struct PointVertex(Vector3 position, Vector4 colorSize)
    {
        public readonly Vector3 Position = position;
        public readonly Vector4 ColorSize = colorSize;
    }
}
