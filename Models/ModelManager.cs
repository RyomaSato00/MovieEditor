
namespace MovieEditor.Models;

internal class ModelManager
{
    private readonly LogSender _logSender;
    public ILogServer LogServer => _logSender;

    public ModelManager()
    {
        _logSender = new LogSender();
    }

    public void Test()
    {
        _logSender.SendLog("テスト１");
        _logSender.SendLog("テスト２");
    }

    public void Debug(string message)
    {
        _logSender.SendLog(message);
    }
}