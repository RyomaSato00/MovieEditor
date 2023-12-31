using MovieEditor.Models.Information;
using System.Diagnostics;

namespace MovieEditor.Models.SpeedChange;

internal class SpeedChanger
{
    public static void ChangeSpeed(MovieInfo movieInfo, string outputPath, double speed, CancellationToken cancelToken = default)
    {
        var processInfo = new ProcessStartInfo("ffmpeg")
        {
            Arguments = ToArgumentsString(movieInfo, outputPath, speed),
            UseShellExecute = false,
            CreateNoWindow = true
        };

        using var process = new Process() { StartInfo = processInfo };
        process.Start();

        while (false == process.HasExited
        && false == cancelToken.IsCancellationRequested) { }
    }

    private static string ToArgumentsString(MovieInfo movieInfo, string outputPath, double speed)
    {
        // スピードが0以下のときは元ファイルのまま出力する
        if (0 >= speed)
        {
            return $"-y -i \"{movieInfo.FilePath}\" \"{outputPath}\"";
        }

        return $"-y -i \"{movieInfo.FilePath}\" -vf setpts=PTS/{speed} -af atempo={speed} \"{outputPath}\"";
    }
}