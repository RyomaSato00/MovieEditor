using System.Collections.ObjectModel;
using System.Diagnostics;
using System.IO;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using MovieEditor.Models;
using MovieEditor.Models.Information;
using MovieEditor.Models.Json;

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

        // 設定値反映
        OutDirectory = _modelManager.SettingReferable.MainSettings_.OutputFolder;
        OutputNameTag = _modelManager.SettingReferable.MainSettings_.AttachedNameTag;
        ProcessMode = (int)_modelManager.SettingReferable.MainSettings_.ProcessMode;
        OutputWidth = _modelManager.SettingReferable.MainSettings_.ScaleWidth;
        OutputHeight = _modelManager.SettingReferable.MainSettings_.ScaleHeight;
        OutputFrameRate = _modelManager.SettingReferable.MainSettings_.FrameRate;
        OutputCodec = _modelManager.SettingReferable.MainSettings_.Codec;
        IsAudioEraced = _modelManager.SettingReferable.MainSettings_.IsAudioEraced;

        MovieInfoList.Add(new SourceListItemElement(MovieInfo.GetMovieInfo(@"C:\OriginalProgramFiles\WPF\MovieEditor\SampleVideo\AGDRec_20230901_182924.mp4")));
        MovieInfoList.Add(new SourceListItemElement(MovieInfo.GetMovieInfo(@"C:\OriginalProgramFiles\WPF\MovieEditor\SampleVideo\AGDRec_20231202_173923.mp4")));
        MovieInfoList.Add(new SourceListItemElement(MovieInfo.GetMovieInfo(@"C:\OriginalProgramFiles\WPF\MovieEditor\SampleVideo\AGDRec_20231128_220111.mp4")));
        MovieInfoList.Add(new SourceListItemElement(MovieInfo.GetMovieInfo(@"C:\OriginalProgramFiles\WPF\MovieEditor\SampleVideo\AGDRec_20231115_215814.mp4")));
    }

    public void Dispose()
    {
        // 設定値保存
        _modelManager.SettingReferable.MainSettings_.OutputFolder = OutDirectory;
        _modelManager.SettingReferable.MainSettings_.AttachedNameTag = OutputNameTag;
        _modelManager.SettingReferable.MainSettings_.ProcessMode = (ProcessModeEnum)ProcessMode;
        _modelManager.SettingReferable.MainSettings_.ScaleWidth = OutputWidth;
        _modelManager.SettingReferable.MainSettings_.ScaleHeight = OutputHeight;
        _modelManager.SettingReferable.MainSettings_.FrameRate = OutputFrameRate;
        _modelManager.SettingReferable.MainSettings_.Codec = OutputCodec;
        _modelManager.SettingReferable.MainSettings_.IsAudioEraced = IsAudioEraced;

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
            if (MovieInfoList.Any(item => item.Info.FilePath == filePath)) continue;

            try
            {
                MovieInfoList.Add(new SourceListItemElement(MovieInfo.GetMovieInfo(filePath)));
            }
            catch (FileNotFoundException e)
            {
                _modelManager.SendLog(e.Message, LogLevel.Warning);
            }
            catch (ArgumentOutOfRangeException e)
            {
                _modelManager.SendLog(e.Message, LogLevel.Warning);
            }
            catch (Exception e)
            {
                _modelManager.SendLog($"想定外のエラー{e.Message}", LogLevel.Error);
            }
        }
    }

    [ObservableProperty] private ObservableCollection<SourceListItemElement> _movieInfoList = [];

    [ObservableProperty] private bool _isAllChecked = true;

    // ***出力先***
    [ObservableProperty] private string _outDirectory = string.Empty;

    [ObservableProperty] private string _outputNameTag = string.Empty;

    // ***処理モード***
    [ObservableProperty] private int _processMode = 0;

    // ***出力設定値***
    [ObservableProperty] private int _outputWidth = 0;

    [ObservableProperty] private int _outputHeight = 0;

    [ObservableProperty] private double _outputFrameRate = 0;

    [ObservableProperty] private string _outputCodec = string.Empty;

    [ObservableProperty] private bool _isAudioEraced = false;

    [ObservableProperty] private string _logHistory = string.Empty;

    [RelayCommand]
    private void ReferSourceFiles()
    {
        string[]? filePaths = _dialogHandler.GetFilesFromDialog();
        if (null == filePaths) return;

        IsAllChecked = true;
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


    public void Test()
    {
        _modelManager.Test();
    }

    [RelayCommand]
    private void Test2()
    {
        foreach (var item in MovieInfoList)
        {
            _modelManager.Debug(item.IsChecked.ToString());
        }
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

        IsAllChecked = true;
        GiveSourceFiles(dropFiles);
    }

    /// <summary>
    /// ListViewの項目をダブルクリックしたときのイベントハンドラ
    /// </summary>
    public void SourceList_OnItemDoubleClicked(SourceListItemElement item)
    {
        _modelManager.Debug($"{item.Info.FileName}を開きます");
        try
        {
            Process.Start(new ProcessStartInfo(item.Info.FilePath) { UseShellExecute = true });
        }
        catch(Exception exception)
        {
            _modelManager.SendLog(exception.Message, LogLevel.Error);
        }
    }

    /// <summary>
    /// ListViewのヘッダ0（チェックボックス）のチェックが変更されたときのイベントハンドラ
    /// </summary>
    /// <param name="isChecked"></param>
    public void SourceListHeader0_OnChecked(bool isChecked)
    {
        // すべての項目をcheckedまたはuncheckedする
        foreach (var item in MovieInfoList)
        {
            item.IsChecked = isChecked;
        }
    }
}

internal partial class SourceListItemElement(MovieInfo movieInfo) : ObservableObject
{
    [ObservableProperty] private bool _isChecked = true;
    public MovieInfo Info { get; init; } = movieInfo;
}
