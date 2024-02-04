using System.Diagnostics;
using System.IO;
using System.Windows.Media.Imaging;
using FFMpegCore;
using MyCommonFunctions;

namespace MovieEditor.Models.Information;

internal record MovieInfo
{
    public static readonly string[] MovieFileExtension =
    [
        ".mp4",
        ".MP4",
        ".mov",
        ".MOV",
        ".agm",
        ".avi",
        ".wmv"
    ];

    public static readonly string ThumbnailCacheFolder = @"cache\thumbnails";

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
    public TimeSpan? TrimStart { get; set; } = null;
    public TimeSpan? TrimEnd { get; set; } = null;
    public string FileSizeString => $"{FileSize} kb";
    public string FormattedDuration => Duration.ToString(@"hh\:mm\:ss\.ff");
    public string ScaleString => $"{Width} : {Height} ({AspectRatio.Width}:{AspectRatio.Height})";
    public string FrameRateString => $"{FrameRate:0.##} fps";
    public string VideoBitRateString => $"{VideoBitRate} kbps";

    public static MovieInfo GetMovieInfo(string filePath)
    {
        if (false == File.Exists(filePath))
        {
            throw new FileNotFoundException("ファイルが見つかりません。", filePath);
        }

        if (false == MovieFileExtension.Contains(Path.GetExtension(filePath)))
        {
            throw new ArgumentOutOfRangeException(Path.GetFileName(filePath), "この拡張子は対応していません。");
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

    /// <summary>
    /// 動画からサムネイル用の画像(jpg)を生成し、そのuriを返す
    /// </summary>
    /// <param name="filePath"></param>
    /// <param name="cancelToken"></param>
    /// <returns></returns>
    public static Uri GetThumbnailUri(string filePath, CancellationToken cancelToken = default)
    {
        if (false == Directory.Exists(ThumbnailCacheFolder))
        {
            Directory.CreateDirectory(ThumbnailCacheFolder);
        }

        string thumbnailImagePath = Path.Combine(ThumbnailCacheFolder, $"{Path.GetFileNameWithoutExtension(filePath)}.jpg");
        // ファイルパスの重複を回避する
        MyApi.ToNonDuplicatePath(ref thumbnailImagePath);
        var startInfo = new ProcessStartInfo("ffmpeg")
        {
            Arguments = $"-y -i \"{filePath}\" -ss 0 -vframes 1 -q:v 0 \"{thumbnailImagePath}\"",
            UseShellExecute = false,
            CreateNoWindow = true
        };

        // キャッシュフォルダにサムネイル用のjpgファイルを生成する
        using var process = new Process() { StartInfo = startInfo };
        process.Start();

        while (false == process.HasExited
        && false == cancelToken.IsCancellationRequested) { }

        // キャンセルが飛ばなければjpgファイルが生成されているはず。ビットマップを渡せる。
        if(false == cancelToken.IsCancellationRequested)
        {
            return new Uri(Path.GetFullPath(thumbnailImagePath));
        }
        else
        {
            throw new FileNotFoundException("サムネイル画像が見つかりません");
        }
    }

    /// <summary>
    /// サムネイル用に生成した画像ファイルをすべて削除する
    /// </summary>
    public static void DeleteThumbnailCaches()
    {
        // キャッシュフォルダがないなら、何もしない
        if(false == Directory.Exists(ThumbnailCacheFolder)) return;

        string[] files = Directory.GetFiles(ThumbnailCacheFolder);
        // キャッシュファイルを削除
        foreach(var file in files)
        {
            try
            {
                File.Delete(file);
            }
            catch(FileNotFoundException)
            {
                continue;
            }
            catch(UnauthorizedAccessException)
            {
                continue;
            }
        }
    }
}
