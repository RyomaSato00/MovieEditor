using System.Collections.ObjectModel;
using System.IO;
using System.Windows;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using MovieEditor.Models;
using MovieEditor.Models.Information;

namespace MovieEditor.ViewModels;

internal partial class MainWindowViewModel : ObservableObject, IDisposable
{
    private readonly DialogHandler _dialogHandler = new();
    private readonly ModelManager _modelManager;


    public MainWindowViewModel()
    {
        _modelManager = new ModelManager
        ((string message) =>
        {
            LogHistory = message;
        });

        MovieInfoList.Add(MovieInfo.GetMovieInfo(@"C:\OriginalProgramFiles\WPF\MovieEditor\SampleVideo\AGDRec_20230901_182924.mp4"));
        MovieInfoList.Add(MovieInfo.GetMovieInfo(@"C:\OriginalProgramFiles\WPF\MovieEditor\SampleVideo\AGDRec_20231202_173923.mp4"));
        MovieInfoList.Add(MovieInfo.GetMovieInfo(@"C:\OriginalProgramFiles\WPF\MovieEditor\SampleVideo\AGDRec_20231128_220111.mp4"));
        MovieInfoList.Add(MovieInfo.GetMovieInfo(@"C:\OriginalProgramFiles\WPF\MovieEditor\SampleVideo\AGDRec_20231115_215814.mp4"));
    }

    public void Dispose()
    {
        _modelManager.Dispose();
    }

    /// <summary>
    /// 複数のファイルパスをもとに動画ファイル情報をMovieInfoListに反映する
    /// </summary>
    /// <param name="filePaths"></param>
    private void GiveSourceFiles(string[] filePaths)
    {
        // ObservableCollectionはAddRangeを使えない
        foreach (var filePath in filePaths)
        {
            // すでに同一のファイルがある場合は追加しない
            if (MovieInfoList.Any(info => info.FilePath == filePath)) continue;

            try
            {
                MovieInfoList.Add(MovieInfo.GetMovieInfo(filePath));
            }
            catch(FileNotFoundException e)
            {
                _modelManager.SendLog(e.Message, LogLevel.Warning);
            }
            catch(ArgumentOutOfRangeException e)
            {
                _modelManager.SendLog(e.Message, LogLevel.Warning);
            }
            catch(Exception e)
            {
                _modelManager.SendLog($"想定外のエラー{e.Message}", LogLevel.Error);
            }
        }
    }

    [ObservableProperty] private ObservableCollection<MovieInfo> _movieInfoList = [];

    [ObservableProperty] private string _outDirectory = string.Empty;

    [ObservableProperty] private string _logHistory = string.Empty;

    [RelayCommand]
    private void ReferSourceFiles()
    {
        string[]? filePaths = _dialogHandler.GetFilesFromDialog();
        if (null == filePaths) return;

        GiveSourceFiles(filePaths);
    }

    [RelayCommand]
    private void ClearSourceFiles()
    {
        MovieInfoList.Clear();
    }

    [RelayCommand]
    private void ReferOutDirectory()
    {
        string? directoryPath = _dialogHandler.GetDirectoryFromDialog();
        if (null != directoryPath)
        {
            OutDirectory = directoryPath;
        }
    }


    [RelayCommand]
    private void Test()
    {
        _modelManager.Test();
    }

    // 以下xamlからBindingできなかったイベントハンドラ等

    /// <summary>
    /// ListViewのDropイベントハンドラ
    /// </summary>
    /// <param name="e"></param>
    public void SourceList_OnDrop(DragEventArgs e)
    {
        var dropFiles = e.Data.GetData(DataFormats.FileDrop) as string[];
        if (null == dropFiles) return;

        GiveSourceFiles(dropFiles);
    }
}