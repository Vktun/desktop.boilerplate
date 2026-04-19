using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Dabp.Infrastructure.Entities;
using Vk.Dbp.Contracts.Events;

namespace Vk.Dbp.Services.Alarm
{
    /// <summary>
    /// 告警服务接口
    /// </summary>
    public interface IAlarmService
    {
        /// <summary>
        /// 获取告警记录列表（支持筛选）
        /// </summary>
        /// <param name="userId">用户ID</param>
        /// <param name="status">告警状态（可选）</param>
        /// <param name="level">告警等级（可选）</param>
        /// <param name="startTime">开始时间（可选）</param>
        /// <param name="endTime">结束时间（可选）</param>
        /// <returns>告警记录列表</returns>
        Task<List<AlarmRecord>> GetAlarmRecordsAsync(int userId, AlarmStatus? status = null, AlarmLevel? level = null, DateTime? startTime = null, DateTime? endTime = null);

        /// <summary>
        /// 获取活跃告警数量
        /// </summary>
        /// <param name="userId">用户ID</param>
        /// <returns>活跃告警数量</returns>
        Task<int> GetActiveAlarmCountAsync(int userId);

        /// <summary>
        /// 获取严重告警数量
        /// </summary>
        /// <param name="userId">用户ID</param>
        /// <returns>严重告警数量</returns>
        Task<int> GetCriticalAlarmCountAsync(int userId);

        /// <summary>
        /// 获取单个告警详情
        /// </summary>
        /// <param name="id">告警记录ID</param>
        /// <returns>告警记录</returns>
        Task<AlarmRecord> GetAlarmByIdAsync(int id);

        /// <summary>
        /// 创建新告警
        /// </summary>
        /// <param name="record">告警记录</param>
        /// <returns>是否成功</returns>
        Task<bool> CreateAlarmAsync(AlarmRecord record);

        /// <summary>
        /// 确认告警
        /// </summary>
        /// <param name="id">告警记录ID</param>
        /// <param name="userId">确认用户ID</param>
        /// <returns>是否成功</returns>
        Task<bool> AcknowledgeAlarmAsync(int id, int userId);

        /// <summary>
        /// 解决告警
        /// </summary>
        /// <param name="id">告警记录ID</param>
        /// <param name="userId">解决用户ID</param>
        /// <returns>是否成功</returns>
        Task<bool> ResolveAlarmAsync(int id, int userId);

        /// <summary>
        /// 忽略告警
        /// </summary>
        /// <param name="id">告警记录ID</param>
        /// <param name="userId">忽略用户ID</param>
        /// <returns>是否成功</returns>
        Task<bool> IgnoreAlarmAsync(int id, int userId);

        /// <summary>
        /// 批量确认所有活跃告警
        /// </summary>
        /// <param name="userId">确认用户ID</param>
        /// <returns>确认数量</returns>
        Task<int> AcknowledgeAllAsync(int userId);

        /// <summary>
        /// 获取今日告警数量
        /// </summary>
        /// <param name="userId">用户ID</param>
        /// <returns>今日告警数量</returns>
        Task<int> GetTodayAlarmCountAsync(int userId);

        /// <summary>
        /// 获取分页告警记录
        /// </summary>
        /// <param name="userId">用户ID</param>
        /// <param name="pageIndex">页码</param>
        /// <param name="pageSize">每页数量</param>
        /// <param name="status">状态筛选</param>
        /// <param name="level">等级筛选</param>
        /// <returns>告警列表和总数</returns>
        Task<(List<AlarmRecord> list, int total)> GetAlarmRecordsPageAsync(int userId, int pageIndex, int pageSize, AlarmStatus? status = null, AlarmLevel? level = null);
    }
}