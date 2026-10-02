namespace Vk.Dbp.Contracts.Industrial;

/// <summary>
/// 点位数据类型（决定驱动解析方式与界面展示格式）
/// </summary>
public enum PointDataType
{
    /// <summary>
    /// 布尔量（0=关，非 0=开）
    /// </summary>
    Boolean = 0,

    /// <summary>
    /// 32 位整数
    /// </summary>
    Int32 = 1,

    /// <summary>
    /// 单精度浮点
    /// </summary>
    Float = 2,

    /// <summary>
    /// 双精度浮点
    /// </summary>
    Double = 3,

    /// <summary>
    /// 字符串
    /// </summary>
    String = 4
}

/// <summary>
/// 数据质量码（采集来源追踪的最小集，对齐 OPC 经典三分法）
/// </summary>
public enum DataQuality
{
    /// <summary>
    /// 采集正常
    /// </summary>
    Good = 0,

    /// <summary>
    /// 采集可疑（如超时恢复后的首帧）
    /// </summary>
    Uncertain = 1,

    /// <summary>
    /// 采集失败（地址解析错误、通讯异常等）
    /// </summary>
    Bad = 2
}

/// <summary>
/// 设备运行时状态（引擎内存态，独立于数据库中的 IsEnabled 启停列）
/// </summary>
public enum DeviceRuntimeStatus
{
    /// <summary>
    /// 设备被禁用，引擎不调度
    /// </summary>
    Disabled = 0,

    /// <summary>
    /// 已停止（引擎未启动或已停止）
    /// </summary>
    Stopped = 1,

    /// <summary>
    /// 连接中
    /// </summary>
    Connecting = 2,

    /// <summary>
    /// 运行中（连接成功且按周期采到数据）
    /// </summary>
    Running = 3,

    /// <summary>
    /// 故障（连续采集失败，退避重连中）
    /// </summary>
    Faulted = 4
}

/// <summary>
/// 协议类型常量。
/// 刻意使用字符串而非枚举：第三方扩展驱动无需修改本清单即可注册新协议（驱动工厂按名称匹配）。
/// </summary>
public static class ProtocolTypes
{
    /// <summary>
    /// 内置模拟驱动（零硬件依赖，按 Address 微语法生成确定性波形）
    /// </summary>
    public const string Simulated = "Simulated";

    /// <summary>
    /// Modbus TCP（规划中，阶段二接入）
    /// </summary>
    public const string ModbusTcp = "ModbusTcp";

    /// <summary>
    /// Modbus RTU / 串口（规划中）
    /// </summary>
    public const string ModbusRtu = "ModbusRtu";

    /// <summary>
    /// OPC UA（规划中）
    /// </summary>
    public const string OpcUa = "OpcUa";
}
