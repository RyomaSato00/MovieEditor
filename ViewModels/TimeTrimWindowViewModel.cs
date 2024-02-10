using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using MovieEditor.Models;

namespace MovieEditor.ViewModels;

internal partial class TimeTrimWindowViewModel : ObservableObject, IDisposable
{
    private readonly TaskCompletionSource<(TimeSpan?, TimeSpan?)> _enterWaitable = new();

    public Task<(TimeSpan?, TimeSpan?)> ResultWaitable => _enterWaitable.Task;

    public TimeTrimWindowViewModel()
    {
        
    }

    public void Dispose()
    {
        if (ResultWaitable.IsCompleted) return;
        _enterWaitable.SetCanceled();
    }

    /// <summary> 時間範囲開始時刻秒数 </summary>
    [ObservableProperty] private double? _trimStart = null;
    /// <summary> 時間範囲終了時刻秒数 </summary>
    [ObservableProperty] private double? _trimEnd = null;

    [RelayCommand]
    private void Enter()
    {
        TimeSpan? start = null, end = null;
        // TrimStartがdouble型（=nullではない）のとき、startにはTrimStartのTimeSpanを入れる
        if(TrimStart is double trimStart) start = TimeSpan.FromSeconds(trimStart);
        // TrimEndがdouble型（=nullではない）のとき、endにはTrimEndのTimeSpanを入れる
        if(TrimEnd is double trimEnd) end = TimeSpan.FromSeconds(trimEnd);
        _enterWaitable.SetResult((start, end));
    }
}