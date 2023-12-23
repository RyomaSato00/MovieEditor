using System.Collections.ObjectModel;
using System.Diagnostics;
using System.IO;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using MovieEditor.Models;
using MovieEditor.Models.Compression;
using MovieEditor.Models.Information;
using MovieEditor.Models.Json;
using MovieEditor.Views;

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
    private async void GiveSourceFiles(string[] filePaths)
    {
        // ファイル情報格納用
        List<MovieInfo> infos = [];
        // 非同期でファイル情報を取得する
        await Task.Run(() =>
        {
            foreach (var filePath in filePaths)
            {
                // すでに同一のファイルがある場合は追加しない（MovieInfoListへの変更はないため、非同期でも参照可）
                if (MovieInfoList.Any(item => item.Info.FilePath == filePath)) continue;

                try
                {
                    infos.Add(MovieInfo.GetMovieInfo(filePath));
                }
                catch (FileNotFoundException e)
                {
                    _modelManager.SendLogFromAsync(e.Message, LogLevel.Warning);
                }
                catch (ArgumentOutOfRangeException e)
                {
                    _modelManager.SendLogFromAsync(e.Message, LogLevel.Warning);
                }
                catch (FFMpegCore.Exceptions.FFMpegException)
                {
                    _modelManager.SendLogFromAsync($"ファイルが壊れている可能性があります。ファイルを読み込めません：{filePath}", LogLevel.Warning);
                }
                catch (Exception e)
                {
                    _modelManager.SendLogFromAsync($"想定外のエラー：{e.Message}", LogLevel.Error);
                }
            }
        });

        // MovieInfoListへの変更はメインスレッドで行う必要がある
        foreach (var info in infos)
        {
            MovieInfoList.Add(new SourceListItemElement(info));
        }
    }

    /// <summary>
    /// チェックのついた項目をリストから削除する
    /// </summary>
    private void RemoveProcessFinishedFiles()
    {
        // 逆順に削除していくことで、その要素をリストから削除しても、残りの要素のインデックスは変化しない
        for (var index = MovieInfoList.Count - 1; index >= 0; index--)
        {
            // チェックが付いている項目のみ削除する
            if (false == MovieInfoList[index].IsChecked) continue;
            MovieInfoList.RemoveAt(index);
        }
    }

    /// <summary>
    /// 動画圧縮処理を非同期実行する
    /// </summary>
    /// <returns></returns>
    private async Task RunCompression()
    {
        CompressionParameter parameter = new()
        {
            ScaleWidth = OutputWidth,
            ScaleHeight = OutputHeight,
            FrameRate = OutputFrameRate,
            VideoCodec = OutputCodec,
            IsAudioEraced = IsAudioEraced
        };
        try
        {
            await _modelManager.ParallelComp.Run
            (
                // チェックを付けたものだけ処理する
                MovieInfoList.Where(item => item.IsChecked).Select(item => item.Info).ToArray(),
                OutDirectory,
                OutputNameTag,
                parameter
            );
        }
        catch (Exception e)
        {
            _modelManager.SendLog($"想定外のエラー：{e}", LogLevel.Error);
        }
    }

    /// <summary>
    /// 音声抽出処理を非同期実行する
    /// </summary>
    /// <returns></returns>
    private async Task RunExtraction()
    {
        try
        {
            await _modelManager.ParallelExtract.Run
            (
                // チェックを付けたものだけ処理する
                MovieInfoList.Where(item => item.IsChecked).Select(item => item.Info).ToArray(),
                OutDirectory,
                OutputNameTag
            );
        }
        catch (Exception e)
        {
            _modelManager.SendLog($"想定外のエラー：{e}", LogLevel.Error);
        }
    }

    /// <summary>
    /// 進捗表示用のサブウィンドウを表示する
    /// </summary>
    /// <param name="process"></param>
    /// <returns></returns>
    private static ProgressWindowViewModel CreateProgressWindow(IAnyProcess process)
    {
        var progressWindow = new ProgressWindow();
        var progressWindowViewModel = new ProgressWindowViewModel(process, progressWindow.Close);
        progressWindow.DataContext = progressWindowViewModel;
        progressWindow.Show();
        return progressWindowViewModel;
    }

    [NotifyCanExecuteChangedFor(nameof(RunCommand))]
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

    [RelayCommand(CanExecute = nameof(CanRun))]
    private async Task Run()
    {
        ProgressWindowViewModel? viewModel = null;
        switch ((ProcessModeEnum)ProcessMode)
        {
            case ProcessModeEnum.VideoCompression:
                _modelManager.SendLog("圧縮処理開始");
                viewModel = CreateProgressWindow(_modelManager.ParallelComp);
                await RunCompression();
                break;

            case ProcessModeEnum.AudioExtraction:
                _modelManager.SendLog("音声抽出処理開始");
                viewModel = CreateProgressWindow(_modelManager.ParallelExtract);
                await RunExtraction();
                break;

            default:
                break;
        }
        viewModel?.Dispose();
        RemoveProcessFinishedFiles();
    }

    private bool CanRun()
    {
        // MovieInfoListが空でなければOK
        return MovieInfoList.Any();
    }


    public void Test()
    {
        _modelManager.Test();
    }

    [RelayCommand]
    private async Task Test2()
    {
        _modelManager.Debug("音声抽出");
        await RunExtraction();
        _modelManager.Debug("音声抽出完了");
        // foreach (var item in MovieInfoList)
        // {
        //     _modelManager.Debug(item.IsChecked.ToString());
        // }

        // _modelManager.ParallelComp.Cancel();
        // _modelManager.Debug("キャンセル");

        // _modelManager.Debug("新しいウィンドウ");
        // var progressWindowViewModel = new ProgressWindowViewModel(_modelManager);
        // var win = new ProgressWindow()
        // {
        //     DataContext = progressWindowViewModel
        // };
        // win.ShowDialog();
    }

    // 以下xamlからBindingできなかったイベントハンドラ等

    /// <summary>
    /// ListViewのDropイベントハンドラ
    /// </summary>
    /// <param name="e"></param>
    public void SourceList_OnDrop(string[] dropFiles)
    {
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
        catch (Exception exception)
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


    public void OutDirectory_OnDrop(string directoryPath)
    {
        if (false == Directory.Exists(directoryPath)) return;
        OutDirectory = directoryPath;
    }
}

internal partial class SourceListItemElement(MovieInfo movieInfo) : ObservableObject
{
    [ObservableProperty] private bool _isChecked = true;
    public MovieInfo Info { get; init; } = movieInfo;
}
