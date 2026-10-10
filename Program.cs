using TheReachScreensaver.Application;
using TheReachScreensaver.Diagnostics;
using TheReachScreensaver.Journey;
using TheReachScreensaver.Persistence;
using TheReachScreensaver.VisualAssets;

namespace TheReachScreensaver;

internal static class Program
{
    [STAThread]
    private static int Main(string[] args)
    {
        if (args.Length == 1 && string.Equals(args[0], "--reach-assets-status", StringComparison.OrdinalIgnoreCase))
        {
            Log.Initialize(ReachVisualConfiguration.RootDirectory);
            return ReachVisualSetup.Status();
        }
        if (args.Length == 1 && string.Equals(args[0], "--reach-assets-setup", StringComparison.OrdinalIgnoreCase))
        {
            Log.Initialize(ReachVisualConfiguration.RootDirectory);
            return ReachVisualSetup.Run();
        }
        if (args.Length == 1 && string.Equals(args[0], "--reach-assets-self-test", StringComparison.OrdinalIgnoreCase))
        {
            Log.Initialize(ReachVisualConfiguration.RootDirectory);
            return ReachAssetSelfTest.Run();
        }
        if (args.Length == 1 && string.Equals(args[0], "--reach-web-display-self-test", StringComparison.OrdinalIgnoreCase))
            return ReachWebApplication.RunDisplayFixtures();
        if (args.Any(a => string.Equals(a, "--path-audit", StringComparison.OrdinalIgnoreCase)))
        {
            Console.WriteLine(PathAuditor.Run());
            return 0;
        }

        if (args.Length > 0 && string.Equals(args[0], "--reach-web", StringComparison.OrdinalIgnoreCase))
        {
            Log.Initialize(new JourneyStateStore().RootDirectory);
            var fullscreen = args.Length >= 2 && string.Equals(args[1], "/s", StringComparison.OrdinalIgnoreCase);
            var allAlias = fullscreen && args.Length == 3 && string.Equals(args[2], "/all", StringComparison.OrdinalIgnoreCase);
            if (args.Length != 1 && !(fullscreen && args.Length == 2) && !allAlias)
            {
                const string message = "Use --reach-web, --reach-web /s, or --reach-web /s /all.";
                Log.Error(message);
                ConfigurationDialog.ShowError(message);
                return 1;
            }
            return ReachWebApplication.Run(fullscreen);
        }

        if (args.Length > 0 && string.Equals(args[0], "--legacy-opentk", StringComparison.OrdinalIgnoreCase))
        {
            Log.Initialize(new JourneyStateStore().RootDirectory);
            var fullscreen = args.Length == 2 && string.Equals(args[1], "/s", StringComparison.OrdinalIgnoreCase);
            if (args.Length != 1 && !fullscreen)
            {
                const string message = "Use --legacy-opentk or --legacy-opentk /s.";
                Log.Error(message);
                ConfigurationDialog.ShowError(message);
                return 1;
            }
            return ScreensaverApplication.Run(fullscreen ? ScreensaverMode.Screensaver : ScreensaverMode.Development, IntPtr.Zero);
        }

        var parsed = ScreensaverArguments.Parse(args);
        var store = new JourneyStateStore();
        Log.Initialize(store.RootDirectory);
        Log.Info($"Arguments: '{parsed.Raw}' mode={parsed.Mode} hwnd={parsed.ParentHwnd}");

        try
        {
            return parsed.Mode switch
            {
                ScreensaverMode.Configuration => RunConfiguration(parsed.ParentHwnd),
                ScreensaverMode.Preview => ReachWebApplication.RunPreview(parsed.ParentHwnd),
                ScreensaverMode.Screensaver => ReachWebApplication.Run(fullscreen: true),
                _ => ReachWebApplication.Run(fullscreen: false)
            };
        }
        catch (Exception ex)
        {
            Log.Error("Unhandled exception.", ex);
            if (parsed.Mode == ScreensaverMode.Development)
                ConfigurationDialog.ShowError("The Reach Screensaver failed.\n\n" + ex.Message);
            return 1;
        }
    }

    private static int RunConfiguration(IntPtr ownerHwnd)
    {
        var visuals = ReachVisualConfiguration.Load();
        if (visuals.Mode == ReachVisualMode.Enhanced && visuals.DownloadOnFirstUse)
            return ReachVisualSetup.Run();
        ConfigurationDialog.Show(ownerHwnd);
        return 0;
    }
}
