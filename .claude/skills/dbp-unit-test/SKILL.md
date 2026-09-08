---
name: dbp-unit-test
description: 编写本项目单元测试（xUnit + Moq + FluentAssertions）的完整规范与骨架。当任务提到"写测试/加单测/补充测试/测试失败路径"，或为新服务、ViewModel 添加 Vk.Dbp.Tests.Unit 测试时使用。
---

# 编写单元测试

## 动手前

1. 读 `.claude/rules/testing.md`（约定速览，本 skill 是其展开）。
2. 读最近范本：`test/Vk.Dbp.Tests.Unit/Services/UserServiceTests.cs`（最新约定）。
3. 确认测试文件放 `test/Vk.Dbp.Tests.Unit/{镜像目录}/`（`Services/`、`ViewModels/`、`Models/`、`Infrastructure/`、`Configuration/`）。

## 新测试文件骨架（DB 依赖服务）

```csharp
using System;
using System.Threading.Tasks;
using Dabp.Utils.Exceptions;
using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;
using Moq;
using Vk.Dbp.Services.Audit;
using Vk.Dbp.Services.Session;
using Vk.Dbp.Tests.Common;
using Xunit;

namespace Vk.Dbp.Tests.Unit.Services;

public class XxxServiceTests : IClassFixture<TestDatabaseFixture>
{
    private readonly TestDatabaseFixture _fixture;
    private readonly Mock<IAuditLogService> _auditLogService = new();
    private readonly Mock<IUserSession> _userSession = new();
    private XxxService _service = null!;

    public XxxServiceTests(TestDatabaseFixture fixture)
    {
        _fixture = fixture;
        SetupAuditLogService();
        SetupUserSession();
        ResetDatabase();
        _service = CreateService();
    }

    private XxxService CreateService() => new(
        _fixture.Db,
        _auditLogService.Object,
        _userSession.Object);

    private void SetupAuditLogService() { /* 全 It.IsAny + ReturnsAsync(true)，确切签名见 IAuditLogService.cs */ }
    private void SetupUserSession()
    {
        _userSession.SetupGet(x => x.IsLoggedIn).Returns(true);
        _userSession.SetupGet(x => x.UserId).Returns(1);
        _userSession.SetupGet(x => x.Username).Returns("admin");
    }
    private void ResetDatabase() { /* _fixture.Db.Deleteable<T>().ExecuteCommand() 逐表清行 */ }
    private async Task SeedXxxAsync() { /* 内联最小种子数据，不依赖 TestDataFactory */ }
}
```

夹具说明：`TestDatabaseFixture`（`test/Vk.Dbp.Tests.Common/TestDatabaseFixture.cs`）持有临时文件 SQLite 的 `SqlSugarScope`，已 CodeFirst 全部 13 张实体表，Dispose 时清理临时库。字段/属性名以该文件现状为准。

## 测试方法风格

```csharp
[Fact]
public async Task CreateXxx_ValidInput_ReturnsTrueAndPersists()
{
    // Arrange（新风格省略注释亦可，与 UserServiceTests 一致）
    var model = new Xxx { Name = "测试数据" };

    // Act
    bool result = await _service.CreateXxxAsync(model);

    // Assert —— 中文 reason 必带
    result.Should().BeTrue("创建有效数据应返回成功");
    (await _service.GetXxxByIdAsync(model.Id)).Should().NotBeNull("创建后应可查询到");
}

[Fact]
public async Task CreateXxx_DbFailure_ReturnsFalseAndLogsFailure()
{
    // 失败路径用 Verify 断言审计与副作用
    ...
    _auditLogService.Verify(
        x => x.LogFailureAsync(It.IsAny<int>(), It.IsAny<string>(),
            AuditActionType.Create, It.IsAny<string>(), It.IsAny<string>(),
            It.IsAny<string>(), It.IsAny<string>(), It.IsAny<int?>(), It.IsAny<string?>()),
        Times.Once);
}
```

- 方法名 `Method_Scenario_ExpectedResult`；一个测试聚焦一个行为。
- 异常断言：`.Should().ThrowAsync<ArgumentException>().WithMessage("*csv*")`（支持通配）。
- 时间断言：`.Should().BeCloseTo(DateTime.Now, TimeSpan.FromSeconds(1))`。
- 集合断言：`.Should().BeEquivalentTo(new[] { ... })`。

## ViewModel 测试

现有范例少（仅 `AdminShellViewModelTests`）。模式：直接 new VM（mock 全部依赖）→ `command.Execute(...)` → 断言状态。不要在单测里触碰 WPF Dispatcher 依赖的路径；确实需要时用 `Application.Current.Dispatcher.Invoke` 的代码路径应拆出可测的纯逻辑。

## UI 耦合服务（导出类）

- 私有 `TestExportService : ExportService` 子类 `override` 虚方法 `ShowSaveFileDialog` 返回预设路径（范例 `ExportServiceTests`）。
- 临时文件：`Path.Combine(Path.GetTempPath(), $"dbp-export-{Guid.NewGuid()}.csv")`，`try/finally` 删除。
- 安全用例照抄思路：CSV 公式注入转义（`=-cmd()` 开头被转义）、扩展名白名单拒绝（`.ps1` 返回 false）。

## 完成自检

- [ ] 类名 `{被测类}Tests`，位于镜像目录
- [ ] 每测试前数据隔离（ResetDatabase）
- [ ] 成功与失败路径都有覆盖，失败断言用 `Verify(..., Times.Once/Once)`
- [ ] 所有 `.Should()` 带中文 reason
- [ ] 没有引入对 `TestDataFactory` 的新依赖
- [ ] `dotnet test test\Vk.Dbp.Tests.Unit\Vk.Dbp.Tests.Unit.csproj` 全绿
