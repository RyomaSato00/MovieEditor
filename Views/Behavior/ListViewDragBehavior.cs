using System.Windows;
using System.Windows.Controls;

namespace MovieEditor.Views.Behavior;

/// <summary>
/// ドラッグ中にリストビュー上に侵入したさいにマウスのエフェクトを変える
/// </summary>
internal class ListViewDragBehavior
{
    public static readonly DependencyProperty DragOverEffectProperty = 
    DependencyProperty.RegisterAttached
    (
        // プロパティの名前
        "DragOverEffect",
        // プロパティの型
        typeof(bool),
        // このプロパティの所有者の型
        typeof(ListViewDragBehavior),
        // プロパティの初期値をfalseに設定し、プロパティ変更時のイベントハンドラにIsPropertyChangedを設定する
        new UIPropertyMetadata(false, IsPropertyChanged)
    );

    [AttachedPropertyBrowsableForType(typeof(ListView))]
    public static bool GetDragOverEffect(DependencyObject obj)
    {
        return (bool)obj.GetValue(DragOverEffectProperty);
    }

    public static void SetDragOverEffect(DependencyObject obj, bool value)
    {
        obj.SetValue(DragOverEffectProperty, value);
    }

    private static void IsPropertyChanged(DependencyObject sender, DependencyPropertyChangedEventArgs e)
    {
        var listView = sender as ListView;
        if(null == listView) return;

        // イベントを登録・削除
        listView.PreviewDragOver -= OnDragOver;

        var newValue = (bool)e.NewValue;
        listView.AllowDrop = newValue;
        if(newValue)
        {
            listView.PreviewDragOver += OnDragOver;
        }
    }

    private static void OnDragOver(object sender, DragEventArgs e)
    {
        if(e.Data.GetDataPresent(DataFormats.FileDrop))
        {
            e.Effects = DragDropEffects.All;
        }
        else
        {
            e.Effects = DragDropEffects.None;
        }
        e.Handled = true;
    }
}