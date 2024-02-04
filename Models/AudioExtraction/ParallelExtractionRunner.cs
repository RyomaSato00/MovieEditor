using System.IO;
using MovieEditor.Models.Compression;
using MovieEditor.Models.Information;
using MyCommonFunctions;

namespace MovieEditor.Models.AudioExtraction;

internal class ParallelExtractionRunner : IDisposable, IAnyProcess
{
    private static readonly object _parallelLock = new();
    private CancellationTokenSource? _cancelable = null;

    public event Action<int>? OnStartProcess = null;
    public event Action<int>? OnUpdateProgress = null;

    /// <summary>
    /// 非同期で音声抽出を並列に実行する
    /// </summary>
    /// <param name="sources"></param>
    /// <param name="outputFolder">出力先フォルダパス</param>
    /// <param name="attachedNameTag"></param>
    /// <returns>処理済みファイル配列</returns>
    public async Task<MovieInfo[]> Run(MovieInfo[] sources, string outputFolder, string attachedNameTag)
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
                        var outputPath = GetOutputPath(movieInfo.FilePath, outputFolder, attachedNameTag);
                        AudioExtractor.Extract
                        (
                            movieInfo,
                            outputPath,
                            _cancelable.Token
                        );

                        _cancelable.Token.ThrowIfCancellationRequested();

                        long fileSize = new FileInfo(outputPath).Length / 1000;
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

    private static string GetOutputPath(
        string inputPath,
        string outputFolder,
        string attachedNameTag
    )
    {
        var pureFileName = Path.GetFileNameWithoutExtension(inputPath);
        return Path.Combine
        (
            outputFolder,
            $"{pureFileName}_{attachedNameTag}.aac"
        );
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