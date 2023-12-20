using System.Diagnostics;
using FFMpegCore;

namespace MovieEditor.Models.Compression;

internal class VideoCompressor
{
    public static void Compress(string inputPath, string outputPath, CompressionParameter parameter, CancellationToken cancelToken)
    {
        ProcessStartInfo info = new("ffmpeg");
        info.Arguments = MakeArguments(inputPath, outputPath, parameter);
        info.UseShellExecute = false;
        info.CreateNoWindow = true;

        using Process process = new() { StartInfo = info };
        process.Start();
        // proces.WaitForExit();
        while(false == process.HasExited 
        && false == cancelToken.IsCancellationRequested) {}
    }

    private static string MakeArguments(string inputPath, string outputPath, CompressionParameter parameter)
    {
        List<string> argList = [];
        argList.Add($"-y -i {inputPath}");

        if(0 < parameter.ScaleWidth && 0 < parameter.ScaleHeight)
        {
            argList.Add($"-vf scale={parameter.ScaleWidth}:{parameter.ScaleHeight}");
        }
        if(0 < parameter.FrameRate)
        {
            argList.Add($"-r {parameter.FrameRate}");
        }
        if(false == string.IsNullOrWhiteSpace(parameter.VideoCodec))
        {
            argList.Add($"-c:v {parameter.VideoCodec}");
        }
        if(parameter.IsAudioEraced)
        {
            argList.Add("-an");
        }

        argList.Add($"{outputPath}");

        return string.Join(" ", argList);
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