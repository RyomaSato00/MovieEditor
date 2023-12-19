using System.Diagnostics;
using FFMpegCore;

namespace MovieEditor.Models.Compression;

internal class VideoCompressor
{
    public static void Compress(string inputPath, string outputPath, CompressionParameter parameter)
    {
        ProcessStartInfo info = new("ffmpeg");
        info.Arguments = MakeArguments(inputPath, outputPath, parameter);
        info.UseShellExecute = false;
        info.CreateNoWindow = true;

        using Process proces = new() { StartInfo = info };
        proces.Start();
        proces.WaitForExit();
    }

    private static string MakeArguments(string inputPath, string outputPath, CompressionParameter parameter)
    {
        string[] args =
        [
            $"-y -i {inputPath} -vf scale={parameter.ScaleWidth}:{parameter.ScaleHeight} -r {parameter.FrameRate} -c:v {parameter.VideoCodec}",
            parameter.IsAudioEraced ? "-an" : string.Empty,
            $"{outputPath}"
        ];

        return string.Join(" ", args);
    }
}

internal record CompressionParameter
{
    public int ScaleWidth { get; init; }
    public int ScaleHeight { get; init; }
    public double FrameRate { get; init; }
    public string VideoCodec { get; init; } = string.Empty;
    public bool IsAudioEraced { get; init; }
}