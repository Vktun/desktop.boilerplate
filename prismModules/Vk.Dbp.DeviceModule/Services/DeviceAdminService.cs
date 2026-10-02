using System.Text.Json;
using Dabp.Infrastructure.Entities;
using Dabp.Utils.Exceptions;
using SqlSugar;
using Vk.Dbp.DeviceModule.Models;
using Vk.Dbp.Services.Audit;
using Vk.Dbp.Services.Session;

namespace Vk.Dbp.DeviceModule.Services;

/// <summary>
/// 设备管理写侧服务：设备/点位/命令的增删改 + 审计。
/// 校验失败返回 <see cref="AdminResult.Fail"/>（输入错误不记审计失败）；
/// 落库成功记 Create/Update/Delete 审计；仓储异常按仓库约定过滤并记 LogFailure。
/// </summary>
public sealed class DeviceAdminService : IDeviceAdminService
{
    private const string ModuleName = "DeviceManagement";

    private readonly ISqlSugarClient _db;
    private readonly IAuditLogService _auditLogService;
    private readonly IUserSession _userSession;

    /// <summary>
    /// 构造设备管理服务
    /// </summary>
    public DeviceAdminService(ISqlSugarClient db, IAuditLogService auditLogService, IUserSession userSession)
    {
        _db = db ?? throw new ArgumentNullException(nameof(db));
        _auditLogService = auditLogService ?? throw new ArgumentNullException(nameof(auditLogService));
        _userSession = userSession ?? throw new ArgumentNullException(nameof(userSession));
    }

    /// <inheritdoc />
    public async Task<AdminResult> SaveDeviceAsync(DeviceEditModel model)
    {
        if (model is null)
        {
            throw new ArgumentNullException(nameof(model));
        }

        model.Code = model.Code.Trim();
        model.Name = model.Name.Trim();

        if (string.IsNullOrWhiteSpace(model.Code) || string.IsNullOrWhiteSpace(model.Name))
        {
            return AdminResult.Fail("设备编码与名称不能为空");
        }

        if (await _db.Queryable<Device>().AnyAsync(device => device.Code == model.Code && device.Id != model.Id))
        {
            return AdminResult.Fail($"设备编码 {model.Code} 已存在");
        }

        if (!string.IsNullOrWhiteSpace(model.ConnectionConfig))
        {
            if (model.ConnectionConfig.Trim().Length > 1000)
            {
                return AdminResult.Fail("连接配置超过 1000 字符上限");
            }

            try
            {
                using var _ = JsonDocument.Parse(model.ConnectionConfig);
            }
            catch (JsonException)
            {
                return AdminResult.Fail("连接配置不是合法的 JSON");
            }
        }

        try
        {
            if (model.Id == 0)
            {
                var entity = MapToDevice(model);
                entity.Id = await _db.Insertable(entity).ExecuteReturnIdentityAsync();
                await LogAuditAsync((service, userId, username) => service.LogCreateAsync(
                    userId, username, ModuleName, "Device", entity.Id, entity, $"创建设备 {entity.Code}"));
                return AdminResult.Ok($"设备 {entity.Code} 创建成功");
            }

            var existing = await _db.Queryable<Device>().FirstAsync(device => device.Id == model.Id);
            if (existing is null)
            {
                return AdminResult.Fail($"设备不存在: {model.Id}");
            }

            var oldSnapshot = MapToEditModel(existing);
            MapToDevice(model, existing);
            existing.UpdatedAt = DateTime.Now;
            await _db.Updateable(existing).ExecuteCommandAsync();
            await LogAuditAsync((service, userId, username) => service.LogUpdateAsync(
                userId, username, ModuleName, "Device", existing.Id, oldSnapshot, MapToEditModel(existing), $"更新设备 {existing.Code}"));
            return AdminResult.Ok($"设备 {existing.Code} 保存成功");
        }
        catch (Exception ex) when (ExpectedOperationExceptionFilter.IsExpectedDataOperationException(ex))
        {
            return await FailWithAuditAsync(
                AuditActionType.Update, "Device", model.Id, $"保存设备失败: {ex.Message}", ex);
        }
    }

