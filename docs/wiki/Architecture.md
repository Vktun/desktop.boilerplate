# 架构概览

## 分层架构

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

## 项目结构

解决方案 `desktop.boilerplate.slnx` 包含 **17个项目**，分布在5个目录中：

```
desktop.boilerplate/
├── src/                              # 核心源代码
│   ├── Vk.Dbp.WpfWindow/            # 主应用程序（Shell宿主，WinExe）
│   │   ├── PrismBootstrapper.cs     #   启动引导器（DI注册、模块加载、DB初始化、Serilog配置）
│   │   ├── MainWindow.xaml          #   主窗口（HeaderRegion + ContentRegion）
│   │   ├── Layout/                  #   布局视图（HeaderView、DefaultContentView）
│   │   ├── ViewModels/              #   Shell级ViewModel（Header、LockScreen、Alarm、Notification）
│   │   ├── Views/                   #   Shell级View（锁屏窗口、告警弹窗、通知面板）
│   │   ├── Services/                #   Shell级服务（主题、锁屏、会话超时、菜单权限、ViewModel工厂、UI对话框、启动服务）
│   │   ├── Constants/               #   导航常量（RegionNames、ViewNames、AccountActions）
│   │   ├── Converters/              #   通用值转换器（告警等级/状态颜色、头像首字、布尔反转等7个）
│   │   ├── Logging/                 #   Serilog配置（UserLogEnricher用户上下文注入）
│   │   └── Themes/                  #   Light/Dark主题资源字典
│   │
│   ├── Vk.Dbp.AdminWindow/          # 管理窗口应用（WinExe）
│   │
│   ├── Vk.Dbp.Contracts/            # 模块契约定义层（二次开发核心引用）
│   │   ├── Modules/                 #   IModuleMetadata、IModuleLifecycle、ModuleDependencyAttribute
│   │   ├── Extensions/              #   IMenuItemProvider、IDashboardWidgetProvider、IReportGenerator
│   │   ├── Services/                #   IViewModelFactory、IExportService、INavigationService、IUiDialogService等
│   │   ├── Events/                  #   Prism事件定义（告警、通知、权限变更、用户登录）
│   │   ├── Caching/                 #   ICacheService
│   │   ├── Data/                    #   IPagedQuery<T>、PagedResult<T>、PagedQuery<T>
│   │   └── Navigation/              #   ShellMenuDefinition、ShellMenuDefinitions（12项菜单定义）
│   │
│   ├── Vk.Dbp.Services/             # 通用服务实现
│   │   ├── Session/                 #   IUserSession / UserSession（用户会话状态、权限检查、锁屏）
│   │   ├── Alarm/                   #   IAlarmService / IAlarmConfigService
│   │   ├── Audit/                   #   IAuditLogService、AuditLogExtensions扩展方法、AuditIdentityExtensions
│   │   ├── Caching/                 #   InMemoryCacheService（ConcurrentDictionary、模式匹配、自动清理）
│   │   ├── Export/                  #   ExportService（CSV反射导出、ClosedXML Excel、QuestPDF PDF、CSV导入）
│   │   └── Settings/                #   IAppSettingsService / AppSettingsService（持久化到%LOCALAPPDATA%）
│   │
│   ├── Vk.Dbp.Infrastructure/       # 基础设施层
│   │   ├── Entities/                #   13个数据库实体（User、Role、Permission、AlarmRecord等）
│   │   ├── Repositories/            #   IRepository<T> / SqlSugarRepository<T>
│   │   ├── OrmSetting/              #   SqlSugar FluentAPI配置（SqlSugarFluentService）
│   │   └── DatabaseInitializer.cs   #   数据库初始化（CodeFirst建13张表+种子数据）
│   │
│   ├── Vk.Dbp.Domain/               # 领域模型（预留层，当前为桩）
│   │
│   ├── Vk.Dbp.Utils/                # 工具类
│   │   ├── Security/                #   IPasswordHasher / PasswordHasher（PBKDF2+SHA256，100000次迭代）
│   │   ├── Algorithm/               #   SM4国密加解密（CBC/PKCS7，BouncyCastle）
│   │   ├── IdGenerator/             #   Yitter雪花ID生成器配置
│   │   └── Logging/                 #   PerformanceLogger（操作计时、分步计时、慢操作告警）
│   │
│   └── Vk.Dbp.Tools/                # 工具项目
│
├── prismModules/                     # Prism业务模块
│   ├── Vk.Dbp.AccountModule/        # 账户管理模块（10个视图、13个ViewModel、11个服务）
│   │   ├── DbpAccountModule.cs     #   模块入口（注册视图和服务）
│   │   ├── Views/                  #   登录、修改密码、用户管理、角色管理、权限管理、
│   │   │                           #   组织管理、审计日志、系统设置、告警配置等13个XAML视图
│   │   ├── ViewModels/             #   LoginVM、UserManagementVM、RoleManagementVM等13个
│   │   ├── Services/               #   UserService、RoleService、PermissionService等17个服务文件
│   │   ├── Models/                 #   User、Role、Permission、OrganizationUnitModel、Notification
│   │   └── Converters/             #   MultiValueToArrayConverter
│   │
│   └── Vk.Dbp.WorkshopModule/      # 车间管理模块（示例业务模块，6个视图）
│       ├── DbpWorkshopModule.cs    #   模块入口
│       ├── Views/                  #   Dashboard、Production、SelfCheck、AlarmRecord等6个
│       └── ViewModels/             #   AlarmRecordVM（完整实现）、其余为占位
│
├── dbpframework/                     # 框架层
│   ├── Vk.Dbp.Core/                # 核心抽象（IDbpModule接口）
│   └── Vk.Dbp.Account/             # 账户领域核心（ICurrentUser、PermissionDto、RoleDto）
│
├── dbpApps/                          # 独立业务应用（客户项目入口，脚手架）
│   ├── Dbp.Material.Forming/       # 成型工艺应用
│   └── Dbp.Material.Mixing/        # 混合工艺应用
│
├── test/                             # 测试项目（54+单元测试）
│   ├── Vk.Dbp.Tests.Unit/          # 单元测试（9个测试类，xUnit+Moq+FluentAssertions）
│   ├── Vk.Dbp.Tests.Integration/   # 集成测试（脚手架）
│   └── Vk.Dbp.Tests.Common/        # 测试辅助（TestDatabaseFixture SQLite、TestDataFactory）
│
├── scripts/                          # 脚本
│   ├── start-wpf-local.ps1         # 本地启动脚本
│   └── publish.ps1                  # 发布脚本
│
└── docs/                             # 文档
    ├── wiki/                        # Wiki文档
    ├── MODULE_DEVELOPMENT_GUIDE.md  # 模块开发指南
    ├── PROJECT_REVIEW_AND_TODO.md   # 项目评审和演进计划
    └── LOCAL_CONFIGURATION.md       # 本地配置说明
```

