# Local Configuration

## Database connection

Create `src/Vk.Dbp.WpfWindow/appsettings.local.json` from `appsettings.local.example.json`.

`appsettings.local.json` is ignored by git and should not be committed.

## Initial administrator password

Before first database initialization, set the initial administrator password with:

```powershell
$env:DBP_INITIAL_ADMIN_PASSWORD = "change-me-before-first-login"
```

The application no longer logs or falls back to a hard-coded administrator password.

## Quick local startup (Windows PowerShell)

Run from repository root:

```powershell
.\scripts\start-wpf-local.ps1 -AdminPassword "your-first-run-password"
```

What it does:

- Starts `MSSQLLocalDB` (if not already running).
- Sets `ConnectionStrings__Default` and `DBP_INITIAL_ADMIN_PASSWORD` in the current process.
- Runs `src/Vk.Dbp.WpfWindow/Vk.Dbp.WpfWindow.csproj`.

Notes:

- `-AdminPassword` is required for first-time schema initialization.
- For a repeat run when DB already initialized, run without password:

```powershell
.\scripts\start-wpf-local.ps1
```

- For first-time initialization, add `-FirstRun`:

```powershell
.\scripts\start-wpf-local.ps1 -FirstRun -AdminPassword "your-first-run-password"
```

- After first successful login, create a strong new password and remove the old one.
