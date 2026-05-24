---
name: "dbp-module-development"
description: "Use for creating, extending, or reviewing Desktop Boilerplate Prism modules, views, ViewModels, services, navigation entries, module registration, and related tests. Invoke whenever the task mentions adding a feature module, screen, menu item, ViewModel, service, navigation route, or secondary-development extension in this WPF/Prism repository."
---

# DBP Module Development Skill

## Purpose

Use this skill to implement feature work inside the Desktop Boilerplate module system without breaking the host shell, shared contracts, or Prism navigation conventions.

## First Reads

Before editing, read the closest relevant files:

- `.trae/project_rules.md`
- `.trae/rules/architecture.md`
- `docs/MODULE_DEVELOPMENT_GUIDE.md`
- `src/Vk.Dbp.WpfWindow/PrismBootstrapper.cs`
- `src/Vk.Dbp.WpfWindow/Constants/NavigationConstants.cs`
- an existing module entry file such as `prismModules/Vk.Dbp.AccountModule/DbpAccountModule.cs`
- the nearest existing View, ViewModel, and service that resemble the requested feature

## Module Placement

Choose the target location by responsibility:

- New reusable feature module: `prismModules/Vk.Dbp.<Feature>Module`
- New host shell behavior: `src/Vk.Dbp.WpfWindow`
- Shared service contract: `src/Vk.Dbp.Contracts/Services`
- Shared service implementation: `src/Vk.Dbp.Services`
- Persistence entity or repository behavior: `src/Vk.Dbp.Infrastructure`
- Customer-specific entry-point behavior: `dbpApps/<AppName>`

Do not put customer-specific business logic in the reusable shell unless the user explicitly asks for a host-level feature.

## Standard Module Shape

Prefer this structure for a new module:

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

## Implementation Checklist

1. Define the feature boundary.
   - Identify whether it belongs in a module, shared service, host shell, or customer app.
   - Identify contracts needed by other projects before implementation details.

2. Add or update project references.
   - WPF modules usually target `net10.0-windows` with `UseWPF=true`.
   - Reuse Prism, HandyControl, and existing project references rather than adding duplicate packages.

3. Register services and views.
   - Use `IContainerRegistry` in the module's `RegisterTypes`.
   - Use `RegisterForNavigation<TView>()` for navigable views.
   - Use singleton lifetimes only for shared stateful services that are safe to share.

4. Add navigation.
   - Use existing region and view-name constants when available.
   - Update menu or permission configuration only when the feature requires it.
   - Keep database permission seed data and static menu permission mappings from drifting apart.

5. Keep ViewModels testable.
   - Put user actions in commands.
   - Inject services through constructors.
   - Avoid direct database calls from ViewModels when a service boundary already exists.

6. Add focused tests.
   - Use xUnit, Moq, and FluentAssertions.
   - Prefer service and ViewModel tests for behavior.
   - Put shared fixtures or factories in `test/Vk.Dbp.Tests.Common`.

## Verification

For most module changes, run:

```powershell
dotnet test test\Vk.Dbp.Tests.Unit\Vk.Dbp.Tests.Unit.csproj
dotnet build desktop.boilerplate.slnx
```

If the change touches persistence or SQL Server initialization, also evaluate whether integration tests or a manual startup through `scripts/start-wpf-local.ps1` are needed.

## Handoff Notes

In the final response, mention:

- the module, View, ViewModel, service, and test files changed
- any manual database or configuration step the user must know
- which verification commands ran and whether anything could not be run

