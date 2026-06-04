# 服务与运维指南

## 服务注册模式

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

| 生命周期 | 适用场景 | 方法 |
|----------|----------|------|
| Singleton | 共享状态、线程安全、初始化成本高 | `RegisterSingleton<TInterface, TImpl>()` |
| Transient | 每次需要新实例、包含用户特定状态 | `Register<TInterface, TImpl>()` |
| Navigation | Prism导航的View | `RegisterForNavigation<TView>()` |

**跨模块暴露服务**：

1. 在 `Vk.Dbp.Contracts` 中定义接口
2. 在模块中实现接口
3. 在模块的 `RegisterTypes()` 中注册为Singleton
4. 其他模块通过DI注入使用

## DI容器注册清单

Bootstrapper中注册的Shell级服务：

| 接口 | 实现 | 生命周期 |
|------|------|----------|
| IAppSettingsService | AppSettingsService | Singleton |
| IThemeService | ThemeService | Singleton |
| IPasswordHasher | PasswordHasher | Singleton |
| IDatabaseInitializer | DatabaseInitializer | Singleton |
| IRepository<> | SqlSugarRepository<> | Transient |
| IMenuPermissionFilter | MenuPermissionFilter | Singleton |
| IUserSession | UserSession | Singleton |
| IExportService | ExportService | Singleton |
| IUiDialogService | UiDialogService | Singleton |
| IAppStartupService | AppStartupService | Singleton |
| ILockScreenService | LockScreenService | Singleton |
| ISessionTimeoutService | SessionTimeoutService | Singleton |
| ICacheService | RedisCacheService / InMemoryCacheService | Singleton |
| IViewModelFactory | ViewModelFactory | Singleton |
| INavigationService | PrismNavigationService | Singleton |
| ISqlSugarClient | SqlSugarScope | Singleton |

## 日志系统

框架使用 **Serilog** 进行结构化日志记录：

### 配置

- **日志位置**：`%LOCALAPPDATA%/<AppName>/Logs/`
- **滚动策略**：按日滚动，单文件最大100MB
- **保留策略**：最多10个文件，保留30天
- **编码**：UTF-8
- **输出格式**：文本 + JSON双格式

### 用户上下文注入

`UserLogEnricher` 自动将当前用户信息注入到每条日志中：

```json
{
  "UserId": 1,
  "Username": "admin",
  "IsLoggedIn": true,
  "Message": "..."
}
```

### 性能日志

`PerformanceLogger` 提供操作计时功能，自动根据耗时分级记录：

| 耗时 | 级别 | 标记 |
|------|------|------|
| < 100ms | Debug | 正常 |
| 100-500ms | Information | 正常 |
| 500-2000ms | Warning | 慢操作 |
| > 2000ms | Warning | 过慢 |

```csharp
using var perf = logger.BeginPerformance("数据导入");
// 执行操作...
perf.LogStep("解析CSV");
// 继续操作...
// Dispose时自动记录总耗时
```

## 应用启动流程

`PrismBootstrapper` 按以下顺序启动应用：

```
1. 构建配置 (appsettings.json → appsettings.local.json → 环境变量)
       ↓
2. 配置 Serilog 日志（文件滚动、用户上下文）
       ↓
3. 验证配置（连接字符串必填；启用 Redis 时连接串必填）
       ↓
4. 注册缓存服务（默认内存缓存；显式启用时切换到 Redis）
       ↓
5. 配置 SqlSugar（自动关闭连接、AOP日志、连接失败触发锁屏）
       ↓
6. 显示启动画面（"正在初始化数据库，请稍候..."）
       ↓
7. 执行 DatabaseInitializer.InitializeAsync()
   ├── CodeFirst 创建/更新 13张表
   ├── 确保 NVARCHAR 列（Unicode支持）
   ├── 初始化管理员账号（环境变量密码）
   ├── 初始化角色和权限
   └── 初始化系统配置和告警配置
       ↓
8. 显示主窗口
   ├── 已登录 → 导航到 Dashboard
   └── 未登录 → 导航到 LoginView
       ↓
9. 启动会话超时监控（读取SystemConfig配置）
```

### 缓存配置

Shell 在启动时根据 `Redis` 配置节注册 `ICacheService`：

```json
{
  "Redis": {
    "Enabled": false,
    "ConnectionString": "",
    "InstanceName": "Vk.Dbp"
  }
}
```

