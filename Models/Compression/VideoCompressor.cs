using System.Diagnostics;
using System.IO;
using FFMpegCore;
using MovieEditor.Models.Information;
using MyCommonFunctions;

namespace MovieEditor.Models.Compression;

internal class VideoCompressor
{
    public static Process ToCompressionProcess(MovieInfo movieInfo, CompressionParameter parameter)
    {
        ProcessStartInfo processInfo = new("ffmpeg")
        {
            Arguments = MakeArguments(movieInfo, parameter),
            UseShellExecute = false,
            CreateNoWindow = true
        };
        Debug.WriteLine($"arg:{processInfo.Arguments}");
        MyConsole.WriteLine($"arg:{processInfo.Arguments}");

        return new Process() { StartInfo = processInfo };
    }

    private static string MakeArguments(MovieInfo movieInfo, CompressionParameter parameter)
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
        // 時間範囲指定開始時刻
        if (movieInfo.TrimStart is not null)
        {
            argList.Add($"-ss {movieInfo.TrimStart:hh\\:mm\\:ss\\.fff}");
        }
        // 時間範囲指定終了時刻
        if (movieInfo.TrimEnd is not null)
        {
            argList.Add($"-to {movieInfo.TrimEnd:hh\\:mm\\:ss\\.fff}");
        }

        // 出力先指定
        if (movieInfo.OutputPath is null)
        {
            throw new ArgumentNullException("出力先が指定されていません");
        }

        argList.Add($"\"{movieInfo.OutputPath}\"");

        return string.Join(" ", argList);
    }
}

internal record CompressionParameter
{
    public int ScaleWidth { get; init; } = -1;
    public int ScaleHeight { get; init; } = -1;
    public double FrameRate { get; init; } = -1;
    public string VideoCodec { get; init; } = string.Empty;
    public bool IsAudioEraced { get; init; } = false;
    public string Format { get; init; } = string.Empty;
}