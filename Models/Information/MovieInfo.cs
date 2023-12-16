using System.IO;
using FFMpegCore;

namespace MovieEditor.Models.Information;

internal record MovieInfo
{
    public static readonly string[] MovieFileExtension =
    [
        ".mp4",
    ];

    public string FilePath { get; init; } = string.Empty;
    public string FileName { get; init; } = string.Empty;
    public int FileSize { get; init; } = 0;
    public TimeSpan Duration { get; init; } = TimeSpan.Zero;
    public int Width { get; init; } = 0;
    public int Height { get; init; } = 0;
    public (int Width, int Height) AspectRatio { get; init; } = (0, 0);
    public double FrameRate { get; init; } = 0;
    public string VideoCodec { get; init; } = string.Empty;
    public int VideoBitRate { get; init; } = 0;
    public string FileSizeString => $"{FileSize} kb";
    public string FormattedDuration => Duration.ToString(@"hh\:mm\:ss\.ff");
    public string ScaleString => $"{Width} : {Height} ({AspectRatio.Width}:{AspectRatio.Height})";
    public string FrameRateString => $"{FrameRate} fps";
    public string VideoBitRateString => $"{VideoBitRate} kbps";

    public static MovieInfo GetMovieInfo(string filePath)
    {
        if(!File.Exists(filePath))
        {
            throw new FileNotFoundException("ファイルが見つかりません。", filePath);
        }

        if (!MovieFileExtension.Contains(Path.GetExtension(filePath)))
        {
            throw new ArgumentOutOfRangeException(Path.GetFileName(filePath),"この拡張子は対応していません。");
        }

        var mediaInfo = FFProbe.Analyse(filePath);
        var video = mediaInfo.PrimaryVideoStream
            ?? throw new ArgumentOutOfRangeException(Path.GetFileName(filePath), "このファイルは動画ファイルではない可能性があります。");

        return new MovieInfo()
        {
            FilePath = filePath,
            FileName = Path.GetFileName(filePath),
            FileSize = (int)(new FileInfo(filePath).Length / 1000),
            Duration = video.Duration,
            Width = video.Width,
            Height = video.Height,
            AspectRatio = video.DisplayAspectRatio,
            FrameRate = video.FrameRate,
            VideoCodec = video.CodecName,
            VideoBitRate = (int)(video.BitRate / 1000)
        };
    }
}