## 技术栈

| 分类 | 技术 | 版本 | 用途 |
|------|------|------|------|
| 框架 | .NET | 10 | 运行时 |
| UI框架 | WPF | net10.0-windows | 桌面界面 |
| MVVM框架 | Prism (Unity) | 9.0 | 模块化、导航、依赖注入、事件聚合 |
| 控件库 | HandyControl | 3.5 | WPF UI组件 |
| ORM | SqlSugar | 5.1.4 | CodeFirst数据库初始化、查询 |
| 日志 | Serilog | - | 结构化日志，文件滚动输出 |
| Excel | ClosedXML | - | Excel导出（支持列映射、枚举映射、冻结表头） |
| PDF | QuestPDF | - | PDF报表生成（A4横向、表格布局） |
| 加密 | BouncyCastle | - | PBKDF2密码哈希、SM4国密加解密 |
| ID生成 | Yitter.IdGenerator | - | 分布式雪花ID |
| 序列化 | System.Text.Json | - | JSON序列化 |
| 测试 | xUnit + Moq + FluentAssertions | - | 单元测试、Mock、断言 |
| 覆盖率 | coverlet | 6.0 | 代码覆盖率（目标>=40%，Cobertura格式） |

## 依赖方向规则

- 依赖向内流动：表示层 → 应用层 → 领域层 → 基础设施层
- `dbpframework` 是最内层；不得依赖 `src/` 或 `prismModules/`
- 业务模块不得直接相互依赖；使用 `Vk.Dbp.Contracts` 共享契约
- ViewModel 不得直接访问 SqlSugar 或仓储类型；使用服务边界
