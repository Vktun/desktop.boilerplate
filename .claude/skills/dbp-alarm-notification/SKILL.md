---
name: dbp-alarm-notification
description: 告警体系（告警事件、告警服务、确认/解决生命周期、通知展示）与跨模块通知的工作指南。当任务涉及"告警/报警/alarm/通知/notification/Growl 提醒/Badge 角标"时使用。
---

# 告警与通知体系

## 事件（定义在 Contracts，跨模块解耦）

`src/Vk.Dbp.Contracts/Events/AlarmEvents.cs`（**载荷类在 `AlarmPayloads.cs`，枚举在 `AlarmEnums.cs`**——不是一事件一文件）：

```csharp
public class AlarmTriggeredEvent   : PubSubEvent<AlarmTriggeredPayload> { }   // 新告警产生
public class AlarmStatusChangedEvent : PubSubEvent<AlarmStatusChangedPayload> { } // 确认/解决等状态变更
public class AlarmCountChangedEvent  : PubSubEvent<AlarmCountChangedPayload> { }  // 数量变化（Badge）
```

注意：载荷是 `XxxPayload` 类（旧文档中的 `EventArgs`/`PubSubEvent<int>` 已过时）。发布方与订阅方必须引用 Contracts 里的同一事件类型。

## 服务

| 组件 | 位置 | 说明 |
|---|---|---|
| `IAlarmService` / `AlarmService` | `src/Vk.Dbp.Services/Alarm/` | 告警生命周期：创建、确认、解决、查询 |
| `IAlarmConfigService` / `AlarmConfigService` | 同上 | 告警类型/阈值配置（DB 持久化） |
| 注册点 | `DbpAccountModule.RegisterTypes` | `RegisterSingleton<IAlarmService, AlarmService>()` |

`AlarmService` 构造器注入 `ISqlSugarClient` + `IRepository<T>` + `IAuditLogService` + `IUserSession`，确认/状态迁移都有审计。**全局告警用 `UserId == 0` 表示**（发给所有人）。

## 生命周期流转

```text
触发:   条件检出 → IAlarmService.CreateAlarmAsync(info)
         → 发布 AlarmTriggeredEvent + AlarmCountChangedEvent
确认:   用户点击 → AcknowledgeAlarmAsync(alarmId, userId) → 发布 AlarmStatusChangedEvent
解决:   条件恢复/人工 → ResolveAlarmAsync(alarmId) → 发布 AlarmStatusChangedEvent + AlarmCountChangedEvent
```

Shell 的 `HeaderViewModel`（`src/Vk.Dbp.WpfWindow/ViewModels/HeaderViewModel.cs`）订阅全部三个事件并更新角标/弹通知，因此业务模块只要正确发布事件，UI 侧自动跟随。

## 订阅方必备三件套（HeaderViewModel 范式）

```csharp
// 构造器订阅
_eventAggregator.GetEvent<AlarmTriggeredEvent>().Subscribe(OnAlarmTriggered);

// 回调：守卫 + UI 线程
private void OnAlarmTriggered(AlarmTriggeredPayload payload)
{
    if (_isDisposed) return;
    Application.Current.Dispatcher.Invoke(() =>
    {
        AlarmCount = ...;
        HandyControl.Controls.Growl.Warning(new GrowlInfo
        {
            Message = payload.Message,
            WaitTime = 10,
            StaysOpen = false
        });
    });
}

// IDisposable：取消订阅后置位
public void Dispose()
{
    if (_isDisposed) return;
    _eventAggregator.GetEvent<AlarmTriggeredEvent>().Unsubscribe(OnAlarmTriggered);
    _isDisposed = true;
}
```

模块可能未加载时的延迟解析（HeaderViewModel 用法）：

```csharp
private IAlarmService? _alarmService;
private IAlarmService AlarmService => _alarmService ??= _container.Resolve<IAlarmService>();
```

## 新增告警源的步骤

1. 检测方模块注入 `IAlarmService` + `IEventAggregator`。
2. 条件命中时 `CreateAlarmAsync`（服务内部或调用方发布 `AlarmTriggeredEvent`——跟随 AlarmService 现有实现，勿双发）。
3. 需要新告警类型/阈值时通过 `IAlarmConfigService` 配置（DB 支撑，非硬编码）。
4. UI 侧无需改动（HeaderViewModel 自动响应）；模块内列表页参照 `prismModules/Vk.Dbp.WorkshopModule/ViewModels/AlarmRecordViewModel.cs`（最完整的告警列表实现，注意其 `IRegionManager` 直接注入是存量违例，新代码用 `INavigationService`）。

## 跨模块普通通知

非告警类的全局通知走 `IGlobalNotificationPublisher`（契约在 `src/Vk.Dbp.Contracts/Services/`，实现在 AccountModule），事件为 `GlobalNotificationEvent`。

## 常见问题

| 症状 | 处理 |
|---|---|
| 事件收不到 | 事件类必须定义在 `Vk.Dbp.Contracts/Events/`；双方引用同一类型 |
| UI 不更新 | 回调没走 `Dispatcher.Invoke` |
| 内存泄漏 | 订阅 VM 缺 `IDisposable`/退订 |
| 启动早期拿不到 AlarmService | 它注册在 AccountModule——用容器延迟解析，别在构造器直接注入 |
| 告警规则硬编码 | 走 `IAlarmConfigService`（路线图方向是告警规则引擎，勿再加静态规则） |

## 验证

```powershell
dotnet test test\Vk.Dbp.Tests.Unit\Vk.Dbp.Tests.Unit.csproj
dotnet build desktop.boilerplate.slnx
```

事件链路属运行时行为，落地后用 `.\scripts\start-wpf-local.ps1` 人工触发一次告警确认角标与 Growl 均生效。
