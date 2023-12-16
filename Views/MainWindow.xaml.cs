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
        _mainWindowViewModel.SourceList_OnDrop(e);
    }

}