---
name: "login-session-system"
description: "Manages user login and session in WPF/Prism applications. Invoke when user needs login functionality, session management, user authentication, lock/unlock screen, or permission checking features."
---

# Login Session System Skill

## Overview

This skill covers the user login and session management system in Desktop Boilerplate, including login/logout, session persistence, lock/unlock screen, permission checking, and user info display.

## Core Components

### IUserSession / IUserInfo (Singleton)

The session system uses a two-tier interface design:

- `IUserInfo` — lightweight interface for audit-only needs: `UserId`, `Username`, `IsLoggedIn`.
- `IUserSession : IUserInfo` — full session interface with `Login()`, `Logout()`, `Lock()`, `Unlock()`, `SetPermissions()`, `HasPermission()`, `Token`, and all user profile fields.

Implementation: `UserSession : BindableBase, IUserSession` registered as `RegisterSingleton<IUserSession, UserSession>`.

```csharp
// Use IUserInfo when you only need identity context (e.g. audit logging)
public class AuditLogService
{
    private readonly IUserInfo _userInfo;
    public AuditLogService(IUserInfo userInfo) { _userInfo = userInfo; }
}

// Use IUserSession when you need full session management
public class LoginViewModel
{
    private readonly IUserSession _userSession;
    public LoginViewModel(IUserSession userSession) { _userSession = userSession; }
}
```

### Login Flow

```
1. User enters credentials in LoginView
2. LoginViewModel validates (non-empty, length limits)
3. Query user via IUserService → check enabled → verify password
4. Each failure step logs via IAuditLogService.LogFailureAsync with specific reason
5. Success → _userSession.Login() + _userSession.SetPermissions()
6. Generate token via RandomNumberGenerator
7. Persist "remember username" preference
8. Navigate to Dashboard via INavigationService
```

### Lock/Unlock Flow

```
Lock: _userSession.Lock() → sets IsLocked=true, shows lock screen overlay
Unlock: _userSession.Unlock() → sets IsLocked=false, returns to previous view
Note: Lock does NOT clear user info or permissions
```

### Logout Flow

```
1. User clicks logout (HeaderViewModel command)
2. _userSession.Logout() → clears all fields, IsLoggedIn=false, resets permissions
3. Navigate back to LoginView
```

### Permission System

```
- _userSession.HasPermission(permissionCode) checks loaded permissions
- Admin user automatically has ALL permissions (hardcoded in HasPermission)
- Menu visibility controlled by IMenuPermissionFilter.IsMenuVisible(viewName)
- Permissions loaded via _userSession.SetPermissions() after login
```

### Auto-Update Mechanism

```
UserSession (BindableBase) property change
    ↓
PropertyChanged event
    ↓
HeaderViewModel subscribes via INotifyPropertyChanged
    ↓
HeaderView auto-updates via data binding
```

## Key Files

| Component | Location |
|-----------|----------|
| IUserSession / IUserInfo | `src/Vk.Dbp.Services/Session/IUserSession.cs`, `IUserInfo.cs` |
| UserSession | `src/Vk.Dbp.Services/Session/UserSession.cs` |
| LoginView | `prismModules/Vk.Dbp.AccountModule/Views/LoginView.xaml` |
| LoginViewModel | `prismModules/Vk.Dbp.AccountModule/ViewModels/LoginViewModel.cs` |
| HeaderView | `src/Vk.Dbp.WpfWindow/Layout/HeaderView.xaml` |
| HeaderViewModel | `src/Vk.Dbp.WpfWindow/ViewModels/HeaderViewModel.cs` |
| PrismBootstrapper (startup) | `src/Vk.Dbp.WpfWindow/PrismBootstrapper.cs` |

## Default Test User

```
Username: admin
Password: 123456
```

**This is development-only.** Change via `start-wpf-local.ps1 -FirstRun -AdminPassword` for any non-development environment.

## Implementation Patterns

### Constructor Injection with Null Guard

```csharp
public class LoginViewModel : BindableBase, INavigationAware
{
    private readonly IUserSession _userSession;
    private readonly IUserService _userService;

    public LoginViewModel(
        IUserSession userSession,
        IUserService userService)
    {
        _userSession = userSession ?? throw new ArgumentNullException(nameof(userSession));
        _userService = userService ?? throw new ArgumentNullException(nameof(userService));
    }
}
```

### Session Change Subscription in HeaderViewModel

```csharp
// Subscribe to session changes
(_userSession as INotifyPropertyChanged).PropertyChanged += OnSessionPropertyChanged;

private void OnSessionPropertyChanged(object sender, PropertyChangedEventArgs e)
{
    Application.Current.Dispatcher.Invoke(() =>
    {
        switch (e.PropertyName)
        {
            case nameof(IUserSession.IsLoggedIn):
                // Update UI
                break;
            case nameof(IUserSession.IsLocked):
                // Show/hide lock screen
                break;
        }
    });
}
```

### INavigationAware for Auto-Redirect

```csharp
public void OnNavigatedTo(NavigationContext navigationContext)
{
    if (_userSession.IsLoggedIn)
    {
        _navigationService.NavigateTo("Dashboard");
    }
}
```

## Quick Test Checklist

- [ ] Application starts with login page
- [ ] User can login with credentials
- [ ] User info displays after login in header
- [ ] Logout function works correctly
- [ ] Lock/unlock screen works without losing session
- [ ] Permission-based menu visibility works
- [ ] Failed login attempts are audit-logged with reason
- [ ] Session timeout triggers lock screen

## Design Patterns Used

- **Singleton Pattern**: `IUserSession` registered as singleton
- **MVVM Pattern**: View-ViewModel separation with `BindableBase`
- **Observer Pattern**: `PropertyChanged` events for session state propagation
- **Dependency Injection**: Prism/Unity IoC container
- **Interface Segregation**: `IUserInfo` vs `IUserSession` for different consumers
