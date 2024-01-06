namespace MovieEditor.Models.Json;

internal class MainSettings
{
    public string OutputFolder { get; set; } = @"C:\Users";
    public string AttachedNameTag { get; set; } = "cmp";
    public int ProcessMode { get; set; } = (int)ProcessModeEnum.VideoCompression;
    public bool IsThumbnailVisible { get; set; } = false;
    public CompressionCondition Comp { get; set; } = new();
    public SpeedCondition Speed { get; set; } = new();
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