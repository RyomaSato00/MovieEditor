using MovieEditor.Models.Information;
using MyCommonFunctions;
using System.Diagnostics;
using System.IO;

namespace MovieEditor.Models.ImageGenerate;

internal class ImageGenerator
{
    public static void Generate(MovieInfo movieInfo, ImageGenerateParameter parameter)
    {
        // 出力先
        if (movieInfo.OutputPath is null)
        {
            throw new ArgumentNullException("出力先が指定されていません");
        }
        // 画像を出力するフォルダ作成
        Directory.CreateDirectory(movieInfo.OutputPath);

        var info = new ProcessStartInfo("ffmpeg")
        {
            Arguments = MakeArguments(movieInfo, parameter),
            UseShellExecute = false,
            CreateNoWindow = true
        };
        Debug.WriteLine($"arg:{info.Arguments}");
        MyConsole.WriteLine($"arg:{info.Arguments}");

        using var process = new Process() { StartInfo = info };
        process.Start();
        process.WaitForExit();
    }

    private static string MakeArguments(MovieInfo movieInfo, ImageGenerateParameter parameter)
    {
        List<string> argList = [];
        argList.Add($"-y -i \"{movieInfo.FilePath}\"");

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

        // 1秒間のフレーム数
        if (parameter.FramePerOneSecond >= 1)
        {
            argList.Add($"-r {parameter.FramePerOneSecond}");
        }

        // 総フレーム数（＝画像ファイル数）
        if (parameter.FrameSum >= 1)
        {
            argList.Add($"-vframes {parameter.FrameSum}");
        }

        // 画像品質（数が大きいほど品質が低下し、容量が少なくなる）
        if (parameter.Quality >= 0)
        {
            argList.Add($"-q:v {parameter.Quality}");
        }

        // 出力先指定
        if (movieInfo.OutputPath is null)
        {
            throw new ArgumentNullException("出力先が指定されていません");
        }
        var imageOutput = Path.Combine(movieInfo.OutputPath, $"img%06d.{parameter.Format}");
        argList.Add($"\"{imageOutput}\"");

        return string.Join(" ", argList);
    }
}

internal record ImageGenerateParameter
{
    public string Format { get; init; } = "png";
    public int FramePerOneSecond { get; init; } = -1;
    public int FrameSum { get; init; } = -1;
    public int Quality { get; init; } = -1;
}