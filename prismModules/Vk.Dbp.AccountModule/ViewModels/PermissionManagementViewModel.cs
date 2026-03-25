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
    /// 权限管理ViewModel
    /// </summary>
    public class PermissionManagementViewModel : BindableBase
    {
        private readonly IPermissionService _permissionService;
        private readonly IAuditLogService _auditLogService;

        private ObservableCollection<Permission> _permissions;
        public ObservableCollection<Permission> Permissions
        {
            get { return _permissions; }
            set { SetProperty(ref _permissions, value); }
        }

        private Permission _selectedPermission;
        public Permission SelectedPermission
        {
            get { return _selectedPermission; }
            set { SetProperty(ref _selectedPermission, value); }
        }

        private ObservableCollection<PermissionType> _permissionTypes;
        public ObservableCollection<PermissionType> PermissionTypes
        {
            get { return _permissionTypes; }
            set { SetProperty(ref _permissionTypes, value); }
        }

        private PermissionType? _selectedPermissionType;
        public PermissionType? SelectedPermissionType
        {
            get { return _selectedPermissionType; }
            set
            {
                SetProperty(ref _selectedPermissionType, value);
                FilterPermissions();
            }
        }

        private bool _isLoading;
        public bool IsLoading
        {
            get { return _isLoading; }
            set { SetProperty(ref _isLoading, value); }
        }

        public DelegateCommand LoadCommand { get; }
        public DelegateCommand AddPermissionCommand { get; }
        public DelegateCommand<Permission> EditPermissionCommand { get; }
        public DelegateCommand<Permission> DeletePermissionCommand { get; }

        public PermissionManagementViewModel(IPermissionService permissionService, IAuditLogService auditLogService)
        {
            _permissionService = permissionService ?? throw new ArgumentNullException(nameof(permissionService));
            _auditLogService = auditLogService ?? throw new ArgumentNullException(nameof(auditLogService));

            Permissions = new ObservableCollection<Permission>();
            PermissionTypes = new ObservableCollection<PermissionType>(
                Enum.GetValues(typeof(PermissionType)).Cast<PermissionType>()
            );

            LoadCommand = new DelegateCommand(async () => await LoadPermissions());
            AddPermissionCommand = new DelegateCommand(AddPermission);
            EditPermissionCommand = new DelegateCommand<Permission>(EditPermission, CanEditPermission);
            DeletePermissionCommand = new DelegateCommand<Permission>(async p => await DeletePermission(p), CanDeletePermission);
        }

        private async Task LoadPermissions()
        {
            IsLoading = true;
            try
            {
                var permissions = await _permissionService.GetAllPermissionsAsync();
                Permissions = new ObservableCollection<Permission>(permissions);
            }
            finally
            {
                IsLoading = false;
            }
        }

        private void FilterPermissions()
        {
            if (!_selectedPermissionType.HasValue)
            {
                LoadCommand.Execute();
                return;
            }

            var filtered = Permissions.Where(p => p.Type == _selectedPermissionType.Value).ToList();
            Permissions = new ObservableCollection<Permission>(filtered);
        }

        private void AddPermission()
        {
            // TODO: 打开新增权限对话框
        }

        private void EditPermission(Permission permission)
        {
            if (permission == null)
                return;
            // TODO: 打开编辑权限对话框
        }

        private bool CanEditPermission(Permission permission)
        {
            return permission != null;
        }

        private async Task DeletePermission(Permission permission)
        {
            if (permission == null)
                return;

            // TODO: 显示确认对话框
            var result = await _permissionService.DeletePermissionAsync(permission.Id);
            if (result)
            {
                Permissions.Remove(permission);
            }
        }

        private bool CanDeletePermission(Permission permission)
        {
            return permission != null;
        }
    }
}
