---
name: "navigation-routing"
description: "Handles Prism region navigation, view routing, NavigationParameters, and navigation constants in the Desktop Boilerplate WPF/Prism repository. Invoke when adding navigation routes, managing region content, passing navigation parameters, or working with NavigationConstants."
---

# Navigation Routing Skill

## Overview

This skill covers the navigation system in Desktop Boilerplate, which uses Prism's region navigation pattern wrapped in a Contracts-layer `INavigationService` for decoupled view routing.

## Core Components

### INavigationService (Contracts Layer)

Defined in `src/Vk.Dbp.Contracts/Services/INavigationService.cs`, implemented by `PrismNavigationService` in `src/Vk.Dbp.WpfWindow/Services/PrismNavigationService.cs`.

```csharp
public interface INavigationService
{
    void NavigateTo(string viewName, NavigationParameters parameters = null);
    void NavigateTo<TView>(NavigationParameters parameters = null);
    event Action<NavigationResult> NavigationCompleted;
}
```

`PrismNavigationService` wraps `IRegionManager.RequestNavigate` and always navigates to `RegionNames.ContentRegion`.

### NavigationConstants

Defined in `src/Vk.Dbp.WpfWindow/Constants/NavigationConstants.cs`:

```csharp
public static class RegionNames
{
    public const string ContentRegion = "ContentRegion";
}

public static class ViewNames
{
    public const string Dashboard = "Dashboard";
    public const string LoginView = "LoginView";
    public const string SelfCheck = "SelfCheck";
    public const string Production = "Production";
    // Add new view names here
}

public static class AccountActions
{
    public const string ChangePassword = "ChangePassword";
    public const string Logout = "Logout";
    public const string Shutdown = "Shutdown";
    public const string Close = "Close";
}
```

## Navigation Patterns

### Basic Navigation

```csharp
// In ViewModel — use INavigationService, NOT IRegionManager
_navigationService.NavigateTo(ViewNames.Dashboard);
```

### Navigation with Parameters

```csharp
var parameters = new NavigationParameters
{
    { "UserId", userId },
    { "EditMode", true }
};
_navigationService.NavigateTo(ViewNames.UserManagement, parameters);
```

### Receiving Parameters (INavigationAware)

```csharp
public class DetailViewModel : BindableBase, INavigationAware
{
    public void OnNavigatedTo(NavigationContext navigationContext)
    {
        var userId = navigationContext.Parameters.GetValue<int>("UserId");
        var editMode = navigationContext.Parameters.GetValue<bool>("EditMode");
        // Initialize with parameters
    }

    public bool IsNavigationTarget(NavigationContext navigationContext) => true;

    public void OnNavigatedFrom(NavigationContext navigationContext) { }
}
```

### Conditional Navigation (Login Check)

```csharp
// In PrismBootstrapper.InitializeShell
var initialView = userSession.IsLoggedIn ? ViewNames.Dashboard : ViewNames.LoginView;
regionManager.RequestNavigate(RegionNames.ContentRegion, initialView);
```

### Auto-Redirect on Navigation

```csharp
public void OnNavigatedTo(NavigationContext navigationContext)
{
    if (_userSession.IsLoggedIn)
    {
        _navigationService.NavigateTo(ViewNames.Dashboard);
    }
}
```

## View Registration

Views are registered in module `RegisterTypes` methods:

```csharp
// In DbpAccountModule.RegisterTypes
containerRegistry.RegisterForNavigation<LoginView>();
containerRegistry.RegisterForNavigation<UserManagementView>();

// In DbpWorkshopModule.RegisterTypes
containerRegistry.RegisterForNavigation<Dashboard>();
containerRegistry.RegisterForNavigation<SelfCheck>();
```

Prism's `ViewModelLocator` automatically resolves ViewModels by convention (`LoginView` → `LoginViewModel`).

For non-convention mappings, register explicitly in `PrismBootstrapper.ConfigureViewModelLocator`:

```csharp
ViewModelLocationProvider.Register<CustomView, CustomViewModel>();
```

## Adding a New Navigation Route

1. Add view name constant to `NavigationConstants.ViewNames`.
2. Create the View (XAML) and ViewModel.
3. Register with `RegisterForNavigation<TView>()` in the module's `RegisterTypes`.
4. Add menu entry in HeaderViewModel with permission check via `IMenuPermissionFilter.IsMenuVisible()`.
5. Navigate using `_navigationService.NavigateTo(ViewNames.NewView)`.

## Key Files

| Component | Location |
|-----------|----------|
| INavigationService | `src/Vk.Dbp.Contracts/Services/INavigationService.cs` |
| PrismNavigationService | `src/Vk.Dbp.WpfWindow/Services/PrismNavigationService.cs` |
| NavigationConstants | `src/Vk.Dbp.WpfWindow/Constants/NavigationConstants.cs` |
| PrismBootstrapper | `src/Vk.Dbp.WpfWindow/PrismBootstrapper.cs` |
| HeaderViewModel (menu navigation) | `src/Vk.Dbp.WpfWindow/ViewModels/HeaderViewModel.cs` |

## Common Issues

### Issue: Navigation silently fails
**Solution**: Check that the view is registered with `RegisterForNavigation<TView>()` and the view name matches exactly (case-sensitive).

### Issue: ViewModel not resolved
**Solution**: Ensure ViewModel naming follows Prism convention (`XxxView` → `XxxViewModel`) or add explicit mapping in `ConfigureViewModelLocator`.

### Issue: Parameters lost after navigation
**Solution**: Ensure the target ViewModel implements `INavigationAware` and reads parameters in `OnNavigatedTo`, not in the constructor.

### Issue: Using IRegionManager directly in ViewModels
**Solution**: Replace with `INavigationService.NavigateTo()` from Contracts layer. `IRegionManager.RequestNavigate` should only be used in `PrismNavigationService` and `PrismBootstrapper`.