- 默认 `Enabled=false`，注册 `InMemoryCacheService`
- `Enabled=true` 且配置了连接串时，注册 `RedisCacheService`
- 如果 Redis 初始化失败，启动阶段会记录告警日志并回退到 `InMemoryCacheService`
- 可通过环境变量 `Redis__Enabled`、`Redis__ConnectionString`、`Redis__InstanceName` 覆盖

## 脚本工具

### 本地启动脚本 (start-wpf-local.ps1)

```powershell
# 使用LocalDB启动（默认）
.\scripts\start-wpf-local.ps1

# 指定连接字符串
.\scripts\start-wpf-local.ps1 -ConnectionString "Server=.;Database=DabpCore;Trusted_Connection=True;"

# 首次运行（初始化数据库+设置管理员密码）
.\scripts\start-wpf-local.ps1 -FirstRun -AdminPassword "your-secure-password"
```

**脚本功能**：
1. 检查 `sqllocaldb` 是否可用
2. 启动 MSSQLLocalDB 实例
3. 设置 `ConnectionStrings__Default` 环境变量
4. 首次运行时验证管理员密码参数
5. 执行 `dotnet run --project src/Vk.Dbp.WpfWindow`

### 发布脚本 (publish.ps1)

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

| 参数 | 默认值 | 说明 |
|------|--------|------|
| `-Configuration` | Release | 构建配置 |
| `-Runtime` | win-x64 | 目标运行时（支持 win-x64/win-x86/win-arm64/linux-x64/osx-x64） |
| `-OutputPath` | ./publish | 输出目录 |
| `-SelfContained` | false | 是否自包含 |
| `-PublishSingleFile` | false | 是否单文件 |
| `-PublishTrimmed` | false | 是否裁剪 |
| `-Version` | | 版本号 |

发布脚本会自动：
- 清理旧发布目录
- 复制 `appsettings.json` 和 `appsettings.local.example.json` 到输出目录
- 生成 `release-info.json`（包含构建日期、配置、运行时、Git提交/分支信息）
- 输出统计信息（文件数量、总大小、构建耗时）

## 测试

```bash
# 运行所有测试
dotnet test

# 运行单元测试（推荐日常使用）
dotnet test test/Vk.Dbp.Tests.Unit

# 运行单元测试并生成覆盖率报告
dotnet test test/Vk.Dbp.Tests.Unit /p:CollectCoverage=true /p:CoverletOutputFormat=cobertura
```

**测试项目结构**：

- `Vk.Dbp.Tests.Unit` - 单元测试（54+测试用例，覆盖率高）
- `Vk.Dbp.Tests.Integration` - 集成测试（脚手架，待实现）
- `Vk.Dbp.Tests.Common` - 测试辅助（TestDatabaseFixture使用SQLite内存数据库、TestDataFactory工厂方法）

**测试类和覆盖范围**：

| 测试类 | 测试数量 | 覆盖内容 |
|--------|----------|----------|
| `UserServiceTests` | 10 | 用户查询（分页+关键字）、CRUD、启用禁用、改密、角色分配 |
| `RoleServiceTests` | 7 | 角色CRUD、权限分配、级联删除、启用禁用 |
| `PermissionServiceTests` | 5 | 用户权限查询（多角色联合）、权限检查、权限树 |
| `OrganizationServiceTests` | 8 | 组织CRUD、树形构建（3级）、用户分配/移除、幂等性 |
| `NotificationServiceTests` | 2 | 通知持久化、已读/未读/删除生命周期 |
| `PasswordHasherTests` | 7 | 加盐哈希、正确/错误验证、空值处理、防时序攻击 |
| `UserSessionTests` | 10 | 登录/注销、权限检查（含admin绕过）、锁屏/解锁 |
| `AppConfigurationBuilderTests` | 3 | 配置层级覆盖（local覆盖json、环境变量覆盖local） |
| `AdminShellViewModelTests` | 2 | Shell ViewModel菜单选择 |

**测试模式**：
- 使用 `IClassFixture<TestDatabaseFixture>` 共享SQLite内存数据库
- 使用 `Moq` mock外部依赖（IAuditLogService、IPasswordHasher等）
- 使用 `FluentAssertions` 进行断言
- 覆盖率目标 >= 40%，Cobertura + JSON格式输出

## 账户管理模块详解

`Vk.Dbp.AccountModule` 是框架的核心业务模块，提供完整的账户管理功能：

### 视图清单

