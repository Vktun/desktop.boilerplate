# Desktop Boilerplate Agent Guide

## Project Snapshot

This repository is a WPF enterprise desktop application boilerplate for Dabp/Dbp-style systems. It uses .NET 10, Prism, Unity, HandyControl, SqlSugar, Serilog, Fody, and xUnit-based tests.

The codebase is organized around a reusable host shell, shared contracts and services, Prism business modules, framework packages, and customer application entry points:

- `src/Vk.Dbp.WpfWindow`: main WPF shell, `PrismBootstrapper`, layout, themes (`IThemeService`), startup, lock screen, notifications, alarms, navigation (`PrismNavigationService`), and host-level services.
- `src/Vk.Dbp.AdminWindow`: independent administration window with its own Models and ViewModels.
- `src/Vk.Dbp.Contracts`: cross-module contracts, Prism events (`PubSubEvent<T>`), extension points, navigation contracts (`INavigationService`), paging/cache abstractions, and service interfaces.
- `src/Vk.Dbp.Domain`: domain abstractions (`DbpDomainModule`). Currently minimal; grow for shared domain logic.
- `src/Vk.Dbp.Services`: reusable application services such as session (`IUserSession`/`IUserInfo`), settings (`IAppSettingsService`), audit (`IAuditLogService`), alarm, export, and caching.
- `src/Vk.Dbp.Infrastructure`: entities, repositories, SqlSugar setup (`SqlSugarScope`), and database initialization (`IAppStartupService`).
- `src/Vk.Dbp.Utils`: security (SM4 encryption, password hashing), ID generation, and logging utilities.
- `src/Vk.Dbp.Tools`: standalone tool applications with their own Layout and ViewModels.
- `dbpframework/Vk.Dbp.Core`: framework-level abstractions (`IDbpModule`, `ServiceCollectionExtensions`).
- `dbpframework/Vk.Dbp.Account`: account primitives (`CurrentUser`, `ICurrentUser`, `PermissionDto`, `RoleDto`).
- `prismModules`: independent Prism feature modules (`Vk.Dbp.AccountModule`, `Vk.Dbp.WorkshopModule`).
- `dbpApps`: customer/project-specific WPF application entry points (`Dbp.Material.Forming`, `Dbp.Material.Mixing`).
- `test`: xUnit, Moq, and FluentAssertions test projects (`Vk.Dbp.Tests.Unit`, `Vk.Dbp.Tests.Integration`, `Vk.Dbp.Tests.Common`).
- `docs`: project documentation (`LOCAL_CONFIGURATION.md`, `MODULE_DEVELOPMENT_GUIDE.md`, `PROJECT_REVIEW_AND_TODO.md`).

## Default Working Rules

- Preserve the layered architecture. Modules should depend on contracts and shared services instead of directly depending on other module implementations.
- Keep host-only behavior in `Vk.Dbp.WpfWindow`; avoid pushing customer-specific logic into the shell.
- Put shared extension contracts in `Vk.Dbp.Contracts` before using them from multiple modules.
- Put persistence entities and repository details in `Vk.Dbp.Infrastructure`; keep UI/ViewModel code away from SqlSugar-specific details when practical.
- Prefer Prism conventions already used in the repository: `IModule`, `IContainerRegistry`, region navigation, `BindableBase`, `DelegateCommand`, and view registration.
- For WPF UI, keep ViewModels testable. Avoid business logic in XAML code-behind except view-only behavior.
- Use `INavigationService.NavigateTo()` from Contracts layer in ViewModels instead of direct `IRegionManager.RequestNavigate`.
- Cross-module communication must use `IEventAggregator` (`PubSubEvent<T>`), not direct module references.
- Use `appsettings.local.json` or environment variables for local secrets. Do not commit real connection strings, passwords, tokens, or customer endpoints.
- Do not modify `bin`, `obj`, logs, publish output, or local IDE files.

## Code Conventions

- Constructor-injected dependencies: `_svc = svc ?? throw new ArgumentNullException(nameof(svc))`.
- ViewModels inherit `BindableBase`; use `SetProperty(ref _field, value)`.
- Commands: `DelegateCommand` / `DelegateCommand<T>` with `.ObservesProperty()` for CanExecute refresh.
- ViewModels that subscribe to events must implement `IDisposable` with `_isDisposed` guard and unsubscribe in `Dispose()`.
- UI updates in event callbacks: wrap in `Application.Current.Dispatcher.Invoke()`.
- Module class naming: `Dbp{Name}Module : IModule`.
- View registration: `RegisterForNavigation<TView>()` relying on ViewModelLocator convention.
- Service registration: `RegisterSingleton<IService, Impl>()` for shared stateful services.

## Commands

Run from the repository root unless a task says otherwise:

```powershell
dotnet restore desktop.boilerplate.slnx
dotnet build desktop.boilerplate.slnx
dotnet test test\Vk.Dbp.Tests.Unit\Vk.Dbp.Tests.Unit.csproj
```

For local startup:

```powershell
.\scripts\start-wpf-local.ps1
```

For first-run database initialization:

```powershell
.\scripts\start-wpf-local.ps1 -FirstRun -AdminPassword "change-me-before-first-login"
```

Integration tests may require a reachable SQL Server or LocalDB instance and valid connection settings.

## Before Editing

- Read the nearest existing implementation before adding a new pattern.
- Check `.trae/project_rules.md` and `.trae/rules/` for project rules.
- Use existing skills in `.trae/skills/` when the task matches architecture, login/session, theme switching, WPF UI optimization, module development, quality review, navigation, database, or alarm features.
- If files already contain user changes, work with them and avoid reverting unrelated edits.

## Review Focus

When reviewing or changing this project, pay special attention to:

- startup order and database initialization (`IAppStartupService.InitializeDatabaseAsync` before navigation)
- session identity (`IUserSession`/`IUserInfo`), audit logging (`IAuditLogService`), and permission consistency
- secret handling and local configuration
- module boundaries and navigation registration
- WPF binding performance and UI thread safety (`Dispatcher.Invoke`)
- tests around service behavior, failure paths, and persistence-sensitive logic
- SqlSugar connection error handling (auto lock-screen in AOP)
