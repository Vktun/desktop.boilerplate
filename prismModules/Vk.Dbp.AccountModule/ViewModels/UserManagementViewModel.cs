using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Windows;
using Dabp.Utils.Exceptions;
using Dabp.Utils.Security;
using HandyControl.Controls;
using Microsoft.Win32;
using Prism.Commands;
using Prism.Mvvm;
using Vk.Dbp.AccountModule.Models;
using Vk.Dbp.AccountModule.Services;
using Vk.Dbp.Services.Audit;
using Vk.Dbp.Services.Session;

namespace Vk.Dbp.AccountModule.ViewModels;

public class UserManagementViewModel : BindableBase
{
    private const int DefaultPageSize = 20;

    private readonly IUserService _userService;
    private readonly IAuditLogService _auditLogService;
    private readonly IUserSession _userSession;
    private readonly IPasswordHasher _passwordHasher;

    private ObservableCollection<User> _users = new();
    private User? _selectedUser;
    private bool _isLoading;
    private string _searchKeyword = string.Empty;
    private bool _isDialogOpen;
    private UserEditDialogViewModel? _currentDialogViewModel;
    private int _pageIndex = 1;
    private int _pageSize = DefaultPageSize;
    private int _totalCount;

    public ObservableCollection<User> Users
    {
        get => _users;
        set => SetProperty(ref _users, value);
    }

    public User? SelectedUser
    {
        get => _selectedUser;
        set => SetProperty(ref _selectedUser, value);
    }

    public bool IsLoading
    {
        get => _isLoading;
        set => SetProperty(ref _isLoading, value);
    }

    public string SearchKeyword
    {
        get => _searchKeyword;
        set => SetProperty(ref _searchKeyword, value);
    }

    public bool IsDialogOpen
    {
        get => _isDialogOpen;
        set => SetProperty(ref _isDialogOpen, value);
    }

    public UserEditDialogViewModel? CurrentDialogViewModel
    {
        get => _currentDialogViewModel;
        set => SetProperty(ref _currentDialogViewModel, value);
    }

    public int PageIndex
    {
        get => _pageIndex;
        set => SetProperty(ref _pageIndex, value);
    }

    public int PageSize
    {
        get => _pageSize;
        set => SetProperty(ref _pageSize, value);
    }

    public int TotalCount
    {
        get => _totalCount;
        set => SetProperty(ref _totalCount, value);
    }

    public int TotalPages => PageSize <= 0 ? 0 : (int)Math.Ceiling((double)TotalCount / PageSize);

    public bool CanGoPrevious => PageIndex > 1;

    public bool CanGoNext => PageIndex < TotalPages;

    public DelegateCommand LoadCommand { get; }
    public DelegateCommand SearchCommand { get; }
    public DelegateCommand PreviousPageCommand { get; }
    public DelegateCommand NextPageCommand { get; }
    public DelegateCommand AddUserCommand { get; }
    public DelegateCommand<User?> EditUserCommand { get; }
    public DelegateCommand<User?> DeleteUserCommand { get; }
    public DelegateCommand<User?> ResetPasswordCommand { get; }
    public DelegateCommand<User?> EnableUserCommand { get; }
    public DelegateCommand ExportCommand { get; }
    public DelegateCommand CloseProgramCommand { get; }

    public UserManagementViewModel(IUserService userService, IAuditLogService auditLogService, IUserSession userSession, IPasswordHasher passwordHasher)
    {
        _userService = userService ?? throw new ArgumentNullException(nameof(userService));
        _auditLogService = auditLogService ?? throw new ArgumentNullException(nameof(auditLogService));
        _userSession = userSession ?? throw new ArgumentNullException(nameof(userSession));
        _passwordHasher = passwordHasher ?? throw new ArgumentNullException(nameof(passwordHasher));

        LoadCommand = new DelegateCommand(async () => await LoadUsersAsync(1));
        SearchCommand = new DelegateCommand(async () => await LoadUsersAsync(1));
        PreviousPageCommand = new DelegateCommand(async () => await LoadUsersAsync(PageIndex - 1), () => CanGoPrevious)
            .ObservesProperty(() => PageIndex)
            .ObservesProperty(() => TotalCount);
        NextPageCommand = new DelegateCommand(async () => await LoadUsersAsync(PageIndex + 1), () => CanGoNext)
            .ObservesProperty(() => PageIndex)
            .ObservesProperty(() => TotalCount);
        AddUserCommand = new DelegateCommand(ShowAddUserDialog);
        EditUserCommand = new DelegateCommand<User?>(ShowEditUserDialog, CanEditUser);
        DeleteUserCommand = new DelegateCommand<User?>(async u => await DeleteUserAsync(u), CanDeleteUser);
        ResetPasswordCommand = new DelegateCommand<User?>(async u => await ResetPasswordAsync(u), CanResetPassword);
        EnableUserCommand = new DelegateCommand<User?>(async u => await EnableUserAsync(u), CanEnableUser);
        ExportCommand = new DelegateCommand(async () => await ExportAsync());
        CloseProgramCommand = new DelegateCommand(CloseProgram);
    }

