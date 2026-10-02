using System.IO;
using System.Text.Json;
using System.Text.Json.Serialization;
using Serilog;
using Vktun.IoT.Connector;
using Vktun.IoT.Connector.Core.Enums;
using Vktun.IoT.Connector.Core.Models;
using Vk.Dbp.Contracts.Industrial;
using Vk.Dbp.DeviceModule.Models;

namespace Vk.Dbp.DeviceModule.Services;

/// <summary>
/// 基于 vktun.iot.connector（IIoTDataCollector 门面）的真实协议驱动，当前覆盖 Modbus TCP / RTU。
/// 连接配置 JSON：{"ip":"127.0.0.1","port":502,"slaveId":1,"pollIntervalMs":1000}（RTU 额外：serialPort/baudRate）。
/// 点位地址语法见 <see cref="ModbusAddressParser"/>（区域:地址[:类型]，如 HR:100:Float）。
/// 采集点表由设备点位定义动态生成 Modbus 模板（临时 JSON 文件），写入走 SendCommandAsync（单线圈/单寄存器）。
/// </summary>
public sealed class IotConnectorDriver : IProtocolDriver
{
    private static readonly JsonSerializerOptions TemplateSerializerOptions = new()
    {
        WriteIndented = true,
        Converters = { new JsonStringEnumConverter() }
    };

    private readonly IotCollectorHost _host;
    private readonly DeviceConnectionInfo _device;
    private readonly string _templatePath;

    private IIoTDataCollector? _collector;
    private bool _isConnected;

    /// <summary>
    /// 构造 vktun.iot.connector 驱动
    /// </summary>
    public IotConnectorDriver(IotCollectorHost host, DeviceConnectionInfo device)
    {
        _host = host ?? throw new ArgumentNullException(nameof(host));
        _device = device ?? throw new ArgumentNullException(nameof(device));
        _templatePath = Path.Combine(Path.GetTempPath(), $"dbp-iot-template-{SanitizeFileName(device.Code)}.json");
    }

    /// <inheritdoc />
    public string ProtocolType => _device.ProtocolType;

    /// <inheritdoc />
    public bool IsConnected => _isConnected;

    /// <inheritdoc />
    public async Task ConnectAsync(CancellationToken cancellationToken)
    {
        var collector = await _host.AcquireAsync(cancellationToken);

        await WriteTemplateFileAsync();

        var connection = ParseConnectionConfig();
        var deviceInfo = new DeviceInfo
        {
            DeviceId = _device.Code,
            DeviceName = _device.Code,
            CommunicationType = IsRtu ? CommunicationType.Serial : CommunicationType.Tcp,
            ConnectionMode = ConnectionMode.Client,
            IpAddress = connection.Ip ?? "127.0.0.1",
            Port = connection.Port ?? 502,
            SerialPort = connection.SerialPort ?? string.Empty,
            BaudRate = connection.BaudRate ?? 9600,
            SlaveId = connection.SlaveId ?? 1,
            ProtocolType = IsRtu ? global::Vktun.IoT.Connector.Core.Enums.ProtocolType.ModbusRtu : global::Vktun.IoT.Connector.Core.Enums.ProtocolType.ModbusTcp,
            ProtocolId = BuildProtocolId(_device.Code),
            ProtocolConfigPath = _templatePath
        };

        if (!await collector.AddDeviceAsync(deviceInfo))
        {
            throw new IOException($"向采集运行时注册设备失败: {_device.Code}");
        }

        if (!await collector.ConnectDeviceAsync(_device.Code))
        {
            await collector.RemoveDeviceAsync(_device.Code);
            throw new IOException($"设备连接失败: {_device.Code}");
        }

        _collector = collector;
        _isConnected = true;
    }

    /// <inheritdoc />
    public async Task DisconnectAsync()
    {
        if (!_isConnected || _collector is null)
        {
            return;
        }

        _isConnected = false;
        await _collector.DisconnectDeviceAsync(_device.Code);
    }

    /// <inheritdoc />
    public async Task<IReadOnlyList<PointValueSnapshot>> ReadPointsAsync(
        IReadOnlyList<PointReadRequest> requests,
        CancellationToken cancellationToken)
    {
        if (_collector is null || !_isConnected)
        {
            return requests
                .Select(request => BadSnapshot(request, "驱动未连接"))
                .ToList();
        }

        DeviceData? data;
        data = await _collector.CollectDataAsync(_device.Code, cancellationToken);

        if (data is null || !data.IsValid)
        {
            var message = data?.ErrorMessage ?? "采集返回无效数据";
            return requests
                .Select(request => BadSnapshot(request, message))
                .ToList();
        }

        var byName = data.DataItems
            .Where(item => item.IsValid)
            .GroupBy(item => item.PointName, StringComparer.OrdinalIgnoreCase)
            .ToDictionary(group => group.Key, group => group.First(), StringComparer.OrdinalIgnoreCase);

        var now = DateTime.Now;
        var snapshots = new List<PointValueSnapshot>(requests.Count);
        foreach (var request in requests)
        {
            if (!byName.TryGetValue(request.PointCode, out var item))
            {
                snapshots.Add(new PointValueSnapshot
                {
                    PointId = request.PointId,
                    PointCode = request.PointCode,
                    DeviceCode = _device.Code,
                    Quality = DataQuality.Uncertain,
                    ValueText = "采集数据缺失",
                    Timestamp = now
                });
                continue;
            }

            snapshots.Add(ToSnapshot(request, item, now));
        }

        return snapshots;
    }

