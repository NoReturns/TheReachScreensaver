using TheReachScreensaver.Journey.Coordinates;
using Vector3d = TheReachScreensaver.Journey.Coordinates.Vector3d;

namespace TheReachScreensaver.Journey;

internal sealed class JourneyPath
{
    private readonly Waypoint[] _points;

    public JourneyPath()
    {
        _points = BuildWaypoints();
    }

    public (Vector3d Position, Vector3d Tangent) Evaluate(double journeySeconds)
    {
        if (!double.IsFinite(journeySeconds))
            journeySeconds = 0;

        var t = journeySeconds;
        var i = 1;
        while (i < _points.Length - 2 && t > _points[i + 1].Time)
            i++;

        var a = _points[i];
        var b = _points[i + 1];
        var span = Math.Max(b.Time - a.Time, 0.0001);
        var u = Math.Clamp((t - a.Time) / span, 0, 1);

        var p0 = _points[i - 1].Position;
        var p1 = a.Position;
        var p2 = b.Position;
        var p3 = _points[i + 2].Position;
        var segmentFallback = p2 - p1;

        var position = CentripetalCatmullRom(p0, p1, p2, p3, u);
        if (!IsFinite(position))
            position = Vector3d.Lerp(p1, p2, u);

        var tangent = CentripetalCatmullRomTangent(p0, p1, p2, p3, u, segmentFallback);
        if (!IsFinite(tangent) || tangent.LengthSquared < 1e-30)
            tangent = segmentFallback;

        return (position, tangent);
    }

