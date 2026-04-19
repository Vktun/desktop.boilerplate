# Desktop Boilerplate (Dabp)

[!\[CI Status\](https://github.com/yourorg/desktop.boilerplate/workflows/CI/badge.svg null)](https://github.com/yourorg/desktop.boilerplate/actions)
[!\[License\](https://img.shields.io/badge/license-MIT-blue.svg null)](LICENSE)

企业级WPF桌面应用框架 - 基于Prism + HandyControl，支持模块化插件架构，开箱即用。

## 特性

- 模块化架构 - 基于Prism的动态模块加载，支持插件化开发
- 完善的RBAC - 用户-角色-权限三级权限管理
- 现代化UI - HandyControl组件库，支持Light/Dark主题切换
- 丰富的扩展点 - 菜单、仪表盘、报表等均可扩展，无需修改核心代码
- 审计追踪 - 完整的操作审计日志
- 会话安全 - 会话超时自动锁屏、数据库断连自动锁屏
- 告警系统 - 多级别告警（信息/警告/严重），支持弹窗和声音提示
- 数据导出 - 支持CSV/Excel/PDF导出和CSV导入
- 性能优化 - 智能缓存、分页查询、异步非阻塞

## 快速开始

### 前置要求

- .NET 10 SDK
- Visual Studio 2026+ 或 JetBrains Rider
- SQL Server 2019+（或使用LocalDB进行开发测试）

### 克隆和构建

```bash
git clone https://github.com/yourorg/desktop.boilerplate.git
cd desktop.boilerplate
dotnet restore
dotnet build
```

### 配置本地 SQL Server

主程序启动时会读取 `src/Vk.Dbp.WpfWindow/appsettings.json`，再读取可选的 `appsettings.local.json` 覆盖本地配置。开发环境建议把实际数据库连接写入 `appsettings.local.json`，该文件已在 `.gitignore` 中排除。

1. 在 `src/Vk.Dbp.WpfWindow` 目录创建 `appsettings.local.json`。

2. 使用本地 SQL Server 连接串，例如 Windows 身份验证：
   ```json
   {
     "ConnectionStrings": {
       "Default": "Server=127.0.0.1;Database=DabpCore;Trusted_Connection=True;TrustServerCertificate=True;"
     }
   }
   ```

   如果使用 SQL Server 账号密码，改为：
   ```json
   {
     "ConnectionStrings": {
       "Default": "Server=127.0.0.1;Database=DabpCore;Trusted_Connection=False;TrustServerCertificate=True;User Id=sa;Password=your_password"
     }
   }
   ```

3. 运行应用后，SqlSugar CodeFirst 会自动创建数据库表并初始化基础种子数据。

### 默认账户

首次启动且用户表为空时，系统会初始化管理员账号：

```text
用户名: admin
密码: 123456
```

首次登录后请立即修改默认密码。

## 项目结构

```
desktop.boilerplate/
├── src/                              # 核心源代码
│   ├── Vk.Dbp.WpfWindow/            # 主应用程序（Shell宿主）
│   │   ├── PrismBootstrapper.cs     #   启动引导器（DI注册、模块加载、DB初始化）
│   │   ├── MainWindow.xaml          #   主窗口（HeaderRegion + ContentRegion）
│   │   ├── Layout/                  #   布局视图（HeaderView、DefaultContentView）
│   │   ├── ViewModels/              #   Shell级ViewModel（Header、LockScreen、Alarm、Notification）
│   │   ├── Views/                   #   Shell级View（锁屏窗口、告警弹窗、通知面板）
│   │   ├── Services/                #   Shell级服务（主题、锁屏、会话超时、菜单权限、ViewModel工厂）
│   │   ├── Constants/               #   导航常量（RegionNames、ViewNames）
│   │   ├── Converters/              #   通用值转换器
│   │   └── Themes/                  #   Light/Dark主题资源字典
│   │
│   ├── Vk.Dbp.Contracts/            # 模块契约定义层（二次开发核心引用）
│   │   ├── Modules/                 #   IModuleMetadata、IModuleLifecycle、ModuleDependencyAttribute
│   │   ├── Extensions/              #   IMenuItemProvider、IDashboardWidgetProvider、IReportGenerator
│   │   ├── Services/                #   IViewModelFactory、IExportService、INavigationService等
│   │   ├── Events/                  #   Prism事件定义（告警、通知、权限变更、用户登录）
│   │   ├── Caching/                 #   ICacheService
│   │   └── Data/                    #   IPagedQuery<T>、PagedResult<T>
│   │
│   ├── Vk.Dbp.Services/             # 通用服务实现
│   │   ├── Session/                 #   IUserSession / UserSession（用户会话状态）
│   │   ├── Alarm/                   #   IAlarmService / IAlarmConfigService
│   │   ├── Audit/                   #   IAuditLogService、审计扩展方法
│   │   ├── Caching/                 #   InMemoryCacheService
│   │   ├── Export/                  #   ExportService（CSV/Excel/PDF）
│   │   └── Settings/                #   IAppSettingsService / AppSettingsService
│   │
│   ├── Vk.Dbp.Infrastructure/       # 基础设施层
│   │   ├── Entities/                #   数据库实体（User、Role、Permission、AlarmRecord等）
│   │   ├── Repositories/            #   IRepository<T> / SqlSugarRepository<T>
│   │   ├── OrmSetting/              #   SqlSugar FluentAPI配置
│   │   └── DatabaseInitializer.cs   #   数据库初始化（建表+种子数据）
│   │
│   ├── Vk.Dbp.Domain/               # 领域模型
│   └── Vk.Dbp.Utils/                # 工具类
│       ├── Security/                #   IPasswordHasher / PasswordHasher
│       ├── Algorithm/               #   SM4加密
│       ├── IdGenerator/             #   ID生成器配置
│       └── Logging/                 #   PerformanceLogger
│
├── prismModules/                     # Prism业务模块
│   ├── Vk.Dbp.AccountModule/        # 账户管理模块
│   │   ├── DbpAccountModule.cs     #   模块入口（注册视图和服务）
│   │   ├── Views/                  #   登录、用户管理、角色管理、权限管理、组织管理、审计日志等
│   │   ├── ViewModels/             #   对应ViewModel
│   │   ├── Services/               #   UserService、RoleService、PermissionService等
│   │   └── Models/                 #   Notification、OrganizationUnitModel等
│   │
│   └── Vk.Dbp.WorkshopModule/      # 车间管理模块（示例业务模块）
│       ├── DbpWorkshopModule.cs    #   模块入口
│       ├── Views/                  #   Dashboard、Production、SelfCheck、AlarmRecord等
│       └── ViewModels/             #   对应ViewModel
│
├── dbpframework/                     # 框架层
│   ├── Vk.Dbp.Core/                # 核心抽象（IDbpModule、ServiceCollectionExtensions）
│   └── Vk.Dbp.Account/             # 账户领域核心（ICurrentUser、PermissionDto、RoleDto）
│
├── dbpApps/                          # 独立业务应用（客户项目入口）
│   ├── Dbp.Material.Forming/       # 成型工艺应用
│   └── Dbp.Material.Mixing/        # 混合工艺应用
│
├── test/                             # 测试项目
│   ├── Vk.Dbp.Tests.Unit/          # 单元测试
│   ├── Vk.Dbp.Tests.Integration/   # 集成测试
│   └── Vk.Dbp.Tests.Common/        # 测试辅助类
│
├── scripts/                          # 构建脚本
│   └── publish.ps1                  # 发布脚本
│
└── docs/                             # 文档
    └── MODULE_DEVELOPMENT_GUIDE.md  # 模块开发指南
```

## 二次开发指南

### 架构概览

本框架采用分层+模块化的架构设计：

```
┌─────────────────────────────────────────────────────┐
│                  dbpApps (客户应用入口)                │
│          组合Shell + 选择模块 + 品牌定制               │
├─────────────────────────────────────────────────────┤
│               prismModules (业务模块层)               │
│     AccountModule  │  WorkshopModule  │  YourModule  │
├─────────────────────────────────────────────────────┤
│              Vk.Dbp.WpfWindow (Shell宿主)             │
│    导航框架 │ 主题 │ 锁屏 │ 告警 │ 通知 │ 菜单权限     │
├─────────────────────────────────────────────────────┤
│             Vk.Dbp.Contracts (契约层)                │
│    模块接口 │ 扩展点 │ 事件 │ 服务契约 │ 分页/缓存     │
├─────────────────────────────────────────────────────┤
│           Vk.Dbp.Services (通用服务层)                │
│    会话 │ 审计 │ 告警 │ 缓存 │ 导出 │ 配置            │
├─────────────────────────────────────────────────────┤
│         Vk.Dbp.Infrastructure (基础设施层)            │
│    实体 │ 仓储 │ ORM │ 数据库初始化                    │
├─────────────────────────────────────────────────────┤
│             Vk.Dbp.Utils (工具层)                    │
│    密码哈希 │ 加密 │ ID生成 │ 性能日志                 │
└─────────────────────────────────────────────────────┘
```

**核心依赖关系**：模块只引用 `Vk.Dbp.Contracts`，不直接引用其他模块实现。模块间通过接口+事件解耦。

### 创建新业务模块

#### 第1步：创建模块项目

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

#### 第2步：创建模块入口类

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

        public void OnInitialized(IContainerProvider containerProvider)
        {
        }

        public void OnModuleLoaded()
        {
        }

        public void OnModuleUnloading()
        {
        }
    }
}
```

#### 第3步：在Shell中注册模块

编辑 `src/Vk.Dbp.WpfWindow/PrismBootstrapper.cs`：

```csharp
protected override void ConfigureModuleCatalog(IModuleCatalog moduleCatalog)
{
    moduleCatalog.AddModule<Vk.Dbp.WorkshopModule.DbpWorkshopModule>();
    moduleCatalog.AddModule<Vk.Dbp.AccountModule.DbpAccountModule>();
    moduleCatalog.AddModule<Vk.Dbp.YourModule.DbpYourModule>();  // 添加新模块
}
```

#### 第4步：添加模块引用

在 `Vk.Dbp.WpfWindow.csproj` 中添加项目引用：

```xml
<ProjectReference Include="..\..\prismModules\Vk.Dbp.YourModule\Vk.Dbp.YourModule.csproj" />
```

#### 推荐的模块目录结构

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

### 核心服务使用

#### 用户会话 (IUserSession)

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
        var realName = _userSession.RealName;
        var hasPermission = _userSession.HasPermission("YourFeature");
    }
}
```

**IUserSession 关键成员**：

| 成员                    | 类型             | 说明                    |
| --------------------- | -------------- | --------------------- |
| `UserId`              | `int`          | 当前用户ID                |
| `Username`            | `string`       | 用户名                   |
| `RealName`            | `string`       | 真实姓名                  |
| `IsLoggedIn`          | `bool`         | 是否已登录                 |
| `Permissions`         | `List<string>` | 权限代码列表                |
| `IsLocked`            | `bool`         | 是否锁屏                  |
| `HasPermission(code)` | `bool`         | 检查是否拥有指定权限            |
| `Login(...)`          | `void`         | 登录（由LoginViewModel调用） |
| `Logout()`            | `void`         | 注销                    |
| `Lock(reason)`        | `void`         | 锁定会话                  |
| `Unlock()`            | `void`         | 解锁会话                  |

#### 数据库访问 (ISqlSugarClient + IRepository<T>)

框架使用SqlSugar ORM，通过DI注入使用：

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

    public async Task<(List<YourEntity> list, int total)> GetPagedAsync(
        int pageIndex, int pageSize)
    {
        return await _db.Queryable<YourEntity>()
            .ToPageListAsync(pageIndex, pageSize);
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

    public async Task<YourEntity> GetByIdAsync(int id)
    {
        return await _repo.GetByIdAsync(id);
    }

    public async Task<int> InsertAsync(YourEntity entity)
    {
        return await _repo.InsertAsync(entity);
    }
}
```

**添加新数据库实体**：

1. 在 `src/Vk.Dbp.Infrastructure/Entities/` 创建实体类
2. 在 `DatabaseInitializer.InitializeDatabase()` 的 `InitTables` 中注册
3. 如需种子数据，在 `InitializeDataAsync()` 中添加

#### 缓存服务 (ICacheService)

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

#### 数据导出 (IExportService)

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

#### ViewModel工厂 (IViewModelFactory)

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

#### 审计日志 (IAuditLogService)

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

#### 全局通知 (IGlobalNotificationPublisher)

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
        await _notificationPublisher.PublishInfoAsync(
            "操作完成", "数据处理已完成", "YourModule");

        await _notificationPublisher.PublishErrorAsync(
            "操作失败", "数据处理异常", "YourModule");

        await _notificationPublisher.PublishToUserAsync(
            targetUserId, "私人消息", "您有一条新任务", NotificationType.Info);
    }
}
```

### 扩展点系统

框架提供三个核心扩展点，模块通过实现接口并注册到DI容器即可自动集成。

#### 扩展菜单 (IMenuItemProvider)

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

| 属性                   | 说明             |
| -------------------- | -------------- |
| `Name`               | 唯一标识，同时作为权限代码  |
| `DisplayName`        | 显示名称           |
| `Icon`               | 图标路径（pack URI） |
| `NavigateTo`         | 导航目标视图名称       |
| `Order`              | 排序（数字越小越靠前）    |
| `RequiredPermission` | 所需权限代码（可选）     |
| `ParentMenu`         | 父菜单名称（用于子菜单）   |

> **注意**：当前菜单权限同时维护在 `MenuPermissionConfig` 静态类中，新增菜单需同步更新该配置。

#### 扩展仪表盘组件 (IDashboardWidgetProvider)

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
            Width = 4,
            Height = 2,
            Order = 10,
            Category = "Production",
            RequiredPermission = "dashboard:your_widget"
        };
    }
}
```

#### 扩展报表 (IReportGenerator)

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

### 事件系统

框架基于Prism EventAggregator定义了以下事件，模块可自由发布和订阅。

#### 告警事件

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
_eventAggregator.GetEvent<AlarmTriggeredEvent>()
    .Subscribe(OnAlarmTriggered);

_eventAggregator.GetEvent<AlarmCountChangedEvent>()
    .Subscribe(OnAlarmCountChanged);
```

