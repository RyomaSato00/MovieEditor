using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using MovieEditor.Models;
using MyCommonFunctions;

namespace MovieEditor.ViewModels;

internal partial class ProgressWindowViewModel : ObservableObject, IDisposable
{
    private readonly IAnyProcess _anyProcess;

    public ProgressWindowViewModel(IAnyProcess process)
    {
        _anyProcess = process;
        // 処理開始時処理
        ProgressMaxCount = process.FileCount;
        // 処理進捗更新時処理
        _anyProcess.OnUpdateProgress += OnUpdateProgress;
    }

    public void Dispose()
    {
        // このクラスと紐づくイベントを削除する
        _anyProcess.OnUpdateProgress -= OnUpdateProgress;
    }

    private void OnUpdateProgress(int progress)
    {
        ProgressCount = progress;
        // 分母が0のときは0
        ProgressRate = (0 != ProgressMaxCount) ? (100 * progress / ProgressMaxCount) : 0;
    }

    [ObservableProperty] private int _progressCount = 0;
    [ObservableProperty] private int _progressMaxCount = 0;
    [ObservableProperty] private float _progressRate = 0;
    [RelayCommand]
    private void Cancel()
    {
        _anyProcess.Cancel();
        MyConsole.WriteLine("キャンセルしています...", MyConsole.Level.Info);
    }

}