---
name: dbp-quality-review
description: 代码评审/质量检查/风险排查/平台就绪度评估的优先级清单与报告格式。当任务提到"review/评审/检查代码/风险/加固/就绪度/CodeQL 告警处理"时使用。
---

# 质量评审

本项目的定位是向工业上位机平台演进（见 `.claude/rules/roadmap.md`），评审重点不是风格洁癖，而是**正确性、配置安全、模块边界、可维护性**。

## 动手前必读

- `CLAUDE.md`、`.claude/rules/code-reality.md`（先弄清什么是"正常现实"再下结论，避免误报）
- `docs/PROJECT_REVIEW_AND_TODO.md`（已知差距与债务，用于区分存量问题与新增回归）
- 变更文件本身 + 相关测试

## 评审优先级（按严重度输出）

1. **安全与机密**
   - 提交了真实凭据/密钥/连接串；SM4 密钥硬编码。
   - 默认密码进入运行时路径；敏感值写入日志。
   - 会话令牌落盘（应仅内存，`Logout()` 清空）。

2. **启动与配置正确性**
   - `IAppStartupService.InitializeDatabaseAsync()` 未在首个 DB 依赖 UI/服务前完成（启动顺序：Splash → DB 初始化 → Shell → 首次导航）。
   - 配置缺失时报错不明确；不安全的连接串回退。
   - SqlSugar 连接错误未触发自动锁屏（AOP `OnError` → `ILockScreenService.Lock`，**该行为绝不可移除**）。

3. **身份、权限与审计**
   - 硬编码操作者身份。
   - `IUserInfo`（轻量审计）与 `IUserSession`(完整会话) 用反。
   - 新增权限来源造成第三份漂移（已有 MenuPermissionConfig 静态 + DB 双写债）。
   - 受保护操作缺拒绝路径测试；`IAuditLogService` 未记录失败原因。

4. **模块与分层边界**
   - 业务模块互相依赖；shell 长出客户业务逻辑。
   - ViewModel 绕过服务边界接触 `ISqlSugarClient`/仓储类型。
   - 跨模块通信用了直接引用而非 `IEventAggregator`（`PubSubEvent<T>` 定义在 `Vk.Dbp.Contracts`）。
   - ViewModel 直接用 `IRegionManager.RequestNavigate` 而非 `INavigationService.NavigateTo`（存量违例：`AlarmRecordViewModel`，改造相邻代码时顺手迁移，但不强制大改）。

5. **WPF 可靠性与性能**
   - UI 线程阻塞（同步等待异步、构造器重数据加载）。
   - 大列表缺虚拟化；事件回调更新 UI 未走 `Application.Current.Dispatcher.Invoke()`。
   - 订阅事件的 VM 缺 `IDisposable`/取消订阅（内存泄漏）。

6. **测试与验证**
   - 变更行为无聚焦测试；集成敏感改动未说明数据库要求。
   - 验证命令未运行且未声明原因。

7. **CodeQL 干净度**（详见 `.claude/rules/quality-patterns.md`，逐条对照）
   - lambda/查询里 `.HasValue` 后 `.Value`（应 `is { } x` 模式）。
   - 局部变量遮蔽字段/参数。
   - 仅初始化赋值的字段/集合未 `readonly`/getter-only。
   - 一对一投影手写 `foreach` 造列表（应 `Select`）。
   - 可合并的嵌套 `if`。
   - 已知误报：`SetProperty(ref _field, value)` 后备字段被要求 readonly——保持可变，选最小改写。

## 评审方法

- 从入口追到服务再到持久化/事件出口，完整走一遍行为路径。
- 结论前对照最相近的既有实现——仓库约定优先于个人偏好。
- 区分"本次变更引入的回归"与"存量风险"（存量问题单独列出，不算阻塞）。
- 不确定的问题写明需要什么证据才能确认。
- CodeQL 告警要落到具体行，给出满足 CodeQL 且符合仓库可读性的最小改写。

## 报告格式

代码评审：

```text
问题（按严重度）
- [严重度] 文件:行 — 问题与影响

存疑
- 需要什么证据/假设

小结
- 评审范围与整体判断

验证
- 已运行/未运行的命令
```

项目级分析：`现状` / `主要优势` / `主要风险` / `建议下一步`。

## 验证命令

```powershell
dotnet test test\Vk.Dbp.Tests.Unit\Vk.Dbp.Tests.Unit.csproj
dotnet build desktop.boilerplate.slnx
git diff --check
```

环境缺 .NET SDK / SQL Server / Windows 桌面工作负载时，明确声明并做纯静态评审。