    /// <inheritdoc />
    public async Task<AdminResult> DeleteDeviceAsync(int deviceId)
    {
        try
        {
            var device = await _db.Queryable<Device>().FirstAsync(entity => entity.Id == deviceId);
            if (device is null)
            {
                return AdminResult.Fail($"设备不存在: {deviceId}");
            }

            var pointCount = await _db.Queryable<DevicePoint>().CountAsync(point => point.DeviceId == deviceId);
            var commandCount = await _db.Queryable<DeviceCommand>().CountAsync(command => command.DeviceId == deviceId);

            // 级联删除点位与命令；PointHistory 保留——PointCode 为冗余编码列，设备删除后历史仍可按编码检索
            var committed = false;
            _db.Ado.BeginTran();
            try
            {
                await _db.Deleteable<DeviceCommand>().Where(command => command.DeviceId == deviceId).ExecuteCommandAsync();
                await _db.Deleteable<DevicePoint>().Where(point => point.DeviceId == deviceId).ExecuteCommandAsync();
                await _db.Deleteable<Device>().Where(entity => entity.Id == deviceId).ExecuteCommandAsync();
                _db.Ado.CommitTran();
                committed = true;
            }
            finally
            {
                if (!committed)
                {
                    _db.Ado.RollbackTran();
                }
            }

            await LogAuditAsync((service, userId, username) => service.LogDeleteAsync(
                userId, username, ModuleName, "Device", deviceId, device, $"删除设备 {device.Code}（含 {pointCount} 个点位、{commandCount} 个命令，历史数据已保留）"));
            return AdminResult.Ok($"已删除设备 {device.Code} 及其 {pointCount} 个点位、{commandCount} 个命令（历史数据已保留）");
        }
        catch (Exception ex) when (ExpectedOperationExceptionFilter.IsExpectedDataOperationException(ex))
        {
            return await FailWithAuditAsync(
                AuditActionType.Delete, "Device", deviceId, $"删除设备失败: {ex.Message}", ex);
        }
    }

    /// <inheritdoc />
    public async Task<AdminResult> SavePointAsync(PointEditModel model)
    {
        if (model is null)
        {
            throw new ArgumentNullException(nameof(model));
        }

        model.Code = model.Code.Trim();
        model.Name = model.Name.Trim();
        model.Address = model.Address.Trim();

        if (string.IsNullOrWhiteSpace(model.Code) || string.IsNullOrWhiteSpace(model.Name) || string.IsNullOrWhiteSpace(model.Address))
        {
            return AdminResult.Fail("点位编码、名称与驱动地址不能为空");
        }

        if (!await _db.Queryable<Device>().AnyAsync(device => device.Id == model.DeviceId))
        {
            return AdminResult.Fail($"所属设备不存在: {model.DeviceId}");
        }

        // 点位 Code 全局唯一（跨设备），与 UX_DevicePoints_Code 唯一索引一致
        if (await _db.Queryable<DevicePoint>().AnyAsync(point => point.Code == model.Code && point.Id != model.Id))
        {
            return AdminResult.Fail($"点位编码 {model.Code} 已被其他点位使用（全局唯一）");
        }

        if (model.AlarmHigh is { } high && model.AlarmLow is { } low && high < low)
        {
            return AdminResult.Fail("高限阈值必须大于等于低限阈值");
        }

        try
        {
            if (model.Id == 0)
            {
                var entity = MapToPoint(model);
                entity.Id = await _db.Insertable(entity).ExecuteReturnIdentityAsync();
                await LogAuditAsync((service, userId, username) => service.LogCreateAsync(
                    userId, username, ModuleName, "DevicePoint", entity.Id, entity, $"创建点位 {entity.Code}"));
                return AdminResult.Ok($"点位 {entity.Code} 创建成功");
            }

            var existing = await _db.Queryable<DevicePoint>().FirstAsync(point => point.Id == model.Id);
            if (existing is null)
            {
                return AdminResult.Fail($"点位不存在: {model.Id}");
            }

            var oldSnapshot = MapToEditModel(existing);
            MapToPoint(model, existing);
            existing.UpdatedAt = DateTime.Now;
            await _db.Updateable(existing).ExecuteCommandAsync();
            await LogAuditAsync((service, userId, username) => service.LogUpdateAsync(
                userId, username, ModuleName, "DevicePoint", existing.Id, oldSnapshot, MapToEditModel(existing), $"更新点位 {existing.Code}"));
            return AdminResult.Ok($"点位 {existing.Code} 保存成功");
        }
        catch (Exception ex) when (ExpectedOperationExceptionFilter.IsExpectedDataOperationException(ex))
        {
            return await FailWithAuditAsync(
                AuditActionType.Update, "DevicePoint", model.Id, $"保存点位失败: {ex.Message}", ex);
        }
    }

