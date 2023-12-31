using MovieEditor.Models.Information;
using System.IO;

namespace MovieEditor.Models.SpeedChange;

internal class ParallelSpeedChangeRunner(ILogSendable logger) : IDisposable, IAnyProcess
{
    private static readonly object _parallelLock = new();
    private readonly ILogSendable _logger = logger;
    private CancellationTokenSource? _cancelable = null;
    public event Action<int>? OnStartProcess;
    public event Action<int>? OnUpdateProgress;

    public async Task<bool> Run(
        MovieInfo[] sources,
        string outputFolder,
        string attachedNameTag,
        double speed
    )
    {
        int finishedCount = 0;
        int allCount = sources.Length;
        _cancelable?.Cancel();
        _cancelable = new CancellationTokenSource();

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
                        string outputPath = ToOutputPath(movieInfo.FilePath, outputFolder, attachedNameTag);
                        SpeedChanger.ChangeSpeed(
                            movieInfo,
                            outputPath,
                            speed,
                            _cancelable.Token
                        );

                        _cancelable.Token.ThrowIfCancellationRequested();

                        long fileSize = new FileInfo(outputPath).Length / 1000;
                        lock(_parallelLock)
                        {
                            finishedCount++;
                            OnUpdateProgress?.Invoke(finishedCount);
                            _logger.SendLog($"{movieInfo.FileName} has finished ({fileSize} kb) ({finishedCount}/{allCount})");
                        }
                    });
            }
            catch(OperationCanceledException)
            {
                _logger.SendLog("キャンセルされました");
            }
        },
        _cancelable.Token);

        var processTime = DateTime.Now - startTime;
        _logger.SendLog($"完了:{(long)processTime.TotalMilliseconds}ms");
        return _cancelable.Token.IsCancellationRequested;
    }

    public void Cancel()
    {
        _cancelable?.Cancel();
    }

    private static string ToOutputPath(
        string inputPath,
        string outputFolder,
        string attachedNameTag
    )
    {
        var pureFileName = Path.GetFileNameWithoutExtension(inputPath);
        var extension = Path.GetExtension(inputPath);
        return Path.Combine(
            outputFolder,
            $"{pureFileName}_{attachedNameTag}{extension}"
        );
    }

    public void Dispose()
    {
        Cancel();
    }
}