    private static Waypoint[] BuildWaypoints()
    {
        var earth = SolarSystem.EarthHeliocentric;
        var moon = earth + SolarSystem.MoonOffsetFromEarth;
        var mars = SolarSystem.MarsHeliocentric;
        var jupiter = SolarSystem.JupiterHeliocentric;
        var saturn = SolarSystem.SaturnHeliocentric;
        var uranus = SolarSystem.UranusHeliocentric;
        var neptune = SolarSystem.NeptuneHeliocentric;
        var pluto = SolarSystem.PlutoHeliocentric;

        var earthR = AstronomicalUnits.EarthRadius;
        var moonR = AstronomicalUnits.MoonRadius;
        var marsR = AstronomicalUnits.MarsRadius;
        var jupiterR = AstronomicalUnits.JupiterRadius;
        var saturnR = AstronomicalUnits.SaturnRadius;
        var uranusR = AstronomicalUnits.UranusRadius;
        var neptuneR = AstronomicalUnits.NeptuneRadius;
        var plutoR = AstronomicalUnits.PlutoRadius;

        var earthStart = earth + OffsetFrom(earth, moon, earthR * 12) + new Vector3d(0, earthR * 6, 0);
        var moonFly = Flyby(moon, earth, moonR * 6, lateralScale: 0.4);
        var marsFly = Flyby(mars, moon, marsR * 8, lateralScale: 0.45);
        var jupiterFly = Flyby(jupiter, mars, jupiterR * 4.5, lateralScale: 0.35);
        var saturnFly = FlybyOverPole(saturn, jupiter, saturnR * 5.5, poleTiltDeg: 26.7);
        var uranusFly = Flyby(uranus, saturn, uranusR * 6, lateralScale: 0.35);
        var neptuneFly = Flyby(neptune, uranus, neptuneR * 6, lateralScale: 0.35);
        var plutoFly = Flyby(pluto, neptune, plutoR * 3, lateralScale: 0.2);

        // Mars-local choreography: fast cruise, decelerate, close flyby, accelerate away.
        var marsInbound = (mars - moon).Normalized();
        var marsOutbound = (jupiter - mars).Normalized();
        var marsLateral = Vector3d.Cross(marsInbound, Vector3d.UnitY);
        if (marsLateral.LengthSquared < 1e-16)
            marsLateral = Vector3d.Cross(marsInbound, Vector3d.UnitX);
        marsLateral = marsLateral.Normalized();

        Vector3d MarsApproach(double radii, double lateralScale) =>
            mars - marsInbound * (marsR * radii) + marsLateral * (marsR * radii * lateralScale);

        Vector3d MarsDeparture(double radii, double lateralScale) =>
            mars + marsOutbound * (marsR * radii) + marsLateral * (marsR * radii * lateralScale);

        return
        [
            new(-25, earthStart + OffsetFrom(earthStart, earth, earthR * 40)),
            new(SolarSystem.EarthEncounter, earthStart),
            new(16, Lerp(earthStart, moonFly, 0.22)),
            new(42, Lerp(earthStart, moonFly, 0.55)),
            new(SolarSystem.MoonEncounter, moonFly),
            new(140, Lerp(moonFly, marsFly, 0.55)),
            new(190, Lerp(moonFly, marsFly, 0.88)),
            new(205, MarsApproach(400, 0.35)),
            new(215, MarsApproach(150, 0.40)),
            new(225, MarsApproach(50, 0.42)),
            new(233, MarsApproach(20, 0.44)),
            new(SolarSystem.MarsEncounter, marsFly),
            new(247, MarsDeparture(20, 0.44)),
            new(255, MarsDeparture(50, 0.42)),
            new(265, MarsDeparture(150, 0.40)),
            new(290, MarsDeparture(400, 0.35)),
            new(360, Lerp(marsFly, jupiterFly, 0.22)),
            new(480, Lerp(marsFly, jupiterFly, 0.48)),
            new(560, Lerp(marsFly, jupiterFly, 0.72)),
            new(590, Lerp(marsFly, jupiterFly, 0.88)),
            new(SolarSystem.JupiterEncounter, jupiterFly),
            new(650, Lerp(jupiterFly, saturnFly, 0.18)),
            new(740, Lerp(jupiterFly, saturnFly, 0.42)),
            new(780, Lerp(jupiterFly, saturnFly, 0.62)),
            new(SolarSystem.SaturnEncounter, saturnFly),
            new(860, Lerp(saturnFly, uranusFly, 0.12)),
            new(980, Lerp(saturnFly, uranusFly, 0.38)),
            new(1080, Lerp(saturnFly, uranusFly, 0.68)),
            new(SolarSystem.UranusEncounter, uranusFly),
            new(1185, Lerp(uranusFly, neptuneFly, 0.15)),
            new(1320, Lerp(uranusFly, neptuneFly, 0.42)),
            new(1420, Lerp(uranusFly, neptuneFly, 0.68)),
            new(SolarSystem.NeptuneEncounter, neptuneFly),
            new(1560, Lerp(neptuneFly, plutoFly, 0.22)),
            new(1680, Lerp(neptuneFly, plutoFly, 0.55)),
            new(1745, Lerp(neptuneFly, plutoFly, 0.78)),
            new(1778, Lerp(neptuneFly, plutoFly, 0.92)),
            new(SolarSystem.PlutoEncounter, plutoFly),
            new(1880, plutoFly + OffsetFrom(plutoFly, pluto, plutoR * 8)),
            new(2000, plutoFly + OffsetFrom(plutoFly, pluto, plutoR * 20))
        ];
    }

    /// <summary>Camera standoff position for an offset flyby.</summary>
    private static Vector3d Flyby(Vector3d body, Vector3d approachFrom, double standoffAu, double lateralScale)
    {
        var inbound = (body - approachFrom).Normalized();
        var lateral = Vector3d.Cross(inbound, Vector3d.UnitY);
        if (lateral.LengthSquared < 1e-16)
            lateral = Vector3d.Cross(inbound, Vector3d.UnitX);
        lateral = lateral.Normalized();
        return body - inbound * standoffAu + lateral * (standoffAu * lateralScale);
    }

    private static Vector3d FlybyOverPole(Vector3d body, Vector3d approachFrom, double standoffAu, double poleTiltDeg)
    {
        var inbound = (body - approachFrom).Normalized();
        var pole = CelestialBody.PoleFromTilt((float)poleTiltDeg, 8f);
        var poleD = new Vector3d(pole.X, pole.Y, pole.Z);
        var lateral = Vector3d.Cross(inbound, poleD);
        if (lateral.LengthSquared < 1e-16)
            lateral = Vector3d.Cross(inbound, Vector3d.UnitX);
        lateral = lateral.Normalized();
        return body - inbound * (standoffAu * 0.55) + poleD * (standoffAu * 0.85) + lateral * (standoffAu * 0.25);
    }