| 视图 | 功能说明 |
|------|----------|
| LoginView | 用户登录（用户名/密码、记住用户名、加载指示器、错误提示） |
| ChangePasswordView | 修改密码（旧密码验证、新密码最少6位、新旧不能相同） |
| AdminSettingView | 后台管理入口（占位） |
| UserManagementView | 用户管理（分页列表、关键字搜索、增删改查、重置密码、启用禁用、CSV导出） |
| RoleManagementView | 角色管理（CRUD、权限分配、启用禁用、级联删除保护） |
| PermissionManagementView | 权限管理（CRUD、按类型/模块/关键字过滤、内联编辑、启用禁用） |
| OrganizationManagementView | 组织管理（树形结构、创建根/子组织、用户分配/移除） |
| AuditLogView | 审计日志查看（日期范围搜索、Excel导出） |
| SystemSettingsView | 系统设置（会话超时配置，需SystemConfig:Edit权限） |
| AlarmConfigView | 告警配置管理（CRUD、阈值设置、比较类型、弹窗/声音配置） |

### 服务清单

| 服务 | 主要方法 |
|------|----------|
| IUserService | GetAll、GetPaged（N+1优化）、GetById/ByUsername、Create/Update/Delete、Enable、ChangePassword、ResetPassword（强密码）、AssignRoles（事务替换） |
| IRoleService | GetAll、Create/Update/Delete（级联）、AssignPermissions（替换）、GetRolePermissions、EnableRole |
| IPermissionService | GetAll、GetPermissionTree（层级）、GetUserPermissions（多角色联合查询）、HasPermission |
| IOrganizationService | GetAll、BuildOrganizationTree（递归树）、AssignUsers（幂等）、GetOrgUsers（过滤软删除） |
| INotificationService | GetByUserId、MarkAsRead/MarkAllAsRead、GetUnreadCount |
| IGlobalNotificationPublisher | PublishGlobal、PublishToUser/CurrentUser、便捷方法（Error/Warning/Info/Success/System） |
| ISystemConfigService | Get/SetConfigValue、GetInt/BoolConfig、SessionTimeout专用方法 |
| IAlarmService | GetRecords（过滤+分页）、TriggerAlarm、Acknowledge/Resolve/Ignore、AcknowledgeAll、统计方法 |
| IAlarmConfigService | GetConfigs、SaveConfig、DeleteConfig、GetEnabled、ValidateThreshold（6种比较方式） |
| IAuditLogService | GetAll/ByUser/ByAction/ByDateRange/ByModule、LogOperation/LogFailure、ExportLogs、DeleteOldLogs |
| IPermissionChecker | IsGranted（同步/异步、当前用户/指定用户、admin绕过） |

## 车间管理模块详解

`Vk.Dbp.WorkshopModule` 是示例业务模块，展示如何开发业务功能：

### 视图清单

| 视图 | 实现状态 | 说明 |
|------|----------|------|
| Dashboard | 占位 | 驾驶舱/仪表盘 |
| Production | 占位 | 生产信息监控 |
| SelfCheck | 占位 | 设备自检 |
| **AlarmRecord** | **完整实现** | 告警记录管理（分页、级别/状态过滤、统计面板、确认/解决/忽略、实时事件更新、Excel导出含中文枚举映射） |
| AuditRecord | 占位 | 审计记录查看 |
| ProductionRecord | 占位 | 生产记录查看 |

> **提示**：开发新业务模块时，可参考 AlarmRecordViewModel 的实现模式（DI注入、EventAggregator订阅、分页查询、导出功能、IDisposable清理）。

## 代码规范

### 命名约定

| 目标 | 风格 | 示例 |
|------|------|------|
| 类、方法、属性 | PascalCase | `UserService`, `GetAllUsersAsync` |
| 私有字段 | \_camelCase | `_userSession` |
| 局部变量、参数 | camelCase | `pageIndex` |
| 常量 | PascalCase | `ContentRegion` |

### C#编码约定

- 缩进：4空格
- 大括号风格：Allman（独占一行）
- 依赖注入：使用DI，避免直接 `new`，通过 `IViewModelFactory` 创建ViewModel
- ViewModel：不包含业务逻辑，委托给Service层
- 异步方法：使用 `Async` 后缀，返回 `Task`/`Task<T>`
- XAML属性：按字母顺序排列，使用 `DynamicResource` 引用主题资源

### 分支和提交

- 分支命名：`feature/xxx`、`fix/xxx`、`docs/xxx`、`refactor/xxx`、`test/xxx`
- 提交规范：遵循 [Conventional Commits](https://www.conventionalcommits.org/)（`feat:`、`fix:`、`docs:`、`refactor:`、`test:` 等前缀）
- PR流程：CI检查通过 + 至少1位维护者审核 + Squash合并
