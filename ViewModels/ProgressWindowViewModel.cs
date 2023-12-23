using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using MovieEditor.Models;

namespace MovieEditor.ViewModels;

internal partial class ProgressWindowViewModel : ObservableObject, IDisposable
{
    private readonly ModelManager _modelManager;
    private readonly Action _windowClose;

    public ProgressWindowViewModel(ModelManager model, Action windowClose)
    {
        _modelManager = model;
        _windowClose = windowClose;
        // 処理進捗更新時処理
        _modelManager.ParallelComp.OnUpdateProgress += OnUpdateProgress;
    }

    public void Dispose()
    {
        // このクラスと紐づくイベントを削除する
        _modelManager.ParallelComp.OnUpdateProgress -= OnUpdateProgress;
        // ウィンドウを閉じる
        _windowClose.Invoke();
    }

    private void OnUpdateProgress(int progress, int max)
    {
        ProgressCount = progress;
        ProgressMaxCount = max;
        // 分母が0のときは0
        ProgressRate = (0 != max) ? (100 * progress / max) : 0;
    }
    
    [ObservableProperty] private int _progressCount = 0;
    [ObservableProperty] private int _progressMaxCount = 0;
    [ObservableProperty] private float _progressRate = 0;
    [RelayCommand] private void Cancel()
    {
        _modelManager.ParallelComp.Cancel();
        Dispose();
    }

}