namespace TheReachScreensaver.Journey;

/// <summary>One-shot departure warp overlay after a body is substantially released.</summary>
internal sealed class WarpDepartureEvent
{
    public required CelestialId FromBody { get; init; }
    public required double StartTime { get; init; }
    public required double DurationSeconds { get; init; }
    public required float Intensity { get; init; }

    public double EndTime => StartTime + DurationSeconds;
}

internal static class WarpDepartureCatalog
{
    public static IReadOnlyList<WarpDepartureEvent> Events { get; } =
    [
        new() { FromBody = CelestialId.Earth, StartTime = 16.4, DurationSeconds = 1.15, Intensity = 0.30f },
        new() { FromBody = CelestialId.Moon, StartTime = 99.2, DurationSeconds = 1.65, Intensity = 0.55f },
        new() { FromBody = CelestialId.Mars, StartTime = 262.0, DurationSeconds = 1.90, Intensity = 0.70f },
        new() { FromBody = CelestialId.Jupiter, StartTime = 637.5, DurationSeconds = 1.95, Intensity = 0.75f },
        new() { FromBody = CelestialId.Saturn, StartTime = 851.0, DurationSeconds = 2.15, Intensity = 0.85f },
        new() { FromBody = CelestialId.Uranus, StartTime = 1174.5, DurationSeconds = 2.15, Intensity = 0.90f },
        new() { FromBody = CelestialId.Neptune, StartTime = 1534.0, DurationSeconds = 2.35, Intensity = 1.00f }
        // No Pluto departure warp — reserved for future interstellar work.
    ];

    public static float EvaluateIntensity(double time)
    {
        foreach (var ev in Events)
        {
            if (time < ev.StartTime || time >= ev.EndTime)
                continue;

            var u = (time - ev.StartTime) / Math.Max(ev.DurationSeconds, 1e-6);
            return ev.Intensity * TemporalEnvelope(u);
        }

        return 0f;
    }

    public static WarpDepartureEvent? ActiveEvent(double time) =>
        Events.FirstOrDefault(e => time >= e.StartTime && time < e.EndTime);

    /// <summary>
    /// 0.00–0.20 build, 0.20–0.70 peak, 0.70–1.00 rapid fade. No second exit event.
    /// </summary>
    private static float TemporalEnvelope(double u)
    {
        u = Math.Clamp(u, 0, 1);
        if (u < 0.20)
            return SmoothStep01(u / 0.20);
        if (u < 0.70)
            return 1f;
        return 1f - SmoothStep01((u - 0.70) / 0.30);
    }

    private static float SmoothStep01(double t)
    {
        t = Math.Clamp(t, 0, 1);
        return (float)(t * t * (3 - 2 * t));
    }
}
