namespace TheReachScreensaver.Journey;

/// <summary>
/// Authoritative Solar-System scale. Positions are stored in AU; radii in AU.
/// 1 AU = 149,597,870.7 km.
/// </summary>
internal static class AstronomicalUnits
{
    public const double KilometersPerAu = 149_597_870.7;

    public static double KilometersToAu(double km) => km / KilometersPerAu;
    public static double AuToKilometers(double au) => au * KilometersPerAu;

    // Mean / equatorial radii (km).
    public const double EarthRadiusKm = 6_371;
    public const double MoonRadiusKm = 1_737;
    public const double MarsRadiusKm = 3_390;
    public const double JupiterRadiusKm = 69_911;
    public const double SaturnRadiusKm = 58_232;
    public const double UranusRadiusKm = 25_362;
    public const double NeptuneRadiusKm = 24_622;
    public const double PlutoRadiusKm = 1_188;

    public static readonly double EarthRadius = KilometersToAu(EarthRadiusKm);
    public static readonly double MoonRadius = KilometersToAu(MoonRadiusKm);
    public static readonly double MarsRadius = KilometersToAu(MarsRadiusKm);
    public static readonly double JupiterRadius = KilometersToAu(JupiterRadiusKm);
    public static readonly double SaturnRadius = KilometersToAu(SaturnRadiusKm);
    public static readonly double UranusRadius = KilometersToAu(UranusRadiusKm);
    public static readonly double NeptuneRadius = KilometersToAu(NeptuneRadiusKm);
    public static readonly double PlutoRadius = KilometersToAu(PlutoRadiusKm);

    // Heliocentric semi-major axis distances (AU).
    public const double EarthOrbitAu = 1.000;
    public const double MarsOrbitAu = 1.524;
    public const double JupiterOrbitAu = 5.203;
    public const double SaturnOrbitAu = 9.537;
    public const double UranusOrbitAu = 19.191;
    public const double NeptuneOrbitAu = 30.070;
    public const double PlutoOrbitAu = 39.48;

    public const double MoonDistanceKm = 384_400;
    public static readonly double MoonDistanceAu = KilometersToAu(MoonDistanceKm);

    // Authored ecliptic longitudes (degrees). Not ephemeris-accurate; spread destinations
    // around Sol so the Solar System does not read as a single forward queue.
    public const double EarthLongitudeDeg = 0;
    public const double MarsLongitudeDeg = 52;
    public const double JupiterLongitudeDeg = 138;
    public const double SaturnLongitudeDeg = 212;
    public const double UranusLongitudeDeg = 288;
    public const double NeptuneLongitudeDeg = 332;
    public const double PlutoLongitudeDeg = 24;

    // Small ecliptic inclinations (degrees) for visual variety.
    public const double MarsInclinationDeg = 1.85;
    public const double JupiterInclinationDeg = 1.30;
    public const double SaturnInclinationDeg = 2.49;
    public const double UranusInclinationDeg = 0.77;
    public const double NeptuneInclinationDeg = 1.77;
    public const double PlutoInclinationDeg = 17.14;
}
