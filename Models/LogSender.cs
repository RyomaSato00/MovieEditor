
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
        var now = DateTime.Now;
        _logHistory.Add($"{now.Hour:D2}:{now.Minute:D2}:{now.Second:D2} [{level}] : {message}");
        OnSendLog?.Invoke(string.Join("\r\n", _logHistory));
    }

    public async void SendLogFromAsync(string message, LogLevel level = LogLevel.Info)
    {
        await Task.Yield();
        SendLog(message, level);
    }
}

internal enum LogLevel
{
    Info,
    Warning,
    Error,
    Debug,
}