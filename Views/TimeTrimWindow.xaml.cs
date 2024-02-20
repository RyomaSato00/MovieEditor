using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Input;
using System.Windows.Threading;
using MovieEditor.ViewModels;


namespace MovieEditor.Views;


public partial class TimeTrimWindow : Window
{
    private readonly DispatcherTimer _dispatcherTimer = new();
    private bool _isSliderDragged = false;
    public TimeTrimWindow()
    {
        InitializeComponent();
        MoviePlayer.Play();

        // TimeSlider.Maximum = 159;
        _dispatcherTimer.Interval = TimeSpan.FromMilliseconds(100);
        _dispatcherTimer.Tick += (_, _) =>
        {
            if(_isSliderDragged)
            {
                MoviePlayer.Position = TimeSpan.FromSeconds(TimeSlider.Value);
            }
            else
            {
                TimeSlider.Value = MoviePlayer.Position.TotalSeconds;
            }
            CurrentTime.Content = $"{MoviePlayer.Position:mm\\:ss\\.fff}";
        };
        _dispatcherTimer.Start();
    }

    private void TimeSlider_DragStarted(object sender, DragStartedEventArgs e)
    {
        System.Diagnostics.Debug.WriteLine("どらっぐ");
        _isSliderDragged = true;
    }

    private void TimeSlider_DragCompleted(object sender, DragCompletedEventArgs e)
    {
        System.Diagnostics.Debug.WriteLine("どろっぷ");
        _isSliderDragged = false;
    }

    private void PlayToggle_OnClick(object sender, RoutedEventArgs e)
    {
        if(PlayToggle.IsChecked is not bool isChecked) return;
        if(isChecked)
        {
            MoviePlayer.Pause();
            PlayToggle.Content = "停止";
        }
        else
        {
            MoviePlayer.Play();
            PlayToggle.Content = "再生";
        }
    }
}