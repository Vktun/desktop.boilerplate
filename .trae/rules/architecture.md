# Architecture Rules

## Layer Ownership

- `Vk.Dbp.WpfWindow` owns shell startup, global layout, region composition, themes, notifications, lock screen, alarms, host services, and application-level configuration.
- `Vk.Dbp.Contracts` owns interfaces, Prism events, extension points, module metadata, paging contracts, and shared service contracts consumed by multiple projects.
- `Vk.Dbp.Services` owns reusable application services that are not tied to a single Prism module.
- `Vk.Dbp.Infrastructure` owns database entities, SqlSugar configuration, repositories, and database initialization.
- `Vk.Dbp.Utils` owns low-level reusable utilities such as password hashing, encryption helpers, ID generation, and logging helpers.
- `prismModules/*` own feature-specific Views, ViewModels, module services, models, and module registration.
- `dbpApps/*` own customer-specific application entry points and composition.

## Dependency Direction

- Prefer dependencies from UI/module layers toward contracts and shared services.
- Do not introduce direct dependencies from one business module into another business module implementation.
- Shared behavior needed by multiple modules should move behind a contract in `Vk.Dbp.Contracts` and an implementation in the appropriate shared layer.
- Keep persistence-specific types out of ViewModels unless the existing nearby code already uses that pattern and changing it would create unrelated churn.

## Prism And WPF Conventions

- Use `IModule.RegisterTypes` for module service and navigation registration.
- Use `IRegionManager.RequestNavigate` for region navigation.
- Use `BindableBase`, `DelegateCommand`, and property notification patterns already present in nearby ViewModels.
- Keep XAML code-behind thin. Put state and user actions in ViewModels unless the behavior is purely visual.
- For reusable View names, region names, or route-like names, prefer constants such as `NavigationConstants` where available.

## New Module Shape

For a new business module, prefer:

```text
prismModules/Vk.Dbp.YourModule/
  Vk.Dbp.YourModule.csproj
  DbpYourModule.cs
  Views/
  ViewModels/
  Services/
  Models/
  Converters/
  Constants/
```

Register the module in the host only after its project builds and its navigation registrations are in place.

