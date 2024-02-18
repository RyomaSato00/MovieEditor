
using Newtonsoft.Json;
using System.IO;

namespace MovieEditor;

internal static class SettingManager
{
    public static readonly string SettingFilePath = "MainSettings.json";
    public static MainSettings LoadSetting()
    {
        try
        {
            var jsonContent = File.ReadAllText(SettingFilePath);
            var setting = JsonConvert.DeserializeObject<MainSettings>(jsonContent);
            if(setting is null) return new MainSettings();
            else return setting;
        }
        catch(Exception)
        {
            return new MainSettings();
        }
    }

    public static void SaveSetting(MainSettings setting)
    {
        var jsonContent = JsonConvert.SerializeObject(setting, Formatting.Indented);
        File.WriteAllText(SettingFilePath, jsonContent);
    }
}

internal class MainSettings
{
    public string OutputFolder { get; set; } = @"C:\Users";
    public string AttachedNameTag { get; set; } = "cmp";
    public int ProcessMode { get; set; } = (int)ProcessModeEnum.VideoCompression;
    public bool IsThumbnailVisible { get; set; } = false;
    public CompressionCondition Comp { get; set; } = new();
    public SpeedCondition Speed { get; set; } = new();
    public bool OpenExplorer { get; set; } = true;
    public bool UseDebugLog { get; set; } = false;
}

internal class CompressionCondition
{
    public int ScaleWidth { get; set; } = 1024;
    public int ScaleHeight { get; set; } = 576;
    public double FrameRate { get; set; } = 24;
    public bool IsAudioEraced { get; set; } = true;
    public string Codec { get; set; } = "hevc";
    public string Format { get; set; } = "mp4";
}

internal class SpeedCondition
{
    public double SpeedRate { get; set; } = 1;
}

internal enum ProcessModeEnum
{
    VideoCompression,
    AudioExtraction,
    SpeedChange,
}