using System.IO;
using MovieEditor.Models.Compression;
using MovieEditor.Models.Information;

namespace MovieEditor.Models.AudioExtraction;

internal class ParallelExtractionRunner(ILogSendable logger) : IDisposable, IAnyProcess
{
    private static readonly object ParallelLock = new();
    private readonly ILogSendable _logger = logger;
    private CancellationTokenSource? _cancelable = null;

    public event Action<int>? OnStartProcess = null;
    public event Action<int>? OnUpdateProgress = null;

    public async Task<bool> Run(MovieInfo[] sources, string outputFolder, string attachedNameTag)
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
                        var outputPath = GetOutputPath(movieInfo.FilePath, outputFolder, attachedNameTag);
                        AudioExtractor.Extract
                        (
                            movieInfo,
                            outputPath,
                            _cancelable.Token
                        );

                        _cancelable.Token.ThrowIfCancellationRequested();

                        long fileSize = new FileInfo(outputPath).Length / 1000;
                        lock(ParallelLock)
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

    public void Dispose()
    {
        Cancel();
    }
}