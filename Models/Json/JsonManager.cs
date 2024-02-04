using System.IO;
using System.Net.Http.Json;
using MyCommonFunctions;
using Newtonsoft.Json;

namespace MovieEditor.Models.Json;

internal class JsonManager : ISettingReferable
{
    public static readonly string MainSettingFilePath = "MainSettings.json";
    public MainSettings MainSettings_ { get; set; }

    public JsonManager()
    {
        try
        {
            MainSettings_ = LoadMainSettings();
            MyConsole.WriteLine("jsonファイル読み取り完了");
        }
        catch(FileNotFoundException)
        {
            MyConsole.WriteLine("jsonファイルが見つかりません。新規生成します。", MyConsole.Level.Info);
            MainSettings_ = new MainSettings();
        }
        catch(FormatException e)
        {
            MyConsole.WriteLine(e.Message, MyConsole.Level.Warning);
            MyConsole.WriteLine("jsonファイルを新規生成します。", MyConsole.Level.Info);
            MainSettings_ = new MainSettings();
        }
        catch(Exception e)
        {
            MyConsole.WriteLine("想定外のエラー", MyConsole.Level.Error);
            MyConsole.WriteLine(e.Message, MyConsole.Level.Error);
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