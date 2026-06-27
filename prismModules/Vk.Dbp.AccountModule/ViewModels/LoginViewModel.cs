using System.Windows.Controls;
using Dabp.Services.Settings;
using Dabp.Utils.Exceptions;
using Dabp.Utils.Security;
using Prism.Commands;
using Prism.Mvvm;
using Prism.Navigation.Regions;
using Vk.Dbp.Contracts.Constants;
using Vk.Dbp.Contracts.Services;
using UserModel = Vk.Dbp.AccountModule.Models.User;
using PermissionModel = Vk.Dbp.AccountModule.Models.Permission;
using Vk.Dbp.AccountModule.Services;
using Vk.Dbp.Services.Audit;
using Vk.Dbp.Services.Session;

namespace Vk.Dbp.AccountModule.ViewModels;

public class LoginViewModel : BindableBase, INavigationAware
{
    private const string RememberUsernameKey = "auth.remember_username";
    private const string UsernameKey = "auth.username";
    private const int MaxFailedLoginAttempts = 5;
    private static readonly TimeSpan LoginLockoutDuration = TimeSpan.FromMinutes(15);

    private readonly IUserService _userService;
    private readonly IPermissionService _permissionService;
    private readonly INavigationService _navigationService;
    private readonly IPasswordHasher _passwordHasher;
    private readonly IUserSession _userSession;
    private readonly IAuditLogService _auditLogService;
    private readonly IAppSettingsService _appSettingsService;

    private string _username = string.Empty;

    public string Username
    {
        get => _username;
        set => SetProperty(ref _username, value);
    }

    private bool _rememberPassword;

    public bool RememberPassword
    {
        get => _rememberPassword;
        set => SetProperty(ref _rememberPassword, value);
    }

    private bool _isLoading;

    public bool IsLoading
    {
        get => _isLoading;
        set => SetProperty(ref _isLoading, value);
    }

    private string _errorMessage = string.Empty;

    public string ErrorMessage
    {
        get => _errorMessage;
        set => SetProperty(ref _errorMessage, value);
    }

    private bool _showError;

    public bool ShowError
    {
        get => _showError;
        set => SetProperty(ref _showError, value);
    }

    public DelegateCommand<PasswordBox> LoginCommand { get; }

    public LoginViewModel(
        IUserService userService,
        IPermissionService permissionService,
        INavigationService navigationService,
        IPasswordHasher passwordHasher,
        IUserSession userSession,
        IAuditLogService auditLogService,
        IAppSettingsService appSettingsService)
    {
        _userService = userService ?? throw new ArgumentNullException(nameof(userService));
        _permissionService = permissionService ?? throw new ArgumentNullException(nameof(permissionService));
        _navigationService = navigationService ?? throw new ArgumentNullException(nameof(navigationService));
        _passwordHasher = passwordHasher ?? throw new ArgumentNullException(nameof(passwordHasher));
        _userSession = userSession ?? throw new ArgumentNullException(nameof(userSession));
        _auditLogService = auditLogService ?? throw new ArgumentNullException(nameof(auditLogService));
        _appSettingsService = appSettingsService ?? throw new ArgumentNullException(nameof(appSettingsService));

        LoadSavedPreferences();
        LoginCommand = new DelegateCommand<PasswordBox>(async password => await LoginAsync(password), CanLogin)
            .ObservesProperty(() => IsLoading);
    }

    private void LoadSavedPreferences()
    {
        RememberPassword = _appSettingsService.GetValue(RememberUsernameKey, false);
        Username = RememberPassword
            ? _appSettingsService.GetValue(UsernameKey, string.Empty)
            : string.Empty;
    }

