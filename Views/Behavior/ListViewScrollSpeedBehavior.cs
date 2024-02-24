using MyCommonFunctions;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;

namespace MovieEditor.Views.Behavior;

/// <summary>
/// マウスホイール時のスクロール量を調整するBehavior
/// </summary>
internal class ListViewScrollSpeedBehavior
{
    /// <summary>
    /// shiftスクロールするたびに毎回子要素からScrollViewerを探したくないので
    /// ListViewオブジェクトをkeyとしてScrollViewerを覚えておく
    /// </summary>
    private static readonly Dictionary<ListView, ScrollViewer> _scrollViewerStore = [];

    /// <summary>
    /// list viewごとのスクロール量を覚えておくための辞書
    /// </summary>
    private static readonly Dictionary<ListView, float> _scrollSpeedStore = [];

    public static readonly DependencyProperty ScrollSpeedRateProperty =
    DependencyProperty.RegisterAttached(
        // プロパティの名前
        "ScrollSpeedRate",
        // プロパティの型
        typeof(float),
        // このプロパティの所有者の型
        typeof(ListViewScrollSpeedBehavior),
        // プロパティの初期値を1に設定し、プロパティ変更時のイベントハンドラを設定
        new UIPropertyMetadata((float)1, OnPropertyChanged)
    );

    [AttachedPropertyBrowsableForType(typeof(ListView))]
    public static float GetScrollSpeedRate(DependencyObject target)
    {
        return (float)target.GetValue(ScrollSpeedRateProperty);
    }


    public static void SetScrollSpeedRate(DependencyObject target, float value)
    {
        target.SetValue(ScrollSpeedRateProperty, value);
    }

    private static void OnPropertyChanged(DependencyObject sender, DependencyPropertyChangedEventArgs e)
    {
        if (sender is not ListView listView) return;

        // イベントクリア
        listView.PreviewMouseWheel -= OnWheeled;
        listView.SizeChanged -= OnSizeChanged;

        try
        {
            var propertyValue = GetScrollSpeedRate(listView);
            _scrollSpeedStore.Add(listView, propertyValue);
            listView.PreviewMouseWheel += OnWheeled;
            listView.SizeChanged += OnSizeChanged;
        }
        catch(Exception exception)
        {
            System.Diagnostics.Debug.WriteLine(exception);
        }

    }


    private static void OnSizeChanged(object sender, SizeChangedEventArgs e)
    {
        if (sender is not ListView listView) return;
        // このListViewのScrollViewerをすでに保存済みならば何もしない
        if(_scrollViewerStore.ContainsKey(listView)) return;
        // scrollviewerを取得
        var scrollViewer = WpfApi.FindVisualChild<ScrollViewer>(listView);

        if(scrollViewer is null) return;
        // listviewとscrollviewerの紐づけを保存
        _scrollViewerStore.Add(listView, scrollViewer);
    }


    private static void OnWheeled(object sender, MouseWheelEventArgs e)
    {
        if(sender is not ListView listView) return;

        try
        {
            var scrollViewer = _scrollViewerStore[listView];
            var speed = _scrollSpeedStore[listView];
            // スクロールする
            scrollViewer.ScrollToVerticalOffset(scrollViewer.VerticalOffset - e.Delta * speed);
            // デフォルトのスクロールを無効にする
            e.Handled = true;
        }
        catch(Exception exception)
        {
            System.Diagnostics.Debug.WriteLine(exception);
        }
    }
}