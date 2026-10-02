using System.Collections.Concurrent;
using Vk.Dbp.Contracts.Industrial;
using Vk.Dbp.DeviceModule.Models;

namespace Vk.Dbp.DeviceModule.Services;

/// <summary>
/// 内置模拟驱动：按点位 Address 微语法生成确定性波形，零硬件依赖。
/// 写入以"覆盖值"语义模拟命令回写：写入后该点持续返回覆盖值，直至再次写入。
/// </summary>
public sealed class SimulatedDriver : IProtocolDriver
{
    private readonly TimeProvider _timeProvider;
    private readonly string _deviceCode;

    // 地址解析结果缓存（null = 解析失败，同样缓存避免每个轮询周期重复解析）
    private readonly ConcurrentDictionary<string, ParsedSignal> _parsedSignals = new();

    // 命令回写覆盖值（点位编码 → 覆盖值）
    private readonly ConcurrentDictionary<string, double?> _writeOverrides = new(StringComparer.OrdinalIgnoreCase);

    /// <summary>
    /// 构造模拟驱动
    /// </summary>
    public SimulatedDriver(TimeProvider timeProvider, string deviceCode)
    {
        _timeProvider = timeProvider ?? throw new ArgumentNullException(nameof(timeProvider));
        _deviceCode = deviceCode;
    }

    /// <inheritdoc />
    public string ProtocolType => ProtocolTypes.Simulated;

    /// <inheritdoc />
    public bool IsConnected { get; private set; }

    /// <inheritdoc />
    public Task ConnectAsync(CancellationToken cancellationToken)
    {
        IsConnected = true;
        return Task.CompletedTask;
    }

    /// <inheritdoc />
    public Task DisconnectAsync()
    {
        IsConnected = false;
        return Task.CompletedTask;
    }

    /// <inheritdoc />
    public Task<IReadOnlyList<PointValueSnapshot>> ReadPointsAsync(
        IReadOnlyList<PointReadRequest> requests,
        CancellationToken cancellationToken)
    {
        var now = _timeProvider.GetLocalNow().LocalDateTime;
        var snapshots = new List<PointValueSnapshot>(requests.Count);

        foreach (var request in requests)
        {
            snapshots.Add(ReadSinglePoint(request, now));
        }

        return Task.FromResult<IReadOnlyList<PointValueSnapshot>>(snapshots);
    }

    /// <inheritdoc />
    public Task<PointValueSnapshot> WritePointAsync(PointWriteRequest request, CancellationToken cancellationToken)
    {
        var now = _timeProvider.GetLocalNow().LocalDateTime;

        if (!TryParseWriteValue(request.Value, out var writeValue, out var writeError))
        {
            return Task.FromResult(CreateSnapshot(request.PointCode, _deviceCode, 0, DataQuality.Bad, now, writeError ?? "写入值无法解析"));
        }

        _writeOverrides[request.PointCode] = writeValue;
        return Task.FromResult(BuildSnapshot(request.PointCode, _deviceCode, 0, request.DataType, writeValue!.Value, now));
    }

    /// <inheritdoc />
    public ValueTask DisposeAsync()
    {
        IsConnected = false;
        return ValueTask.CompletedTask;
    }

    private PointValueSnapshot ReadSinglePoint(PointReadRequest request, DateTime now)
    {
        // 命令回写覆盖优先：模拟"下发设定值后设备保持该值"的语义
        if (_writeOverrides.TryGetValue(request.PointCode, out var overrideValue))
        {
            return BuildSnapshot(request.PointCode, _deviceCode, request.PointId, request.DataType, overrideValue, now);
        }

        var parsed = _parsedSignals.GetOrAdd(
            request.Address,
            address => SimulatedSignal.TryParse(address, out var signal, out var error)
                ? new ParsedSignal(signal, null)
                : new ParsedSignal(null, error ?? "地址解析失败"));

        if (parsed.Signal is null)
        {
            return CreateSnapshot(request.PointCode, _deviceCode, request.PointId, DataQuality.Bad, now, $"地址解析失败: {parsed.Error}");
        }

        var value = parsed.Signal.Evaluate(now, request.PointId);
        return BuildSnapshot(request.PointCode, _deviceCode, request.PointId, request.DataType, value, now);
    }

    private static PointValueSnapshot BuildSnapshot(
        string pointCode,
        string deviceCode,
        int pointId,
        PointDataType dataType,
        double? value,
        DateTime now)
    {
        if (value is { } numericValue)
        {
            return dataType switch
            {
                PointDataType.Boolean => new PointValueSnapshot
                {
                    PointId = pointId,
                    PointCode = pointCode,
                    DeviceCode = deviceCode,
                    Value = numericValue != 0 ? 1 : 0,
                    ValueText = numericValue != 0 ? "开" : "关",
                    Quality = DataQuality.Good,
                    Timestamp = now
                },
                PointDataType.Int32 => new PointValueSnapshot
                {
                    PointId = pointId,
                    PointCode = pointCode,
                    DeviceCode = deviceCode,
                    Value = Math.Round(numericValue),
                    ValueText = ((int)Math.Round(numericValue)).ToString(),
                    Quality = DataQuality.Good,
                    Timestamp = now
                },
                PointDataType.String => new PointValueSnapshot
                {
                    PointId = pointId,
                    PointCode = pointCode,
                    DeviceCode = deviceCode,
                    Value = null,
                    ValueText = numericValue.ToString("F2"),
                    Quality = DataQuality.Good,
                    Timestamp = now
                },
                _ => new PointValueSnapshot
                {
                    PointId = pointId,
                    PointCode = pointCode,
                    DeviceCode = deviceCode,
                    Value = Math.Round(numericValue, 4),
                    ValueText = numericValue.ToString("F2"),
                    Quality = DataQuality.Good,
                    Timestamp = now
                }
            };
        }

        return new PointValueSnapshot
        {
            PointId = pointId,
            PointCode = pointCode,
            DeviceCode = deviceCode,
            Value = null,
            ValueText = null,
            Quality = DataQuality.Good,
            Timestamp = now
        };
    }

    private static PointValueSnapshot CreateSnapshot(
        string pointCode,
        string deviceCode,
        int pointId,
        DataQuality quality,
        DateTime now,
        string message)
    {
        return new PointValueSnapshot
        {
            PointId = pointId,
            PointCode = pointCode,
            DeviceCode = deviceCode,
            Value = null,
            ValueText = message,
            Quality = quality,
            Timestamp = now
        };
    }

    private static bool TryParseWriteValue(string? writeValue, out double? value, out string? error)
    {
        value = null;
        error = null;

        if (string.IsNullOrWhiteSpace(writeValue))
        {
            error = "写入值为空";
            return false;
        }

        var trimmed = writeValue.Trim();
        if (bool.TryParse(trimmed, out var booleanValue))
        {
            value = booleanValue ? 1 : 0;
            return true;
        }

        if (double.TryParse(trimmed, System.Globalization.NumberStyles.Float, System.Globalization.CultureInfo.InvariantCulture, out var numericValue))
        {
            value = numericValue;
            return true;
        }

        error = $"写入值无法解析为数值: {trimmed}";
        return false;
    }

    private sealed record ParsedSignal(SimulatedSignal? Signal, string? Error);
}
