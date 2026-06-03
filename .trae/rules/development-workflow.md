# Development Workflow Rules

## Investigation First

- Start every task by reading the closest existing files, relevant docs, and current git status.
- Use `rg` or `rg --files` for search.
- Prefer small, targeted edits over broad rewrites.
- Keep unrelated formatting changes out of feature and bug-fix work.
- Before adding a new service, check whether an existing service in `Vk.Dbp.Services` or `Vk.Dbp.Contracts` already covers the responsibility.

## Implementation

- Follow existing style in the touched project: namespace style, constructor style, nullability, and registration pattern.
- For public interfaces and shared contracts, choose names that reflect domain concepts rather than implementation details.
- When adding services, add interfaces only when they create a real testing or boundary benefit.
- Avoid duplicating permission, menu, session, or audit concepts. Check existing services before adding new ones.

### Constructor And Dependency Injection

- All constructor-injected dependencies must have null-guard: `_svc = svc ?? throw new ArgumentNullException(nameof(svc))`.
- Prefer constructor injection over property injection or service locator, except for services registered in modules that may not be loaded yet (use `_container.Resolve<T>()` lazily in that case).
- Register services in the appropriate composition root:
  - Global services: `PrismBootstrapper.RegisterTypes` with `RegisterSingleton`.
  - Module services: `Dbp{Name}Module.RegisterTypes` with `RegisterSingleton` or `Register`.
  - Navigable views: `RegisterForNavigation<TView>()` in the module's `RegisterTypes`.

### ViewModel Patterns

- Inherit `BindableBase`; use `SetProperty(ref _field, value)` for bindable properties.
- Use `DelegateCommand` / `DelegateCommand<T>` for commands; chain `.ObservesProperty(() => Prop)` when `CanExecute` depends on bindable properties.
- ViewModels that subscribe to `IEventAggregator` or `INotifyPropertyChanged` must implement `IDisposable` with an `_isDisposed` guard and unsubscribe in `Dispose()`.
- In event callbacks that update UI state, wrap updates in `Application.Current.Dispatcher.Invoke()`.
- For `INavigationAware` ViewModels, use `OnNavigatedTo` for initialization and `OnNavigatedFrom` for cleanup.

### Navigation

- Use `INavigationService.NavigateTo(viewName)` from `Vk.Dbp.Contracts` in ViewModels, not `IRegionManager.RequestNavigate` directly.
- Add view-name constants to `NavigationConstants.ViewNames` for new navigable views.
- Pass parameters via `NavigationParameters`.

## Tests

- Unit tests use xUnit, Moq, and FluentAssertions.
- Put shared test builders and fixtures under `test/Vk.Dbp.Tests.Common`.
- Prefer focused tests for service logic, permission filtering, session behavior, password hashing, and configuration validation.
- Integration tests may need SQL Server or LocalDB setup; do not assume they can run in every environment.
- The integration test project (`Vk.Dbp.Tests.Integration`) currently has no tests; add integration tests there when needed.

## Verification

Default verification order:

```powershell
dotnet test test\Vk.Dbp.Tests.Unit\Vk.Dbp.Tests.Unit.csproj
dotnet build desktop.boilerplate.slnx
```

When changing only markdown, rules, skills, or agent prompts, validate with:

```powershell
git diff --check
```

## Git Hygiene

- Do not revert user changes.
- Do not commit local secrets, `appsettings.local.json`, logs, `bin`, `obj`, or publish output.
- Mention any pre-existing dirty files in the final handoff when they are visible in `git status`.
