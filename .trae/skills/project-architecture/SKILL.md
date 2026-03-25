---
name: "project-architecture"
description: "Provides project architecture guidance for WPF/Prism desktop applications. Invoke when user needs to understand project structure, add new modules, or follow architecture patterns."
---

# Project Architecture Skill

## Overview

This skill provides architectural guidance for the Desktop Boilerplate (Dabp) project - a modern WPF enterprise desktop application framework.

## Project Structure

```
desktop.boilerplate/
├── dbpApps/                          # Application projects
│   ├── Dbp.Material.Forming/         # Material forming app
│   └── Dbp.Material.Mixing/          # Material mixing app
├── dbpframework/                     # Core framework
│   ├── Vk.Dbp.Account/               # Account domain
│   └── Vk.Dbp.Core/                  # Core abstractions
├── prismModules/                     # Prism modules
│   ├── Vk.Dbp.AccountModule/         # Account management module
│   └── Vk.Dbp.WorkshopModule/        # Workshop module
└── src/                              # Source libraries
    ├── Vk.Dbp.Domain/                # Domain layer
    ├── Vk.Dbp.Infrastructure/        # Infrastructure layer
    ├── Vk.Dbp.Services/              # Service layer
    ├── Vk.Dbp.Tools/                 # Tools application
    ├── Vk.Dbp.Utils/                 # Utilities
    └── Vk.Dbp.WpfWindow/             # Main WPF window
```

## Layered Architecture

```
┌─────────────────────────────────────────────┐
│              Presentation Layer             │
│  (Views, ViewModels, Converters, Commands)  │
├─────────────────────────────────────────────┤
│              Application Layer              │
│     (Services, Module Registration)         │
├─────────────────────────────────────────────┤
│               Domain Layer                  │
│        (Entities, Interfaces, Logic)        │
├─────────────────────────────────────────────┤
│           Infrastructure Layer              │
│   (Data Access, External Services, ORM)     │
└─────────────────────────────────────────────┘
```

## Tech Stack

| Component | Technology | Purpose |
|-----------|------------|---------|
| UI Framework | WPF (.NET 10) | Desktop UI |
| MVVM Framework | Prism.Wpf | MVVM pattern, Navigation |
| UI Components | HandyControl | Modern UI controls |
| ORM | SqlSugar | Database access |
| Logging | Serilog | Logging |
| DI Container | Unity (via Prism) | Dependency injection |

## Core Design Patterns

### 1. MVVM Pattern

```csharp
// Model
public class User : BindableBase
{
    private string _username;
    public string Username
    {
        get => _username;
        set => SetProperty(ref _username, value);
    }
}

// ViewModel
public class UserViewModel : BindableBase
{
    private readonly IUserService _userService;
    
    public DelegateCommand SaveCommand { get; }
    
    public UserViewModel(IUserService userService)
    {
        _userService = userService;
        SaveCommand = new DelegateCommand(OnSave);
    }
}

// View (XAML)
<UserControl prism:ViewModelLocator.AutoWireViewModel="True">
    <TextBox Text="{Binding Username, Mode=TwoWay}"/>
    <Button Command="{Binding SaveCommand}"/>
</UserControl>
```

### 2. Module Pattern

```csharp
public class DbpAccountModule : IModule
{
    public void OnInitialized(IContainerProvider containerProvider)
    {
        // Module initialization
    }
    
    public void RegisterTypes(IContainerRegistry containerRegistry)
    {
        // Register services and views
        containerRegistry.Register<IUserService, UserService>();
        containerRegistry.RegisterForNavigation<LoginView>();
    }
}
```

### 3. Region Navigation

```csharp
// Register region in MainWindow.xaml
<ContentControl prism:RegionManager.RegionName="ContentRegion"/>

// Navigate to view
_regionManager.RequestNavigate("ContentRegion", "DashboardView");
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
public class NewModule : IModule
{
    public void RegisterTypes(IContainerRegistry containerRegistry)
    {
        containerRegistry.Register<INewService, NewService>();
        containerRegistry.RegisterForNavigation<NewView>();
    }
    
    public void OnInitialized(IContainerProvider containerProvider)
    {
        var regionManager = containerProvider.Resolve<IRegionManager>();
        regionManager.RegisterViewWithRegion("ContentRegion", typeof(NewView));
    }
}
```

### Step 3: Register in Bootstrapper

```csharp
protected override void ConfigureModuleCatalog(IModuleCatalog moduleCatalog)
{
    moduleCatalog.AddModule<NewModule>();
}
```

## Naming Conventions

| Type | Convention | Example |
|------|------------|---------|
| View | XxxView.xaml | LoginView.xaml |
| ViewModel | XxxViewModel.cs | LoginViewModel.cs |
| Service | IXxxService / XxxService | IUserService / UserService |
| Model | Xxx.cs | User.cs |
| Module | XxxModule.cs | AccountModule.cs |

## Key Files Reference

| Purpose | File |
|---------|------|
| App startup | `App.xaml.cs` |
| Prism config | `PrismBootstrapper.cs` |
| Main window | `MainWindow.xaml` |
| Header layout | `Layout/HeaderView.xaml` |
| Module registration | `DbpAccountModule.cs` |
