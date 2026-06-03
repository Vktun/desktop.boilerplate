# Security And Configuration Rules

## Secrets

- Never commit real database credentials, customer endpoints, API keys, tokens, certificates, or production passwords.
- Prefer `appsettings.local.json`, environment variables, or local PowerShell scripts for developer-specific values.
- Keep `appsettings.local.example.json` safe and non-secret.
- Do not store session tokens in plain-text configuration files. The `UserSession.Token` is generated at runtime via `RandomNumberGenerator` and held in memory only.

## Database Startup

- Treat startup order as important. Database initialization (`IAppStartupService.InitializeDatabaseAsync`) must complete before first navigation depends on database-backed data.
- The `PrismBootstrapper.InitializeShell` method enforces this order: splash screen → DB init → shell display → initial navigation.
- Prefer explicit error messages when required configuration is missing.
- Do not hide database connection failures behind silent fallbacks.
- SqlSugar connection errors trigger automatic lock-screen in the `ConfigureSqlSugarDb` AOP handler — do not remove this safety behavior.

## Identity, Audit, And Permissions

- Use the active `IUserSession` (or `IUserInfo` for lightweight audit-only needs) for audit entries.
- `IUserInfo` provides `UserId`, `Username`, `IsLoggedIn` — use it in services that only need identity context.
- `IUserSession` extends `IUserInfo` with full session management: `Login()`, `Logout()`, `Lock()`, `Unlock()`, `SetPermissions()`, `HasPermission()`, `Token`.
- Avoid hard-coded user IDs, usernames, roles, or permission grants in runtime paths.
- The admin user automatically has all permissions (`HasPermission` returns `true` for any permission code). This is intentional for development but must not be relied upon in production authorization logic.
- Keep menu permissions (`IMenuPermissionFilter.IsMenuVisible`), database permissions, and permission checker behavior consistent.
- When changing authorization behavior, test both allowed and denied cases.
- Use `IAuditLogService` to record both successful and failed operations. Login failures, for example, log each failure reason (user not found, account disabled, wrong password) separately.

## Logging

- Use structured logging (Serilog) with meaningful context.
- Do not log secrets, passwords, connection strings, or sensitive personal data.
- Keep log file locations local to the user or application data directories (`LocalApplicationData`) rather than the repository.
- Serilog is configured for daily rolling files with 10-day retention in `PrismBootstrapper`.

## Defaults

- Default administrator credentials must be development-only and clearly documented as something to change before first real use.
- Prefer explicit setup instructions over magic insecure defaults.
- The default test user (admin / 123456) is for local development only and must be changed via `start-wpf-local.ps1 -FirstRun -AdminPassword` in any non-development environment.

## Token And Session Security

- Session tokens are generated using `RandomNumberGenerator` (cryptographically secure) in `LoginViewModel`.
- Tokens are held in `IUserSession.Token` (in-memory only) and cleared on `Logout()`.
- Do not persist tokens to disk or configuration files.
- Session timeout monitoring is started after login in `PrismBootstrapper.InitializeShell`.
