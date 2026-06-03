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

Entities are defined in `src/Vk.Dbp.Infrastructure/Entities/` with SqlSugar attributes:

```csharp
[SugarTable("SysUsers")]
public class SysUser
{
    [SugarColumn(IsPrimaryKey = true, IsIdentity = true)]
    public int Id { get; set; }

    [SugarColumn(Length = 50)]
    public string Username { get; set; }

    // ...
}
```

### Repository Pattern

Repositories are in `src/Vk.Dbp.Infrastructure/Repositories/`:

```csharp
public class GenericRepository<T> where T : class, new()
{
    protected readonly ISqlSugarClient _db;

    public GenericRepository(ISqlSugarClient db)
    {
        _db = db ?? throw new ArgumentNullException(nameof(db));
    }

    public T GetById(object id) => _db.Queryable<T>().InSingle(id);
    public List<T> GetAll() => _db.Queryable<T>().ToList();
    public int Insert(T entity) => _db.Insertable(entity).ExecuteCommand();
    public int Update(T entity) => _db.Updateable(entity).ExecuteCommand();
    public int Delete(object id) => _db.Deleteable<T>().In(id).ExecuteCommand();
}
```

Registered in `PrismBootstrapper.RegisterTypes`:

```csharp
containerRegistry.Register(typeof(IGenericRepository<>), typeof(GenericRepository<>));
```

### Module-Specific Repositories

Modules may define specialized repositories in their own `Services/` directory:

```csharp
public class UserService : IUserService
{
    private readonly ISqlSugarClient _db;

    public UserService(ISqlSugarClient db)
    {
        _db = db ?? throw new ArgumentNullException(nameof(db));
    }

    public User GetByUsername(string username)
    {
        return _db.Queryable<SysUser>()
            .Where(u => u.Username == username)
            .First();
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

1. For generic CRUD, use `IGenericRepository<T>` — already registered globally.
2. For specialized queries, create a service that injects `ISqlSugarClient`.
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
| IAppStartupService | `src/Vk.Dbp.Contracts/Services/IAppStartupService.cs` |
| AppStartupService | `src/Vk.Dbp.Infrastructure/` |
| Entities | `src/Vk.Dbp.Infrastructure/Entities/` |
| Repositories | `src/Vk.Dbp.Infrastructure/Repositories/` |
| ORM settings | `src/Vk.Dbp.Infrastructure/OrmSetting/` |
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
