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
using MovieEditor.Models.ImageGenerate;
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

            if (MainSettings.OpenExplorer)
            {
                // 出力先ディレクトリをエクスプローラで開く
                await OpenExplorer(OutputDirectory);
            }
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
                    lock (_parallelLock)
                    {
                        MyConsole.WriteLine(e.Message, MyConsole.Level.Warning);
                    }
                }
                catch (FFMpegCore.Exceptions.FFMpegException)
                {
                    lock (_parallelLock)
                    {
                        MyConsole.WriteLine($"ファイルが壊れている可能性があります。ファイルを読み込めません：{filePath}", MyConsole.Level.Warning);
                    }
                }
                catch (Exception e)
                {
                    lock (_parallelLock)
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
                using (var disposable = SubWindowCreator.CreateProgressWindow(_modelManager.ParallelComp))
                {
                    processedFiles = await RunCompression(sources);
                }
                break;

            case ProcessModeEnum.AudioExtraction:
                MyConsole.WriteLine("音声抽出処理開始", MyConsole.Level.Info);
                using (var disposable = SubWindowCreator.CreateProgressWindow(_modelManager.ParallelExtract))
                {
                    processedFiles = await RunExtraction(sources);
                }
                break;

            case ProcessModeEnum.SpeedChange:
                MyConsole.WriteLine("再生速度変更開始", MyConsole.Level.Info);
                using (var disposable = SubWindowCreator.CreateProgressWindow(_modelManager.ParallelSpeedChange))
                {
                    processedFiles = await RunSpeedChange(sources);
                }
                break;

            case ProcessModeEnum.ImageGenerate:
                MyConsole.WriteLine("画像出力処理開始", MyConsole.Level.Info);
                using (var disposable = SubWindowCreator.CreateProgressWindow(_modelManager.ParallelImageGenerate))
                {
                    processedFiles = await RunImageGenerate(sources);
                }
                break;

            default:
                break;
        }
        return processedFiles;
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

    /// <summary>
    /// 画像出力処理を非同期実行する
    /// </summary>
    /// <param name="sources"></param>
    /// <returns>処理済みファイル配列</returns>
    private async Task<MovieInfo[]> RunImageGenerate(MovieInfo[] sources)
    {
        var parameter = new ImageGenerateParameter()
        {
            Format = MainSettings.ImgGenerate.Format,
            FramePerOneSecond = MainSettings.ImgGenerate.FramePerOneSecond,
            FrameSum = MainSettings.ImgGenerate.FrameSum,
            Quality = MainSettings.ImgGenerate.Quality
        };
        try
        {
            return await _modelManager.ParallelImageGenerate.Run(
                sources, OutputDirectory, parameter
            );
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
    private async Task TrimByTime(MovieInfo info)
    {
        var (window, viewModel) = SubWindowCreator.CreateTimeTrimWindow(info.FilePath);
        try
        {
            var (trimStart, trimEnd) = await viewModel.ResultWaitable;
            window.Close();
            System.Diagnostics.Debug.WriteLine($"start:{trimStart}, end:{trimEnd}");

            foreach (var item in MovieInfoList)
            {
                if (info.FilePath != item.Info.FilePath) continue;

                item.Info.TrimStart = trimStart;
                item.Info.TrimEnd = trimEnd;
                item.UpdateTrimPeriod();
            }
        }
        catch (TaskCanceledException)
        {

        }
        System.Diagnostics.Debug.WriteLine("時間範囲指定終了");
    }

    [RelayCommand]
    private async Task JoinMovies()
    {
        var (window, viewModel) = SubWindowCreator.CreateMovieJoinWindow();
        var targets = MovieInfoList.Where(item => item.IsChecked).Select(item => item.Clone()).ToArray();
        viewModel.AddMovies(targets);
        viewModel.IsThumbnailVisible = MainSettings.IsThumbnailVisible;
        try
        {
            var (joinedVideo, usedFiles) = await viewModel.JoinWaitable;
            window.Close();

            // 結合に使用したファイルはリストから削除する
            RemoveProcessFinishedFiles(usedFiles);
            // 結合後のファイルをリストに追加する
            await GiveSourceFiles([joinedVideo]);
        }
        catch (TaskCanceledException)
        {

        }
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

    /// <summary>
    /// 指定パスをエクスプローラで開く
    /// </summary>
    /// <param name="path"></param>
    /// <returns></returns>
    private static async Task OpenExplorer(string path)
    {
        var info = new ProcessStartInfo("EXPLORER.EXE")
        {
            Arguments = path,
            UseShellExecute = false
        };
        using var process = new Process() { StartInfo = info };
        // UIをフリーズさせないために、非同期でエクスプローラが起動するのを待つ
        await Task.Run(() =>
        {
            process.Start();
            process.WaitForExit();
        });
    }
}

internal partial class SourceListItemElement(MovieInfo movieInfo, Uri thumbnailUri) : ObservableObject
{
    [ObservableProperty] private bool _isChecked = true;
    public Uri ThumbnailUri { get; init; } = thumbnailUri;
    public BitmapImage Thumbnail { get; init; } = new BitmapImage(thumbnailUri);
    public MovieInfo Info { get; init; } = movieInfo;
    [ObservableProperty] private string _trimPeriod = $"{TimeSpan.Zero:mm\\:ss\\.ff}-E";

    public void UpdateTrimPeriod()
    {
        var start = Info.TrimStart ?? TimeSpan.Zero;
        var end = Info.TrimEnd;
        // 時間範囲終了時刻を指定していないときは、終了時刻にEと表示する
        string endToken = "E";
        if (end is TimeSpan endTime) endToken = endTime.ToString(@"mm\:ss\.ff");
        TrimPeriod = $"{start:mm\\:ss\\.ff}-{endToken}";
    }

    /// <summary>
    /// このオブジェクトのコピーを返す
    /// movie infoとthumbnail uriはソースと同じ参照を持つ
    /// </summary>
    /// <param name="source"></param>
    /// <returns></returns>
    public SourceListItemElement Clone()
    {
        return new SourceListItemElement(Info, ThumbnailUri);
    }
}
