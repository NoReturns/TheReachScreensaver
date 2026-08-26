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
        var moonFly = ClosestFlyby(EncounterCatalog.Moon, moon, earth, moonR);
        var marsFly = ClosestFlyby(EncounterCatalog.Mars, mars, moon, marsR);
        var jupiterFly = ClosestFlyby(EncounterCatalog.Jupiter, jupiter, mars, jupiterR);
        var saturnFly = ClosestFlyby(EncounterCatalog.Saturn, saturn, jupiter, saturnR);
        var uranusFly = ClosestFlyby(EncounterCatalog.Uranus, uranus, saturn, uranusR);
        var neptuneFly = ClosestFlyby(EncounterCatalog.Neptune, neptune, uranus, neptuneR);
        var plutoFly = ClosestFlyby(EncounterCatalog.Pluto, pluto, neptune, plutoR);

        var points = new List<Waypoint>
        {
            new(-25, earthStart + OffsetFrom(earthStart, earth, earthR * 40)),
            new(SolarSystem.EarthEncounter, earthStart),
            new(16, Lerp(earthStart, moonFly, 0.22)),
            new(42, Lerp(earthStart, moonFly, 0.55))
        };

        AppendEncounter(points, EncounterCatalog.Moon, moon, earth, mars, moonR, moonFly);
        points.Add(new(140, Lerp(moonFly, marsFly, 0.55)));
        points.Add(new(190, Lerp(moonFly, marsFly, 0.88)));
        AppendEncounter(points, EncounterCatalog.Mars, mars, moon, jupiter, marsR, marsFly);
        points.Add(new(360, Lerp(marsFly, jupiterFly, 0.22)));
        points.Add(new(480, Lerp(marsFly, jupiterFly, 0.48)));
        AppendEncounter(points, EncounterCatalog.Jupiter, jupiter, mars, saturn, jupiterR, jupiterFly);
        points.Add(new(700, Lerp(jupiterFly, saturnFly, 0.28)));
        AppendEncounter(points, EncounterCatalog.Saturn, saturn, jupiter, uranus, saturnR, saturnFly);
        points.Add(new(960, Lerp(saturnFly, uranusFly, 0.28)));
        points.Add(new(1050, Lerp(saturnFly, uranusFly, 0.55)));
        AppendEncounter(points, EncounterCatalog.Uranus, uranus, saturn, neptune, uranusR, uranusFly);
        points.Add(new(1280, Lerp(uranusFly, neptuneFly, 0.28)));
        points.Add(new(1400, Lerp(uranusFly, neptuneFly, 0.55)));
        AppendEncounter(points, EncounterCatalog.Neptune, neptune, uranus, pluto, neptuneR, neptuneFly);
        points.Add(new(1620, Lerp(neptuneFly, plutoFly, 0.28)));
        points.Add(new(1710, Lerp(neptuneFly, plutoFly, 0.58)));
        AppendEncounter(points, EncounterCatalog.Pluto, pluto, neptune, pluto, plutoR, plutoFly);
        points.Add(new(1880, plutoFly + OffsetFrom(plutoFly, pluto, plutoR * 8)));
        points.Add(new(2000, plutoFly + OffsetFrom(plutoFly, pluto, plutoR * 20)));

        return points.OrderBy(p => p.Time).ToArray();
    }

    private static void AppendEncounter(
        List<Waypoint> points,
        EncounterPresentationProfile profile,
        Vector3d body,
        Vector3d approachFrom,
        Vector3d departToward,
        double bodyRadius,
        Vector3d closestFly)
    {
        foreach (var key in profile.ApproachKeys)
        {
            points.Add(new(
                key.Time,
                LocalApproach(profile, body, approachFrom, bodyRadius * key.Radii, key.LateralScale)));
        }

        points.Add(new(profile.EncounterTime, closestFly));

        foreach (var key in profile.DepartureKeys)
        {
            points.Add(new(
                key.Time,
                LocalDeparture(profile, body, approachFrom, departToward, bodyRadius * key.Radii, key.LateralScale)));
        }
    }

    private static Vector3d ClosestFlyby(
        EncounterPresentationProfile profile,
        Vector3d body,
        Vector3d approachFrom,
        double bodyRadius)
    {
        var standoff = bodyRadius * profile.ClosestApproachRadii;
        if (profile.FlybyStyle == EncounterFlybyStyle.OverPole)
            return FlybyOverPole(body, approachFrom, standoff, profile.PoleTiltDegrees);

        var lateral = profile.ClosestLateralScale
            ?? (profile.ApproachKeys.Count > 0 ? profile.ApproachKeys[^1].LateralScale : 0.35);
        return Flyby(body, approachFrom, standoff, lateral);
    }

    private static Vector3d LocalApproach(
        EncounterPresentationProfile profile,
        Vector3d body,
        Vector3d approachFrom,
        double standoffAu,
        double lateralScale)
    {
        if (profile.FlybyStyle == EncounterFlybyStyle.OverPole)
            return FlybyOverPole(body, approachFrom, standoffAu, profile.PoleTiltDegrees);
        return Flyby(body, approachFrom, standoffAu, lateralScale);
    }

    private static Vector3d LocalDeparture(
        EncounterPresentationProfile profile,
        Vector3d body,
        Vector3d approachFrom,
        Vector3d departToward,
        double standoffAu,
        double lateralScale)
    {
        if (profile.FlybyStyle == EncounterFlybyStyle.OverPole)
            return FlybyOverPoleDeparture(body, departToward, standoffAu, profile.PoleTiltDegrees);

        var inbound = (body - approachFrom).Normalized();
        var lateral = Vector3d.Cross(inbound, Vector3d.UnitY);
        if (lateral.LengthSquared < 1e-16)
            lateral = Vector3d.Cross(inbound, Vector3d.UnitX);
        lateral = lateral.Normalized();

        if (profile.FlybyStyle == EncounterFlybyStyle.ThroughPass)
        {
            // Continue past the body along the inbound axis with the same lateral bias.
            return body + inbound * standoffAu + lateral * (standoffAu * lateralScale);
        }

        // Mars model: shared lateral from inbound, outbound along next-leg direction.
        var outbound = (departToward - body).Normalized();
        return body + outbound * standoffAu + lateral * (standoffAu * lateralScale);
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

    private static Vector3d FlybyOverPoleDeparture(Vector3d body, Vector3d departToward, double standoffAu, double poleTiltDeg)
    {
        var outbound = (departToward - body).Normalized();
        var pole = CelestialBody.PoleFromTilt((float)poleTiltDeg, 8f);
        var poleD = new Vector3d(pole.X, pole.Y, pole.Z);
        var lateral = Vector3d.Cross(outbound, poleD);
        if (lateral.LengthSquared < 1e-16)
            lateral = Vector3d.Cross(outbound, Vector3d.UnitX);
        lateral = lateral.Normalized();
        return body + outbound * (standoffAu * 0.55) + poleD * (standoffAu * 0.85) + lateral * (standoffAu * 0.25);
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
