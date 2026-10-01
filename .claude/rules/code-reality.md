# 代码现实规则（命名空间与工程怪癖）

本仓库项目文件名与代码命名空间**不一致**，且存在若干历史怪癖。以下事实经代码核验，优先级高于直觉推断；新代码遵循所在项目/邻近文件的既有风格。

## 双命名空间：`Vk.Dbp.*` 与 `Dabp.*` 并存

项目名是 `Vk.Dbp.*`，但多数项目内部的根命名空间是 `Dabp.*`：

| 项目 | 实际根命名空间 |
|---|---|
| `Vk.Dbp.WpfWindow` | 混合：`Dabp.WpfWindow`（App、Bootstrapper、Services）+ `Vk.Dbp.WpfWindow.ViewModels/Views/Converters` |
| `Vk.Dbp.Infrastructure` | `Dabp.Infrastructure`、`Dabp.Infrastructure.Entities`、`Dabp.Infrastructure.Repositories` |
| `Vk.Dbp.Utils` | `Dabp.Utils.Security`、`Dabp.Utils.Algorithm`、`Dabp.Utils.Exceptions` |
| `Vk.Dbp.Services` | 混合：`Vk.Dbp.Services.Session/Audit/Alarm` + `Dabp.Services.Settings/Caching/Export` |
| `Vk.Dbp.Contracts` | `Vk.Dbp.Contracts.*`（csproj 有显式 `<RootNamespace>`） |
| `Vk.Dbp.Tools` | `Dabp.Tools` |
| `prismModules/*` | `Vk.Dbp.AccountModule`、`Vk.Dbp.WorkshopModule` |
| `dbpApps/*` | `Dabp.Material.Forming`、`Dabp.Material.Mixing` |

规则：

- 写新文件前先看**同目录邻近文件**的 namespace，保持一致；不要"统一"两种前缀。
- 同一文件里 `using Dabp.Services.Settings` 和 `using Vk.Dbp.Services.Audit` 混用是正常的。
- 实体与模块 Model 同名时用别名区分：`using UserEntity = Dabp.Infrastructure.Entities.User; using UserModel = Vk.Dbp.AccountModule.Models.User;`

## Prism 隐式全局 using —— 不要"修复"

启用了 `ImplicitUsings` 的 WPF 项目中，Prism.Wpf 9.0.537 会自动生成 `global using Prism.Commands / Prism.Mvvm / Prism.Navigation.Regions / ...`。因此文件里**缺少 `using Prism.Mvvm;` 等是完全合法的**，不要顺手补上。

## 工程文件怪癖（既成事实，勿顺手"修正"）

- Services 项目文件名是 `Vk.Dbp.Service.csproj`（单数）。
- 静态资源目录叫 `Asserts/`（非 Assets）；`DbpDomainMoudle.cs` 拼写如此。
- 无 `Directory.Build.props` / `Directory.Packages.props`；包版本逐 csproj 固定。
- 解决方案是 `desktop.boilerplate.slnx`（新 XML 格式）。
- WPF 项目 target `net10.0-windows`；`Vk.Dbp.Contracts` 是 `net10.0`（UI 无关）。
- 仅 `Vk.Dbp.WpfWindow.csproj` 引入根目录 `common.props`（Fody/ConfigureAwait 等）。

## 文件编码

仓库存在 GBK 乱码历史文件（部分 Service/ViewModel 中文串已损坏）。新写或修改中文注释/字符串时**必须以 UTF-8 保存**，不要复制邻近文件里的乱码文本。

## 服务注册位置速查（经核验）

| 服务 | 接口位置 | 实现 | 注册点 |
|---|---|---|---|
| `IUserSession` | `src/Vk.Dbp.Services/Session/` | 同目录 `UserSession` | `PrismBootstrapper.RegisterTypes` |
| `IAuditLogService` | `src/Vk.Dbp.Services/Audit/` | **AccountModule** `Services/DbAuditLogService` | `DbpAccountModule.RegisterTypes` |
| `IAlarmService`/`IAlarmConfigService` | `src/Vk.Dbp.Services/Alarm/` | 同目录实现 | `DbpAccountModule.RegisterTypes` |
| `IRepository<>`（开放泛型） | `src/Vk.Dbp.Infrastructure/Repositories/` | `SqlSugarRepository<>` | `PrismBootstrapper.RegisterTypes` |
| 账户域服务（User/Role/Permission/Org） | 模块内 `Services/`（接口+实现同目录） | 同目录 | `DbpAccountModule.RegisterTypes` |
| `INotificationService`/`ISystemConfigService`/`IUserCredentialService` | `src/Vk.Dbp.Contracts/Services/`（跨模块契约，2026-10 从模块内提升） | **AccountModule** `Services/`（`Notification` DTO 在 `Vk.Dbp.Contracts.Models`） | `DbpAccountModule.RegisterTypes` |
| `IAppStartupService` | `src/Vk.Dbp.WpfWindow/Services/`（接口在 shell 层） | 同目录 `AppStartupService`；建表种子在 `src/Vk.Dbp.Infrastructure/DatabaseInitializer.cs` | `PrismBootstrapper.RegisterTypes` |

## 既有现实约定（与理想约定的差异）

- **仓储层使用率低**：`IRepository<T>` 存在且已注册，但真实服务普遍直接注入 `ISqlSugarClient` 用 `Queryable/Insertable/Updateable`（含联表）。新服务跟随现实，直接用 `ISqlSugarClient`；ViewModel 仍禁止接触 SqlSugar 类型。
- **对话框不用 Prism IDialogService**：既定模式是 View 内 `Popup IsOpen="{Binding IsDialogOpen}"` + 对话框 ViewModel（构造器带回调 + `Initialize(...)`）。
- `dbpframework/Vk.Dbp.Core` 的 `ServiceCollectionExtensions` 与 `IDbpModule` 是**未接线的空壳**，所有注册走 Prism `IContainerRegistry`。
- `dbpApps/*` 目前是模板占位入口（默认 `StartupUri`，未接 shell 与模块目录）。
