
using MovieEditor.Models.Compression;
using MovieEditor.Models.Json;

namespace MovieEditor.Models;

internal class ModelManager : IDisposable
{
    private readonly LogSender _logSender;
    private readonly JsonManager _jsonManager;

    public ILogServer LogServer => _logSender;
    public ISettingReferable SettingReferable => _jsonManager;

    // 仮
    public ParallelCompressionRunner ParallelComp { get; }

    public ModelManager(Action<string> OnSendLogEventHandler)
    {
        _logSender = new LogSender(OnSendLogEventHandler);
        _jsonManager = new JsonManager(_logSender);

        ParallelComp = new ParallelCompressionRunner(_logSender);
    }

    public void Test()
    {
        _logSender.SendLog("テスト１");
        _logSender.SendLog("テスト２");
    }

    public void SendLog(string message, LogLevel level) => _logSender.SendLog(message, level);
    public void SendLogFromAsync(string message, LogLevel level) => _logSender.SendLogFromAsync(message, level);

    public void Debug(string message)
    {
        _logSender.SendLog(message, LogLevel.Debug);
    }

    public void Dispose()
    {
        ParallelComp.Dispose();
        _jsonManager.SaveMainSettings();
    }
}