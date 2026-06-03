# 快速开始

## 前置要求

- .NET 10 SDK
- Visual Studio 2026+ 或 JetBrains Rider
- SQL Server 2019+（或使用LocalDB进行开发测试）

## 克隆和构建

```bash
git clone https://github.com/yourorg/desktop.boilerplate.git
cd desktop.boilerplate
dotnet restore
dotnet build
```

## 配置本地 SQL Server

主程序启动时会读取 `src/Vk.Dbp.WpfWindow/appsettings.json`，再读取可选的 `appsettings.local.json` 覆盖本地配置。开发环境建议把实际数据库连接写入 `appsettings.local.json`，该文件已在 `.gitignore` 中排除。

1. 在 `src/Vk.Dbp.WpfWindow` 目录创建 `appsettings.local.json`。

2. 使用本地 SQL Server 连接串，例如 Windows 身份验证：
   ```json
   {
     "ConnectionStrings": {
       "Default": "Server=127.0.0.1;Database=DabpCore;Trusted_Connection=True;TrustServerCertificate=True;"
     }
   }
   ```

   如果使用 SQL Server 账号密码，改为：
   ```json
   {
     "ConnectionStrings": {
       "Default": "Server=127.0.0.1;Database=DabpCore;Trusted_Connection=False;TrustServerCertificate=True;User Id=sa;Password=your_password"
     }
   }
   ```

3. 运行应用后，SqlSugar CodeFirst 会自动创建数据库表并初始化基础种子数据。

## 默认账户

首次启动且用户表为空时，系统会初始化管理员账号：

```text
用户名: admin
密码: 123456
```

首次登录后请立即修改默认密码。

## 配置系统

应用配置通过三层配置源管理（优先级从高到低）：

1. **环境变量**（最高优先级）- `ConnectionStrings__Default`、`DBP_INITIAL_ADMIN_PASSWORD` 等
2. **`appsettings.local.json`** - 本地覆盖配置（不提交到版本库，包含实际连接字符串）
3. **`appsettings.json`** - 默认配置（提交到版本库，连接字符串留空）

```json
{
  "ConnectionStrings": {
    "Default": ""
  },
  "Redis": {
    "Configuration": "127.0.0.1"
  },
  "Session": {
    "TimeoutMinutes": 15
  }
}
```

### 运行时配置

运行时用户偏好通过 `IAppSettingsService` 读写，持久化到 `%LOCALAPPDATA%/<AppName>/settings.json`（JSON键值存储，线程安全）：

```csharp
public class YourService
{
    private readonly IAppSettingsService _settingsService;

    public YourService(IAppSettingsService settingsService)
    {
        _settingsService = settingsService;
    }

    public void ReadConfig()
    {
        var value = _settingsService.GetValue<string>("YourKey", "defaultValue");
        _settingsService.SetValue("YourKey", "newValue");
    }
}
```

### 数据库配置

系统级运行时配置存储在数据库 `SystemConfig` 表中，通过 `ISystemConfigService` 读写：

| 配置键 | 说明 | 默认值 | 类型 |
|--------|------|--------|------|
| `Session.TimeoutEnabled` | 是否启用会话超时锁屏 | True | Boolean |
| `Session.TimeoutMinutes` | 超时时间（1-480分钟） | 15 | Integer |

```csharp
var timeout = await _systemConfigService.GetSessionTimeoutMinutesAsync();
await _systemConfigService.SetSessionTimeoutMinutesAsync(30);
```

### 关键环境变量

| 变量名 | 说明 | 使用场景 |
|--------|------|----------|
| `ConnectionStrings__Default` | SQL Server连接字符串 | 覆盖配置文件中的连接串 |
| `DBP_INITIAL_ADMIN_PASSWORD` | 首次启动时的管理员初始密码 | 数据库初始化种子数据（必填） |
| `ASPNETCORE_ENVIRONMENT` | 环境名称 | 默认 "Production" |

## 本地启动脚本

```powershell
# 使用LocalDB启动（默认）
.\scripts\start-wpf-local.ps1

# 指定连接字符串
.\scripts\start-wpf-local.ps1 -ConnectionString "Server=.;Database=DabpCore;Trusted_Connection=True;"

# 首次运行（初始化数据库+设置管理员密码）
.\scripts\start-wpf-local.ps1 -FirstRun -AdminPassword "your-secure-password"
```

更多配置说明详见 [本地配置指南](../LOCAL_CONFIGURATION.md)。