    private void CloseProgram()
    {
        MessageBoxResult result = System.Windows.MessageBox.Show(
            "确定要关闭程序吗？",
            "确认关闭",
            MessageBoxButton.YesNo,
            MessageBoxImage.Question);

        if (result == MessageBoxResult.Yes)
        {
            Application.Current?.Shutdown();
        }
    }

    private async Task LoadUsersAsync(int targetPageIndex)
    {
        IsLoading = true;
        try
        {
            int normalizedPageIndex = targetPageIndex <= 0 ? 1 : targetPageIndex;
            var result = await _userService.GetUsersPagedAsync(normalizedPageIndex, PageSize, SearchKeyword);

            PageIndex = result.PageIndex;
            PageSize = result.PageSize;
            TotalCount = result.TotalCount;
            Users = new ObservableCollection<User>(result.Items);
            RaisePropertyChanged(nameof(TotalPages));
            RaisePropertyChanged(nameof(CanGoPrevious));
            RaisePropertyChanged(nameof(CanGoNext));
        }
        finally
        {
            IsLoading = false;
        }
    }

    private void ShowAddUserDialog()
    {
        CurrentDialogViewModel = new UserEditDialogViewModel(async result =>
        {
            IsDialogOpen = false;

            if (result && CurrentDialogViewModel?.EditUser is not null)
            {
                await SaveNewUserAsync(CurrentDialogViewModel.EditUser);
            }
        }, _passwordHasher);

        CurrentDialogViewModel.Initialize(null, true);
        IsDialogOpen = true;
    }

    private async Task SaveNewUserAsync(User user)
    {
        try
        {
            IsLoading = true;
            bool success = await _userService.CreateUserAsync(user);
            if (!success)
            {
                Growl.Error("创建用户失败");
                return;
            }

            Growl.Success("创建用户成功");
            await LoadUsersAsync(PageIndex);
        }
        catch (Exception ex) when (ExpectedOperationExceptionFilter.IsExpectedDataOperationException(ex))
        {
            Growl.Error($"创建用户失败: {ex.Message}");
        }
        finally
        {
            IsLoading = false;
        }
    }

    private void ShowEditUserDialog(User? user)
    {
        if (user is null)
        {
            return;
        }

        CurrentDialogViewModel = new UserEditDialogViewModel(async result =>
        {
            IsDialogOpen = false;

            if (result && CurrentDialogViewModel?.EditUser is not null)
            {
                await UpdateUserAsync(CurrentDialogViewModel.EditUser);
            }
        }, _passwordHasher);

        CurrentDialogViewModel.Initialize(user, false);
        IsDialogOpen = true;
    }

    private async Task UpdateUserAsync(User user)
    {
        try
        {
            IsLoading = true;
            bool success = await _userService.UpdateUserAsync(user);
            if (!success)
            {
                Growl.Error("更新用户失败");
                return;
            }

            Growl.Success("更新用户成功");
            await LoadUsersAsync(PageIndex);
        }
        catch (Exception ex) when (ExpectedOperationExceptionFilter.IsExpectedDataOperationException(ex))
        {
            Growl.Error($"更新用户失败: {ex.Message}");
        }
        finally
        {
            IsLoading = false;
        }
    }

    private static bool CanEditUser(User? user)
    {
        return user is not null;
    }

    private async Task DeleteUserAsync(User? user)
    {
        if (user is null)
        {
            return;
        }

        var result = System.Windows.MessageBox.Show(
            $"确定要删除用户 \"{user.Username}\" 吗？",
            "确认删除",
            MessageBoxButton.YesNo,
            MessageBoxImage.Warning);

        if (result != MessageBoxResult.Yes)
        {
            return;
        }

        try
        {
            IsLoading = true;
            bool success = await _userService.DeleteUserAsync(user.Id);
            if (!success)
            {
                Growl.Error("删除用户失败");
                return;
            }

            Growl.Success("删除用户成功");
            int reloadPage = Users.Count == 1 && PageIndex > 1 ? PageIndex - 1 : PageIndex;
            await LoadUsersAsync(reloadPage);
        }
        catch (Exception ex) when (ExpectedOperationExceptionFilter.IsExpectedDataOperationException(ex))
        {
            Growl.Error($"删除用户失败: {ex.Message}");
        }
        finally
        {
            IsLoading = false;
        }
    }

