using System.Net;
using System.Text;

namespace TheReachScreensaver.VisualAssets;

internal static class ReachAssetSelfTest
{
    private static readonly byte[] Payload = Encoding.ASCII.GetBytes("reach-fixture-image-v1");
    private const string Hash = "D080A7F03562EEBF1FDE5450B6963A22F1FDD8DEB902A4B859ACDB586CE8D6CA";

    public static int Run()
    {
        var root = Path.Combine(Path.GetTempPath(), "ReachAssetFixture-" + Guid.NewGuid().ToString("N"));
        try
        {
            var pinned = ReachAssetManifest.LoadPinned();
            Check(pinned.Assets.Length == 1 && pinned.Assets[0].AssetId == "earth-day-high",
                "production manifest pins optional Earth imagery");
            var asset = new ReachAsset("earth-day", "earth", "Local fixture", "Test data",
                "https://example.com/source", "earth/day.bin", "earth/day.bin", Payload.Length, Hash, 1, 1);
            var manifest = new ReachAssetManifest(1, [asset]);
            manifest.Validate();
            var source = new Uri("https://mirror.example.test/assets/");
            var procedural = new ReachVisualConfiguration(ReachVisualMode.Procedural, source, false, root, true);
            var basic = procedural with { Mode = ReachVisualMode.Basic };
            var enhanced = procedural with { Mode = ReachVisualMode.Enhanced };
            var handler = new FixtureHandler();
            using var client = new HttpClient(handler);
            var proceduralCache = new ReachAssetCache(manifest, procedural);
            Check(proceduralCache.VerifiedAssets().Count == 0, "procedural mode ignored assets");
            Check(proceduralCache.AcquireApprovedAsync(true, client).GetAwaiter().GetResult() == 0 && handler.Count == 0,
                "procedural mode did not request imagery");
            var basicCache = new ReachAssetCache(manifest, basic);
            Check(basicCache.VerifiedAssets().Count == 0, "basic mode uses bundled maps only");
            Check(basicCache.AcquireApprovedAsync(true, client).GetAwaiter().GetResult() == 0 && handler.Count == 0,
                "basic mode did not request imagery");
            var enhancedCache = new ReachAssetCache(manifest, enhanced);
            Check(enhancedCache.VerifiedAssets().Count == 0, "missing enhanced asset falls back");
            Check(enhancedCache.AcquireApprovedAsync(false, client).GetAwaiter().GetResult() == 0 && handler.Count == 0,
                "enhanced acquisition requires consent");
            handler.Corrupt = true;
            var corruptResult = enhancedCache.AcquireApprovedDetailedAsync(true, client).GetAwaiter().GetResult();
            Check(corruptResult.Installed == 0 && corruptResult.Failures.Count == 1
                && corruptResult.Failures[0].Reason.Contains("SHA-256 mismatch"), "corrupt download reported for retry");
            Check(!File.Exists(enhancedCache.LocalPath(asset))
                && !Directory.EnumerateFiles(root, "*.tmp", SearchOption.AllDirectories).Any(),
                "corrupt download left no installed or temporary file");
            handler.Corrupt = false;
            var countAfterCorrupt = handler.Count;
            Check(enhancedCache.AcquireApprovedAsync(true, client).GetAwaiter().GetResult() == 1
                && handler.Count == countAfterCorrupt + 1,
                "approved download installed one file");
            Check(handler.LastUri?.AbsoluteUri == "https://mirror.example.test/assets/earth/day.bin",
                "source override selected the expected mirror path");
            Check(enhancedCache.VerifiedAssets().Count == 1, "enhanced mode uses verified file");
            Check(basicCache.VerifiedAssets().Count == 0, "basic mode ignores cache");
            Check(proceduralCache.VerifiedAssets().Count == 0
                && proceduralCache.AcquireApprovedAsync(true, client).GetAwaiter().GetResult() == 0,
                "procedural mode ignores a populated cache");
            Check(enhancedCache.AcquireApprovedAsync(true, client).GetAwaiter().GetResult() == 0
                && handler.Count == countAfterCorrupt + 1,
                "valid file skipped on relaunch");
            File.WriteAllBytes(enhancedCache.LocalPath(asset), Encoding.ASCII.GetBytes(new string('x', Payload.Length)));
            Check(enhancedCache.VerifiedAssets().Count == 0, "corrupt file detected");
            Check(enhancedCache.AcquireApprovedAsync(true, client).GetAwaiter().GetResult() == 1
                && handler.Count == countAfterCorrupt + 2,
                "corrupt file replaced only after approval");
            var second = asset with { AssetId = "moon-day", DestinationBodyId = "moon",
                DownloadPath = "moon/day.bin", LocalRelativePath = "moon/day.bin" };
            var privateSource = new Uri("https://mirror.example.test/assets/earth/day.bin?token=fixture-secret");
            var multipleManifest = new ReachAssetManifest(1, [asset, second]);
            var multiple = new ReachAssetCache(multipleManifest, enhanced with
                { SourceOverrides = new Dictionary<string, Uri> { [asset.AssetId] = privateSource } });
            File.Delete(enhancedCache.LocalPath(asset));
            handler.FailAssetId = "earth";
            var partial = multiple.AcquireApprovedDetailedAsync(true, client).GetAwaiter().GetResult();
            Check(partial.Installed == 1 && partial.Failures.Count == 1 && partial.Failures[0].AssetId == "earth-day"
                && multiple.IsVerified(second) && !multiple.IsVerified(asset), "unreachable asset does not stop other downloads");
            Check(partial.Failures[0].Source.EndsWith("/earth/day.bin")
                && !partial.Failures[0].Reason.Contains("fixture-secret"), "failure report omits source credentials");
            var acquisitionLog = Path.Combine(root, "acquisition.log");
            ReachVisualSetup.WriteAcquisitionLog(acquisitionLog, multipleManifest, multiple, partial);
            var logText = File.ReadAllText(acquisitionLog);
            Check(logText.Contains("earth-day FAILED https://mirror.example.test/assets/earth/day.bin")
                && logText.Contains("moon-day VERIFIED") && !logText.Contains("fixture-secret"),
                "timestamped log records safe failure URL and other verified files");
            handler.FailAssetId = null;
            var countBeforeRetry = handler.Count;
            var recovered = multiple.AcquireApprovedDetailedAsync(true, client).GetAwaiter().GetResult();
            Check(recovered.Installed == 1 && recovered.Failures.Count == 0 && recovered.Skipped == 1
                && handler.Count == countBeforeRetry + 1 && multiple.IsVerified(asset),
                "later setup retries failure and skips previously verified asset");
            var bad = ReachVisualConfiguration.Parse("[Visuals]\nMode=garbage\n[Downloads]\nSourceBaseUrl=http://example.com\n[Storage]\nAssetDirectory=../escape");
            Check(bad.Mode == ReachVisualMode.Basic && bad.SourceBaseUrl is null
                && bad.AssetDirectory == ReachVisualConfiguration.DefaultAssetDirectory, "invalid INI defaults");
            Check(ReachVisualConfiguration.Parse("[Visuals]\nMode=cached").Mode == ReachVisualMode.Enhanced
                && ReachVisualConfiguration.Parse("[Visuals]\nMode=download").Mode == ReachVisualMode.Enhanced,
                "legacy modes preserve enhanced cache access");
            Check(ReachVisualConfiguration.Parse("[Countdown]\nEnabled=0\nPosition=bottom-centre").CountdownEnabled == false
                && ReachVisualConfiguration.Parse("[Countdown]\nEnabled=0\nPosition=bottom-centre").CountdownPosition == "bottom-centre",
                "countdown disable and fixed position");
            foreach (var position in new[] { "top-left", "top-centre", "top-right",
                "bottom-left", "bottom-centre", "bottom-right", "auto" })
                Check(ReachVisualConfiguration.Parse($"[Countdown]\nPosition={position}").CountdownPosition == position,
                    $"countdown position {position}");
            Check(ReachVisualConfiguration.Parse("[Countdown]\nEnabled=invalid\nPosition=invalid").CountdownEnabled
                && ReachVisualConfiguration.Parse("[Countdown]\nPosition=invalid").CountdownPosition == "auto",
                "invalid countdown settings use defaults");
            var direct = enhanced with { SourceBaseUrl = null };
            var directRoot = Path.Combine(root, "direct");
            var directCache = new ReachAssetCache(manifest, direct with { AssetDirectory = directRoot });
            var directHandler = new FixtureHandler();
            using var directClient = new HttpClient(directHandler);
            Check(directCache.AcquireApprovedAsync(true, directClient).GetAwaiter().GetResult() == 1
                && directHandler.LastUri?.AbsoluteUri == asset.SourceUrl,
                "per-asset direct HTTPS source used without a mirror");
            var overridden = ReachVisualConfiguration.Parse("[Visuals]\nMode=enhanced\n[Downloads]\nSourceBaseUrl=https://mirror.example.test/assets/\n[AssetSources]\nearth-day=https://alternate.example.test/earth.jpg");
            Check(overridden.SourceFor(asset).AbsoluteUri == "https://alternate.example.test/earth.jpg",
                "per-asset override takes precedence over shared mirror");
            var rejectedOverride = ReachVisualConfiguration.Parse("[AssetSources]\nearth-day=http://alternate.example.test/earth.jpg");
            Check(rejectedOverride.SourceFor(asset).AbsoluteUri == asset.SourceUrl,
                "unsafe per-asset source override rejected");
            Check(!ReachVisualConfiguration.TryParseSourceBaseUrl("https://127.0.0.1/", out _)
                && ReachVisualConfiguration.TryParseSourceBaseUrl("https://mirror.example.test/assets", out _),
                "source URL validation");
            try
            {
                new ReachAssetManifest(1, [asset with { LocalRelativePath = "../escape.bin" }]).Validate();
                throw new Exception("Traversal path was accepted.");
            }
            catch (InvalidDataException) { }
            Diagnostics.Log.Info("Reach visual asset fixture: PASS");
            return 0;
        }
        catch (Exception ex)
        {
            Diagnostics.Log.Error("Reach visual asset fixture: FAIL", ex);
            return 1;
        }
        finally
        {
            var full = Path.GetFullPath(root);
            var temp = Path.GetFullPath(Path.GetTempPath()).TrimEnd(Path.DirectorySeparatorChar) + Path.DirectorySeparatorChar;
            if (full.StartsWith(temp, StringComparison.OrdinalIgnoreCase) && Directory.Exists(full))
                Directory.Delete(full, true);
        }
    }

    private static void Check(bool condition, string name)
    {
        if (!condition) throw new Exception(name);
    }

    private sealed class FixtureHandler : HttpMessageHandler
    {
        public int Count { get; private set; }
        public Uri? LastUri { get; private set; }
        public bool Corrupt { get; set; }
        public string? FailAssetId { get; set; }
        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            Count++;
            LastUri = request.RequestUri;
            if (FailAssetId is not null && request.RequestUri?.AbsolutePath.Contains(FailAssetId) == true)
                throw new HttpRequestException("Publisher unreachable at " + request.RequestUri);
            return Task.FromResult(new HttpResponseMessage(HttpStatusCode.OK)
                { Content = new ByteArrayContent(Corrupt ? Encoding.ASCII.GetBytes(new string('z', Payload.Length)) : Payload) });
        }
    }
}
