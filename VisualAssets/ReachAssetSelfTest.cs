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
            var batchBodies = new[] { "moon", "mars", "io", "tethys", "dione", "iapetus", "titan",
                "enceladus", "europa", "rhea", "venus", "vesta", "pluto" };
            Check(pinned.Assets.Length == 26 && pinned.Assets[0].AssetId == "earth-day-high"
                && pinned.Assets[1].AssetId == "moon-day-high"
                && pinned.Assets.All(asset => asset.DestinationBodyId is not ("ceres" or "mercury"))
                && batchBodies.All(body => pinned.Assets.Any(asset => asset.DestinationBodyId == body))
                && batchBodies.Where(body => body != "moon").All(body =>
                    pinned.Assets.Count(asset => asset.DestinationBodyId == body && asset.Width == 2048) == 1
                    && pinned.Assets.Count(asset => asset.DestinationBodyId == body && asset.Width == 4096) == 1)
                && pinned.Assets.Where(asset => asset.SourceUrl.Contains("releases/download/reach-imagery-v1/"))
                    .All(asset => !asset.SourceUrl.Contains("/review-"))
                && pinned.Assets.Where(asset => asset.DestinationBodyId == "venus")
                    .All(asset => asset.SourceAttribution.Contains("radar surface, fully simulated colour")),
                "public offer pins 26 files and excludes Ceres and Mercury");
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
            handler.DelayUntilCancelled = true;
            using (var cancellation = new CancellationTokenSource())
            {
                var pending = enhancedCache.AcquireApprovedDetailedAsync(true, client, cancellation.Token);
                Check(!pending.IsCompleted, "slow download remains asynchronous");
                cancellation.Cancel();
                try { pending.GetAwaiter().GetResult(); throw new Exception("Cancellation was ignored."); }
                catch (OperationCanceledException) { }
                Check(!File.Exists(enhancedCache.LocalPath(asset)), "cancelled download left no installed asset");
            }
            handler.DelayUntilCancelled = false;
            handler.Corrupt = true;
            var corruptResult = enhancedCache.AcquireApprovedDetailedAsync(true, client).GetAwaiter().GetResult();
            Check(corruptResult.Installed == 0 && corruptResult.Failures.Count == 1
                && corruptResult.Failures[0].Reason.Contains("SHA-256 mismatch"), "corrupt download reported for retry");
            Check(!File.Exists(enhancedCache.LocalPath(asset))
                && !Directory.EnumerateFiles(root, "*.tmp", SearchOption.AllDirectories).Any(),
                "corrupt download left no installed or temporary file");
            handler.Corrupt = false;
            var countAfterCorrupt = handler.Count;
            var progressUpdates = new List<ReachAssetCache.AcquisitionProgress>();
            var progress = new FixtureProgress(progressUpdates);
            Check(enhancedCache.AcquireApprovedDetailedAsync(true, client, progress: progress).GetAwaiter().GetResult().Installed == 1
                && handler.Count == countAfterCorrupt + 1,
                "approved download installed one file");
            Check(progressUpdates.Count > 0 && progressUpdates[^1].BytesProcessed == Payload.Length
                && progressUpdates[^1].TotalBytes == Payload.Length, "download reports byte progress");
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
            var settingsPath = Path.Combine(root, "settings.json");
            ReachVisualConfiguration.SavePreferences(procedural with { Mode = ReachVisualMode.Enhanced,
                CountdownEnabled = false, CountdownPosition = "bottom-right" }, settingsPath);
            var persisted = ReachVisualConfiguration.Load(Path.Combine(root, "missing.ini"), settingsPath);
            Check(persisted.Mode == ReachVisualMode.Enhanced && !persisted.CountdownEnabled
                && persisted.CountdownPosition == "bottom-right", "enhanced mode and existing preferences persist");
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
            var releaseAsset = asset with { AssetId = "moon-day-high", DestinationBodyId = "moon",
                SourceUrl = pinned.Assets[1].SourceUrl, DownloadPath = "moon/day.bin",
                LocalRelativePath = "moon/day.bin" };
            var releaseManifest = new ReachAssetManifest(1, [releaseAsset]);
            var releaseConfiguration = enhanced with { SourceBaseUrl = null, SourceOverrides = null };
            var releaseTarget = new Uri("https://release-assets.githubusercontent.com/github-production-release-asset/fixture?token=test");
            var releaseHandler = new RedirectHandler(releaseTarget);
            using (var releaseClient = new HttpClient(releaseHandler))
            {
                var releaseCache = new ReachAssetCache(releaseManifest,
                    releaseConfiguration with { AssetDirectory = Path.Combine(root, "release") });
                var releaseResult = releaseCache.AcquireApprovedDetailedAsync(true, releaseClient).GetAwaiter().GetResult();
                Check(releaseResult.Installed == 1
                    && releaseHandler.Count == 2 && releaseHandler.LastUri == releaseTarget
                    && releaseCache.IsVerified(releaseAsset),
                    $"pinned GitHub release follows one approved HTTPS redirect: installed={releaseResult.Installed}, requests={releaseHandler.Count}, failure={releaseResult.Failures.FirstOrDefault()?.Reason}");
            }
            foreach (var unsafeTarget in new[] {
                "http://release-assets.githubusercontent.com/github-production-release-asset/fixture",
                "https://other.example.test/github-production-release-asset/fixture",
                "https://release-assets.githubusercontent.com/other/fixture" })
            {
                var rejectedHandler = new RedirectHandler(new Uri(unsafeTarget));
                using var rejectedClient = new HttpClient(rejectedHandler);
                var rejectedCache = new ReachAssetCache(releaseManifest,
                    releaseConfiguration with { AssetDirectory = Path.Combine(root, "rejected-" + rejectedHandler.GetHashCode()) });
                var rejected = rejectedCache.AcquireApprovedDetailedAsync(true, rejectedClient).GetAwaiter().GetResult();
                Check(rejected.Installed == 0 && rejected.Failures.Count == 1 && rejectedHandler.Count == 1
                    && !rejectedCache.IsVerified(releaseAsset), "unsafe GitHub redirect was not followed");
            }
            var overrideHandler = new RedirectHandler(releaseTarget);
            using (var overrideClient = new HttpClient(overrideHandler))
            {
                var overrideCache = new ReachAssetCache(releaseManifest,
                    releaseConfiguration with { AssetDirectory = Path.Combine(root, "release-override"),
                        SourceOverrides = new Dictionary<string, Uri> {
                            [releaseAsset.AssetId] = new("https://mirror.example.test/moon.jpg") } });
                var overrideResult = overrideCache.AcquireApprovedDetailedAsync(true, overrideClient).GetAwaiter().GetResult();
                Check(overrideResult.Installed == 0 && overrideResult.Failures.Count == 1
                    && overrideHandler.Count == 1 && !overrideCache.IsVerified(releaseAsset),
                    "mirror redirect does not inherit pinned GitHub permission");
            }
            var secondHandler = new RedirectHandler(releaseTarget) { RedirectAgain = true };
            using (var secondClient = new HttpClient(secondHandler))
            {
                var secondCache = new ReachAssetCache(releaseManifest,
                    releaseConfiguration with { AssetDirectory = Path.Combine(root, "second-redirect") });
                var secondResult = secondCache.AcquireApprovedDetailedAsync(true, secondClient).GetAwaiter().GetResult();
                Check(secondResult.Installed == 0 && secondResult.Failures.Count == 1
                    && secondHandler.Count == 2 && !secondCache.IsVerified(releaseAsset),
                    "second release redirect was not followed");
            }
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
        public bool DelayUntilCancelled { get; set; }
        public string? FailAssetId { get; set; }
        protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            Count++;
            LastUri = request.RequestUri;
            if (DelayUntilCancelled) await Task.Delay(Timeout.Infinite, cancellationToken);
            if (FailAssetId is not null && request.RequestUri?.AbsolutePath.Contains(FailAssetId) == true)
                throw new HttpRequestException("Publisher unreachable at " + request.RequestUri);
            return new HttpResponseMessage(HttpStatusCode.OK)
                { Content = new ByteArrayContent(Corrupt ? Encoding.ASCII.GetBytes(new string('z', Payload.Length)) : Payload) };
        }
    }

    private sealed class RedirectHandler(Uri target) : HttpMessageHandler
    {
        public int Count { get; private set; }
        public Uri? LastUri { get; private set; }
        public bool RedirectAgain { get; set; }

        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            Count++;
            LastUri = request.RequestUri;
            if (Count == 1 || RedirectAgain)
            {
                var redirect = new HttpResponseMessage(HttpStatusCode.Found);
                redirect.Headers.Location = target;
                return Task.FromResult(redirect);
            }
            return Task.FromResult(new HttpResponseMessage(HttpStatusCode.OK)
                { Content = new ByteArrayContent(Payload) });
        }
    }

    private sealed class FixtureProgress(List<ReachAssetCache.AcquisitionProgress> updates)
        : IProgress<ReachAssetCache.AcquisitionProgress>
    {
        public void Report(ReachAssetCache.AcquisitionProgress value) => updates.Add(value);
    }
}
