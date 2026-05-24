using System.Windows;
using Vk.Dbp.AdminWindow.ViewModels;

namespace Vk.Dbp.AdminWindow;

/// <summary>
/// Interaction logic for MainWindow.xaml.
/// </summary>
public partial class MainWindow : Window
{
    /// <summary>
    /// Initializes a new instance of the <see cref="MainWindow"/> class.
    /// </summary>
    public MainWindow()
    {
        InitializeComponent();
        DataContext = new AdminShellViewModel();
    }
}
