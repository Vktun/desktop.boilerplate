# Desktop Boilerplate — Claude Code Project Instructions

## Project Overview

This is a .NET 10 WPF enterprise desktop application boilerplate using Prism, Unity, HandyControl, SqlSugar, Serilog, Fody, and xUnit. It follows a layered architecture with a reusable host shell, shared contracts/services, Prism business modules, framework packages, and customer application entry points.

## Project Structure

```
desktop.boilerplate/
├── dbpApps/                          # Customer application entry points
│   ├── Dbp.Material.Forming/         # Material forming app
│   └── Dbp.Material.Mixing/          # Material mixing app
├── dbpframework/                     # Core framework (innermost layer)
│   ├── Vk.Dbp.Account/               # Account primitives (CurrentUser, PermissionDto)
│   └── Vk.Dbp.Core/                  # Core abstractions (IDbpModule, extensions)
├── prismModules/                     # Prism feature modules
│   ├── Vk.Dbp.AccountModule/         # Account management (login, users, roles, permissions)
│   └── Vk.Dbp.WorkshopModule/        # Workshop example (dashboard, production, self-check)
├── src/                              # Core source libraries
│   ├── Vk.Dbp.WpfWindow/             # Main WPF shell & PrismBootstrapper
│   ├── Vk.Dbp.AdminWindow/           # Independent admin window
│   ├── Vk.Dbp.Contracts/             # Cross-module contracts & Prism events
│   ├── Vk.Dbp.Domain/                # Domain abstractions
│   ├── Vk.Dbp.Services/              # Shared application services
│   ├── Vk.Dbp.Infrastructure/        # Persistence & SqlSugar setup
│   ├── Vk.Dbp.Utils/                 # Security (SM4, password hashing), ID generation
│   └── Vk.Dbp.Tools/                 # Standalone tool applications
├── test/                             # Test projects
│   ├── Vk.Dbp.Tests.Unit/            # Unit tests (xUnit + Moq + FluentAssertions)
│   ├── Vk.Dbp.Tests.Integration/     # Integration tests (SQL Server required)
│   └── Vk.Dbp.Tests.Common/          # Shared test fixtures & factories
├── docs/                             # Project documentation
└── scripts/                          # PowerShell scripts (start-wpf-local, publish)
```

## Layer Ownership

| Project | Owns |
|---------|------|
| `Vk.Dbp.WpfWindow` | Shell startup (`PrismBootstrapper`), global layout, themes (`IThemeService`), notifications, lock screen, alarms, navigation (`PrismNavigationService`), host services |
| `Vk.Dbp.AdminWindow` | Independent administration window with own Models/ViewModels |
| `Vk.Dbp.Contracts` | Interfaces, Prism events (`PubSubEvent<T>`), extension points, navigation contracts (`INavigationService`), shared service contracts |
| `Vk.Dbp.Domain` | Domain abstractions (`DbpDomainModule`) |
| `Vk.Dbp.Services` | Reusable app services — session (`IUserSession`/`IUserInfo`), settings (`IAppSettingsService`), audit (`IAuditLogService`), alarm, export, caching |
| `Vk.Dbp.Infrastructure` | Database entities, SqlSugar config (`SqlSugarScope`), repositories, DB init (`IAppStartupService`) |
| `Vk.Dbp.Utils` | Password hashing, SM4 encryption, ID generation, logging helpers |
| `Vk.Dbp.Tools` | Standalone tool applications |
| `dbpframework/Vk.Dbp.Core` | Framework abstractions (`IDbpModule`, `ServiceCollectionExtensions`) |
| `dbpframework/Vk.Dbp.Account` | Account primitives (`CurrentUser`, `ICurrentUser`, `PermissionDto`, `RoleDto`) |
| `prismModules/*` | Feature-specific Views, ViewModels, module services, models, registration (`Dbp{Name}Module : IModule`) |
| `dbpApps/*` | Customer-specific application entry points |

## Architecture Rules

### Dependency Direction

- Dependencies flow inward: Presentation → Application → Domain → Infrastructure.
- `dbpframework` is the innermost layer; must NOT depend on `src/` or `prismModules/`.
- Business modules must NOT depend on each other directly; use `Vk.Dbp.Contracts` for shared contracts.
- ViewModels must NOT directly access SqlSugar or repository types; use service boundaries.
- Shared behavior needed by multiple modules goes behind a contract in `Vk.Dbp.Contracts`.

### Prism And WPF Conventions

