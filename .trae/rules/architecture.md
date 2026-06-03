# Architecture Rules

## Layer Ownership

- `Vk.Dbp.WpfWindow` owns shell startup (`PrismBootstrapper`), global layout, region composition, themes (`IThemeService`), notifications, lock screen, alarms, host-level services, and application-level configuration.
- `Vk.Dbp.AdminWindow` owns the independent administration window with its own Models and ViewModels, separate from the main shell.
- `Vk.Dbp.Contracts` owns interfaces, Prism events (`PubSubEvent<T>`), extension points, module metadata, paging contracts, navigation contracts (`INavigationService`), and shared service contracts consumed by multiple projects.
- `Vk.Dbp.Domain` owns domain abstractions and module definitions (`DbpDomainModule`). Currently minimal; grow it for shared domain logic that does not belong in a specific module or Infrastructure.
- `Vk.Dbp.Services` owns reusable application services that are not tied to a single Prism module — session (`IUserSession`), settings (`IAppSettingsService`), audit, alarm, export, and caching.
- `Vk.Dbp.Infrastructure` owns database entities, SqlSugar configuration (`SqlSugarScope`), repositories, and database initialization (`IAppStartupService`).
- `Vk.Dbp.Utils` owns low-level reusable utilities such as password hashing, SM4 encryption helpers, ID generation, and logging helpers.
- `Vk.Dbp.Tools` owns standalone tool applications with their own Layout and ViewModels.
- `dbpframework/Vk.Dbp.Core` owns framework-level abstractions (`IDbpModule`, `ServiceCollectionExtensions`).
- `dbpframework/Vk.Dbp.Account` owns account primitives — `CurrentUser`, `ICurrentUser`, `PermissionDto`, `RoleDto`.
- `prismModules/*` own feature-specific Views, ViewModels, module services, models, and module registration (`Dbp{Name}Module : IModule`).
- `dbpApps/*` own customer-specific application entry points and composition (e.g. `Dbp.Material.Forming`, `Dbp.Material.Mixing`).

## Dependency Direction

- Prefer dependencies from UI/module layers toward contracts and shared services.
- Do not introduce direct dependencies from one business module into another business module implementation.
- Shared behavior needed by multiple modules should move behind a contract in `Vk.Dbp.Contracts` and an implementation in the appropriate shared layer.
- Keep persistence-specific types out of ViewModels unless the existing nearby code already uses that pattern and changing it would create unrelated churn.
- `dbpframework` projects are the innermost layer; they must not depend on `src/` or `prismModules/`.

## Prism And WPF Conventions

- Use `IModule.RegisterTypes` for module service and navigation registration.
- Use `RegisterForNavigation<TView>()` for navigable views; rely on Prism's `ViewModelLocator` convention to resolve ViewModels.
- Use `RegisterSingleton<IService, Impl>()` for shared stateful services.
- Use `INavigationService.NavigateTo(viewName)` (the Contracts-layer wrapper) instead of calling `IRegionManager.RequestNavigate` directly in ViewModels, unless the ViewModel is in the shell where `PrismNavigationService` lives.
- Use `BindableBase`, `DelegateCommand` / `DelegateCommand<T>`, and `SetProperty(ref _field, value)` for property notification.
- Keep XAML code-behind thin. Put state and user actions in ViewModels unless the behavior is purely visual.
- For reusable View names, region names, or route-like names, prefer constants in `NavigationConstants` (`ViewNames`, `RegionNames`, `AccountActions`).
- Subscribe to cross-module events through `IEventAggregator` (Prism `PubSubEvent<T>`), not direct module references.
- In event callbacks that update UI, use `Application.Current.Dispatcher.Invoke()` to ensure UI-thread execution.

## ViewModel Conventions

- All constructor-injected dependencies must have null-checks: `_svc = svc ?? throw new ArgumentNullException(nameof(svc))`.
- Commands use `DelegateCommand` with `ObservesProperty` for automatic `CanExecute` refresh.
- ViewModels that subscribe to events or `INotifyPropertyChanged` should implement `IDisposable` with an `_isDisposed` guard and unsubscribe in `Dispose()`.
- Lazy-resolve services only when the service is registered in a module that may not be loaded yet (e.g. `IAlarmService` from AccountModule resolved via `_container.Resolve<T>()`).

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

Register the module in `PrismBootstrapper.ConfigureModuleCatalog` only after its project builds and its navigation registrations are in place.