**告警等级**：`Info`(0) / `Warning`(1) / `Critical`(2)
**告警状态**：`Active`(0) / `Acknowledged`(1) / `Resolved`(2) / `Ignored`(3)
**告警类型**：`Threshold`(0) / `Device`(1) / `Process`(2) / `System`(3) / `Safety`(4)

#### 全局通知事件

```csharp
// 订阅通知
_eventAggregator.GetEvent<GlobalNotificationEvent>()
    .Subscribe(OnNotificationReceived);

_eventAggregator.GetEvent<NotificationCountChangedEvent>()
    .Subscribe(OnNotificationCountChanged);
```

#### 权限变更事件

```csharp
_eventAggregator.GetEvent<PermissionChangedEvent>()
    .Subscribe(OnPermissionChanged);
// PermissionChangeType: Granted / Revoked / Updated
```

#### 用户登录事件

```csharp
_eventAggregator.GetEvent<UserLoggedInEvent>()
    .Subscribe(OnUserLoggedIn);
```

#### 自定义事件

在 `Vk.Dbp.Contracts/Events/` 中定义自定义事件：

```csharp
using Prism.Events;

namespace Vk.Dbp.Contracts.Events
{
    public class YourDataChangedEvent : PubSubEvent<YourDataChangedPayload>
    {
    }

    public class YourDataChangedPayload
    {
        public int Id { get; set; }
        public string ChangeType { get; set; } = string.Empty;
    }
}
```