    /// <inheritdoc />
    public async Task<PointValueSnapshot> WritePointAsync(PointWriteRequest request, CancellationToken cancellationToken)
    {
        if (_collector is null || !_isConnected)
        {
            return WriteSnapshot(request, DataQuality.Bad, "驱动未连接");
        }

        if (!ModbusAddressParser.TryParse(request.Address, out var address, out var error))
        {
            return WriteSnapshot(request, DataQuality.Bad, $"地址解析失败: {error}");
        }

        var command = new global::Vktun.IoT.Connector.Core.Models.DeviceCommand
        {
            DeviceId = _device.Code,
            CommandName = address!.RegisterType == ModbusRegisterType.Coil ? "WriteSingleCoil" : "WriteSingleRegister",
            Parameters = new Dictionary<string, object>()
        };
        command.Parameters["Address"] = (int)address.Address;

        if (address.RegisterType == ModbusRegisterType.Coil)
        {
            if (!TryParseCoilValue(request.Value, out var coilValue))
            {
                return WriteSnapshot(request, DataQuality.Bad, $"线圈写入值必须为布尔或 0/1: {request.Value}");
            }

            command.Parameters["Value"] = coilValue;
        }
        else
        {
            if (!double.TryParse(request.Value, System.Globalization.NumberStyles.Float, System.Globalization.CultureInfo.InvariantCulture, out var numericValue))
            {
                return WriteSnapshot(request, DataQuality.Bad, $"寄存器写入值必须为数值: {request.Value}");
            }

            command.Parameters["Value"] = (ushort)Math.Round(numericValue);
        }

        var result = await _collector.SendCommandAsync(command, cancellationToken);
        if (!result.Success)
        {
            return WriteSnapshot(request, DataQuality.Bad, result.ErrorMessage ?? "写入失败");
        }

        double? writtenValue = address.RegisterType == ModbusRegisterType.Coil
            ? IsTruthy(request.Value) ? 1 : 0
            : double.TryParse(request.Value, System.Globalization.NumberStyles.Float, System.Globalization.CultureInfo.InvariantCulture, out var value) ? value : null;
        return WriteSnapshot(request, DataQuality.Good, null, writtenValue);
    }

    /// <inheritdoc />
    public async ValueTask DisposeAsync()
    {
        if (_collector is not null)
        {
            _isConnected = false;
            try
            {
                await _collector.DisconnectDeviceAsync(_device.Code);
                await _collector.RemoveDeviceAsync(_device.Code);
            }
            catch (Exception ex)
            {
                Log.Warning(ex, "设备 {DeviceCode} 从采集运行时摘除失败", _device.Code);
            }
            finally
            {
                await _host.ReleaseAsync();
                _collector = null;
            }
        }

        TryDeleteTemplateFile();
    }

    private bool IsRtu => string.Equals(_device.ProtocolType, ProtocolTypes.ModbusRtu, StringComparison.OrdinalIgnoreCase);

    private string BuildProtocolId(string deviceCode)
    {
        return $"dbp-{SanitizeFileName(deviceCode)}";
    }

    private async Task WriteTemplateFileAsync()
    {
        // SDK 模板文件格式 = 扁平的 ModbusConfig 定义 JSON（BuildProtocolConfig 把整个文件作为 DefinitionJson
        // 与 ParseRules["ModbusConfig"] 使用，ProtocolType 由 ModbusType 字段推断）；
        // 不要包 ProtocolConfig{DefinitionJson} 信封——外层空的 Points 列表会把真实点位盖掉
        var modbusConfig = new ModbusConfig
        {
            ProtocolId = BuildProtocolId(_device.Code),
            ProtocolName = $"{_device.Code} Modbus 点表",
            ModbusType = IsRtu ? ModbusType.Rtu : ModbusType.Tcp,
            SlaveId = (byte)(ParseConnectionConfig().SlaveId ?? 1)
        };

        foreach (var point in _device.Points)
        {
            if (!ModbusAddressParser.TryCreatePointConfig(point, out var pointConfig, out var error))
            {
                Log.Warning("设备 {DeviceCode} 点位 {PointCode} 地址无效，未进入采集模板: {Error}",
                    _device.Code, point.Code, error);
                continue;
            }

            modbusConfig.Points.Add(pointConfig!);
        }

        if (modbusConfig.Points.Count == 0)
        {
            throw new InvalidOperationException($"设备 {_device.Code} 无有效 Modbus 点位，无法生成采集模板");
        }

        await File.WriteAllTextAsync(_templatePath, JsonSerializer.Serialize(modbusConfig, TemplateSerializerOptions));
    }

