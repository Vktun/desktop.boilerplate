
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
using Vk.Dbp.Services.Audit;
using Vk.Dbp.Services.Alarm;
using Dabp.Utils.Security;
using Vk.Dbp.Contracts.Services;

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
            containerRegistry.RegisterForNavigation<SystemSettingsView>();
            containerRegistry.RegisterForNavigation<AlarmConfigView>();

            containerRegistry.RegisterSingleton<IAuditLogService, DbAuditLogService>();

            containerRegistry.RegisterSingleton<IPasswordHasher, PasswordHasher>();

            containerRegistry.RegisterSingleton<IUserService, UserService>();
            containerRegistry.RegisterSingleton<IRoleService, RoleService>();
            containerRegistry.RegisterSingleton<IPermissionService, PermissionService>();
            containerRegistry.RegisterSingleton<INotificationService, NotificationService>();
            containerRegistry.RegisterSingleton<IGlobalNotificationPublisher, GlobalNotificationPublisher>();
            containerRegistry.RegisterSingleton<IOrganizationService, OrganizationService>();
            containerRegistry.RegisterSingleton<ISystemConfigService, SystemConfigService>();

            // Alarm services
            containerRegistry.RegisterSingleton<IAlarmService, AlarmService>();
            containerRegistry.RegisterSingleton<IAlarmConfigService, AlarmConfigService>();
        }
    }
}
