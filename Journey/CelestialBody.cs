using OpenTK.Mathematics;
using TheReachScreensaver.Journey.Coordinates;
using Vector3d = TheReachScreensaver.Journey.Coordinates.Vector3d;

namespace TheReachScreensaver.Journey;

internal sealed class CelestialBody
{
    public required CelestialId Id { get; init; }
    public required string Name { get; init; }

    /// <summary>Physical radius in AU.</summary>
    public required double Radius { get; init; }

    public Vector3d WorldPosition { get; set; }

    public required Vector3 Albedo { get; init; }
    public PlanetStyle Style { get; init; }
    public float EncounterTime { get; init; }

    /// <summary>Authored closest-approach standoff in AU.</summary>
    public double FlybyDistanceAu { get; init; }

    public bool IsSatellite { get; init; }

    public bool UnresolvedVisible { get; init; }
    public float UnresolvedBrightness { get; init; } = 1f;
    public Vector3 UnresolvedColor { get; init; }

    public Vector3 RotationAxis { get; init; } = Vector3.UnitY;
    public float RotationRate { get; init; }
    public float InitialRotation { get; init; }

    /// <summary>Ring radii in AU from planet center.</summary>
    public double RingInnerRadiusAu { get; init; }
    public double RingOuterRadiusAu { get; init; }
    public Vector3 RingColor { get; init; } = new(0.78f, 0.70f, 0.55f);
    public float RingOpacity { get; init; } = 0.72f;

    public bool HasRings => RingOuterRadiusAu > RingInnerRadiusAu && RingInnerRadiusAu > 0;

    public Vector3d RenderPosition(Vector3d cameraWorldPosition) =>
        WorldPosition - cameraWorldPosition;

    public Matrix3 LocalToWorld(double journeySeconds)
    {
        var pole = RotationAxis.LengthSquared > 1e-10f ? Vector3.Normalize(RotationAxis) : Vector3.UnitY;
        var tilt = AlignLocalYTo(pole);
        var angle = InitialRotation + RotationRate * (float)journeySeconds;
        var spin = AxisAngle(pole, angle);
        return spin * tilt;
    }

    public Vector3 PoleVector() =>
        RotationAxis.LengthSquared > 1e-10f ? Vector3.Normalize(RotationAxis) : Vector3.UnitY;

    public static Vector3 PoleFromTilt(float tiltDegrees, float yawDegrees)
    {
        var tilt = MathHelper.DegreesToRadians(tiltDegrees);
        var yaw = MathHelper.DegreesToRadians(yawDegrees);
        var x = MathF.Sin(tilt);
        var y = MathF.Cos(tilt);
        var c = MathF.Cos(yaw);
        var s = MathF.Sin(yaw);
        return Vector3.Normalize(new Vector3(c * x, y, -s * x));
    }

    private static Matrix3 AlignLocalYTo(Vector3 worldAxis)
    {
        var to = Vector3.Normalize(worldAxis);
        var from = Vector3.UnitY;
        var dot = Vector3.Dot(from, to);
        if (dot > 0.9999f)
            return Matrix3.Identity;
        if (dot < -0.9999f)
            return AxisAngle(Vector3.UnitX, MathF.PI);

        var axis = Vector3.Normalize(Vector3.Cross(from, to));
        var angle = MathF.Acos(Math.Clamp(dot, -1f, 1f));
        return AxisAngle(axis, angle);
    }

    private static Matrix3 AxisAngle(Vector3 axis, float angle)
    {
        axis.Normalize();
        var c = MathF.Cos(angle);
        var s = MathF.Sin(angle);
        var t = 1f - c;
        var x = axis.X;
        var y = axis.Y;
        var z = axis.Z;
        return new Matrix3(
            t * x * x + c, t * x * y - s * z, t * x * z + s * y,
            t * x * y + s * z, t * y * y + c, t * y * z - s * x,
            t * x * z - s * y, t * y * z + s * x, t * z * z + c);
    }
}
