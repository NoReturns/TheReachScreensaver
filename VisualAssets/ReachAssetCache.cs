using System.Net;
using System.Security.Cryptography;

namespace TheReachScreensaver.VisualAssets;

internal sealed class ReachAssetCache
{
    internal sealed record Failure(string AssetId, string Source, string Reason);
    internal sealed record AcquisitionResult(int Installed, int Skipped, IReadOnlyList<Failure> Failures);
    private readonly ReachAssetManifest _manifest;
    private readonly ReachVisualConfiguration _configuration;

    public ReachAssetCache(ReachAssetManifest manifest, ReachVisualConfiguration configuration)
    {
        manifest.Validate();
        _manifest = manifest;
        _configuration = configuration;
    }

    public IReadOnlyList<ReachAsset> VerifiedAssets()
    {
        if (_configuration.Mode != ReachVisualMode.Enhanced) return [];
        return _manifest.Assets.Where(IsVerified).ToArray();
    }

    public bool IsVerified(ReachAsset asset)
    {
        try
        {
            var path = LocalPath(asset);
            var file = new FileInfo(path);
            if (!file.Exists || file.Length != asset.ExpectedFileSize) return false;
            using var stream = file.OpenRead();
            var hash = SHA256.HashData(stream);
            return CryptographicOperations.FixedTimeEquals(hash, Convert.FromHexString(asset.Sha256));
        }
        catch { return false; }
    }

    public long MissingByteCount() => _manifest.Assets.Where(asset => !IsVerified(asset)).Sum(asset => asset.ExpectedFileSize);

    public async Task<int> AcquireApprovedAsync(bool consent, HttpClient? client = null, CancellationToken cancellationToken = default)
        => (await AcquireApprovedDetailedAsync(consent, client, cancellationToken)).Installed;

