# Desktop Boilerplate Project Rules

These rules apply to all AI-assisted work in this repository.

## Project Context

Desktop Boilerplate is a .NET 10 WPF application framework based on Prism, Unity, HandyControl, SqlSugar, Serilog, and xUnit. Treat it as an enterprise desktop shell plus reusable platform services plus Prism business modules.

Primary directories:

- `src/Vk.Dbp.WpfWindow`: host shell, `PrismBootstrapper`, layout, themes, notifications, lock screen, alarms, navigation, and host-level services.
- `src/Vk.Dbp.AdminWindow`: independent administration window with its own Models and ViewModels.
- `src/Vk.Dbp.Contracts`: module contracts, Prism events, extension points, navigation contracts (`INavigationService`), and shared interfaces.
- `src/Vk.Dbp.Domain`: domain abstractions (`DbpDomainModule`). Currently minimal.
- `src/Vk.Dbp.Services`: reusable application services — session (`IUserSession`), settings, audit, alarm, export, caching.
- `src/Vk.Dbp.Infrastructure`: persistence, entities, repositories, SqlSugar setup, and database initialization.
- `src/Vk.Dbp.Utils`: security (SM4, password hashing), ID generation, logging helpers.
- `src/Vk.Dbp.Tools`: standalone tool applications.
- `dbpframework/Vk.Dbp.Core`: framework abstractions (`IDbpModule`, `ServiceCollectionExtensions`).
- `dbpframework/Vk.Dbp.Account`: account primitives (`CurrentUser`, `ICurrentUser`, `PermissionDto`, `RoleDto`).
- `prismModules`: feature modules (`Vk.Dbp.AccountModule`, `Vk.Dbp.WorkshopModule`).
- `dbpApps`: customer application entry projects (`Dbp.Material.Forming`, `Dbp.Material.Mixing`).
- `test`: unit (`Vk.Dbp.Tests.Unit`), integration (`Vk.Dbp.Tests.Integration`), and common (`Vk.Dbp.Tests.Common`) test projects.
- `docs`: project documentation (`LOCAL_CONFIGURATION.md`, `MODULE_DEVELOPMENT_GUIDE.md`, `PROJECT_REVIEW_AND_TODO.md`).
- `scripts`: PowerShell scripts (`start-wpf-local.ps1`, `publish.ps1`).

## Required Behavior

- Answer and document in concise Chinese when the user writes Chinese, but keep code identifiers and committed technical docs in clear English unless an existing file is already Chinese.
- Read the closest existing implementation before adding or changing code.
- Keep edits scoped to the requested feature or fix.
- Do not rewrite generated files, IDE metadata, `bin`, `obj`, logs, or publish artifacts.
- Do not revert existing user changes unless the user explicitly asks.
- Prefer repository conventions over generic framework advice.

## Architecture Rules

- Keep Prism module boundaries clean. A module may expose contracts through `Vk.Dbp.Contracts`, but should not reach into another module implementation.
- Keep shell concerns in `src/Vk.Dbp.WpfWindow`; keep reusable services in `src/Vk.Dbp.Services`; keep persistence details in `src/Vk.Dbp.Infrastructure`.
- Register services through Prism/Unity composition roots, usually module `RegisterTypes` methods or the host bootstrapper.
- Register navigable views with `RegisterForNavigation<TView>()` and use constants for region/view names where they already exist.
- Keep ViewModels independent from concrete Views where possible.
- Use `INavigationService.NavigateTo()` from Contracts layer in ViewModels instead of direct `IRegionManager.RequestNavigate`.
- Cross-module communication must use `IEventAggregator` (`PubSubEvent<T>`), not direct module references.

## Code Conventions

- Constructor-injected dependencies: `_svc = svc ?? throw new ArgumentNullException(nameof(svc))`.
- ViewModels inherit `BindableBase`; use `SetProperty(ref _field, value)`.
- Commands: `DelegateCommand` / `DelegateCommand<T>` with `.ObservesProperty()` for CanExecute refresh.
- ViewModels that subscribe to events must implement `IDisposable` with `_isDisposed` guard.
- UI updates in event callbacks: wrap in `Application.Current.Dispatcher.Invoke()`.

## Configuration And Security

- Do not commit real database credentials or customer secrets.
- Use `src/Vk.Dbp.WpfWindow/appsettings.local.json`, environment variables, or local scripts for developer-specific settings.
- Treat `appsettings.local.example.json` as a template only.
- Prefer current session identity (`IUserSession` or `IUserInfo`) for audit logs and permissions. Avoid hard-coded operator IDs or usernames.
- Use `IAuditLogService` to record both successful and failed operations.

## Testing And Verification

- For non-trivial code changes, add or update xUnit tests with FluentAssertions and Moq where practical.
- Run the most focused test project first, then broaden to solution build when risk warrants it.
- Use these default commands:

```powershell
dotnet build desktop.boilerplate.slnx
dotnet test test\Vk.Dbp.Tests.Unit\Vk.Dbp.Tests.Unit.csproj
```

## Skill Index

Use these project skills when applicable:

- `.trae/skills/project-architecture`: project structure, layers, Prism module pattern.
- `.trae/skills/dbp-module-development`: creating or changing Prism modules, views, ViewModels, services, navigation, and tests.
- `.trae/skills/login-session-system`: login, user session, logout, lock screen, and authentication-related changes.
- `.trae/skills/theme-switch`: theme switching and theme persistence.
- `.trae/skills/wpf-ui-optimization`: WPF layout, binding, rendering, and resource performance.
- `.trae/skills/dbp-quality-review`: startup, security, configuration, permission, audit, and platform-quality review.
- `.trae/skills/navigation-routing`: region navigation, view routing, NavigationParameters, and navigation constants.
- `.trae/skills/database-repository`: SqlSugar configuration, entities, repositories, and database initialization.
- `.trae/skills/alarm-notification`: alarm management, Prism events for alarms, and notification display.

For more detailed rule files, also read `.trae/rules/`.
