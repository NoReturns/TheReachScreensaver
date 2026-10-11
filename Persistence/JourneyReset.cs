namespace TheReachScreensaver.Persistence;

internal static class JourneyReset
{
    private static string PendingPath => Path.Combine(VisualAssets.ReachVisualConfiguration.RootDirectory,
        "pending-journey-reset.txt");

    public static string? PendingToken()
    {
        try { return File.Exists(PendingPath) ? File.ReadAllText(PendingPath).Trim() : null; }
        catch (Exception ex)
        {
            Diagnostics.Log.Warn($"Could not inspect pending journey reset: {ex.Message}");
            return null;
        }
    }

    public static void Schedule()
    {
        Directory.CreateDirectory(VisualAssets.ReachVisualConfiguration.RootDirectory);
        var temporary = PendingPath + "." + Guid.NewGuid().ToString("N") + ".tmp";
        try
        {
            File.WriteAllText(temporary, Guid.NewGuid().ToString("N"));
            File.Move(temporary, PendingPath, true);
        }
        finally { if (File.Exists(temporary)) File.Delete(temporary); }
    }

    public static void Complete(string token)
    {
        if (PendingToken() == token) File.Delete(PendingPath);
    }
}
