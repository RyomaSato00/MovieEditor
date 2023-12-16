using System.IO;
using System.Net.Http.Json;
using Newtonsoft.Json;

namespace MovieEditor.Models.Json;

internal class JsonManager : ISettingReferable
{
    private readonly ILogSendable _logger;
    public static readonly string MainSettingFilePath = "MainSettings.json";
    public MainSettings MainSettings_ { get; }

    public JsonManager(ILogSendable logger)
    {
        _logger = logger;
        try
        {
            MainSettings_ = LoadMainSettings();
            _logger.SendLog("jsonファイル読み取り完了");
        }
        catch(FileNotFoundException)
        {
            _logger.SendLog("jsonファイルが見つかりません。新規生成します。");
            MainSettings_ = new MainSettings();
        }
        catch(FormatException e)
        {
            _logger.SendLog(e.Message, LogLevel.Warning);
            _logger.SendLog("jsonファイルを新規生成します。");
            MainSettings_ = new MainSettings();
        }
        catch(Exception e)
        {
            _logger.SendLog("想定外のエラー", LogLevel.Error);
            _logger.SendLog(e.Message, LogLevel.Error);
            throw;
        }
    }

    private static MainSettings LoadMainSettings()
    {
        string jsonContent = File.ReadAllText(MainSettingFilePath);
        return JsonConvert.DeserializeObject<MainSettings>(jsonContent)
            ?? throw new FormatException("jsonファイルのフォーマットが異なる可能性があります");
    }

    public void SaveMainSettings()
    {
        string jsonContent = JsonConvert.SerializeObject(MainSettings_, Formatting.Indented);
        File.WriteAllText(MainSettingFilePath, jsonContent);
    }
}