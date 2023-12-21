using System.Diagnostics;
using System.IO;
using MovieEditor.Models.Information;

namespace MovieEditor.Models.Compression;

internal class ParallelCompressionRunner(ILogSendable logger) : IDisposable
{
    private static readonly object ParallelLock = new();
    private readonly ILogSendable _logger = logger;
    private CancellationTokenSource? _cancelable = null;

    public async Task Run
    (
        MovieInfo[] sources,
        string outputFolder,
        string attachedNameTag,
        CompressionParameter parameter
    )
    {
        int finishedCount = 0;
        int allCount = sources.Length;
        _cancelable?.Cancel();
        _cancelable = new CancellationTokenSource();

        Stopwatch time = new();
        time.Start();
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
                    VideoCompressor.Compress
                    (
                        movieInfo,
                        GetOutputPath(movieInfo.FilePath, outputFolder, attachedNameTag),
                        parameter,
                        _cancelable.Token
                    );

                    _cancelable.Token.ThrowIfCancellationRequested();

                    lock (ParallelLock)
                    {
                        finishedCount++;
                        _logger.SendLog($"{movieInfo.FileName} has finished ({finishedCount}/{allCount})");
                    }
                });
            }
            catch (OperationCanceledException)
            {
                _logger.SendLog("キャンセルされました");
            }
        },
        _cancelable.Token);

        time.Stop();
        _logger.SendLog($"完了:{time.ElapsedMilliseconds}ms");
    }

    public void Cancel()
    {
        _cancelable?.Cancel();
    }

    private static string GetOutputPath
    (
        string inputPath,
        string outputFolder,
        string attachedNameTag
    )
    {
        var purefileName = Path.GetFileNameWithoutExtension(inputPath);
        var extension = Path.GetExtension(inputPath);
        return Path.Combine
        (
            outputFolder,
            $"{purefileName}_{attachedNameTag}{extension}"
        );
    }

    public void Dispose()
    {
        Cancel();
    }
}