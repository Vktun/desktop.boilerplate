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
