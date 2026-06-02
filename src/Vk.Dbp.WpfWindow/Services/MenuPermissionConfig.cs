using System.Collections.Generic;
using System.Linq;
using Vk.Dbp.Contracts.Navigation;

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
        private static readonly Dictionary<string, MenuItemInfo> MenuItemsInternal = ShellMenuDefinitions.All
            .Where(definition => definition.IsShellMenu)
            .ToDictionary(
                definition => definition.Name,
                definition => new MenuItemInfo(
                    definition.Name,
                    definition.DisplayName,
                    definition.PermissionCode,
                    definition.RequireAuthentication));

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
