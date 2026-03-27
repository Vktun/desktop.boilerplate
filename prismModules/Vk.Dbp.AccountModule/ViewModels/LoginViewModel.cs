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

namespace Vk.Dbp.AccountModule.ViewModels
{
    /// <summary>
    /// 登录ViewModel
    /// </summary>
    public class LoginViewModel : BindableBase, INavigationAware
    {
        private readonly IUserService _userService;
        private readonly IPermissionService _permissionService;
        private readonly IRegionManager _regionManager;
        private readonly IPasswordHasher _passwordHasher;

        private string _username = "admin";
        /// <summary>
        /// 用户名
        /// </summary>
        public string Username
        {
            get { return _username; }
            set { SetProperty(ref _username, value); }
        }

        private bool _rememberPassword;
        /// <summary>
        /// 记住密码
        /// </summary>
        public bool RememberPassword
        {
            get { return _rememberPassword; }
            set { SetProperty(ref _rememberPassword, value); }
        }

        private bool _isLoading;
        /// <summary>
        /// 是否正在加载
        /// </summary>
        public bool IsLoading
        {
            get { return _isLoading; }
            set { SetProperty(ref _isLoading, value); }
        }

        private string _errorMessage;
        /// <summary>
        /// 错误消息
        /// </summary>
        public string ErrorMessage
        {
            get { return _errorMessage; }
            set { SetProperty(ref _errorMessage, value); }
        }

        private bool _showError;
        /// <summary>
        /// 是否显示错误
        /// </summary>
        public bool ShowError
        {
            get { return _showError; }
            set { SetProperty(ref _showError, value); }
        }

        public DelegateCommand<PasswordBox> LoginCommand { get; }

        public LoginViewModel(IUserService userService, IPermissionService permissionService, IRegionManager regionManager, IPasswordHasher passwordHasher)
        {
            _userService = userService ?? throw new ArgumentNullException(nameof(userService));
            _permissionService = permissionService ?? throw new ArgumentNullException(nameof(permissionService));
            _regionManager = regionManager ?? throw new ArgumentNullException(nameof(regionManager));
            _passwordHasher = passwordHasher ?? throw new ArgumentNullException(nameof(passwordHasher));

            LoginCommand = new DelegateCommand<PasswordBox>(async password => await Login(password), CanLogin);
        }

        /// <summary>
        /// 执行登录
        /// </summary>
        private async Task Login(PasswordBox passwordBox)
        {
            if (string.IsNullOrWhiteSpace(Username) || passwordBox == null || string.IsNullOrWhiteSpace(passwordBox.Password))
            {
                ShowError = true;
                ErrorMessage = "用户名和密码不能为空";
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
                    ErrorMessage = "用户名不存在";
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
                    var userSession = UserSession.Instance;
                    userSession.Login(user, GenerateToken());

                    var permissions = await _permissionService.GetUserPermissionsAsync(user.Id);
                    var permissionCodes = permissions.Select(p => p.Code).ToList();
                    userSession.SetPermissions(permissionCodes);

                    passwordBox.Clear();

                    _regionManager.RequestNavigate("ContentRegion", "Dashboard");
                }
                else
                {
                    ShowError = true;
                    ErrorMessage = "用户名或密码错误";
                }
            }
            catch (Exception ex)
            {
                ShowError = true;
                ErrorMessage = $"登录失败: {ex.Message}";
            }
            finally
            {
                IsLoading = false;
            }
        }

        /// <summary>
        /// 验证是否可以登录
        /// </summary>
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

        /// <summary>
        /// 生成认证令牌
        /// </summary>
        private string GenerateToken()
        {
            return Guid.NewGuid().ToString();
        }

        public void OnNavigatedTo(NavigationContext navigationContext)
        {
            if (UserSession.Instance.IsLoggedIn)
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
