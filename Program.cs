using TheReachScreensaver.Application;
using TheReachScreensaver.Diagnostics;
using TheReachScreensaver.Journey;
using TheReachScreensaver.Persistence;

namespace TheReachScreensaver;

internal static class Program
{
    [STAThread]
    private static int Main(string[] args)
    {
        if (args.Any(a => string.Equals(a, "--path-audit", StringComparison.OrdinalIgnoreCase)))
        {
            Console.WriteLine(PathAuditor.Run());
            return 0;
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
                ScreensaverMode.Preview => ScreensaverApplication.Run(ScreensaverMode.Preview, parsed.ParentHwnd),
                ScreensaverMode.Screensaver => ScreensaverApplication.Run(ScreensaverMode.Screensaver, IntPtr.Zero),
                _ => ScreensaverApplication.Run(ScreensaverMode.Development, IntPtr.Zero)
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
        ConfigurationDialog.Show(ownerHwnd);
        return 0;
    }
}
