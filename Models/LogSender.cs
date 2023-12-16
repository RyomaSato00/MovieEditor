
namespace MovieEditor.Models;

internal class LogSender : ILogSendable, ILogServer
{
    private readonly List<string> _logHistory = new();

    public event Action<string>? OnSendLog = null;

    public LogSender(Action<string> OnSendLogEventHandler)
    {
        OnSendLog += OnSendLogEventHandler;
    }

    public void SendLog(string message, LogLevel level = LogLevel.Info)
    {
        _logHistory.Add($"[{level}] : {message}");
        OnSendLog?.Invoke(string.Join("\r\n", _logHistory));
    }
}

internal enum LogLevel
{
    Info,
    Warning,
    Error,
    Debug,
}