using System.Collections.Generic;

namespace Vk.Dbp.Contracts.Navigation;

public sealed class ShellMenuDefinition
{
    public required string Name { get; init; }

    public required string DisplayName { get; init; }

    public required string PermissionCode { get; init; }

    public required int ProviderId { get; init; }

    public bool IsShellMenu { get; init; }

    public bool RequireAuthentication { get; init; } = true;

    public bool DefaultUserVisible { get; init; }
}

public static class ShellMenuDefinitions
{
    public const int MenuPermissionProviderId = 1;

    public static IReadOnlyList<ShellMenuDefinition> All { get; } =
    [
        new ShellMenuDefinition { Name = "Dashboard", DisplayName = "驾驶舱", PermissionCode = "Dashboard", ProviderId = MenuPermissionProviderId, IsShellMenu = true, RequireAuthentication = false, DefaultUserVisible = true },
        new ShellMenuDefinition { Name = "SelfCheck", DisplayName = "自检", PermissionCode = "SelfCheck", ProviderId = MenuPermissionProviderId, IsShellMenu = true, RequireAuthentication = true, DefaultUserVisible = true },
        new ShellMenuDefinition { Name = "Production", DisplayName = "生产信息", PermissionCode = "Production", ProviderId = MenuPermissionProviderId, IsShellMenu = true, RequireAuthentication = true, DefaultUserVisible = true },
        new ShellMenuDefinition { Name = "ProductionRecord", DisplayName = "生产记录", PermissionCode = "ProductionRecord", ProviderId = MenuPermissionProviderId, IsShellMenu = true, RequireAuthentication = true, DefaultUserVisible = true },
        new ShellMenuDefinition { Name = "AlarmRecord", DisplayName = "报警记录", PermissionCode = "AlarmRecord", ProviderId = MenuPermissionProviderId, IsShellMenu = true, RequireAuthentication = true, DefaultUserVisible = true },
        new ShellMenuDefinition { Name = "AuditRecord", DisplayName = "审计追踪", PermissionCode = "AuditRecord", ProviderId = MenuPermissionProviderId, IsShellMenu = true, RequireAuthentication = true, DefaultUserVisible = false },
        new ShellMenuDefinition { Name = "AdminSettingView", DisplayName = "后台管理", PermissionCode = "AdminSettingView", ProviderId = MenuPermissionProviderId, IsShellMenu = true, RequireAuthentication = true, DefaultUserVisible = false },
        new ShellMenuDefinition { Name = "UserManagement", DisplayName = "用户管理", PermissionCode = "UserManagement", ProviderId = MenuPermissionProviderId, IsShellMenu = false, RequireAuthentication = true, DefaultUserVisible = false },
        new ShellMenuDefinition { Name = "RoleManagement", DisplayName = "角色管理", PermissionCode = "RoleManagement", ProviderId = MenuPermissionProviderId, IsShellMenu = false, RequireAuthentication = true, DefaultUserVisible = false },
        new ShellMenuDefinition { Name = "PermissionManagement", DisplayName = "权限管理", PermissionCode = "PermissionManagement", ProviderId = MenuPermissionProviderId, IsShellMenu = false, RequireAuthentication = true, DefaultUserVisible = false },
        new ShellMenuDefinition { Name = "OrganizationManagement", DisplayName = "组织管理", PermissionCode = "OrganizationManagement", ProviderId = MenuPermissionProviderId, IsShellMenu = false, RequireAuthentication = true, DefaultUserVisible = false },
        new ShellMenuDefinition { Name = "AuditLog", DisplayName = "审计日志", PermissionCode = "AuditLog", ProviderId = MenuPermissionProviderId, IsShellMenu = false, RequireAuthentication = true, DefaultUserVisible = false }
    ];
}
