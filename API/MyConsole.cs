namespace MyCommonFunctions;

public static class MyConsole
{
    public static event Action<string>? OnWrite = null;

    private static readonly List<string> _logHistory = [];

    public static bool UseDebugLog { get; set; } = true;

    public static void WriteLine(string message, Level level = Level.Debug)
    {
        // "Debug"は使用しない？
        if(false == UseDebugLog && Level.Debug == level) return;

        _logHistory.Add($"{DateTime.Now:HH:mm:ss} [{level}] : {message}");
        OnWrite?.Invoke(string.Join("\r\n", _logHistory));
    }

    public enum Level
    {
        Info,
        Warning,
        Error,
        Debug,
    }
}