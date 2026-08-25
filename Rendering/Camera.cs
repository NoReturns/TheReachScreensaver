using OpenTK.Mathematics;
using TheReachScreensaver.Diagnostics;
using TheReachScreensaver.Display;
using TheReachScreensaver.Journey.Coordinates;
using Vector3d = TheReachScreensaver.Journey.Coordinates.Vector3d;

namespace TheReachScreensaver.Rendering;

internal sealed class Camera
{
    private static bool _loggedFirstProjection;
    /// <summary>Authoritative camera position in AU (heliocentric).</summary>
    public Vector3d WorldPosition { get; set; }

    /// <summary>Render-space position is always origin (camera-relative / floating origin).</summary>
    public Vector3 Position => Vector3.Zero;

    public Vector3 LookDirection { get; set; } = -Vector3.UnitZ;
    public Vector3 WorldUp { get; set; } = Vector3.UnitY;
    public float FieldOfViewY { get; set; } = MathHelper.DegreesToRadians(55f);

    /// <summary>Near/far in AU for camera-relative render space.</summary>
    public float NearPlane { get; set; } = 1e-7f;
    public float FarPlane { get; set; } = 150f;

    public Vector3 Forward
    {
        get
        {
            var dir = LookDirection;
            return dir.LengthSquared > 1e-12f ? Vector3.Normalize(dir) : -Vector3.UnitZ;
        }
    }

    public Matrix4 ViewMatrix => Matrix4.LookAt(Vector3.Zero, Forward, WorldUp);

    /// <summary>
    /// Off-axis slice of the master frustum.
    /// Camera forward (NDC 0,0) is the ray through (<paramref name="anchorX"/>, <paramref name="anchorY"/>)
    /// in virtual-desktop pixels. Vertical FOV is measured against <paramref name="referenceHeightPixels"/>
    /// (the visual-anchor monitor height in /s).
    /// </summary>
    public Matrix4 ProjectionForRegion(
        PixelRect region,
        float anchorX,
        float anchorY,
        float referenceHeightPixels)
    {
        float worldPerPixel = MathF.Tan(FieldOfViewY * 0.5f) * NearPlane * 2f /
                              MathF.Max(referenceHeightPixels, 1f);

        float left = (region.Left - anchorX) * worldPerPixel;
        float right = (region.Right - anchorX) * worldPerPixel;
        float top = (anchorY - region.Top) * worldPerPixel;
        float bottom = (anchorY - region.Bottom) * worldPerPixel;

        if (right <= left)
        {
            var minimumSpan = MathF.Max(MathF.Abs(worldPerPixel), float.Epsilon);
            right = left + minimumSpan;
        }

        if (top <= bottom)
        {
            var minimumSpan = MathF.Max(MathF.Abs(worldPerPixel), float.Epsilon);
            bottom = top - minimumSpan;
        }

        if (!_loggedFirstProjection)
        {
            _loggedFirstProjection = true;
            Log.Info(
                $"ProjectionForRegion: NearPlane={NearPlane:E3}, worldPerPixel={worldPerPixel:E3}, " +
                $"region={region.Width}x{region.Height}, left={left:E3}, right={right:E3}, " +
                $"bottom={bottom:E3}, top={top:E3}, spanX={right - left:E3}, spanY={top - bottom:E3}.");
        }

        return Matrix4.CreatePerspectiveOffCenter(left, right, bottom, top, NearPlane, FarPlane);
    }
}
