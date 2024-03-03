using MovieEditor.Models.Information;
using System.Diagnostics;
using MyCommonFunctions;
using System.IO;

namespace MovieEditor.Models.SpeedChange;

internal class ParallelSpeedChangeRunner(MovieInfo[] sources) : IDisposable, IAnyProcess
{
    private static readonly object _parallelLock = new();

    private readonly CancellationTokenSource _cancelable = new();
    /// <summary> 処理を行う動画ソース </summary>
    private readonly MovieInfo[] _sources = sources;
    /// <summary> 処理完了確認用チェックリスト </summary>
    private readonly Dictionary<MovieInfo, bool> _checkList = sources.ToDictionary(movieInfo => movieInfo, _ => false);
    /// <summary> プロセスキャンセル用一時保存リスト </summary>
    private readonly List<Process> _processes = [];

    /// <summary> 処理を行うファイルの総数 </summary>
    public int FileCount => _sources.Length;
    /// <summary> 1つのファイルの処理が完了するたびに実行するイベント（引数：処理完了したファイルの数） </summary>
    public event Action<int>? OnUpdateProgress = null;

    /// <summary>
    /// 再生速度変更を並列に実行する
    /// </summary>
    /// <param name="outputFolder">出力先フォルダパス</param>
    /// <param name="attachedNameTag"></param>
    /// <param name="speed">速度倍率</param>
    /// <returns>処理済みファイル配列</returns>
    public MovieInfo[] Run(
        string outputFolder,
        string attachedNameTag,
        double speed
    )
    {
        MyConsole.WriteLine($"再生速度変更処理開始", MyConsole.Level.Info);

        // 処理開始時刻
        var startTime = DateTime.Now;

        try
        {
            // 並列に圧縮処理を行う
            ProcessParallelly(
                outputFolder, attachedNameTag, speed
            );
        }
        catch (OperationCanceledException)
        {
            MyConsole.WriteLine("キャンセルされました", MyConsole.Level.Info);
        }
        catch (Exception e)
        {
            MyConsole.WriteLine($"想定外のエラー:{e}", MyConsole.Level.Error);
            System.Diagnostics.Debug.WriteLine(e);
        }

        // 処理完了までにかかった時間を取得する
        var processTime = DateTime.Now - startTime;

        // コンソール出力
        MyConsole.WriteLine($"完了:{(long)processTime.TotalMilliseconds}ms", MyConsole.Level.Info);

        // 処理済みの動画データの配列を返す
        return _checkList
            .Where(item => true == item.Value)
            .Select(item => item.Key)
            .ToArray();
    }

    /// <summary>
    /// 再生速度変更を並列に行う
    /// </summary>
    /// <param name="outputFolder">出力先フォルダパス</param>
    /// <param name="attachedNameTag"></param>
    /// <param name="speed">再生速度倍率</param>
    private void ProcessParallelly(
        string outputFolder,
        string attachedNameTag,
        double speed
    )
    {
        // 処理が終了したファイルの数
        int finishedCount = 0;
        // 処理を行う予定のファイル総数
        int allCount = _sources.Length;

        _sources
            .AsParallel()
            .WithCancellation(_cancelable.Token)
            // .WithDegreeOfParallelism(2)
            .ForAll(movieInfo =>
            {
                // キャンセルされていたらここで終了
                _cancelable.Token.ThrowIfCancellationRequested();

                // 出力パスがすでに指定されていればそれを使用する。
                // 出力パスがなければ（nullならば）ここで指定する
                movieInfo.OutputPath ??= ToOutputPath(
                    movieInfo.FilePath, outputFolder, attachedNameTag, movieInfo.DuplicateCount
                );

                // 速度変更処理のプロセスオブジェクトを作成する。ここではまだ実行されていない
                using var process = SpeedChanger.ToSpeedChageProcess(movieInfo, speed);

                // 速度変更処理のプロセスを一時保存用リストに追加する。リストはスレッドセーフではなため、ロックをかける
                lock (_parallelLock)
                {
                    _processes.Add(process);
                }

                // 速度変更処理プロセス開始
                process.Start();

                // 速度変更処理待機
                process.WaitForExit();

                // 速度変更処理中にキャンセルされたときはここで終了
                _cancelable.Token.ThrowIfCancellationRequested();

                // 速度変更処理が完了したファイルのファイルサイズ(kb)を取得する
                var fileSize = new FileInfo(movieInfo.OutputPath).Length / 1000;

                lock (_parallelLock)
                {
                    // 一時保存用リストから今回のプロセスを削除する
                    _processes.Remove(process);

                    // 処理完了ファイル数をインクリメント
                    finishedCount++;

                    // 処理完了イベントを発行
                    OnUpdateProgress?.Invoke(finishedCount);

                    // コンソール出力
                    MyConsole.WriteLine($"{movieInfo.FileName} has been finished ({fileSize} kb) ({finishedCount}/{allCount})", MyConsole.Level.Info);

                    // チェックリストを完了にする
                    _checkList[movieInfo] = true;
                }
            });
    }

    /// <summary>
    /// 速度変更処理キャンセル
    /// </summary>
    public void Cancel()
    {
        // キャンセルを発行
        _cancelable.Cancel();

        // リストはスレッドセーフではないため、ロックをかける
        lock (_parallelLock)
        {
            foreach (var process in _processes)
            {
                // プロセス中断
                process.Kill();

                // プロセス破棄
                process.Dispose();
            }

            // 一時保存用リストの中身を全削除
            // （キャンセルが複数回呼ばれるとprocess.Disposeが複数回行われ、エラーとなる）
            _processes.Clear();
        }
    }

    /// <summary>
    /// 条件に合わせて出力パスを作成する
    /// </summary>
    /// <param name="inputPath">入力パス</param>
    /// <param name="outputFolder">出力先ディレクトリ</param>
    /// <param name="attachedNameTag">タグ</param>
    /// <param name="duplicateCount">同ファイルの複製回数</param>
    /// <returns></returns>
    private static string ToOutputPath(
        string inputPath,
        string outputFolder,
        string? attachedNameTag,
        int duplicateCount
    )
    {
        var pureFileName = Path.GetFileNameWithoutExtension(inputPath);
        var extension = Path.GetExtension(inputPath);

        string fileName;
        // タグを付けないとき
        if (attachedNameTag is null)
        {
            // 複製回数が1回以上
            if (0 < duplicateCount)
            {
                fileName = $"{pureFileName}({duplicateCount}){extension}";
            }
            else
            {
                fileName = $"{pureFileName}{extension}";
            }
        }
        // タグを付けるとき
        else
        {
            // 複製回数が1回以上
            if (0 < duplicateCount)
            {
                fileName = $"{pureFileName}({duplicateCount})_{attachedNameTag}{extension}";
            }
            else
            {
                fileName = $"{pureFileName}_{attachedNameTag}{extension}";
            }
        }
        return Path.Combine(outputFolder, fileName);
    }

    public void Dispose()
    {
        Cancel();
    }
}