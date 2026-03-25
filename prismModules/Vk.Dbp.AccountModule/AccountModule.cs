using Prism.Ioc;
using Prism.Modularity;
using Vk.Dbp.AccountModule.Services;
using Vk.Dbp.AccountModule.ViewModels;
using Vk.Dbp.Core.Audit.Interfaces;
using Vk.Dbp.Core.Audit.Services;

namespace Vk.Dbp.AccountModule
{
    /// <summary>
    /// 账户模块 - 提供用户、角色、权限管理功能
    /// 审计日志由 Vk.Dbp.Core 提供
    /// </summary>
    public class AccountModule : IModule
    {
        /// <summary>
        /// 注册服务和视图
        /// </summary>
        public void RegisterTypes(IContainerRegistry containerRegistry)
        {
            // 注册审计日志服务（来自 Core）
            containerRegistry.RegisterSingleton<IAuditLogService, AuditLogService>();

            // 注册账户模块特有的服务
            containerRegistry.RegisterSingleton<IUserService, UserService>();
            containerRegistry.RegisterSingleton<IRoleService, RoleService>();
            containerRegistry.RegisterSingleton<IPermissionService, PermissionService>();

            // 注册 ViewModel
            containerRegistry.Register<UserManagementViewModel>();
            containerRegistry.Register<RoleManagementViewModel>();
            containerRegistry.Register<AuditLogViewModel>();

            // 注册导航视图 (待创建)
            // containerRegistry.RegisterForNavigation<UserManagementView, UserManagementViewModel>("UserManagement");
            // containerRegistry.RegisterForNavigation<RoleManagementView, RoleManagementViewModel>("RoleManagement");
            // containerRegistry.RegisterForNavigation<AuditLogView, AuditLogViewModel>("AuditLog");
        }

        /// <summary>
        /// 初始化模块
        /// </summary>
        public void OnInitialized(IContainerProvider containerProvider)
        {
            // 模块初始化逻辑
        }
    }
}