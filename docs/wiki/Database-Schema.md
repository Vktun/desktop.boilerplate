# 数据库 Schema

框架通过 SqlSugar CodeFirst 自动创建 **13张表**，以下是核心实体关系：

## 核心实体

### User（用户）

| 字段 | 类型 | 说明 |
|------|------|------|
| Id | int | 主键，自增 |
| UserName | string(50) | 登录名 |
| PasswordHash | string(100) | PBKDF2哈希密码 |
| SurName | string(50) | 昵称/真实姓名（NVARCHAR） |
| PhoneNumber | string(11) | 手机号 |
| IsActive | bool | 是否启用 |
| ChangePasswordLastTime | DateTime | 最近改密时间 |
| ValideDays | int | 密码有效期（天） |
| IsDeleted | bool | 软删除标记 |

### Role（角色）

| 字段 | 类型 | 说明 |
|------|------|------|
| Id | int | 主键 |
| Name | string(50) | 角色名（NVARCHAR） |
| IsDefault | bool | 是否默认角色 |
| RoleLevel | int | 角色等级（1=启用, 0=禁用） |

### Permission（权限）

| 字段 | 类型 | 说明 |
|------|------|------|
| Id | int | 主键 |
| DisplyName | string(100) | 显示名（NVARCHAR） |
| ParentName | string(100) | 父级名称（NVARCHAR） |
| ProviderId | int | 1=用户, 2=角色 |
| ProviderKey | string(50) | 权限代码 |
| IsEnabled | bool | 是否启用 |

### 关联表

- **UserRole** - 用户-角色关联（复合主键：UserId + RoleId）
- **RolePermission** - 角色-权限关联（复合主键：RoleId + PermissionId）
- **UserOrganizationUnit** - 用户-组织关联（复合主键：UserId + OrganizationUnitId）
- **RoleOrganizationUnit** - 角色-组织关联（复合主键：RoleId + OrganizationUnitId）

### OrganizationUnit（组织单位）

| 字段 | 类型 | 说明 |
|------|------|------|
| Id | int | 主键 |
| DisplyName | string(100) | 组织名称 |
| Code | string(60) | 组织编码 |
| ParentId | int | 父级组织ID |

### AlarmConfig（告警配置）

| 字段 | 类型 | 说明 |
|------|------|------|
| Id | int | 主键，自增 |
| AlarmCode | string(100) | 告警代码（唯一） |
| AlarmName | string(200) | 告警名称 |
| ThresholdMin/Max | decimal? | 阈值范围 |
| ComparisonType | string(50) | GreaterThan/LessThan/InRange/OutOfRange/Equal/NotEqual |
| EnablePopup | bool | 弹窗通知 |
| EnableSound | bool | 声音告警 |
| AutoAcknowledge | bool | 自动确认 |
| Priority | int | 优先级排序 |

### AlarmRecord（告警记录）

| 字段 | 类型 | 说明 |
|------|------|------|
| Id | int | 主键，自增 |
| AlarmCode | string(100) | 告警代码 |
| AlarmLevel | enum | Info(0) / Warning(1) / Critical(2) |
| AlarmStatus | enum | Active(0) / Acknowledged(1) / Resolved(2) / Ignored(3) |
| AlarmType | enum | Threshold / Device / Process / System / Safety |
| ThresholdValue | decimal? | 阈值设定 |
| ActualValue | decimal? | 实际测量值 |

### SystemConfig（系统配置）

| 字段 | 类型 | 说明 |
|------|------|------|
| ConfigKey | string(100) | 配置键 |
| ConfigValue | string(500) | 配置值 |
| ConfigType | string(50) | 类型标记（Boolean/Integer/String） |

### AuditLog（审计日志）

| 字段 | 类型 | 说明 |
|------|------|------|
| ModuleName | string(100) | 模块名 |
| ServiceName | string(100) | 服务名 |
| MethodName | string(200) | 方法名 |
| IsSuccess | bool | 是否成功 |
| ExecutionDuration | long | 执行耗时（毫秒） |
| Parameters | string(5000) | 参数（JSON） |
| Exceptions | string(5000) | 异常详情 |

### Notification（通知）

| 字段 | 类型 | 说明 |
|------|------|------|
| Title | string(200) | 标题 |
| Content | string(2000) | 内容 |
| Type | string(50) | 类型字符串 |
| IsRead | bool | 已读标记 |
| UserId | int | 目标用户 |

## 实体关系图

```
User ──┬── UserRole ──── Role ──── RolePermission ──── Permission
       │                        │
       │                   RoleOrganizationUnit
       │                        │
       └── UserOrganizationUnit ── OrganizationUnit
```

## 种子数据

首次启动时，`DatabaseInitializer` 自动初始化以下种子数据：

| 类型 | 内容 |
|------|------|
| **角色** | "Admin"（默认角色，Level=1）、"Normal User" |
| **权限** | 从 `ShellMenuDefinitions` 自动生成12项菜单权限 |
| **管理员** | admin用户，密码来自环境变量 `DBP_INITIAL_ADMIN_PASSWORD`，默认90天有效期 |
| **默认角色权限** | Admin角色拥有所有权限；普通用户默认拥有 Dashboard、SelfCheck、Production、ProductionRecord、AlarmRecord |
| **系统配置** | Session.TimeoutEnabled=True, Session.TimeoutMinutes=15 |
| **告警配置** | TEMP_HIGH（温度过高，>85°C）、PRESSURE_LOW（压力过低，<0.5MPa）、DEVICE_FAULT（设备故障）、SYSTEM_INFO（系统信息） |
