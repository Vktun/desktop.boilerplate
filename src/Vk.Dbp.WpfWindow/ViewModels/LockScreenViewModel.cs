using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using Dabp.Utils.Exceptions;
using Prism.Commands;
using Prism.Mvvm;
using Dabp.WpfWindow.Services;

namespace Dabp.WpfWindow.ViewModels
{
    /// <summary>
    /// 锁屏界面视图模型
    /// </summary>
    public class LockScreenViewModel : BindableBase
    {
        private readonly ILockScreenService _lockScreenService;

        private string _username = string.Empty;
        private string _realName = string.Empty;
        private string _lockReason = string.Empty;
        private DateTime _lockTime = DateTime.Now;
        private string? _errorMessage;
        private bool _showError;
        private bool _isUnlocking;

        /// <summary>
        /// 用户名
        /// </summary>
        public string Username
        {
            get => _username;
            set => SetProperty(ref _username, value);
        }

        /// <summary>
        /// 真实姓名
        /// </summary>
        public string RealName
        {
            get => _realName;
            set => SetProperty(ref _realName, value);
        }

        /// <summary>
        /// 锁屏原因
        /// </summary>
        public string LockReason
        {
            get => _lockReason;
            set => SetProperty(ref _lockReason, value);
        }

        /// <summary>
        /// 锁屏时间
        /// </summary>
        public DateTime LockTime
        {
            get => _lockTime;
            set => SetProperty(ref _lockTime, value);
        }

        /// <summary>
        /// 锁屏时间显示文本
        /// </summary>
        public string LockTimeText => $"锁定时间: {LockTime:yyyy-MM-dd HH:mm:ss}";

        /// <summary>
        /// 错误消息
        /// </summary>
        public string? ErrorMessage
        {
            get => _errorMessage;
            set => SetProperty(ref _errorMessage, value);
        }

        /// <summary>
        /// 是否显示错误
        /// </summary>
        public bool ShowError
        {
            get => _showError;
            set => SetProperty(ref _showError, value);
        }

        /// <summary>
        /// 是否正在解锁
        /// </summary>
        public bool IsUnlocking
        {
            get => _isUnlocking;
            set => SetProperty(ref _isUnlocking, value);
        }

        /// <summary>
        /// 解锁命令
        /// </summary>
        public ICommand UnlockCommand { get; }

        /// <summary>
        /// 退出程序命令
        /// </summary>
        public ICommand ExitCommand { get; }

        public LockScreenViewModel(ILockScreenService lockScreenService)
        {
            _lockScreenService = lockScreenService ?? throw new ArgumentNullException(nameof(lockScreenService));

            UnlockCommand = new DelegateCommand<PasswordBox>(ExecuteUnlock, CanExecuteUnlock);
            ExitCommand = new DelegateCommand(ExecuteExit);
        }

        /// <summary>
        /// 初始化视图模型
        /// </summary>
        public void Initialize(string username, string realName, string reason)
        {
            Username = username;
            RealName = realName;
            LockReason = reason;
            LockTime = DateTime.Now;
            ShowError = false;
            ErrorMessage = null;
        }

        /// <summary>
        /// 是否可以执行解锁
        /// </summary>
        private bool CanExecuteUnlock(PasswordBox? passwordBox)
        {
            return !IsUnlocking && passwordBox != null && !string.IsNullOrEmpty(passwordBox.Password);
        }

        /// <summary>
        /// 执行解锁
        /// </summary>
        private void ExecuteUnlock(PasswordBox? passwordBox)
        {
            if (passwordBox == null || string.IsNullOrEmpty(passwordBox.Password))
                return;

            IsUnlocking = true;
            ShowError = false;

            try
            {
                bool success = _lockScreenService.Unlock(passwordBox.Password);
                if (!success)
                {
                    ShowError = true;
                    ErrorMessage = "密码错误，请重新输入";
                    passwordBox.Clear();
                }
            }
            catch (Exception ex) when (ExpectedOperationExceptionFilter.IsExpectedUserOperationException(ex))
            {
                ShowError = true;
                ErrorMessage = $"解锁失败: {ex.Message}";
            }
            finally
            {
                IsUnlocking = false;
                (UnlockCommand as DelegateCommand<PasswordBox>)?.RaiseCanExecuteChanged();
            }
        }

        /// <summary>
        /// 执行退出程序
        /// </summary>
        private void ExecuteExit()
        {
            Application.Current.Shutdown();
        }
    }
}
