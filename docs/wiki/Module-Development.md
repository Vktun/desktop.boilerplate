# 二次开发指南

## 创建新业务模块

### 第1步：创建模块项目

在 `prismModules/` 目录下创建新的类库项目：

```bash
cd prismModules
dotnet new classlib -n Vk.Dbp.YourModule
```

编辑 `.csproj`，添加必要引用：

```xml
<Project Sdk="Microsoft.NET.Sdk">
  <PropertyGroup>
    <TargetFramework>net10.0-windows</TargetFramework>
    <UseWPF>true</UseWPF>
  </PropertyGroup>

  <ItemGroup>
    <PackageReference Include="Prism.Unity" Version="9.0.*" />
    <PackageReference Include="HandyControl" Version="3.5.*" />
    <ProjectReference Include="..\..\src\Vk.Dbp.Contracts\Vk.Dbp.Contracts.csproj" />
    <ProjectReference Include="..\..\src\Vk.Dbp.Services\Vk.Dbp.Service.csproj" />
    <ProjectReference Include="..\..\src\Vk.Dbp.Infrastructure\Vk.Dbp.Infrastructure.csproj" />
  </ItemGroup>
</Project>
```

### 第2步：创建模块入口类

```csharp
using Prism.Ioc;
using Prism.Modularity;
using Vk.Dbp.Contracts.Modules;

namespace Vk.Dbp.YourModule
{
    [ModuleDependency("AccountModule")]
    public class DbpYourModule : IModule, IModuleMetadata
    {
        public string ModuleName => "YourModule";
        public string Version => "1.0.0";
        public string Description => "你的模块描述";
        public string[] Dependencies => new[] { "AccountModule" };
        public string[] ProvidedServices => new[] { "IYourService" };

        public void RegisterTypes(IContainerRegistry containerRegistry)
        {
            containerRegistry.RegisterSingleton<IYourService, YourService>();
            containerRegistry.RegisterForNavigation<YourMainView>();
            containerRegistry.RegisterForNavigation<YourDetailView>();
        }

        public void OnInitialized(IContainerProvider containerProvider) { }
        public void OnModuleLoaded() { }
        public void OnModuleUnloading() { }
    }
}
```

### 第3步：在Shell中注册模块

编辑 `src/Vk.Dbp.WpfWindow/PrismBootstrapper.cs`：

```csharp
protected override void ConfigureModuleCatalog(IModuleCatalog moduleCatalog)
{
    moduleCatalog.AddModule<Vk.Dbp.WorkshopModule.DbpWorkshopModule>();
    moduleCatalog.AddModule<Vk.Dbp.AccountModule.DbpAccountModule>();
    moduleCatalog.AddModule<Vk.Dbp.YourModule.DbpYourModule>();  // 添加新模块
}
```

### 第4步：添加模块引用

在 `Vk.Dbp.WpfWindow.csproj` 中添加项目引用：

```xml
<ProjectReference Include="..\..\prismModules\Vk.Dbp.YourModule\Vk.Dbp.YourModule.csproj" />
```

### 推荐的模块目录结构

```
Vk.Dbp.YourModule/
├── DbpYourModule.cs           # 模块入口
├── Views/                     # XAML视图
│   ├── YourMainView.xaml(.cs)
│   └── Dialogs/
│       └── YourDialog.xaml(.cs)
├── ViewModels/                # ViewModel
│   ├── YourMainViewModel.cs
│   └── Dialogs/
│       └── YourDialogViewModel.cs
├── Services/                  # 服务（接口+实现）
│   ├── IYourService.cs
│   └── YourService.cs
├── Models/                    # 数据模型和DTO
│   └── YourModel.cs
└── Converters/                # 值转换器（可选）
    └── YourConverter.cs
```

## 核心服务使用

### 用户会话 (IUserSession)

通过依赖注入获取当前登录用户信息：

```csharp
public class YourViewModel : BindableBase
{
    private readonly IUserSession _userSession;

    public YourViewModel(IUserSession userSession)
    {
        _userSession = userSession;
    }

    public void DoSomething()
    {
        if (!_userSession.IsLoggedIn) return;
        var userId = _userSession.UserId;
        var username = _userSession.Username;
        var hasPermission = _userSession.HasPermission("YourFeature");
    }
}
```

