using System;
using System.Threading.Tasks;
using System.Windows.Controls;
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
        private readonly IRegionManager _regionManager;

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

        public LoginViewModel(IUserService userService, IRegionManager regionManager)
        {
            _userService = userService ?? throw new ArgumentNullException(nameof(userService));
            _regionManager = regionManager ?? throw new ArgumentNullException(nameof(regionManager));

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
                // 验证用户凭证 (这里简化处理，实际应该调用认证服务)
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

                // 验证密码 (简化实现，实际应该进行密码哈希验证)
                if (ValidatePassword(passwordBox.Password, user.PasswordHash))
                {
                    // 登录成功，保存用户会话信息
                    var userSession = UserSession.Instance;
                    userSession.Login(user, GenerateToken());

                    // 清除密码框
                    passwordBox.Clear();

                    // 导航到主页面
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

        /// <summary>
        /// 验证密码 (简化实现)
        /// </summary>
        private bool ValidatePassword(string inputPassword, string storedHash)
        {
            // 实际应使用密码哈希验证
            // 这里使用简单的演示实现
            if (string.IsNullOrEmpty(storedHash))
                return inputPassword == "123456"; // 默认密码用于演示

            return inputPassword == storedHash; // 实际应该用安全的密码验证
        }

        /// <summary>
        /// 生成认证令牌
        /// </summary>
        private string GenerateToken()
        {
            // 实际应该从认证服务获取
            return Guid.NewGuid().ToString();
        }

        public void OnNavigatedTo(NavigationContext navigationContext)
        {
            // 检查是否已经登录，如果已登录则直接导航到主页面
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
