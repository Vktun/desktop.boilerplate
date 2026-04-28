using System.Windows;
using System.Windows.Controls;

namespace Vk.Dbp.AccountModule.Views;

public partial class LoginView : UserControl
{
    private bool _isSyncingPassword;

    public LoginView()
    {
        InitializeComponent();
    }

    private void PasswordBox_PasswordChanged(object sender, RoutedEventArgs e)
    {
        if (!_isSyncingPassword)
        {
            _isSyncingPassword = true;
            PasswordRevealTextBox.Text = PasswordBox.Password;
            _isSyncingPassword = false;
        }

        UpdatePasswordPlaceholder();
    }

    private void PasswordRevealTextBox_TextChanged(object sender, TextChangedEventArgs e)
    {
        if (!_isSyncingPassword)
        {
            _isSyncingPassword = true;
            PasswordBox.Password = PasswordRevealTextBox.Text;
            _isSyncingPassword = false;
        }

        UpdatePasswordPlaceholder();
    }

    private void PasswordToggleButton_Click(object sender, RoutedEventArgs e)
    {
        bool isRevealed = PasswordToggleButton.IsChecked == true;

        PasswordRevealTextBox.Visibility = isRevealed ? Visibility.Visible : Visibility.Collapsed;
        PasswordBox.Visibility = isRevealed ? Visibility.Collapsed : Visibility.Visible;

        if (isRevealed)
        {
            PasswordRevealTextBox.Focus();
            PasswordRevealTextBox.CaretIndex = PasswordRevealTextBox.Text.Length;
            return;
        }

        PasswordBox.Focus();
    }

    private void UpdatePasswordPlaceholder()
    {
        PasswordPlaceholder.Visibility = string.IsNullOrEmpty(PasswordBox.Password)
            ? Visibility.Visible
            : Visibility.Collapsed;
    }
}
