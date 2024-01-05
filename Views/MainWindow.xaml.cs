using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using MovieEditor.ViewModels;

namespace MovieEditor.Views;

/// <summary>
/// Interaction logic for MainWindow.xaml
/// </summary>
public partial class MainWindow : Window
{
    private readonly MainWindowViewModel _mainWindowViewModel;
    public MainWindow()
    {
        InitializeComponent();
        _mainWindowViewModel = new MainWindowViewModel();
        DataContext = _mainWindowViewModel;
        Closing += (_, _) => _mainWindowViewModel.Dispose();
    }

    private void SourceList_OnDrop(object sender, DragEventArgs e)
    {
        if (sender is not ListView) return;
        if (e.Data.GetData(DataFormats.FileDrop) is not string[] dropFiles) return;
        _ = _mainWindowViewModel.SourceList_OnDrop(dropFiles);
    }

    private void SourceList_OnItemDoubleClicked(object sender, MouseButtonEventArgs e)
    {
        if(sender is not ListViewItem listViewItem) return;
        if(listViewItem.DataContext is not SourceListItemElement item) return;
        _mainWindowViewModel.SourceList_OnItemDoubleClicked(item);
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

    private void SourceListHeader0_OnChecked(object sender, RoutedEventArgs e)
    {
        if(sender is not CheckBox) return;
        _mainWindowViewModel.SourceListHeader0_OnChecked(true);
    }

    private void SourceListHeader0_OnUnchecked(object sender, RoutedEventArgs e)
    {
        if(sender is not CheckBox) return;
        _mainWindowViewModel.SourceListHeader0_OnChecked(false);
    }

    private void OutDirectory_OnDrop(object sender, DragEventArgs e)
    {
        if(sender is not TextBox) return;
        if(e.Data.GetData(DataFormats.FileDrop) is not string[] dropFiles) return;
        _mainWindowViewModel.OutDirectory_OnDrop(dropFiles[0]);
    }
}