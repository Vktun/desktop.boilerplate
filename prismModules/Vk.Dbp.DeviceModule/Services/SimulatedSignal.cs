namespace Vk.Dbp.DeviceModule.Services;

/// <summary>
/// 模拟波形种类
/// </summary>
internal enum SignalKind
{
    /// <summary>
    /// 正弦波：sine(min,max,periodSec)，余弦相位保证值域为闭区间 [min,max]
    /// </summary>
    Sine,

    /// <summary>
    /// 随机值：rand(min,max)，同一整秒内稳定（种子 = 点位ID ^ 秒桶）
    /// </summary>
    Rand,

    /// <summary>
    /// 锯齿波：ramp(min,max,periodSec)，周期折返
    /// </summary>
    Ramp,

    /// <summary>
    /// 方波：pulse(periodSec)，前半周期 0、后半周期 1
    /// </summary>
    Pulse,

    /// <summary>
    /// 常量：const(v)
    /// </summary>
    Const
}

/// <summary>
/// 模拟驱动地址微语法的解析与确定性求值。
/// 语法（大小写不敏感、容忍空白）：sine(min,max,periodSec) / rand(min,max) / ramp(min,max,periodSec) / pulse(periodSec) / const(v)。
/// </summary>
internal sealed class SimulatedSignal
{
    // 波形相位的时间原点：取固定历史时刻，保证同一时刻任意进程求值结果一致（可测、可复现）
    private static readonly DateTime Epoch = new(2000, 1, 1, 0, 0, 0, DateTimeKind.Local);

    private SimulatedSignal(SignalKind kind, double min, double max, double periodSeconds, double constant)
    {
        Kind = kind;
        Min = min;
        Max = max;
        PeriodSeconds = periodSeconds;
        Constant = constant;
    }

    /// <summary>
    /// 波形种类
    /// </summary>
    public SignalKind Kind { get; }

    /// <summary>
    /// 下限
    /// </summary>
    public double Min { get; }

    /// <summary>
    /// 上限
    /// </summary>
    public double Max { get; }

    /// <summary>
    /// 周期（秒）
    /// </summary>
    public double PeriodSeconds { get; }

    /// <summary>
    /// 常量值（const 专用）
    /// </summary>
    public double Constant { get; }

    /// <summary>
    /// 解析地址串。成功时 <paramref name="signal"/> 为波形对象、<paramref name="error"/> 为 null；
    /// 失败时 <paramref name="signal"/> 为 null、<paramref name="error"/> 为中文错误描述。
    /// </summary>
    public static bool TryParse(string? address, out SimulatedSignal? signal, out string? error)
    {
        signal = null;
        error = null;

        if (string.IsNullOrWhiteSpace(address))
        {
            error = "未配置驱动地址";
            return false;
        }

        var trimmed = address.Trim();
        var openIndex = trimmed.IndexOf('(');
        if (openIndex <= 0 || !trimmed.EndsWith(")"))
        {
            error = $"地址格式应为 name(参数,...)：{trimmed}";
            return false;
        }

        var name = trimmed[..openIndex].Trim().ToLowerInvariant();
        var argumentText = trimmed[(openIndex + 1)..^1];
        var arguments = argumentText
            .Split(',', StringSplitOptions.TrimEntries)
            .Where(part => part.Length > 0)
            .Select(part => double.TryParse(part, System.Globalization.NumberStyles.Float, System.Globalization.CultureInfo.InvariantCulture, out var value) ? value : (double?)null)
            .ToList();

        if (arguments.Any(argument => argument is null))
        {
            error = $"参数必须为数字：{argumentText}";
            return false;
        }

        var values = arguments.Select(argument => argument!.Value).ToList();

        switch (name)
        {
            case "sine" when values.Count == 3 && IsValidRange(values[0], values[1]) && values[2] > 0:
                signal = new SimulatedSignal(SignalKind.Sine, values[0], values[1], values[2], 0);
                return true;

            case "rand" when values.Count == 2 && IsValidRange(values[0], values[1]):
                signal = new SimulatedSignal(SignalKind.Rand, values[0], values[1], 0, 0);
                return true;

            case "ramp" when values.Count == 3 && IsValidRange(values[0], values[1]) && values[2] > 0:
                signal = new SimulatedSignal(SignalKind.Ramp, values[0], values[1], values[2], 0);
                return true;

            case "pulse" when values.Count == 1 && values[0] > 0:
                signal = new SimulatedSignal(SignalKind.Pulse, 0, 1, values[0], 0);
                return true;

            case "const" when values.Count == 1:
                signal = new SimulatedSignal(SignalKind.Const, values[0], values[0], 0, values[0]);
                return true;

            default:
                error = BuildGrammarError(name, values.Count);
                return false;
        }
    }

    /// <summary>
    /// 按时刻确定性求值。<paramref name="seedKey"/> 为点位稳定种子（如 PointId），用于 rand 波形。
    /// </summary>
    public double Evaluate(DateTime now, int seedKey)
    {
        var seconds = (now - Epoch).TotalSeconds;

        switch (Kind)
        {
            case SignalKind.Sine:
            {
                var phase = Math.Cos(2.0 * Math.PI * seconds / PeriodSeconds);
                return Min + (Max - Min) * (0.5 + 0.5 * phase);
            }

            case SignalKind.Rand:
            {
                var secondBucket = (int)(now.Ticks / TimeSpan.TicksPerSecond);
                var random = new Random(seedKey ^ secondBucket);
                return Min + (Max - Min) * random.NextDouble();
            }

            case SignalKind.Ramp:
            {
                var phase = seconds / PeriodSeconds;
                phase -= Math.Floor(phase);
                return Min + (Max - Min) * phase;
            }

            case SignalKind.Pulse:
            {
                var phase = seconds / PeriodSeconds;
                phase -= Math.Floor(phase);
                return phase < 0.5 ? 0.0 : 1.0;
            }

            case SignalKind.Const:
                return Constant;

            default:
                return Constant;
        }
    }

    private static bool IsValidRange(double min, double max)
    {
        return min < max;
    }

    private static string BuildGrammarError(string name, int argumentCount)
    {
        string[] known =
        {
            "sine(min,max,periodSec)",
            "rand(min,max)",
            "ramp(min,max,periodSec)",
            "pulse(periodSec)",
            "const(v)"
        };

        if (known.Any(sample => sample.StartsWith(name, StringComparison.Ordinal)))
        {
            return $"参数个数或取值不合法（收到 {argumentCount} 个）";
        }

        return $"未知波形 {name}，支持：{string.Join(" / ", known)}";
    }
}
