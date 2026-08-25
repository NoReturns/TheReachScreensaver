using OpenTK.Mathematics;
using TheReachScreensaver.Journey.Coordinates;
using Vector3d = TheReachScreensaver.Journey.Coordinates.Vector3d;

namespace TheReachScreensaver.Journey;

internal static class SolarSystem
{
    public const float EarthEncounter = 0f;
    public const float MoonEncounter = 75f;
    public const float MarsEncounter = 240f;
    public const float JupiterEncounter = 600f;
    public const float SaturnEncounter = 810f;
    public const float UranusEncounter = 1140f;
    public const float NeptuneEncounter = 1500f;
    public const float PlutoEncounter = 1800f;

    public static readonly Vector3d SolPosition = Vector3d.Zero;

    /// <summary>Heliocentric positions at authored epoch (AU, ecliptic XZ plane + small inclinations).</summary>
    public static readonly Vector3d EarthHeliocentric = Heliocentric(
        AstronomicalUnits.EarthOrbitAu,
        AstronomicalUnits.EarthLongitudeDeg,
        0);

    public static readonly Vector3d MarsHeliocentric = Heliocentric(
        AstronomicalUnits.MarsOrbitAu,
        AstronomicalUnits.MarsLongitudeDeg,
        AstronomicalUnits.MarsInclinationDeg);

    public static readonly Vector3d JupiterHeliocentric = Heliocentric(
        AstronomicalUnits.JupiterOrbitAu,
        AstronomicalUnits.JupiterLongitudeDeg,
        AstronomicalUnits.JupiterInclinationDeg);

    public static readonly Vector3d SaturnHeliocentric = Heliocentric(
        AstronomicalUnits.SaturnOrbitAu,
        AstronomicalUnits.SaturnLongitudeDeg,
        AstronomicalUnits.SaturnInclinationDeg);

    public static readonly Vector3d UranusHeliocentric = Heliocentric(
        AstronomicalUnits.UranusOrbitAu,
        AstronomicalUnits.UranusLongitudeDeg,
        AstronomicalUnits.UranusInclinationDeg);

    public static readonly Vector3d NeptuneHeliocentric = Heliocentric(
        AstronomicalUnits.NeptuneOrbitAu,
        AstronomicalUnits.NeptuneLongitudeDeg,
        AstronomicalUnits.NeptuneInclinationDeg);

    public static readonly Vector3d PlutoHeliocentric = Heliocentric(
        AstronomicalUnits.PlutoOrbitAu,
        AstronomicalUnits.PlutoLongitudeDeg,
        AstronomicalUnits.PlutoInclinationDeg);

    /// <summary>Moon offset from Earth at epoch: toward +Z in ecliptic (AU).</summary>
    public static readonly Vector3d MoonOffsetFromEarth = new(0, AstronomicalUnits.KilometersToAu(18_000), AstronomicalUnits.MoonDistanceAu);

    public static Vector3 LightDirectionAt(Vector3d worldPosition, Vector3d cameraWorldPosition)
    {
        var solRender = (SolPosition - cameraWorldPosition).ToVector3();
        var bodyRender = (worldPosition - cameraWorldPosition).ToVector3();
        var toSun = solRender - bodyRender;
        return toSun.LengthSquared > 1e-12f ? Vector3.Normalize(toSun) : Vector3.UnitZ;
    }