    private async Task LoginAsync(PasswordBox? passwordBox)
    {
        if (string.IsNullOrWhiteSpace(Username) || passwordBox is null || string.IsNullOrWhiteSpace(passwordBox.Password))
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
            UserModel? user = await _userService.GetUserByUsernameAsync(Username);

            if (user is null)
            {
                await _auditLogService.LogFailureAsync(
                    0,
                    Username,
                    AuditActionType.Login,
                    "Account",
                    $"用户登录失败: {Username}",
                    "User not found",
                    "User");

                ShowError = true;
                ErrorMessage = "用户名或密码错误";
                return;
            }

            if (!user.IsEnabled)
            {
                await _auditLogService.LogFailureAsync(
                    user.Id,
                    user.Username ?? Username,
                    AuditActionType.Login,
                    "Account",
                    $"用户登录失败: {Username}",
                    "User disabled",
                    "User",
                    user.Id);

                ShowError = true;
                ErrorMessage = "该用户已被禁用";
                return;
            }

            if (user.LockoutEndTime is { } lockoutEndTime && lockoutEndTime > DateTime.Now)
            {
                await _auditLogService.LogFailureAsync(
                    user.Id,
                    user.Username ?? Username,
                    AuditActionType.Login,
                    "Account",
                    $"用户登录失败: {Username}",
                    $"User locked until {lockoutEndTime:yyyy-MM-dd HH:mm:ss}",
                    "User",
                    user.Id);

                ShowError = true;
                ErrorMessage = $"账号已锁定，请在 {lockoutEndTime:HH:mm} 后重试";
                return;
            }

            if (user.LockoutEndTime is { } expiredLockoutEndTime && expiredLockoutEndTime <= DateTime.Now)
            {
                await _userService.ClearLoginFailuresAsync(user.Id);
                user.LockoutEndTime = null;
                user.FailedLoginCount = 0;
            }

            if (!ValidatePassword(passwordBox.Password, user.PasswordHash))
            {
                DateTime? newLockoutEndTime = await _userService.RecordLoginFailureAsync(
                    user.Id,
                    MaxFailedLoginAttempts,
                    LoginLockoutDuration);
                string failureReason = newLockoutEndTime is { } lockedUntil
                    ? $"Invalid password; user locked until {lockedUntil:yyyy-MM-dd HH:mm:ss}"
                    : "Invalid password";

                await _auditLogService.LogFailureAsync(
                    user.Id,
                    user.Username ?? Username,
                    AuditActionType.Login,
                    "Account",
                    $"用户登录失败: {Username}",
                    failureReason,
                    "User",
                    user.Id);

                ShowError = true;
                ErrorMessage = newLockoutEndTime is { } lockedUntilMessage
                    ? $"密码错误次数过多，账号已锁定至 {lockedUntilMessage:HH:mm}"
                    : "用户名或密码错误";
                return;
            }

            _userSession.Login(user.Id, user.Username ?? Username, user.RealName ?? "", user.Email ?? "", user.Phone ?? "", GenerateToken());
            await _userService.RecordLoginSuccessAsync(user.Id);

            List<PermissionModel> permissions = await _permissionService.GetUserPermissionsAsync(user.Id);
            List<string> permissionCodes = permissions.Select(p => p.Code)
                .Where(code => !string.IsNullOrWhiteSpace(code))
                .Distinct()
                .ToList()!;
            _userSession.SetPermissions(permissionCodes);

            PersistPreferences();
            await _auditLogService.LogLoginAsync(user.Id, user.Username ?? Username);

            passwordBox.Clear();
            _navigationService.NavigateTo(ViewNames.Dashboard);
        }
        catch (Exception ex) when (ExpectedOperationExceptionFilter.IsExpectedDataOperationException(ex))
        {
            await _auditLogService.LogFailureAsync(
                0,
                Username,
                AuditActionType.Login,
                "Account",
                $"用户登录失败: {Username}",
                ex.Message,
                "Database");

            ShowError = true;
            ErrorMessage = "数据库连接失败，请稍后重试";
        }
        catch (Exception ex) when (ExpectedOperationExceptionFilter.IsExpectedUserOperationException(ex))
        {
            await _auditLogService.LogFailureAsync(
                0,
                Username,
                AuditActionType.Login,
                "Account",
                $"用户登录失败: {Username}",
                ex.Message,
                "System");

            ShowError = true;
            ErrorMessage = "登录失败，请稍后重试";
        }
        finally
        {
            IsLoading = false;
        }
    }

    private void PersistPreferences()
    {
        _appSettingsService.SetValue(RememberUsernameKey, RememberPassword);
        if (RememberPassword)
        {
            _appSettingsService.SetValue(UsernameKey, Username);
            return;
        }

        _appSettingsService.Remove(UsernameKey);
    }

    private bool CanLogin(PasswordBox? passwordBox)
    {
        return !IsLoading;
    }

    private bool ValidatePassword(string inputPassword, string? storedHash)
    {
        if (string.IsNullOrEmpty(inputPassword) || string.IsNullOrEmpty(storedHash))
        {
            return false;
        }

        return _passwordHasher.VerifyPassword(inputPassword, storedHash);
    }

    private static string GenerateToken()
    {
        // 使用加密安全的随机数生成Token
        byte[] tokenBytes = new byte[32];
        using (var rng = System.Security.Cryptography.RandomNumberGenerator.Create())
        {
            rng.GetBytes(tokenBytes);
        }
        return Convert.ToBase64String(tokenBytes)
            .Replace("+", "-")
            .Replace("/", "_")
            .TrimEnd('=');
    }

    public void OnNavigatedTo(NavigationContext navigationContext)
    {
        if (_userSession.IsLoggedIn)
        {
            _navigationService.NavigateTo(ViewNames.Dashboard);
            return;
        }

        LoadSavedPreferences();
    }

    public bool IsNavigationTarget(NavigationContext navigationContext)
    {
        return true;
    }

    public void OnNavigatedFrom(NavigationContext navigationContext)
    {
    }
}
