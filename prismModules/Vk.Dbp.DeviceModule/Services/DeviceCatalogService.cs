using Dabp.Infrastructure.Entities;
using SqlSugar;
using Vk.Dbp.DeviceModule.Models;

namespace Vk.Dbp.DeviceModule.Services;

/// <summary>
/// 设备目录读取服务：直接查询 ISqlSugarClient（仓库既定现实），映射为模块模型返回。
/// </summary>
public sealed class DeviceCatalogService : IDeviceCatalogService
{
    private readonly ISqlSugarClient _db;

    /// <summary>
    /// 构造目录服务
    /// </summary>
    public DeviceCatalogService(ISqlSugarClient db)
    {
        _db = db ?? throw new ArgumentNullException(nameof(db));
    }

    /// <inheritdoc />
    public async Task<List<DeviceSummary>> GetDevicesAsync(bool includeDisabled = false)
    {
        var query = _db.Queryable<Device>().OrderBy(device => device.Id);
        if (!includeDisabled)
        {
            query = query.Where(device => device.IsEnabled);
        }

        var devices = await query.ToListAsync();
        return devices
            .Select(device => new DeviceSummary
            {
                Id = device.Id,
                Code = device.Code,
                Name = device.Name,
                ProtocolType = device.ProtocolType,
                IsEnabled = device.IsEnabled
            })
            .ToList();
    }

    /// <inheritdoc />
    public async Task<List<PointDefinition>> GetPointsAsync(int deviceId)
    {
        var points = await _db.Queryable<DevicePoint>()
            .Where(point => point.DeviceId == deviceId)
            .OrderBy(point => point.Id)
            .ToListAsync();

        return points
            .Select(point => new PointDefinition
            {
                Id = point.Id,
                DeviceId = point.DeviceId,
                Code = point.Code,
                Name = point.Name,
                DataType = point.DataType,
                Unit = point.Unit,
                Address = point.Address,
                AlarmHigh = point.AlarmHigh,
                AlarmLow = point.AlarmLow,
                IsEnabled = point.IsEnabled
            })
            .ToList();
    }

    /// <inheritdoc />
    public async Task<List<CommandDefinition>> GetCommandsAsync(int deviceId)
    {
        var commands = await _db.Queryable<DeviceCommand>()
            .Where(command => command.DeviceId == deviceId)
            .OrderBy(command => command.Id)
            .ToListAsync();

        return commands
            .Select(command => new CommandDefinition
            {
                Id = command.Id,
                DeviceId = command.DeviceId,
                Code = command.Code,
                Name = command.Name,
                TargetPointCode = command.TargetPointCode,
                WriteValue = command.WriteValue,
                IsEnabled = command.IsEnabled
            })
            .ToList();
    }
}
