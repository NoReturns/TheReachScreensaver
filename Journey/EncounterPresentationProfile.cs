namespace TheReachScreensaver.Journey;

internal enum EncounterFlybyStyle
{
    /// <summary>Approach along inbound; depart along next-leg outbound (Mars model).</summary>
    StandardOffset,
    /// <summary>Approach and depart along the same inbound axis — avoids knife-edge turns (Moon).</summary>
    ThroughPass,
    OverPole
}

/// <summary>
/// Data-driven local encounter presentation for a major Solar-System destination.
/// Times are journey seconds; radii are multiples of body radius.
/// </summary>
internal sealed class EncounterPresentationProfile
{
    public required CelestialId BodyId { get; init; }
    public required double EncounterTime { get; init; }
    public required double ApproachStart { get; init; }
    public required double FocusBlendInStart { get; init; }
    public required double FullFocusStart { get; init; }
    public required double FullFocusEnd { get; init; }
    public required double FocusBlendOutEnd { get; init; }
    public required double LocalExitEnd { get; init; }
    public required double ClosestApproachRadii { get; init; }
    public required IReadOnlyList<LocalRadiusKey> ApproachKeys { get; init; }
    public required IReadOnlyList<LocalRadiusKey> DepartureKeys { get; init; }
    public EncounterFlybyStyle FlybyStyle { get; init; } = EncounterFlybyStyle.StandardOffset;
    public float FocusPeakWeight { get; init; } = 1f;
    public double PoleTiltDegrees { get; init; }
    /// <summary>Lateral scale at the encounter-time flyby point; defaults to last approach key.</summary>
    public double? ClosestLateralScale { get; init; }

    public bool HasDeparture => DepartureKeys.Count > 0;
}

internal readonly record struct LocalRadiusKey(double Time, double Radii, double LateralScale);

internal static class EncounterCatalog
{
    /// <summary>Earth retains its dedicated opening-shot policy in JourneyController.</summary>
    public static readonly EncounterPresentationProfile Earth = new()
    {
        BodyId = CelestialId.Earth,
        EncounterTime = SolarSystem.EarthEncounter,
        ApproachStart = 0,
        FocusBlendInStart = 0,
        FullFocusStart = 0,
        FullFocusEnd = 6,
        FocusBlendOutEnd = 16,
        LocalExitEnd = 16,
        ClosestApproachRadii = 12,
        ApproachKeys = [],
        DepartureKeys = [],
        FocusPeakWeight = 1f
    };

    public static readonly EncounterPresentationProfile Moon = new()
    {
        BodyId = CelestialId.Moon,
        EncounterTime = SolarSystem.MoonEncounter,
        ApproachStart = 52,
        FocusBlendInStart = 58,
        FullFocusStart = 65,
        FullFocusEnd = 86,
        FocusBlendOutEnd = 98,
        LocalExitEnd = 98,
        ClosestApproachRadii = 16,
        ApproachKeys =
        [
            new(52, 70, 0.42),
            new(60, 40, 0.42),
            new(67, 24, 0.42),
            new(71, 18, 0.42)
        ],
        DepartureKeys =
        [
            new(79, 18, 0.42),
            new(86, 24, 0.42),
            new(92, 40, 0.42),
            new(98, 70, 0.42)
        ],
        ClosestLateralScale = 0.42,
        FlybyStyle = EncounterFlybyStyle.StandardOffset,
        FocusPeakWeight = 0.78f
    };

    /// <summary>Accepted Mars choreography from 89a6524 — preserve timing and radii.</summary>
    public static readonly EncounterPresentationProfile Mars = new()
    {
        BodyId = CelestialId.Mars,
        EncounterTime = SolarSystem.MarsEncounter,
        ApproachStart = 205,
        FocusBlendInStart = 215,
        FullFocusStart = 225,
        FullFocusEnd = 248,
        FocusBlendOutEnd = 260,
        LocalExitEnd = 290,
        ClosestApproachRadii = 8,
        ApproachKeys =
        [
            new(205, 400, 0.35),
            new(215, 150, 0.40),
            new(225, 50, 0.42),
            new(233, 20, 0.44)
        ],
        DepartureKeys =
        [
            new(247, 20, 0.44),
            new(255, 50, 0.42),
            new(265, 150, 0.40),
            new(290, 400, 0.35)
        ],
        ClosestLateralScale = 0.45,
        FocusPeakWeight = 1f
    };

    public static readonly EncounterPresentationProfile Jupiter = new()
    {
        BodyId = CelestialId.Jupiter,
        EncounterTime = SolarSystem.JupiterEncounter,
        ApproachStart = 560,
        FocusBlendInStart = 570,
        FullFocusStart = 580,
        FullFocusEnd = 620,
        FocusBlendOutEnd = 635,
        LocalExitEnd = 645,
        ClosestApproachRadii = 4.5,
        ApproachKeys =
        [
            new(560, 120, 0.34),
            new(572, 50, 0.35),
            new(585, 18, 0.35),
            new(593, 8, 0.35)
        ],
        DepartureKeys =
        [
            new(607, 8, 0.35),
            new(615, 18, 0.35),
            new(628, 50, 0.35),
            new(645, 120, 0.34)
        ],
        ClosestLateralScale = 0.35,
        FocusPeakWeight = 1f
    };

