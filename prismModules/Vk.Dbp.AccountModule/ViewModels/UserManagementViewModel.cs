using System;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using HandyControl.Controls;
using Prism.Commands;
using Prism.Mvvm;
using Vk.Dbp.AccountModule.Models;
using Vk.Dbp.AccountModule.Services;
using Vk.Dbp.AccountModule.Views;
using Vk.Dbp.Core.Audit.Interfaces;
using Vk.Dbp.Core.Audit;

namespace Vk.Dbp.AccountModule.ViewModels
{
    public class UserManagementViewModel : BindableBase
    {
        private readonly IUserService _userService;
        private readonly IAuditLogService _auditLogService;

        private ObservableCollection<User> _users = new();
        public ObservableCollection<User> Users
        {
            get { return _users; }
            set { SetProperty(ref _users, value); }
        }

        private ObservableCollection<User> _allUsers = new();
        public ObservableCollection<User> AllUsers
        {
            get { return _allUsers; }
            set { SetProperty(ref _allUsers, value); }
        }

        private User? _selectedUser;
        public User? SelectedUser
        {
            get { return _selectedUser; }
            set { SetProperty(ref _selectedUser, value); }
        }

        private bool _isLoading;
        public bool IsLoading
        {
            get { return _isLoading; }
            set { SetProperty(ref _isLoading, value); }
        }

        private string _searchKeyword = "";
        public string SearchKeyword
        {
            get { return _searchKeyword; }
            set 
            { 
                SetProperty(ref _searchKeyword, value);
                FilterUsers();
            }
        }

        private bool _isDialogOpen;
        public bool IsDialogOpen
        {
            get { return _isDialogOpen; }
            set { SetProperty(ref _isDialogOpen, value); }
        }

        private UserEditDialogViewModel? _currentDialogViewModel;
        public UserEditDialogViewModel? CurrentDialogViewModel
        {
            get { return _currentDialogViewModel; }
            set { SetProperty(ref _currentDialogViewModel, value); }
        }

        public DelegateCommand LoadCommand { get; }
        public DelegateCommand SearchCommand { get; }
        public DelegateCommand AddUserCommand { get; }
        public DelegateCommand<User?> EditUserCommand { get; }
        public DelegateCommand<User?> DeleteUserCommand { get; }
        public DelegateCommand<User?> ResetPasswordCommand { get; }
        public DelegateCommand<User?> EnableUserCommand { get; }
        public DelegateCommand ExportCommand { get; }

        public UserManagementViewModel(IUserService userService, IAuditLogService auditLogService)
        {
            _userService = userService ?? throw new ArgumentNullException(nameof(userService));
            _auditLogService = auditLogService ?? throw new ArgumentNullException(nameof(auditLogService));

            LoadCommand = new DelegateCommand(async () => await LoadUsers());
            SearchCommand = new DelegateCommand(FilterUsers);
            AddUserCommand = new DelegateCommand(ShowAddUserDialog);
            EditUserCommand = new DelegateCommand<User?>(ShowEditUserDialog, CanEditUser);
            DeleteUserCommand = new DelegateCommand<User?>(async u => await DeleteUser(u), CanDeleteUser);
            ResetPasswordCommand = new DelegateCommand<User?>(async u => await ResetPassword(u), CanResetPassword);
            EnableUserCommand = new DelegateCommand<User?>(async u => await EnableUser(u), CanEnableUser);
            ExportCommand = new DelegateCommand(Export);
        }