**IUserSession 关键成员**：

| 成员 | 类型 | 说明 |
|------|------|------|
| `UserId` | `int` | 当前用户ID |
| `Username` | `string` | 用户名 |
| `RealName` | `string` | 真实姓名 |
| `IsLoggedIn` | `bool` | 是否已登录 |
| `Permissions` | `List<string>` | 权限代码列表 |
| `IsLocked` | `bool` | 是否锁屏 |
| `HasPermission(code)` | `bool` | 检查是否拥有指定权限 |
| `Login(...)` | `void` | 登录 |
| `Logout()` | `void` | 注销 |
| `Lock(reason)` | `void` | 锁定会话 |
| `Unlock()` | `void` | 解锁会话 |

### 数据库访问 (ISqlSugarClient + IRepository<T>)

```csharp
public class YourService : IYourService
{
    private readonly ISqlSugarClient _db;

    public YourService(ISqlSugarClient db)
    {
        _db = db;
    }

    public async Task<List<YourEntity>> GetAllAsync()
    {
        return await _db.Queryable<YourEntity>()
            .Where(x => !x.IsDeleted)
            .ToListAsync();
    }
}
```

也可以使用泛型仓储 `IRepository<T>`：

```csharp
public class YourService : IYourService
{
    private readonly IRepository<YourEntity> _repo;

    public YourService(IRepository<YourEntity> repo)
    {
        _repo = repo;
    }

    public async Task<YourEntity> GetByIdAsync(int id) => await _repo.GetByIdAsync(id);
    public async Task<int> InsertAsync(YourEntity entity) => await _repo.InsertAsync(entity);
}
```

**添加新数据库实体**：

1. 在 `src/Vk.Dbp.Infrastructure/Entities/` 创建实体类
2. 在 `DatabaseInitializer.InitializeDatabase()` 的 `InitTables` 中注册
3. 如需种子数据，在 `InitializeDataAsync()` 中添加

### 缓存服务 (ICacheService)

```csharp
public class YourService : IYourService
{
    private readonly ICacheService _cacheService;

    public YourService(ICacheService cacheService)
    {
        _cacheService = cacheService;
    }

    public async Task<IEnumerable<YourData>> GetDataAsync()
    {
        return await _cacheService.GetOrCreateAsync(
            "your_data_cache_key",
            () => FetchFromDatabaseAsync(),
            TimeSpan.FromMinutes(5));
    }

    public void InvalidateCache()
    {
        _cacheService.Remove("your_data_cache_key");
        _cacheService.RemoveByPattern("your_data_*");
    }
}
```

### 数据导出 (IExportService)

```csharp
public class YourViewModel : BindableBase
{
    private readonly IExportService _exportService;

    public YourViewModel(IExportService exportService)
    {
        _exportService = exportService;
    }

    public async Task ExportDataAsync()
    {
        var data = await _yourService.GetDataAsync();
        var filePath = await _exportService.ExportToExcelAsync(data, "导出数据");
        await _exportService.OpenExportedFileAsync(filePath);
    }
}
```

支持的导出格式：CSV、Excel（带配置选项）、PDF。

### ViewModel工厂 (IViewModelFactory)

用于创建需要DI注入的ViewModel实例，避免手动 `new`：

```csharp
public class YourViewModel : BindableBase
{
    private readonly IViewModelFactory _viewModelFactory;

    public YourViewModel(IViewModelFactory viewModelFactory)
    {
        _viewModelFactory = viewModelFactory;
    }

    public void ShowDialog()
    {
        var dialogViewModel = _viewModelFactory.Create<YourDialogViewModel>();
    }
}
```

### 审计日志 (IAuditLogService)

```csharp
public class YourService : IYourService
{
    private readonly IAuditLogService _auditLogService;
    private readonly IUserSession _userSession;

    public YourService(IAuditLogService auditLogService, IUserSession userSession)
    {
        _auditLogService = auditLogService;
        _userSession = userSession;
    }

    public async Task DoImportantActionAsync()
    {
        await _auditLogService.LogAsync(
            _userSession.UserId,
            AuditActionType.Create,
            "YourEntity",
            entityId.ToString(),
            "创建了新记录");
    }
}
```

### 全局通知 (IGlobalNotificationPublisher)