- Use `IModule.RegisterTypes` for module service and navigation registration.
- Use `RegisterForNavigation<TView>()` for navigable views; rely on Prism's `ViewModelLocator` convention.
- Use `RegisterSingleton<IService, Impl>()` for shared stateful services.
- Use `INavigationService.NavigateTo(viewName)` (Contracts-layer wrapper) instead of `IRegionManager.RequestNavigate` directly in ViewModels.
- Use `BindableBase`, `DelegateCommand` / `DelegateCommand<T>`, and `SetProperty(ref _field, value)`.
- Keep XAML code-behind thin. Put state and user actions in ViewModels.
- Use constants in `NavigationConstants` (`ViewNames`, `RegionNames`, `AccountActions`).
- Cross-module events use `IEventAggregator` (`PubSubEvent<T>`), not direct module references.
- Event callbacks updating UI must use `Application.Current.Dispatcher.Invoke()`.

### ViewModel Conventions

- Constructor-injected dependencies: `_svc = svc ?? throw new ArgumentNullException(nameof(svc))`.
- Commands: `DelegateCommand` with `.ObservesProperty()` for automatic CanExecute refresh.
- ViewModels subscribing to events must implement `IDisposable` with `_isDisposed` guard and unsubscribe in `Dispose()`.
- Lazy-resolve services only when registered in modules that may not be loaded yet (e.g. `_container.Resolve<IAlarmService>()`).

### New Module Shape

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

Register in `PrismBootstrapper.ConfigureModuleCatalog` via `moduleCatalog.AddModule<DbpYourModule>()`.

## Code Conventions

- Constructor null-guards: `_svc = svc ?? throw new ArgumentNullException(nameof(svc))`.
- ViewModels inherit `BindableBase`; use `SetProperty(ref _field, value)`.
- Commands: `DelegateCommand` / `DelegateCommand<T>` with `.ObservesProperty()`.
- Event-subscribing ViewModels implement `IDisposable` with `_isDisposed` guard.
- UI updates in event callbacks: `Application.Current.Dispatcher.Invoke()`.
- Module class naming: `Dbp{Name}Module : IModule`.
- View registration: `RegisterForNavigation<TView>()` with ViewModelLocator convention.
- Service registration: `RegisterSingleton<IService, Impl>()` for shared stateful services.

## Security Rules

- Never commit real credentials, API keys, tokens, or production passwords.
- Use `appsettings.local.json` or environment variables for developer-specific values.
- `IUserInfo` for audit-only needs; `IUserSession` for full session management.
- Admin user has all permissions in dev — must not be relied upon in production.
- Use `IAuditLogService` to record both successful and failed operations.
- Session tokens generated via `RandomNumberGenerator`, held in-memory only, cleared on `Logout()`.
- SqlSugar connection errors trigger automatic lock-screen — do not remove this safety behavior.
- Default test user (admin / 123456) is development-only.

## Database Startup Order

```
PrismBootstrapper.InitializeShell:
  Splash screen → IAppStartupService.InitializeDatabaseAsync() → Shell display → Initial navigation
```

Database initialization MUST complete before any DB-backed UI or service is accessed.

## Commands

```powershell
dotnet restore desktop.boilerplate.slnx
dotnet build desktop.boilerplate.slnx
dotnet test test\Vk.Dbp.Tests.Unit\Vk.Dbp.Tests.Unit.csproj
```

Local startup:
```powershell
.\scripts\start-wpf-local.ps1
```

First-run database initialization:
```powershell
.\scripts\start-wpf-local.ps1 -FirstRun -AdminPassword "change-me-before-first-login"
```

## Testing

- Unit tests: xUnit + Moq + FluentAssertions.
- Shared fixtures: `test/Vk.Dbp.Tests.Common`.
- Integration tests: `test/Vk.Dbp.Tests.Integration` (requires SQL Server/LocalDB).
- For non-trivial changes, add or update focused tests.

## Review Focus

When reviewing or changing this project, pay special attention to:

- Startup order and database initialization
- Session identity, audit logging, and permission consistency
- Secret handling and local configuration
- Module boundaries and navigation registration
- WPF binding performance and UI thread safety
- Tests around service behavior, failure paths, and persistence-sensitive logic
- SqlSugar connection error handling (auto lock-screen in AOP)

## Key Files Reference

| Purpose | File |
|---------|------|
| Bootstrapper | `src/Vk.Dbp.WpfWindow/PrismBootstrapper.cs` |
| Navigation constants | `src/Vk.Dbp.WpfWindow/Constants/NavigationConstants.cs` |
| Navigation service interface | `src/Vk.Dbp.Contracts/Services/INavigationService.cs` |
| Navigation implementation | `src/Vk.Dbp.WpfWindow/Services/PrismNavigationService.cs` |
| Session interface | `src/Vk.Dbp.Services/Session/IUserSession.cs` |
| Session implementation | `src/Vk.Dbp.Services/Session/UserSession.cs` |
| Theme service | `src/Vk.Dbp.WpfWindow/Services/ThemeService.cs` |
| Module registration example | `prismModules/Vk.Dbp.AccountModule/DbpAccountModule.cs` |
