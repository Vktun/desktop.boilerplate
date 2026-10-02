namespace Vk.Dbp.Tests.Unit.Services;

/// <summary>
/// 可手动推进时间的假时间源（免引 Microsoft.Extensions.TimeProvider.Testing 包）。
/// GetUtcNow 返回固定值，Advance 推进；GetLocalNow 由基类按本地时区换算。
/// </summary>
internal sealed class FakeTimeProvider : TimeProvider
{
    private DateTimeOffset _utcNow;

    public FakeTimeProvider(DateTimeOffset start)
    {
        _utcNow = start.ToUniversalTime();
    }

    public void Advance(TimeSpan delta)
    {
        _utcNow = _utcNow.Add(delta);
    }

    public override DateTimeOffset GetUtcNow() => _utcNow;
}

/// <summary>
/// 时序测试助手：以 10ms 轮询等待条件成立，超时抛出带诊断信息的异常（替代脆弱的 Task.Delay 猜时长）。
/// </summary>
internal static class TimingTestHelper
{
    public static async Task WaitUntilAsync(Func<bool> condition, TimeSpan? timeout = null, string? diagnostics = null)
    {
        var deadline = DateTime.UtcNow + (timeout ?? TimeSpan.FromSeconds(5));
        while (DateTime.UtcNow < deadline)
        {
            if (condition())
            {
                return;
            }

            await Task.Delay(TimeSpan.FromMilliseconds(10));
        }

        throw new TimeoutException($"等待条件超时（{timeout ?? TimeSpan.FromSeconds(5)}）。{diagnostics}");
    }
}