```csharp
public class YourService : IYourService
{
    private readonly IGlobalNotificationPublisher _notificationPublisher;

    public YourService(IGlobalNotificationPublisher notificationPublisher)
    {
        _notificationPublisher = notificationPublisher;
    }

    public async Task NotifyAsync()
    {
        await _notificationPublisher.PublishInfoAsync("操作完成", "数据处理已完成", "YourModule");
        await _notificationPublisher.PublishErrorAsync("操作失败", "数据处理异常", "YourModule");
        await _notificationPublisher.PublishToUserAsync(
            targetUserId, "私人消息", "您有一条新任务", NotificationType.Info);
    }
}
```

## 扩展点系统

框架提供三个核心扩展点，模块通过实现接口并注册到DI容器即可自动集成。

### 扩展菜单 (IMenuItemProvider)

```csharp
using Vk.Dbp.Contracts.Extensions;

public class YourMenuProvider : IMenuItemProvider
{
    public IEnumerable<MenuItemInfo> GetMenuItems()
    {
        yield return new MenuItemInfo
        {
            Name = "YourFeature",
            DisplayName = "自定义功能",
            Icon = "pack://application:,,,/Vk.Dbp.YourModule;component/Resources/icon.png",
            NavigateTo = "YourMainView",
            Order = 100,
            RequiredPermission = "YourFeature"
        };
    }
}

// 在模块入口注册
public void RegisterTypes(IContainerRegistry containerRegistry)
{
    containerRegistry.RegisterSingleton<IMenuItemProvider, YourMenuProvider>();
}
```

**MenuItemInfo 属性**：

| 属性 | 说明 |
|------|------|
| `Name` | 唯一标识，同时作为权限代码 |
| `DisplayName` | 显示名称 |
| `Icon` | 图标路径（pack URI） |
| `NavigateTo` | 导航目标视图名称 |
| `Order` | 排序（数字越小越靠前） |
| `RequiredPermission` | 所需权限代码（可选） |
| `ParentMenu` | 父菜单名称（用于子菜单） |

> **注意**：当前菜单权限同时维护在 `MenuPermissionConfig` 静态类中，新增菜单需同步更新该配置。

### 扩展仪表盘组件 (IDashboardWidgetProvider)

```csharp
public class YourWidgetProvider : IDashboardWidgetProvider
{
    public IEnumerable<DashboardWidget> GetWidgets()
    {
        yield return new DashboardWidget
        {
            Id = "your_widget",
            Title = "实时数据",
            ViewName = "YourWidgetView",
            Width = 4, Height = 2, Order = 10,
            Category = "Production",
            RequiredPermission = "dashboard:your_widget"
        };
    }
}
```

### 扩展报表 (IReportGenerator)

```csharp
public class YourReportGenerator : IReportGenerator
{
    public string ReportType => "your_report";
    public string DisplayName => "自定义报表";
    public string Description => "生成自定义数据报表";

    public async Task<byte[]> GenerateReportAsync(ReportParameters parameters)
    {
        // 生成报表内容（PDF/Excel等）
        return reportBytes;
    }

    public ValidationResult ValidateParameters(ReportParameters parameters)
    {
        if (parameters.StartDate == null)
            return ValidationResult.Failure("请选择开始日期");
        return ValidationResult.Success();
    }
}
```

## 事件系统

框架基于Prism EventAggregator定义了以下事件，模块可自由发布和订阅。

### 告警事件

```csharp
// 发布告警
_eventAggregator.GetEvent<AlarmTriggeredEvent>()
    .Publish(new AlarmTriggeredPayload
    {
        AlarmCode = "TEMP_HIGH",
        Level = AlarmLevel.Critical,
        Title = "温度过高",
        Content = "设备温度超过85℃",
        Source = "YourModule"
    });

// 订阅告警
_eventAggregator.GetEvent<AlarmTriggeredEvent>().Subscribe(OnAlarmTriggered);
_eventAggregator.GetEvent<AlarmCountChangedEvent>().Subscribe(OnAlarmCountChanged);
```

**告警等级**：`Info`(0) / `Warning`(1) / `Critical`(2)
**告警状态**：`Active`(0) / `Acknowledged`(1) / `Resolved`(2) / `Ignored`(3)
**告警类型**：`Threshold`(0) / `Device`(1) / `Process`(2) / `System`(3) / `Safety`(4)

