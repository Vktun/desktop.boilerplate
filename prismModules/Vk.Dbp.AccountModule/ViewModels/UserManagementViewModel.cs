using System.IO;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using HandyControl.Controls;
using Microsoft.Win32;
using System.Windows;
using Prism.Commands;
using Prism.Mvvm;
using Vk.Dbp.AccountModule.Models;
using Vk.Dbp.AccountModule.Services;
using Vk.Dbp.Services.Audit;
using Vk.Dbp.Services.Session;

namespace Vk.Dbp.AccountModule.ViewModels;

public class UserManagementViewModel : BindableBase
{
    private readonly IUserService _userService;
    private readonly IAuditLogService _auditLogService;
    private readonly IUserSession _userSession;

    private ObservableCollection<User> _users = new();

    public ObservableCollection<User> Users
    {
        get => _users;
        set => SetProperty(ref _users, value);
    }

    private ObservableCollection<User> _allUsers = new();

    public ObservableCollection<User> AllUsers
    {
        get => _allUsers;
        set => SetProperty(ref _allUsers, value);
    }

    private User? _selectedUser;

    public User? SelectedUser
    {
        get => _selectedUser;
        set => SetProperty(ref _selectedUser, value);
    }

    private bool _isLoading;

    public bool IsLoading
    {
        get => _isLoading;
        set => SetProperty(ref _isLoading, value);
    }

    private string _searchKeyword = string.Empty;

    public string SearchKeyword
    {
        get => _searchKeyword;
        set
        {
            SetProperty(ref _searchKeyword, value);
            FilterUsers();
        }
    }

    private bool _isDialogOpen;

    public bool IsDialogOpen
    {
        get => _isDialogOpen;
        set => SetProperty(ref _isDialogOpen, value);
    }

    private UserEditDialogViewModel? _currentDialogViewModel;

    public UserEditDialogViewModel? CurrentDialogViewModel
    {
        get => _currentDialogViewModel;
        set => SetProperty(ref _currentDialogViewModel, value);
    }

    public DelegateCommand LoadCommand { get; }
    public DelegateCommand SearchCommand { get; }
    public DelegateCommand AddUserCommand { get; }
    public DelegateCommand<User?> EditUserCommand { get; }
    public DelegateCommand<User?> DeleteUserCommand { get; }
    public DelegateCommand<User?> ResetPasswordCommand { get; }
    public DelegateCommand<User?> EnableUserCommand { get; }
    public DelegateCommand ExportCommand { get; }
    public DelegateCommand CloseProgramCommand { get; }

    public UserManagementViewModel(IUserService userService, IAuditLogService auditLogService, IUserSession userSession)
    {
        _userService = userService ?? throw new ArgumentNullException(nameof(userService));
        _auditLogService = auditLogService ?? throw new ArgumentNullException(nameof(auditLogService));
        _userSession = userSession ?? throw new ArgumentNullException(nameof(userSession));

        LoadCommand = new DelegateCommand(async () => await LoadUsersAsync());
        SearchCommand = new DelegateCommand(FilterUsers);
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

    private async Task LoadUsersAsync()
    {
        IsLoading = true;
        try
        {
            List<User> users = await _userService.GetAllUsersAsync();
            AllUsers = new ObservableCollection<User>(users);
            Users = new ObservableCollection<User>(users);
        }
        finally
        {
            IsLoading = false;
        }
    }

    private void FilterUsers()
    {
        if (AllUsers.Count == 0)
        {
            return;
        }

        if (string.IsNullOrWhiteSpace(SearchKeyword))
        {
            Users = new ObservableCollection<User>(AllUsers);
            return;
        }

        List<User> filtered = AllUsers.Where(u =>
                (u.Username?.Contains(SearchKeyword, StringComparison.OrdinalIgnoreCase) ?? false) ||
                (u.RealName?.Contains(SearchKeyword, StringComparison.OrdinalIgnoreCase) ?? false) ||
                (u.Email?.Contains(SearchKeyword, StringComparison.OrdinalIgnoreCase) ?? false))
            .ToList();

        Users = new ObservableCollection<User>(filtered);
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
        });

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
            await LoadUsersAsync();
        }
        catch (Exception ex)
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
        });

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
            await LoadUsersAsync();
        }
        catch (Exception ex)
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
            $"确定要删除用户\"{user.Username}\"吗？",
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

            Users.Remove(user);
            if (AllUsers.Contains(user))
            {
                AllUsers.Remove(user);
            }

            Growl.Success("删除用户成功");
        }
        catch (Exception ex)
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
            $"确定要重置用户\"{user.Username}\"的登录密码吗？",
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
            // 生成随机强密码
            string newPassword = GenerateStrongPassword();
            bool success = await _userService.ResetPasswordAsync(user.Id, newPassword);
            if (!success)
            {
                Growl.Error("重置密码失败");
                return;
            }

            Growl.Success($"密码重置成功，初始密码为: {newPassword}\n请提醒用户并及时修改密码。");
        }
        catch (Exception ex)
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
        catch (Exception ex)
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

            var csv = new StringBuilder();
            csv.AppendLine("ID,用户名,真实姓名,邮箱,电话,是否启用,创建时间,最后登录时间");

            foreach (User user in Users)
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
        catch (Exception ex)
        {
            Growl.Error($"导出失败: {ex.Message}");
        }
    }

    /// <summary>
    /// 生成一个12位随机密码（含大小写字母、数字和符号）
    /// </summary>
    private static string GenerateStrongPassword()
    {
        const string upperCase = "ABCDEFGHIJKLMNOPQRSTUVWXYZ";
        const string lowerCase = "abcdefghijklmnopqrstuvwxyz";
        const string digits = "0123456789";
        const string special = "!@#$%^&*";

        var allChars = upperCase + lowerCase + digits + special;
        var random = new System.Security.Cryptography.RNGCryptoServiceProvider();
        var password = new char[12];

        // 强制覆盖每一类字符
        password[0] = upperCase[GetRandomInt(random, upperCase.Length)];
        password[1] = lowerCase[GetRandomInt(random, lowerCase.Length)];
        password[2] = digits[GetRandomInt(random, digits.Length)];
        password[3] = special[GetRandomInt(random, special.Length)];

        for (int i = 4; i < password.Length; i++)
        {
            password[i] = allChars[GetRandomInt(random, allChars.Length)];
        }

        // 打乱字符序列
        return new string(password.OrderBy(_ => GetRandomInt(random, password.Length)).ToArray());
    }

    private static int GetRandomInt(System.Security.Cryptography.RNGCryptoServiceProvider rng, int maxValue)
    {
        byte[] bytes = new byte[4];
        rng.GetBytes(bytes);
        return Math.Abs(BitConverter.ToInt32(bytes, 0)) % maxValue;
    }
}
