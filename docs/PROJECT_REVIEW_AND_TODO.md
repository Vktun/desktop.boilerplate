# Desktop Boilerplate Review And TODO

Review date: 2026-04-02

## 1. Current project status

This repository already has the basic shape of a desktop application platform:

- WPF + Prism + HandyControl shell
- Prism modularization (`AccountModule`, `WorkshopModule`)
- SqlSugar-based persistence
- Basic user / role / permission / organization / audit concepts
- Theme switching and a small amount of reusable UI shell code
- Example business applications under `dbpApps`

From an engineering perspective, it is currently closer to:

`desktop management demo + industrial UI shell prototype`

It is not yet a complete upper-computer platform for secondary development.

## 2. What is already usable

### 2.1 Good foundation

- The shell, regions, and module registration are already separated.
- The repository structure has been split into `src`, `prismModules`, `dbpframework`, and `dbpApps`.
- User / role / permission / organization concepts are already present.
- The solution can build successfully on 2026-04-02. Current status: `0 errors`, `259 warnings`.

### 2.2 Useful for future extension

- `Vk.Dbp.WpfWindow` can continue to evolve into a reusable host shell.
- `AccountModule` can continue to evolve into a common account center.
- `dbpApps` can be used as customer/project-specific entry applications.

## 3. Major gaps blocking upper-computer reuse

### P0: Platform capability is not complete yet

- The current `WorkshopModule` is still mostly demo content and does not yet provide device abstraction, protocol abstraction, real-time data collection, alarm engine, historian, report center, or engineering configuration.
- Audit logging is implemented as in-memory storage in `Vk.Dbp.Core.Audit.Services.AuditLogService`, so logs are lost after restart.
- Login/session is in-memory only. There is no persistent session, token refresh, auto-login, or "remember me" implementation.

### P1: Architecture is not stable enough for long-term reuse

- Database initialization is triggered asynchronously during shell startup without being awaited. Startup order is unsafe.
- Menu permissions are maintained in two places: database permissions and `MenuPermissionConfig`. This is easy to drift.
- Many service methods write audit logs with hard-coded user identity (`1`, `admin`) instead of the current session identity.
- `src/Vk.Dbp.Services` is still empty and has not become a real application service layer.

### P1: Security and deployment need strengthening

- The repository contains a plain-text database connection string and password in `appsettings.json`.
- Weak default passwords are still present in initialization / reset flows.
- Theme persistence currently uses `Application.Current.Properties`, which does not solve real settings persistence across restarts.

### P2: Engineering quality needs cleanup

- Nullable warnings are large in number.
- Test projects are empty.
- There is no real build/test CI workflow, only repository mirroring.
- `bin/obj` artifacts and logs have been committed before; `.gitignore` is not preventing repository pollution effectively enough.

## 4. Competitor comparison summary

Typical mature upper-computer / SCADA competitors usually provide several capabilities out of the box:

- Device/protocol access: Modbus, OPC UA, MQTT, serial, PLC drivers
- Real-time tag model: tag browsing, subscriptions, write-back, quality state
- Alarm/event center: acknowledgement, shelving, history, notification
- Historian/trend/reporting: trend playback, aggregation, export, scheduled reports
- Engineering configuration: driver config, screen config, alarm config, recipe/config files
- Multi-role security: user, role, permission, operation traceability
- Deployment/runtime: project packaging, runtime mode, remote management, update strategy

This repository currently covers only a small subset:

- Shell / navigation: yes
- User / role / permission: yes
- Audit concept: partially
- Theme / basic UI shell: yes
- Device abstraction: no
- Protocol drivers: no
- Real-time tag engine: no
- Alarm engine: no
- Historian / trend / report: no
- Project configuration center: no
- Plugin/runtime packaging strategy: no

## 5. Recommended platform direction

If the goal is "make future secondary development easier and make upper-computer development easier", the repository should evolve toward:

`host shell + common platform services + industrial runtime kernel + project modules`

Recommended layering:

1. Host shell
   - WPF shell, navigation, theme, notifications, dialogs, status bar, layout
2. Platform services
   - account, permission, configuration, logging, audit, settings, file storage, update service
