using TheReachScreensaver.Diagnostics;
using TheReachScreensaver.Native;

namespace TheReachScreensaver.Application;

internal static class ConfigurationDialog
{
    public static void Show(IntPtr ownerHwnd)
    {
        const string message = "The Reach Screensaver has no configurable settings yet.";
        ShowMessage(ownerHwnd, message, iconError: false);
    }

    public static void ShowError(string message)
    {
        ShowMessage(IntPtr.Zero, message, iconError: true);
    }

    private static void ShowMessage(IntPtr ownerHwnd, string message, bool iconError)
    {
        Log.Info($"Configuration/message: {message}");
        if (OperatingSystem.IsWindows())
        {
            var icon = iconError ? Win32.MbIconError : Win32.MbIconInformation;
            Win32.MessageBoxW(ownerHwnd, message, "The Reach Screensaver", Win32.MbOk | icon);
            return;
        }

        try
        {
            Console.WriteLine(message);
        }
        catch
        {
            // WinExe without a console.
        }
    }
}
