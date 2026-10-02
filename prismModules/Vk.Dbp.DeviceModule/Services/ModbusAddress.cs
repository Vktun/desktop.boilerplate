using Vk.Dbp.Contracts.Industrial;
using Vktun.IoT.Connector.Core.Enums;
using Vktun.IoT.Connector.Core.Models;

namespace Vk.Dbp.DeviceModule.Services;

/// <summary>
/// Modbus 点位地址解析结果（区域 + 寄存器地址 + 可选库类型覆盖）
/// </summary>
/// <param name="RegisterType">寄存器区域</param>
/// <param name="Address">寄存器地址（0 起）</param>
/// <param name="DataTypeOverride">显式指定的库数据类型（null = 按点位 DataType 映射）</param>
internal sealed record ModbusAddress(ModbusRegisterType RegisterType, ushort Address, DataType? DataTypeOverride);

/// <summary>
/// Modbus 点位地址微语法解析与类型映射。
/// 语法：&lt;区域&gt;:&lt;地址&gt;[:&lt;库数据类型&gt;]，区域别名 HR/IR/C/DI（大小写不敏感），如 "HR:100:Float"、"C:5"。
/// </summary>
internal static class ModbusAddressParser
{
    public static bool TryParse(string? address, out ModbusAddress? parsed, out string? error)
    {
        parsed = null;
        error = null;

        if (string.IsNullOrWhiteSpace(address))
        {
            error = "未配置驱动地址";
            return false;
        }

        var segments = address.Trim().Split(':', StringSplitOptions.TrimEntries | StringSplitOptions.RemoveEmptyEntries);
        if (segments.Length is < 2 or > 3)
        {
            error = $"Modbus 地址格式应为 区域:地址[:类型]：{address}";
            return false;
        }

        if (!TryParseRegisterType(segments[0], out var registerType))
        {
            error = $"未知寄存器区域 {segments[0]}（支持 HR/IR/C/DI）";
            return false;
        }

        if (!ushort.TryParse(segments[1], out var registerAddress))
        {
            error = $"寄存器地址必须为 0-65535：{segments[1]}";
            return false;
        }

        DataType? typeOverride = null;
        if (segments.Length == 3)
        {
            if (!Enum.TryParse<DataType>(segments[2], ignoreCase: true, out var libraryType))
            {
                error = $"未知数据类型 {segments[2]}";
                return false;
            }

            typeOverride = libraryType;
        }

        parsed = new ModbusAddress(registerType, registerAddress, typeOverride);
        return true;
    }

    /// <summary>
    /// 平台点位类型 → SDK 库数据类型映射
    /// </summary>
    public static DataType ToLibraryDataType(PointDataType dataType)
    {
        return dataType switch
        {
            PointDataType.Boolean => DataType.Bool,
            PointDataType.Int32 => DataType.Int32,
            PointDataType.Float => DataType.Float,
            PointDataType.String => DataType.Ascii,
            _ => DataType.Double
        };
    }

    /// <summary>
    /// 解析结果 → SDK Modbus 点配置
    /// </summary>
    public static bool TryCreatePointConfig(
        Models.PointDefinition point,
        out ModbusPointConfig? pointConfig,
        out string? error)
    {
        pointConfig = null;

        if (!TryParse(point.Address, out var parsed, out error))
        {
            return false;
        }

        var libraryType = parsed!.DataTypeOverride ?? ToLibraryDataType(point.DataType);
        pointConfig = new ModbusPointConfig
        {
            PointName = point.Code,
            RegisterType = parsed.RegisterType,
            Address = parsed.Address,
            DataType = libraryType,
            Unit = point.Unit ?? string.Empty,
            IsReadOnly = false
        };
        return true;
    }

    private static bool TryParseRegisterType(string token, out ModbusRegisterType registerType)
    {
        switch (token.ToUpperInvariant())
        {
            case "HR":
            case "HOLDINGREGISTER":
                registerType = ModbusRegisterType.HoldingRegister;
                return true;
            case "IR":
            case "INPUTREGISTER":
                registerType = ModbusRegisterType.InputRegister;
                return true;
            case "C":
            case "COIL":
                registerType = ModbusRegisterType.Coil;
                return true;
            case "DI":
            case "DISCRETEINPUT":
                registerType = ModbusRegisterType.DiscreteInput;
                return true;
            default:
                registerType = default;
                return false;
        }
    }
}
