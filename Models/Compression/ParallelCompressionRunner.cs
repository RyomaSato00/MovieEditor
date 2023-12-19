using System.IO;
using MovieEditor.Models.Information;

namespace MovieEditor.Models.Compression;

internal class ParallelCompressionRunner(ILogSendable logger)
{
    private static readonly object ParallelLock = new();
    private readonly ILogSendable _logger = logger;

    public void Run
    (
        MovieInfo[] sources,
        string outputFolder,
        string attachedNameTag,
        CompressionParameter parameter
    )
    {
        int finishedCount = 0;
        int allCount = sources.Length;

        Task.Run(() =>
        {
            sources
            .AsParallel()
            // .WithDegreeOfParallelism(1)
            .ForAll(movieInfo =>
            {
                VideoCompressor.Compress
                (
                    movieInfo.FilePath,
                    GetOutputPath(movieInfo.FilePath, outputFolder, attachedNameTag),
                    parameter
                );

                lock(ParallelLock)
                {
                    finishedCount++;
                    _logger.SendLogFromAsync($"{movieInfo.FileName} has finished ({finishedCount}/{allCount})");
                }
            });
        });
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
}