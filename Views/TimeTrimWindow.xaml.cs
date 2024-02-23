using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Animation;
using System.Windows.Threading;
using MovieEditor.ViewModels;


namespace MovieEditor.Views;


public partial class TimeTrimWindow : Window
{
    private readonly DispatcherTimer _dispatcherTimer = new();
    private readonly Storyboard _timelineStory;

    private double _volume;
    private bool _requestReset = false;
    
    public TimeTrimWindow()
    {
        InitializeComponent();
        _timelineStory = (Storyboard)FindResource("TimelineStory");

        // スライダーを操作中に0.1秒ごとに行う処理
        _dispatcherTimer.Interval = TimeSpan.FromMilliseconds(100);
        _dispatcherTimer.Tick += (_, _) =>
        {
            // スライダーの位置と動画の再生位置を同期させる
            _timelineStory.Seek(TimeSpan.FromMilliseconds(TimeSlider.Value));
        };
    }

    /// <summary>
    /// UIが表示されたとき最初に行われる処理
    /// </summary>
    /// <param name="sender"></param>
    /// <param name="e"></param>
    private void MoviePlayer_Loaded(object sender, RoutedEventArgs e)
    {
        // サムネイル読み込み
        _timelineStory.Begin();
        _timelineStory.Pause();
    }

    /// <summary>
    /// 動画を再生(_timelineStory.Begin())させたとき最初に行われる処理
    /// </summary>
    /// <param name="sender"></param>
    /// <param name="e"></param>
    private void MoviePlayer_MediaOpened(object sender, EventArgs e)
    {
        // スライダーの最大値を動画の総再生時間に設定
        TimeSlider.Maximum = MoviePlayer.NaturalDuration.TimeSpan.TotalMilliseconds;
        // 動画総再生時間表示
        MaxTime.Content = $"{MoviePlayer.NaturalDuration.TimeSpan:mm\\:ss\\.fff}";
    }

    /// <summary>
    /// 動画がすべて再生し終わったときの処理
    /// </summary>
    /// <param name="sender"></param>
    /// <param name="e"></param>
    private void MoviePlayer_MediaEnded(object? sender, EventArgs e)
    {
        // 動画再生位置リセットフラグ
        _requestReset = true;
        // トグルボタンを一時停止状態に切り替える
        PlayToggle.IsChecked = false;
        PlayToPause();
    }

    /// <summary>
    /// 動画再生中に都度呼ばれる処理
    /// </summary>
    /// <param name="sender"></param>
    /// <param name="e"></param>
    private void MediaTimeline_CurrentTimeInvalidated(object sender, EventArgs e)
    {
        // 動画の再生位置→スライダーの位置更新
        TimeSlider.Value = MoviePlayer.Position.TotalMilliseconds;
        // 動画再生時間更新
        CurrentTime.Content = $"{MoviePlayer.Position:mm\\:ss\\.fff}";
    }

    /// <summary>
    /// スライダーをドラッグ開始したときのイベントハンドラ
    /// </summary>
    /// <param name="sender"></param>
    /// <param name="e"></param>
    private void TimeSlider_DragStarted(object sender, DragStartedEventArgs e)
    {
        // 動画のボリュームを記憶
        _volume = MoviePlayer.Volume;
        // 動画のボリュームを0にする
        MoviePlayer.Volume = 0;
        // 動画再生中のとき
        if(true == PlayToggle.IsChecked)
        {
            // 動画を一時停止状態にする
            _timelineStory.Pause();
        }
        // スライダーの位置→動画の再生位置更新処理
        _dispatcherTimer.Start();
    }

    /// <summary>
    /// スライダーのドラッグ終了したときのイベントハンドラ
    /// </summary>
    /// <param name="sender"></param>
    /// <param name="e"></param>
    private void TimeSlider_DragCompleted(object sender, DragCompletedEventArgs e)
    {
        // 動画のボリュームをもとに戻す
        MoviePlayer.Volume = _volume;
        // スライダーの位置→動画の再生位置更新を止める
        _dispatcherTimer.Stop();
        // 動画再生中のとき
        if(true == PlayToggle.IsChecked)
        {
            // 動画を再生状態に戻す
            _timelineStory.Resume();
        }
    }

    /// <summary>
    /// トグルボタンをクリックしたときのイベントハンドラ
    /// </summary>
    /// <param name="sender"></param>
    /// <param name="e"></param>
    private void PlayToggle_OnClick(object sender, RoutedEventArgs e)
    {
        // 動画再生命令
        if(true == PlayToggle.IsChecked)
        {
            // 動画がすべて再生し終わったときは再生位置を0に戻す
            if(_requestReset)
            {
                _timelineStory.Seek(TimeSpan.Zero);
                _requestReset = false;
            }
            PauseToPlay();
        }
        // 動画一時停止命令
        else if(false == PlayToggle.IsChecked)
        {
            PlayToPause();
        }
    }

    /// <summary>
    /// 動画を再生状態→一時停止状態にするときの処理
    /// </summary>
    private void PlayToPause()
    {
        _timelineStory.Pause();
        PlayToggle.Content = "▶";
    }

    /// <summary>
    /// 動画ｗ一時停止状態→再生状態にするときの処理
    /// </summary>
    private void PauseToPlay()
    {
        _timelineStory.Resume();
        PlayToggle.Content = "■";
    }
}