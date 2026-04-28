using System.Windows.Controls;
using System.Windows;

namespace Vk.Dbp.WpfWindow.Views
{
    /// <summary>
    /// AppAlarmView.xaml 的交互逻辑
    /// </summary>
    public partial class AppAlarmView : UserControl
    {
        public AppAlarmView()
        {
            InitializeComponent();
        }

        private void CloseButton_Click(object sender, RoutedEventArgs e)
        {
            Window? hostWindow = Window.GetWindow(this);
            hostWindow?.Close();
        }
    }
}
