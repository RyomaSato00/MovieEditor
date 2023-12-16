namespace MovieEditor.Models.Json;

internal class MainSettings
{
    public string OutputFolder { get; set; } = @"C:\Users";
    public string AttachedNameTag { get; set; } = "cmp";
    public int ScaleWidth { get; set; } = 1280;
    public int ScaleHeight { get; set; } = 720;
    public int FrameRate { get; set; } = 24;
    public bool IsAudioEraced { get; set; } = true;
    public string Codec { get; set; } = "libx265";

}