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
        var t = journeySeconds;
        var i = 1;
        while (i < _points.Length - 2 && t > _points[i + 1].Time)
            i++;

        var a = _points[i];
        var b = _points[i + 1];
        var span = Math.Max(b.Time - a.Time, 0.0001);
        var u = Math.Clamp((t - a.Time) / span, 0, 1);

        return (
            CatmullRom(_points[i - 1].Position, a.Position, b.Position, _points[i + 2].Position, u),
            CatmullRomTangent(_points[i - 1].Position, a.Position, b.Position, _points[i + 2].Position, u));
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

        return
        [
            new(-25, earthStart + OffsetFrom(earthStart, earth, earthR * 40)),
            new(SolarSystem.EarthEncounter, earthStart),
            new(16, Lerp(earthStart, moonFly, 0.22)),
            new(42, Lerp(earthStart, moonFly, 0.55)),
            new(SolarSystem.MoonEncounter, moonFly),
            new(140, Lerp(moonFly, marsFly, 0.28)),
            new(SolarSystem.MarsEncounter, marsFly),
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

    private static Vector3d CatmullRom(Vector3d p0, Vector3d p1, Vector3d p2, Vector3d p3, double t)
    {
        var t2 = t * t;
        var t3 = t2 * t;
        return (
            2 * p1 +
            (-p0 + p2) * t +
            (2 * p0 - 5 * p1 + 4 * p2 - p3) * t2 +
            (-p0 + 3 * p1 - 3 * p2 + p3) * t3) * 0.5;
    }

    private static Vector3d CatmullRomTangent(Vector3d p0, Vector3d p1, Vector3d p2, Vector3d p3, double t)
    {
        var t2 = t * t;
        return (
            (-p0 + p2) +
            2 * (2 * p0 - 5 * p1 + 4 * p2 - p3) * t +
            3 * (-p0 + 3 * p1 - 3 * p2 + p3) * t2) * 0.5;
    }

    private readonly record struct Waypoint(double Time, Vector3d Position);
}
