---
name: "project-architecture"
description: "Provides project architecture guidance for WPF/Prism desktop applications. Invoke when user needs to understand project structure, add new modules, or follow architecture patterns."
---

# Project Architecture Skill

## Overview

This skill provides architectural guidance for the Desktop Boilerplate (Dbp) project — a modern WPF enterprise desktop application framework targeting .NET 10 with Prism, Unity, HandyControl, SqlSugar, and Serilog.

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
│   ├── Vk.Dbp.WpfWindow/             # Main WPF shell & bootstrapper
│   ├── Vk.Dbp.AdminWindow/           # Independent admin window
│   ├── Vk.Dbp.Contracts/             # Cross-module contracts & events
│   ├── Vk.Dbp.Domain/                # Domain abstractions
│   ├── Vk.Dbp.Services/              # Shared application services
│   ├── Vk.Dbp.Infrastructure/        # Persistence & SqlSugar setup
│   ├── Vk.Dbp.Utils/                 # Security, ID generation, logging
│   └── Vk.Dbp.Tools/                 # Standalone tool applications
├── test/                             # Test projects
│   ├── Vk.Dbp.Tests.Unit/            # Unit tests (xUnit + Moq + FluentAssertions)
│   ├── Vk.Dbp.Tests.Integration/     # Integration tests (SQL Server required)
│   └── Vk.Dbp.Tests.Common/          # Shared test fixtures & factories
├── docs/                             # Project documentation
└── scripts/                          # PowerShell scripts (start-wpf-local, publish)
```

## Layered Architecture

```
┌─────────────────────────────────────────────────┐
│              Presentation Layer                  │
│  WpfWindow, AdminWindow, prismModules           │
│  (Views, ViewModels, Converters, Commands)      │
├─────────────────────────────────────────────────┤
│              Application Layer                   │
│  Vk.Dbp.Services, Vk.Dbp.Contracts              │
│  (Session, Audit, Settings, Navigation, Events) │
├─────────────────────────────────────────────────┤
│               Domain Layer                       │
│  Vk.Dbp.Domain, dbpframework/Vk.Dbp.Account     │
│  (Entities, Domain Logic, Account Primitives)   │
├─────────────────────────────────────────────────┤
│           Infrastructure Layer                   │
│  Vk.Dbp.Infrastructure, Vk.Dbp.Utils            │
│  (SqlSugar, Repositories, Encryption, Logging)  │
└─────────────────────────────────────────────────┘
```

## Dependency Rules

- Dependencies flow inward: Presentation → Application → Domain → Infrastructure.
- `dbpframework` is the innermost layer; must not depend on `src/` or `prismModules/`.
- Business modules must not depend on each other directly; use `Vk.Dbp.Contracts` for shared contracts.
- ViewModels must not directly access SqlSugar or repository types; use service boundaries.

## Tech Stack

| Component | Technology | Purpose |
|-----------|------------|---------|
| UI Framework | WPF (.NET 10) | Desktop UI |
| MVVM Framework | Prism.Wpf | MVVM pattern, Navigation, Modules |
| UI Components | HandyControl | Modern UI controls, themes |
| ORM | SqlSugar | Database access, `SqlSugarScope` singleton |
| Logging | Serilog | Structured logging, daily rolling files |
| DI Container | Unity (via Prism) | Dependency injection |
| Weaving | PropertyChanged.Fody | Auto INotifyPropertyChanged |
| Testing | xUnit + Moq + FluentAssertions | Unit and integration tests |

## Core Design Patterns

### 1. MVVM Pattern

```csharp
// ViewModel
public class SampleViewModel : BindableBase
{
    private string _title;
    public string Title
    {
        get => _title;
        set => SetProperty(ref _title, value);
    }

    public DelegateCommand SaveCommand { get; }

    public SampleViewModel(IService service)
    {
        _service = service ?? throw new ArgumentNullException(nameof(service));
        SaveCommand = new DelegateCommand(OnSave, CanSave)
            .ObservesProperty(() => Title);
    }
}

// View — ViewModelLocator auto-wires ViewModel
<UserControl prism:ViewModelLocator.AutoWireViewModel="True">
    <TextBox Text="{Binding Title, Mode=TwoWay}"/>
    <Button Command="{Binding SaveCommand}"/>
