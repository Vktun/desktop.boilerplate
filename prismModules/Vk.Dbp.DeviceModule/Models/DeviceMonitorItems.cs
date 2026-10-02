using System.Windows.Media;
using Prism.Mvvm;
using Vk.Dbp.Contracts.Industrial;

namespace Vk.Dbp.DeviceModule.Models;

/// <summary>
/// 设备列表行视图模型（状态色圆点 + 编码/名称）
/// </summary>
public sealed class DeviceListItemViewModel : BindableBase
{
    private string _statusText = "停止";
    private Brush _statusBrush = new SolidColorBrush(Color.FromRgb(148, 163, 184));

    /// <summary>
    /// 设备ID
    /// </summary>
    public int Id { get; init; }

    /// <summary>
    /// 设备编码
    /// </summary>
    public required string Code { get; init; }

    /// <summary>
    /// 设备名称
    /// </summary>
    public required string Name { get; init; }

    /// <summary>
    /// 协议显示名（Simulated/ModbusTcp/…）
    /// </summary>
    public required string ProtocolTypeText { get; init; }

    /// <summary>
    /// 运行状态文本
    /// </summary>
    public string StatusText
    {
        get => _statusText;
        private set => SetProperty(ref _statusText, value);
    }

    /// <summary>
    /// 状态圆点画刷
    /// </summary>
    public Brush StatusBrush
    {
        get => _statusBrush;
        private set => SetProperty(ref _statusBrush, value);
    }

    /// <summary>
    /// 按运行时状态刷新显示
    /// </summary>
    public void ApplyStatus(DeviceRuntimeStatus status)
    {
        (StatusText, var color) = status switch
        {
            DeviceRuntimeStatus.Running => ("运行中", Color.FromRgb(34, 197, 94)),
            DeviceRuntimeStatus.Connecting => ("连接中", Color.FromRgb(249, 158, 11)),
            DeviceRuntimeStatus.Faulted => ("故障", Color.FromRgb(239, 68, 68)),
            DeviceRuntimeStatus.Disabled => ("已禁用", Color.FromRgb(148, 163, 184)),
            _ => ("停止", Color.FromRgb(148, 163, 184))
        };
        StatusBrush = new SolidColorBrush(color);
        StatusBrush.Freeze();
    }
}

/// <summary>
/// 点位表格行视图模型（实时值/质量/时间戳/越限高亮）
/// </summary>
public sealed class PointRowViewModel : BindableBase
{
    private string _displayValue = "--";
    private string _qualityText = "--";
    private string _timestampText = "--";
    private bool _isHighAlarm;
    private bool _isLowAlarm;
    private bool _isBadQuality;

    /// <summary>
    /// 点位ID
    /// </summary>
    public int Id { get; init; }

    /// <summary>
    /// 点位编码
    /// </summary>
    public required string Code { get; init; }

    /// <summary>
    /// 点位名称
    /// </summary>
    public required string Name { get; init; }

    /// <summary>
    /// 工程单位
    /// </summary>
    public string? Unit { get; init; }

    /// <summary>
    /// 高限阈值（显示用）
    /// </summary>
    public decimal? AlarmHigh { get; init; }

    /// <summary>
    /// 低限阈值（显示用）
    /// </summary>
    public decimal? AlarmLow { get; init; }

    /// <summary>
    /// 实时值显示（含单位）
    /// </summary>
    public string DisplayValue
    {
        get => _displayValue;
        private set => SetProperty(ref _displayValue, value);
    }

    /// <summary>
    /// 质量文本
    /// </summary>
    public string QualityText
    {
        get => _qualityText;
        private set => SetProperty(ref _qualityText, value);
    }

    /// <summary>
    /// 时间戳文本（HH:mm:ss）
    /// </summary>
    public string TimestampText
    {
        get => _timestampText;
        private set => SetProperty(ref _timestampText, value);
    }

    /// <summary>
    /// 高限越限高亮
    /// </summary>
    public bool IsHighAlarm
    {
        get => _isHighAlarm;
        private set => SetProperty(ref _isHighAlarm, value);
    }

    /// <summary>
    /// 低限越限高亮
    /// </summary>
    public bool IsLowAlarm
    {
        get => _isLowAlarm;
        private set => SetProperty(ref _isLowAlarm, value);
    }

    /// <summary>
    /// Bad 质量高亮
    /// </summary>
    public bool IsBadQuality
    {
        get => _isBadQuality;
        private set => SetProperty(ref _isBadQuality, value);
    }

    /// <summary>
    /// 高限阈值显示文本
    /// </summary>
    public string AlarmHighText => AlarmHigh is { } high ? $"{high:0.##}" : "--";

    /// <summary>
    /// 低限阈值显示文本
    /// </summary>
    public string AlarmLowText => AlarmLow is { } low ? $"{low:0.##}" : "--";

    /// <summary>
    /// 用最新快照刷新行显示
    /// </summary>
    public void ApplySnapshot(PointValueSnapshot snapshot)
    {
        if (snapshot.Quality != DataQuality.Good)
        {
            DisplayValue = snapshot.ValueText ?? "--";
        }
        else if (snapshot.Value is { } numericValue)
        {
            DisplayValue = Unit is null ? numericValue.ToString("0.##") : $"{numericValue:0.##} {Unit}";
        }
        else
        {
            DisplayValue = snapshot.ValueText ?? "--";
        }

        QualityText = snapshot.Quality switch
        {
            DataQuality.Good => "良好",
            DataQuality.Uncertain => "可疑",
            _ => "坏值"
        };
        TimestampText = snapshot.Timestamp.ToString("HH:mm:ss");
        IsBadQuality = snapshot.Quality == DataQuality.Bad;
        IsHighAlarm = snapshot.Quality == DataQuality.Good
                      && snapshot.Value is { } value
                      && AlarmHigh is { } high
                      && value > (double)high;
        IsLowAlarm = snapshot.Quality == DataQuality.Good
                     && snapshot.Value is { } lowValue
                     && AlarmLow is { } low
                     && lowValue < (double)low;
    }
}
