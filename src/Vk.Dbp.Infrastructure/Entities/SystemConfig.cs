using SqlSugar;
using System;

namespace Dabp.Infrastructure.Entities
{
    /// <summary>
    /// 系统配置表
    /// </summary>
    [SugarTable("SystemConfigs")]
    public class SystemConfig
    {
        /// <summary>
        /// 配置ID
        /// </summary>
        [SugarColumn(IsPrimaryKey = true, IsIdentity = true)]
        public int Id { get; set; }

        /// <summary>
        /// 配置键
        /// </summary>
        [SugarColumn(Length = 100, IsNullable = false)]
        public string ConfigKey { get; set; } = string.Empty;

        /// <summary>
        /// 配置值
        /// </summary>
        [SugarColumn(Length = 500, IsNullable = true)]
        public string? ConfigValue { get; set; }

        /// <summary>
        /// 配置描述
        /// </summary>
        [SugarColumn(Length = 200, IsNullable = true)]
        public string? Description { get; set; }

        /// <summary>
        /// 配置类型（用于前端显示控件类型）
        /// </summary>
        [SugarColumn(Length = 50, IsNullable = true)]
        public string? ConfigType { get; set; }

        /// <summary>
        /// 创建时间
        /// </summary>
        public DateTime CreatedAt { get; set; } = DateTime.Now;

        /// <summary>
        /// 更新时间
        /// </summary>
        public DateTime? UpdatedAt { get; set; } = DateTime.Now;
    }

    /// <summary>
    /// 系统配置键常量
    /// </summary>
    public static class SystemConfigKeys
    {
        /// <summary>
        /// 会话超时时间（分钟）
        /// </summary>
        public const string SessionTimeoutMinutes = "Session.TimeoutMinutes";

        /// <summary>
        /// 是否启用会话超时
        /// </summary>
        public const string SessionTimeoutEnabled = "Session.TimeoutEnabled";
    }
}