    private static bool CanDeleteUser(User? user)
    {
        return user is not null && user.Username != "admin";
    }

    private async Task ResetPasswordAsync(User? user)
    {
        if (user is null)
        {
            return;
        }

        var result = System.Windows.MessageBox.Show(
            $"确定要重置用户 \"{user.Username}\" 的登录密码吗？",
            "重置密码",
            MessageBoxButton.YesNo,
            MessageBoxImage.Question);

        if (result != MessageBoxResult.Yes)
        {
            return;
        }

        try
        {
            IsLoading = true;
            string newPassword = GenerateStrongPassword();
            bool success = await _userService.ResetPasswordAsync(user.Id, newPassword);
            if (!success)
            {
                Growl.Error("重置密码失败");
                return;
            }

            Growl.Success($"密码重置成功，初始密码为: {newPassword}\n请提醒用户并及时修改密码。");
        }
        catch (Exception ex) when (ExpectedOperationExceptionFilter.IsExpectedDataOperationException(ex))
        {
            Growl.Error($"重置密码失败: {ex.Message}");
        }
        finally
        {
            IsLoading = false;
        }
    }

    private static bool CanResetPassword(User? user)
    {
        return user is not null;
    }

    private async Task EnableUserAsync(User? user)
    {
        if (user is null)
        {
            return;
        }

        try
        {
            user.IsEnabled = !user.IsEnabled;
            bool success = await _userService.EnableUserAsync(user.Id, user.IsEnabled);
            if (!success)
            {
                user.IsEnabled = !user.IsEnabled;
                Growl.Error("操作失败");
                return;
            }

            string status = user.IsEnabled ? "启用" : "禁用";
            Growl.Success($"用户已{status}");
            RaisePropertyChanged(nameof(Users));
        }
        catch (Exception ex) when (ExpectedOperationExceptionFilter.IsExpectedDataOperationException(ex))
        {
            user.IsEnabled = !user.IsEnabled;
            Growl.Error($"操作失败: {ex.Message}");
        }
    }

    private static bool CanEnableUser(User? user)
    {
        return user is not null && user.Username != "admin";
    }

    private async Task ExportAsync()
    {
        try
        {
            var dialog = new SaveFileDialog
            {
                Filter = "CSV 文件(*.csv)|*.csv",
                FileName = $"用户列表导出_{DateTime.Now:yyyyMMdd_HHmmss}.csv"
            };

            if (dialog.ShowDialog() != true)
            {
                return;
            }

            List<User> users = await _userService.GetAllUsersAsync();
            var csv = new StringBuilder();
            csv.AppendLine("ID,用户名,真实姓名,邮箱,电话,是否启用,创建时间,最后登录时间");

            foreach (User user in users)
            {
                csv.AppendLine(
                    $"{user.Id},{user.Username},{user.RealName},{user.Email},{user.Phone},{user.IsEnabled},{user.CreatedTime:yyyy-MM-dd HH:mm},{user.LastLoginTime?.ToString("yyyy-MM-dd HH:mm") ?? string.Empty}");
            }

            File.WriteAllText(dialog.FileName, csv.ToString(), Encoding.UTF8);
            Growl.Success("导出成功");

            await _auditLogService.LogExportAsync(
                _userSession.GetAuditUserId(),
                _userSession.GetAuditUsername(),
                "UserManagement",
                "导出用户列表");
        }
        catch (Exception ex) when (ExpectedOperationExceptionFilter.IsExpectedUserOperationException(ex))
        {
            Growl.Error($"导出失败: {ex.Message}");
        }
    }

    private static string GenerateStrongPassword()
    {
        const string upperCase = "ABCDEFGHIJKLMNOPQRSTUVWXYZ";
        const string lowerCase = "abcdefghijklmnopqrstuvwxyz";
        const string digits = "0123456789";
        const string special = "!@#$%^&*";

        var allChars = upperCase + lowerCase + digits + special;
        var password = new char[12];

        password[0] = upperCase[GetRandomInt(upperCase.Length)];
        password[1] = lowerCase[GetRandomInt(lowerCase.Length)];
        password[2] = digits[GetRandomInt(digits.Length)];
        password[3] = special[GetRandomInt(special.Length)];

        for (int i = 4; i < password.Length; i++)
        {
            password[i] = allChars[GetRandomInt(allChars.Length)];
        }

        return new string(password.OrderBy(_ => GetRandomInt(password.Length)).ToArray());
    }

    private static int GetRandomInt(int maxValue)
    {
        return RandomNumberGenerator.GetInt32(maxValue);
    }
}

