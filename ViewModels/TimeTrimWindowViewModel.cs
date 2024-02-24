using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using MovieEditor.Models;
using MovieEditor.Models.Information;
using Reactive.Bindings;
using Reactive.Bindings.Disposables;
using Reactive.Bindings.Extensions;
using System.ComponentModel;
using System.Windows.Media.Imaging;

namespace MovieEditor.ViewModels;

internal partial class TimeTrimWindowViewModel : ObservableObject, IDisposable
{
    private static readonly string _timeSpanFormat = "mm\\:ss\\.fff";
    private readonly TaskCompletionSource<(TimeSpan?, TimeSpan?)> _enterWaitable = new();

    private readonly CompositeDisposable _disposables = [];

    /// <summary> 初期動画総再生時間（秒） </summary>
    private readonly double _defaultDuration;

    public Task<(TimeSpan?, TimeSpan?)> ResultWaitable => _enterWaitable.Task;

    public TimeTrimWindowViewModel(string filePath, TimeSpan duration)
    {
        MoviePath = filePath;
        TrimedDuration = duration.ToString(_timeSpanFormat);
        _defaultDuration = duration.TotalSeconds;

        TrimStart
            // TrimStart.Valueが変更されたときのイベントハンドラを定義する
            .Subscribe(OnTrimStartChanged)
            .AddTo(_disposables);

        TrimEnd
            // TrimEnd.Valueが変更されたときのイベントハンドラを定義する
            .Subscribe(OnTrimEndChanged)
            .AddTo(_disposables);
    }

    public void Dispose()
    {
        _disposables.Dispose();
        if (ResultWaitable.IsCompleted) return;
        _enterWaitable.SetCanceled();
    }

    /// <summary> 動画ファイルのパス </summary>
    [ObservableProperty] private string _moviePath = string.Empty;
    
    /// <summary> 時間範囲開始時刻秒数 </summary>
    public ReactivePropertySlim<double?> TrimStart { get; } = new(null);
    /// <summary> 時間範囲終了時刻秒数 </summary>
    public ReactivePropertySlim<double?> TrimEnd { get; } = new(null);
    /// <summary> トリミング後の総再生時間 </summary>
    [ObservableProperty] private string _trimedDuration;
    /// <summary> トリミング開始時刻におけるサムネイル </summary>
    [ObservableProperty] private BitmapImage? _startImage;
    /// <summary> トリミング終了時刻におけるサムネイル </summary>
    [ObservableProperty] private BitmapImage? _endImage;


    [RelayCommand] private void SetStartTime(double currentPosition)
    {
        // 小数点第3位まで丸める
        TrimStart.Value = Math.Round(currentPosition / 1000, 3);
    }

    [RelayCommand] private void SetEndTime(double currentPosition)
    {
        // 小数点第3位まで丸める
        TrimEnd.Value = Math.Round(currentPosition / 1000, 3);
    }

    /// <summary>
    /// TrimStart.Valueが変更されたときのイベントハンドラ
    /// </summary>
    /// <param name="time"></param>
    private void OnTrimStartChanged(double? time)
    {
        // timeがnullでないとき
        if(time is double trimStartTime)
        {
            // timeにおけるサムネイルを取得してImageに更新
            var imageUri = MovieInfo.GetThumbnailUri(MoviePath, trimStartTime);
            StartImage = new BitmapImage(imageUri);

            // timeを適用したときのトリミング後総再生時間を再計算
            double trimEnd = TrimEnd.Value ?? _defaultDuration;
            TrimedDuration = TimeSpan.FromSeconds(trimEnd - trimStartTime).ToString(_timeSpanFormat);
        }
        // timeがnullだったとき
        else
        {
            // TrimStart.Value = 0として、トリミング後総再生時間を再計算
            double trimEnd = TrimEnd.Value ?? _defaultDuration;
            TrimedDuration = TimeSpan.FromSeconds(trimEnd - 0).ToString(_timeSpanFormat);
        }
    }

    /// <summary>
    /// TrimEndが変更されたときのイベントハンドラ
    /// </summary>
    /// <param name="time"></param>
    private void OnTrimEndChanged(double? time)
    {
        // timeがnullでないとき
        if(time is double trimEndTime)
        {
            // timeにおけるサムネイルを取得してImageに更新
            var imageUri = MovieInfo.GetThumbnailUri(MoviePath, trimEndTime);
            EndImage = new BitmapImage(imageUri);

            // timeを適用したときのトリミング後総再生時間を再計算
            double trimStart = TrimStart.Value ?? 0;
            TrimedDuration = TimeSpan.FromSeconds(trimEndTime - trimStart).ToString(_timeSpanFormat);
        }
        // timeがnullだったとき
        else
        {
            // TrimEnd.Value = _defaultDurationとして、トリミング後総再生時間を再計算
            double trimStart = TrimStart.Value ?? 0;
            TrimedDuration = TimeSpan.FromSeconds(_defaultDuration - trimStart).ToString(_timeSpanFormat);
        }
    }

    [RelayCommand]
    private void Enter()
    {
        TimeSpan? start = null, end = null;
        // TrimStartがdouble型（=nullではない）のとき、startにはTrimStartのTimeSpanを入れる
        if(TrimStart.Value is double trimStart) start = TimeSpan.FromSeconds(trimStart);
        // TrimEndがdouble型（=nullではない）のとき、endにはTrimEndのTimeSpanを入れる
        if(TrimEnd.Value is double trimEnd) end = TimeSpan.FromSeconds(trimEnd);
        _enterWaitable.SetResult((start, end));
    }
}