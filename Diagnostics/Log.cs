namespace TheReachScreensaver.Diagnostics;

internal static class Log
{
    private static readonly object Gate = new();
    private static string? _filePath;

    public static void Initialize(string rootDirectory)
    {
        try
        {
            var directory = Path.Combine(rootDirectory, "logs");
            Directory.CreateDirectory(directory);
            _filePath = Path.Combine(directory, $"reach-{DateTime.Now:yyyyMMdd}.log");
            Info("Log started.");
        }
        catch
        {
            _filePath = null;
        }
    }

    public static void Info(string message) => Write("INFO", message);

    public static void Warn(string message) => Write("WARN", message);

    public static void Error(string message, Exception? exception = null)
    {
        Write("ERROR", exception is null ? message : $"{message} {exception}");
    }

    private static void Write(string level, string message)
    {
        var line = $"{DateTime.Now:yyyy-MM-dd HH:mm:ss.fff} [{level}] {message}";
        try
        {
            Console.Error.WriteLine(line);
        }
        catch
        {
            // WinExe may have no console.
        }

        if (_filePath is null)
            return;

        try
        {
            lock (Gate)
            {
                File.AppendAllText(_filePath, line + Environment.NewLine);
            }
        }
        catch
        {
            // Persistence of logs must never take the process down.
        }
    }
}
