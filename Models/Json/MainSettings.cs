namespace MovieEditor.Models.Json;

internal class MainSettings
{
    public string OutputFolder { get; set; } = @"C:\Users";
    public string AttachedNameTag { get; set; } = "cmp";
    public ProcessModeEnum ProcessMode { get; set; } = ProcessModeEnum.VideoCompression;
    public int ScaleWidth { get; set; } = 1024;
    public int ScaleHeight { get; set; } = 576;
    public double FrameRate { get; set; } = 24;
    public bool IsAudioEraced { get; set; } = true;
    public string Codec { get; set; } = "hevc";

}

internal enum ProcessModeEnum
{
    VideoCompression,
    AudioExtraction,
}