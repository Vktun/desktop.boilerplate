using Vk.Dbp.DeviceModule.Models;

namespace Vk.Dbp.DeviceModule.Services;

/// <summary>
/// 设备管理写侧契约（设备/点位/命令的增删改；读侧清单走 <see cref="IDeviceCatalogService"/>）。
/// 所有写操作记录审计日志（成功 LogOperation / 失败 LogFailure，模块名 DeviceManagement）。
/// </summary>
public interface IDeviceAdminService
{
    /// <summary>
    /// 保存设备（Id==0 新增，否则更新；Code 全局唯一，ConnectionConfig 需为合法 JSON）
    /// </summary>
    Task<AdminResult> SaveDeviceAsync(DeviceEditModel model);

    /// <summary>
    /// 删除设备：事务级联删除其点位与命令；PointHistory 历史保留（PointCode 冗余列仍可检索）
    /// </summary>
    Task<AdminResult> DeleteDeviceAsync(int deviceId);

    /// <summary>
    /// 保存点位（Code 全局唯一；双阈值同设时 High ≥ Low；设备必须存在）
    /// </summary>
    Task<AdminResult> SavePointAsync(PointEditModel model);

    /// <summary>
    /// 删除点位：级联删除引用该点位的目标命令；历史保留
    /// </summary>
    Task<AdminResult> DeletePointAsync(int pointId);

    /// <summary>
    /// 保存命令（Code 设备内唯一；目标点位必须属于同设备；写入值必填）
    /// </summary>
    Task<AdminResult> SaveCommandAsync(CommandEditModel model);

    /// <summary>
    /// 删除命令
    /// </summary>
    Task<AdminResult> DeleteCommandAsync(int commandId);

    /// <summary>
    /// 取设备全量编辑数据（对话框编辑模式用；未找到返回 null）
    /// </summary>
    Task<DeviceEditModel?> GetDeviceAsync(int deviceId);

    /// <summary>
    /// 取点位全量编辑数据
    /// </summary>
    Task<PointEditModel?> GetPointAsync(int pointId);

    /// <summary>
    /// 取命令全量编辑数据
    /// </summary>
    Task<CommandEditModel?> GetCommandAsync(int commandId);
}
