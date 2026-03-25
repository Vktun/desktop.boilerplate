using System;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;
using Prism.Commands;
using Prism.Mvvm;
using Vk.Dbp.AccountModule.Models;
using Vk.Dbp.AccountModule.Services;
using Vk.Dbp.Core.Audit.Interfaces;

namespace Vk.Dbp.AccountModule.ViewModels
{
    /// <summary>
    /// 用户管理ViewModel
    /// </summary>
    public class UserManagementViewModel : BindableBase
    {
        private readonly IUserService _userService;
        private readonly IAuditLogService _auditLogService;

        private ObservableCollection<User> _users;
        public ObservableCollection<User> Users
        {
            get { return _users; }
            set { SetProperty(ref _users, value); }
        }

        private User _selectedUser;
        public User SelectedUser
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

        public DelegateCommand LoadCommand { get; }
        public DelegateCommand<User> AddUserCommand { get; }
        public DelegateCommand<User> EditUserCommand { get; }
        public DelegateCommand<User> DeleteUserCommand { get; }
        public DelegateCommand<User> ResetPasswordCommand { get; }
        public DelegateCommand<User> EnableUserCommand { get; }
        public DelegateCommand ExportCommand { get; }

        public UserManagementViewModel(IUserService userService, IAuditLogService auditLogService)
        {
            _userService = userService ?? throw new ArgumentNullException(nameof(userService));
            _auditLogService = auditLogService ?? throw new ArgumentNullException(nameof(auditLogService));

            Users = new ObservableCollection<User>();

            LoadCommand = new DelegateCommand(async () => await LoadUsers());
            AddUserCommand = new DelegateCommand<User>(AddUser, CanAddUser);
            EditUserCommand = new DelegateCommand<User>(EditUser, CanEditUser);
            DeleteUserCommand = new DelegateCommand<User>(async u => await DeleteUser(u), CanDeleteUser);
            ResetPasswordCommand = new DelegateCommand<User>(async u => await ResetPassword(u), CanResetPassword);
            EnableUserCommand = new DelegateCommand<User>(async u => await EnableUser(u), CanEnableUser);
            ExportCommand = new DelegateCommand(Export);
        }

        private async Task LoadUsers()
        {
            IsLoading = true;
            try
            {
                var users = await _userService.GetAllUsersAsync();
                Users = new ObservableCollection<User>(users);
            }
            finally
            {
                IsLoading = false;
            }
        }

        private void FilterUsers()
        {
            if (string.IsNullOrEmpty(SearchKeyword))
            {
                LoadCommand.Execute();
                return;
            }

            var filtered = Users.Where(u =>
                u.Username.Contains(SearchKeyword) ||
                u.RealName.Contains(SearchKeyword) ||
                u.Email.Contains(SearchKeyword))
                .ToList();

            Users = new ObservableCollection<User>(filtered);
        }

        private void AddUser(User user)
        {
            // TODO: 打开新增用户对话框
        }

        private bool CanAddUser(User user)
        {
            return true;
        }

        private void EditUser(User user)
        {
            if (user == null)
                return;
            // TODO: 打开编辑用户对话框
        }

        private bool CanEditUser(User user)
        {
            return user != null;
        }

        private async Task DeleteUser(User user)
        {
            if (user == null)
                return;

            // TODO: 显示确认对话框
            var result = await _userService.DeleteUserAsync(user.Id);
            if (result)
            {
                Users.Remove(user);
            }
        }

        private bool CanDeleteUser(User user)
        {
            return user != null && user.Username != "admin";
        }

        private async Task ResetPassword(User user)
        {
            if (user == null)
                return;

            // TODO: 显示重置密码对话框
            var newPassword = "123456";
            await _userService.ResetPasswordAsync(user.Id, newPassword);
        }

        private bool CanResetPassword(User user)
        {
            return user != null;
        }

        private async Task EnableUser(User user)
        {
            if (user == null)
                return;

            user.IsEnabled = !user.IsEnabled;
            await _userService.EnableUserAsync(user.Id, user.IsEnabled);
        }

        private bool CanEnableUser(User user)
        {
            return user != null && user.Username != "admin";
        }

        private void Export()
        {
            // TODO: 导出用户列表
        }
    }
}