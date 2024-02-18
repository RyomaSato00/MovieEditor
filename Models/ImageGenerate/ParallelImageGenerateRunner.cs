using MovieEditor.Models.Information;
using MyCommonFunctions;
using System.IO;

namespace MovieEditor.Models.ImageGenerate;

internal class ParallelImageGenerateRunner : IDisposable, IAnyProcess
{
    private static readonly object _parallelLock = new();
    private CancellationTokenSource? _cancelable = null;

    public event Action<int>? OnStartProcess = null;
    public event Action<int>? OnUpdateProgress = null;

    public void Cancel()
    {
        _cancelable?.Cancel();
    }

    public void Dispose()
    {
        Cancel();
    }

    public async Task<MovieInfo[]> Run(
        MovieInfo[] sources, string outputFolder, ImageGenerateParameter parameter
    )
    {
        _cancelable?.Cancel();
        _cancelable = new CancellationTokenSource();

        // 処理完了確認用チェックリスト作成
        var checkList = sources.ToDictionary(movieInfo => movieInfo, _ => false);

        OnStartProcess?.Invoke(sources.Length);
        // 処理開始時刻
        var startTime = DateTime.Now;
        await Task.Run(() =>
        {
            try
            {
                ProcessParallelly(
                    sources, outputFolder, parameter, checkList, _cancelable.Token
                );
            }
            catch (OperationCanceledException)
            {
                MyConsole.WriteLine("キャンセルされました", MyConsole.Level.Info);
            }
            catch (Exception e)
            {
                MyConsole.WriteLine($"想定外のエラー:{e.ToString()}", MyConsole.Level.Error);
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

    private void ProcessParallelly(
        MovieInfo[] sources,
        string outputFolder,
        ImageGenerateParameter parameter,
        Dictionary<MovieInfo, bool> checkList,
        CancellationToken cancel = default
    )
    {
        // 処理が終了したファイルの数
        int finishedCount = 0;
        // 処理を行う予定のファイル総数
        int allCount = sources.Length;

        sources
            .AsParallel()
            .WithCancellation(cancel)
            .WithDegreeOfParallelism(2)
            .ForAll(movieInfo =>
            {
                // 出力パスがすでに指定されていればそれを使用する。
                // 出力パスがなければ（nullならば）ここで指定する
                movieInfo.OutputPath ??= ToOutputPath(movieInfo.FilePath, outputFolder);
                ImageGenerator.Generate(movieInfo, parameter);

                cancel.ThrowIfCancellationRequested();

                lock (_parallelLock)
                {
                    finishedCount++;
                    // チェックリストを完了にする
                    checkList[movieInfo] = true;
                    OnUpdateProgress?.Invoke(finishedCount);
                    MyConsole.WriteLine($"{movieInfo.FileName} has been finished ({finishedCount}/{allCount})", MyConsole.Level.Info);
                }
            });
    }

    private static string ToOutputPath(
        string inputPath, string outputFolder
    )
    {
        var fileName = Path.GetFileName(inputPath);

        return Path.Combine(outputFolder, fileName);
    }
}