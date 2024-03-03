
using MovieEditor.Models.AudioExtraction;
using MovieEditor.Models.Compression;
using MovieEditor.Models.ImageGenerate;
using MovieEditor.Models.Information;
using MovieEditor.Models.Join;
using MovieEditor.Models.SpeedChange;

namespace MovieEditor.Models;

internal class ModelManager : IDisposable
{

    public ModelManager()
    {
        // 前回使用したキャッシュがあれば削除する
        MovieInfo.DeleteThumbnailCaches();
        VideoJoiner.DeleteJoinCaches();
    }

    public void Dispose()
    {
    }
}