using System.Collections.ObjectModel;
using System.Diagnostics;
using System.IO;
using System.Reactive.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media.Imaging;
using System.Windows.Shell;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using MovieEditor.Models;
using MovieEditor.Models.Compression;
using MovieEditor.Models.Information;
using MovieEditor.Views;
using MyCommonFunctions;
using Reactive.Bindings;
using Reactive.Bindings.Disposables;
using Reactive.Bindings.Extensions;

namespace MovieEditor.ViewModels;

internal partial class MainWindowViewModel : ObservableObject, IDisposable
{
    private static readonly object _parallelLock = new();
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

    [ObservableProperty]
    private ObservableCollection<SourceListItemElement> _movieInfoList = [];

    [ObservableProperty] private bool _isAllChecked = true;

    // ***設定値***
    [ObservableProperty] private MainSettings _mainSettings;
    // 出力フォルダだけはダイアログボックスの取得やドラッグドロップによる取得に対応させるため、
    // 個別にバインドを用意する
    [ObservableProperty] private string _outputDirectory;

    [ObservableProperty] private string _logHistory = string.Empty;

    public MainWindowViewModel()
    {
        MyConsole.OnWrite += messages => LogHistory = messages;
        // 設定値反映
        MainSettings = SettingManager.LoadSetting();
        OutputDirectory = MainSettings.OutputFolder;
        MyConsole.UseDebugLog = MainSettings.UseDebugLog;
        _modelManager = new ModelManager();
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
            // チェック付きのファイルリストを取得する
            var sources = MovieInfoList.Where(item => item.IsChecked).Select(item => item.Info).ToArray();
            var processedFiles = await Run(sources);
            // 処理実行終了のため、実行可能（ボタンを押せる）に戻す
            _isRunnable.Value = true;
            // 処理済ファイルをSourceListから消す（isRunnableの変更可能性あり）
            RemoveProcessFinishedFiles(processedFiles);
        });

    }

    public void Dispose()
    {
        _disposables.Dispose();
        _modelManager.Dispose();
        // 設定値保存
        MainSettings.OutputFolder = OutputDirectory;
        SettingManager.SaveSetting(MainSettings);
    }

    /// <summary>
    /// 複数のファイルパスをもとに動画ファイル情報をMovieInfoListに反映する
    /// </summary>
    /// <param name="filePaths"></param>
    private async Task GiveSourceFiles(string[] filePaths)
    {
        // ファイル情報格納用
        List<(MovieInfo info, Uri thumbnailUri)> items = [];
        // 非同期でファイル情報を取得する
        await Task.Run(() =>
        {
            foreach (var filePath in filePaths)
            {
                // すでに同一のファイルがある場合は追加しない（MovieInfoListへの変更はないため、非同期でも参照可）
                if (MovieInfoList.Any(item => item.Info.FilePath == filePath)) continue;

                try
                {
                    var info = MovieInfo.GetMovieInfo(filePath);
                    var thumbnailUri = MovieInfo.GetThumbnailUri(filePath);
                    items.Add((info, thumbnailUri));
                }
                catch (FileNotFoundException e)
                {
                    lock (_parallelLock)
                    {
                        MyConsole.WriteLine(e.Message, MyConsole.Level.Warning);
                    }
                }
                catch (ArgumentOutOfRangeException e)
                {
                    lock(_parallelLock)
                    {
                        MyConsole.WriteLine(e.Message, MyConsole.Level.Warning);
                    }
                }
                catch (FFMpegCore.Exceptions.FFMpegException)
                {
                    lock(_parallelLock)
                    {
                        MyConsole.WriteLine($"ファイルが壊れている可能性があります。ファイルを読み込めません：{filePath}", MyConsole.Level.Warning);
                    }
                }
                catch (Exception e)
                {
                    lock(_parallelLock)
                    {
                        MyConsole.WriteLine($"想定外のエラー：{e.Message}", MyConsole.Level.Error);
                    }
                }
            }
        });

        // MovieInfoListへの変更はメインスレッドで行う必要がある
        foreach (var (info, thumbnailUri) in items)
        {
            MovieInfoList.Add(new SourceListItemElement(info, thumbnailUri));
        }
    }

    /// <summary>
    /// チェックのついた項目をリストから削除する
    /// </summary>
    private void RemoveProcessFinishedFiles(MovieInfo[] sources)
    {
        // 逆順に削除していくことで、その要素をリストから削除しても、残りの要素のインデックスは変化しない
        for (var index = MovieInfoList.Count - 1; index >= 0; index--)
        {
            // 処理済のファイルのみリストから削除する
            if (false == sources.Contains(MovieInfoList[index].Info)) continue;
            MovieInfoList.RemoveAt(index);
        }
    }

    /// <summary>
    /// 指定のプロセスを実行する
    /// </summary>
    /// <returns>処理済みファイル配列</returns>
    private async Task<MovieInfo[]> Run(MovieInfo[] sources)
    {
        var processedFiles = Array.Empty<MovieInfo>();
        switch ((ProcessModeEnum)MainSettings.ProcessMode)
        {
            case ProcessModeEnum.VideoCompression:
                MyConsole.WriteLine("圧縮処理開始", MyConsole.Level.Info);
                using (var viewModel = CreateProgressWindow(_modelManager.ParallelComp))
                {
                    processedFiles = await RunCompression(sources);
                }
                break;

            case ProcessModeEnum.AudioExtraction:
                MyConsole.WriteLine("音声抽出処理開始", MyConsole.Level.Info);
                using (var viewModel = CreateProgressWindow(_modelManager.ParallelExtract))
                {
                    processedFiles = await RunExtraction(sources);
                }
                break;

            case ProcessModeEnum.SpeedChange:
                MyConsole.WriteLine("再生速度変更開始", MyConsole.Level.Info);
                using (var viewModel = CreateProgressWindow(_modelManager.ParallelSpeedChange))
                {
                    processedFiles = await RunSpeedChange(sources);
                }
                break;

            default:
                break;
        }
        return processedFiles;
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

    private static (TimeTrimWindow, TimeTrimWindowViewModel) CreateTimeTrimWindow()
    {
        var timeTrimWindow = new TimeTrimWindow();
        var timeTrimWindowViewModel = new TimeTrimWindowViewModel();
        timeTrimWindow.DataContext = timeTrimWindowViewModel;
        timeTrimWindow.Closing += (_, _) => timeTrimWindowViewModel.Dispose();
        timeTrimWindow.Show();
        return (timeTrimWindow, timeTrimWindowViewModel);
    }

    /// <summary>
    /// 動画圧縮処理を非同期実行する
    /// </summary>
    /// <returns>処理済みファイル配列</returns>
    private async Task<MovieInfo[]> RunCompression(MovieInfo[] sources)
    {
        CompressionParameter parameter = new()
        {
            ScaleWidth = MainSettings.Comp.ScaleWidth,
            ScaleHeight = MainSettings.Comp.ScaleHeight,
            FrameRate = MainSettings.Comp.FrameRate,
            VideoCodec = MainSettings.Comp.Codec,
            IsAudioEraced = MainSettings.Comp.IsAudioEraced,
            Format = MainSettings.Comp.Format
        };
        try
        {
            var processedFiles = await _modelManager.ParallelComp.Run
            (
                sources,
                OutputDirectory,
                MainSettings.AttachedNameTag,
                parameter
            );
            return processedFiles;
        }
        catch (Exception e)
        {
            MyConsole.WriteLine($"想定外のエラー：{e}", MyConsole.Level.Error);
            // 空配列を返す
            return Array.Empty<MovieInfo>();
        }
    }

    /// <summary>
    /// 音声抽出処理を非同期実行する
    /// </summary>
    /// <returns>処理済みファイル配列</returns>
    private async Task<MovieInfo[]> RunExtraction(MovieInfo[] sources)
    {
        try
        {
            var processedFiles = await _modelManager.ParallelExtract.Run
            (
                sources,
                OutputDirectory,
                MainSettings.AttachedNameTag
            );
            return processedFiles;
        }
        catch (Exception e)
        {
            MyConsole.WriteLine($"想定外のエラー：{e}", MyConsole.Level.Error);
            return Array.Empty<MovieInfo>();
        }
    }

    /// <summary>
    /// 再生速度変更処理を非同期実行する
    /// </summary>
    /// <returns>処理済みファイル配列</returns>
    private async Task<MovieInfo[]> RunSpeedChange(MovieInfo[] sources)
    {
        try
        {
            var processedFiles = await _modelManager.ParallelSpeedChange.Run
            (
                sources,
                OutputDirectory,
                MainSettings.AttachedNameTag,
                MainSettings.Speed.SpeedRate
            );
            return processedFiles;
        }
        catch (Exception e)
        {
            MyConsole.WriteLine($"想定外のエラー：{e}", MyConsole.Level.Error);
            return Array.Empty<MovieInfo>();
        }
    }

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
            OutputDirectory = directoryPath;
        }
    }

    [RelayCommand]
    private void RemoveItem(string filePath)
    {
        for (var index = 0; index < MovieInfoList.Count; index++)
        {
            if (filePath != MovieInfoList[index].Info.FilePath) continue;
            // 指定のファイルパスの項目を削除する
            MovieInfoList.RemoveAt(index);
            return;
        }
    }

    /// <summary>
    /// ListViewの項目で右クリックメニュー「時間範囲指定」をクリックしたときのイベントハンドラ
    /// </summary>
    /// <param name="info"></param>
    [RelayCommand]
    private async Task TrimByTime(string filePath)
    {
        var (window, viewModel) = CreateTimeTrimWindow();
        try
        {
            var (trimStart, trimEnd) = await viewModel.ResultWaitable;
            window.Close();
            System.Diagnostics.Debug.WriteLine($"start:{trimStart}, end:{trimEnd}");

            foreach (var item in MovieInfoList)
            {
                if (filePath != item.Info.FilePath) continue;

                if (trimStart is not null)
                {
                    item.Info.TrimStart = TimeSpan.FromSeconds(trimStart.Value);
                }
                if (trimEnd is not null)
                {
                    item.Info.TrimEnd = TimeSpan.FromSeconds(trimEnd.Value);
                }
                item.UpdateTrimPeriod();
            }
        }
        catch (TaskCanceledException)
        {

        }
        System.Diagnostics.Debug.WriteLine("時間範囲指定終了");
    }

    [RelayCommand]
    private void Test()
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
        try
        {
            Process.Start(new ProcessStartInfo(item.Info.FilePath) { UseShellExecute = true });
        }
        catch (Exception exception)
        {
            MyConsole.WriteLine(exception.Message, MyConsole.Level.Error);
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
        OutputDirectory = directoryPath;
    }
}

internal partial class SourceListItemElement(MovieInfo movieInfo, Uri thumbnailUri) : ObservableObject
{
    [ObservableProperty] private bool _isChecked = true;
    public BitmapImage Thumbnail { get; init; } = new BitmapImage(thumbnailUri);
    public MovieInfo Info { get; init; } = movieInfo;
    [ObservableProperty] private string _trimPeriod = $"{TimeSpan.Zero:mm\\:ss\\.ff}-{movieInfo.Duration:mm\\:ss\\.ff}";

    public void UpdateTrimPeriod()
    {
        var start = Info.TrimStart ?? TimeSpan.Zero;
        var end = Info.TrimEnd ?? Info.Duration;
        TrimPeriod = $"{start:mm\\:ss\\.ff}-{end:mm\\:ss\\.ff}";
    }
}
