using System.Diagnostics;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;

namespace MovieEditor.Views.Behavior;

internal class TextBoxExplorerPathBehavior
{
    public static readonly DependencyProperty UseExplorerPathMenuProperty =
    DependencyProperty.RegisterAttached(
        // プロパティの名前
        "UseExplorerPathMenu",
        // プロパティの型
        typeof(bool),
        // このプロパティの所有者の型
        typeof(TextBoxExplorerPathBehavior),
        // プロパティの初期値をfalseに設定し、プロパティ変更時のイベントハンドラを設定
        new UIPropertyMetadata(false, OnPropertyChanged)
    );

    /// <summary>
    /// プロパティを取得する
    /// </summary>
    /// <param name="target">対象とするdependency object</param>
    /// <returns></returns>
    [AttachedPropertyBrowsableForType(typeof(TextBox))]
    public static bool GetUseExplorerPathMenu(DependencyObject target)
    {
        return (bool)target.GetValue(UseExplorerPathMenuProperty);
    }

    /// <summary>
    /// プロパティを設定します
    /// </summary>
    /// <param name="target">対象とするdependency object</param>
    /// <param name="value">設定する値</param>
    public static void SetUseExplorerPathMenu(DependencyObject target, bool value)
    {
        target.SetValue(UseExplorerPathMenuProperty, value);
    }

    private static void OnPropertyChanged(DependencyObject sender, DependencyPropertyChangedEventArgs e)
    {
        // イベント発行元がTextBoxでなければ何もしない
        if (sender is not TextBox textBox) return;

        if (GetUseExplorerPathMenu(textBox))
        {
            // コンテキストメニュー定義
            var menu = new ContextMenu();
            // デフォルトメニュー定義
            MenuItem[] defaultMenuItems =
            [
                new MenuItem() { Header = "切り取り", Command = ApplicationCommands.Cut },
                new MenuItem() { Header = "コピー", Command = ApplicationCommands.Copy },
                new MenuItem() { Header = "貼り付け", Command = ApplicationCommands.Paste },
                new MenuItem() { Header = "すべて選択", Command = ApplicationCommands.SelectAll }
            ];

            foreach(var item in defaultMenuItems)
            {
                menu.Items.Add(item);
            }

            // 「エクスプローラで開く」を定義
            var explorerItem = new MenuItem() { Header = "エクスプローラで開く" };
            explorerItem.Click += (_, _) => OpenExplorer(textBox.Text);
            menu.Items.Add(explorerItem);

            textBox.ContextMenu = menu;
        }
    }

    /// <summary>
    /// エクスプローラでpathのフォルダを開く
    /// </summary>
    /// <param name="path"></param>
    private static void OpenExplorer(string path)
    {
        var info = new ProcessStartInfo("EXPLORER.EXE")
        {
            Arguments = path,
            UseShellExecute = false
        };

        using var process = new Process() { StartInfo = info };
        process.Start();
        _ = process.WaitForExitAsync();
    }
}