namespace TheReachScreensaver.VisualAssets;

internal enum ReachVisualMode { Basic, Procedural, Enhanced }

internal sealed record ReachVisualConfiguration(
    ReachVisualMode Mode, Uri? SourceBaseUrl, bool DownloadOnFirstUse,
    string AssetDirectory, bool VerifyHashes, IReadOnlyDictionary<string, Uri>? SourceOverrides = null,
    bool CountdownEnabled = true, string CountdownPosition = "auto")
{
    public static string RootDirectory => Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "TheReachScreensaver");
    public static string IniPath => Path.Combine(RootDirectory, "reach.ini");
    public static string DefaultAssetDirectory => Path.Combine(RootDirectory, "Assets");

    public static ReachVisualConfiguration Default => new(
        ReachVisualMode.Basic, null, false, DefaultAssetDirectory, true);

    public static ReachVisualConfiguration Load()
    {
        if (!File.Exists(IniPath)) return Default;
        try { return Parse(File.ReadAllText(IniPath)); }
        catch (Exception ex)
        {
            Diagnostics.Log.Warn($"Could not read reach.ini; using basic defaults: {ex.Message}");
            return Default;
        }
    }

    public static void EnsureDefaultFile()
    {
        Directory.CreateDirectory(RootDirectory);
        if (File.Exists(IniPath)) return;
        const string template = "[Visuals]\r\nMode=basic\r\n\r\n[Countdown]\r\nEnabled=1\r\nPosition=auto\r\n\r\n[Downloads]\r\nSourceBaseUrl=\r\nDownloadOnFirstUse=0\r\nVerifyHashes=1\r\n\r\n[AssetSources]\r\n; earth-day-high=https://your-https-mirror.example/earth.jpg\r\n\r\n[Storage]\r\nAssetDirectory=%LOCALAPPDATA%\\TheReachScreensaver\\Assets\r\n";
        try
        {
            using var file = new FileStream(IniPath, FileMode.CreateNew, FileAccess.Write, FileShare.Read);
            using var writer = new StreamWriter(file);
            writer.Write(template);
        }
        catch (IOException) when (File.Exists(IniPath)) { }
    }

    public static ReachVisualConfiguration Parse(string text)
    {
        if (text.Length > 64 * 1024) return Default;
        var entries = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        var section = "";
        foreach (var original in text.Split('\n'))
        {
            var line = original.Trim();
            if (line.Length == 0 || line[0] is ';' or '#') continue;
            if (line.StartsWith('[') && line.EndsWith(']')) { section = line[1..^1].Trim(); continue; }
            var equals = line.IndexOf('=');
            if (equals <= 0) continue;
            entries[$"{section}.{line[..equals].Trim()}"] = line[(equals + 1)..].Trim();
        }

        var modeText = entries.GetValueOrDefault("Visuals.Mode", "basic");
        var mode = modeText.ToLowerInvariant() switch
        {
            "procedural" => ReachVisualMode.Procedural,
            "cached" or "download" or "enhanced" => ReachVisualMode.Enhanced,
            _ => ReachVisualMode.Basic
        };
        if (modeText.ToLowerInvariant() is not ("basic" or "procedural" or "enhanced" or "cached" or "download"))
            Diagnostics.Log.Warn("Invalid Visuals.Mode; using basic.");

        var countdownEnabledText = entries.GetValueOrDefault("Countdown.Enabled", "1");
        if (countdownEnabledText is not ("0" or "1"))
            Diagnostics.Log.Warn("Invalid Countdown.Enabled; using enabled.");
        var countdownPosition = entries.GetValueOrDefault("Countdown.Position", "auto").ToLowerInvariant();
        if (countdownPosition is not ("auto" or "top-left" or "top-centre" or "top-right"
            or "bottom-left" or "bottom-centre" or "bottom-right"))
        {
            Diagnostics.Log.Warn("Invalid Countdown.Position; using auto.");
            countdownPosition = "auto";
        }

        Uri? source = null;
        var sourceText = entries.GetValueOrDefault("Downloads.SourceBaseUrl", "");
        if (sourceText.Length > 0 && !TryParseSourceBaseUrl(sourceText, out source))
            Diagnostics.Log.Warn("Invalid Downloads.SourceBaseUrl; downloads are unavailable.");

        var downloadOnFirstUse = entries.GetValueOrDefault("Downloads.DownloadOnFirstUse", "0") == "1";
        if (entries.GetValueOrDefault("Downloads.VerifyHashes", "1") != "1")
            Diagnostics.Log.Warn("VerifyHashes=0 does not disable required asset integrity checks.");

        var pathText = entries.GetValueOrDefault("Storage.AssetDirectory", DefaultAssetDirectory);
        var expanded = Environment.ExpandEnvironmentVariables(pathText);
        var assetDirectory = IsSafeAssetDirectory(expanded) ? Path.GetFullPath(expanded) : DefaultAssetDirectory;
        if (assetDirectory == DefaultAssetDirectory && expanded != DefaultAssetDirectory)
            Diagnostics.Log.Warn("Invalid Storage.AssetDirectory; using the default asset directory.");
        var overrides = new Dictionary<string, Uri>(StringComparer.OrdinalIgnoreCase);
        foreach (var (key, value) in entries)
        {
            if (!key.StartsWith("AssetSources.", StringComparison.OrdinalIgnoreCase)) continue;
            var id = key["AssetSources.".Length..];
            if (id.Length is < 1 or > 80 || !id.All(ch => char.IsAsciiLetterOrDigit(ch) || ch is '-' or '_')
                || !TryParseAssetSourceUrl(value, out var overrideUrl))
            {
                Diagnostics.Log.Warn($"Invalid AssetSources entry for {id}; using the pinned source.");
                continue;
            }
            overrides[id] = overrideUrl!;
        }
        return new(mode, source, downloadOnFirstUse, assetDirectory, true, overrides,
            countdownEnabledText != "0", countdownPosition);
    }

    public Uri SourceFor(ReachAsset asset)
    {
        if (SourceOverrides?.TryGetValue(asset.AssetId, out var url) == true) return url;
        return SourceBaseUrl is null ? new Uri(asset.SourceUrl) : new Uri(SourceBaseUrl, asset.DownloadPath);
    }

    public static bool TryParseAssetSourceUrl(string value, out Uri? url)
    {
        url = null;
        if (!Uri.TryCreate(value, UriKind.Absolute, out var parsed) || parsed.Scheme != Uri.UriSchemeHttps
            || parsed.UserInfo.Length != 0 || parsed.Fragment.Length != 0
            || parsed.HostNameType == UriHostNameType.IPv4 || parsed.HostNameType == UriHostNameType.IPv6
            || parsed.Host.Equals("localhost", StringComparison.OrdinalIgnoreCase)) return false;
        url = parsed;
        return true;
    }

    public static bool TryParseSourceBaseUrl(string value, out Uri? url)
    {
        url = null;
        if (!Uri.TryCreate(value, UriKind.Absolute, out var parsed) || parsed.Scheme != Uri.UriSchemeHttps
            || parsed.UserInfo.Length != 0 || parsed.Query.Length != 0 || parsed.Fragment.Length != 0
            || parsed.HostNameType == UriHostNameType.IPv4 || parsed.HostNameType == UriHostNameType.IPv6
            || parsed.Host.Equals("localhost", StringComparison.OrdinalIgnoreCase)) return false;
        url = new Uri(parsed.AbsoluteUri.TrimEnd('/') + "/");
        return true;
    }

    private static bool IsSafeAssetDirectory(string path)
    {
        try { return Path.IsPathFullyQualified(path) && !path.StartsWith("\\\\") && !path.Contains('%')
                && Path.GetFullPath(path) != Path.GetPathRoot(path); }
        catch { return false; }
    }
}