    public async Task<AcquisitionResult> AcquireApprovedDetailedAsync(bool consent, HttpClient? client = null, CancellationToken cancellationToken = default)
    {
        if (!consent || _configuration.Mode != ReachVisualMode.Enhanced)
            return new(0, _manifest.Assets.Length, []);
        Directory.CreateDirectory(_configuration.AssetDirectory);
        using var setupLock = new FileStream(Path.Combine(_configuration.AssetDirectory, ".reach-asset-setup.lock"),
            FileMode.OpenOrCreate, FileAccess.ReadWrite, FileShare.None);
        var missing = _manifest.Assets.Where(asset => !IsVerified(asset)).ToArray();
        if (missing.Length == 0) return new(0, _manifest.Assets.Length, []);
        var total = missing.Sum(asset => asset.ExpectedFileSize);
        if (total > 268_435_456) throw new InvalidDataException("Asset download batch exceeds 256 MiB.");
        var drive = new DriveInfo(Path.GetPathRoot(Path.GetFullPath(_configuration.AssetDirectory))!);
        if (drive.AvailableFreeSpace < total * 2 + 67_108_864)
            throw new IOException("Insufficient disk space for verified asset installation.");

        var ownsClient = client is null;
        client ??= new HttpClient(new HttpClientHandler { AllowAutoRedirect = false })
            { Timeout = TimeSpan.FromSeconds(30) };
        try
        {
            var installed = 0;
            var failures = new List<Failure>();
            foreach (var asset in missing)
            {
                cancellationToken.ThrowIfCancellationRequested();
                if (IsVerified(asset)) continue;
                var url = _configuration.SourceFor(asset);
                try
                {
                    var destination = LocalPath(asset);
                    var parent = Path.GetDirectoryName(destination)!;
                    Directory.CreateDirectory(parent);
                    foreach (var stale in Directory.EnumerateFiles(parent, "." + Path.GetFileName(destination) + ".*.tmp"))
                    {
                        if (File.GetLastWriteTimeUtc(stale) < DateTime.UtcNow.AddHours(-1)) File.Delete(stale);
                    }
                    var temporary = Path.Combine(parent, "." + Path.GetFileName(destination) + "." + Guid.NewGuid().ToString("N") + ".tmp");
                    try
                    {
                    using var deadline = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
                    deadline.CancelAfter(TimeSpan.FromSeconds(60));
                    using var response = await client.GetAsync(url, HttpCompletionOption.ResponseHeadersRead, deadline.Token);
                    if (response.StatusCode != HttpStatusCode.OK)
                        throw new HttpRequestException($"Asset {asset.AssetId} returned HTTP {(int)response.StatusCode}.");
                    if (response.Content.Headers.ContentLength is long length && length != asset.ExpectedFileSize)
                        throw new InvalidDataException($"Unexpected size for {asset.AssetId}.");
                    await using (var source = await response.Content.ReadAsStreamAsync(deadline.Token))
                    await using (var target = new FileStream(temporary, FileMode.CreateNew, FileAccess.Write, FileShare.None, 65536, FileOptions.WriteThrough))
                    {
                        var buffer = new byte[65536];
                        long bytes = 0;
                        int read;
                        while ((read = await source.ReadAsync(buffer, deadline.Token)) > 0)
                        {
                            bytes += read;
                            if (bytes > asset.ExpectedFileSize) throw new InvalidDataException($"Oversized asset {asset.AssetId}.");
                            await target.WriteAsync(buffer.AsMemory(0, read), deadline.Token);
                        }
                        if (bytes != asset.ExpectedFileSize) throw new InvalidDataException($"Incomplete asset {asset.AssetId}.");
                    }
                    using (var stream = File.OpenRead(temporary))
                    {
                        var actual = SHA256.HashData(stream);
                        if (!CryptographicOperations.FixedTimeEquals(actual, Convert.FromHexString(asset.Sha256)))
                            throw new InvalidDataException($"SHA-256 mismatch for {asset.AssetId}.");
                    }
                    File.Move(temporary, destination, true);
                    installed++;
                    }
                    finally { if (File.Exists(temporary)) File.Delete(temporary); }
                }
                catch (Exception ex) when (ex is HttpRequestException or IOException or InvalidDataException
                    or TaskCanceledException or UnauthorizedAccessException)
                {
                    if (cancellationToken.IsCancellationRequested) throw;
                    // Query strings may hold mirror credentials; log only the public path.
                    var reason = ex.Message.Replace(url.AbsoluteUri, "[source]", StringComparison.OrdinalIgnoreCase);
                    if (url.Query.Length > 0) reason = reason.Replace(url.Query, "[query]", StringComparison.OrdinalIgnoreCase);
                    failures.Add(new(asset.AssetId, url.GetLeftPart(UriPartial.Path), ex.GetType().Name + ": " + reason));
                }
            }
            return new(installed, _manifest.Assets.Length - missing.Length, failures);
        }
        finally { if (ownsClient) client.Dispose(); }
    }

    public string LocalPath(ReachAsset asset)
    {
        if (!ReachAssetManifest.ValidRelativePath(asset.LocalRelativePath))
            throw new InvalidDataException("Unsafe asset path.");
        var root = Path.GetFullPath(_configuration.AssetDirectory);
        var path = Path.GetFullPath(Path.Combine(root, asset.LocalRelativePath.Replace('/', Path.DirectorySeparatorChar)));
        if (!path.StartsWith(root.TrimEnd(Path.DirectorySeparatorChar) + Path.DirectorySeparatorChar, StringComparison.OrdinalIgnoreCase))
            throw new InvalidDataException("Asset path escapes the asset directory.");
        for (var current = Path.GetDirectoryName(path); current is not null && current.Length >= root.Length;
             current = Path.GetDirectoryName(current))
        {
            if (Directory.Exists(current) && (File.GetAttributes(current) & FileAttributes.ReparsePoint) != 0)
                throw new InvalidDataException("Asset path crosses a symbolic link.");
            if (current.Equals(root, StringComparison.OrdinalIgnoreCase)) break;
        }
        if (File.Exists(path) && (File.GetAttributes(path) & FileAttributes.ReparsePoint) != 0)
            throw new InvalidDataException("Asset file is a symbolic link.");
        return path;
    }
}
