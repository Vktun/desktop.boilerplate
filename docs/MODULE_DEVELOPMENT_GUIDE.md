# Desktop Boilerplate 模块开发指南

本文档为Desktop Boilerplate框架提供完整的模块开发指南，帮助开发者快速创建功能模块。

---

## 目录

1. [快速开始](#快速开始)
2. [模块结构规范](#模块结构规范)
3. [服务注册最佳实践](#服务注册最佳实践)
4. [模块间通信](#模块间通信)
5. [扩展点使用](#扩展点使用)
6. [测试模块](#测试模块)
7. [打包和分发](#打包和分发)

---

## 快速开始

### 创建新模块的步骤

1. **创建模块项目**

```bash
# 在prismModules目录下创建新模块
mkdir Vk.Dbp.YourModule
cd Vk.Dbp.YourModule
dotnet new classlib -n Vk.Dbp.YourModule
```

2. **添加必要的引用**

编辑 `.csproj` 文件:

```xml
<ItemGroup>
  <PackageReference Include="Prism.Unity" Version="9.0.*" />
  <PackageReference Include="HandyControl" Version="3.5.*" />
  <ProjectReference Include="..\..\src\Vk.Dbp.Contracts\Vk.Dbp.Contracts.csproj" />
</ItemGroup>
```

3. **创建模块入口类**

```csharp
using Prism.Ioc;
using Prism.Modularity;
using Vk.Dbp.Contracts.Modules;

namespace Vk.Dbp.YourModule
{
    [ModuleDependency("AccountModule")] // 声明依赖
    public class YourModule : IModule, IModuleMetadata
    {
        public string ModuleName => "YourModule";
        public string Version => "1.0.0";
        public string Description => "您的模块描述";
        public string[] Dependencies => new[] { "AccountModule" };
        public string[] ProvidedServices => new[] { "IYourService" };
        
        public void RegisterTypes(IContainerRegistry containerRegistry)
        {
            // 注册服务
            containerRegistry.RegisterSingleton<IYourService, YourService>();
            
            // 注册视图
            containerRegistry.RegisterForNavigation<YourView>();
        }
        
        public void OnInitialized(IContainerProvider containerProvider)
        {
            OnModuleLoaded();
        }
        
        public void OnModuleLoaded()
        {
            // 模块加载完成后的初始化逻辑
        }
        
        public void OnModuleUnloading()
        {
            // 模块卸载时的清理逻辑
        }
    }
}
```

4. **在主程序注册模块**

编辑 `src/Vk.Dbp.WpfWindow/PrismBootstrapper.cs`:

```csharp
protected override void ConfigureModuleCatalog(IModuleCatalog moduleCatalog)
{
    moduleCatalog.AddModule<Vk.Dbp.WorkshopModule.DbpWorkshopModule>();
    moduleCatalog.AddModule<Vk.Dbp.AccountModule.DbpAccountModule>();
    moduleCatalog.AddModule<Vk.Dbp.YourModule.YourModule>(); // 添加新模块
}
```

### 最小模块模板

完整的最小模块结构:

```
Vk.Dbp.YourModule/
├── Vk.Dbp.YourModule.csproj
├── YourModule.cs              # 模块入口
├── Views/
│   └── YourView.xaml(.cs)
├── ViewModels/
│   └── YourViewModel.cs
├── Services/
│   ├── IYourService.cs
│   └── YourService.cs
└── Models/
    └── YourModel.cs
```

---

## 模块结构规范

### 推荐目录结构

```
Vk.Dbp.YourModule/
├── Vk.Dbp.YourModule.csproj    # 项目文件
├── YourModule.cs               # 模块入口（IModule实现）
│
├── Views/                      # 视图层
│   ├── YourView.xaml           # XAML视图
│   ├── YourView.xaml.cs        # 代码后台
│   └── Dialogs/                # 对话框视图
│       └── YourDialog.xaml(.cs)
│
├── ViewModels/                 # 视图模型层
│   ├── YourViewModel.cs        # 主ViewModel
│   └── Dialogs/
│       └── YourDialogViewModel.cs
│
├── Services/                   # 服务层
│   ├── IYourService.cs         # 服务接口
│   └── YourService.cs          # 服务实现
│
├── Models/                     # 数据模型
│   ├── YourModel.cs
│   └── Dtos/                   # 数据传输对象
│       └── YourDto.cs
│
├── Converters/                 # 值转换器（可选）
│   └── YourConverter.cs
│
├── Resources/                  # 资源文件（可选）
│   ├── Styles.xaml             # 样式
│   └── Images/                 # 图片资源
│
└── Constants/                  # 常量定义（可选）
    └── ViewNames.cs
```

### 命名规范

| 类型 | 命名规则 | 示例 |
|------|----------|------|
| 模块类 | `{ModuleName}Module` | `WorkshopModule` |
| 视图 | `{Name}View` | `ProductionView` |
| ViewModel | `{Name}ViewModel` | `ProductionViewModel` |
| 服务接口 | `I{ServiceName}Service` | `IProductionService` |
| 服务实现 | `{ServiceName}Service` | `ProductionService` |
| 模型 | `{Name}Model` | `ProductionModel` |
| DTO | `{Name}Dto` | `ProductionDto` |

---

## 服务注册最佳实践

### Singleton vs Transient

**使用 Singleton 当:**
- 服务维护共享状态（如 `IUserSession`）
- 服务是线程安全的
- 服务初始化成本高

```csharp
containerRegistry.RegisterSingleton<IUserService, UserService>();
```

**使用 Transient 当:**
- 每次需要新实例
- 服务包含用户特定状态

```csharp
containerRegistry.Register<IReportGenerator, ReportGenerator>();
```

### 如何暴露服务给其他模块

1. **在Contracts项目中定义接口**

```csharp
// src/Vk.Dbp.Contracts/Services/IYourService.cs
public interface IYourService
{
    Task<IEnumerable<YourData>> GetDataAsync();
}
```

2. **在模块中实现接口**

```csharp
// prismModules/Vk.Dbp.YourModule/Services/YourService.cs
public class YourService : IYourService
{
    public async Task<IEnumerable<YourData>> GetDataAsync()
    {
        // 实现逻辑
    }
}
```

3. **注册服务**

```csharp
// YourModule.cs
public void RegisterTypes(IContainerRegistry containerRegistry)
{
    containerRegistry.RegisterSingleton<IYourService, YourService>();
}
```

4. **在其他模块中使用**

```csharp
public class OtherViewModel
{
    private readonly IYourService _yourService;
    
    public OtherViewModel(IYourService yourService)
    {
        _yourService = yourService;
    }
}
```

### 接口与实现分离原则

**原则**: 模块只暴露接口，实现细节对其他模块不可见。

**好处**:
- 降低耦合度
- 便于单元测试
- 支持替换实现

---

## 模块间通信

### 使用EventAggregator解耦

Prism提供了`EventAggregator`用于模块间松散耦合的通信。

**发布事件:**

```csharp
public class YourViewModel
{
    private readonly IEventAggregator _eventAggregator;
    
    public YourViewModel(IEventAggregator eventAggregator)
    {
        _eventAggregator = eventAggregator;
    }
    
    public void OnDataChanged()
    {
        _eventAggregator.GetEvent<DataChangedEvent>()
            .Publish(new DataChangedEventArgs { Id = 1 });
    }
}
```

**订阅事件:**

```csharp
public class OtherViewModel
{
    public OtherViewModel(IEventAggregator eventAggregator)
    {
        eventAggregator.GetEvent<DataChangedEvent>()
            .Subscribe(OnDataChanged);
    }
    
    private void OnDataChanged(DataChangedEventArgs args)
    {
        // 处理事件
    }
}
```

### 通过共享服务通信

模块可以共享服务实例来进行通信。

```csharp
// 定义共享服务
public interface ISharedDataService
{
    event EventHandler<DataChangedEventArgs>? DataChanged;
    void NotifyDataChanged(DataChangedEventArgs args);
}

// 在AccountModule中注册
containerRegistry.RegisterSingleton<ISharedDataService, SharedDataService>();

// 在其他模块中使用
public class YourViewModel
{
    public YourViewModel(ISharedDataService sharedDataService)
    {
        sharedDataService.DataChanged += OnDataChanged;
    }
}
```

### 避免循环依赖

**循环依赖示例**:
```
ModuleA → ModuleB → ModuleA (错误!)
```

**解决方案**:
1. 重构模块结构，提取共享功能到基础模块
2. 使用EventAggregator通信
3. 使用中介者模式

---

## 扩展点使用

Desktop Boilerplate提供了丰富的扩展点，让模块可以扩展系统功能。

### 添加自定义菜单

**步骤1: 实现IMenuItemProvider**

```csharp
using Vk.Dbp.Contracts.Extensions;

public class YourMenuProvider : IMenuItemProvider
{
    public IEnumerable<MenuItemInfo> GetMenuItems()
    {
        yield return new MenuItemInfo
        {
            Name = "your_feature",
            DisplayName = "自定义功能",
            Icon = "pack://application:,,,/Resources/Icons/icon.png",
            NavigateTo = "YourView",
            Order = 100,
            RequiredPermission = "your_feature:view"
        };
    }
}
```

**步骤2: 注册提供者**

```csharp
public void RegisterTypes(IContainerRegistry containerRegistry)
{
    containerRegistry.RegisterSingleton<IMenuItemProvider, YourMenuProvider>();
}
```

### 添加仪表盘组件

**步骤1: 实现IDashboardWidgetProvider**

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
            RequiredPermission = "dashboard:your_widget"
        };
    }
}
```

**步骤2: 创建小组件视图**

```xml
<!-- Views/YourWidgetView.xaml -->
<UserControl x:Class="Vk.Dbp.YourModule.Views.YourWidgetView"
             xmlns="http://schemas.microsoft.com/winfx/2006/xaml/presentation">
    <Grid>
        <TextBlock Text="实时数据内容" />
    </Grid>
</UserControl>
```

### 添加报表类型

**步骤1: 实现IReportGenerator**

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

---

## 测试模块

### 单元测试模块服务

```csharp
using Xunit;
using Moq;
using FluentAssertions;

public class YourServiceTests
{
    [Fact]
    public async Task GetDataAsync_ReturnsExpectedData()
    {
        // Arrange
        var mockDb = new Mock<ISqlSugarClient>();
        var service = new YourService(mockDb.Object);
        
        // Act
        var result = await service.GetDataAsync();
        
        // Assert
        result.Should().NotBeNullOrEmpty();
    }
}
```

### 集成测试模块交互

```csharp
[Collection("ModuleCollection")]
public class YourModuleIntegrationTests
{
    private readonly IContainerProvider _container;
    
    public YourModuleIntegrationTests(ModuleFixture fixture)
    {
        _container = fixture.Container;
    }
    
    [Fact]
    public void Module_LoadsSuccessfully()
    {
        // 验证模块加载
        var module = _container.Resolve<IModuleCatalog>()
            .Modules.FirstOrDefault(m => m.ModuleName == "YourModule");
        
        module.Should().NotBeNull();
        module.State.Should().Be(ModuleState.Initialized);
    }
}
```

### Mock依赖技巧

使用Moq模拟依赖项:

```csharp
// 模拟IUserSession
var mockUserSession = new Mock<IUserSession>();
mockUserSession.Setup(u => u.IsLoggedIn).Returns(true);
mockUserSession.Setup(u => u.UserId).Returns(1);

// 模拟ISqlSugarClient
var mockDb = new Mock<ISqlSugarClient>();
mockDb.Setup(db => db.Queryable<YourEntity>())
      .Returns(Mock.Of<IQueryable<YourEntity>>);

// 注入Mock对象
var service = new YourService(mockDb.Object, mockUserSession.Object);
```

---

## 打包和分发

### 打包为NuGet

1. **创建.nuspec文件**

```xml
<?xml version="1.0"?>
<package>
  <metadata>
    <id>Vk.Dbp.YourModule</id>
    <version>1.0.0</version>
    <title>Your Module</title>
    <authors>Your Name</authors>
    <description>Your module description</description>
    <dependencies>
      <dependency id="Prism.Unity" version="9.0.0" />
    </dependencies>
  </metadata>
  <files>
    <file src="bin\Release\**\Vk.Dbp.YourModule.dll" target="lib\net10.0\" />
  </files>
</package>
```

2. **打包**

```bash
dotnet build -c Release
nuget pack YourModule.nuspec
```

### 版本兼容性注意事项

- 遵循语义化版本（Semantic Versioning）
- 主版本号变更表示不兼容的API修改
- 次版本号变更表示向后兼容的功能新增
- 修订号变更表示向后兼容的问题修正

示例:
- `1.0.0` → `1.0.1` (Bug修复)
- `1.0.1` → `1.1.0` (新功能)
- `1.1.0` → `2.0.0` (重大变更)

### 文档要求

发布模块时应包含:
1. **README.md** - 模块介绍和快速开始
2. **CHANGELOG.md** - 版本变更记录
3. **API文档** - 公共API说明
4. **示例代码** - 使用示例

---

## 常见问题

### Q: 如何访问用户会话信息？

```csharp
public class YourViewModel
{
    private readonly IUserSession _userSession;
    
    public YourViewModel(IUserSession userSession)
    {
        _userSession = userSession;
        
        // 访问当前用户信息
        var userId = _userSession.UserId;
        var username = _userSession.Username;
        var hasPermission = _userSession.HasPermission("your_permission");
    }
}
```

### Q: 如何使用ViewModel工厂创建ViewModel？

```csharp
public class YourViewModel
{
    private readonly IViewModelFactory _viewModelFactory;
    
    public YourViewModel(IViewModelFactory viewModelFactory)
    {
        _viewModelFactory = viewModelFactory;
    }
    
    public void ShowDialog()
    {
        // 使用工厂创建ViewModel
        var dialogViewModel = _viewModelFactory.Create<YourDialogViewModel>();
        // 而不是: var dialogViewModel = new YourDialogViewModel();
    }
}
```

### Q: 如何使用缓存服务？

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
        // 使用缓存（5分钟过期）
        return await _cacheService.GetOrCreateAsync(
            "your_data_cache_key",
            () => FetchDataFromDatabaseAsync(),
            TimeSpan.FromMinutes(5)
        );
    }
}
```

---

## 最佳实践总结

1. **单一职责**: 每个模块只负责一个业务领域
2. **依赖注入**: 所有依赖通过构造函数注入
3. **接口隔离**: 模块间只通过接口通信
4. **事件驱动**: 使用EventAggregator解耦模块间通信
5. **扩展优先**: 利用扩展点而非修改核心代码
6. **测试覆盖**: 为核心业务逻辑编写单元测试
7. **文档完善**: 提供清晰的模块文档和示例

---

## 参考资源

- [Prism官方文档](https://prismlibrary.com/docs/)
- [HandyControl Wiki](https://github.com/HandyOrg/HandyControl/wiki)
- [SqlSugar文档](https://www.donet5.com/Home/Doc)
- [项目架构文档](../../docs/ARCHITECTURE.md)

---

**更新时间**: 2026-04-06  
**版本**: 1.0.0