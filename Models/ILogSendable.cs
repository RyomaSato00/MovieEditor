
namespace MovieEditor.Models;

internal interface ILogSendable
{
    public void SendLog(string message, LogLevel level);
}

internal interface ILogServer
{
    public event Action<string> OnSendLog;
}