    private static Vector3d OffsetFrom(Vector3d from, Vector3d toward, double distanceAu)
    {
        var dir = (toward - from).Normalized();
        return dir * distanceAu;
    }

    private static Vector3d Lerp(Vector3d a, Vector3d b, double t) => Vector3d.Lerp(a, b, t);

    private const double CentripetalAlpha = 0.5;
    private const double DivisionEpsilon = 1e-15;
    private const double TangentStep = 1e-4;

    private static double CentripetalKnotDelta(Vector3d from, Vector3d to)
    {
        var distance = (to - from).Length;
        if (distance < DivisionEpsilon)
            return DivisionEpsilon;
        return Math.Pow(distance, CentripetalAlpha);
    }

    private static double SafeDivide(double numerator, double denominator)
    {
        if (Math.Abs(denominator) < DivisionEpsilon)
            return 0;
        var value = numerator / denominator;
        return double.IsFinite(value) ? value : 0;
    }

    private static (double T0, double T1, double T2, double T3) CentripetalKnots(
        Vector3d p0,
        Vector3d p1,
        Vector3d p2,
        Vector3d p3)
    {
        var t0 = 0.0;
        var t1 = t0 + CentripetalKnotDelta(p0, p1);
        var t2 = t1 + CentripetalKnotDelta(p1, p2);
        var t3 = t2 + CentripetalKnotDelta(p2, p3);
        return (t0, t1, t2, t3);
    }

    private static Vector3d CentripetalCatmullRom(Vector3d p0, Vector3d p1, Vector3d p2, Vector3d p3, double u)
    {
        var (t0, t1, t2, t3) = CentripetalKnots(p0, p1, p2, p3);
        var t = t1 + u * (t2 - t1);
        return EvaluateCentripetalAt(p0, p1, p2, p3, t0, t1, t2, t3, t);
    }

    private static Vector3d EvaluateCentripetalAt(
        Vector3d p0,
        Vector3d p1,
        Vector3d p2,
        Vector3d p3,
        double t0,
        double t1,
        double t2,
        double t3,
        double t)
    {
        var a1 = p0 * SafeDivide(t1 - t, t1 - t0) + p1 * SafeDivide(t - t0, t1 - t0);
        var a2 = p1 * SafeDivide(t2 - t, t2 - t1) + p2 * SafeDivide(t - t1, t2 - t1);
        var a3 = p2 * SafeDivide(t3 - t, t3 - t2) + p3 * SafeDivide(t - t2, t3 - t2);

        var b1 = a1 * SafeDivide(t2 - t, t2 - t0) + a2 * SafeDivide(t - t0, t2 - t0);
        var b2 = a2 * SafeDivide(t3 - t, t3 - t1) + a3 * SafeDivide(t - t1, t3 - t1);

        return b1 * SafeDivide(t2 - t, t2 - t1) + b2 * SafeDivide(t - t1, t2 - t1);
    }

    private static Vector3d CentripetalCatmullRomTangent(
        Vector3d p0,
        Vector3d p1,
        Vector3d p2,
        Vector3d p3,
        double u,
        Vector3d segmentFallback)
    {
        var u0 = Math.Max(0, u - TangentStep);
        var u1 = Math.Min(1, u + TangentStep);
        var pos0 = CentripetalCatmullRom(p0, p1, p2, p3, u0);
        var pos1 = CentripetalCatmullRom(p0, p1, p2, p3, u1);
        var tangent = pos1 - pos0;
        if (!IsFinite(tangent) || tangent.LengthSquared < 1e-30)
            return segmentFallback;
        return tangent;
    }

    private static bool IsFinite(Vector3d value) =>
        double.IsFinite(value.X) && double.IsFinite(value.Y) && double.IsFinite(value.Z);

    private readonly record struct Waypoint(double Time, Vector3d Position);
}
