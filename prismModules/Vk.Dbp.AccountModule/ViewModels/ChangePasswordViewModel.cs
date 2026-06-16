using Dabp.Utils.Exceptions;
using Prism.Commands;
using Prism.Mvvm;
using Prism.Navigation.Regions;
using Vk.Dbp.AccountModule.Services;
using Vk.Dbp.Contracts.Constants;
using Vk.Dbp.Contracts.Services;
using Vk.Dbp.Services.Session;

namespace Vk.Dbp.AccountModule.ViewModels;

public class ChangePasswordViewModel : BindableBase, INavigationAware
{
    private readonly IUserService _userService;
    private readonly INavigationService _navigationService;
    private readonly IUserSession _userSession;

    private string _message = string.Empty;

    public string Message
    {
        get => _message;
        set => SetProperty(ref _message, value);
    }

    private bool _showMessage;

    public bool ShowMessage
    {
        get => _showMessage;
        set => SetProperty(ref _showMessage, value);
    }

    private bool _isError;

    public bool IsError
    {
        get => _isError;
        set => SetProperty(ref _isError, value);
    }

    private bool _isLoading;

    public bool IsLoading
    {
        get => _isLoading;
        set => SetProperty(ref _isLoading, value);
    }

    public DelegateCommand<object[]> ChangeCommand { get; }

    public DelegateCommand CancelCommand { get; }

    public ChangePasswordViewModel(IUserService userService, INavigationService navigationService, IUserSession userSession)
    {
        _userService = userService ?? throw new ArgumentNullException(nameof(userService));
        _navigationService = navigationService ?? throw new ArgumentNullException(nameof(navigationService));
        _userSession = userSession ?? throw new ArgumentNullException(nameof(userSession));

        ChangeCommand = new DelegateCommand<object[]>(async passwords => await ChangeAsync(passwords), CanChange);
        CancelCommand = new DelegateCommand(Cancel);
    }

    private bool CanChange(object[]? passwords)
    {
        return !IsLoading;
    }

    private async Task ChangeAsync(object[]? passwords)
    {
        if (passwords is null || passwords.Length < 3)
        {
            ShowMessage = true;
            IsError = true;
            Message = "瀵嗙爜鏁版嵁鏃犳晥";
            return;
        }

        var oldPasswordBox = passwords[0] as System.Windows.Controls.PasswordBox;
        var newPasswordBox = passwords[1] as System.Windows.Controls.PasswordBox;
        var confirmPasswordBox = passwords[2] as System.Windows.Controls.PasswordBox;

        if (oldPasswordBox is null || newPasswordBox is null || confirmPasswordBox is null)
        {
            ShowMessage = true;
            IsError = true;
            Message = "瀵嗙爜妗嗘棤鏁?";
            return;
        }

        string oldPassword = oldPasswordBox.Password;
        string newPassword = newPasswordBox.Password;
        string confirmPassword = confirmPasswordBox.Password;

        if (string.IsNullOrWhiteSpace(oldPassword) ||
            string.IsNullOrWhiteSpace(newPassword) ||
            string.IsNullOrWhiteSpace(confirmPassword))
        {
            ShowMessage = true;
            IsError = true;
            Message = "鎵€鏈夊瘑鐮佸瓧娈甸兘涓嶈兘涓虹┖";
            return;
        }

        if (newPassword != confirmPassword)
        {
            ShowMessage = true;
            IsError = true;
            Message = "鏂板瘑鐮佸拰纭瀵嗙爜涓嶄竴鑷?";
            return;
        }

        if (newPassword.Length < 6)
        {
            ShowMessage = true;
            IsError = true;
            Message = "鏂板瘑鐮侀暱搴︿笉鑳藉皯浜?浣?";
            return;
        }

        if (oldPassword == newPassword)
        {
            ShowMessage = true;
            IsError = true;
            Message = "鏂板瘑鐮佷笉鑳戒笌鍘熷瘑鐮佺浉鍚?";
            return;
        }

        IsLoading = true;
        ShowMessage = false;

        try
        {
            if (!_userSession.IsLoggedIn)
            {
                ShowMessage = true;
                IsError = true;
                Message = "鐢ㄦ埛鏈櫥褰?";
                return;
            }

            bool result = await _userService.ChangePasswordAsync(_userSession.UserId, oldPassword, newPassword);

            if (!result)
            {
                ShowMessage = true;
                IsError = true;
                Message = "鍘熷瘑鐮侀敊璇紝璇烽噸鏂拌緭鍏?";
                return;
            }

            ShowMessage = true;
            IsError = false;
            Message = "瀵嗙爜淇敼鎴愬姛";

            oldPasswordBox.Clear();
            newPasswordBox.Clear();
            confirmPasswordBox.Clear();

            await Task.Delay(1500);
            _navigationService.NavigateTo(ViewNames.Dashboard);
        }
        catch (Exception ex) when (ExpectedOperationExceptionFilter.IsExpectedDataOperationException(ex))
        {
            ShowMessage = true;
            IsError = true;
            Message = $"瀵嗙爜淇敼澶辫触: {ex.Message}";
        }
        finally
        {
            IsLoading = false;
        }
    }

    private void Cancel()
    {
        _navigationService.NavigateTo(ViewNames.Dashboard);
    }

    public void OnNavigatedTo(NavigationContext navigationContext)
    {
        ShowMessage = false;
        IsError = false;
        Message = string.Empty;
    }

    public bool IsNavigationTarget(NavigationContext navigationContext)
    {
        return true;
    }

    public void OnNavigatedFrom(NavigationContext navigationContext)
    {
    }
}
