---
name: "login-session-system"
description: "Manages user login and session in WPF/Prism applications. Invoke when user needs login functionality, session management, or user authentication features."
---

# Login Session System Skill

## Overview

This skill provides a complete user login and session management system for WPF/Prism applications, including login, logout, session persistence, and user info display.

## Core Components

### UserSession (Singleton)

```csharp
// Location: prismModules/Vk.Dbp.AccountModule/Models/UserSession.cs
public class UserSession : BindableBase
{
    private static readonly Lazy<UserSession> _instance = 
        new Lazy<UserSession>(() => new UserSession());
    
    public static UserSession Instance => _instance.Value;
    
    public bool IsLoggedIn { get; private set; }
    public string Username { get; private set; }
    public string RealName { get; private set; }
    public string Email { get; private set; }
    public DateTime? LoginTime { get; private set; }
    
    public void Login(User user, string token = null);
    public void Logout();
}
```

### Login Flow

```
1. User enters credentials
2. LoginViewModel validates
3. Success → Save to UserSession → Navigate
4. Failure → Show error
```

### Auto-Update Mechanism

```
UserSession property change
    ↓
PropertyChanged event
    ↓
HeaderViewModel subscription
    ↓
HeaderView auto-update
```

### Logout Flow

```
Click logout
    ↓
UserSession.Logout()
    ↓
Clear all info, set IsLoggedIn=false
    ↓
Navigate back to LoginView
```

## Key Files

| Component | Location |
|-----------|----------|
| UserSession | `prismModules/Vk.Dbp.AccountModule/Models/UserSession.cs` |
| LoginView | `prismModules/Vk.Dbp.AccountModule/Views/LoginView.xaml` |
| LoginViewModel | `prismModules/Vk.Dbp.AccountModule/ViewModels/LoginViewModel.cs` |
| HeaderView | `src/Vk.Dbp.WpfWindow/Layout/HeaderView.xaml` |
| HeaderViewModel | `src/Vk.Dbp.WpfWindow/ViewModels/HeaderViewModel.cs` |

## Default Test User

```
Username: admin
Password: 123456
```

## Quick Test Checklist

- [ ] Application starts with login page
- [ ] User can login with credentials
- [ ] User info displays after login
- [ ] HeaderView shows username and status
- [ ] Logout function works correctly
- [ ] Username changes to "Not logged in" after logout

## API Reference

### Check Login Status

```csharp
var session = UserSession.Instance;
if (session.IsLoggedIn)
{
    Console.WriteLine($"Current user: {session.Username}");
    Console.WriteLine($"Real name: {session.RealName}");
}
```

### Programmatic Login

```csharp
var user = new User
{
    Id = 1,
    Username = "testuser",
    RealName = "Test User",
    Email = "test@example.com"
};

var session = UserSession.Instance;
session.Login(user, "test-token");
```

### Logout

```csharp
var session = UserSession.Instance;
session.Logout();
Assert.IsFalse(session.IsLoggedIn);
```

## Design Patterns Used

- **Singleton Pattern**: UserSession
- **MVVM Pattern**: View-ViewModel separation
- **Observer Pattern**: PropertyChanged events
- **Dependency Injection**: Prism IoC Container
