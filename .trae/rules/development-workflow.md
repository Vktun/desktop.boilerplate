# Development Workflow Rules

## Investigation First

- Start every task by reading the closest existing files, relevant docs, and current git status.
- Use `rg` or `rg --files` for search.
- Prefer small, targeted edits over broad rewrites.
- Keep unrelated formatting changes out of feature and bug-fix work.

## Implementation

- Follow existing style in the touched project: namespace style, constructor style, nullability, and registration pattern.
- For public interfaces and shared contracts, choose names that reflect domain concepts rather than implementation details.
- When adding services, add interfaces only when they create a real testing or boundary benefit.
- Avoid duplicating permission, menu, session, or audit concepts. Check existing services before adding new ones.

## Tests

- Unit tests use xUnit, Moq, and FluentAssertions.
- Put shared test builders and fixtures under `test/Vk.Dbp.Tests.Common`.
- Prefer focused tests for service logic, permission filtering, session behavior, password hashing, and configuration validation.
- Integration tests may need SQL Server or LocalDB setup; do not assume they can run in every environment.

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

