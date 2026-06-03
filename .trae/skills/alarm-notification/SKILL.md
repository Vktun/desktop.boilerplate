---
name: "alarm-notification"
description: "Handles alarm management, Prism events for alarms, and notification display in the Desktop Boilerplate WPF/Prism repository. Invoke when adding alarm features, alarm configuration, notification display, or working with alarm-related Prism events."
---

# Alarm Notification Skill

## Overview

This skill covers the alarm and notification system in Desktop Boilerplate, which uses Prism's `IEventAggregator` for cross-module alarm event communication, `IAlarmService` for alarm management, and the shell's HeaderViewModel for alarm display.

## Core Components

### Prism Events for Alarms

Defined in `src/Vk.Dbp.Contracts/Events/`:

```csharp
// Alarm triggered — new alarm occurred
public class AlarmTriggeredEvent : PubSubEvent<AlarmTriggeredEventArgs> { }

// Alarm count changed — update badge/count
public class AlarmCountChangedEvent : PubSubEvent<int> { }

// Alarm status changed — acknowledged, resolved, etc.
public class AlarmStatusChangedEvent : PubSubEvent<AlarmStatusChangedEventArgs> { }
```

### IAlarmService

Registered as singleton in `DbpAccountModule.RegisterTypes`:

```csharp
containerRegistry.RegisterSingleton<IAlarmService, AlarmService>();
```

The service is lazy-resolved in `HeaderViewModel` because it lives in AccountModule which may not be loaded yet:

```csharp
private IAlarmService _alarmService;
private IAlarmService AlarmService =>
    _alarmService ??= _container.Resolve<IAlarmService>();
```

### HeaderViewModel — Alarm Display

`HeaderViewModel` subscribes to all three alarm events:

```csharp
_eventAggregator.GetEvent<AlarmTriggeredEvent>().Subscribe(OnAlarmTriggered);
_eventAggregator.GetEvent<AlarmCountChangedEvent>().Subscribe(OnAlarmCountChanged);
_eventAggregator.GetEvent<AlarmStatusChangedEvent>().Subscribe(OnAlarmStatusChanged);
```

Event handlers use `Dispatcher.Invoke` for UI thread safety:

```csharp
private void OnAlarmTriggered(AlarmTriggeredEventArgs args)
{
    Application.Current.Dispatcher.Invoke(() =>
    {
        // Update alarm badge, show notification
    });
}
```

### Alarm Configuration

Alarm types and thresholds are managed through the AccountModule's alarm configuration views. Configuration is persisted in the database via `IAlarmService`.

## Alarm Flow

### Triggering an Alarm

```
1. Condition detected (e.g. sensor threshold exceeded)
2. IAlarmService.CreateAlarmAsync(alarmInfo)
3. Publish AlarmTriggeredEvent via IEventAggregator
4. Publish AlarmCountChangedEvent with new count
5. HeaderViewModel receives events → updates UI
6. Notification displayed to user
```

### Acknowledging an Alarm

```
1. User clicks alarm in notification/list
2. IAlarmService.AcknowledgeAlarmAsync(alarmId, userId)
3. Publish AlarmStatusChangedEvent
4. HeaderViewModel updates alarm badge
```

### Resolving an Alarm

```
1. Condition returns to normal or user resolves
2. IAlarmService.ResolveAlarmAsync(alarmId)
3. Publish AlarmStatusChangedEvent + AlarmCountChangedEvent
4. UI updates accordingly
```

## Adding a New Alarm Type

1. Define the alarm type in the alarm configuration (database-backed).
2. In the detecting module, inject `IAlarmService` and `IEventAggregator`.
3. Call `IAlarmService.CreateAlarmAsync()` when the condition is detected.
4. Publish `AlarmTriggeredEvent` via `IEventAggregator`.
5. The shell's HeaderViewModel will automatically display the notification.

## Notification Display Patterns

### Badge Count in Header

```csharp
private int _alarmCount;
public int AlarmCount
{
    get => _alarmCount;
    set => SetProperty(ref _alarmCount, value);
}

private void OnAlarmCountChanged(int count)
{
    Application.Current.Dispatcher.Invoke(() =>
    {
        AlarmCount = count;
    });
}
```

### Alarm Notification Popup

Use HandyControl's `Growl` or notification components for transient alarm notifications:

```csharp
private void OnAlarmTriggered(AlarmTriggeredEventArgs args)
{
    Application.Current.Dispatcher.Invoke(() =>
    {
        HandyControl.Controls.Growl.Warning(new GrowlInfo
        {
            Message = args.Message,
            WaitTime = 10,
            StaysOpen = false
        });
    });
}
```

## Key Files

| Component | Location |
|-----------|----------|
| Alarm events | `src/Vk.Dbp.Contracts/Events/` |
| IAlarmService | `src/Vk.Dbp.Contracts/Services/` (interface) |
| AlarmService | `prismModules/Vk.Dbp.AccountModule/Services/` (implementation) |
| HeaderViewModel | `src/Vk.Dbp.WpfWindow/ViewModels/HeaderViewModel.cs` |
| HeaderView | `src/Vk.Dbp.WpfWindow/Layout/HeaderView.xaml` |
| Alarm config views | `prismModules/Vk.Dbp.AccountModule/Views/` |
| Alarm entities | `src/Vk.Dbp.Infrastructure/Entities/` |

## Common Issues

### Issue: AlarmService not available at startup
**Solution**: `IAlarmService` is registered in AccountModule. Use lazy resolution via `_container.Resolve<IAlarmService>()` in components that may initialize before the module loads.

### Issue: Alarm events not received
**Solution**: Ensure the event class inherits `PubSubEvent<T>` and is defined in `Vk.Dbp.Contracts`. Both publisher and subscriber must reference the same event type from Contracts.

### Issue: UI not updating on alarm
**Solution**: Ensure event handlers use `Application.Current.Dispatcher.Invoke()` to marshal updates to the UI thread.

### Issue: Memory leak from event subscriptions
**Solution**: ViewModels that subscribe to alarm events must implement `IDisposable` and unsubscribe in `Dispose()`. Use `_isDisposed` guard to prevent double-unsubscribe.

## Testing

```csharp
[Fact]
public void AlarmTriggered_PublishesEvent()
{
    var eventAggregator = new Mock<IEventAggregator>();
    var alarmEvent = new Mock<AlarmTriggeredEvent>();
    eventAggregator.Setup(e => e.GetEvent<AlarmTriggeredEvent>())
        .Returns(alarmEvent.Object);

    // Trigger alarm
    alarmEvent.Verify(e => e.Publish(It.IsAny<AlarmTriggeredEventArgs>()), Times.Once);
}
```
