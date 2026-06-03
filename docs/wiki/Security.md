# 安全特性

## 密码安全

- **哈希算法**：PBKDF2 + SHA-256（BouncyCastle `Pkcs5S2ParametersGenerator`）
- **盐值**：每次哈希使用独立16字节随机盐
- **迭代次数**：100,000次
- **哈希长度**：32字节
- **存储格式**：`PBKDF2$100000$<base64-salt>$<base64-hash>`
- **防时序攻击**：使用常量时间比较，无效格式输入也执行等时dummy哈希

## Token生成

- 登录时生成32字节密码学安全随机Token（`System.Security.Cryptography.RandomNumberGenerator`）
- 输出格式：Base64url编码

## 密码重置

- 自动生成12位强密码（包含大写字母、小写字母、数字、特殊字符）
- 禁止将admin用户密码重置为弱密码

## 国密SM4加密

- 使用BouncyCastle实现SM4/CBC/PKCS7
- 提供 `SM4.Encrypt(plainText, key)` 和 `SM4.Decrypt(cipherText, key)` 静态方法
- 每次加密生成随机16字节IV，IV前置于密文输出（Base64格式）
- 密钥从 `appsettings.local.json` 的 `Encryption:SM4Key` 或环境变量读取
- 适用于需要国密合规的场景（如加密数据库连接字符串）

```csharp
// 加密
var sm4Key = configuration["Encryption:SM4Key"] ?? "DabpSm4DefaultKey";
string encrypted = SM4.Encrypt(connectionString, sm4Key);

// 解密
string decrypted = SM4.Decrypt(encrypted, sm4Key);
```

## 会话安全

### 会话超时自动锁屏

系统通过 `ISessionTimeoutService` 监控用户活动，超时后自动触发锁屏。配置存储在数据库 `SystemConfig` 表中：

| 配置键 | 说明 | 默认值 |
|--------|------|--------|
| `SessionTimeoutEnabled` | 是否启用超时锁屏 | True |
| `SessionTimeoutMinutes` | 超时时间（分钟） | 15 |

### 数据库断连自动锁屏

当SqlSugar检测到数据库连接错误时，自动触发锁屏（`LockScreenService.Lock("数据库连接失败")`）。

### 锁屏服务

```csharp
public class YourService
{
    private readonly ILockScreenService _lockScreenService;

    public YourService(ILockScreenService lockScreenService)
    {
        _lockScreenService = lockScreenService;
    }

    public void LockForSecurity()
    {
        _lockScreenService.Lock("安全原因锁定");
    }
}
```

- 锁屏需要重新输入密码验证才能解锁
- 锁屏事件通过 `LockScreenEventArgs` 携带原因和时间

## 审计安全

- 登录成功/失败均记录审计日志
- 敏感操作（用户CRUD、角色分配、权限变更、数据导出）自动记录
- 审计日志包含旧数据/新数据对比（JSON序列化）

## 告警服务

```csharp
public class YourService
{
    private readonly IAlarmService _alarmService;
    private readonly IAlarmConfigService _alarmConfigService;

    public async Task CheckAndTriggerAlarmAsync()
    {
        var configs = await _alarmConfigService.GetEnabledConfigsAsync();

        foreach (var config in configs)
        {
            if (IsThresholdExceeded(config))
            {
                await _alarmService.TriggerAlarmAsync(new AlarmRecord
                {
                    AlarmCode = config.AlarmCode,
                    Level = (AlarmLevel)config.Priority,
                    Title = config.AlarmName,
                    Content = $"当前值超出阈值",
                    Status = AlarmStatus.Active
                });
            }
        }
    }
}
```

告警配置在 `DatabaseInitializer` 中预置了4种示例配置：`TEMP_HIGH`、`PRESSURE_LOW`、`DEVICE_FAULT`、`SYSTEM_INFO`。
