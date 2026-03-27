using System;
using System.Collections.ObjectModel;
using System.Threading.Tasks;
using Prism.Commands;
using Prism.Mvvm;
using Vk.Dbp.AccountModule.Models;
using Vk.Dbp.AccountModule.Services;
using Vk.Dbp.AccountModule.Views;
using Vk.Dbp.Core.Audit.Interfaces;

namespace Vk.Dbp.AccountModule.ViewModels
{
    public class RoleManagementViewModel : BindableBase
    {
        private readonly IRoleService _roleService;
        private readonly IPermissionService _permissionService;
        private readonly IAuditLogService _auditLogService;

        private ObservableCollection<Role> _roles;
        public ObservableCollection<Role> Roles
        {
            get { return _roles; }
            set { SetProperty(ref _roles, value); }
        }

        private Role _selectedRole;
        public Role SelectedRole
        {
            get { return _selectedRole; }
            set 
            { 
                SetProperty(ref _selectedRole, value);
                RaiseCanExecuteChanged();
            }
        }

        private ObservableCollection<Permission> _availablePermissions;
        public ObservableCollection<Permission> AvailablePermissions
        {
            get { return _availablePermissions; }
            set { SetProperty(ref _availablePermissions, value); }
        }

        private bool _isLoading;
        public bool IsLoading
        {
            get { return _isLoading; }
            set { SetProperty(ref _isLoading, value); }
        }

        public DelegateCommand LoadCommand { get; }
        public DelegateCommand AddRoleCommand { get; }
        public DelegateCommand<Role> EditRoleCommand { get; }
        public DelegateCommand<Role> DeleteRoleCommand { get; }
        public DelegateCommand<Role> AssignPermissionsCommand { get; }
        public DelegateCommand<Role> EnableRoleCommand { get; }

        public RoleManagementViewModel(IRoleService roleService, IPermissionService permissionService,
            IAuditLogService auditLogService)
        {
            _roleService = roleService ?? throw new ArgumentNullException(nameof(roleService));
            _permissionService = permissionService ?? throw new ArgumentNullException(nameof(permissionService));
            _auditLogService = auditLogService ?? throw new ArgumentNullException(nameof(auditLogService));

            Roles = new ObservableCollection<Role>();
            AvailablePermissions = new ObservableCollection<Permission>();

            LoadCommand = new DelegateCommand(async () => await LoadRoles());
            AddRoleCommand = new DelegateCommand(AddRole);
            EditRoleCommand = new DelegateCommand<Role>(EditRole, CanEditRole);
            DeleteRoleCommand = new DelegateCommand<Role>(async r => await DeleteRole(r), CanDeleteRole);
            AssignPermissionsCommand = new DelegateCommand<Role>(async r => await AssignPermissions(r), CanAssignPermissions);
            EnableRoleCommand = new DelegateCommand<Role>(async r => await EnableRole(r), CanEnableRole);
        }

        private async Task LoadRoles()
        {
            IsLoading = true;
            try
            {
                var roles = await _roleService.GetAllRolesAsync();
                Roles = new ObservableCollection<Role>(roles);

                var permissions = await _permissionService.GetAllPermissionsAsync();
                AvailablePermissions = new ObservableCollection<Permission>(permissions);
            }
            finally
            {
                IsLoading = false;
            }
        }

        private void AddRole()
        {
            var dialog = new RoleEditDialog();
            var viewModel = dialog.DataContext as RoleEditDialogViewModel;
            
            viewModel?.Initialize(null, true, async (result) =>
            {
                if (result)
                {
                    await LoadRoles();
                }
                dialog.Close();
            });

            dialog.ShowDialog();
        }

        private void EditRole(Role role)
        {
            if (role == null)
                return;

            var dialog = new RoleEditDialog();
            var viewModel = dialog.DataContext as RoleEditDialogViewModel;
            
            var roleCopy = new Role
            {
                Id = role.Id,
                Name = role.Name,
                Description = role.Description,
                IsEnabled = role.IsEnabled,
                CreatedTime = role.CreatedTime,
                Remarks = role.Remarks
            };

            viewModel?.Initialize(roleCopy, false, async (result) =>
            {
                if (result)
                {
                    await LoadRoles();
                }
                dialog.Close();
            });

            dialog.ShowDialog();
        }

        private bool CanEditRole(Role role)
        {
            return role != null;
        }

        private async Task DeleteRole(Role role)
        {
            if (role == null || role.Id == 1)
                return;

            var result = HandyControl.Controls.MessageBox.Show(
                $"确定要删除角色 '{role.Name}' 吗?",
                "确认删除",
                System.Windows.MessageBoxButton.YesNo,
                System.Windows.MessageBoxImage.Question);

            if (result == System.Windows.MessageBoxResult.Yes)
            {
                var success = await _roleService.DeleteRoleAsync(role.Id);
                if (success)
                {
                    Roles.Remove(role);
                    HandyControl.Controls.Growl.Success("角色删除成功!");
                }
                else
                {
                    HandyControl.Controls.Growl.Error("角色删除失败!");
                }
            }
        }

        private bool CanDeleteRole(Role role)
        {
            return role != null && role.Id != 1;
        }

        private async Task AssignPermissions(Role role)
        {
            if (role == null)
                return;

            var dialog = new PermissionAssignDialog();
            var viewModel = dialog.DataContext as PermissionAssignDialogViewModel;
            
            await viewModel?.InitializeAsync(role, async (result) =>
            {
                if (result)
                {
                    await LoadRoles();
                }
                dialog.Close();
            });

            dialog.ShowDialog();
        }

        private bool CanAssignPermissions(Role role)
        {
            return role != null;
        }

        private async Task EnableRole(Role role)
        {
            if (role == null)
                return;

            role.IsEnabled = !role.IsEnabled;
            var success = await _roleService.EnableRoleAsync(role.Id, role.IsEnabled);
            
            if (success)
            {
                HandyControl.Controls.Growl.Success($"角色{(role.IsEnabled ? "启用" : "禁用")}成功!");
            }
            else
            {
                role.IsEnabled = !role.IsEnabled;
                HandyControl.Controls.Growl.Error($"角色{(role.IsEnabled ? "启用" : "禁用")}失败!");
            }
        }

        private bool CanEnableRole(Role role)
        {
            return role != null && role.Id != 1;
        }

        private void RaiseCanExecuteChanged()
        {
            EditRoleCommand.RaiseCanExecuteChanged();
            DeleteRoleCommand.RaiseCanExecuteChanged();
            AssignPermissionsCommand.RaiseCanExecuteChanged();
            EnableRoleCommand.RaiseCanExecuteChanged();
        }
    }
}