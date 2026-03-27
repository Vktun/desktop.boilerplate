using System.Collections.Generic;

namespace Dabp.WpfWindow.Services
{
    public interface IMenuPermissionFilter
    {
        bool IsMenuVisible(string menuName);
        IEnumerable<MenuItemInfo> GetVisibleMenus();
        void RefreshPermissions();
    }
}
