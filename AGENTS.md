# Desktop Boilerplate Agent Guide

## Project Snapshot

This repository is a WPF enterprise desktop application boilerplate for Dabp/Dbp-style systems. It uses .NET 10, Prism, Unity, HandyControl, SqlSugar, Serilog, Fody, and xUnit-based tests.

The codebase is organized around a reusable host shell, shared contracts and services, Prism business modules, framework packages, and customer application entry points:

- `src/Vk.Dbp.WpfWindow`: main WPF shell, Prism bootstrapper, layout, themes, startup, lock screen, notifications, alarms, navigation, and host-level services.
- `src/Vk.Dbp.Contracts`: cross-module contracts, events, extension points, paging/cache abstractions, and service interfaces.
- `src/Vk.Dbp.Services`: reusable application services such as session, settings, audit, alarm, export, and caching.
- `src/Vk.Dbp.Infrastructure`: entities, repositories, SqlSugar setup, and database initialization.
- `src/Vk.Dbp.Utils`: security, logging, ID generation, and algorithm utilities.
- `prismModules`: independent Prism feature modules such as account management and workshop examples.
- `dbpframework`: framework-level abstractions and account primitives.
- `dbpApps`: customer/project-specific WPF application entry points.
- `test`: xUnit, Moq, and FluentAssertions test projects.

## Default Working Rules

- Preserve the layered architecture. Modules should depend on contracts and shared services instead of directly depending on other module implementations.
- Keep host-only behavior in `Vk.Dbp.WpfWindow`; avoid pushing customer-specific logic into the shell.
- Put shared extension contracts in `Vk.Dbp.Contracts` before using them from multiple modules.
- Put persistence entities and repository details in `Vk.Dbp.Infrastructure`; keep UI/ViewModel code away from SqlSugar-specific details when practical.
- Prefer Prism conventions already used in the repository: `IModule`, `IContainerRegistry`, region navigation, `BindableBase`, `DelegateCommand`, and view registration.
- For WPF UI, keep ViewModels testable. Avoid business logic in XAML code-behind except view-only behavior.
- Use `appsettings.local.json` or environment variables for local secrets. Do not commit real connection strings, passwords, tokens, or customer endpoints.
- Do not modify `bin`, `obj`, logs, publish output, or local IDE files.

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
- Use existing skills in `.trae/skills/` when the task matches architecture, login/session, theme switching, WPF UI optimization, module development, or quality review.
- If files already contain user changes, work with them and avoid reverting unrelated edits.

## Review Focus

When reviewing or changing this project, pay special attention to:

- startup order and database initialization
- session identity, audit logging, and permission consistency
- secret handling and local configuration
- module boundaries and navigation registration
- WPF binding performance and UI thread safety
- tests around service behavior, failure paths, and persistence-sensitive logic

