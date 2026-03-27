using System;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;
using Prism.Commands;
using Prism.Mvvm;
using Vk.Dbp.AccountModule.Models;
using Vk.Dbp.AccountModule.Services;

namespace Vk.Dbp.AccountModule.ViewModels
{
    public class PermissionItem : BindableBase
    {
        public Permission Permission { get; set; }

        private bool _isSelected;
        public bool IsSelected
        {
            get { return _isSelected; }
            set { SetProperty(ref _isSelected, value); }
        }
    }

    public class PermissionAssignDialogViewModel : BindableBase
    {
        private readonly IRoleService _roleService;
        private readonly IPermissionService _permissionService;
        private Role _role;
        private string _dialogTitle;
        private string _roleInfo;
        private int _selectedCount;
        private Action<bool> _closeAction;

        public ObservableCollection<PermissionItem> Permissions { get; set; }

        public string DialogTitle
        {
            get { return _dialogTitle; }
            set { SetProperty(ref _dialogTitle, value); }
        }

        public string RoleInfo
        {
            get { return _roleInfo; }
            set { SetProperty(ref _roleInfo, value); }
        }

        public int SelectedCount
        {
            get { return _selectedCount; }
            set { SetProperty(ref _selectedCount, value); }
        }

        public DelegateCommand SelectAllCommand { get; }
        public DelegateCommand UnselectAllCommand { get; }
        public DelegateCommand ConfirmCommand { get; }
        public DelegateCommand CancelCommand { get; }

        public PermissionAssignDialogViewModel(IRoleService roleService, IPermissionService permissionService)
        {
            _roleService = roleService ?? throw new ArgumentNullException(nameof(roleService));
            _permissionService = permissionService ?? throw new ArgumentNullException(nameof(permissionService));

            Permissions = new ObservableCollection<PermissionItem>();

            SelectAllCommand = new DelegateCommand(() =>
            {
                foreach (var item in Permissions)
                {
                    item.IsSelected = true;
                }
                UpdateSelectedCount();
            });

            UnselectAllCommand = new DelegateCommand(() =>
            {
                foreach (var item in Permissions)
                {
                    item.IsSelected = false;
                }
                UpdateSelectedCount();
            });

            ConfirmCommand = new DelegateCommand(async () =>
            {
                var selectedPermissionIds = Permissions
                    .Where(p => p.IsSelected)
                    .Select(p => p.Permission.Id)
                    .ToList();

                var result = await _roleService.AssignPermissionsToRoleAsync(_role.Id, selectedPermissionIds);
                
                if (result)
                {
                    HandyControl.Controls.Growl.Success("权限分配成功!");
                    _closeAction?.Invoke(true);
                }
                else
                {
                    HandyControl.Controls.Growl.Error("权限分配失败!");
                }
            });

            CancelCommand = new DelegateCommand(() =>
            {
                _closeAction?.Invoke(false);
            });
        }

        public async Task InitializeAsync(Role role, Action<bool> closeAction)
        {
            _role = role ?? throw new ArgumentNullException(nameof(role));
            _closeAction = closeAction;

            DialogTitle = "分配权限";
            RoleInfo = $"角色: {_role.Name}";

            var allPermissions = await _permissionService.GetAllPermissionsAsync();
            var rolePermissions = await _roleService.GetRolePermissionsAsync(_role.Id);
            var rolePermissionIds = rolePermissions.Select(p => p.Id).ToList();

            Permissions.Clear();
            foreach (var permission in allPermissions)
            {
                var item = new PermissionItem
                {
                    Permission = permission,
                    IsSelected = rolePermissionIds.Contains(permission.Id)
                };
                item.PropertyChanged += (s, e) =>
                {
                    if (e.PropertyName == nameof(PermissionItem.IsSelected))
                    {
                        UpdateSelectedCount();
                    }
                };
                Permissions.Add(item);
            }

            UpdateSelectedCount();
        }

        private void UpdateSelectedCount()
        {
            SelectedCount = Permissions.Count(p => p.IsSelected);
        }
    }
}
