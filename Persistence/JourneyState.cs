using System.Text.Json.Serialization;

namespace TheReachScreensaver.Persistence;

internal sealed class JourneyState
{
    public const int CurrentVersion = 1;
    public const int DefaultSeed = 18472931;

    [JsonPropertyName("version")]
    public int Version { get; set; } = CurrentVersion;

    [JsonPropertyName("journeySeconds")]
    public double JourneySeconds { get; set; }

    [JsonPropertyName("seed")]
    public int Seed { get; set; } = DefaultSeed;

    public static JourneyState CreateDefault() => new();
}
