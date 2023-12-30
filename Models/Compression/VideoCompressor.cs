using System.Diagnostics;
using FFMpegCore;
using MovieEditor.Models.Information;

namespace MovieEditor.Models.Compression;

internal class VideoCompressor
{
    public static void Compress(MovieInfo movieInfo, string outputPath, CompressionParameter parameter, CancellationToken cancelToken)
    {
        ProcessStartInfo processInfo = new("ffmpeg")
        {
            Arguments = MakeArguments(movieInfo, outputPath, parameter),
            UseShellExecute = false,
            CreateNoWindow = true
        };

        using Process process = new() { StartInfo = processInfo };
        process.Start();
        // proces.WaitForExit();
        while (false == process.HasExited
        && false == cancelToken.IsCancellationRequested) { }
    }

    private static string MakeArguments(MovieInfo movieInfo, string outputPath, CompressionParameter parameter)
    {
        List<string> argList = [];
        argList.Add($"-y -i \"{movieInfo.FilePath}\"");

        if (0 < parameter.ScaleWidth && 0 < parameter.ScaleHeight)
        {
            argList.Add($"-vf scale={parameter.ScaleWidth}:{parameter.ScaleHeight}");
        }
        else if (0 >= parameter.ScaleWidth && 0 < parameter.ScaleHeight)
        {
            // 動画ファイルの解像度情報からWidthを自動計算
            int autoWidth = 0;
            // 0除算回避
            if (0 != movieInfo.Height)
            {
                autoWidth = parameter.ScaleHeight * movieInfo.Width / movieInfo.Height;
            }
            // 解像度は偶数にする必要がある。
            if (0 != autoWidth % 2)
            {
                autoWidth--;
            }
            argList.Add($"-vf scale={autoWidth}:{parameter.ScaleHeight}");
        }
        else if (0 < parameter.ScaleWidth && 0 >= parameter.ScaleHeight)
        {
            // 動画ファイルの解像度情報からHeightを自動計算
            int autoHeight = 0;
            // 0除算回避
            if (0 != movieInfo.Width)
            {
                autoHeight = parameter.ScaleWidth * movieInfo.Height / movieInfo.Width;
            }
            // 解像度は偶数にする必要がある。
            if (0 != autoHeight % 2)
            {
                autoHeight--;
            }
            argList.Add($"-vf scale={parameter.ScaleWidth}:{autoHeight}");
        }
        if (0 < parameter.FrameRate)
        {
            argList.Add($"-r {parameter.FrameRate}");
        }
        if (false == string.IsNullOrWhiteSpace(parameter.VideoCodec))
        {
            argList.Add($"-c:v {parameter.VideoCodec}");
        }
        if (parameter.IsAudioEraced)
        {
            argList.Add("-an");
        }

        argList.Add($"\"{outputPath}\"");

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
    public string Format { get; init; } = string.Empty;
}