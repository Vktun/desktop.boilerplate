using System.Collections.Generic;

namespace Dabp.WpfWindow.Services
{
    public class MenuItemInfo
    {
        public MenuItemInfo(string name, string displayName, string? permissionCode = null, bool requireAuthentication = true)
        {
            Name = name;
            DisplayName = displayName;
            PermissionCode = string.IsNullOrWhiteSpace(permissionCode) ? name : permissionCode;
            RequireAuthentication = requireAuthentication;
        }

        public string Name { get; set; }

        public string DisplayName { get; set; }

        public string PermissionCode { get; set; }

        public bool RequireAuthentication { get; set; } = true;
    }

    public static class MenuPermissionConfig
    {
        private static readonly Dictionary<string, MenuItemInfo> MenuItemsInternal = new()
        {
            { "Dashboard", new MenuItemInfo("Dashboard", "驾驶舱", requireAuthentication: false) },
            { "SelfCheck", new MenuItemInfo("SelfCheck", "自检") },
            { "Production", new MenuItemInfo("Production", "生产信息") },
            { "ProductionRecord", new MenuItemInfo("ProductionRecord", "生产记录") },
            { "AlarmRecord", new MenuItemInfo("AlarmRecord", "报警记录") },
            { "AuditRecord", new MenuItemInfo("AuditRecord", "审计追踪") },
            { "AdminSettingView", new MenuItemInfo("AdminSettingView", "后台管理") }
        };

        public static IReadOnlyDictionary<string, MenuItemInfo> MenuItems => MenuItemsInternal;

        public static MenuItemInfo? GetMenuItem(string name)
        {
            return MenuItemsInternal.TryGetValue(name, out MenuItemInfo? item) ? item : null;
        }

        public static IEnumerable<MenuItemInfo> GetAllMenuItems()
        {
            return MenuItemsInternal.Values;
        }
    }
}
