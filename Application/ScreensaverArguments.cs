namespace TheReachScreensaver.Application;

internal sealed class ScreensaverArguments
{
    public ScreensaverMode Mode { get; }
    public IntPtr ParentHwnd { get; }
    public string Raw { get; }

    public ScreensaverArguments(ScreensaverMode mode, IntPtr parentHwnd, string raw)
    {
        Mode = mode;
        ParentHwnd = parentHwnd;
        Raw = raw;
    }

    public static ScreensaverArguments Parse(string[] args)
    {
        if (args.Length == 0)
            return new ScreensaverArguments(ScreensaverMode.Development, IntPtr.Zero, string.Empty);

        var raw = string.Join(' ', args);
        var token = args[0].Trim();
        if (token.StartsWith('/') || token.StartsWith('-'))
            token = token[1..];

        if (token.Length == 0)
            return new ScreensaverArguments(ScreensaverMode.Development, IntPtr.Zero, raw);

        var modeChar = char.ToLowerInvariant(token[0]);
        var attached = token.Length > 1 ? token[1..].Trim().TrimStart(':') : string.Empty;

        TryParseHwnd(attached, out var hwnd);
        if (hwnd == IntPtr.Zero && args.Length > 1)
        {
            var next = args[1].Trim().TrimStart('/', '-').TrimStart(':');
            TryParseHwnd(next, out hwnd);
        }

        return modeChar switch
        {
            's' => new ScreensaverArguments(ScreensaverMode.Screensaver, IntPtr.Zero, raw),
            'c' => new ScreensaverArguments(ScreensaverMode.Configuration, hwnd, raw),
            'p' => new ScreensaverArguments(ScreensaverMode.Preview, hwnd, raw),
            _ => new ScreensaverArguments(ScreensaverMode.Development, IntPtr.Zero, raw)
        };
    }

    private static bool TryParseHwnd(string text, out IntPtr hwnd)
    {
        hwnd = IntPtr.Zero;
        if (string.IsNullOrWhiteSpace(text))
            return false;

        text = text.Trim();
        try
        {
            if (text.StartsWith("0x", StringComparison.OrdinalIgnoreCase))
            {
                hwnd = new IntPtr(Convert.ToInt64(text, 16));
                return hwnd != IntPtr.Zero;
            }

            if (long.TryParse(text, out var value))
            {
                hwnd = new IntPtr(value);
                return hwnd != IntPtr.Zero;
            }
        }
        catch (OverflowException)
        {
            return false;
        }

        return false;
    }
}
