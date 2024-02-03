using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using MovieEditor.Models;

namespace MovieEditor.ViewModels;

internal partial class TimeTrimWindowViewModel : ObservableObject, IDisposable
{
    private readonly TaskCompletionSource<(double?, double?)> _enterWaitable = new();

    public Task<(double?, double?)> ResultWaitable => _enterWaitable.Task;

    public TimeTrimWindowViewModel()
    {
        
    }

    public void Dispose()
    {
        if (ResultWaitable.IsCompleted) return;
        _enterWaitable.SetCanceled();
    }

    [ObservableProperty] private double? _trimStart = null;
    [ObservableProperty] private double? _trimEnd = null;

    [RelayCommand]
    private void Enter()
    {
        _enterWaitable.SetResult((TrimStart, TrimEnd));
    }
}