3. Industrial runtime kernel
   - devices, protocols, tags, polling scheduler, command write-back, alarm engine, historian
4. Project modules
   - forming, mixing, workshop, quality, report, recipe, maintenance
5. Project app
   - customer-specific startup composition and branding

## 6. TODO roadmap

### Phase A: Fix platform correctness first

- [ ] Await database initialization before first navigation
- [ ] Remove hard-coded database credentials from repository defaults
- [ ] Replace hard-coded audit user info with `IUserSession`
- [ ] Unify permission source of truth; remove duplicate menu permission mapping
- [ ] Add persistent local settings service
- [ ] Replace in-memory audit log service with persistent storage implementation
- [ ] Normalize file encoding and ensure Chinese text is stored/displayed correctly

### Phase B: Build reusable platform services

- [ ] Create a real `Vk.Dbp.Services` application service layer
- [ ] Add `IAppSettingsService`, `IConfigurationProfileService`, `IUpdateService`
- [ ] Add a dialog service, notification service, and common shell command service
- [ ] Add module metadata and module discovery model
- [ ] Add a plugin/extension contract for project modules
- [ ] Add DTO / entity / mapper conventions to reduce repeated mapping code

### Phase C: Add upper-computer core capabilities

- [ ] Design a device abstraction: `Device`, `Point`, `Tag`, `Command`, `Protocol`
- [ ] Add polling/subscription scheduler
- [ ] Add protocol adapter interfaces for Modbus TCP/RTU, OPC UA, MQTT, serial
- [ ] Add quality code / timestamp / source tracking for all real-time values
- [ ] Add alarm rule engine and alarm lifecycle
- [ ] Add historian storage schema and trend query API
- [ ] Add report/export service
- [ ] Add recipe/parameter download-upload service
- [ ] Add equipment status board and communication diagnosis view

### Phase D: Improve developer experience for secondary development

- [ ] Add project template guidance for "new module", "new view", "new device driver", "new app"
- [ ] Add a standard folder contract for modules
- [ ] Add sample module with real data flow instead of placeholder dashboard
- [ ] Add integration tests for account, permission, and persistence
- [ ] Add CI workflow for restore/build/test
- [ ] Add sample publish profiles for dev/test/prod environments
- [ ] Add environment-specific config files
- [ ] Add architectural decision records and extension documents

### Phase E: Industrialization and delivery

- [ ] Add offline-first mode with SQLite/local cache
- [ ] Add remote API sync strategy
- [ ] Add exception collection, log rotation, and health diagnostics
- [ ] Add upgrade/rollback mechanism
- [ ] Add operator audit export and compliance reports
- [ ] Add project backup/import/export mechanism
- [ ] Add installer/package strategy (MSIX or unpackaged installer)

## 7. Files worth prioritizing

- `src/Vk.Dbp.WpfWindow/PrismBootstrapper.cs`
- `src/Vk.Dbp.WpfWindow/appsettings.json`
- `src/Vk.Dbp.WpfWindow/Services/MenuPermissionConfig.cs`
- `src/Vk.Dbp.WpfWindow/Services/ThemeService.cs`
- `dbpframework/Vk.Dbp.Core/Audit/Services/AuditLogService.cs`
- `prismModules/Vk.Dbp.AccountModule/Services/UserService.cs`
- `prismModules/Vk.Dbp.AccountModule/ViewModels/LoginViewModel.cs`
- `prismModules/Vk.Dbp.AccountModule/ViewModels/UserManagementViewModel.cs`
- `prismModules/Vk.Dbp.AccountModule/ViewModels/AuditLogViewModel.cs`
- `src/Vk.Dbp.Services/Class1.cs`

## 8. Suggested next implementation order

Recommended actual execution order:

1. fix startup / security / audit correctness
2. build persistent settings + persistent audit + unified permission source
3. establish device/tag/protocol abstraction
4. add alarm/historian/trend/reporting
5. provide one real industrial sample module
6. add tests, CI, packaging, and deployment docs

If the repository continues in this direction, it can become a good upper-computer base project. If not, it will remain a management-system demo with a WPF shell.
