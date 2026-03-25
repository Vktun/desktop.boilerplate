using Dabp.Tools.Layout;
using Prism.Navigation.Regions;
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

namespace Dabp.Tools
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        public MainWindow(IRegionManager regionManager)
        {
            InitializeComponent();
            //view discovery
            regionManager.RegisterViewWithRegion("ContentRegion", typeof(Content));
            regionManager.RegisterViewWithRegion("HeaderRegion", typeof(Header));
            regionManager.RegisterViewWithRegion("LeftRegion", typeof(Left));
        }
    }
}