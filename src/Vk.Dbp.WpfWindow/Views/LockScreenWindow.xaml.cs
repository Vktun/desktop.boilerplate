using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;

namespace Dabp.WpfWindow.Views
{
    /// <summary>
    /// LockScreenWindow.xaml 的交互逻辑
    /// </summary>
    public partial class LockScreenWindow : Window
    {
        public LockScreenWindow()
        {
            InitializeComponent();

            // 窗口加载时聚焦密码框
            Loaded += OnWindowLoaded;
        }

        private void OnWindowLoaded(object sender, RoutedEventArgs e)
        {
            PasswordBox.Focus();
        }

        /// <summary>
        /// 密码框按键事件 - Enter键触发解锁
        /// </summary>
        private void OnPasswordKeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Enter)
            {
                var viewModel = DataContext as ViewModels.LockScreenViewModel;
                if (viewModel != null && viewModel.UnlockCommand.CanExecute(PasswordBox))
                {
                    viewModel.UnlockCommand.Execute(PasswordBox);
                }
            }
        }
    }
}