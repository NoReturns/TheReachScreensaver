using TheReachScreensaver.Application;
using TheReachScreensaver.Diagnostics;
using TheReachScreensaver.Native;

namespace TheReachScreensaver.VisualAssets;

internal static class ReachVisualSetup
{
    private static string AcquisitionLogPath => Path.Combine(ReachVisualConfiguration.RootDirectory, "asset-acquisition.log");

    public static int Status()
    {
        try
        {
            var configuration = ReachVisualConfiguration.Load();
            var manifest = ReachAssetManifest.LoadPinned();
            var cache = new ReachAssetCache(manifest, configuration);
            var verified = manifest.Assets.Count(cache.IsVerified);
            var message = $"Mode: {configuration.Mode}\nVerified: {verified}/{manifest.Assets.Length}\n"
                + $"Missing or corrupt: {manifest.Assets.Length - verified}\nRequired download: {cache.MissingByteCount():N0} bytes\n"
                + $"Cache: {configuration.AssetDirectory}\nAcquisition log: {AcquisitionLogPath}";
            Console.WriteLine(message);
            return 0;
        }
        catch (Exception ex)
        {
            Log.Error("Reach imagery status failed.", ex);
            ConfigurationDialog.ShowError(ex.Message);
            return 1;
        }
    }

    public static int Run()
    {
        try
        {
            ReachVisualConfiguration.EnsureDefaultFile();
            var configuration = ReachVisualConfiguration.Load();
            var manifest = ReachAssetManifest.LoadPinned();
            var cache = new ReachAssetCache(manifest, configuration);
            if (configuration.Mode != ReachVisualMode.Enhanced)
            {
                ConfigurationDialog.ShowInfo("Reach imagery is not set to enhanced mode. Edit "
                    + ReachVisualConfiguration.IniPath + " and run --reach-assets-setup again.");
                return 0;
            }
            var missing = cache.MissingByteCount();
            if (missing == 0)
            {
                ConfigurationDialog.ShowInfo(manifest.Assets.Length == 0
                    ? "No downloadable imagery is pinned in this build. Reach can run with bundled maps."
                    : "All pinned imagery assets are already verified locally.");
                return 0;
            }
            var sources = string.Join(", ", manifest.Assets.Where(a => !cache.IsVerified(a))
                .Select(a => configuration.SourceFor(a).Host).Distinct());
            var pending = manifest.Assets.Count(a => !cache.IsVerified(a));
            var question = $"Download {pending} missing or corrupt assets, {missing:N0} bytes ({missing / 1048576.0:0.0} MiB), of pinned Reach imagery "
                + $"from {sources} into {configuration.AssetDirectory}?\n\n"
                + "Declining keeps bundled maps available. Downloads occur only during setup.";
            if (Win32.MessageBoxW(IntPtr.Zero, question, "The Reach asset setup",
                    Win32.MbYesNo | Win32.MbIconInformation) != Win32.IdYes)
            {
                Log.Info("Reach imagery download declined.");
                return 0;
            }
            var result = cache.AcquireApprovedDetailedAsync(true).GetAwaiter().GetResult();
            WriteAcquisitionLog(AcquisitionLogPath, manifest, cache, result);
            Log.Info($"Installed {result.Installed} verified Reach imagery assets; {result.Failures.Count} failed. See {AcquisitionLogPath}.");
            var failures = string.Join("\n", result.Failures.Select(f => $"{f.AssetId}: {f.Reason} ({f.Source})"));
            Win32.MessageBoxW(IntPtr.Zero, $"Installed {result.Installed} assets; {result.Failures.Count} failed. Restart Reach to use verified assets.\nLog: {AcquisitionLogPath}"
                + (failures.Length > 0 ? "\n\n" + failures : ""),
                "The Reach asset setup", Win32.MbOk | Win32.MbIconInformation);
            return result.Failures.Count == 0 ? 0 : 1;
        }
        catch (Exception ex)
        {
            Log.Error("Reach imagery setup failed.", ex);
            ConfigurationDialog.ShowError("Reach imagery setup failed. Procedural rendering remains available.\n\n" + ex.Message);
            return 1;
        }
    }

    internal static void WriteAcquisitionLog(string path, ReachAssetManifest manifest, ReachAssetCache cache,
        ReachAssetCache.AcquisitionResult result)
    {
        var stamp = DateTimeOffset.UtcNow.ToString("yyyy-MM-ddTHH:mm:ssZ");
        var entries = manifest.Assets.Select(asset =>
        {
            var failure = result.Failures.FirstOrDefault(f => f.AssetId == asset.AssetId);
            var state = failure is not null ? $"FAILED {failure.Source} {failure.Reason}"
                : cache.IsVerified(asset) ? "VERIFIED" : "MISSING";
            return $"{stamp} {asset.AssetId} {state}";
        });
        Directory.CreateDirectory(Path.GetDirectoryName(path)!);
        File.AppendAllLines(path, entries);
    }
}