    public static IReadOnlyList<CelestialBody> CreateBodies()
    {
        var earthPos = EarthHeliocentric;
        var marsPos = MarsHeliocentric;
        var jupiterPos = JupiterHeliocentric;
        var saturnPos = SaturnHeliocentric;
        var uranusPos = UranusHeliocentric;
        var neptunePos = NeptuneHeliocentric;
        var plutoPos = PlutoHeliocentric;

        return
        [
            new CelestialBody
            {
                Id = CelestialId.Earth,
                Name = "Earth",
                Radius = AstronomicalUnits.EarthRadius,
                WorldPosition = earthPos,
                Albedo = new Vector3(0.10f, 0.22f, 0.48f),
                Style = PlanetStyle.Earth,
                EncounterTime = EarthEncounter,
                FlybyDistanceAu = AstronomicalUnits.EarthRadius * 8.0,
                UnresolvedVisible = false,
                RotationAxis = CelestialBody.PoleFromTilt(23.5f, 12f),
                RotationRate = 0.045f,
                InitialRotation = 0.85f
            },
            new CelestialBody
            {
                Id = CelestialId.Moon,
                Name = "Moon",
                Radius = AstronomicalUnits.MoonRadius,
                WorldPosition = earthPos + MoonOffsetFromEarth,
                Albedo = new Vector3(0.68f, 0.66f, 0.61f),
                EncounterTime = MoonEncounter,
                FlybyDistanceAu = AstronomicalUnits.MoonRadius * 6.0,
                UnresolvedVisible = false,
                RotationAxis = CelestialBody.PoleFromTilt(6.7f, 40f),
                RotationRate = 0.038f,
                InitialRotation = 1.2f
            },
            new CelestialBody
            {
                Id = CelestialId.Mars,
                Name = "Mars",
                Radius = AstronomicalUnits.MarsRadius,
                WorldPosition = marsPos,
                Albedo = new Vector3(0.66f, 0.32f, 0.18f),
                Style = PlanetStyle.Mars,
                EncounterTime = MarsEncounter,
                FlybyDistanceAu = AstronomicalUnits.MarsRadius * 8.0,
                UnresolvedVisible = true,
                UnresolvedBrightness = 0.55f,
                UnresolvedColor = new Vector3(0.95f, 0.55f, 0.35f),
                RotationAxis = CelestialBody.PoleFromTilt(25.2f, 188f),
                RotationRate = 0.042f,
                InitialRotation = 2.4f
            },
            new CelestialBody
            {
                Id = CelestialId.Jupiter,
                Name = "Jupiter",
                Radius = AstronomicalUnits.JupiterRadius,
                WorldPosition = jupiterPos,
                Albedo = new Vector3(0.78f, 0.58f, 0.35f),
                Style = PlanetStyle.Jupiter,
                EncounterTime = JupiterEncounter,
                FlybyDistanceAu = AstronomicalUnits.JupiterRadius * 4.5,
                UnresolvedVisible = true,
                UnresolvedBrightness = 0.95f,
                UnresolvedColor = new Vector3(0.96f, 0.88f, 0.68f),
                RotationAxis = CelestialBody.PoleFromTilt(3.1f, 18f),
                RotationRate = 0.070f,
                InitialRotation = 0.35f
            },
            new CelestialBody
            {
                Id = CelestialId.Io,
                Name = "Io",
                Radius = AstronomicalUnits.KilometersToAu(1_821),
                WorldPosition = jupiterPos,
                Albedo = new Vector3(0.90f, 0.82f, 0.35f),
                IsSatellite = true,
                UnresolvedVisible = false,
                RotationAxis = CelestialBody.PoleFromTilt(3f, 0f),
                RotationRate = 0.050f,
                InitialRotation = 0.6f
            },
            new CelestialBody
            {
                Id = CelestialId.Europa,
                Name = "Europa",
                Radius = AstronomicalUnits.KilometersToAu(1_560),
                WorldPosition = jupiterPos,
                Albedo = new Vector3(0.82f, 0.84f, 0.80f),
                IsSatellite = true,
                UnresolvedVisible = false,
                RotationAxis = CelestialBody.PoleFromTilt(2f, 20f),
                RotationRate = 0.044f,
                InitialRotation = 1.8f
            },
            new CelestialBody
            {
                Id = CelestialId.Ganymede,
                Name = "Ganymede",
                Radius = AstronomicalUnits.KilometersToAu(2_634),
                WorldPosition = jupiterPos,
                Albedo = new Vector3(0.58f, 0.52f, 0.45f),
                IsSatellite = true,
                UnresolvedVisible = false,
                RotationAxis = CelestialBody.PoleFromTilt(4f, 40f),
                RotationRate = 0.036f,
                InitialRotation = 2.7f
            },
            new CelestialBody
            {
                Id = CelestialId.Callisto,
                Name = "Callisto",
                Radius = AstronomicalUnits.KilometersToAu(2_410),
                WorldPosition = jupiterPos,
                Albedo = new Vector3(0.42f, 0.38f, 0.34f),
                IsSatellite = true,
                UnresolvedVisible = false,
                RotationAxis = CelestialBody.PoleFromTilt(5f, 55f),
                RotationRate = 0.030f,
                InitialRotation = 4.1f
            },
            new CelestialBody
            {
                Id = CelestialId.Saturn,
                Name = "Saturn",
                Radius = AstronomicalUnits.SaturnRadius,
                WorldPosition = saturnPos,
                Albedo = new Vector3(0.86f, 0.74f, 0.52f),
                Style = PlanetStyle.Saturn,
                EncounterTime = SaturnEncounter,
                FlybyDistanceAu = AstronomicalUnits.SaturnRadius * 5.5,
                UnresolvedVisible = true,
                UnresolvedBrightness = 0.72f,
                UnresolvedColor = new Vector3(0.92f, 0.86f, 0.68f),
                RotationAxis = CelestialBody.PoleFromTilt(26.7f, 8f),
                RotationRate = 0.055f,
                InitialRotation = 1.15f,
                RingInnerRadiusAu = AstronomicalUnits.SaturnRadius * 1.28,
                RingOuterRadiusAu = AstronomicalUnits.SaturnRadius * 2.35,
                RingColor = new Vector3(0.92f, 0.84f, 0.66f),
                RingOpacity = 0.92f
            },
            new CelestialBody
            {
                Id = CelestialId.Titan,
                Name = "Titan",
                Radius = AstronomicalUnits.KilometersToAu(2_574),
                WorldPosition = saturnPos,
                Albedo = new Vector3(0.78f, 0.62f, 0.32f),
                IsSatellite = true,
                UnresolvedVisible = false,
                RotationAxis = CelestialBody.PoleFromTilt(27f, 8f),
                RotationRate = 0.028f,
                InitialRotation = 0.9f
            },
            new CelestialBody
            {
                Id = CelestialId.Uranus,
                Name = "Uranus",
                Radius = AstronomicalUnits.UranusRadius,
                WorldPosition = uranusPos,
                Albedo = new Vector3(0.62f, 0.82f, 0.84f),
                Style = PlanetStyle.Uranus,
                EncounterTime = UranusEncounter,
                FlybyDistanceAu = AstronomicalUnits.UranusRadius * 6.0,
                UnresolvedVisible = true,
                UnresolvedBrightness = 0.22f,
                UnresolvedColor = new Vector3(0.72f, 0.90f, 0.92f),
                RotationAxis = CelestialBody.PoleFromTilt(97.8f, 74f),
                RotationRate = 0.040f,
                InitialRotation = 0.2f
            },
            new CelestialBody
            {
                Id = CelestialId.Neptune,
                Name = "Neptune",
                Radius = AstronomicalUnits.NeptuneRadius,
                WorldPosition = neptunePos,
                Albedo = new Vector3(0.10f, 0.28f, 0.72f),
                Style = PlanetStyle.Neptune,
                EncounterTime = NeptuneEncounter,
                FlybyDistanceAu = AstronomicalUnits.NeptuneRadius * 6.0,
                UnresolvedVisible = false,
                RotationAxis = CelestialBody.PoleFromTilt(28.3f, 226f),
                RotationRate = 0.050f,
                InitialRotation = 3.05f
            },
            new CelestialBody
            {
                Id = CelestialId.Pluto,
                Name = "Pluto",
                Radius = AstronomicalUnits.PlutoRadius,
                WorldPosition = plutoPos,
                Albedo = new Vector3(0.62f, 0.54f, 0.46f),
                Style = PlanetStyle.Pluto,
                EncounterTime = PlutoEncounter,
                FlybyDistanceAu = AstronomicalUnits.PlutoRadius * 3.0,
                UnresolvedVisible = false,
                RotationAxis = CelestialBody.PoleFromTilt(32f, 64f),
                RotationRate = 0.028f,
                InitialRotation = 1.55f
            },
            new CelestialBody
            {
                Id = CelestialId.Charon,
                Name = "Charon",
                Radius = AstronomicalUnits.KilometersToAu(606),
                WorldPosition = plutoPos,
                Albedo = new Vector3(0.48f, 0.46f, 0.50f),
                IsSatellite = true,
                UnresolvedVisible = false,
                RotationAxis = CelestialBody.PoleFromTilt(32f, 64f),
                RotationRate = 0.024f,
                InitialRotation = 2.2f
            }
        ];
    }

