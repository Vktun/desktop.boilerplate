---
name: dbp-add-audit-logging
description: 给服务添加审计日志（操作审计）的标准配方。当任务提到"加审计/审计日志/记录操作日志/操作留痕"，或要给某个 Service 的增删改操作补充 IAuditLogService 记录时使用。这是本仓库 git 历史中最高频的重复改造。
---

# 给服务添加审计日志

## 适用判断

- 目标是"每个成功的增/删/改/导出等操作记录审计，失败也记录原因"。
- 只需身份上下文（不改会话状态）时注入 `IUserSession` 或 `IUserInfo` 均可，`GetAuditUserId/GetAuditUsername` 扩展两者都适用。
- 纯查询服务一般不加写审计（查看详情可用 `LogViewAsync`，按需）。

## 改造配方（对照范例写）

最佳范例：`prismModules/Vk.Dbp.AccountModule/Services/NotificationService.cs`（先读它再动手）。

### 1. 构造器注入两个依赖（带 null guard）

```csharp
private readonly IAuditLogService _auditLogService;
private readonly IUserSession _userSession;

public NotificationService(
    ISqlSugarClient db,
    IAuditLogService auditLogService,
    IUserSession userSession)
{
    _db = db ?? throw new ArgumentNullException(nameof(db));
    _auditLogService = auditLogService ?? throw new ArgumentNullException(nameof(auditLogService));
    _userSession = userSession ?? throw new ArgumentNullException(nameof(userSession));
}
```

命名空间：`using Vk.Dbp.Services.Audit;`（接口与扩展都在此）。

### 2. 成功路径：用 AuditLogExtensions 便捷封装

`src/Vk.Dbp.Services/Audit/AuditLogExtensions.cs` 提供（自动序列化 old/new 为 JSON）：

| 扩展 | 签名要点 |
|---|---|
| `LogCreateAsync` | `(userId, username, module, entityType, entityId, newData, description?)` |
| `LogUpdateAsync` | `(userId, username, module, entityType, entityId, oldData, newData, description?)` |
| `LogDeleteAsync` | `(userId, username, module, entityType, entityId, deletedData?, description?)` |
| `LogLoginAsync` / `LogLogoutAsync` / `LogChangePasswordAsync` | `(userId, username, clientIp?)`，module 固定 "Account" |
| `LogExportAsync` / `LogImportAsync` / `LogDownloadAsync` | `(userId, username, module, description, clientIp?)` |
| `LogViewAsync` | `(userId, username, module, entityType, entityId?, clientIp?)` |

身份取值规范：

```csharp
await _auditLogService.LogCreateAsync(
    _userSession.GetAuditUserId(),      // 未登录回退 0
    _userSession.GetAuditUsername(),    // 未登录回退 "system"
    "Notification",                     // module 名（通常与实体/服务域同名）
    "Notification",                     // entityType
    id,
    notification,                       // newData：直接传模型对象
    $"创建通知: {notification.Title}");
```

### 3. 失败路径：私有辅助方法统一包 LogFailureAsync

```csharp
private async Task LogNotificationFailureAsync(
    AuditActionType actionType, int? entityId, string operation, string reason)
{
    await _auditLogService.LogFailureAsync(
        _userSession.GetAuditUserId(),
        _userSession.GetAuditUsername(),
        actionType,
        "Notification",
        operation,
        reason,                          // 失败原因必须记录（ex.Message 或业务原因）
        "Notification",
        entityId);
}
```

### 4. 方法体结构（成功记审计、失败记原因）

```csharp
public async Task<bool> CreateNotificationAsync(Notification notification)
{
    try
    {
        int id = await _db.Insertable(entity).ExecuteReturnIdentityAsync();
        if (id > 0)
        {
            await _auditLogService.LogCreateAsync(
                _userSession.GetAuditUserId(), _userSession.GetAuditUsername(),
                "Notification", "Notification", id, notification,
                $"创建通知: {notification.Title}");
        }
        return id > 0;
    }
    catch (Exception ex) when (ExpectedOperationExceptionFilter.IsExpectedDataOperationException(ex))
    {
        await LogNotificationFailureAsync(AuditActionType.Create, notification.Id, "创建通知失败", ex.Message);
        return false;
    }
}
```

要点：

- catch 过滤器用 `ExpectedOperationExceptionFilter.IsExpectedDataOperationException(ex)`（`Dabp.Utils.Exceptions`），不要裸 catch。
- "目标不存在"这类业务失败也走 `LogXxxFailureAsync`（见范例 UpdateNotificationAsync 开头）。
- 审计调用不必包 try/catch——审计失败不应让业务操作回滚（范例即如此）。
- `AuditActionType` 枚举值见 `src/Vk.Dbp.Services/Audit/AuditActionType.cs`。

## 配套测试（必须更新）

参照 `test/Vk.Dbp.Tests.Unit/Services/UserServiceTests.cs` / `NotificationServiceTests.cs`：

1. 构造测试类时 mock `IAuditLogService` 并调 `SetupAuditLogService()`（全 `It.IsAny` + `ReturnsAsync(true)`；确切签名见 `.claude/rules/testing.md` 或直接读 `IAuditLogService.cs`）。
2. 成功路径：`_auditLogService.Verify(x => x.LogOperationAsync(...对应参数...), Times.Once)`。
3. 失败路径：`_auditLogService.Verify(x => x.LogFailureAsync(...), Times.Once)`。
4. mock `IUserSession` 时给 `GetAuditUserId/GetAuditUsername` 的宿主属性打桩（`IsLoggedIn=true`、`UserId`、`Username`），或用真实 `UserSession`。

## 验证

```powershell
dotnet test test\Vk.Dbp.Tests.Unit\Vk.Dbp.Tests.Unit.csproj
dotnet build desktop.boilerplate.slnx
```

完成后自检：构造器两个新依赖都有 null guard；每个写操作成功/失败两路都有审计；catch 用的是异常过滤器而非裸 catch。
