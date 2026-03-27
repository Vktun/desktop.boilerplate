using System;
using System.Threading.Tasks;
using Prism.Commands;
using Prism.Mvvm;
using Prism.Navigation.Regions;
using Vk.Dbp.AccountModule.Models;
using Vk.Dbp.AccountModule.Services;

namespace Vk.Dbp.AccountModule.ViewModels
{
    public class ChangePasswordViewModel : BindableBase, INavigationAware
    {
        private readonly IUserService _userService;
        private readonly IRegionManager _regionManager;

        private string _message;
        public string Message
        {
            get { return _message; }
            set { SetProperty(ref _message, value); }
        }

        private bool _showMessage;
        public bool ShowMessage
        {
            get { return _showMessage; }
            set { SetProperty(ref _showMessage, value); }
        }

        private bool _isError;
        public bool IsError
        {
            get { return _isError; }
            set { SetProperty(ref _isError, value); }
        }

        private bool _isLoading;
        public bool IsLoading
        {
            get { return _isLoading; }
            set { SetProperty(ref _isLoading, value); }
        }

        public DelegateCommand<object[]> ChangeCommand { get; }
        public DelegateCommand CancelCommand { get; }

        public ChangePasswordViewModel(IUserService userService, IRegionManager regionManager)
        {
            _userService = userService ?? throw new ArgumentNullException(nameof(userService));
            _regionManager = regionManager ?? throw new ArgumentNullException(nameof(regionManager));

            ChangeCommand = new DelegateCommand<object[]>(async passwords => await Change(passwords), CanChange);
            CancelCommand = new DelegateCommand(Cancel);
        }

        private bool CanChange(object[] passwords)
        {
            return !IsLoading;
        }

        private async Task Change(object[] passwords)
        {
            if (passwords == null || passwords.Length < 3)
            {
                ShowMessage = true;
                IsError = true;
                Message = "密码数据无效";
                return;
            }

            var oldPasswordBox = passwords[0] as System.Windows.Controls.PasswordBox;
            var newPasswordBox = passwords[1] as System.Windows.Controls.PasswordBox;
            var confirmPasswordBox = passwords[2] as System.Windows.Controls.PasswordBox;

            if (oldPasswordBox == null || newPasswordBox == null || confirmPasswordBox == null)
            {
                ShowMessage = true;
                IsError = true;
                Message = "密码框无效";
                return;
            }

            var oldPassword = oldPasswordBox.Password;
            var newPassword = newPasswordBox.Password;
            var confirmPassword = confirmPasswordBox.Password;

            if (string.IsNullOrWhiteSpace(oldPassword) ||
                string.IsNullOrWhiteSpace(newPassword) ||
                string.IsNullOrWhiteSpace(confirmPassword))
            {
                ShowMessage = true;
                IsError = true;
                Message = "所有密码字段都不能为空";
                return;
            }

            if (newPassword != confirmPassword)
            {
                ShowMessage = true;
                IsError = true;
                Message = "新密码和确认密码不一致";
                return;
            }

            if (newPassword.Length < 6)
            {
                ShowMessage = true;
                IsError = true;
                Message = "新密码长度不能少于6位";
                return;
            }

            if (oldPassword == newPassword)
            {
                ShowMessage = true;
                IsError = true;
                Message = "新密码不能与原密码相同";
                return;
            }

            IsLoading = true;
            ShowMessage = false;

            try
            {
                var userSession = UserSession.Instance;
                if (!userSession.IsLoggedIn)
                {
                    ShowMessage = true;
                    IsError = true;
                    Message = "用户未登录";
                    return;
                }

                var result = await _userService.ChangePasswordAsync(userSession.UserId, oldPassword, newPassword);

                if (result)
                {
                    ShowMessage = true;
                    IsError = false;
                    Message = "密码修改成功";

                    oldPasswordBox.Clear();
                    newPasswordBox.Clear();
                    confirmPasswordBox.Clear();

                    await Task.Delay(1500);
                    _regionManager.RequestNavigate("ContentRegion", "Dashboard");
                }
                else
                {
                    ShowMessage = true;
                    IsError = true;
                    Message = "原密码错误，请重新输入";
                }
            }
            catch (Exception ex)
            {
                ShowMessage = true;
                IsError = true;
                Message = $"密码修改失败: {ex.Message}";
            }
            finally
            {
                IsLoading = false;
            }
        }

        private void Cancel()
        {
            _regionManager.RequestNavigate("ContentRegion", "Dashboard");
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
}
