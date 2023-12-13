using CommunityToolkit.Mvvm.ComponentModel;
using MovieEditor.Models;

namespace MovieEditor.ViewModels;

internal partial class MainWindowViewModel : ObservableObject
{
    private readonly ModelManager _modelManager;

    [ObservableProperty] private string _logHistory = string.Empty;

    public MainWindowViewModel()
    {
        _modelManager = new ModelManager();
        _modelManager.LogServer.OnSendLog += (string message) =>
        {
            LogHistory = message;
        };

        _modelManager.Test();
    }
}