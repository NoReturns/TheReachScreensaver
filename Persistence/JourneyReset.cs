using System.Text.Json;

namespace TheReachScreensaver.Persistence;

internal static class JourneyReset
{
    internal const string FullscreenMutexName = @"Local\TheReachScreensaverFullscreenVoyage";
    internal const string CheckpointKey = "the-reach:journey:v1";
    private const string MarkerName = "pending-journey-reset.txt";

    private static string DefaultRoot => VisualAssets.ReachVisualConfiguration.RootDirectory;
    private static string MarkerPath(string root) => Path.Combine(root, MarkerName);

    public static string? PendingToken() => PendingToken(DefaultRoot);

    internal static string? PendingToken(string root)
    {
        var path = MarkerPath(root);
        string token;
        try { token = File.ReadAllText(path).Trim(); }
        catch (FileNotFoundException) { return null; }
        catch (DirectoryNotFoundException) { return null; }
        if (token.Length != 32 || !Guid.TryParseExact(token, "N", out _))
            throw new InvalidDataException("The pending journey reset marker is invalid.");
        return token;
    }

    public static void Schedule() => Schedule(DefaultRoot, FullscreenMutexName);

    internal static void Schedule(string root, string mutexName)
    {
        using var mutex = new Mutex(false, mutexName);
        bool acquired;
        try { acquired = mutex.WaitOne(0); }
        catch (AbandonedMutexException) { acquired = true; }
        if (!acquired)
            throw new InvalidOperationException("Close the running fullscreen screensaver before resetting the trip.");
        try
        {
            if (PendingToken(root) is not null) return;
            Directory.CreateDirectory(root);
            var marker = MarkerPath(root);
            var temporary = marker + "." + Guid.NewGuid().ToString("N") + ".tmp";
            try
            {
                using (var file = new FileStream(temporary, FileMode.CreateNew, FileAccess.Write, FileShare.None))
                using (var writer = new StreamWriter(file))
                {
                    writer.Write(Guid.NewGuid().ToString("N"));
                    writer.Flush();
                    file.Flush(flushToDisk: true);
                }
                File.Move(temporary, marker, true);
                if (PendingToken(root) is null) throw new IOException("Journey reset marker was not saved.");
            }
            finally { if (File.Exists(temporary)) File.Delete(temporary); }
        }
        finally { mutex.ReleaseMutex(); }
    }

    public static bool Complete(string token) => Complete(token, DefaultRoot);

    internal static bool Complete(string token, string root)
    {
        var marker = MarkerPath(root);
        if (PendingToken(root) != token) return false;
        File.Delete(marker);
        if (File.Exists(marker)) throw new IOException("Journey reset marker could not be cleared.");
        return true;
    }

    internal static string RemovalScript =>
        "try { localStorage.removeItem('" + CheckpointKey + "'); window.__reachResetApplied = true; } " +
        "catch { window.__reachResetApplied = false; }";

    internal static string ProbeScript =>
        "JSON.stringify({applied: window.__reachResetApplied === true, checkpoint: " +
        "localStorage.getItem('" + CheckpointKey + "')})";

    internal static bool IsFreshCheckpoint(string scriptResult)
    {
        try
        {
            var payload = JsonSerializer.Deserialize<string>(scriptResult);
            if (payload is null) return false;
            using var probe = JsonDocument.Parse(payload);
            if (!probe.RootElement.GetProperty("applied").GetBoolean()) return false;
            var checkpointText = probe.RootElement.GetProperty("checkpoint").GetString();
            if (checkpointText is null) return false;
            using var checkpoint = JsonDocument.Parse(checkpointText);
            var state = checkpoint.RootElement;
            var itinerary = state.GetProperty("itinerary");
            var odometer = state.GetProperty("odometer");
            return state.GetProperty("schema").GetInt32() == 1
                && itinerary.GetProperty("id").GetString() == "solar-system-41"
                && itinerary.GetProperty("version").GetInt32() is 4 or 5
                && state.GetProperty("destinationId").GetString() == "earth"
                && state.GetProperty("routeIndex").GetInt32() == 0
                && state.GetProperty("phase").GetProperty("kind").GetString() == "observe"
                && odometer.GetProperty("completedKm").GetDouble() == 0
                && odometer.GetProperty("roundTrips").GetInt32() == 0
                && odometer.GetProperty("leg").ValueKind == JsonValueKind.Null;
        }
        catch (Exception ex) when (ex is JsonException or InvalidOperationException or KeyNotFoundException
            or FormatException or OverflowException)
        {
            return false;
        }
    }
}
