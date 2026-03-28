using System.Collections.Generic;
using System.Linq;
using Vk.Dbp.AccountModule.Models;
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
            _visibleMenus = new HashSet<string>();
            RefreshPermissions();
        }

        public bool IsMenuVisible(string menuName)
        {
            if (string.IsNullOrEmpty(menuName))
                return false;

            var menuItem = MenuPermissionConfig.GetMenuItem(menuName);
            if (menuItem == null)
                return false;

            if (!menuItem.RequireAuthentication)
                return true;

            if (!_userSession.IsLoggedIn)
                return false;

            if (_userSession.Permissions == null || _userSession.Permissions.Count == 0)
            {
                return true;
            }

            return _userSession.HasPermission(menuItem.PermissionCode);
        }

        public IEnumerable<MenuItemInfo> GetVisibleMenus()
        {
            return MenuPermissionConfig.GetAllMenuItems()
                .Where(item => IsMenuVisible(item.Name));
        }

        public void RefreshPermissions()
        {
            _visibleMenus = new HashSet<string>(
                MenuPermissionConfig.GetAllMenuItems()
                    .Where(item => IsMenuVisible(item.Name))
                    .Select(item => item.Name)
            );
        }
    }
}
