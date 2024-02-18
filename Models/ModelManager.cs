
using MovieEditor.Models.AudioExtraction;
using MovieEditor.Models.Compression;
using MovieEditor.Models.ImageGenerate;
using MovieEditor.Models.Information;
using MovieEditor.Models.Join;
using MovieEditor.Models.SpeedChange;

namespace MovieEditor.Models;

internal class ModelManager : IDisposable
{
    private readonly ParallelCompressionRunner _parallelComp;
    private readonly ParallelExtractionRunner _parallelExtract;
    private readonly ParallelSpeedChangeRunner _speedChange;
    private readonly ParallelImageGenerateRunner _imageGenerate;

    public ParallelCompressionRunner ParallelComp => _parallelComp;
    public ParallelExtractionRunner ParallelExtract => _parallelExtract;
    public ParallelSpeedChangeRunner ParallelSpeedChange => _speedChange;
    public ParallelImageGenerateRunner ParallelImageGenerate => _imageGenerate;

    public ModelManager()
    {
        _parallelComp = new ParallelCompressionRunner();
        _parallelExtract = new ParallelExtractionRunner();
        _speedChange = new ParallelSpeedChangeRunner();
        _imageGenerate = new ParallelImageGenerateRunner();
        // 前回使用したキャッシュがあれば削除する
        MovieInfo.DeleteThumbnailCaches();
        VideoJoiner.DeleteJoinCaches();
    }

    public void Dispose()
    {
        _imageGenerate.Dispose();
        _speedChange.Dispose();
        _parallelComp.Dispose();
        _parallelExtract.Dispose();
    }
}