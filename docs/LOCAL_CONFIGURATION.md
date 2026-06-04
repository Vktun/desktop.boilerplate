# 本地配置指南

## 数据库连接

在 `src/Vk.Dbp.WpfWindow/` 目录下，从 `appsettings.local.example.json` 复制创建 `appsettings.local.json`。

`appsettings.local.json` 已被 `.gitignore` 排除，不会被提交到版本库。

### 配置示例

使用 Windows 身份验证：

```json
{
  "ConnectionStrings": {
    "Default": "Server=127.0.0.1;Database=DabpCore;Trusted_Connection=True;TrustServerCertificate=True;"
  }
}
```

使用 SQL Server 账号密码：

```json
{
  "ConnectionStrings": {
    "Default": "Server=127.0.0.1;Database=DabpCore;Trusted_Connection=False;TrustServerCertificate=True;User Id=sa;Password=your_password"
  }
}
```

### Redis 缓存配置

Redis 为可选配置，默认不启用。未启用时，应用继续使用进程内缓存，不需要额外安装 Redis。

禁用 Redis（默认值）：

```json
{
  "ConnectionStrings": {
    "Default": "Server=127.0.0.1;Database=DabpCore;Trusted_Connection=True;TrustServerCertificate=True;"
  },
  "Redis": {
    "Enabled": false,
    "ConnectionString": "",
    "InstanceName": "Vk.Dbp"
  }
}
```

启用 Redis：

```json
{
  "ConnectionStrings": {
    "Default": "Server=127.0.0.1;Database=DabpCore;Trusted_Connection=True;TrustServerCertificate=True;"
  },
  "Redis": {
    "Enabled": true,
    "ConnectionString": "127.0.0.1:6379,abortConnect=false",
    "InstanceName": "Vk.Dbp"
  }
}
```

说明：
- `Redis:Enabled` 为 `true` 时，必须提供 `Redis:ConnectionString`
- `Redis:InstanceName` 用于给缓存键加前缀，隔离不同应用或环境
- 如果 Redis 初始化失败，应用会自动回退到内存缓存并记录告警日志

### 配置优先级

应用配置通过三层配置源管理（优先级从高到低）：

1. **环境变量**（最高优先级）— 如 `ConnectionStrings__Default`、`DBP_INITIAL_ADMIN_PASSWORD`、`Redis__Enabled`
2. **`appsettings.local.json`** — 本地覆盖配置（不提交到版本库）
3. **`appsettings.json`** — 默认配置（提交到版本库，连接字符串留空）

### SM4 加密连接字符串

如需在配置文件中存储加密的连接字符串（如 Tools 项目），使用 SM4 加密：

```json
{
  "ConnectionStrings": {
    "Default": "<SM4加密后的Base64密文>"
  },
  "Encryption": {
    "SM4Key": "你的16字节密钥"
  }
}
```

> 主 Shell（WpfWindow）直接使用明文连接字符串，不经过 SM4 解密。

## 初始管理员密码

首次数据库初始化前，设置初始管理员密码：

```powershell
$env:DBP_INITIAL_ADMIN_PASSWORD = "change-me-before-first-login"
```

应用不会在日志中记录密码，也不会回退到硬编码的默认密码。

## 快速本地启动（Windows PowerShell）

从仓库根目录运行：

```powershell
.\scripts\start-wpf-local.ps1 -AdminPassword "your-first-run-password"
```

脚本执行内容：

- 启动 `MSSQLLocalDB`（如未运行）
- 在当前进程中设置 `ConnectionStrings__Default` 和 `DBP_INITIAL_ADMIN_PASSWORD` 环境变量
- 运行 `src/Vk.Dbp.WpfWindow/Vk.Dbp.WpfWindow.csproj`

### 常用命令

```powershell
# 首次运行（初始化数据库 + 设置管理员密码）
.\scripts\start-wpf-local.ps1 -FirstRun -AdminPassword "your-first-run-password"

# 重复运行（数据库已初始化，无需密码）
.\scripts\start-wpf-local.ps1

# 指定连接字符串
.\scripts\start-wpf-local.ps1 -ConnectionString "Server=.;Database=DabpCore;Trusted_Connection=True;"
```

### 注意事项

- `-AdminPassword` 在首次 Schema 初始化时必填
- 首次成功登录后，请立即修改默认密码
- 数据库初始化完成后，后续启动无需再传 `-AdminPassword` 参数

## 运行时用户配置

运行时用户偏好通过 `IAppSettingsService` 读写，持久化到 `%LOCALAPPDATA%/<AppName>/settings.json`：

```csharp
var value = _settingsService.GetValue<string>("Theme", "Light");
_settingsService.SetValue("Theme", "Dark");
```

## 数据库运行时配置

系统级运行时配置存储在数据库 `SystemConfig` 表中，通过 `ISystemConfigService` 读写：

| 配置键 | 说明 | 默认值 |
|--------|------|--------|
| `Session.TimeoutEnabled` | 是否启用会话超时锁屏 | True |
| `Session.TimeoutMinutes` | 超时时间（1-480分钟） | 15 |

```csharp
var timeout = await _systemConfigService.GetSessionTimeoutMinutesAsync();
await _systemConfigService.SetSessionTimeoutMinutesAsync(30);
```

## 关键环境变量

| 变量名 | 说明 | 使用场景 |
|--------|------|----------|
| `ConnectionStrings__Default` | SQL Server 连接字符串 | 覆盖配置文件中的连接串 |
| `DBP_INITIAL_ADMIN_PASSWORD` | 首次启动时的管理员初始密码 | 数据库初始化种子数据（必填） |
| `Encryption:SM4Key` | SM4 加解密密钥 | Tools 项目解密连接字符串 |
| `Redis__Enabled` | 是否启用 Redis 缓存 | 覆盖 `Redis:Enabled` |
| `Redis__ConnectionString` | Redis 连接字符串 | 覆盖 `Redis:ConnectionString` |
| `Redis__InstanceName` | Redis 键前缀 | 覆盖 `Redis:InstanceName` |
