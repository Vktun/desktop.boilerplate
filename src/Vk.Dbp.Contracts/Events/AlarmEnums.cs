namespace Vk.Dbp.Contracts.Events
{
    /// <summary>
    /// 告警等级枚举
    /// </summary>
    public enum AlarmLevel
    {
        /// <summary>
        /// 信息级别 - 一般提示，蓝色显示
        /// </summary>
        Info = 0,

        /// <summary>
        /// 警告级别 - 需要关注，橙色显示
        /// </summary>
        Warning = 1,

        /// <summary>
        /// 严重级别 - 需要立即处理，红色显示
        /// </summary>
        Critical = 2
    }

    /// <summary>
    /// 告警状态枚举
    /// </summary>
    public enum AlarmStatus
    {
        /// <summary>
        /// 活跃状态 - 告警正在发生，未处理
        /// </summary>
        Active = 0,

        /// <summary>
        /// 已确认状态 - 用户已知晓告警
        /// </summary>
        Acknowledged = 1,

        /// <summary>
        /// 已解决状态 - 告警已消除或处理完成
        /// </summary>
        Resolved = 2,

        /// <summary>
        /// 已忽略状态 - 用户主动忽略该告警
        /// </summary>
        Ignored = 3
    }

    /// <summary>
    /// 告警类型枚举
    /// </summary>
    public enum AlarmType
    {
        /// <summary>
        /// 阈值告警 - 参数越限
        /// </summary>
        Threshold = 0,

        /// <summary>
        /// 设备告警 - 设备故障或异常
        /// </summary>
        Device = 1,

        /// <summary>
        /// 流程告警 - 工艺流程异常
        /// </summary>
        Process = 2,

        /// <summary>
        /// 系统告警 - 系统级异常
        /// </summary>
        System = 3,

        /// <summary>
        /// 安全告警 - 安全相关警告
        /// </summary>
        Safety = 4
    }
}