using TheReachScreensaver.Journey.Coordinates;
using Vector3d = TheReachScreensaver.Journey.Coordinates.Vector3d;

namespace TheReachScreensaver.Journey;

internal readonly record struct BodyAppearance(
    BodyRenderMode Mode,
    double ProjectedRadiusPixels,
    double SphereBlend,
    double PointAlpha,
    double DistanceAu,
    double DistanceKm)
{
    public const double SphereThresholdPixels = 2.0;
    public const double PointThresholdPixels = 0.5;

    public static BodyAppearance Evaluate(
        CelestialBody body,
        Vector3d cameraWorldPosition,
        double referenceHeightPixels,
        double fieldOfViewYRadians)
    {
        var delta = body.WorldPosition - cameraWorldPosition;
        var distanceAu = delta.Length;
        var distanceKm = AstronomicalUnits.AuToKilometers(distanceAu);
        var projectedRadius = ComputeProjectedRadiusPixels(body.Radius, distanceAu, referenceHeightPixels, fieldOfViewYRadians);

        if (projectedRadius >= SphereThresholdPixels)
            return new BodyAppearance(BodyRenderMode.Sphere, projectedRadius, 1, 0, distanceAu, distanceKm);

        if (projectedRadius >= PointThresholdPixels)
        {
            var t = SmoothStep(PointThresholdPixels, SphereThresholdPixels, projectedRadius);
            return new BodyAppearance(BodyRenderMode.Transition, projectedRadius, t, 1.0 - t, distanceAu, distanceKm);
        }

        if (body.UnresolvedVisible)
            return new BodyAppearance(BodyRenderMode.Point, projectedRadius, 0, 1, distanceAu, distanceKm);

        return new BodyAppearance(BodyRenderMode.Hidden, projectedRadius, 0, 0, distanceAu, distanceKm);
    }

    public static double ComputeProjectedRadiusPixels(
        double bodyRadiusAu,
        double distanceAu,
        double referenceHeightPixels,
        double fieldOfViewYRadians)
    {
        if (distanceAu <= 1e-20 || bodyRadiusAu <= 0)
            return 0;
        var angularRadius = Math.Atan2(bodyRadiusAu, distanceAu);
        var tanHalfFov = Math.Tan(fieldOfViewYRadians * 0.5);
        if (tanHalfFov <= 1e-12)
            return 0;
        return angularRadius / tanHalfFov * referenceHeightPixels * 0.5;
    }

    public bool ShouldDrawRings(CelestialBody body) =>
        body.HasRings && Mode is BodyRenderMode.Sphere or BodyRenderMode.Transition && SphereBlend > 0.35;

    private static double SmoothStep(double edge0, double edge1, double x)
    {
        if (edge1 <= edge0)
            return x >= edge1 ? 1 : 0;
        var t = Math.Clamp((x - edge0) / (edge1 - edge0), 0, 1);
        return t * t * (3 - 2 * t);
    }
}