### 导航系统

框架使用Prism Region进行视图导航：

**Region定义**（在MainWindow\.xaml中）：

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
        var parameters = new NavigationParameters
        {
            { "Id", id }
        };
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

| 常量                   | 值                    | 说明   |
| -------------------- | -------------------- | ---- |
| `Dashboard`          | "Dashboard"          | 驾驶舱  |
| `LoginView`          | "LoginView"          | 登录页  |
| `SelfCheck`          | "SelfCheck"          | 自检   |
| `Production`         | "Production"         | 生产信息 |
| `AlarmRecord`        | "AlarmRecord"        | 报警记录 |
| `AdminSettingView`   | "AdminSettingView"   | 后台管理 |
| `UserManagementView` | "UserManagementView" | 用户管理 |

### 主题系统

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

主题资源字典位于 `Vk.Dbp.WpfWindow/Themes/`：

- `LightTheme.xaml` - 亮色主题
- `DarkTheme.xaml` - 暗色主题

在XAML中使用 `DynamicResource` 引用主题资源以支持切换：

```xml
<TextBlock Foreground="{DynamicResource PrimaryTextBrush}" />
<Border Background="{DynamicResource PrimaryBackgroundBrush}" />
```

### 会话安全

#### 会话超时自动锁屏

系统通过 `ISessionTimeoutService` 监控用户活动，超时后自动触发锁屏。配置存储在数据库 `SystemConfig` 表中：

