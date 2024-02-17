using System.IO;
using MovieEditor.Models.Information;
using MyCommonFunctions;

namespace MovieEditor.Models.Compression;

internal class ParallelCompressionRunner : IDisposable, IAnyProcess
{
    private static readonly object _parallelLock = new();
    private CancellationTokenSource? _cancelable = null;

    public event Action<int>? OnStartProcess = null;
    public event Action<int>? OnUpdateProgress = null;

    /// <summary>
    /// 非同期で動画圧縮を並列に実行する
    /// </summary>
    /// <param name="sources"></param>
    /// <param name="outputFolder">出力先フォルダパス</param>
    /// <param name="attachedNameTag"></param>
    /// <param name="parameter">圧縮条件</param>
    /// <returns>処理済みファイル配列</returns>
    public async Task<MovieInfo[]> Run
    (
        MovieInfo[] sources,
        string outputFolder,
        string? attachedNameTag,
        CompressionParameter parameter
    )
    {
        int finishedCount = 0;
        int allCount = sources.Length;
        _cancelable?.Cancel();
        _cancelable = new CancellationTokenSource();
        // 処理完了確認用チェックリストを作成
        var checkList = ToCheckList(sources);

        OnStartProcess?.Invoke(allCount);
        var startTime = DateTime.Now;
        await Task.Run(() =>
        {
            try
            {
                sources
                .AsParallel()
                .WithCancellation(_cancelable.Token)
                .WithDegreeOfParallelism(2)
                .ForAll(movieInfo =>
                {
                    // 出力パスがすでに指定されていればそれを使用する。
                    // 出力パスがなければ（nullならば）ここで指定する
                    movieInfo.OutputPath ??= GetOutputPath(movieInfo.FilePath, outputFolder, attachedNameTag, parameter.Format);
                    VideoCompressor.Compress
                    (
                        movieInfo,
                        parameter,
                        _cancelable.Token
                    );

                    _cancelable.Token.ThrowIfCancellationRequested();

                    long fileSize = new FileInfo(movieInfo.OutputPath).Length / 1000;
                    lock (_parallelLock)
                    {
                        finishedCount++;
                        // チェックリストを完了にする
                        checkList[movieInfo] = true;
                        OnUpdateProgress?.Invoke(finishedCount);
                        MyConsole.WriteLine($"{movieInfo.FileName} has finished ({fileSize} kb) ({finishedCount}/{allCount})", MyConsole.Level.Info);
                    }
                });
            }
            catch (OperationCanceledException)
            {
                MyConsole.WriteLine("キャンセルされました", MyConsole.Level.Info);
            }
            catch(Exception e)
            {
                MyConsole.WriteLine($"想定外のエラー:{e.Message}", MyConsole.Level.Error);
                System.Diagnostics.Debug.WriteLine(e);
            }
        },
        _cancelable.Token);

        var processTime = DateTime.Now - startTime;
        MyConsole.WriteLine($"完了:{(long)processTime.TotalMilliseconds}ms", MyConsole.Level.Info);
        // 処理済みの動画データの配列を返す
        return checkList
            .Where(item => true == item.Value)
            .Select(item => item.Key)
            .ToArray();
    }

    public void Cancel()
    {
        _cancelable?.Cancel();
    }

    private static string GetOutputPath
    (
        string inputPath,
        string outputFolder,
        string? attachedNameTag,
        string format
    )
    {
        var purefileName = Path.GetFileNameWithoutExtension(inputPath);

        string fileName;
        if (attachedNameTag is null)
        {
            fileName = $"{purefileName}.{format}";
        }
        else
        {
            fileName = $"{purefileName}_{attachedNameTag}.{format}";
        }
        return Path.Combine(outputFolder, fileName);
    }

    private static Dictionary<MovieInfo, bool> ToCheckList(MovieInfo[] sources)
    {
        // keyを各MovieInfoオブジェクト、valueをfalseにもつdictionary型
        return sources.ToDictionary(movieInfo => movieInfo, value => false);
    }

    public void Dispose()
    {
        Cancel();
    }
}