# Desktop Boilerplate Project Rules

These rules apply to all AI-assisted work in this repository.

## Project Context

Desktop Boilerplate is a .NET 10 WPF application framework based on Prism, Unity, HandyControl, SqlSugar, Serilog, and xUnit. Treat it as an enterprise desktop shell plus reusable platform services plus Prism business modules.

Primary directories:

- `src/Vk.Dbp.WpfWindow`: host shell and startup composition.
- `src/Vk.Dbp.Contracts`: module contracts, events, extension points, and shared interfaces.
- `src/Vk.Dbp.Services`: reusable application services.
- `src/Vk.Dbp.Infrastructure`: persistence, entities, repositories, and database initialization.
- `prismModules`: feature modules.
- `dbpApps`: customer application entry projects.
- `test`: unit, integration, and common test projects.

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
- Register navigable views explicitly and use constants for region/view names where they already exist.
- Keep ViewModels independent from concrete Views where possible.

## Configuration And Security

- Do not commit real database credentials or customer secrets.
- Use `src/Vk.Dbp.WpfWindow/appsettings.local.json`, environment variables, or local scripts for developer-specific settings.
- Treat `appsettings.local.example.json` as a template only.
- Prefer current session identity for audit logs and permissions. Avoid hard-coded operator IDs or usernames.

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

For more detailed rule files, also read `.trae/rules/`.

