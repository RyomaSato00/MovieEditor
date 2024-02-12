
using MovieEditor.Models.AudioExtraction;
using MovieEditor.Models.Compression;
using MovieEditor.Models.Information;
using MovieEditor.Models.Join;
using MovieEditor.Models.SpeedChange;

namespace MovieEditor.Models;

internal class ModelManager : IDisposable
{
    private readonly ParallelCompressionRunner _parallelComp;
    private readonly ParallelExtractionRunner _parallelExtract;
    private readonly ParallelSpeedChangeRunner _speedChange;

    public ParallelCompressionRunner ParallelComp => _parallelComp;
    public ParallelExtractionRunner ParallelExtract => _parallelExtract;
    public ParallelSpeedChangeRunner ParallelSpeedChange => _speedChange;

    public ModelManager()
    {
        _parallelComp = new ParallelCompressionRunner(); 
        _parallelExtract = new ParallelExtractionRunner();
        _speedChange = new ParallelSpeedChangeRunner();
        // 前回使用したキャッシュがあれば削除する
        MovieInfo.DeleteThumbnailCaches();
        VideoJoiner.DeleteJoinCaches();
    }

    public void Dispose()
    {
        _speedChange.Dispose();
        _parallelComp.Dispose();
        _parallelExtract.Dispose();
    }
}