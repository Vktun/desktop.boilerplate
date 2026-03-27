using System.Collections.Generic;

namespace Dabp.WpfWindow.Services
{
    public class MenuItemInfo
    {
        public string Name { get; set; }
        public string DisplayName { get; set; }
        public string PermissionCode { get; set; }
        public bool RequireAuthentication { get; set; } = true;

        public MenuItemInfo(string name, string displayName, string permissionCode, bool requireAuthentication = true)
        {
            Name = name;
            DisplayName = displayName;
            PermissionCode = permissionCode;
            RequireAuthentication = requireAuthentication;
        }
    }

    public static class MenuPermissionConfig
    {
        private static readonly Dictionary<string, MenuItemInfo> _menuItems = new Dictionary<string, MenuItemInfo>
        {
            { "Dashboard", new MenuItemInfo("Dashboard", "驾驶舱", "Menu.Dashboard", false) },
            { "SelfCheck", new MenuItemInfo("SelfCheck", "自检", "Menu.SelfCheck") },
            { "Production", new MenuItemInfo("Production", "生产信息", "Menu.Production") },
            { "ProductionRecord", new MenuItemInfo("ProductionRecord", "生产记录", "Menu.ProductionRecord") },
            { "AlarmRecord", new MenuItemInfo("AlarmRecord", "报警记录", "Menu.AlarmRecord") },
            { "AuditRecord", new MenuItemInfo("AuditRecord", "审计追踪", "Menu.AuditRecord") },
            { "AdminSettingView", new MenuItemInfo("AdminSettingView", "后台管理", "Menu.Admin") }
        };

        public static IReadOnlyDictionary<string, MenuItemInfo> MenuItems => _menuItems;

        public static MenuItemInfo GetMenuItem(string name)
        {
            return _menuItems.TryGetValue(name, out var item) ? item : null;
        }

        public static IEnumerable<MenuItemInfo> GetAllMenuItems()
        {
            return _menuItems.Values;
        }
    }
}