    public static readonly EncounterPresentationProfile Saturn = new()
    {
        BodyId = CelestialId.Saturn,
        EncounterTime = SolarSystem.SaturnEncounter,
        ApproachStart = 765,
        FocusBlendInStart = 775,
        FullFocusStart = 785,
        FullFocusEnd = 832,
        FocusBlendOutEnd = 848,
        LocalExitEnd = 860,
        ClosestApproachRadii = 5.5,
        ApproachKeys =
        [
            new(765, 100, 0.28),
            new(778, 40, 0.28),
            new(792, 15, 0.28),
            new(802, 8, 0.28)
        ],
        DepartureKeys =
        [
            new(818, 8, 0.28),
            new(828, 15, 0.28),
            new(842, 40, 0.28),
            new(860, 100, 0.28)
        ],
        FlybyStyle = EncounterFlybyStyle.OverPole,
        PoleTiltDegrees = 26.7,
        FocusPeakWeight = 1f
    };

    public static readonly EncounterPresentationProfile Uranus = new()
    {
        BodyId = CelestialId.Uranus,
        EncounterTime = SolarSystem.UranusEncounter,
        ApproachStart = 1105,
        FocusBlendInStart = 1112,
        FullFocusStart = 1122,
        FullFocusEnd = 1158,
        FocusBlendOutEnd = 1172,
        LocalExitEnd = 1182,
        ClosestApproachRadii = 6,
        ApproachKeys =
        [
            new(1105, 120, 0.34),
            new(1114, 50, 0.35),
            new(1125, 18, 0.35),
            new(1133, 9, 0.35)
        ],
        DepartureKeys =
        [
            new(1147, 9, 0.35),
            new(1155, 18, 0.35),
            new(1168, 50, 0.35),
            new(1182, 120, 0.34)
        ],
        ClosestLateralScale = 0.35,
        FocusPeakWeight = 0.88f
    };

    public static readonly EncounterPresentationProfile Neptune = new()
    {
        BodyId = CelestialId.Neptune,
        EncounterTime = SolarSystem.NeptuneEncounter,
        ApproachStart = 1465,
        FocusBlendInStart = 1472,
        FullFocusStart = 1482,
        FullFocusEnd = 1518,
        FocusBlendOutEnd = 1532,
        LocalExitEnd = 1542,
        ClosestApproachRadii = 6,
        ApproachKeys =
        [
            new(1465, 120, 0.34),
            new(1474, 50, 0.35),
            new(1485, 18, 0.35),
            new(1493, 9, 0.35)
        ],
        DepartureKeys =
        [
            new(1507, 9, 0.35),
            new(1515, 18, 0.35),
            new(1528, 50, 0.35),
            new(1542, 120, 0.34)
        ],
        ClosestLateralScale = 0.35,
        FocusPeakWeight = 0.88f
    };

    /// <summary>Approach / close encounter only — no post-Pluto departure yet.</summary>
    public static readonly EncounterPresentationProfile Pluto = new()
    {
        BodyId = CelestialId.Pluto,
        EncounterTime = SolarSystem.PlutoEncounter,
        ApproachStart = 1765,
        FocusBlendInStart = 1770,
        FullFocusStart = 1778,
        FullFocusEnd = JourneyController.DurationSeconds,
        FocusBlendOutEnd = JourneyController.DurationSeconds + 0.01,
        LocalExitEnd = SolarSystem.PlutoEncounter,
        ClosestApproachRadii = 3,
        ApproachKeys =
        [
            new(1765, 100, 0.22),
            new(1774, 40, 0.22),
            new(1782, 15, 0.20),
            new(1789, 7, 0.20),
            new(1794, 4, 0.20)
        ],
        DepartureKeys = [],
        ClosestLateralScale = 0.20,
        FocusPeakWeight = 1f
    };

    public static IReadOnlyList<EncounterPresentationProfile> MajorBodies { get; } =
    [
        Earth, Moon, Mars, Jupiter, Saturn, Uranus, Neptune, Pluto
    ];

    /// <summary>Bodies that use generic path-tangent ↔ body focus blending (not Earth opening).</summary>
    public static IReadOnlyList<EncounterPresentationProfile> PathFocusBodies { get; } =
    [
        Moon, Mars, Jupiter, Saturn, Uranus, Neptune, Pluto
    ];

    public static EncounterPresentationProfile Get(CelestialId id) =>
        MajorBodies.First(p => p.BodyId == id);

    public static float EvaluateFocusWeight(EncounterPresentationProfile profile, double time)
    {
        if (time <= profile.FocusBlendInStart)
            return 0f;
        if (time < profile.FullFocusStart)
        {
            var t = (time - profile.FocusBlendInStart) / Math.Max(profile.FullFocusStart - profile.FocusBlendInStart, 1e-6);
            return profile.FocusPeakWeight * SmoothStep01(t);
        }

        if (time <= profile.FullFocusEnd)
            return profile.FocusPeakWeight;

        if (time < profile.FocusBlendOutEnd)
        {
            var t = (time - profile.FullFocusEnd) / Math.Max(profile.FocusBlendOutEnd - profile.FullFocusEnd, 1e-6);
            return profile.FocusPeakWeight * (1f - SmoothStep01(t));
        }

        return 0f;
    }

    private static float SmoothStep01(double t)
    {
        t = Math.Clamp(t, 0, 1);
        return (float)(t * t * (3 - 2 * t));
    }
}
