using MovieEditor.Models.Compression;
using MovieEditor.Models.Information;
using MyCommonFunctions;
using System.Diagnostics;
using System.IO;

namespace MovieEditor.Models.Join;

internal class VideoJoiner(MovieInfo[] sources) : IDisposable
{
    public static readonly string JoinSourceCacheFolder = @"cache\join\sources";
    public static readonly string JoinedFileCacheFolder = @"cache\join\joined";

    /// <summary>
    /// 動画結合用に生成したキャッシュファイルをすべて削除する
    /// </summary>
    public static void DeleteJoinCaches()
    {
        // キャッシュフォルダが存在するとき
        if (Directory.Exists(JoinSourceCacheFolder))
        {
            var files = Directory.GetFiles(JoinSourceCacheFolder);
            foreach (var file in files)
            {
                try
                {
                    File.Delete(file);
                }
                catch (Exception)
                {
                    continue;
                }
            }
        }
        // キャッシュフォルダが存在するとき
        if (Directory.Exists(JoinedFileCacheFolder))
        {
            var files = Directory.GetFiles(JoinedFileCacheFolder);
            foreach (var file in files)
            {
                try
                {
                    File.Delete(file);
                }
                catch (Exception)
                {
                    continue;
                }
            }
        }
    }


    private readonly CancellationTokenSource _cancelable = new();
    /// <summary> 処理を行う動画ソース </summary>
    private readonly MovieInfo[] _sources = sources;
    /// <summary> 圧縮処理用オブジェクト </summary>
    private readonly ParallelCompressionRunner _compressor = new(sources);

    /// <summary> プロセスキャンセル用一時保存変数 </summary>
    private Process? _runningProcess = null;

    /// <summary>
    /// 動画結合処理キャンセル
    /// </summary>
    public void Cancel()
    {
        _compressor.Cancel();
        _cancelable.Cancel();
        _runningProcess?.Kill();
        _runningProcess?.Dispose();
        _runningProcess = null;
    }

    /// <summary>
    /// 動画結合処理を行う
    /// </summary>
    /// <returns>結合によって生成した動画のパス</returns>
    public async Task<string> Join()
    {
        // 時間範囲指定後のファイルまたはコピーをcacheに保存する

        // キャッシュフォルダ作成
        if (false == Directory.Exists(JoinSourceCacheFolder))
        {
            Directory.CreateDirectory(JoinSourceCacheFolder);
        }

        if (false == Directory.Exists(JoinedFileCacheFolder))
        {
            Directory.CreateDirectory(JoinedFileCacheFolder);
        }

        // ファイル名に-や()が含まれていると結合できない
        // 結合に使用するファイルの名前を変更する
        int id = 0;
        foreach (var movie in _sources)
        {
            movie.OutputPath = Path.Combine(JoinSourceCacheFolder, $"{id}{movie.Extension}");
            id++;
        }

        // キャッシュフォルダに時間範囲トリミング後のファイルを保存。時間範囲指定していない場合もコピーを保存
        var processedFiles = await Task.Run(() =>
        {
            return _compressor.Run(
                JoinSourceCacheFolder, null, new CompressionParameter()
                {
                    VideoCodec = "h264",
                    Format = "mp4"
                }
            );
        });

        // 結合リストファイル作成
        var joinListFile = await CreateJoinListFile(_sources, _cancelable.Token);

        // 結合動画生成
        var joinedFile = await Task.Run(() => CreateJoinedVideo(joinListFile), _cancelable.Token);

        // 生成した結合動画のパスを返す
        return joinedFile;
    }

    /// <summary>
    /// ffmpegでの動画結合処理に必要なリストファイルを作成する
    /// </summary>
    /// <param name="movieInfos"></param>
    /// <param name="cancelToken"></param>
    /// <returns>リストファイルのパスa</returns>
    private static async Task<string> CreateJoinListFile(MovieInfo[] movieInfos, CancellationToken cancelToken = default)
    {
        var contents = movieInfos
            .Select(info => $"file {Path.GetFileNameWithoutExtension(info.OutputPath)}.mp4");
        var filePath = Path.Combine(
            JoinSourceCacheFolder,
            $"join_list_{DateTime.Now:yyyyMMdd_HHmmss}.txt"
        );
        await File.WriteAllLinesAsync(filePath, contents, cancelToken);
        return filePath;
    }

    /// <summary>
    /// ffmpegを使用して動画結合処理を行う
    /// </summary>
    /// <param name="joinListPath"></param>
    /// <returns>結合によって生成した動画のパス</returns>
    private string CreateJoinedVideo(string joinListPath)
    {
        var outputPath = Path.Combine(
            JoinedFileCacheFolder,
            $"joined_{DateTime.Now:yyyyMMdd_HHmmss}.mp4"
        );

        ProcessStartInfo processInfo = new("ffmpeg")
        {
            Arguments = $"-y -f concat -i \"{joinListPath}\" \"{outputPath}\"",
            UseShellExecute = false,
            CreateNoWindow = true
        };
        Debug.WriteLine($"arg:{processInfo.Arguments}");
        MyConsole.WriteLine($"arg:{processInfo.Arguments}");

        using var process = new Process() { StartInfo = processInfo };
        _runningProcess = process;
        process.Start();
        process.WaitForExit();
        _runningProcess = null;

        return outputPath;
    }

    public void Dispose()
    {
        Cancel();
    }
}