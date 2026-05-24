# Security And Configuration Rules

## Secrets

- Never commit real database credentials, customer endpoints, API keys, tokens, certificates, or production passwords.
- Prefer `appsettings.local.json`, environment variables, or local PowerShell scripts for developer-specific values.
- Keep `appsettings.local.example.json` safe and non-secret.

## Database Startup

- Treat startup order as important. Database initialization should complete before first navigation depends on database-backed data.
- Prefer explicit error messages when required configuration is missing.
- Do not hide database connection failures behind silent fallbacks.

## Identity, Audit, And Permissions

- Use the active `IUserSession` or current identity abstraction for audit entries.
- Avoid hard-coded user IDs, usernames, roles, or permission grants in runtime paths.
- Keep menu permissions, database permissions, and permission checker behavior consistent.
- When changing authorization behavior, test both allowed and denied cases.

## Logging

- Use structured logging with meaningful context.
- Do not log secrets, passwords, connection strings, or sensitive personal data.
- Keep log file locations local to the user or application data directories rather than the repository.

## Defaults

- Default administrator credentials must be development-only and clearly documented as something to change before first real use.
- Prefer explicit setup instructions over magic insecure defaults.