### 全局通知事件

```csharp
_eventAggregator.GetEvent<GlobalNotificationEvent>().Subscribe(OnNotificationReceived);
_eventAggregator.GetEvent<NotificationCountChangedEvent>().Subscribe(OnNotificationCountChanged);
```

### 权限变更事件

```csharp
_eventAggregator.GetEvent<PermissionChangedEvent>().Subscribe(OnPermissionChanged);
// PermissionChangeType: Granted / Revoked / Updated
```

### 用户登录事件

```csharp
_eventAggregator.GetEvent<UserLoggedInEvent>().Subscribe(OnUserLoggedIn);
```

### 自定义事件

在 `Vk.Dbp.Contracts/Events/` 中定义自定义事件：

```csharp
using Prism.Events;

namespace Vk.Dbp.Contracts.Events
{
    public class YourDataChangedEvent : PubSubEvent<YourDataChangedPayload> { }

    public class YourDataChangedPayload
    {
        public int Id { get; set; }
        public string ChangeType { get; set; } = string.Empty;
    }
}
```

## 导航系统

框架使用Prism Region进行视图导航：

**Region定义**（在MainWindow.xaml中）：

- `HeaderRegion` - 顶部导航栏
- `ContentRegion` - 主内容区域

**导航操作**：

```csharp
public class YourViewModel : BindableBase
{
    private readonly IRegionManager _regionManager;

    public YourViewModel(IRegionManager regionManager)
    {
        _regionManager = regionManager;
    }

    public void NavigateToDetail(int id)
    {
        var parameters = new NavigationParameters { { "Id", id } };
        _regionManager.RequestNavigate("ContentRegion", "YourDetailView", parameters);
    }
}
```

**接收导航参数**：

```csharp
public class YourDetailViewModel : BindableBase, INavigationAware
{
    public void OnNavigatedTo(NavigationContext navigationContext)
    {
        var id = navigationContext.Parameters.GetValue<int>("Id");
    }

    public bool IsNavigationTarget(NavigationContext navigationContext) => true;
    public void OnNavigatedFrom(NavigationContext navigationContext) { }
}
```

**视图名称常量**（定义在 `Vk.Dbp.WpfWindow.Constants.ViewNames`）：

| 常量 | 值 | 说明 |
|------|------|------|
| `Dashboard` | "Dashboard" | 驾驶舱 |
| `LoginView` | "LoginView" | 登录页 |
| `SelfCheck` | "SelfCheck" | 自检 |
| `Production` | "Production" | 生产信息 |
| `AlarmRecord` | "AlarmRecord" | 报警记录 |
| `AdminSettingView` | "AdminSettingView" | 后台管理 |
| `UserManagementView` | "UserManagementView" | 用户管理 |

## 主题系统

```csharp
public class YourViewModel : BindableBase
{
    private readonly IThemeService _themeService;

    public YourViewModel(IThemeService themeService)
    {
        _themeService = themeService;
    }

    public void SwitchTheme()
    {
        var current = _themeService.CurrentTheme;
        var newTheme = current == "Light" ? "Dark" : "Light";
        _themeService.SetTheme(newTheme);
    }
}
```

主题资源字典位于 `Vk.Dbp.WpfWindow/Themes/`：`LightTheme.xaml` 和 `DarkTheme.xaml`。

在XAML中使用 `DynamicResource` 引用主题资源以支持切换：

```xml
<TextBlock Foreground="{DynamicResource PrimaryTextBrush}" />
<Border Background="{DynamicResource PrimaryBackgroundBrush}" />
```

## 创建独立业务应用 (dbpApps)

`dbpApps/` 目录用于创建面向特定客户/项目的独立WPF应用，复用框架Shell和模块：

1. 创建新的WPF应用项目
2. 引用 `Vk.Dbp.WpfWindow` 或直接引用所需模块
3. 配置自己的 `appsettings.json`
4. 在 `PrismBootstrapper` 中选择加载的模块

```
dbpApps/Dbp.Material.Forming/
├── App.xaml(.cs)
├── MainWindow.xaml(.cs)
├── appsettings.json             # 独立的数据库连接配置
└── Dbp.Material.Forming.csproj
```