</UserControl>
```

### 2. Module Pattern

```csharp
public class DbpAccountModule : IModule
{
    public void OnInitialized(IContainerProvider containerProvider) { }

    public void RegisterTypes(IContainerRegistry containerRegistry)
    {
        // Services
        containerRegistry.RegisterSingleton<IUserService, UserService>();
        containerRegistry.RegisterSingleton<IRoleService, RoleService>();

        // Views — ViewModelLocator resolves ViewModel by convention
        containerRegistry.RegisterForNavigation<LoginView>();
        containerRegistry.RegisterForNavigation<UserManagementView>();
    }
}
```

### 3. Region Navigation (via INavigationService)

```csharp
// Contracts layer defines the interface
public interface INavigationService
{
    void NavigateTo(string viewName, NavigationParameters parameters = null);
    event Action<NavigationResult> NavigationCompleted;
}

// ViewModels use INavigationService, not IRegionManager directly
_navigationService.NavigateTo(ViewNames.Dashboard);
```

### 4. Cross-Module Events

```csharp
// Define event in Contracts
public class AlarmTriggeredEvent : PubSubEvent<AlarmTriggeredEventArgs> { }

// Publish from any module
_eventAggregator.GetEvent<AlarmTriggeredEvent>().Publish(args);

// Subscribe in another module
_eventAggregator.GetEvent<AlarmTriggeredEvent>().Subscribe(OnAlarmTriggered);
```

## Adding a New Module

### Step 1: Create Module Project

```xml
<Project Sdk="Microsoft.NET.Sdk">
  <PropertyGroup>
    <TargetFramework>net10.0-windows</TargetFramework>
    <UseWPF>true</UseWPF>
  </PropertyGroup>
  <ItemGroup>
    <PackageReference Include="Prism.Wpf" Version="*" />
  </ItemGroup>
</Project>
```

### Step 2: Create Module Class

```csharp
public class DbpNewModule : IModule
{
    public void RegisterTypes(IContainerRegistry containerRegistry)
    {
        containerRegistry.RegisterSingleton<INewService, NewService>();
        containerRegistry.RegisterForNavigation<NewView>();
    }

    public void OnInitialized(IContainerProvider containerProvider) { }
}
```

### Step 3: Register in Bootstrapper

```csharp
protected override void ConfigureModuleCatalog(IModuleCatalog moduleCatalog)
{
    moduleCatalog.AddModule<DbpNewModule>();
}
```

### Step 4 (Optional): Register Non-Convention ViewModel Mapping

```csharp
protected override void ConfigureViewModelLocator()
{
    base.ConfigureViewModelLocator();
    ViewModelLocationProvider.Register<NewView, CustomViewModel>();
}
```

## Naming Conventions

| Type | Convention | Example |
|------|------------|---------|
| Module class | `Dbp{Name}Module` | `DbpAccountModule` |
| View | `{Name}View.xaml` | `LoginView.xaml` |
| ViewModel | `{Name}ViewModel.cs` | `LoginViewModel.cs` |
| Service interface | `I{Name}Service` | `IUserService` |
| Service implementation | `{Name}Service` | `UserService` |
| Model | `{Name}.cs` | `User.cs` |
| Navigation constant | `ViewNames.{Name}` | `ViewNames.Dashboard` |
| Prism event | `{Name}Event` | `AlarmTriggeredEvent` |

## Key Files Reference

| Purpose | File |
|---------|------|
| Bootstrapper | `src/Vk.Dbp.WpfWindow/PrismBootstrapper.cs` |
| Navigation constants | `src/Vk.Dbp.WpfWindow/Constants/NavigationConstants.cs` |
| Navigation service | `src/Vk.Dbp.Contracts/Services/INavigationService.cs` |
| Navigation implementation | `src/Vk.Dbp.WpfWindow/Services/PrismNavigationService.cs` |
| Main window | `src/Vk.Dbp.WpfWindow/MainWindow.xaml` |
| Header layout | `src/Vk.Dbp.WpfWindow/Layout/HeaderView.xaml` |
| Module registration | `prismModules/Vk.Dbp.AccountModule/DbpAccountModule.cs` |
| Session | `src/Vk.Dbp.Services/Session/UserSession.cs` |
| Theme service | `src/Vk.Dbp.WpfWindow/Services/ThemeService.cs` |
