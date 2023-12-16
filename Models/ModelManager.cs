
using MovieEditor.Models.Json;

namespace MovieEditor.Models;

internal class ModelManager : IDisposable
{
    private readonly LogSender _logSender;
    private readonly JsonManager _jsonManager;

    public ILogServer LogServer => _logSender;
    public ISettingReferable SettingReferable => _jsonManager;

    public ModelManager(Action<string> OnSendLogEventHandler)
    {
        _logSender = new LogSender(OnSendLogEventHandler);
        _jsonManager = new JsonManager(_logSender);
    }

    public void Test()
    {
        _logSender.SendLog("テスト１");
        _logSender.SendLog("テスト２");
    }

    public void SendLog(string message, LogLevel level) => _logSender.SendLog(message, level);

    public void Debug(string message)
    {
        _logSender.SendLog(message, LogLevel.Debug);
    }

    public void Dispose()
    {
        _jsonManager.SaveMainSettings();
    }
}