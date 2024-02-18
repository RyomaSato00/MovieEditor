using MovieEditor.Views;
using MovieEditor.Models;
using System.Windows;

namespace MovieEditor.ViewModels;

internal static class SubWindowCreator
{
    /// <summary>
    /// 進捗表示用のサブウィンドウを表示する
    /// </summary>
    /// <param name="process"></param>
    /// <returns></returns>
    public static WindowDisposable CreateProgressWindow(IAnyProcess process)
    {
        var progressWindow = new ProgressWindow();
        var progressWindowViewModel = new ProgressWindowViewModel(process);
        progressWindow.DataContext = progressWindowViewModel;
        // ウィンドウを消したときもDisposeをする
        progressWindow.Closing += (_, _) => progressWindowViewModel.Dispose();
        progressWindow.Show();
        return new WindowDisposable(progressWindow);
    }

    /// <summary>
    /// 時間範囲指定用のサブウィンドウを表示する
    /// </summary>
    /// <param name="CreateTimeTrimWindow("></param>
    /// <returns></returns>
    public static (TimeTrimWindow, TimeTrimWindowViewModel) CreateTimeTrimWindow()
    {
        var timeTrimWindow = new TimeTrimWindow();
        var timeTrimWindowViewModel = new TimeTrimWindowViewModel();
        timeTrimWindow.DataContext = timeTrimWindowViewModel;
        timeTrimWindow.Closing += (_, _) => timeTrimWindowViewModel.Dispose();
        timeTrimWindow.Show();
        return (timeTrimWindow, timeTrimWindowViewModel);
    }

    /// <summary>
    /// 動画結合用のサブウィンドウを表示する
    /// </summary>
    /// <param name="CreateMovieJoinWindow("></param>
    /// <returns></returns>
    public static (MovieJoinWindow, MovieJoinWindowViewModel) CreateMovieJoinWindow()
    {
        var movieJoinWindow = new MovieJoinWindow();
        var movieJoinWindowViewModel = new MovieJoinWindowViewModel();
        movieJoinWindow.DataContext = movieJoinWindowViewModel;
        movieJoinWindow.Closing += (_, _) => movieJoinWindowViewModel.Dispose();
        movieJoinWindow.Show();
        return (movieJoinWindow, movieJoinWindowViewModel);
    }
}

/// <summary>
/// サブウィンドウ破棄用クラス
/// </summary>
internal class WindowDisposable(Window window) : IDisposable
{
    private readonly Window _window = window;

    public void Dispose()
    {
        _window.Close();
    }
}
