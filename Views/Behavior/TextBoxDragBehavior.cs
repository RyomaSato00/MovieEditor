using System.Windows;
using System.Windows.Controls;

namespace MovieEditor.Views.Behavior;

/// <summary>
/// ドラッグ中にテキストボックス内に侵入した際にマウスのエフェクトを変える
/// </summary>
internal class TextBoxDragBehavior
{
    public static readonly DependencyProperty DragOverEffectProperty = 
    DependencyProperty.RegisterAttached
    (
        // プロパティの名前
        "DragOverEffect",
        // プロパティの型
        typeof(bool),
        // このプロパティの所有者の型
        typeof(TextBoxDragBehavior),
        // プロパティの初期値をfalseに設定し、プロパティ変更時のイベントハンドラにIsPropertyChangedを設定する
        new UIPropertyMetadata(false, IsPropertyChanged)
    );

    [AttachedPropertyBrowsableForType(typeof(TextBox))]
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
        var textBox = sender as TextBox;
        if(null == textBox) return;

        // イベントを登録・削除
        textBox.PreviewDragOver -= OnDragOver;

        var newValue = (bool)e.NewValue;
        textBox.AllowDrop = newValue;
        if(newValue)
        {
            textBox.PreviewDragOver += OnDragOver;
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