using System;
using System.Security.Cryptography;
using System.Text;
using System.Windows.Controls;
using Prism.Commands;
using Prism.Mvvm;
using Vk.Dbp.AccountModule.Models;

namespace Vk.Dbp.AccountModule.ViewModels
{
    public class UserEditDialogViewModel : BindableBase
    {
        private readonly Action<bool> _closeCallback;

        private User _editUser = new();
        public User EditUser
        {
            get { return _editUser; }
            set { SetProperty(ref _editUser, value); }
        }

        private bool _isNewUser;
        public bool IsNewUser
        {
            get { return _isNewUser; }
            set 
            { 
                SetProperty(ref _isNewUser, value);
                RaisePropertyChanged(nameof(DialogTitle));
            }
        }

        public string DialogTitle => IsNewUser ? "新增用户" : "编辑用户";

        private string _errorMessage = string.Empty;
        public string ErrorMessage
        {
            get { return _errorMessage; }
            set 
            { 
                SetProperty(ref _errorMessage, value);
                ShowError = !string.IsNullOrEmpty(value);
            }
        }

        private bool _showError;
        public bool ShowError
        {
            get { return _showError; }
            set { SetProperty(ref _showError, value); }
        }

        public DelegateCommand CancelCommand { get; }
        public DelegateCommand<PasswordBox> SaveCommand { get; }

        public UserEditDialogViewModel(Action<bool> closeCallback)
        {
            _closeCallback = closeCallback;
            CancelCommand = new DelegateCommand(Cancel);
            SaveCommand = new DelegateCommand<PasswordBox>(Save);
        }

        public void Initialize(User? user, bool isNew)
        {
            IsNewUser = isNew;
            if (isNew)
            {
                EditUser = new User
                {
                    IsEnabled = true,
                    CreatedTime = DateTime.Now
                };
            }
            else if (user != null)
            {
                EditUser = new User
                {
                    Id = user.Id,
                    Username = user.Username,
                    RealName = user.RealName,
                    Email = user.Email,
                    Phone = user.Phone,
                    IsEnabled = user.IsEnabled,
                    Remarks = user.Remarks,
                    CreatedTime = user.CreatedTime,
                    LastModifiedTime = user.LastModifiedTime,
                    LastLoginTime = user.LastLoginTime,
                    RoleIds = new System.Collections.Generic.List<int>(user.RoleIds)
                };
            }
            ErrorMessage = string.Empty;
        }

        private void Cancel()
        {
            _closeCallback?.Invoke(false);
        }

        private void Save(PasswordBox? passwordBox)
        {
            if (!Validate(passwordBox))
                return;

            if (IsNewUser && passwordBox != null)
            {
                EditUser.PasswordHash = HashPassword(passwordBox.Password);
            }

            _closeCallback?.Invoke(true);
        }

        private bool Validate(PasswordBox? passwordBox)
        {
            if (string.IsNullOrWhiteSpace(EditUser.Username))
            {
                ErrorMessage = "用户名不能为空";
                return false;
            }

            if (string.IsNullOrWhiteSpace(EditUser.RealName))
            {
                ErrorMessage = "真实姓名不能为空";
                return false;
            }

            if (IsNewUser)
            {
                if (passwordBox == null || string.IsNullOrWhiteSpace(passwordBox.Password))
                {
                    ErrorMessage = "密码不能为空";
                    return false;
                }

                if (passwordBox.Password.Length < 6)
                {
                    ErrorMessage = "密码长度不能少于6位";
                    return false;
                }
            }

            return true;
        }

        private string HashPassword(string password)
        {
            using (var sha256 = SHA256.Create())
            {
                var bytes = sha256.ComputeHash(Encoding.UTF8.GetBytes(password));
                return Convert.ToBase64String(bytes);
            }
        }
    }
}
