
using MovieEditor.Models.AudioExtraction;
using MovieEditor.Models.Compression;
using MovieEditor.Models.Json;

namespace MovieEditor.Models;

internal class ModelManager : IDisposable
{
    private readonly LogSender _logSender;
    private readonly JsonManager _jsonManager;
    private readonly ParallelCompressionRunner _parallelComp;
    private readonly ParallelExtractionRunner _parallelExtract;

    public ILogServer LogServer => _logSender;
    public ISettingReferable SettingReferable => _jsonManager;
    public ParallelCompressionRunner ParallelComp => _parallelComp;
    public ParallelExtractionRunner ParallelExtract => _parallelExtract;

    public ModelManager(Action<string> OnSendLogEventHandler)
    {
        _logSender = new LogSender(OnSendLogEventHandler);
        _jsonManager = new JsonManager(_logSender);
        _parallelComp = new ParallelCompressionRunner(_logSender);
        _parallelExtract = new ParallelExtractionRunner(_logSender);
    }

    public void Test()
    {
        _logSender.SendLog("テスト１");
        _logSender.SendLog("テスト２");
    }

    public void SendLog(string message, LogLevel level = LogLevel.Info) => _logSender.SendLog(message, level);
    public void SendLogFromAsync(string message, LogLevel level) => _logSender.SendLogFromAsync(message, level);

    public void Debug(string message)
    {
        _logSender.SendLog(message, LogLevel.Debug);
    }

    public void Dispose()
    {
        _parallelComp.Dispose();
        _parallelExtract.Dispose();
        _jsonManager.SaveMainSettings();
    }
}