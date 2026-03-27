using System;
using Prism.Commands;
using Prism.Mvvm;
using Vk.Dbp.AccountModule.Models;
using Vk.Dbp.AccountModule.Services;

namespace Vk.Dbp.AccountModule.ViewModels
{
    public class RoleEditDialogViewModel : BindableBase
    {
        private readonly IRoleService _roleService;
        private Role _role;
        private bool _isAddMode;
        private string _dialogTitle;
        private Action<bool> _closeAction;

        public Role Role
        {
            get { return _role; }
            set { SetProperty(ref _role, value); }
        }

        public bool IsAddMode
        {
            get { return _isAddMode; }
            set { SetProperty(ref _isAddMode, value); }
        }

        public string DialogTitle
        {
            get { return _dialogTitle; }
            set { SetProperty(ref _dialogTitle, value); }
        }

        public DelegateCommand ConfirmCommand { get; }
        public DelegateCommand CancelCommand { get; }

        public RoleEditDialogViewModel(IRoleService roleService)
        {
            _roleService = roleService ?? throw new ArgumentNullException(nameof(roleService));
            
            ConfirmCommand = new DelegateCommand(async () =>
            {
                if (string.IsNullOrWhiteSpace(Role.Name))
                {
                    HandyControl.Controls.Growl.Warning("角色名称不能为空!");
                    return;
                }

                bool result;
                if (IsAddMode)
                {
                    result = await _roleService.CreateRoleAsync(Role);
                    if (result)
                    {
                        HandyControl.Controls.Growl.Success("角色创建成功!");
                        _closeAction?.Invoke(true);
                    }
                    else
                    {
                        HandyControl.Controls.Growl.Error("角色创建失败!");
                    }
                }
                else
                {
                    result = await _roleService.UpdateRoleAsync(Role);
                    if (result)
                    {
                        HandyControl.Controls.Growl.Success("角色更新成功!");
                        _closeAction?.Invoke(true);
                    }
                    else
                    {
                        HandyControl.Controls.Growl.Error("角色更新失败!");
                    }
                }
            });

            CancelCommand = new DelegateCommand(() =>
            {
                _closeAction?.Invoke(false);
            });
        }

        public void Initialize(Role role, bool isAddMode, Action<bool> closeAction)
        {
            _closeAction = closeAction;
            IsAddMode = isAddMode;
            DialogTitle = isAddMode ? "新增角色" : "编辑角色";
            
            if (isAddMode)
            {
                Role = new Role
                {
                    IsEnabled = true,
                    CreatedTime = DateTime.Now
                };
            }
            else
            {
                Role = role ?? throw new ArgumentNullException(nameof(role));
            }
        }
    }
}