    public static void UpdateSatellites(IReadOnlyList<CelestialBody> bodies, double journeySeconds)
    {
        var t = (float)journeySeconds;
        UpdateJovianMoons(bodies, t);
        UpdateTitan(bodies, t);
        UpdateCharon(bodies, t);
    }

    public static CelestialBody? NextDestination(IReadOnlyList<CelestialBody> bodies, double journeySeconds)
    {
        CelestialBody? next = null;
        foreach (var body in bodies)
        {
            if (body.IsSatellite || body.EncounterTime <= journeySeconds)
                continue;
            if (next is null || body.EncounterTime < next.EncounterTime)
                next = body;
        }
        return next;
    }

    public static Vector3d Heliocentric(double au, double longitudeDegrees, double inclinationDegrees)
    {
        var lon = longitudeDegrees * Math.PI / 180.0;
        var inc = inclinationDegrees * Math.PI / 180.0;
        var cosLon = Math.Cos(lon);
        var sinLon = Math.Sin(lon);
        var cosInc = Math.Cos(inc);
        var x = au * cosLon * cosInc;
        var z = au * sinLon * cosInc;
        var y = au * Math.Sin(inc);
        return new Vector3d(x, y, z);
    }

    private static void UpdateJovianMoons(IReadOnlyList<CelestialBody> bodies, float t)
    {
        var jupiter = bodies.First(b => b.Id == CelestialId.Jupiter);
        var orbitRadiiAu = new[]
        {
            AstronomicalUnits.KilometersToAu(421_700),
            AstronomicalUnits.KilometersToAu(671_100),
            AstronomicalUnits.KilometersToAu(1_070_400),
            AstronomicalUnits.KilometersToAu(1_882_700)
        };
        var speeds = new[] { 0.11f, 0.07f, 0.045f, 0.028f };
        var phases = new[] { 0.4f, 1.7f, 3.1f, 5.2f };
        var ids = new[] { CelestialId.Io, CelestialId.Europa, CelestialId.Ganymede, CelestialId.Callisto };
        for (var i = 0; i < ids.Length; i++)
        {
            var moon = bodies.First(b => b.Id == ids[i]);
            var a = t * speeds[i] + phases[i];
            var r = orbitRadiiAu[i];
            moon.WorldPosition = jupiter.WorldPosition + new Vector3d(
                Math.Cos(a) * r,
                Math.Sin(a * 0.35) * r * 0.12,
                Math.Sin(a) * r);
        }
    }

