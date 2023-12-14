
namespace MovieEditor.Models;

internal interface ILogSendable
{
    public void SendLog(string message, LogLevel level = LogLevel.Info);
}

internal interface ILogServer
{
    public event Action<string> OnSendLog;
}