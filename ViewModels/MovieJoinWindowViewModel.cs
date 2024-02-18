using CommunityToolkit.Mvvm.ComponentModel;
using System.Collections.ObjectModel;
using Reactive.Bindings;
using CommunityToolkit.Mvvm.Input;
using MovieEditor.Models.Information;
using MovieEditor.Models.Join;
using MovieEditor.Views;

namespace MovieEditor.ViewModels;

internal partial class MovieJoinWindowViewModel : ObservableObject, IDisposable
{
    private readonly TaskCompletionSource<(string, MovieInfo[])> _joinWaitable = new();
    public Task<(string, MovieInfo[])> JoinWaitable => _joinWaitable.Task;
    [ObservableProperty] private ObservableCollection<SourceListItemElement> _movieInfoList = [];
    [ObservableProperty] private bool _isAllChecked = true;
    [ObservableProperty] private bool _isThumbnailVisible = false;
    [ObservableProperty] private int _selectedIndex = -1;
    private TimeTrimWindow? _timeTrimWindow = null;

    /// <summary>
    /// 「削除」ボタンを押したときにその項目をリストから削除する
    /// </summary>
    /// <param name="filePath"></param>
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

    [RelayCommand]
    private void Up(int index)
    {
        // 何も選択していないときは-1が返ってくる。その場合何もしない
        // また、インデックスが0のときはこれ以上上に移動できないため何もしない
        if (0 >= index) return;

        // 要素を上にひとつ入れ替える
        (MovieInfoList[index - 1], MovieInfoList[index]) = (MovieInfoList[index], MovieInfoList[index - 1]);
        // 上に移動させた要素を選択状態にして終える
        SelectedIndex = index - 1;
    }

    [RelayCommand]
    private void Down(int index)
    {
        // 何も選択していないときは-1が返ってくる。その場合何もしない
        // また、インデックスが最後尾のときはこれ以上下に移動できないため何もしない
        if (0 > index || MovieInfoList.Count - 1 <= index) return;

        // 要素を上にひとつ入れ替える
        (MovieInfoList[index + 1], MovieInfoList[index]) = (MovieInfoList[index], MovieInfoList[index + 1]);
        // 下に移動させた要素を選択状態にして終える
        SelectedIndex = index + 1;
    }

    [RelayCommand]
    private async Task Decide()
    {
        System.Diagnostics.Debug.WriteLine("決定");

        using var joinProcess = new VideoJoiner();
        var argumentFiles = MovieInfoList
            .Where(item => item.IsChecked)
            .Select(item => item.Info)
            .ToArray();

        var joinedVideo = await joinProcess.Join(argumentFiles);
        _joinWaitable.SetResult((joinedVideo, argumentFiles));
    }

    [RelayCommand]
    private async Task TrimByTime(string filePath)
    {
        var (window, viewModel) = SubWindowCreator.CreateTimeTrimWindow();
        _timeTrimWindow = window;
        try
        {
            var (trimStart, trimEnd) = await viewModel.ResultWaitable;
            window.Close();
            System.Diagnostics.Debug.WriteLine($"start:{trimStart}, end:{trimEnd}");

            foreach (var item in MovieInfoList)
            {
                if (filePath != item.Info.FilePath) continue;

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

    /// <summary>
    /// SelectedIndexのアイテムをindexへ挿入する
    /// </summary>
    /// <param name="index"></param>
    [RelayCommand]
    private void ReplaceItem(int index)
    {
        if (index >= 0)
        {
            MovieInfoList.Move(SelectedIndex, index);
        }
    }

    /// <summary>
    /// リストに項目を追加する
    /// </summary>
    /// <param name="movies"></param>
    public void AddMovies(SourceListItemElement[] movies)
    {
        foreach (var movie in movies)
        {
            MovieInfoList.Add(movie);
        }
    }

    public void Dispose()
    {
        // 動画結合が完了せずにdisposeされたら、キャンセル通知を送る
        if (false == JoinWaitable.IsCompleted)
        {
            _joinWaitable.SetCanceled();
        }

        // 時間指定ウィンドウが開いたままならば、消す
        _timeTrimWindow?.Close();
    }


}