    private static void UpdateTitan(IReadOnlyList<CelestialBody> bodies, float t)
    {
        var saturn = bodies.First(b => b.Id == CelestialId.Saturn);
        var titan = bodies.First(b => b.Id == CelestialId.Titan);
        var a = t * 0.022f + 4.3f;
        var r = AstronomicalUnits.KilometersToAu(1_221_870);
        OrbitMoon(saturn, titan, saturn.PoleVector(), a, r, r * 0.04);
    }

    private static void UpdateCharon(IReadOnlyList<CelestialBody> bodies, float t)
    {
        var pluto = bodies.First(b => b.Id == CelestialId.Pluto);
        var charon = bodies.First(b => b.Id == CelestialId.Charon);
        var a = t * 0.019f + 2.8f;
        var r = AstronomicalUnits.KilometersToAu(19_596);
        OrbitMoon(pluto, charon, pluto.PoleVector(), a, r, 0);
    }

    private static void OrbitMoon(
        CelestialBody parent,
        CelestialBody moon,
        Vector3 pole,
        float angle,
        double radiusAu,
        double poleWobbleAu)
    {
        var equatorial = EquatorialBasis(pole);
        var cosA = Math.Cos(angle);
        var sinA = Math.Sin(angle);
        var along = new Vector3d(equatorial.Along.X, equatorial.Along.Y, equatorial.Along.Z);
        var across = new Vector3d(equatorial.Across.X, equatorial.Across.Y, equatorial.Across.Z);
        var poleD = new Vector3d(pole.X, pole.Y, pole.Z);
        moon.WorldPosition = parent.WorldPosition
            + along * (cosA * radiusAu)
            + across * (sinA * radiusAu)
            + poleD * (Math.Sin(angle * 0.4) * poleWobbleAu);
    }

    private static (Vector3 Along, Vector3 Across) EquatorialBasis(Vector3 pole)
    {
        var along = Vector3.Normalize(Vector3.Cross(pole, MathF.Abs(pole.Y) > 0.9f ? Vector3.UnitX : Vector3.UnitY));
        var across = Vector3.Normalize(Vector3.Cross(pole, along));
        return (along, across);
    }
}
