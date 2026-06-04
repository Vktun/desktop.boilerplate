# Desktop Boilerplate 项目评审与演进计划

评审日期：2026-06-03

## 1. 当前项目状态

本仓库已具备桌面应用平台的基本形态：

- WPF + Prism + HandyControl 宿主 Shell
- Prism 模块化架构（`AccountModule`、`WorkshopModule`）
- 基于 SqlSugar 的持久化层（13张表、CodeFirst 自动建表、种子数据）
- 完整的用户/角色/权限/组织/审计体系
- 主题切换（Light/Dark）、锁屏、告警、通知
- SM4 国密加解密、PBKDF2 密码哈希
- 数据导出（CSV/Excel/PDF）、缓存服务、分页查询
- 跨模块事件系统（IEventAggregator）
- 扩展点系统（IMenuItemProvider、IDashboardWidgetProvider、IReportGenerator）
- `dbpApps` 下的客户应用入口
- 81个单元测试（xUnit + Moq + FluentAssertions）

从工程角度看，当前状态为：

`可用的管理后台 + 可扩展的工业桌面 Shell 基座`

距离完整的上位机二次开发平台仍有差距，但核心架构已稳定。

## 2. 已完成的能力

### 2.1 基础架构

- Shell、Region、模块注册已分离，遵循 Prism 约定
- 仓库结构已拆分为 `src`、`prismModules`、`dbpframework`、`dbpApps`
- 分层依赖方向正确：模块 → Contracts → Services → Infrastructure → Utils
- 解决方案可成功构建，0 错误

### 2.2 账户与安全

- 用户/角色/权限/组织 CRUD 完整实现
- PBKDF2 + SHA-256 密码哈希（100,000次迭代、随机盐、防时序攻击）
- `IUserSession`/`IUserInfo` 接口分层（轻量审计 vs 完整会话）
- 审计日志持久化到数据库（`IAuditLogService`，支持11种操作类型）
- 会话超时自动锁屏（可配置1-480分钟）
- 数据库断连自动锁屏
- SM4/CBC/PKCS7 国密加解密（支持加密连接字符串）
- Token 使用 `RandomNumberGenerator` 生成，仅内存持有

### 2.3 平台服务

- `IAppSettingsService` — 运行时配置持久化（%LOCALAPPDATA%）
- `ISystemConfigService` — 数据库级系统配置
- `ICacheService` — 内存缓存（ConcurrentDictionary、模式匹配失效）
- `IExportService` — CSV/Excel(ClosedXML)/PDF(QuestPDF) 导出
- `INavigationService` — Contracts 层导航封装
- `IThemeService` — 主题切换 + 偏好持久化
- `IViewModelFactory` — ViewModel 工厂
- `IGlobalNotificationPublisher` — 跨模块通知
- `IAlarmService`/`IAlarmConfigService` — 告警管理与配置

### 2.4 扩展点

- `IMenuItemProvider` — 菜单扩展
- `IDashboardWidgetProvider` — 仪表盘组件扩展
- `IReportGenerator` — 报表扩展
- Prism `PubSubEvent<T>` — 跨模块事件

### 2.5 测试

- 81个单元测试全部通过
- 覆盖：UserService、RoleService、PermissionService、OrganizationService、NotificationService、PasswordHasher、UserSession、AppConfigurationBuilder、AdminShellViewModel、SM4
- SQLite 内存数据库 + TestDatabaseFixture 共享

## 3. 仍需改进的差距

### P0：上位机核心能力尚未具备

- `WorkshopModule` 仍以示例为主，未提供设备抽象、协议抽象、实时数据采集、历史数据库、报表中心、工程配置
- 无设备/协议/点位/命令的领域模型
- 无实时数据引擎（轮询/订阅调度器）
- 无历史数据库和趋势查询

### P1：架构细节需加固

- 菜单权限同时维护在 `MenuPermissionConfig` 静态类和数据库中，存在漂移风险
- `UserEditDialogViewModel` 通过 `Action<bool>` 回调而非 DI，不符合项目约定
- 集成测试项目为空，无法验证端到端流程
- 无 CI/CD 流水线

### P2：工程品质

- Nullable 警告较多
- 部分 ViewModel 未实现 `IDisposable`（事件订阅可能泄漏）
- 无代码覆盖率门禁（目标 >= 40% 已配置但未强制）

## 4. 竞品对比

典型成熟上位机/SCADA 平台通常开箱提供：

| 能力 | 本项目现状 |
|------|-----------|
| Shell/导航 | 已具备 |
| 用户/角色/权限 | 已具备 |
| 审计追踪 | 已具备（持久化） |
| 主题/基础UI | 已具备 |
| 告警系统 | 已具备（配置+生命周期+事件） |
| 通知系统 | 已具备 |
| 数据导出 | 已具备（CSV/Excel/PDF） |
| 缓存服务 | 已具备 |
| 设备抽象 | 未具备 |
| 协议驱动 | 未具备 |
| 实时点位引擎 | 未具备 |
| 历史数据库/趋势 | 未具备 |
| 报表中心 | 部分具备（IReportGenerator 扩展点） |
| 工程配置中心 | 未具备 |
| 插件/运行时打包 | 未具备 |

## 5. 推荐平台演进方向

目标：`宿主 Shell + 通用平台服务 + 工业运行时内核 + 项目模块`

