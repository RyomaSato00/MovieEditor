using System.Diagnostics;
using MovieEditor.Models.Information;

namespace MovieEditor.Models.AudioExtraction;

internal class AudioExtractor
{
    public static void Extract(MovieInfo movieInfo, string outputPath, CancellationToken cancelToken)
    {
        ProcessStartInfo processInfo = new("ffmpeg")
        {
            Arguments = MakeArguments(movieInfo, outputPath),
            UseShellExecute = false,
            CreateNoWindow = true
        };

        using Process process = new() {StartInfo = processInfo};
        process.Start();

        while(false == process.HasExited
            && false == cancelToken.IsCancellationRequested) {}
    }

    private static string MakeArguments(MovieInfo movieInfo, string outputPath)
    {
        List<string> argList = [];
        argList.Add($"-y -i \"{movieInfo.FilePath}\"");
        argList.Add($"-vn -acodec aac \"{outputPath}\"");
        return string.Join(" ", argList);
    }
}