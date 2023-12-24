using System.Collections.ObjectModel;
using System.Diagnostics;
using System.IO;
using System.Reactive.Linq;
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
using Reactive.Bindings;
using Reactive.Bindings.Disposables;
using Reactive.Bindings.Extensions;

namespace MovieEditor.ViewModels;

internal partial class MainWindowViewModel : ObservableObject, IDisposable
{
    private readonly DialogHandler _dialogHandler = new();
    private readonly ModelManager _modelManager;
    /// <summary> Reactive Propertyの後始末用 </summary>
    private readonly CompositeDisposable _disposables = new();

    /// <summary> Runコマンドの実行可否 </summary>
    private readonly ReactiveProperty<bool> _isRunnable = new();

    /// <summary> SourceListの見守り用ReactiveProperty </summary>
    private readonly ReadOnlyReactiveCollection<SourceListItemElement> _reactiveSourceList;

    /// <summary> ReactivePropertyに依存可能なRunコマンド </summary>
    public ReactiveCommand RunCommand { get; }

    public MainWindowViewModel()
    {
        _modelManager = new ModelManager
        ((string message) =>
        {
            LogHistory = message;
        });

        _reactiveSourceList = MovieInfoList.ToReadOnlyReactiveCollection();

        _reactiveSourceList
            // SourceListの各要素の中身のプロパティ(IsChecked)変更時に呼ばれるイベント
            .ObserveElementPropertyChanged()
            .Subscribe(args =>
            {
                var checkCount = MovieInfoList.Where(item => item.IsChecked).Count();
                // すべてチェックなしならば実行不可にする
                _isRunnable.Value = checkCount > 0;
            })
            .AddTo(_disposables);

        _reactiveSourceList
            // SourceListの要素変更時に呼ばれるイベント
            .CollectionChangedAsObservable()
            .Subscribe(args =>
            {
                // SourceListに要素が1つもないときは実行不可にする
                if (false == MovieInfoList.Any()) _isRunnable.Value = false;
                // SourceListに要素が1つ以上あるとき
                else
                {
                    var checkCount = MovieInfoList.Where(item => item.IsChecked).Count();
                    // すべてチェックなしならば実行不可にする
                    _isRunnable.Value = checkCount > 0;
                }
            })
            .AddTo(_disposables);

        // isRunnableがtrueのときのみ実行可能なコマンド
        RunCommand = _isRunnable.ToReactiveCommand();

        // コマンド実行内容設定
        RunCommand.Subscribe(async () =>
        {
            // 処理実行中はコマンド実行不可（ボタンを押せない）にする
            _isRunnable.Value = false;
            var isCanceled = await Run();
            // 処理実行終了のため、実行可能（ボタンを押せる）に戻す
            _isRunnable.Value = true;
            // キャンセルされたならSourceListは変更しない
            if(isCanceled) return;
            // 処理済ファイルをSourceListから消す（isRunnableの変更可能性あり）
            RemoveProcessFinishedFiles();
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
        OutputFormat = _modelManager.SettingReferable.MainSettings_.Format;
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
        _modelManager.SettingReferable.MainSettings_.Format = OutputFormat;

        // ReactivePropertyのSubscribeを解除する
        _disposables.Dispose();
        _modelManager.Dispose();
    }

    /// <summary>
    /// 複数のファイルパスをもとに動画ファイル情報をMovieInfoListに反映する
    /// </summary>
    /// <param name="filePaths"></param>
    private async Task GiveSourceFiles(string[] filePaths)
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
    /// 指定のプロセスを実行する
    /// </summary>
    /// <returns>キャンセルされたならtrue</returns>
    private async Task<bool> Run()
    {
        var isCanceled = true;
        switch ((ProcessModeEnum)ProcessMode)
        {
            case ProcessModeEnum.VideoCompression:
                _modelManager.SendLog("圧縮処理開始");
                using(var viewModel = CreateProgressWindow(_modelManager.ParallelComp))
                {
                    isCanceled = await RunCompression();
                }
                break;

            case ProcessModeEnum.AudioExtraction:
                _modelManager.SendLog("音声抽出処理開始");
                using(var viewModel = CreateProgressWindow(_modelManager.ParallelExtract))
                {
                    isCanceled = await RunExtraction();
                }
                break;

            default:
                return true;
        }
        return isCanceled;
    }

    /// <summary>
    /// 動画圧縮処理を非同期実行する
    /// </summary>
    /// <returns>キャンセルされたならtrue</returns>
    private async Task<bool> RunCompression()
    {
        CompressionParameter parameter = new()
        {
            ScaleWidth = OutputWidth,
            ScaleHeight = OutputHeight,
            FrameRate = OutputFrameRate,
            VideoCodec = OutputCodec,
            IsAudioEraced = IsAudioEraced,
            Format = OutputFormat
        };
        try
        {
            var isCanceled = await _modelManager.ParallelComp.Run
            (
                // チェックを付けたものだけ処理する
                MovieInfoList.Where(item => item.IsChecked).Select(item => item.Info).ToArray(),
                OutDirectory,
                OutputNameTag,
                parameter
            );
            return isCanceled;
        }
        catch (Exception e)
        {
            _modelManager.SendLog($"想定外のエラー：{e}", LogLevel.Error);
            return true;
        }
    }

    /// <summary>
    /// 音声抽出処理を非同期実行する
    /// </summary>
    /// <returns>キャンセルされたならtrue</returns>
    private async Task<bool> RunExtraction()
    {
        try
        {
            var isCanceled = await _modelManager.ParallelExtract.Run
            (
                // チェックを付けたものだけ処理する
                MovieInfoList.Where(item => item.IsChecked).Select(item => item.Info).ToArray(),
                OutDirectory,
                OutputNameTag
            );
            return isCanceled;
        }
        catch (Exception e)
        {
            _modelManager.SendLog($"想定外のエラー：{e}", LogLevel.Error);
            return true;
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

    [ObservableProperty]
    private ObservableCollection<SourceListItemElement> _movieInfoList = [];

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
    [ObservableProperty] private string _outputFormat = string.Empty;

    [ObservableProperty] private string _logHistory = string.Empty;

    [RelayCommand]
    private async Task ReferSourceFiles()
    {
        string[]? filePaths = _dialogHandler.GetFilesFromDialog();
        if (null == filePaths) return;

        // 非同期でファイルを読み込む
        await GiveSourceFiles(filePaths);
        IsAllChecked = true;
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

    [RelayCommand] private void Test()
    {

    }

    // 以下xamlからBindingできなかったイベントハンドラ等

    /// <summary>
    /// ListViewのDropイベントハンドラ
    /// </summary>
    /// <param name="e"></param>
    public async Task SourceList_OnDrop(string[] dropFiles)
    {
        // 非同期でファイルを読み込む
        await GiveSourceFiles(dropFiles);
        IsAllChecked = true;
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
