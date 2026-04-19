using System;
using System.Collections.Generic;
using System.Linq;
using Vk.Dbp.Services.Session;

namespace Dabp.WpfWindow.Services
{
    public class MenuPermissionFilter : IMenuPermissionFilter
    {
        private const string AdminUsername = "admin";

        private readonly IUserSession _userSession;
        private HashSet<string> _visibleMenus;

        public MenuPermissionFilter(IUserSession userSession)
        {
            _userSession = userSession ?? throw new ArgumentNullException(nameof(userSession));
            _visibleMenus = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            RefreshPermissions();
        }

        public bool IsMenuVisible(string menuName)
        {
            if (string.IsNullOrWhiteSpace(menuName))
            {
                return false;
            }

            MenuItemInfo? menuItem = MenuPermissionConfig.GetMenuItem(menuName);
            if (menuItem is null)
            {
                return false;
            }

            if (!menuItem.RequireAuthentication)
            {
                return true;
            }

            if (!_userSession.IsLoggedIn)
            {
                return false;
            }

            if (IsAdminUser())
            {
                return true;
            }

            return _visibleMenus.Contains(menuName);
        }

        public IEnumerable<MenuItemInfo> GetVisibleMenus()
        {
            return MenuPermissionConfig.GetAllMenuItems()
                .Where(item => IsMenuVisible(item.Name));
        }

        public void RefreshPermissions()
        {
            if (!_userSession.IsLoggedIn)
            {
                _visibleMenus = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
                return;
            }

            if (IsAdminUser())
            {
                _visibleMenus = new HashSet<string>(
                    MenuPermissionConfig.GetAllMenuItems()
                        .Where(item => item.RequireAuthentication)
                        .Select(item => item.Name),
                    StringComparer.OrdinalIgnoreCase);
                return;
            }

            var userPermissions = new HashSet<string>(
                _userSession.Permissions ?? Enumerable.Empty<string>(),
                StringComparer.OrdinalIgnoreCase);

            _visibleMenus = new HashSet<string>(
                MenuPermissionConfig.GetAllMenuItems()
                    .Where(item => item.RequireAuthentication &&
                                   userPermissions.Contains(item.PermissionCode))
                    .Select(item => item.Name),
                StringComparer.OrdinalIgnoreCase);
        }

        private bool IsAdminUser()
        {
            return string.Equals(_userSession.Username, AdminUsername, StringComparison.OrdinalIgnoreCase);
        }
    }
}
