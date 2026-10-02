---
paths:
  - "test/**"
---

# 测试约定（Vk.Dbp.Tests.*）

技术栈：xUnit 2.6 + Moq 4.20 + FluentAssertions 6.12 + coverlet.msbuild（**行覆盖门禁真实生效**：排除 UI 壳工程 WpfWindow/WorkshopModule 与测试基建后，业务代码总行覆盖 ≥32%，低于即 `dotnet test` 失败；40% 为目标值）。测试目录结构镜像被测项目（`Services/`、`ViewModels/`、`Models/`、`Infrastructure/`、`Configuration/`）。

## 命名

- 测试类：`{被测类}Tests`（如 `UserServiceTests`）。
- 方法：`Method_Scenario_ExpectedResult`（新测试统一用此风格，不再引入下划线混排旧式）。
- FluentAssertions 断言**必须带中文 reason**：`.Should().BeTrue("admin用户应该拥有所有权限");`

## 数据库依赖服务的测试模式（"单元测试"实为小型集成测试）

```csharp
public class XxxServiceTests : IClassFixture<TestDatabaseFixture>
{
    private readonly TestDatabaseFixture _fixture;
    private readonly XxxService _service;

    public XxxServiceTests(TestDatabaseFixture fixture)
    {
        _fixture = fixture;
        ResetDatabase();          // 删除相关表全部行，保证每测试隔离
        SeedXxx();                // 私有内联种子方法
        _service = CreateService();
    }
}
```

- 共享夹具：`test/Vk.Dbp.Tests.Common/TestDatabaseFixture.cs`（临时文件 SQLite + CodeFirst 13 张表 + Dispose 带重试清理）。
- 种子数据用**私有 `SeedXxx` 内联方法**；`TestDataFactory` 是旧倾向，新测试不要再依赖它。
- 集成测试项目 `Vk.Dbp.Tests.Integration` 目前为空（需 SQL Server，不要假设任何环境可跑）。

## Moq 样板（几乎每个 DB 服务测试都需要）

```csharp
private void SetupAuditLogService()
{
    _auditLogService
        .Setup(x => x.LogOperationAsync(
            It.IsAny<int>(), It.IsAny<string>(), It.IsAny<AuditActionType>(),
            It.IsAny<string>(), It.IsAny<string>(),
            It.IsAny<string?>(), It.IsAny<int?>(),
            It.IsAny<string?>(), It.IsAny<string?>(), It.IsAny<string?>()))
        .ReturnsAsync(true);

    _auditLogService
        .Setup(x => x.LogFailureAsync(
            It.IsAny<int>(), It.IsAny<string>(), It.IsAny<AuditActionType>(),
            It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(),
            It.IsAny<string?>(), It.IsAny<int?>(), It.IsAny<string?>()))
        .ReturnsAsync(true);
}
```

与 `src/Vk.Dbp.Services/Audit/IAuditLogService.cs` 当前签名一一对应：`LogOperationAsync(userId, username, actionType, module, description, entityType?, entityId?, oldData?, newData?, clientIp?)`、`LogFailureAsync(userId, username, actionType, module, description, failureReason, entityType?, entityId?, clientIp?)`。审计增强时参数可能变化，写 Setup 前先读接口。

- 密码流程：`Mock<IPasswordHasher>` + `Setup(x => x.VerifyPassword(...))`。
- 会话：`Mock<IUserSession>` + `SetupGet(x => x.IsLoggedIn).Returns(true)`；或用真实 `UserSession` 实例（NotificationServiceTests 风格）。
- 失败路径断言：`_auditLogService.Verify(x => x.LogFailureAsync(...), Times.Once);` 及 `_passwordHasher.Verify(..., Times.Never);`

## UI 耦合服务与临时文件

- WPF 对话框旁路：私有 `TestExportService : ExportService` 子类，`override` 虚方法 `ShowSaveFileDialog` 返回预设路径。
- 临时文件命名 `dbp-{用途}-{Guid}.ext`，`try/finally` 中删除。
- 安全用例参照 `ExportServiceTests`：CSV 公式注入转义、扩展名白名单拒绝 `.ps1` 等。

## 现代范例

写新测试前优先参照 `test/Vk.Dbp.Tests.Unit/Services/UserServiceTests.cs`（最新约定、无冗余 AAA 注释、Seed 内联、失败路径覆盖完整）。

## 验证命令

```powershell
dotnet test test\Vk.Dbp.Tests.Unit\Vk.Dbp.Tests.Unit.csproj
dotnet build desktop.boilerplate.slnx
```