        private async Task LoadUsers()
        {
            IsLoading = true;
            try
            {
                var users = await _userService.GetAllUsersAsync();
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
            if (AllUsers == null || AllUsers.Count == 0)
            {
                return;
            }

            if (string.IsNullOrWhiteSpace(SearchKeyword))
            {
                Users = new ObservableCollection<User>(AllUsers);
                return;
            }

            var filtered = AllUsers.Where(u =>
                (u.Username != null && u.Username.Contains(SearchKeyword, StringComparison.OrdinalIgnoreCase)) ||
                (u.RealName != null && u.RealName.Contains(SearchKeyword, StringComparison.OrdinalIgnoreCase)) ||
                (u.Email != null && u.Email.Contains(SearchKeyword, StringComparison.OrdinalIgnoreCase)))
                .ToList();

            Users = new ObservableCollection<User>(filtered);
        }

        private void ShowAddUserDialog()
        {
            CurrentDialogViewModel = new UserEditDialogViewModel(async result =>
            {
                IsDialogOpen = false;

                if (result && CurrentDialogViewModel?.EditUser != null)
                {
                    await SaveNewUser(CurrentDialogViewModel.EditUser);
                }
            });

            CurrentDialogViewModel.Initialize(null, true);
            IsDialogOpen = true;
        }

        private async Task SaveNewUser(User user)
        {
            try
            {
                IsLoading = true;
                var success = await _userService.CreateUserAsync(user);
                if (success)
                {
                    Growl.Success("用户创建成功");
                    await LoadUsers();
                    await _auditLogService.LogOperationAsync(1, "admin", AuditActionType.Create, 
                        "用户管理", $"创建用户: {user.Username}", "User", user.Id);
                }
                else
                {
                    Growl.Error("用户创建失败");
                }
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
            if (user == null)
                return;

            CurrentDialogViewModel = new UserEditDialogViewModel(async result =>
            {
                IsDialogOpen = false;

                if (result && CurrentDialogViewModel?.EditUser != null)
                {
                    await UpdateUser(CurrentDialogViewModel.EditUser);
                }
            });

            CurrentDialogViewModel.Initialize(user, false);
            IsDialogOpen = true;
        }

        private async Task UpdateUser(User user)
        {
            try
            {
                IsLoading = true;
                var success = await _userService.UpdateUserAsync(user);
                if (success)
                {
                    Growl.Success("用户更新成功");
                    await LoadUsers();
                    await _auditLogService.LogOperationAsync(1, "admin", AuditActionType.Update, 
                        "用户管理", $"更新用户: {user.Username}", "User", user.Id);
                }
                else
                {
                    Growl.Error("用户更新失败");
                }
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

        private bool CanEditUser(User? user)
        {
            return user != null;
        }

        private async Task DeleteUser(User? user)
        {
            if (user == null)
                return;

            var result = System.Windows.MessageBox.Show(
                $"确定要删除用户 \"{user.Username}\" 吗？\n此操作不可恢复。",
                "确认删除",
                MessageBoxButton.YesNo,
                MessageBoxImage.Warning);

            if (result == MessageBoxResult.Yes)
            {
                try
                {
                    IsLoading = true;
                    var success = await _userService.DeleteUserAsync(user.Id);
                    if (success)
                    {
                        Users.Remove(user);
                        if (AllUsers.Contains(user))
                        {
                            AllUsers.Remove(user);
                        }
                        Growl.Success("用户删除成功");
                        await _auditLogService.LogOperationAsync(1, "admin", AuditActionType.Delete, 
                            "用户管理", $"删除用户: {user.Username}", "User", user.Id);
                    }
                    else
                    {
                        Growl.Error("用户删除失败");
                    }
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
        }

        private bool CanDeleteUser(User? user)
        {
            return user != null && user.Username != "admin";
        }

        private async Task ResetPassword(User? user)
        {
            if (user == null)
                return;

            var result = System.Windows.MessageBox.Show(
                $"确定要重置用户 \"{user.Username}\" 的密码吗？\n密码将被重置为: 123456",
                "重置密码",
                MessageBoxButton.YesNo,
                MessageBoxImage.Question);

            if (result == MessageBoxResult.Yes)
            {
                try
                {
                    IsLoading = true;
                    var newPassword = "123456";
                    var success = await _userService.ResetPasswordAsync(user.Id, newPassword);
                    if (success)
                    {
                        Growl.Success($"密码已重置为: {newPassword}");
                        await _auditLogService.LogOperationAsync(1, "admin", AuditActionType.ChangePassword, 
                            "用户管理", $"重置用户密码: {user.Username}", "User", user.Id);
                    }
                    else
                    {
                        Growl.Error("密码重置失败");
                    }
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
        }

        private bool CanResetPassword(User? user)
        {
            return user != null;
        }

        private async Task EnableUser(User? user)
        {
            if (user == null)
                return;

            try
            {
                user.IsEnabled = !user.IsEnabled;
                var success = await _userService.EnableUserAsync(user.Id, user.IsEnabled);
                if (success)
                {
                    var status = user.IsEnabled ? "启用" : "禁用";
                    Growl.Success($"用户已{status}");
                    await _auditLogService.LogOperationAsync(1, "admin", AuditActionType.Update, 
                        "用户管理", $"{status}用户: {user.Username}", "User", user.Id);
                    RaisePropertyChanged(nameof(Users));
                }
                else
                {
                    user.IsEnabled = !user.IsEnabled;
                    Growl.Error("操作失败");
                }
            }
            catch (Exception ex)
            {
                user.IsEnabled = !user.IsEnabled;
                Growl.Error($"操作失败: {ex.Message}");
            }
        }

        private bool CanEnableUser(User? user)
        {
            return user != null && user.Username != "admin";
        }

        private void Export()
        {
            try
            {
                var dialog = new Microsoft.Win32.SaveFileDialog
                {
                    Filter = "CSV文件 (*.csv)|*.csv",
                    FileName = $"用户列表_{DateTime.Now:yyyyMMdd_HHmmss}.csv"
                };

                if (dialog.ShowDialog() == true)
                {
                    var csv = new System.Text.StringBuilder();
                    csv.AppendLine("ID,用户名,真实姓名,邮箱,电话,是否启用,创建时间,最后登录时间");

                    foreach (var user in Users)
                    {
                        csv.AppendLine($"{user.Id},{user.Username},{user.RealName},{user.Email},{user.Phone},{user.IsEnabled},{user.CreatedTime:yyyy-MM-dd HH:mm},{user.LastLoginTime?.ToString("yyyy-MM-dd HH:mm") ?? ""}");
                    }

                    System.IO.File.WriteAllText(dialog.FileName, csv.ToString(), System.Text.Encoding.UTF8);
                    Growl.Success("导出成功");
                    _ = _auditLogService.LogOperationAsync(1, "admin", AuditActionType.Export, 
                        "用户管理", "导出用户列表");
                }
            }
            catch (Exception ex)
            {
                Growl.Error($"导出失败: {ex.Message}");
            }
        }
    }
}
