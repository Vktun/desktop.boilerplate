---
name: "database-repository"
description: "Handles SqlSugar database configuration, entity definitions, repository patterns, and database initialization in the Desktop Boilerplate WPF/Prism repository. Invoke when adding entities, repositories, database migrations, or modifying SqlSugar configuration."
---

# Database Repository Skill

## Overview

This skill covers the database layer in Desktop Boilerplate, which uses SqlSugar ORM with a `SqlSugarScope` singleton, repository pattern, and database initialization via `IAppStartupService`.

## Core Components

### SqlSugar Configuration

Configured in `PrismBootstrapper.ConfigureSqlSugarDb`:

```csharp
var db = new SqlSugarScope(new ConnectionConfig
{
    ConnectionString = connectionString,
    DbType = DbType.SqlServer,
    IsAutoCloseConnection = true,
    InitKeyType = InitKeyType.Attribute,
}, db =>
{
    // SQL logging
    db.Aop.OnLogExecuting = (sql, pars) => { ... };

    // Connection error → auto lock-screen
    db.Aop.OnError = (exp) =>
    {
        // Triggers lock screen on connection failure
    };
});

containerRegistry.RegisterInstance<ISqlSugarClient>(db);
```

Key behaviors:
- `SqlSugarScope` is registered as singleton — thread-safe for concurrent use.
- SQL logging captures all queries for debugging.
- Connection errors automatically trigger the lock screen via AOP handler.

### IAppStartupService — Database Initialization

```csharp
public interface IAppStartupService
{
    Task InitializeDatabaseAsync();
}
```

Called in `PrismBootstrapper.InitializeShell` before any navigation:

```
Splash screen → IAppStartupService.InitializeDatabaseAsync() → Shell display → Initial navigation
```

This ensures the database is ready before any DB-backed UI or service is accessed.

### Entity Definitions

Entities are defined in `src/Vk.Dbp.Infrastructure/Entities/` (namespace `Dabp.Infrastructure.Entities`) as plain POCOs combining SqlSugar attributes with DataAnnotations, with Chinese XML docs and ABP-style audit columns (`CreationTime`, `CreatorId`, `LastModificationTime`, `LastModifierId`, `IsDeleted`, ...):

```csharp
public class User
{
    [Key]
    [SugarColumn(IsPrimaryKey = true, IsIdentity = true)]
    public int Id { get; set; }

    [SugarColumn(IsNullable = true, ColumnDataType = "nvarchar")]
    [StringLength(50)]
    public string? UserName { get; set; }

    // ...
}
```

Soft delete is manual — every query adds `.Where(u => !u.IsDeleted)`.

### Repository Pattern

Repositories are in `src/Vk.Dbp.Infrastructure/Repositories/` (`IRepository<T>` + `SqlSugarRepository<T>`, `where T : class, new()`), an async wrapper including `GetByIdAsync`, `GetListAsync(expr)`, `InsertAsync`, and `GetPageListAsync` returning a `(List<T> list, int total)` tuple via `RefAsync<int>`.

Registered in `PrismBootstrapper.RegisterTypes` as an open generic:

```csharp
containerRegistry.Register(typeof(IRepository<>), typeof(SqlSugarRepository<>));
```

In practice most services bypass the repository and inject `ISqlSugarClient` directly:

```csharp
public class UserService : IUserService
{
    private readonly ISqlSugarClient _db;

    public UserService(ISqlSugarClient db)
    {
        _db = db ?? throw new ArgumentNullException(nameof(db));
    }

    public async Task<UserEntity?> GetByUsernameAsync(string username)
    {
        return await _db.Queryable<UserEntity>()
            .Where(u => !u.IsDeleted && u.UserName == username)
            .FirstAsync();
    }
}
```

## Adding a New Entity

1. Create entity class in `src/Vk.Dbp.Infrastructure/Entities/` with `[SugarTable]` and `[SugarColumn]` attributes.
2. Update `IAppStartupService.InitializeDatabaseAsync` to include the new entity in the `InitTables` call if using code-first.
3. Create a service interface in `src/Vk.Dbp.Contracts/Services/` if the entity needs cross-module access.
4. Create a service implementation in the appropriate project (Infrastructure for generic, module for specific).
5. Register the service in the module's `RegisterTypes` or `PrismBootstrapper.RegisterTypes`.

## Adding a New Repository

1. For generic CRUD, `IRepository<T>` is already registered globally — inject it directly.
2. For specialized queries, create a service that injects `ISqlSugarClient` (the prevailing pattern in this repo).
3. Do NOT inject `ISqlSugarClient` directly into ViewModels; always use a service boundary.

## Database Initialization Flow

```
PrismBootstrapper.InitializeShell
    ↓
Splash screen shown
    ↓
IAppStartupService.InitializeDatabaseAsync()
    ↓
SqlSugar CodeFirst creates/migrates tables
    ↓
Seed data (admin user, default roles, permissions)
    ↓
Shell displayed
    ↓
Initial navigation (Login or Dashboard based on session)
```

## Key Files

| Component | Location |
|-----------|----------|
| SqlSugar config | `src/Vk.Dbp.WpfWindow/PrismBootstrapper.cs` (ConfigureSqlSugarDb) |
| IAppStartupService / AppStartupService | `src/Vk.Dbp.WpfWindow/Services/` |
| DatabaseInitializer (CodeFirst + seeds) | `src/Vk.Dbp.Infrastructure/DatabaseInitializer.cs` |
| Entities | `src/Vk.Dbp.Infrastructure/Entities/` |
| Repositories (`IRepository<T>` / `SqlSugarRepository<T>`) | `src/Vk.Dbp.Infrastructure/Repositories/` |
| ORM settings (`SqlSugarFluentService`) | `src/Vk.Dbp.Infrastructure/OrmSetting/` |
| Connection string | `src/Vk.Dbp.WpfWindow/appsettings.json` / `appsettings.local.json` |

## Common Issues

### Issue: "Table does not exist" at runtime
**Solution**: Ensure the entity is included in `InitTables` in `IAppStartupService.InitializeDatabaseAsync`.

### Issue: Connection failure causes app crash
**Solution**: The SqlSugar AOP handler should auto-lock the screen. If it doesn't, check `ConfigureSqlSugarDb` error handler.

### Issue: ViewModel directly uses ISqlSugarClient
**Solution**: Extract a service interface in Contracts, implement in Infrastructure or the module, and inject the service instead.

### Issue: Seed data not applied
**Solution**: Check `IAppStartupService.InitializeDatabaseAsync` for the seed data logic and ensure it runs after table creation.
