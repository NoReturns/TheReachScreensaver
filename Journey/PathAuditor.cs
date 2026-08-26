using System.Text;
using TheReachScreensaver.Journey.Coordinates;
using Vector3d = TheReachScreensaver.Journey.Coordinates.Vector3d;

namespace TheReachScreensaver.Journey;

/// <summary>Samples the authored path at 1 s intervals and reports encounter metrics.</summary>
internal static class PathAuditor
{
    public static string Run()
    {
        var path = new JourneyPath();
        var sb = new StringBuilder();
        sb.AppendLine("=== Journey Path Audit (1 s samples, 0..1800) ===");

        Vector3d? previous = null;
        Vector3d? previousTangent = null;
        var reversalCount = 0;
        var nonFinite = 0;
        var maxSpeed = 0.0;
        var maxSpeedTime = 0.0;
        var excursionPeak = 0.0;

        for (var t = 0; t <= (int)JourneyController.DurationSeconds; t++)
        {
            var (pos, tangent) = path.Evaluate(t);
            if (!IsFinite(pos) || !IsFinite(tangent))
            {
                nonFinite++;
                continue;
            }

            excursionPeak = Math.Max(excursionPeak, pos.Length);

            if (previous is not null)
            {
                var delta = pos - previous.Value;
                var speed = delta.Length; // AU per second
                if (speed > maxSpeed)
                {
                    maxSpeed = speed;
                    maxSpeedTime = t;
                }

                if (previousTangent is not null && previousTangent.Value.LengthSquared > 1e-30 && tangent.LengthSquared > 1e-30)
                {
                    var a = previousTangent.Value.Normalized();
                    var b = tangent.Normalized();
                    if (Vector3d.Dot(a, b) < -0.25)
                        reversalCount++;
                }
            }

            previous = pos;
            previousTangent = tangent;
        }

        sb.AppendLine($"Non-finite samples: {nonFinite}");
        sb.AppendLine($"Approx tangent reversals (dot < -0.25 between consecutive seconds): {reversalCount}");
        sb.AppendLine($"Peak heliocentric distance: {excursionPeak:0.###} AU");
        sb.AppendLine($"Peak camera speed: {maxSpeed:0.######} AU/s at t={maxSpeedTime:0}s ({AstronomicalUnits.AuToKilometers(maxSpeed):0} km/s)");
        sb.AppendLine();

        foreach (var profile in EncounterCatalog.MajorBodies)
        {
            if (profile.BodyId == CelestialId.Earth)
            {
                sb.AppendLine("Earth: opening shot 0–16s (dedicated policy); no local flyby audit.");
                var earthWarp = WarpDepartureCatalog.Events.FirstOrDefault(e => e.FromBody == CelestialId.Earth);
                if (earthWarp is not null)
                    sb.AppendLine($"  Warp departure: {earthWarp.StartTime:0.##}s duration {earthWarp.DurationSeconds:0.##}s intensity {earthWarp.Intensity:0.##}");
                sb.AppendLine();
                continue;
            }

            ReportBody(sb, path, profile);
        }

        return sb.ToString();
    }

    private static void ReportBody(StringBuilder sb, JourneyPath path, EncounterPresentationProfile profile)
    {
        var bodyPos = BodyPosition(profile.BodyId);
        var radius = BodyRadius(profile.BodyId);
        var start = Math.Max(0, (int)Math.Floor(profile.ApproachStart) - 30);
        var end = Math.Min((int)JourneyController.DurationSeconds, (int)Math.Ceiling(profile.LocalExitEnd) + 30);

        var closestDist = double.MaxValue;
        var closestTime = profile.EncounterTime;
        var under50 = 0;
        var under20 = 0;
        var peakLocalSpeed = 0.0;
        Vector3d? prev = null;

        for (var t = start; t <= end; t++)
        {
            var (pos, _) = path.Evaluate(t);
            var dist = (pos - bodyPos).Length;
            if (dist < closestDist)
            {
                closestDist = dist;
                closestTime = t;
            }

            if (dist < radius * 50)
                under50++;
            if (dist < radius * 20)
                under20++;

            if (prev is not null && t >= profile.ApproachStart && t <= profile.LocalExitEnd)
            {
                var speed = (pos - prev.Value).Length;
                peakLocalSpeed = Math.Max(peakLocalSpeed, speed);
            }

            prev = pos;
        }

        var collision = closestDist < radius * 1.05;
        sb.AppendLine($"{profile.BodyId}:");
        sb.AppendLine($"  Focus window: {profile.FocusBlendInStart:0} → full {profile.FullFocusStart:0}–{profile.FullFocusEnd:0} → release {profile.FocusBlendOutEnd:0}");
        sb.AppendLine($"  Closest: {closestDist:0.######} AU ({closestDist / radius:0.##} R) at t≈{closestTime:0}s");
        sb.AppendLine($"  Duration <50R: ~{under50}s; <20R: ~{under20}s");
        sb.AppendLine($"  Peak local encounter speed: {peakLocalSpeed:0.######} AU/s ({AstronomicalUnits.AuToKilometers(peakLocalSpeed):0} km/s)");
        sb.AppendLine($"  Center collision risk (<1.05R): {(collision ? "YES" : "no")}");

        var warp = WarpDepartureCatalog.Events.FirstOrDefault(e => e.FromBody == profile.BodyId);
        if (warp is not null)
            sb.AppendLine($"  Warp departure: {warp.StartTime:0.##}s (after release {profile.FocusBlendOutEnd:0}) duration {warp.DurationSeconds:0.##}s intensity {warp.Intensity:0.##}");
        else
            sb.AppendLine("  Warp departure: none");

        sb.AppendLine();
    }

    private static Vector3d BodyPosition(CelestialId id) => id switch
    {
        CelestialId.Moon => SolarSystem.EarthHeliocentric + SolarSystem.MoonOffsetFromEarth,
        CelestialId.Mars => SolarSystem.MarsHeliocentric,
        CelestialId.Jupiter => SolarSystem.JupiterHeliocentric,
        CelestialId.Saturn => SolarSystem.SaturnHeliocentric,
        CelestialId.Uranus => SolarSystem.UranusHeliocentric,
        CelestialId.Neptune => SolarSystem.NeptuneHeliocentric,
        CelestialId.Pluto => SolarSystem.PlutoHeliocentric,
        _ => SolarSystem.EarthHeliocentric
    };

    private static double BodyRadius(CelestialId id) => id switch
    {
        CelestialId.Moon => AstronomicalUnits.MoonRadius,
        CelestialId.Mars => AstronomicalUnits.MarsRadius,
        CelestialId.Jupiter => AstronomicalUnits.JupiterRadius,
        CelestialId.Saturn => AstronomicalUnits.SaturnRadius,
        CelestialId.Uranus => AstronomicalUnits.UranusRadius,
        CelestialId.Neptune => AstronomicalUnits.NeptuneRadius,
        CelestialId.Pluto => AstronomicalUnits.PlutoRadius,
        _ => AstronomicalUnits.EarthRadius
    };

    private static bool IsFinite(Vector3d value) =>
        double.IsFinite(value.X) && double.IsFinite(value.Y) && double.IsFinite(value.Z);
}
