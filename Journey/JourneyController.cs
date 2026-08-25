using OpenTK.Mathematics;
using TheReachScreensaver.Diagnostics;
using TheReachScreensaver.Journey.Coordinates;
using TheReachScreensaver.Rendering;
using Vector3d = TheReachScreensaver.Journey.Coordinates.Vector3d;

namespace TheReachScreensaver.Journey;

internal sealed class JourneyController
{
    public const double DurationSeconds = 1800.0;
    public const double FadeInSeconds = 3.5;
    public const double FadeOutSeconds = 4.0;
    private const double StartupEarthHoldEnd = 6.0;
    private const double StartupEarthBlendEnd = 16.0;

    private readonly JourneyPath _path = new();
    private readonly List<CelestialBody> _bodies;
    private Vector3 _smoothedLook = -Vector3.UnitZ;
    private bool _lookInitialized;
    private bool _fadeInAfterLoop;

    public JourneyController(double savedSeconds)
    {
        _bodies = SolarSystem.CreateBodies().ToList();
        Time = Normalize(savedSeconds);
        SnapLook();
        SolarSystem.UpdateSatellites(_bodies, Time);
        Log.Info($"JourneyController ready at {Time:0.###}s / {DurationSeconds:0}s.");
    }

    public double Time { get; private set; }
    public IReadOnlyList<CelestialBody> Bodies => _bodies;

    /// <summary>1 = fully visible scene, 0 = black.</summary>
    public float Visibility
    {
        get
        {
            if (_fadeInAfterLoop && Time < FadeInSeconds)
                return (float)(Time / FadeInSeconds);
            if (Time > DurationSeconds - FadeOutSeconds)
                return (float)((DurationSeconds - Time) / FadeOutSeconds);
            return 1f;
        }
    }

    public void Advance(double deltaSeconds)
    {
        if (deltaSeconds <= 0)
            return;
        var previous = Time;
        Time = Normalize(Time + deltaSeconds);
        if (Time < previous)
            _fadeInAfterLoop = true;
        if (Time >= FadeInSeconds)
            _fadeInAfterLoop = false;
        SolarSystem.UpdateSatellites(_bodies, Time);
    }

    public void Seek(double seconds, bool snapOrientation = true)
    {
        Time = Normalize(seconds);
        _fadeInAfterLoop = false;
        SolarSystem.UpdateSatellites(_bodies, Time);
        if (snapOrientation)
            SnapLook();
        var (pos, tangent) = _path.Evaluate(Time);
        Log.Info($"Journey seek to {FormatTime(Time)}. camera={pos} ({pos.Length:0.###} AU from Sol) look={tangent.Normalized().ToVector3()}");
        foreach (var body in _bodies.Where(b => !b.IsSatellite))
        {
            var delta = body.WorldPosition - pos;
            var distAu = delta.Length;
            var projected = BodyAppearance.ComputeProjectedRadiusPixels(
                body.Radius,
                distAu,
                720,
                MathHelper.DegreesToRadians(55f));
            var mode = BodyAppearance.Evaluate(body, pos, 720, MathHelper.DegreesToRadians(55f)).Mode;
            Log.Info($"  {body.Name} dist={distAu:0.###} AU ({AstronomicalUnits.AuToKilometers(distAu):0} km) proj={projected:0.00}px mode={mode}");
        }
    }

    public void Apply(Camera camera, double deltaSeconds, bool smoothLook)
    {
        var (position, tangent) = _path.Evaluate(Time);
        var look = DesiredLook(position, tangent, Time);

        if (!_lookInitialized || !smoothLook)
        {
            _smoothedLook = look;
            _lookInitialized = true;
        }
        else
        {
            var alpha = 1f - MathF.Exp(-(float)Math.Max(deltaSeconds, 0.0) / 1.6f);
            _smoothedLook = Vector3.Normalize(Vector3.Lerp(_smoothedLook, look, alpha));
        }

        camera.WorldPosition = position;
        camera.LookDirection = _smoothedLook;
        camera.WorldUp = Vector3.UnitY;
    }

    public string BuildDevDiagnostics(Camera camera, float referenceHeightPixels)
    {
        var camAu = camera.WorldPosition.Length;
        var next = SolarSystem.NextDestination(_bodies, Time);
        var destName = next?.Name ?? "Loop";
        var distAu = 0.0;
        var projected = 0.0;
        var mode = BodyRenderMode.Hidden;

        if (next is not null)
        {
            var appearance = BodyAppearance.Evaluate(
                next,
                camera.WorldPosition,
                referenceHeightPixels,
                camera.FieldOfViewY);
            distAu = appearance.DistanceAu;
            projected = appearance.ProjectedRadiusPixels;
            mode = appearance.Mode;
        }

        return $"{FormatTime(Time)} | {destName} | cam {camAu:0.00} AU | {distAu:0.00} AU | {projected:0.00} px | {ModeLabel(mode)}";
    }

    public static double Normalize(double seconds)
    {
        if (double.IsNaN(seconds) || double.IsInfinity(seconds) || seconds < 0)
            return 0;
        var wrapped = seconds % DurationSeconds;
        if (wrapped < 0)
            wrapped += DurationSeconds;
        if (wrapped >= DurationSeconds)
            wrapped = 0;
        return wrapped;
    }

    public static string FormatTime(double seconds)
    {
        var t = (int)Math.Floor(Normalize(seconds));
        return $"{t / 60:00}:{t % 60:00}";
    }

    private static string ModeLabel(BodyRenderMode mode) => mode switch
    {
        BodyRenderMode.Hidden => "HIDDEN",
        BodyRenderMode.Point => "POINT",
        BodyRenderMode.Transition => "BLEND",
        BodyRenderMode.Sphere => "SPHERE",
        _ => mode.ToString().ToUpperInvariant()
    };

    private void SnapLook()
    {
        var (position, tangent) = _path.Evaluate(Time);
        _smoothedLook = DesiredLook(position, tangent, Time);
        _lookInitialized = true;
    }

    private Vector3 DesiredLook(Vector3d position, Vector3d tangent, double time)
    {
        var pathLook = tangent.LengthSquared > 1e-16 ? tangent.Normalized().ToVector3() : -Vector3.UnitZ;
        if (time >= StartupEarthBlendEnd)
            return pathLook;

        var toEarth = (SolarSystem.EarthHeliocentric - position).ToVector3();
        if (toEarth.LengthSquared < 1e-12f)
            return pathLook;

        var earthLook = Vector3.Normalize(toEarth);
        if (time <= StartupEarthHoldEnd)
            return earthLook;

        var blendT = (time - StartupEarthHoldEnd) / (StartupEarthBlendEnd - StartupEarthHoldEnd);
        blendT = Math.Clamp(blendT, 0, 1);
        var blend = blendT * blendT * (3 - 2 * blendT);
        var blended = Vector3.Lerp(earthLook, pathLook, (float)blend);
        if (blended.LengthSquared < 1e-12f)
            return blend < 0.5 ? earthLook : pathLook;

        return Vector3.Normalize(blended);
    }
}
