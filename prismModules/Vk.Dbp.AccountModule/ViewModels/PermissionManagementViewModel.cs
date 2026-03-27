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

        private ObservableCollection<Permission> _filteredPermissions;
        public ObservableCollection<Permission> FilteredPermissions
        {
            get { return _filteredPermissions; }
            set { SetProperty(ref _filteredPermissions, value); }
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

        private ObservableCollection<string> _modules;
        public ObservableCollection<string> Modules
        {
            get { return _modules; }
            set { SetProperty(ref _modules, value); }
        }

        private string _selectedModule;
        public string SelectedModule
        {
            get { return _selectedModule; }
            set
            {
                SetProperty(ref _selectedModule, value);
                FilterPermissions();
            }
        }

        private string _searchKeyword = "";
        public string SearchKeyword
        {
            get { return _searchKeyword; }
            set
            {
                SetProperty(ref _searchKeyword, value);
                FilterPermissions();
            }
        }

        private bool _isLoading;
        public bool IsLoading
        {
            get { return _isLoading; }
            set { SetProperty(ref _isLoading, value); }
        }

        private bool _isDialogOpen;
        public bool IsDialogOpen
        {
            get { return _isDialogOpen; }
            set { SetProperty(ref _isDialogOpen, value); }
        }

        private Permission _editingPermission;
        public Permission EditingPermission
        {
            get { return _editingPermission; }
            set { SetProperty(ref _editingPermission, value); }
        }

        private bool _isEditMode;
        public bool IsEditMode
        {
            get { return _isEditMode; }
            set { SetProperty(ref _isEditMode, value); }
        }

        private string _dialogTitle;
        public string DialogTitle
        {
            get { return _dialogTitle; }
            set { SetProperty(ref _dialogTitle, value); }
        }

        public DelegateCommand LoadCommand { get; }
        public DelegateCommand AddPermissionCommand { get; }
        public DelegateCommand<Permission> EditPermissionCommand { get; }
        public DelegateCommand<Permission> DeletePermissionCommand { get; }
        public DelegateCommand<Permission> ToggleEnabledCommand { get; }
        public DelegateCommand SavePermissionCommand { get; }
        public DelegateCommand CancelDialogCommand { get; }
        public DelegateCommand ResetFilterCommand { get; }

        public PermissionManagementViewModel(IPermissionService permissionService, IAuditLogService auditLogService)
        {
            _permissionService = permissionService ?? throw new ArgumentNullException(nameof(permissionService));
            _auditLogService = auditLogService ?? throw new ArgumentNullException(nameof(auditLogService));

            Permissions = new ObservableCollection<Permission>();
            FilteredPermissions = new ObservableCollection<Permission>();
            PermissionTypes = new ObservableCollection<PermissionType>(
                Enum.GetValues(typeof(PermissionType)).Cast<PermissionType>()
            );
            Modules = new ObservableCollection<string>();

            LoadCommand = new DelegateCommand(async () => await LoadPermissions());
            AddPermissionCommand = new DelegateCommand(AddPermission);
            EditPermissionCommand = new DelegateCommand<Permission>(EditPermission, CanEditPermission);
            DeletePermissionCommand = new DelegateCommand<Permission>(async p => await DeletePermission(p), CanDeletePermission);
            ToggleEnabledCommand = new DelegateCommand<Permission>(async p => await ToggleEnabled(p), CanToggleEnabled);
            SavePermissionCommand = new DelegateCommand(async () => await SavePermission());
            CancelDialogCommand = new DelegateCommand(CancelDialog);
            ResetFilterCommand = new DelegateCommand(ResetFilter);
        }

        private async Task LoadPermissions()
        {
            IsLoading = true;
            try
            {
                var permissions = await _permissionService.GetAllPermissionsAsync();
                Permissions = new ObservableCollection<Permission>(permissions);
                
                var moduleList = permissions
                    .Where(p => !string.IsNullOrEmpty(p.Module))
                    .Select(p => p.Module)
                    .Distinct()
                    .OrderBy(m => m)
                    .ToList();
                Modules = new ObservableCollection<string>(moduleList);
                
                FilterPermissions();
            }
            finally
            {
                IsLoading = false;
            }
        }

        private void FilterPermissions()
        {
            var query = Permissions.AsEnumerable();

            if (_selectedPermissionType.HasValue)
            {
                query = query.Where(p => p.Type == _selectedPermissionType.Value);
            }

            if (!string.IsNullOrEmpty(_selectedModule))
            {
                query = query.Where(p => p.Module == _selectedModule);
            }

            if (!string.IsNullOrEmpty(_searchKeyword))
            {
                query = query.Where(p =>
                    p.Name.Contains(_searchKeyword, StringComparison.OrdinalIgnoreCase) ||
                    p.Code.Contains(_searchKeyword, StringComparison.OrdinalIgnoreCase) ||
                    (p.Description != null && p.Description.Contains(_searchKeyword, StringComparison.OrdinalIgnoreCase)));
            }

            FilteredPermissions = new ObservableCollection<Permission>(query.ToList());
        }

        private void ResetFilter()
        {
            SelectedPermissionType = null;
            SelectedModule = null;
            SearchKeyword = "";
            FilterPermissions();
        }

        private void AddPermission()
        {
            IsEditMode = false;
            DialogTitle = "新增权限";
            EditingPermission = new Permission
            {
                IsEnabled = true,
                Type = PermissionType.Menu,
                SortOrder = 0,
                CreatedTime = DateTime.Now
            };
            IsDialogOpen = true;
        }

        private void EditPermission(Permission permission)
        {
            if (permission == null)
                return;

            IsEditMode = true;
            DialogTitle = "编辑权限";
            EditingPermission = new Permission
            {
                Id = permission.Id,
                Name = permission.Name,
                Code = permission.Code,
                Type = permission.Type,
                Description = permission.Description,
                Module = permission.Module,
                Icon = permission.Icon,
                SortOrder = permission.SortOrder,
                ParentId = permission.ParentId,
                IsEnabled = permission.IsEnabled,
                CreatedTime = permission.CreatedTime
            };
            IsDialogOpen = true;
        }

        private bool CanEditPermission(Permission permission)
        {
            return permission != null;
        }

        private async Task SavePermission()
        {
            if (EditingPermission == null)
                return;

            if (string.IsNullOrWhiteSpace(EditingPermission.Name))
            {
                return;
            }

            if (string.IsNullOrWhiteSpace(EditingPermission.Code))
            {
                return;
            }

            IsLoading = true;
            try
            {
                bool result;
                if (IsEditMode)
                {
                    result = await _permissionService.UpdatePermissionAsync(EditingPermission);
                    if (result)
                    {
                        var existing = Permissions.FirstOrDefault(p => p.Id == EditingPermission.Id);
                        if (existing != null)
                        {
                            var index = Permissions.IndexOf(existing);
                            Permissions[index] = EditingPermission;
                        }
                    }
                }
                else
                {
                    result = await _permissionService.CreatePermissionAsync(EditingPermission);
                    if (result)
                    {
                        Permissions.Add(EditingPermission);
                    }
                }

                if (result)
                {
                    FilterPermissions();
                    IsDialogOpen = false;
                    EditingPermission = null;
                }
            }
            finally
            {
                IsLoading = false;
            }
        }

        private void CancelDialog()
        {
            IsDialogOpen = false;
            EditingPermission = null;
        }

        private async Task DeletePermission(Permission permission)
        {
            if (permission == null)
                return;

            IsLoading = true;
            try
            {
                var result = await _permissionService.DeletePermissionAsync(permission.Id);
                if (result)
                {
                    Permissions.Remove(permission);
                    FilterPermissions();
                }
            }
            finally
            {
                IsLoading = false;
            }
        }

        private bool CanDeletePermission(Permission permission)
        {
            return permission != null;
        }

        private async Task ToggleEnabled(Permission permission)
        {
            if (permission == null)
                return;

            permission.IsEnabled = !permission.IsEnabled;
            await _permissionService.EnablePermissionAsync(permission.Id, permission.IsEnabled);
        }

        private bool CanToggleEnabled(Permission permission)
        {
            return permission != null;
        }
    }
}
