using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using MyCommonFunctions;

namespace MovieEditor.Views.Behavior;

/// <summary>
/// shiftを押しながらマウスホイールで横スクロールをできるようにするBehavior
/// </summary>
internal class ListViewHorizontalScrollBehavior
{
    /// <summary>
    /// shiftスクロールするたびに毎回子要素からScrollViewerを探したくないので
    /// ListViewオブジェクトをkeyとしてScrollViewerを覚えておく
    /// </summary>
    /// <returns></returns>
    private static readonly Dictionary<ListView, ScrollViewer> _scrollViewerStore = new();

    public static readonly DependencyProperty ShiftHorizontalScrollProperty =
    DependencyProperty.RegisterAttached
    (
        // プロパティの名前
        "ShiftHorizontalScroll",
        // プロパティの型
        typeof(bool),
        // このプロパティの所有者の型
        typeof(ListViewHorizontalScrollBehavior),
        // プロパティの初期値をfalseに設定し、プロパティ変更時のイベントハンドラにIsPropertyChangedを設定する
        new UIPropertyMetadata(false, IsPropertyChanged)
    );

    [AttachedPropertyBrowsableForType(typeof(ListView))]
    public static bool GetShiftHorizontalScroll(DependencyObject obj)
    {
        return (bool)obj.GetValue(ShiftHorizontalScrollProperty);
    }

    public static void SetShiftHorizontalScroll(DependencyObject obj, bool value)
    {
        obj.SetValue(ShiftHorizontalScrollProperty, value);
    }

    private static void IsPropertyChanged(DependencyObject sender, DependencyPropertyChangedEventArgs e)
    {
        if (sender is not ListView listView) return;

        // イベントを登録・削除
        listView.PreviewMouseWheel -= OnWheeled;
        listView.SizeChanged -= OnSizeChanged;

        var newValue = (bool)e.NewValue;
        if (newValue)
        {
            listView.PreviewMouseWheel += OnWheeled;
            listView.SizeChanged += OnSizeChanged;
        }
    }

    private static void OnSizeChanged(object sender, SizeChangedEventArgs e)
    {
        if (sender is not ListView listView) return;
        // このListViewのScrollViewerをすでに保存済ならなにもしない
        if (_scrollViewerStore.ContainsKey(listView)) return;
        // ScrollViewerを取得
        var scrollViewer = WpfApi.FindVisualChild<ScrollViewer>(listView);

        if (scrollViewer is null) return;
        // ListViewとScrollViewerの紐づけを保存
        _scrollViewerStore.Add(listView, scrollViewer);
    }

    private static void OnWheeled(object sender, MouseWheelEventArgs e)
    {
        if (sender is not ListView listView) return;

        // shiftキーが押されているか
        if ((Keyboard.Modifiers & ModifierKeys.Shift) == ModifierKeys.Shift)
        {
            // マウスホイールの回転量を取得し、横方向にスクロール
            var scrollViewer = _scrollViewerStore[listView];
            // マウスホイール量をそのままスクロールに反映すると速すぎるのでホイール量を0.5倍する
            scrollViewer.ScrollToHorizontalOffset(scrollViewer.HorizontalOffset - e.Delta * 0.5);
            e.Handled = true;
        }
    }
}