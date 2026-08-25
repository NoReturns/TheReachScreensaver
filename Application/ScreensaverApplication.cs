using TheReachScreensaver.Diagnostics;
using TheReachScreensaver.Display;
using TheReachScreensaver.Native;
using TheReachScreensaver.Persistence;
using TheReachScreensaver.Rendering;

namespace TheReachScreensaver.Application;

internal static class ScreensaverApplication
{
    public static int Run(ScreensaverMode mode, IntPtr previewParent)
    {
        var store = new JourneyStateStore();
        Log.Initialize(store.RootDirectory);
        Log.Info($"Launching The Reach Screensaver in {mode} mode.");

        if (OperatingSystem.IsWindows())
        {
            try
            {
                Win32.SetProcessDpiAwarenessContext(Win32.DpiAwarenessContextPerMonitorAwareV2);
            }
            catch (Exception ex)
            {
                Log.Warn($"Could not set PerMonitorV2 DPI awareness: {ex.Message}");
            }
        }

        if (mode == ScreensaverMode.Preview)
        {
            if (previewParent == IntPtr.Zero)
            {
                Log.Warn("Preview mode was requested without a parent HWND. Exiting without opening a window.");
                return 0;
            }

            if (!OperatingSystem.IsWindows())
            {
                Log.Warn("Preview embedding requires Windows. Exiting without opening a window.");
                return 0;
            }
        }

        var state = store.Load();
        var layout = DisplayLayout.Query();

        try
        {
            using var window = SpaceWindow.Create(mode, layout, state, store, previewParent);
            window.Run();
            Log.Info("Window loop ended.");
            return 0;
        }
        catch (Exception ex)
        {
            Log.Error("Failed to create or run the OpenGL window.", ex);
            if (mode == ScreensaverMode.Development)
            {
                ConfigurationDialog.ShowError(
                    "The Reach Screensaver could not create an OpenGL window.\n\n" + ex.Message);
            }

            try
            {
                store.Save(state);
            }
            catch
            {
                // already logged inside the store
            }

            return 1;
        }
    }
}
