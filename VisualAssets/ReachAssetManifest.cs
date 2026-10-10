using System.Reflection;
using System.Text.Json;

namespace TheReachScreensaver.VisualAssets;

internal sealed record ReachAsset(
    string AssetId, string DestinationBodyId, string SourceAttribution, string LicenseUsageTerms,
    string SourceUrl, string DownloadPath, string LocalRelativePath, long ExpectedFileSize,
    string Sha256, int Width, int Height);

internal sealed record ReachAssetManifest(int Version, ReachAsset[] Assets)
{
    public static ReachAssetManifest LoadPinned()
    {
        using var stream = Assembly.GetExecutingAssembly().GetManifestResourceStream(
            "TheReachScreensaver.VisualAssets.reach-assets.manifest.json")
            ?? throw new InvalidDataException("Pinned visual asset manifest is missing.");
        var manifest = JsonSerializer.Deserialize<ReachAssetManifest>(stream,
            new JsonSerializerOptions { PropertyNameCaseInsensitive = true })
            ?? throw new InvalidDataException("Pinned visual asset manifest is empty.");
        manifest.Validate();
        return manifest;
    }

    public void Validate()
    {
        if (Version != 1 || Assets is null) throw new InvalidDataException("Unsupported visual asset manifest version.");
        if (Assets.Sum(asset => asset.ExpectedFileSize) > 268_435_456)
            throw new InvalidDataException("Pinned visual asset manifest exceeds the 256 MiB download limit.");
        var ids = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        var paths = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        foreach (var asset in Assets)
        {
            if (!ValidId(asset.AssetId) || !ValidId(asset.DestinationBodyId)
                || !ids.Add(asset.AssetId) || !paths.Add(asset.LocalRelativePath)
                || string.IsNullOrWhiteSpace(asset.SourceAttribution)
                || string.IsNullOrWhiteSpace(asset.LicenseUsageTerms)
                || !ValidHttps(asset.SourceUrl)
                || !ValidRelativePath(asset.DownloadPath) || !ValidRelativePath(asset.LocalRelativePath)
                || asset.ExpectedFileSize is < 1 or > 33_554_432
                || asset.Width is < 1 or > 8192 || asset.Height is < 1 or > 8192
                || asset.Sha256.Length != 64 || !asset.Sha256.All(Uri.IsHexDigit))
                throw new InvalidDataException($"Invalid pinned visual asset record: {asset.AssetId}.");
        }
    }

    internal static bool ValidRelativePath(string path) =>
        !string.IsNullOrWhiteSpace(path) && path.Length < 240 && !path.Contains('\\')
        && path.Split('/').All(part => part.Length > 0 && part is not ("." or "..")
            && part.All(ch => char.IsAsciiLetterOrDigit(ch) || ch is '-' or '_' or '.'));

    private static bool ValidId(string value) => value.Length is > 0 and <= 80
        && value.All(ch => char.IsAsciiLetterOrDigit(ch) || ch is '-' or '_');

    private static bool ValidHttps(string value) => Uri.TryCreate(value, UriKind.Absolute, out var uri)
        && uri.Scheme == Uri.UriSchemeHttps && uri.UserInfo.Length == 0;
}
