---
name: dbp-login-session
description: 登录、会话、锁屏解锁、权限校验体系的工作原理与改动指南。当任务涉及"登录/登出/会话/锁屏/IUserSession/权限验证/记住用户名/会话超时"时使用。
---

# 登录与会话体系

## 核心分层：IUserInfo vs IUserSession

两者都在 `src/Vk.Dbp.Services/Session/`：

| 接口 | 用途 | 成员 |
|---|---|---|
| `IUserInfo` | **只做审计等轻量身份读取** | `UserId`、`Username`、`IsLoggedIn` |
| `IUserSession : IUserInfo` | **完整会话管理** | `Login()`、`Logout()`、`Lock()`、`Unlock()`、`SetPermissions()`、`HasPermission()`、`Token`、全部用户档案字段 |

实现 `UserSession : BindableBase, IUserSession`（**可绑定单例**），注册于 `PrismBootstrapper.RegisterTypes`：`RegisterSingleton<IUserSession, UserSession>()`。

选择规则：只需要身份上下文 → 注入 `IUserInfo`（或配合 `GetAuditUserId()/GetAuditUsername()` 扩展，见 `/dbp-add-audit-logging`）；要改会话状态 → 注入 `IUserSession`。

## 登录流程（LoginViewModel）

```text
1. LoginView 输入凭据（LoginViewModel 校验非空/长度）
2. IUserService 查用户 → 账号启用检查 → IPasswordHasher.VerifyPassword
3. 每一步失败都单独记 IAuditLogService.LogFailureAsync（用户不存在/已禁用/密码错误分别记录）
4. 成功 → _userSession.Login(...) 填充档案 + SetPermissions(...)
5. Token 由 RandomNumberGenerator 生成，仅内存持有
6. 持久化"记住用户名"偏好
7. INavigationService.NavigateTo(ViewNames.Dashboard)
```

修改授权行为时**必须同时测试允许与拒绝两条路径**。

## 锁屏 / 登出

```text
Lock:   _userSession.Lock(reason) → IsLocked=true，显示锁屏窗口（不清用户信息与权限）
Unlock: 校验密码后 _userSession.Unlock() → 恢复原视图
Logout: _userSession.Logout() → 清空全部字段、IsLoggedIn=false、权限重置、Token 清空 → 导航回 LoginView
```

两个自动锁屏来源（都不可移除）：

1. **会话超时**：`ISessionTimeoutService`（登录后在 `PrismBootstrapper.InitializeShell` 启动监控，1–480 分钟可配置）。
2. **数据库断连**：SqlSugar AOP `OnError` 识别连接类错误 → `ILockScreenService.Lock("数据库连接失败…")`。

## 权限体系

- `_userSession.HasPermission(permissionCode)` 检查登录时加载的权限集。
- **admin 用户对任意权限码返回 true（硬编码）——仅限开发便利，生产授权逻辑绝不能依赖它**。
- 菜单可见性走 `IMenuPermissionFilter.IsMenuVisible(viewName)`；菜单权限在 `MenuPermissionConfig` 静态类与数据库**双写**（已知债：改动时两处同步，勿扩大漂移）。
- 权限集变更后发布 `PermissionChangedEvent`（`Vk.Dbp.Contracts/Events/`）。

## UI 联动模式（HeaderViewModel 范式）

`UserSession` 是 `BindableBase`，UI 通过两种方式跟随会话变化：

```csharp
// HeaderViewModel：订阅 INPC（构造器中条件订阅，Dispose 中退订）
(_userSession as INotifyPropertyChanged).PropertyChanged += OnSessionPropertyChanged;

private void OnSessionPropertyChanged(object? sender, PropertyChangedEventArgs e)
{
    Application.Current.Dispatcher.Invoke(() =>
    {
        switch (e.PropertyName)
        {
            case nameof(IUserSession.IsLoggedIn): /* 更新头部显示 */ break;
            case nameof(IUserSession.IsLocked):   /* 显示/隐藏锁屏 */ break;
        }
    });
}
```

要点：回调内 UI 更新必须 `Dispatcher.Invoke`；订阅方实现 `IDisposable` + `_isDisposed` 守卫。

## 关键文件

| 组件 | 位置 |
|---|---|
| `IUserInfo` / `IUserSession` / `UserSession` | `src/Vk.Dbp.Services/Session/` |
| `LoginView` / `LoginViewModel` | `prismModules/Vk.Dbp.AccountModule/Views|ViewModels/` |
| `HeaderView` / `HeaderViewModel` | `src/Vk.Dbp.WpfWindow/Layout/HeaderView.xaml`、`src/Vk.Dbp.WpfWindow/ViewModels/HeaderViewModel.cs` |
| 密码哈希（PBKDF2-SHA256，10万次迭代，防时序攻击） | `src/Vk.Dbp.Utils/Security/PasswordHasher.cs` |
| 会话/权限事件 | `src/Vk.Dbp.Contracts/Events/`（`UserLoggedInEvent`、`PermissionChangedEvent`） |
| 启动与会话监控接线 | `src/Vk.Dbp.WpfWindow/PrismBootstrapper.cs`（`InitializeShell`） |
| 锁屏服务 | `src/Vk.Dbp.Services/`（`ILockScreenService`，经 `LockScreenViewModel`/`LockScreenWindow` 展示） |

## 开发默认账号

`admin / 123456`——**仅开发环境**。非开发环境用 `scripts/start-wpf-local.ps1 -FirstRun -AdminPassword` 设置（首跑必须提供 `DBP_INITIAL_ADMIN_PASSWORD`，无回退默认值）。

## 快速验证清单

- [ ] 冷启动落在登录页
- [ ] 登录成功后头部显示用户信息
- [ ] 登录失败各原因分别产生审计记录
- [ ] 锁屏不丢会话；解锁后恢复
- [ ] 登出清空会话并回登录页
- [ ] 权限控制菜单可见性生效
- [ ] 会话超时触发锁屏