| 配置键                     | 说明       | 默认值  |
| ----------------------- | -------- | ---- |
| `SessionTimeoutEnabled` | 是否启用超时锁屏 | True |
| `SessionTimeoutMinutes` | 超时时间（分钟） | 15   |

#### 数据库断连自动锁屏

当SqlSugar检测到数据库连接错误时，自动触发锁屏（`LockScreenService.Lock("数据库连接失败")`）。

#### 锁屏服务

```csharp
public class YourService
{
    private readonly ILockScreenService _lockScreenService;

    public YourService(ILockScreenService lockScreenService)
    {
        _lockScreenService = lockScreenService;
    }

    public void LockForSecurity()
    {
        _lockScreenService.Lock("安全原因锁定");
    }
}
```

### 告警服务

```csharp
public class YourService
{
    private readonly IAlarmService _alarmService;
    private readonly IAlarmConfigService _alarmConfigService;

    public async Task CheckAndTriggerAlarmAsync()
    {
        var configs = await _alarmConfigService.GetEnabledConfigsAsync();

        foreach (var config in configs)
        {
            if (IsThresholdExceeded(config))
            {
                await _alarmService.TriggerAlarmAsync(new AlarmRecord
                {
                    AlarmCode = config.AlarmCode,
                    Level = (AlarmLevel)config.Priority,
                    Title = config.AlarmName,
                    Content = $"当前值超出阈值",
                    Status = AlarmStatus.Active
                });
            }
        }
    }
}
```

