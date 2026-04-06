using System;
using System.Collections.Generic;
using System.Linq;
using Vk.Dbp.AccountModule.Services;

namespace Dabp.WpfWindow.Services
{
    public class MenuPermissionFilter : IMenuPermissionFilter
    {
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

            return _visibleMenus.Contains(menuName);
        }

        public IEnumerable<MenuItemInfo> GetVisibleMenus()
        {
            return MenuPermissionConfig.GetAllMenuItems()
                .Where(item => IsMenuVisible(item.Name));
        }

        public void RefreshPermissions()
        {
            IEnumerable<string> userPermissions = _userSession.Permissions ?? Enumerable.Empty<string>();

            _visibleMenus = new HashSet<string>(
                MenuPermissionConfig.GetAllMenuItems()
                    .Where(item => item.RequireAuthentication &&
                                   _userSession.IsLoggedIn &&
                                   userPermissions.Contains(item.PermissionCode))
                    .Select(item => item.Name),
                StringComparer.OrdinalIgnoreCase);
        }
    }
}
