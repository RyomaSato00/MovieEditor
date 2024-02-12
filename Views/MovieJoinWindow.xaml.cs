using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Diagnostics;
using MyCommonFunctions;
using MovieEditor.ViewModels;

namespace MovieEditor.Views;

public partial class MovieJoinWindow : Window
{
    public MovieJoinWindow()
    {
        InitializeComponent();
    }

    private void SourceList_OnItemDoubleClicked(object sender, MouseButtonEventArgs e)
    {
        if(sender is not ListViewItem listViewItem) return;
        if(listViewItem.DataContext is not SourceListItemElement item) return;
        try
        {
            Process.Start(new ProcessStartInfo(item.Info.FilePath) { UseShellExecute = true });
        }
        catch (Exception exception)
        {
            MyConsole.WriteLine(exception.Message, MyConsole.Level.Error);
        }
    }

    /// <summary>
    /// リストヘッダのチェックボックスをマークしたときのイベントハンドラ
    /// </summary>
    /// <param name="sender"></param>
    /// <param name="e"></param>
    private void SourceListHeader0_OnChecked(object sender, RoutedEventArgs e)
    {
        if(sender is not CheckBox) return;
        // リストのすべてのアイテムにチェックをつける
        foreach(var item in SourceList.ItemsSource)
        {
            if(item is SourceListItemElement itemElement)
            {
                itemElement.IsChecked = true;
            }
        }
    }

    /// <summary>
    /// リストヘッダのチェックボックスのマークを外した時のイベントハンドラ
    /// </summary>
    /// <param name="sender"></param>
    /// <param name="e"></param>
    private void SourceListHeader0_OnUnchecked(object sender, RoutedEventArgs e)
    {
        if(sender is not CheckBox) return;
        // リストのすべてのアイテムのチェックをはずす
        foreach(var item in SourceList.ItemsSource)
        {
            if(item is SourceListItemElement itemElement)
            {
                itemElement.IsChecked = false;
            }
        }
    }

    /// <summary>
    /// トグルスイッチによってサムネイルの表示・非表示を切り替える
    /// </summary>
    /// <param name="sender"></param>
    /// <param name="e"></param>
    private void SourceList_OnTemplateToggled(object sender, RoutedEventArgs e)
    {
        if(sender is not ModernWpf.Controls.ToggleSwitch toggleSwitch) return;
        // トグルONならばサムネイルを表示する
        if(toggleSwitch.IsOn)
        {
            SourceList.ItemTemplate = (DataTemplate)FindResource("ThumbnailVisible");
        }
        // トグルOFFならばサムネイルではなくファイル名を表示する
        else
        {
            SourceList.ItemTemplate = (DataTemplate)FindResource("ThumbnailHidden");
        }
    }

    private void UpButton_OnClicked(object sender, RoutedEventArgs e)
    {
        // リスト上で何も選択されていない場合は何もしない
        if(SourceList.SelectedItem is null) return;

        // 選択されたアイテムのインデックスを取得
        var itemIndex = SourceList.SelectedIndex;

        // 取得したインデックスが0のときはこれ以上上に移動できないため、何もしない
        if(0 == itemIndex) return;

    }
}