    /// <inheritdoc />
    public async Task<AdminResult> DeletePointAsync(int pointId)
    {
        try
        {
            var point = await _db.Queryable<DevicePoint>().FirstAsync(entity => entity.Id == pointId);
            if (point is null)
            {
                return AdminResult.Fail($"点位不存在: {pointId}");
            }

            // 级联删除引用该点位作为目标的命令，避免留下必然执行失败的命令
            var referencingCommands = await _db.Queryable<DeviceCommand>()
                .Where(command => command.TargetPointCode == point.Code)
                .ToListAsync();

            var committed = false;
            _db.Ado.BeginTran();
            try
            {
                if (referencingCommands.Count > 0)
                {
                    await _db.Deleteable<DeviceCommand>()
                        .Where(command => command.TargetPointCode == point.Code)
                        .ExecuteCommandAsync();
                }

                await _db.Deleteable<DevicePoint>().Where(entity => entity.Id == pointId).ExecuteCommandAsync();
                _db.Ado.CommitTran();
                committed = true;
            }
            finally
            {
                if (!committed)
                {
                    _db.Ado.RollbackTran();
                }
            }

            await LogAuditAsync((service, userId, username) => service.LogDeleteAsync(
                userId, username, ModuleName, "DevicePoint", pointId, point,
                $"删除点位 {point.Code}（并删除引用该点位的 {referencingCommands.Count} 个命令，历史数据已保留）"));
            return AdminResult.Ok($"已删除点位 {point.Code} 及引用它的 {referencingCommands.Count} 个命令（历史数据已保留）");
        }
        catch (Exception ex) when (ExpectedOperationExceptionFilter.IsExpectedDataOperationException(ex))
        {
            return await FailWithAuditAsync(
                AuditActionType.Delete, "DevicePoint", pointId, $"删除点位失败: {ex.Message}", ex);
        }
    }