告警配置在 `DatabaseInitializer` 中预置了4种示例配置：`TEMP_HIGH`、`PRESSURE_LOW`、`DEVICE_FAULT`、`SYSTEM_INFO`。

### 创建独立业务应用 (dbpApps)

`dbpApps/` 目录用于创建面向特定客户/项目的独立WPF应用，复用框架Shell和模块：

1. 创建新的WPF应用项目
2. 引用 `Vk.Dbp.WpfWindow` 或直接引用所需模块
3. 配置自己的 `appsettings.json`
4. 在 `PrismBootstrapper` 中选择加载的模块

```
dbpApps/Dbp.Material.Forming/    # 成型工艺应用
├── App.xaml(.cs)
├── MainWindow.xaml(.cs)
├── appsettings.json             # 独立的数据库连接配置
└── Dbp.Material.Forming.csproj
```

### 配置系统

应用配置通过 `appsettings.json` + `appsettings.local.json` 管理：

```json
{
  "ConnectionStrings": {
    "Default": ""
  },
  "Redis": {
    "Configuration": "127.0.0.1"
  },
  "Session": {
    "TimeoutMinutes": 15
  }
}
```

- `appsettings.json` - 默认配置（提交到版本库，连接字符串留空）
- `appsettings.local.json` - 本地覆盖配置（不提交，包含实际连接字符串）
- 环境变量 `ConnectionStrings__Default` 也可覆盖连接字符串

运行时配置通过 `IAppSettingsService` 读写：

```csharp
public class YourService
{
    private readonly IAppSettingsService _settingsService;

    public YourService(IAppSettingsService settingsService)
    {
        _settingsService = settingsService;
    }

    public void ReadConfig()
    {
        var value = _settingsService.GetValue<string>("YourKey", "defaultValue");
        _settingsService.SetValue("YourKey", "newValue");
    }
}
```

### 服务注册模式

在 `PrismBootstrapper.RegisterTypes()` 中注册Shell级服务：

```csharp
containerRegistry.RegisterSingleton<IUserSession, UserSession>();
containerRegistry.RegisterSingleton<IExportService, ExportService>();
containerRegistry.RegisterSingleton<IThemeService, ThemeService>();
containerRegistry.RegisterSingleton<ILockScreenService, LockScreenService>();
containerRegistry.RegisterSingleton<ISessionTimeoutService, SessionTimeoutService>();
```

