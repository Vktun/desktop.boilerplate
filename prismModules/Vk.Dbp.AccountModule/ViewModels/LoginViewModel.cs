using System;
using System.Linq;
using System.Threading.Tasks;
using System.Windows.Controls;
using Dabp.Utils.Security;
using Prism.Commands;
using Prism.Mvvm;
using Prism.Navigation.Regions;
using Vk.Dbp.AccountModule.Models;
using Vk.Dbp.AccountModule.Services;
using SqlSugar;

namespace Vk.Dbp.AccountModule.ViewModels
{
    public class LoginViewModel : BindableBase, INavigationAware
    {
        private readonly IUserService _userService;
        private readonly IPermissionService _permissionService;
        private readonly IRegionManager _regionManager;
        private readonly IPasswordHasher _passwordHasher;
        private readonly IUserSession _userSession;

        private string _username = "admin";
        public string Username
        {
            get { return _username; }
            set { SetProperty(ref _username, value); }
        }

        private bool _rememberPassword;
        public bool RememberPassword
        {
            get { return _rememberPassword; }
            set { SetProperty(ref _rememberPassword, value); }
        }

        private bool _isLoading;
        public bool IsLoading
        {
            get { return _isLoading; }
            set { SetProperty(ref _isLoading, value); }
        }

        private string _errorMessage;
        public string ErrorMessage
        {
            get { return _errorMessage; }
            set { SetProperty(ref _errorMessage, value); }
        }

        private bool _showError;
        public bool ShowError
        {
            get { return _showError; }
            set { SetProperty(ref _showError, value); }
        }

        public DelegateCommand<PasswordBox> LoginCommand { get; }

        public LoginViewModel(
            IUserService userService, 
            IPermissionService permissionService, 
            IRegionManager regionManager, 
            IPasswordHasher passwordHasher,
            IUserSession userSession)
        {
            _userService = userService ?? throw new ArgumentNullException(nameof(userService));
            _permissionService = permissionService ?? throw new ArgumentNullException(nameof(permissionService));
            _regionManager = regionManager ?? throw new ArgumentNullException(nameof(regionManager));
            _passwordHasher = passwordHasher ?? throw new ArgumentNullException(nameof(passwordHasher));
            _userSession = userSession ?? throw new ArgumentNullException(nameof(userSession));

            LoginCommand = new DelegateCommand<PasswordBox>(async password => await Login(password), CanLogin);
        }

        private async Task Login(PasswordBox passwordBox)
        {
            if (string.IsNullOrWhiteSpace(Username) || passwordBox == null || string.IsNullOrWhiteSpace(passwordBox.Password))
            {
                ShowError = true;
                ErrorMessage = "用户名和密码不能为空";
                return;
            }

            if (Username.Length > 50)
            {
                ShowError = true;
                ErrorMessage = "用户名长度不能超过50个字符";
                return;
            }

            IsLoading = true;
            ShowError = false;

            try
            {
                var user = await _userService.GetUserByUsernameAsync(Username);

                if (user == null)
                {
                    ShowError = true;
                    ErrorMessage = "用户名或密码错误";
                    return;
                }

                if (!user.IsEnabled)
                {
                    ShowError = true;
                    ErrorMessage = "该用户已被禁用";
                    return;
                }

                if (ValidatePassword(passwordBox.Password, user.PasswordHash))
                {
                    _userSession.Login(user, GenerateToken());

                    var permissions = await _permissionService.GetUserPermissionsAsync(user.Id);
                    var permissionCodes = permissions.Select(p => p.Code).ToList();
                    _userSession.SetPermissions(permissionCodes);

                    passwordBox.Clear();

                    _regionManager.RequestNavigate("ContentRegion", "Dashboard");
                }
                else
                {
                    ShowError = true;
                    ErrorMessage = "用户名或密码错误";
                }
            }
            catch (Exception ex) when (ex is SqlSugarException)
            {
                ShowError = true;
                ErrorMessage = "数据库连接失败，请稍后重试";
            }
            catch (Exception)
            {
                ShowError = true;
                ErrorMessage = "登录失败，请稍后重试";
            }
            finally
            {
                IsLoading = false;
            }
        }

        private bool CanLogin(PasswordBox passwordBox)
        {
            return !IsLoading;
        }

        private bool ValidatePassword(string inputPassword, string storedHash)
        {
            if (string.IsNullOrEmpty(inputPassword))
                return false;

            return _passwordHasher.VerifyPassword(inputPassword, storedHash);
        }

        private string GenerateToken()
        {
            return Guid.NewGuid().ToString();
        }

        public void OnNavigatedTo(NavigationContext navigationContext)
        {
            if (_userSession.IsLoggedIn)
            {
                _regionManager.RequestNavigate("ContentRegion", "Dashboard");
            }
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
