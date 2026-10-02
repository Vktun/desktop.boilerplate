using Vk.Dbp.Contracts.Industrial;

namespace Vk.Dbp.DeviceModule.Models;

/// <summary>
/// 设备管理写操作统一返回（消息可直接用于 Growl 提示）
/// </summary>
public sealed record AdminResult(bool Success, string Message)
{
    /// <summary>
    /// 构造成功结果
    /// </summary>
    public static AdminResult Ok(string message)
    {
        return new AdminResult(true, message);
    }

    /// <summary>
    /// 构造失败结果
    /// </summary>
    public static AdminResult Fail(string message)
    {
        return new AdminResult(false, message);
    }
}

/// <summary>
/// 设备编辑模型（编辑对话框与管理服务的数据载体）
/// </summary>
public sealed class DeviceEditModel
{
    /// <summary>
    /// 设备ID（0=新增）
    /// </summary>
    public int Id { get; set; }

    /// <summary>
    /// 设备编码（全局唯一）
    /// </summary>
    public string Code { get; set; } = string.Empty;

    /// <summary>
    /// 设备名称
    /// </summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>
    /// 协议类型（见 ProtocolTypes 常量）
    /// </summary>
    public string ProtocolType { get; set; } = ProtocolTypes.Simulated;

    /// <summary>
    /// 连接配置 JSON（协议相关）
    /// </summary>
    public string? ConnectionConfig { get; set; }

    /// <summary>
    /// 描述
    /// </summary>
    public string? Description { get; set; }

    /// <summary>
    /// 是否启用
    /// </summary>
    public bool IsEnabled { get; set; } = true;
}

/// <summary>
/// 点位编辑模型
/// </summary>
public sealed class PointEditModel
{
    /// <summary>
    /// 点位ID（0=新增）
    /// </summary>
    public int Id { get; set; }

    /// <summary>
    /// 所属设备ID
    /// </summary>
    public int DeviceId { get; set; }

    /// <summary>
    /// 点位编码（全局唯一）
    /// </summary>
    public string Code { get; set; } = string.Empty;

    /// <summary>
    /// 点位名称
    /// </summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>
    /// 驱动地址（模拟波形微语法或 Modbus 地址）
    /// </summary>
    public string Address { get; set; } = string.Empty;

    /// <summary>
    /// 数据类型
    /// </summary>
    public PointDataType DataType { get; set; } = PointDataType.Double;

    /// <summary>
    /// 工程单位
    /// </summary>
    public string? Unit { get; set; }

    /// <summary>
    /// 高限告警阈值（null=不检测）
    /// </summary>
    public decimal? AlarmHigh { get; set; }

    /// <summary>
    /// 低限告警阈值（null=不检测）
    /// </summary>
    public decimal? AlarmLow { get; set; }

    /// <summary>
    /// 描述
    /// </summary>
    public string? Description { get; set; }

    /// <summary>
    /// 是否启用
    /// </summary>
    public bool IsEnabled { get; set; } = true;
}

/// <summary>
/// 命令编辑模型
/// </summary>
public sealed class CommandEditModel
{
    /// <summary>
    /// 命令ID（0=新增）
    /// </summary>
    public int Id { get; set; }

    /// <summary>
    /// 所属设备ID
    /// </summary>
    public int DeviceId { get; set; }

    /// <summary>
    /// 命令编码（设备内唯一）
    /// </summary>
    public string Code { get; set; } = string.Empty;

    /// <summary>
    /// 命令名称
    /// </summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>
    /// 目标点位编码
    /// </summary>
    public string TargetPointCode { get; set; } = string.Empty;

    /// <summary>
    /// 写入值（字符串形式，由驱动按点位数据类型解析）
    /// </summary>
    public string WriteValue { get; set; } = string.Empty;

    /// <summary>
    /// 描述
    /// </summary>
    public string? Description { get; set; }

    /// <summary>
    /// 是否启用
    /// </summary>
    public bool IsEnabled { get; set; } = true;
}