在模块的 `RegisterTypes()` 中注册模块级服务：

```csharp
containerRegistry.RegisterSingleton<IUserService, UserService>();
containerRegistry.Register<IYourService, YourService>();  // 瞬态
containerRegistry.RegisterForNavigation<YourView>();       // 导航视图
```

**注册策略选择**：

| 生命周期       | 适用场景             | 方法                                       |
| ---------- | ---------------- | ---------------------------------------- |
| Singleton  | 共享状态、线程安全、初始化成本高 | `RegisterSingleton<TInterface, TImpl>()` |
| Transient  | 每次需要新实例、包含用户特定状态 | `Register<TInterface, TImpl>()`          |
| Navigation | Prism导航的View     | `RegisterForNavigation<TView>()`         |

**跨模块暴露服务**：

1. 在 `Vk.Dbp.Contracts` 中定义接口
2. 在模块中实现接口
3. 在模块的 `RegisterTypes()` 中注册为Singleton
4. 其他模块通过DI注入使用

### 发布和部署

使用发布脚本：

```powershell
# 基本发布
.\scripts\publish.ps1

# 自包含发布（不依赖.NET运行时）
.\scripts\publish.ps1 -SelfContained -Runtime win-x64

# 单文件发布
.\scripts\publish.ps1 -SelfContained -PublishSingleFile -Runtime win-x64

# 指定版本
.\scripts\publish.ps1 -Version "1.0.0"
```

**发布参数**：

| 参数                   | 默认值       | 说明    |
| -------------------- | --------- | ----- |
| `-Configuration`     | Release   | 构建配置  |
| `-Runtime`           | win-x64   | 目标运行时 |
| `-OutputPath`        | ./publish | 输出目录  |
| `-SelfContained`     | false     | 是否自包含 |
| `-PublishSingleFile` | false     | 是否单文件 |
| `-PublishTrimmed`    | false     | 是否裁剪  |
| `-Version`           | <br />    | 版本号   |

发布后需配置 `appsettings.local.json` 中的数据库连接字符串。

### 测试

```bash
# 运行所有测试
dotnet test

# 运行特定测试项目
dotnet test test/Vk.Dbp.Tests.Unit
```

**测试项目结构**：

- `Vk.Dbp.Tests.Unit` - 单元测试（PasswordHasher、UserService、PermissionService等）
- `Vk.Dbp.Tests.Integration` - 集成测试
- `Vk.Dbp.Tests.Common` - 测试辅助类（TestDatabaseFixture、TestDataFactory）

## 文档

| 文档                                                                     | 说明             |
| ---------------------------------------------------------------------- | -------------- |
| [模块开发指南](docs/MODULE_DEVELOPMENT_GUIDE.md)                             | 详细的模块开发教程和最佳实践 |
| [项目评审和路线图](docs/PROJECT_REVIEW_AND_TODO.md)                            | 项目现状分析和演进计划    |
| [登录会话指南](prismModules/Vk.Dbp.AccountModule/LOGIN_AND_SESSION_GUIDE.md) | 登录和会话管理API参考   |
| [更新日志](CHANGELOG.md)                                                   | 版本变更记录和升级指南    |

## 代码规范

- 命名约定：PascalCase（类、方法、属性），\_camelCase（私有字段），camelCase（局部变量、参数）
- 依赖注入：使用DI，避免直接 `new`，通过 `IViewModelFactory` 创建ViewModel
- ViewModel：不包含业务逻辑，委托给Service层
- 异步方法：使用Async后缀
- 提交规范：遵循 [Conventional Commits](https://www.conventionalcommits.org/)

## 贡献

欢迎贡献！请先阅读 [贡献指南](CONTRIBUTING.md)

## 许可证

MIT License - 详见 [LICENSE](LICENSE)

## 致谢

- [Prism](https://github.com/PrismLibrary/Prism) - MVVM框架
- [HandyControl](https://github.com/HandyOrg/HandyControl) - WPF控件库
- [SqlSugar](https://github.com/DotNetNext/SqlSugar) - ORM框架
- [Serilog](https://github.com/serilog/serilog) - 日志框架
