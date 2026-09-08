---
name: dbp-database
description: 数据库层（SqlSugar）配置、实体、仓储、事务与初始化的真实约定。当任务涉及"加实体/加表/建库/初始化/连接串/事务/仓储/repository/SqlSugar 配置"时使用。注意：本 skill 已按当前代码修正，旧 .trae 版本中的 IGenericRepository 等描述与实际不符。
---

# 数据库层（SqlSugar）

## 组合根（PrismBootstrapper.ConfigureSqlSugarDb）

`src/Vk.Dbp.WpfWindow/PrismBootstrapper.cs`：

```csharp
var db = new SqlSugarScope(new ConnectionConfig
{
    ConnectionString = connectionString,   // 环境变量 > appsettings.local.json > appsettings.json
    DbType = DbType.SqlServer,
    IsAutoCloseConnection = true,
    ConfigureExternalServices = SqlSugarFluentService.GetConfigureExternalServices(),
}, db =>
{
    db.Aop.OnLogExecuting = (sql, _) => Log.Debug("SQL: {Sql}", sql);
    db.Aop.OnError = (exp) =>
    {
        Log.Error(...);
        if (IsConnectionError(exp))   // 子串启发式：connection/timeout/network/登录失败 等
        {
            Application.Current?.Dispatcher.Invoke(() =>
                Container.Resolve<ILockScreenService>().Lock("数据库连接失败，请检查网络后解锁重试"));
        }
    };
});
containerRegistry.RegisterSingleton<ISqlSugarClient>(db);
```

- `SqlSugarScope` 单例，线程安全，自动关连接。
- **断连自动锁屏是安全底线，绝不可移除或弱化**。
- 连接串支持 SM4 密文（`src/Vk.Dbp.Utils/Algorithm/SM4.cs`，CBC/PKCS7，密钥来自本地配置/环境变量，永不入库入仓）。

## 初始化流程

```text
PrismBootstrapper.InitializeShell
  → Splash
  → IAppStartupService.InitializeDatabaseAsync()   （接口在 src/Vk.Dbp.WpfWindow/Services/）
      → DatabaseInitializer（src/Vk.Dbp.Infrastructure/DatabaseInitializer.cs）
          → CodeFirst.InitTables(13 张实体)
          → 种子数据（admin、默认角色、权限等）
  → Shell 显示 → 首次导航
```

数据库初始化**必须**在任何 DB 依赖 UI/服务之前完成——改启动顺序时保住这一点。

## 实体（src/Vk.Dbp.Infrastructure/Entities/，命名空间 Dabp.Infrastructure.Entities）

```csharp
/// <summary>用户实体</summary>
public class User
{
    [Key]
    [SugarColumn(IsPrimaryKey = true, IsIdentity = true)]
    public int Id { get; set; }

    [SugarColumn(IsNullable = true, ColumnDataType = "nvarchar")]
    [StringLength(50)]
    public string? UserName { get; set; }

    // ABP 式审计列：CreationTime/CreatorId/LastModificationTime/LastModifierId
    // 软删列：IsDeleted/DeletionTime/DeleterId
}
```

约定：

- SqlSugar 特性 + DataAnnotations 组合使用；属性带中文 XML 注释。
- **软删除是手动的**——每个查询都要 `.Where(x => !x.IsDeleted)`，删除实现为置位 IsDeleted。
- 实体（Infrastructure）与模块 Model 分离，服务里私有 `MapToEntity/MapToModel` + using 别名转换（范例 `prismModules/Vk.Dbp.AccountModule/Services/UserService.cs`）。

## 仓储与现实用法

`src/Vk.Dbp.Infrastructure/Repositories/` 提供 `IRepository<T>` / `SqlSugarRepository<T>`（`where T : class, new()`），以**开放泛型**注册于 PrismBootstrapper：`containerRegistry.Register(typeof(IRepository<>), typeof(SqlSugarRepository<>))`；含异步 CRUD 与 `GetPageListAsync`（`RefAsync<int>` 返回 `(List<T> list, int total)` 元组）。完整 API 以 `IRepository.cs` 现状为准。

**现实约定：服务普遍绕过仓储直接注入 `ISqlSugarClient`**：

```csharp
// 单表
List<User> users = await _db.Queryable<User>()
    .Where(u => !u.IsDeleted && u.UserName == name)
    .ToListAsync();

// 联表（角色-用户关联）
var roles = await _db.Queryable<Role, UserRole>((r, ur) => new JoinQueryInfos(
        JoinType.Inner, r.Id == ur.RoleId))
    .Where((r, ur) => ur.UserId == userId)
    .Select((r, ur) => r)
    .ToListAsync();

// 写入
int id = await _db.Insertable(entity).ExecuteReturnIdentityAsync();
await _db.Updateable(entity).ExecuteCommandAsync();
```

新服务跟随现实（直接 `ISqlSugarClient`）；ViewModel 仍然禁止接触 SqlSugar 类型——必须隔服务边界。

## 事务（范例 UserService.AssignRolesToUserAsync）

```csharp
_db.Ado.BeginTran();
bool committed = false;
try
{
    // 多步写操作
    _db.Ado.CommitTran();
    committed = true;
}
finally
{
    if (!committed)
    {
        _db.Ado.RollbackTran();
    }
}
```

## 新增实体接入步骤

1. 在 `src/Vk.Dbp.Infrastructure/Entities/` 建实体（含审计列与软删列，中文 XML 注释）。
2. 在 `DatabaseInitializer` 的 `InitTables` 清单登记（**漏登记 = 运行时表不存在**）。
3. 需要跨模块访问时：契约放 `Vk.Dbp.Contracts`（服务接口），通用实现放 `Vk.Dbp.Services`，模块专属实现放模块 `Services/`。
4. 注册（`PrismBootstrapper.RegisterTypes` 或模块 `RegisterTypes`）。
5. 种子数据（如需）加在 DatabaseInitializer 建表之后。
6. 服务测试用 `TestDatabaseFixture`（见 `/dbp-unit-test`）。

## 常见问题

| 症状 | 处理 |
|---|---|
| 运行时"表不存在" | 实体没进 `DatabaseInitializer.InitTables` 清单 |
| 连接失败导致崩溃 | 不应发生——AOP 锁屏兜底；若复现检查 `ConfigureSqlSugarDb.OnError` 是否被改动 |
| 数据"删了还在" | 软删实现，查询补 `!IsDeleted` 过滤 |
| 本地起不来 | `scripts/start-wpf-local.ps1` 自动起 LocalDB 并注入连接串；首跑需 `-FirstRun -AdminPassword` |

## 验证

```powershell
dotnet test test\Vk.Dbp.Tests.Unit\Vk.Dbp.Tests.Unit.csproj   # SQLite 临时库覆盖实体/服务
dotnet build desktop.boilerplate.slnx
.\scripts\start-wpf-local.ps1                                  # 真库人工验证
```
