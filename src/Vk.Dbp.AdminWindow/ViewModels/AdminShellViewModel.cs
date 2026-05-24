using System.Collections.ObjectModel;
using Prism.Commands;
using Prism.Mvvm;
using Vk.Dbp.AdminWindow.Models;

namespace Vk.Dbp.AdminWindow.ViewModels;

/// <summary>
/// ViewModel for the admin shell window.
/// </summary>
public sealed class AdminShellViewModel : BindableBase
{
    private AdminMenuItem? _selectedMenuItem;

    /// <summary>
    /// Initializes a new instance of the <see cref="AdminShellViewModel"/> class.
    /// </summary>
    public AdminShellViewModel()
    {
        MenuItems = new ObservableCollection<AdminMenuItem>
        {
            new("dashboard", "控制台", "系统运行概览与待办信息", "D"),
            new("users", "用户管理", "维护用户、状态和基础资料", "U"),
            new("roles", "角色管理", "维护角色与职责边界", "R"),
            new("permissions", "权限管理", "配置菜单、功能和数据权限", "P"),
            new("settings", "系统设置", "维护运行参数和本地配置", "S"),
            new("audit", "审计日志", "查看登录、操作与异常记录", "A")
        };

        SelectMenuItemCommand = new DelegateCommand<AdminMenuItem?>(SelectMenuItem);
        SelectedMenuItem = MenuItems[0];
    }

    /// <summary>
    /// Gets the window title.
    /// </summary>
    public string WindowTitle => "DBP Admin Window";

    /// <summary>
    /// Gets the sidebar menu items.
    /// </summary>
    public ObservableCollection<AdminMenuItem> MenuItems { get; }

    /// <summary>
    /// Gets the command that selects a sidebar menu item.
    /// </summary>
    public DelegateCommand<AdminMenuItem?> SelectMenuItemCommand { get; }

    /// <summary>
    /// Gets or sets the selected sidebar menu item.
    /// </summary>
    public AdminMenuItem? SelectedMenuItem
    {
        get => _selectedMenuItem;
        set
        {
            if (SetProperty(ref _selectedMenuItem, value))
            {
                RaisePropertyChanged(nameof(ContentTitle));
            }
        }
    }

    /// <summary>
    /// Gets the title displayed in the right content header.
    /// </summary>
    public string ContentTitle => SelectedMenuItem?.Title ?? "管理控制台";

    private void SelectMenuItem(AdminMenuItem? menuItem)
    {
        if (menuItem == null)
        {
            return;
        }

        SelectedMenuItem = menuItem;
    }
}
