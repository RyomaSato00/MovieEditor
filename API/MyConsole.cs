namespace MyCommonFunctions;

public static class MyConsole
{
    public static event Action<string>? OnWrite = null;

    // private static readonly List<string> _logHistory = [];

    private static readonly string[] _logHistory = [string.Empty, string.Empty];

    public static bool UseDebugLog { get; set; } = true;

    public static void WriteLine(string message, Level level = Level.Debug)
    {
        // "Debug"は使用しない？
        if (false == UseDebugLog && Level.Debug == level) return;

        // 初回
        if(string.Empty == _logHistory[0])
        {
            _logHistory[0] = $"{DateTime.Now:HH:mm:ss} [{level}] : {message}";
        }
        else
        {
            _logHistory[1] = $"{DateTime.Now:HH:mm:ss} [{level}] : {message}";
            _logHistory[0] = string.Join("\r\n", _logHistory);
        }
        
        OnWrite?.Invoke(_logHistory[0]);
    }

    public enum Level
    {
        Info,
        Warning,
        Error,
        Debug,
    }
}