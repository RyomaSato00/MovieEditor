using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using MovieEditor.Models;

namespace MovieEditor.ViewModels;

internal partial class MainWindowViewModel : ObservableObject
{
    private readonly DialogHandler _dialogHandler = new();
    private readonly ModelManager _modelManager;


    public MainWindowViewModel()
    {
        _modelManager = new ModelManager();
        _modelManager.LogServer.OnSendLog += (string message) =>
        {
            LogHistory = message;
        };
    }

    [ObservableProperty] private string _logHistory = string.Empty;

    [ObservableProperty] private string _outDirectory = string.Empty;

    [RelayCommand] private void ReferOutDirectory()
    {
        string? directoryPath = _dialogHandler.GetDirectoryFromDialog();
        if(null != directoryPath)
        {
            OutDirectory = directoryPath;
        }
    }

    [RelayCommand]
    private void Test()
    {
        _modelManager.Test();
    }
}