推荐分层：

1. **宿主 Shell** — WPF Shell、导航、主题、通知、对话框、状态栏、布局
2. **平台服务** — 账户、权限、配置、日志、审计、设置、文件存储、更新服务
3. **工业运行时内核** — 设备、协议、点位、轮询调度、命令回写、告警引擎、历史数据库
4. **项目模块** — 成型、搅拌、车间、质量、报表、配方、维护
5. **项目应用** — 客户特定启动组合与品牌定制

## 6. 演进路线图

### 阶段 A：修正平台正确性 ✅ 大部分已完成

- [x] 数据库初始化在首次导航前同步等待
- [x] 移除仓库默认配置中的明文凭据
- [x] 审计日志使用 `IUserSession`/`IUserInfo` 替代硬编码用户
- [x] 添加持久化本地设置服务（`IAppSettingsService`）
- [x] 审计日志持久化到数据库（`IAuditLogService`）
- [x] 密码哈希使用 PBKDF2 + 随机盐 + 防时序攻击
- [x] SM4 加解密支持双参数签名（key 从配置读取）
- [x] 会话超时自动锁屏 + 数据库断连自动锁屏
- [ ] 统一权限数据源，消除菜单权限双写

### 阶段 B：构建可复用平台服务 ✅ 大部分已完成

- [x] 创建 `Vk.Dbp.Services` 应用服务层
- [x] 添加 `IAppSettingsService`、`ISystemConfigService`
- [x] 添加对话框服务（`IUiDialogService`）、通知服务（`IGlobalNotificationPublisher`）
- [x] 添加模块元数据（`IModuleMetadata`、`IModuleLifecycle`）
- [x] 添加插件/扩展契约（`IMenuItemProvider`、`IDashboardWidgetProvider`、`IReportGenerator`）
- [x] 添加缓存服务（`ICacheService`）、导出服务（`IExportService`）
- [x] 添加导航服务（`INavigationService`）、ViewModel 工厂（`IViewModelFactory`）
- [ ] 添加更新服务（`IUpdateService`）
- [ ] 添加文件存储服务

### 阶段 C：添加上位机核心能力

- [ ] 设计设备抽象：`Device`、`Point`、`Tag`、`Command`、`Protocol`
- [ ] 添加轮询/订阅调度器
- [ ] 添加协议适配接口（Modbus TCP/RTU、OPC UA、MQTT、串口）
- [ ] 添加质量码/时间戳/来源追踪
- [ ] 添加告警规则引擎和告警生命周期（已有基础框架，需扩展规则引擎）
- [ ] 添加历史数据库 Schema 和趋势查询 API
- [ ] 添加报表/导出服务（已有 `IReportGenerator` 扩展点，需实现具体报表）
- [ ] 添加配方/参数下载上传服务
- [ ] 添加设备状态面板和通信诊断视图

### 阶段 D：改善二次开发体验

- [x] 模块开发指南文档
- [x] 标准模块目录结构约定
- [ ] 添加"新模块"、"新视图"、"新设备驱动"、"新应用"的项目模板
- [ ] 用真实数据流替换占位仪表盘
- [ ] 添加账户、权限、持久化的集成测试
- [ ] 添加 CI 流水线（restore/build/test）
- [ ] 添加 dev/test/prod 环境的发布配置示例
- [ ] 添加架构决策记录和扩展文档

### 阶段 E：工业化与交付

- [ ] 添加离线优先模式（SQLite/本地缓存）
- [ ] 添加远程 API 同步策略
- [ ] 添加异常收集、日志轮转、健康诊断
- [ ] 添加升级/回滚机制
- [ ] 添加操作员审计导出和合规报表
- [ ] 添加项目备份/导入/导出机制
- [ ] 添加安装包/打包策略（MSIX 或免安装部署）

## 7. 优先关注文件

| 文件 | 关注原因 |
|------|----------|
| `src/Vk.Dbp.WpfWindow/PrismBootstrapper.cs` | 启动流程、DI注册、数据库初始化 |
| `src/Vk.Dbp.WpfWindow/Constants/NavigationConstants.cs` | 导航常量定义 |
| `src/Vk.Dbp.Contracts/Services/INavigationService.cs` | 导航契约 |
| `src/Vk.Dbp.Services/Session/UserSession.cs` | 会话状态核心 |
| `src/Vk.Dbp.Infrastructure/DatabaseInitializer.cs` | 数据库初始化和种子数据 |
| `prismModules/Vk.Dbp.AccountModule/DbpAccountModule.cs` | 模块注册示例 |
| `prismModules/Vk.Dbp.AccountModule/Services/UserService.cs` | 服务实现示例 |
| `prismModules/Vk.Dbp.AccountModule/ViewModels/LoginViewModel.cs` | ViewModel 实现示例 |

## 8. 建议的实施顺序

1. ~~修正启动/安全/审计正确性~~ ✅ 已完成
2. ~~构建持久化设置 + 持久化审计 + 统一权限源~~ ✅ 大部分完成
3. 建立设备/点位/协议抽象
4. 添加告警引擎/历史数据库/趋势/报表
5. 提供一个真实的工业示例模块
6. 添加测试、CI、打包和部署文档

如果仓库继续朝此方向演进，可以成为一个优秀的上位机基础项目。否则，它将保持为一个带有 WPF Shell 的管理系统演示。
