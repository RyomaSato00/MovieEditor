using System.Diagnostics;
using MovieEditor.Models.Information;
using MyCommonFunctions;

namespace MovieEditor.Models.AudioExtraction;

internal class AudioExtractor
{
    public static Process ToExtractionProcess(MovieInfo movieInfo)
    {
        ProcessStartInfo processInfo = new("ffmpeg")
        {
            Arguments = MakeArguments(movieInfo),
            UseShellExecute = false,
            CreateNoWindow = true
        };
        Debug.WriteLine($"arg:{processInfo.Arguments}");
        MyConsole.WriteLine($"arg:{processInfo.Arguments}");

        return new Process() { StartInfo = processInfo };
    }

    private static string MakeArguments(MovieInfo movieInfo)
    {
        List<string> argList = [];
        argList.Add($"-y -i \"{movieInfo.FilePath}\"");

        // 出力先指定
        if (movieInfo.OutputPath is null)
        {
            throw new ArgumentNullException("出力先が指定されていません");
        }

        argList.Add($"-vn -acodec aac \"{movieInfo.OutputPath}\"");

        return string.Join(" ", argList);
    }
}