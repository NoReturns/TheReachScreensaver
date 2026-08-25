using System.Text.Json;
using TheReachScreensaver.Diagnostics;

namespace TheReachScreensaver.Persistence;

internal sealed class JourneyStateStore
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        WriteIndented = true,
        PropertyNameCaseInsensitive = true
    };

    public string RootDirectory { get; }
    public string StatePath { get; }

    public JourneyStateStore(string? rootDirectory = null)
    {
        RootDirectory = rootDirectory
            ?? Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
                "TheReachScreensaver");
        StatePath = Path.Combine(RootDirectory, "state.json");
    }

    public JourneyState Load()
    {
        try
        {
            Directory.CreateDirectory(RootDirectory);
            if (!File.Exists(StatePath))
            {
                Log.Info($"No journey state at {StatePath}; using defaults.");
                return JourneyState.CreateDefault();
            }

            var json = File.ReadAllText(StatePath);
            var state = JsonSerializer.Deserialize<JourneyState>(json, JsonOptions);
            if (state is null || state.Version < 1 || state.Seed == 0)
            {
                Log.Warn("Journey state was empty or invalid; recovering to defaults.");
                return JourneyState.CreateDefault();
            }

            Log.Info($"Loaded journey state: seconds={state.JourneySeconds:0.###}, seed={state.Seed}.");
            return state;
        }
        catch (Exception ex)
        {
            Log.Warn($"Failed to load journey state; recovering to defaults. {ex.Message}");
            return JourneyState.CreateDefault();
        }
    }

    public void Save(JourneyState state)
    {
        try
        {
            Directory.CreateDirectory(RootDirectory);
            state.Version = JourneyState.CurrentVersion;
            var json = JsonSerializer.Serialize(state, JsonOptions);
            var tempPath = StatePath + ".tmp";
            File.WriteAllText(tempPath, json);
            if (File.Exists(StatePath))
            {
                File.Replace(tempPath, StatePath, destinationBackupFileName: null);
            }
            else
            {
                File.Move(tempPath, StatePath);
            }
        }
        catch (Exception ex)
        {
            Log.Warn($"Failed to save journey state. {ex.Message}");
        }
    }
}