    private void TryDeleteTemplateFile()
    {
        try
        {
            if (File.Exists(_templatePath))
            {
                File.Delete(_templatePath);
            }
        }
        catch (IOException ex)
        {
            Log.Debug(ex, "清理设备模板文件失败: {Path}", _templatePath);
        }
        catch (UnauthorizedAccessException ex)
        {
            Log.Debug(ex, "清理设备模板文件失败: {Path}", _templatePath);
        }
    }

    private (string? Ip, int? Port, int? SlaveId, string? SerialPort, int? BaudRate) ParseConnectionConfig()
    {
        if (string.IsNullOrWhiteSpace(_device.ConnectionConfig))
        {
            return (null, null, null, null, null);
        }

        try
        {
            using var document = JsonDocument.Parse(_device.ConnectionConfig);
            var root = document.RootElement;

            string? ip = root.TryGetProperty("ip", out var ipElement) && ipElement.ValueKind == JsonValueKind.String
                ? ipElement.GetString()
                : null;
            int? port = root.TryGetProperty("port", out var portElement) && portElement.ValueKind == JsonValueKind.Number
                ? portElement.GetInt32()
                : null;
            int? slaveId = root.TryGetProperty("slaveId", out var slaveElement) && slaveElement.ValueKind == JsonValueKind.Number
                ? slaveElement.GetInt32()
                : null;
            string? serialPort = root.TryGetProperty("serialPort", out var serialElement) && serialElement.ValueKind == JsonValueKind.String
                ? serialElement.GetString()
                : null;
            int? baudRate = root.TryGetProperty("baudRate", out var baudElement) && baudElement.ValueKind == JsonValueKind.Number
                ? baudElement.GetInt32()
                : null;

            return (ip, port, slaveId, serialPort, baudRate);
        }
        catch (JsonException)
        {
            return (null, null, null, null, null);
        }
    }

    private PointValueSnapshot ToSnapshot(PointReadRequest request, DataPoint item, DateTime fallbackTimestamp)
    {
        double? numericValue = null;
        string? textValue = null;

        switch (item.Value)
        {
            case null:
                break;
            case bool boolValue:
                numericValue = boolValue ? 1 : 0;
                textValue = boolValue ? "开" : "关";
                break;
            case IConvertible convertible:
                try
                {
                    numericValue = convertible.ToDouble(System.Globalization.CultureInfo.InvariantCulture);
                    textValue = numericValue.Value.ToString("F2");
                }
                catch (Exception ex) when (ex is FormatException or OverflowException or InvalidCastException)
                {
                    numericValue = null;
                    textValue = convertible.ToString();
                }

                break;
            default:
                textValue = item.Value.ToString();
                break;
        }

        return new PointValueSnapshot
        {
            PointId = request.PointId,
            PointCode = request.PointCode,
            DeviceCode = _device.Code,
            Value = numericValue,
            ValueText = textValue,
            Quality = item.IsValid ? DataQuality.Good : DataQuality.Bad,
            Timestamp = item.Timestamp == default ? fallbackTimestamp : item.Timestamp
        };
    }

    private PointValueSnapshot BadSnapshot(PointReadRequest request, string message)
    {
        return new PointValueSnapshot
        {
            PointId = request.PointId,
            PointCode = request.PointCode,
            DeviceCode = _device.Code,
            Quality = DataQuality.Bad,
            ValueText = message,
            Timestamp = DateTime.Now
        };
    }

    private PointValueSnapshot WriteSnapshot(PointWriteRequest request, DataQuality quality, string? message, double? value = null)
    {
        return new PointValueSnapshot
        {
            PointId = 0,
            PointCode = request.PointCode,
            DeviceCode = _device.Code,
            Value = value,
            ValueText = message,
            Quality = quality,
            Timestamp = DateTime.Now
        };
    }

    private static bool TryParseCoilValue(string text, out bool value)
    {
        if (bool.TryParse(text, out var boolValue))
        {
            value = boolValue;
            return true;
        }

        if (int.TryParse(text, out var intValue) && intValue is 0 or 1)
        {
            value = intValue == 1;
            return true;
        }

        value = false;
        return false;
    }

    private static bool IsTruthy(string value)
    {
        return bool.TryParse(value, out var boolValue)
            ? boolValue
            : int.TryParse(value, out var intValue) && intValue != 0;
    }

    private static string SanitizeFileName(string value)
    {
        var invalid = Path.GetInvalidFileNameChars();
        var chars = value.Select(c => invalid.Contains(c) ? '_' : c).ToArray();
        return new string(chars);
    }
}