    /// <inheritdoc />
    public async Task<AdminResult> SaveCommandAsync(CommandEditModel model)
    {
        if (model is null)
        {
            throw new ArgumentNullException(nameof(model));
        }

        model.Code = model.Code.Trim();
        model.Name = model.Name.Trim();
        model.TargetPointCode = model.TargetPointCode.Trim();
        model.WriteValue = (model.WriteValue ?? string.Empty).Trim();

        if (string.IsNullOrWhiteSpace(model.Code) || string.IsNullOrWhiteSpace(model.Name)
            || string.IsNullOrWhiteSpace(model.TargetPointCode) || string.IsNullOrWhiteSpace(model.WriteValue))
        {
            return AdminResult.Fail("命令编码、名称、目标点位与写入值不能为空");
        }

        if (!await _db.Queryable<Device>().AnyAsync(device => device.Id == model.DeviceId))
        {
            return AdminResult.Fail($"所属设备不存在: {model.DeviceId}");
        }

        // 命令编码按设备内唯一校验（实体无唯一索引，应用层保证）
        if (await _db.Queryable<DeviceCommand>().AnyAsync(command =>
                command.DeviceId == model.DeviceId && command.Code == model.Code && command.Id != model.Id))
        {
            return AdminResult.Fail($"命令编码 {model.Code} 在该设备下已存在");
        }

        if (!await _db.Queryable<DevicePoint>().AnyAsync(point =>
                point.DeviceId == model.DeviceId && point.Code == model.TargetPointCode))
        {
            return AdminResult.Fail($"目标点位 {model.TargetPointCode} 不属于该设备");
        }

        try
        {
            if (model.Id == 0)
            {
                var entity = MapToCommand(model);
                entity.Id = await _db.Insertable(entity).ExecuteReturnIdentityAsync();
                await LogAuditAsync((service, userId, username) => service.LogCreateAsync(
                    userId, username, ModuleName, "DeviceCommand", entity.Id, entity, $"创建命令 {entity.Code}"));
                return AdminResult.Ok($"命令 {entity.Code} 创建成功");
            }

            var existing = await _db.Queryable<DeviceCommand>().FirstAsync(command => command.Id == model.Id);
            if (existing is null)
            {
                return AdminResult.Fail($"命令不存在: {model.Id}");
            }

            var oldSnapshot = MapToEditModel(existing);
            MapToCommand(model, existing);
            existing.UpdatedAt = DateTime.Now;
            await _db.Updateable(existing).ExecuteCommandAsync();
            await LogAuditAsync((service, userId, username) => service.LogUpdateAsync(
                userId, username, ModuleName, "DeviceCommand", existing.Id, oldSnapshot, MapToEditModel(existing), $"更新命令 {existing.Code}"));
            return AdminResult.Ok($"命令 {existing.Code} 保存成功");
        }
        catch (Exception ex) when (ExpectedOperationExceptionFilter.IsExpectedDataOperationException(ex))
        {
            return await FailWithAuditAsync(
                AuditActionType.Update, "DeviceCommand", model.Id, $"保存命令失败: {ex.Message}", ex);
        }
    }

    /// <inheritdoc />
    public async Task<AdminResult> DeleteCommandAsync(int commandId)
    {
        try
        {
            var command = await _db.Queryable<DeviceCommand>().FirstAsync(entity => entity.Id == commandId);
            if (command is null)
            {
                return AdminResult.Fail($"命令不存在: {commandId}");
            }

            await _db.Deleteable<DeviceCommand>().Where(entity => entity.Id == commandId).ExecuteCommandAsync();
            await LogAuditAsync((service, userId, username) => service.LogDeleteAsync(
                userId, username, ModuleName, "DeviceCommand", commandId, command, $"删除命令 {command.Code}"));
            return AdminResult.Ok($"已删除命令 {command.Code}");
        }
        catch (Exception ex) when (ExpectedOperationExceptionFilter.IsExpectedDataOperationException(ex))
        {
            return await FailWithAuditAsync(
                AuditActionType.Delete, "DeviceCommand", commandId, $"删除命令失败: {ex.Message}", ex);
        }
    }

    /// <inheritdoc />
    public async Task<DeviceEditModel?> GetDeviceAsync(int deviceId)
    {
        var device = await _db.Queryable<Device>().FirstAsync(entity => entity.Id == deviceId);
        return device is null ? null : MapToEditModel(device);
    }

    /// <inheritdoc />
    public async Task<PointEditModel?> GetPointAsync(int pointId)
    {
        var point = await _db.Queryable<DevicePoint>().FirstAsync(entity => entity.Id == pointId);
        return point is null ? null : MapToEditModel(point);
    }

    /// <inheritdoc />
    public async Task<CommandEditModel?> GetCommandAsync(int commandId)
    {
        var command = await _db.Queryable<DeviceCommand>().FirstAsync(entity => entity.Id == commandId);
        return command is null ? null : MapToEditModel(command);
    }

    private async Task LogAuditAsync(
        Func<IAuditLogService, int, string, Task<bool>> write)
    {
        var userId = _userSession.GetAuditUserId();
        var username = _userSession.GetAuditUsername();
        await write(_auditLogService, userId, username);
    }

