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
    public class OrganizationManagementViewModel : BindableBase
    {
        private readonly IOrganizationService _organizationService;
        private readonly IUserService _userService;

        private ObservableCollection<OrganizationUnitModel> _organizationTree;
        public ObservableCollection<OrganizationUnitModel> OrganizationTree
        {
            get { return _organizationTree; }
            set { SetProperty(ref _organizationTree, value); }
        }

        private OrganizationUnitModel _selectedOrganization;
        public OrganizationUnitModel SelectedOrganization
        {
            get { return _selectedOrganization; }
            set
            {
                SetProperty(ref _selectedOrganization, value);
                RaiseCanExecuteChanged();
                if (value != null)
                {
                    _ = LoadOrganizationUsersAsync(value.Id);
                }
            }
        }

        private ObservableCollection<User> _organizationUsers;
        public ObservableCollection<User> OrganizationUsers
        {
            get { return _organizationUsers; }
            set { SetProperty(ref _organizationUsers, value); }
        }

        private ObservableCollection<User> _allUsers;
        public ObservableCollection<User> AllUsers
        {
            get { return _allUsers; }
            set { SetProperty(ref _allUsers, value); }
        }

        private ObservableCollection<User> _availableUsers;
        public ObservableCollection<User> AvailableUsers
        {
            get { return _availableUsers; }
            set { SetProperty(ref _availableUsers, value); }
        }

        private User _selectedUser;
        public User SelectedUser
        {
            get { return _selectedUser; }
            set
            {
                SetProperty(ref _selectedUser, value);
                RemoveUserCommand.RaiseCanExecuteChanged();
            }
        }

        private User _selectedAvailableUser;
        public User SelectedAvailableUser
        {
            get { return _selectedAvailableUser; }
            set
            {
                SetProperty(ref _selectedAvailableUser, value);
                AddUserCommand.RaiseCanExecuteChanged();
            }
        }

        private bool _isLoading;
        public bool IsLoading
        {
            get { return _isLoading; }
            set { SetProperty(ref _isLoading, value); }
        }

        private string _newOrgName;
        public string NewOrgName
        {
            get { return _newOrgName; }
            set { SetProperty(ref _newOrgName, value); }
        }

        private string _newOrgCode;
        public string NewOrgCode
        {
            get { return _newOrgCode; }
            set { SetProperty(ref _newOrgCode, value); }
        }

        public DelegateCommand LoadCommand { get; }
        public DelegateCommand AddRootOrganizationCommand { get; }
        public DelegateCommand AddChildOrganizationCommand { get; }
        public DelegateCommand EditOrganizationCommand { get; }
        public DelegateCommand DeleteOrganizationCommand { get; }
        public DelegateCommand AddUserCommand { get; }
        public DelegateCommand RemoveUserCommand { get; }
        public DelegateCommand RefreshUsersCommand { get; }

        public OrganizationManagementViewModel(IOrganizationService organizationService, IUserService userService)
        {
            _organizationService = organizationService ?? throw new ArgumentNullException(nameof(organizationService));
            _userService = userService ?? throw new ArgumentNullException(nameof(userService));

            OrganizationTree = new ObservableCollection<OrganizationUnitModel>();
            OrganizationUsers = new ObservableCollection<User>();
            AllUsers = new ObservableCollection<User>();
            AvailableUsers = new ObservableCollection<User>();

            LoadCommand = new DelegateCommand(async () => await LoadDataAsync());
            AddRootOrganizationCommand = new DelegateCommand(async () => await AddRootOrganizationAsync());
            AddChildOrganizationCommand = new DelegateCommand(async () => await AddChildOrganizationAsync(), CanAddChildOrganization);
            EditOrganizationCommand = new DelegateCommand(async () => await EditOrganizationAsync(), CanEditOrganization);
            DeleteOrganizationCommand = new DelegateCommand(async () => await DeleteOrganizationAsync(), CanDeleteOrganization);
            AddUserCommand = new DelegateCommand(async () => await AddUserToOrganizationAsync(), CanAddUser);
            RemoveUserCommand = new DelegateCommand(async () => await RemoveUserFromOrganizationAsync(), CanRemoveUser);
            RefreshUsersCommand = new DelegateCommand(async () => await LoadAllUsersAsync());
        }

        private async Task LoadDataAsync()
        {
            IsLoading = true;
            try
            {
                var tree = await _organizationService.BuildOrganizationTreeAsync();
                OrganizationTree = new ObservableCollection<OrganizationUnitModel>(tree);

                await LoadAllUsersAsync();
            }
            finally
            {
                IsLoading = false;
            }
        }

        private async Task LoadAllUsersAsync()
        {
            var users = await _userService.GetAllUsersAsync();
            AllUsers = new ObservableCollection<User>(users);
            UpdateAvailableUsers();
        }

        private async Task LoadOrganizationUsersAsync(int orgUnitId)
        {
            var users = await _organizationService.GetOrganizationUsersAsync(orgUnitId);
            OrganizationUsers = new ObservableCollection<User>(users);
            UpdateAvailableUsers();
        }

        private void UpdateAvailableUsers()
        {
            if (SelectedOrganization == null)
            {
                AvailableUsers = new ObservableCollection<User>();
                return;
            }

            var orgUserIds = OrganizationUsers.Select(u => u.Id).ToHashSet();
            var available = AllUsers.Where(u => !orgUserIds.Contains(u.Id)).ToList();
            AvailableUsers = new ObservableCollection<User>(available);
        }

        private async Task AddRootOrganizationAsync()
        {
            if (string.IsNullOrWhiteSpace(NewOrgName))
            {
                HandyControl.Controls.Growl.Warning("请输入组织名称！");
                return;
            }

            var orgUnit = new OrganizationUnitModel
            {
                DisplayName = NewOrgName,
                Code = NewOrgCode ?? NewOrgName,
                ParentId = 0
            };

            var success = await _organizationService.CreateOrganizationUnitAsync(orgUnit);
            if (success)
            {
                OrganizationTree.Add(orgUnit);
                NewOrgName = string.Empty;
                NewOrgCode = string.Empty;
                HandyControl.Controls.Growl.Success("组织创建成功！");
            }
            else
            {
                HandyControl.Controls.Growl.Error("组织创建失败！");
            }
        }

        private async Task AddChildOrganizationAsync()
        {
            if (SelectedOrganization == null)
            {
                HandyControl.Controls.Growl.Warning("请先选择父组织！");
                return;
            }

            if (string.IsNullOrWhiteSpace(NewOrgName))
            {
                HandyControl.Controls.Growl.Warning("请输入组织名称！");
                return;
            }

            var orgUnit = new OrganizationUnitModel
            {
                DisplayName = NewOrgName,
                Code = NewOrgCode ?? NewOrgName,
                ParentId = SelectedOrganization.Id
            };

            var success = await _organizationService.CreateOrganizationUnitAsync(orgUnit);
            if (success)
            {
                SelectedOrganization.Children.Add(orgUnit);
                NewOrgName = string.Empty;
                NewOrgCode = string.Empty;
                HandyControl.Controls.Growl.Success("子组织创建成功！");
            }
            else
            {
                HandyControl.Controls.Growl.Error("子组织创建失败！");
            }
        }

        private bool CanAddChildOrganization()
        {
            return SelectedOrganization != null;
        }

        private async Task EditOrganizationAsync()
        {
            if (SelectedOrganization == null)
                return;

            if (string.IsNullOrWhiteSpace(NewOrgName))
            {
                NewOrgName = SelectedOrganization.DisplayName;
                NewOrgCode = SelectedOrganization.Code;
                HandyControl.Controls.Growl.Info("请修改组织名称和编码后再次点击保存！");
                return;
            }

            SelectedOrganization.DisplayName = NewOrgName;
            SelectedOrganization.Code = NewOrgCode ?? NewOrgName;

            var success = await _organizationService.UpdateOrganizationUnitAsync(SelectedOrganization);
            if (success)
            {
                NewOrgName = string.Empty;
                NewOrgCode = string.Empty;
                HandyControl.Controls.Growl.Success("组织更新成功！");
            }
            else
            {
                HandyControl.Controls.Growl.Error("组织更新失败！");
            }
        }

        private bool CanEditOrganization()
        {
            return SelectedOrganization != null;
        }

        private async Task DeleteOrganizationAsync()
        {
            if (SelectedOrganization == null)
                return;

            var result = HandyControl.Controls.MessageBox.Show(
                $"确定要删除组织 '{SelectedOrganization.DisplayName}' 吗？\n该操作将同时移除组织内的用户关联。",
                "确认删除",
                System.Windows.MessageBoxButton.YesNo,
                System.Windows.MessageBoxImage.Question);

            if (result == System.Windows.MessageBoxResult.Yes)
            {
                var success = await _organizationService.DeleteOrganizationUnitAsync(SelectedOrganization.Id);
                if (success)
                {
                    RemoveFromTree(OrganizationTree, SelectedOrganization);
                    SelectedOrganization = null;
                    OrganizationUsers.Clear();
                    HandyControl.Controls.Growl.Success("组织删除成功！");
                }
                else
                {
                    HandyControl.Controls.Growl.Error("组织删除失败！");
                }
            }
        }

        private bool CanDeleteOrganization()
        {
            return SelectedOrganization != null;
        }

        private void RemoveFromTree(ObservableCollection<OrganizationUnitModel> tree, OrganizationUnitModel target)
        {
            if (tree.Contains(target))
            {
                tree.Remove(target);
                return;
            }

            foreach (var node in tree)
            {
                RemoveFromTree(node.Children, target);
            }
        }

        private async Task AddUserToOrganizationAsync()
        {
            if (SelectedOrganization == null || SelectedAvailableUser == null)
                return;

            var success = await _organizationService.AssignUsersToOrganizationAsync(
                SelectedOrganization.Id,
                new System.Collections.Generic.List<int> { SelectedAvailableUser.Id });

            if (success)
            {
                OrganizationUsers.Add(SelectedAvailableUser);
                AvailableUsers.Remove(SelectedAvailableUser);
                HandyControl.Controls.Growl.Success("用户添加成功！");
            }
            else
            {
                HandyControl.Controls.Growl.Error("用户添加失败！");
            }
        }

        private bool CanAddUser()
        {
            return SelectedOrganization != null && SelectedAvailableUser != null;
        }

        private async Task RemoveUserFromOrganizationAsync()
        {
            if (SelectedOrganization == null || SelectedUser == null)
                return;

            var success = await _organizationService.RemoveUserFromOrganizationAsync(
                SelectedOrganization.Id,
                SelectedUser.Id);

            if (success)
            {
                OrganizationUsers.Remove(SelectedUser);
                AvailableUsers.Add(SelectedUser);
                HandyControl.Controls.Growl.Success("用户移除成功！");
            }
            else
            {
                HandyControl.Controls.Growl.Error("用户移除失败！");
            }
        }

        private bool CanRemoveUser()
        {
            return SelectedOrganization != null && SelectedUser != null;
        }

        private void RaiseCanExecuteChanged()
        {
            AddChildOrganizationCommand.RaiseCanExecuteChanged();
            EditOrganizationCommand.RaiseCanExecuteChanged();
            DeleteOrganizationCommand.RaiseCanExecuteChanged();
            AddUserCommand.RaiseCanExecuteChanged();
            RemoveUserCommand.RaiseCanExecuteChanged();
        }
    }
}
