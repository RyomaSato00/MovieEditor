using System.Windows;
using System.Windows.Controls;

namespace MovieEditor.Views.Behavior;

/// <summary>
/// TextBoxで最終行まで自動スクロールするためのBehavior
/// </summary>
internal class TextBoxBehavior
{
    /// <summary>
    /// 複数行のテキストを扱う
    /// テキスト追加時に最終行が表示されるようにする
    /// </summary>
    /// <param name=""AutoScrollToEnd""></param>
    /// <param name="typeof(bool)"></param>
    /// <param name="typeof(TextBoxBehavior)"></param>
    /// <param name="UIPropertyMetadata(false"></param>
    /// <returns></returns>
    public static readonly DependencyProperty AutoScrollToEndProperty = 
    DependencyProperty.RegisterAttached
    (
        // プロパティの名前
        "AutoScrollToEnd",
        // プロパティの型
        typeof(bool),
        // このプロパティの所有者の型
        typeof(TextBoxBehavior),
        // プロパティの初期値をfalseに設定し、プロパティ変更時のイベントハンドラにIsTextChangedを設定する
        new UIPropertyMetadata(false, IsTextChanged)
    );

    [AttachedPropertyBrowsableForType(typeof(TextBox))]
    public static bool GetAutoScrollToEnd(DependencyObject obj)
    {
        return (bool)obj.GetValue(AutoScrollToEndProperty);
    }

    public static void SetAutoScrollToEnd(DependencyObject obj, bool value)
    {
        obj.SetValue(AutoScrollToEndProperty, value);
    }

    private static void IsTextChanged
    (
        DependencyObject sender, DependencyPropertyChangedEventArgs e
    )
    {
        var textBox = sender as TextBox;
        if(null == textBox) return;

        // イベントを登録・削除
        textBox.TextChanged -= OnTextChanged;

        var newValue = (bool)e.NewValue;
        if(newValue)
        {
            textBox.TextChanged += OnTextChanged;
        }
    }

    private static void OnTextChanged(object sender, TextChangedEventArgs e)
    {
        var textBox = sender as TextBox;
        if(null == textBox) return;

        if(string.IsNullOrEmpty(textBox.Text)) return;

        textBox.ScrollToEnd();
    }
}