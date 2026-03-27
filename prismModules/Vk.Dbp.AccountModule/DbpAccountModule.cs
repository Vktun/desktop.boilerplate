
using Prism.Ioc;
using Prism.Modularity;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Controls;
using Vk.Dbp.AccountModule.Views;
using Vk.Dbp.AccountModule.Services;
using Vk.Dbp.Core.Audit.Interfaces;
using Vk.Dbp.Core.Audit.Services;
using Dabp.Utils.Security;

namespace Vk.Dbp.AccountModule
{
    public class DbpAccountModule : IModule
    {
        public void OnInitialized(IContainerProvider containerProvider)
        {

        }

        public void RegisterTypes(IContainerRegistry containerRegistry)
        {
            containerRegistry.RegisterForNavigation<LoginView>();
            containerRegistry.RegisterForNavigation<ChangePasswordView>();
            containerRegistry.RegisterForNavigation<AdminSettingView>();
            containerRegistry.RegisterForNavigation<UserManagementView>();
            containerRegistry.RegisterForNavigation<RoleManagementView>();
            containerRegistry.RegisterForNavigation<PermissionManagementView>();
            containerRegistry.RegisterForNavigation<AuditLogView>();
            containerRegistry.RegisterForNavigation<OrganizationManagementView>();

            containerRegistry.RegisterSingleton<IAuditLogService, AuditLogService>();

            containerRegistry.RegisterSingleton<IPasswordHasher, PasswordHasher>();

            containerRegistry.RegisterSingleton<IUserService, UserService>();
            containerRegistry.RegisterSingleton<IRoleService, RoleService>();
            containerRegistry.RegisterSingleton<IPermissionService, PermissionService>();
            containerRegistry.RegisterSingleton<INotificationService, NotificationService>();
            containerRegistry.RegisterSingleton<IOrganizationService, OrganizationService>();
        }
    }
}
