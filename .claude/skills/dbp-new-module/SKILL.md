---
name: dbp-new-module
description: 创建或扩展 Prism 业务模块（新模块、新页面、新 ViewModel、模块服务、导航注册）的标准流程。当任务提到"新建模块/加页面/加菜单项/二次开发扩展/注册导航"时使用。
---

# 新建/扩展 Prism 模块

## 动手前必读

- `CLAUDE.md`（分层规则）
- `.claude/rules/code-reality.md`（命名空间现实、注册位置速查）
- 范例模块：`prismModules/Vk.Dbp.AccountModule/DbpAccountModule.cs`
- 导航常量（**实际位置**）：`src/Vk.Dbp.Contracts/Constants/NavigationConstants.cs`（不是 WpfWindow 下的旧路径）

## 放置决策表

| 需求 | 位置 |
|---|---|
| 新业务功能模块 | `prismModules/Vk.Dbp.<Feature>Module` |
| 宿主 shell 行为（全局布局/主题/锁屏） | `src/Vk.Dbp.WpfWindow` |
| 跨模块共享契约（接口/事件/常量） | `src/Vk.Dbp.Contracts` |
| 跨模块共享服务实现 | `src/Vk.Dbp.Services` |
| 持久化实体/仓储 | `src/Vk.Dbp.Infrastructure` |
| 客户专属入口组合 | `dbpApps/<AppName>`（注意：现为模板占位，尚未接线 shell） |
| 框架级抽象 | `dbpframework/Vk.Dbp.Core` |

不把客户业务逻辑放进可复用 shell，除非用户明确要宿主级功能。

## 模块目录形状（按 AccountModule 现实）

```text
prismModules/Vk.Dbp.YourModule/
  Vk.Dbp.YourModule.csproj      # net10.0-windows, UseWPF, 引用 Contracts/Services/Infrastructure/Utils
  DbpYourModule.cs              # public class DbpYourModule : IModule，命名空间 = 项目名
  Views/                        # XxxView.xaml + XxxDialog.xaml（AccountModule 风格带 View 后缀）
  ViewModels/                   # XxxViewModel
  Services/                     # 接口 + 实现同目录（IXxxService.cs + XxxService.cs）
  Models/                       # 模块对外模型（可绑定 POCO）
  Converters/                   # 可选，IValueConverter
```

**没有 `Constants/` 文件夹**——视图名/区域名常量一律加到 `Vk.Dbp.Contracts` 的 `NavigationConstants`（`ViewNames`/`RegionNames`）。

## 实施清单

1. **csproj**：`net10.0-windows` + `UseWPF=true`；引用既有项目而非重复加包（Prism/HandyControl 已在依赖链上）。命名空间跟项目名（`Vk.Dbp.YourModule`），别用 `Dabp` 前缀（模块层无历史包袱）。

2. **模块类**：

```csharp
public class DbpYourModule : IModule
{
    public void OnInitialized(IContainerProvider containerProvider) { }

    public void RegisterTypes(IContainerRegistry containerRegistry)
    {
        // 视图注册（一行一个，ViewModelLocator 按约定找 ViewModel）
        containerRegistry.RegisterForNavigation<Views.MainView>();

        // 模块服务（单例为默认；接口与实现同在 Services/）
        containerRegistry.RegisterSingleton<Services.IYourService, Services.YourService>();
    }
}
```

3. **注册模块目录**：`src/Vk.Dbp.WpfWindow/PrismBootstrapper.cs` → `ConfigureModuleCatalog` 中 `moduleCatalog.AddModule<Vk.Dbp.YourModule.DbpYourModule>();`（全限定名，现有顺序 Workshop 在前）。

4. **导航常量**：在 `src/Vk.Dbp.Contracts/Constants/NavigationConstants.cs` 的 `ViewNames` 加视图名常量；菜单项接入参考 HeaderViewModel/ShellMenu 的现有写法，涉及权限时同步 `MenuPermissionConfig`（注意：菜单权限静态+DB 双写是已知债，新增时两处都要改并加注释）。

5. **ViewModel 规范**（详见 CLAUDE.md 约定）：
   - `BindableBase`；构造器注入 + null guard；`SetProperty(ref _field, value)`。
   - `DelegateCommand` / `DelegateCommand<T>`，CanExecute 依赖属性时 `.ObservesProperty(() => Prop)`。
   - 导航用 `INavigationService.NavigateTo(ViewNames.Xxx)`（Contracts 封装），**不注入 IRegionManager**。
   - 订阅事件的 VM 实现 `IDisposable`（`_isDisposed` 守卫 + 取消订阅）。
   - 区域导航 VM 实现 `INavigationAware`，`OnNavigatedTo` 触发初始加载。

6. **View 规范**：
   - `UserControl` + `prism:ViewModelLocator.AutoWireViewModel="True"`。
   - code-behind 只留 `InitializeComponent()`（唯一例外：纯视觉行为，如 LoginView 的密码显示切换）。
   - 主题画刷用 `{DynamicResource PrimaryBackgroundBrush}` 等既有资源；按钮样式用 HandyControl `StaticResource ButtonPrimary/ButtonDefault/...`。
   - 列表页四段式 Grid：标题栏 + 工具栏（`hc:SearchBar` + 操作按钮）+ `DataGrid` + 底部分页（参照 `UserManagementView.xaml`）。
   - 弹窗用 `Popup IsOpen="{Binding IsDialogOpen}"` + 对话框 ViewModel 模式，**不用** Prism IDialogService（例外警示：`UserEditDialogViewModel` 的 `Action<bool>` 回调是不合约定的历史债，勿模仿）。

7. **数据访问**：服务注入 `ISqlSugarClient`（现实约定）；ViewModel 只依赖服务接口。

8. **测试**：服务测试放 `test/Vk.Dbp.Tests.Unit/Services/`，用 `/dbp-unit-test` 流程。

## 验证

```powershell
dotnet build desktop.boilerplate.slnx
dotnet test test\Vk.Dbp.Tests.Unit\Vk.Dbp.Tests.Unit.csproj
.\scripts\start-wpf-local.ps1   # 需要人工确认导航/菜单实际可用时
```

## 交付说明要点

- 列出新建/修改的模块、View、ViewModel、服务、注册点、常量、测试文件。
- 说明任何需要的人工步骤（数据库种子、配置）。
- 说明运行了哪些验证命令、哪些无法运行。