    private async Task<AdminResult> FailWithAuditAsync(
        AuditActionType actionType,
        string entityType,
        int entityId,
        string message,
        Exception exception)
    {
        var userId = _userSession.GetAuditUserId();
        var username = _userSession.GetAuditUsername();
        await _auditLogService.LogFailureAsync(
            userId, username, actionType, ModuleName, message, exception.Message, entityType, entityId);
        return AdminResult.Fail(message);
    }

    private static Device MapToDevice(DeviceEditModel model)
    {
        return MapToDevice(model, new Device());
    }

    private static Device MapToDevice(DeviceEditModel model, Device entity)
    {
        entity.Code = model.Code;
        entity.Name = model.Name;
        entity.ProtocolType = model.ProtocolType;
        entity.ConnectionConfig = string.IsNullOrWhiteSpace(model.ConnectionConfig) ? null : model.ConnectionConfig.Trim();
        entity.Description = string.IsNullOrWhiteSpace(model.Description) ? null : model.Description.Trim();
        entity.IsEnabled = model.IsEnabled;
        return entity;
    }

    private static DevicePoint MapToPoint(PointEditModel model)
    {
        return MapToPoint(model, new DevicePoint());
    }

    private static DevicePoint MapToPoint(PointEditModel model, DevicePoint entity)
    {
        entity.DeviceId = model.DeviceId;
        entity.Code = model.Code;
        entity.Name = model.Name;
        entity.DataType = model.DataType;
        entity.Unit = string.IsNullOrWhiteSpace(model.Unit) ? null : model.Unit.Trim();
        entity.Address = model.Address;
        entity.AlarmHigh = model.AlarmHigh;
        entity.AlarmLow = model.AlarmLow;
        entity.Description = string.IsNullOrWhiteSpace(model.Description) ? null : model.Description.Trim();
        entity.IsEnabled = model.IsEnabled;
        return entity;
    }

    private static DeviceCommand MapToCommand(CommandEditModel model)
    {
        return MapToCommand(model, new DeviceCommand());
    }

    private static DeviceCommand MapToCommand(CommandEditModel model, DeviceCommand entity)
    {
        entity.DeviceId = model.DeviceId;
        entity.Code = model.Code;
        entity.Name = model.Name;
        entity.TargetPointCode = model.TargetPointCode;
        entity.WriteValue = model.WriteValue;
        entity.Description = string.IsNullOrWhiteSpace(model.Description) ? null : model.Description.Trim();
        entity.IsEnabled = model.IsEnabled;
        return entity;
    }

    private static DeviceEditModel MapToEditModel(Device entity)
    {
        return new DeviceEditModel
        {
            Id = entity.Id,
            Code = entity.Code,
            Name = entity.Name,
            ProtocolType = entity.ProtocolType,
            ConnectionConfig = entity.ConnectionConfig,
            Description = entity.Description,
            IsEnabled = entity.IsEnabled
        };
    }

    private static PointEditModel MapToEditModel(DevicePoint entity)
    {
        return new PointEditModel
        {
            Id = entity.Id,
            DeviceId = entity.DeviceId,
            Code = entity.Code,
            Name = entity.Name,
            DataType = entity.DataType,
            Unit = entity.Unit,
            Address = entity.Address,
            AlarmHigh = entity.AlarmHigh,
            AlarmLow = entity.AlarmLow,
            Description = entity.Description,
            IsEnabled = entity.IsEnabled
        };
    }

    private static CommandEditModel MapToEditModel(DeviceCommand entity)
    {
        return new CommandEditModel
        {
            Id = entity.Id,
            DeviceId = entity.DeviceId,
            Code = entity.Code,
            Name = entity.Name,
            TargetPointCode = entity.TargetPointCode,
            WriteValue = entity.WriteValue ?? string.Empty,
            Description = entity.Description,
            IsEnabled = entity.IsEnabled
        };
    }
}
