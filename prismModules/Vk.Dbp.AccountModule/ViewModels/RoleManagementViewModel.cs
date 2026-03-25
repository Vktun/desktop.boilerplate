using System;
using System.Collections.ObjectModel;
using System.Threading.Tasks;
using Prism.Commands;
using Prism.Mvvm;
using Vk.Dbp.AccountModule.Models;
using Vk.Dbp.AccountModule.Services;
using Vk.Dbp.Core.Audit.Interfaces;

namespace Vk.Dbp.AccountModule.ViewModels
{
    /// <summary>
    /// 角色管理ViewModel
    /// </summary>
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
            set { SetProperty(ref _selectedRole, value); }
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
            // TODO: 打开新增角色对话框
        }

        private void EditRole(Role role)
        {
            if (role == null)
                return;
            // TODO: 打开编辑角色对话框
        }

        private bool CanEditRole(Role role)
        {
            return role != null;
        }

        private async Task DeleteRole(Role role)
        {
            if (role == null || role.Id == 1)
                return;

            var result = await _roleService.DeleteRoleAsync(role.Id);
            if (result)
            {
                Roles.Remove(role);
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
            // TODO: 打开权限分配对话框
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
            await _roleService.EnableRoleAsync(role.Id, role.IsEnabled);
        }

        private bool CanEnableRole(Role role)
        {
            return role != null && role.Id != 1;
        }
    }
}