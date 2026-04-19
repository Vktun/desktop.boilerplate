using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Dabp.Infrastructure.Entities;

namespace Vk.Dbp.Services.Alarm
{
    /// <summary>
    /// 告警配置服务接口
    /// </summary>
    public interface IAlarmConfigService
    {
        /// <summary>
        /// 获取所有告警配置
        /// </summary>
        /// <returns>告警配置列表</returns>
        Task<List<AlarmConfig>> GetAlarmConfigsAsync();

        /// <summary>
        /// 按告警代码获取配置
        /// </summary>
        /// <param name="alarmCode">告警代码</param>
        /// <returns>告警配置</returns>
        Task<AlarmConfig> GetAlarmConfigByCodeAsync(string alarmCode);

        /// <summary>
        /// 按ID获取配置
        /// </summary>
        /// <param name="id">配置ID</param>
        /// <returns>告警配置</returns>
        Task<AlarmConfig> GetAlarmConfigByIdAsync(int id);

        /// <summary>
        /// 保存告警配置
        /// </summary>
        /// <param name="config">告警配置</param>
        /// <returns>是否成功</returns>
        Task<bool> SaveAlarmConfigAsync(AlarmConfig config);

        /// <summary>
        /// 删除告警配置
        /// </summary>
        /// <param name="id">配置ID</param>
        /// <returns>是否成功</returns>
        Task<bool> DeleteAlarmConfigAsync(int id);

        /// <summary>
        /// 获取启用的告警配置
        /// </summary>
        /// <returns>启用的告警配置列表</returns>
        Task<List<AlarmConfig>> GetEnabledAlarmConfigsAsync();

        /// <summary>
        /// 验证值是否触发告警
        /// </summary>
        /// <param name="alarmCode">告警代码</param>
        /// <param name="value">实际值</param>
        /// <returns>是否触发告警</returns>
        Task<bool> ValidateThresholdAsync(string alarmCode, decimal value);
    }
}