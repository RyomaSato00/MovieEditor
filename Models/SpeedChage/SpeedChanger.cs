using MovieEditor.Models.Information;
using MyCommonFunctions;
using System.Diagnostics;

namespace MovieEditor.Models.SpeedChange;

internal class SpeedChanger
{
    public static Process ToSpeedChageProcess(MovieInfo movieInfo, double speed)
    {
        var processInfo = new ProcessStartInfo("ffmpeg")
        {
            Arguments = ToArgumentsString(movieInfo, speed),
            UseShellExecute = false,
            CreateNoWindow = true
        };
        Debug.WriteLine($"arg:{processInfo.Arguments}");
        MyConsole.WriteLine($"arg:{processInfo.Arguments}");

        return new Process() { StartInfo = processInfo };
    }

    private static string ToArgumentsString(MovieInfo movieInfo, double speed)
    {
        // 出力先指定
        if (movieInfo.OutputPath is null)
        {
            throw new ArgumentNullException("出力先が指定されていません");
        }

        // スピードが0以下のときは元ファイルのまま出力する
        if (0 >= speed)
        {
            return $"-y -i \"{movieInfo.FilePath}\" \"{movieInfo.OutputPath}\"";
        }
        else
        {
            return $"-y -i \"{movieInfo.FilePath}\" -vf setpts=PTS/{speed} -af atempo={speed} \"{movieInfo.OutputPath}\"";
        }

    }
}