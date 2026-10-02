using Microsoft.Extensions.DependencyInjection;
using Vktun.IoT.Connector;

namespace Vk.Dbp.DeviceModule.Services;

/// <summary>
/// vktun.iot.connector 采集门面的共享宿主。
/// SDK 按多设备单门面设计（AddVktunIoTConnector + IIoTDataCollector），
/// 本项目用 Prism/Unity 容器，故在此内部自持一个 Microsoft DI 容器装载 SDK，
/// 并用引用计数管理门面的 Initialize/Start/Stop 生命周期：
/// 第一个设备接入时启动，最后一个设备释放时停止，多台 Modbus 设备共享同一采集运行时。
/// </summary>
public sealed class IotCollectorHost
{
    private readonly SemaphoreSlim _lock = new(1, 1);
    private ServiceProvider? _serviceProvider;
    private IIoTDataCollector? _collector;
    private int _referenceCount;

    /// <summary>
    /// 获取采集门面；首次获取时初始化并启动 SDK 运行时（引用计数 +1）
    /// </summary>
    public async Task<IIoTDataCollector> AcquireAsync(CancellationToken cancellationToken)
    {
        await _lock.WaitAsync(cancellationToken);
        try
        {
            _referenceCount++;

            if (_collector is null)
            {
                var services = new ServiceCollection();
                services.AddVktunIoTConnector();
                _serviceProvider = services.BuildServiceProvider();
                _collector = _serviceProvider.GetRequiredService<IIoTDataCollector>();
                await _collector.InitializeAsync();
                await _collector.StartAsync(cancellationToken);
            }

            return _collector;
        }
        finally
        {
            _lock.Release();
        }
    }

    /// <summary>
    /// 释放一次引用；最后一个引用释放时停止并销毁 SDK 运行时
    /// </summary>
    public async Task ReleaseAsync()
    {
        await _lock.WaitAsync();
        try
        {
            if (_referenceCount == 0)
            {
                return;
            }

            _referenceCount--;

            if (_referenceCount == 0 && _collector is not null)
            {
                try
                {
                    await _collector.StopAsync();
                }
                finally
                {
                    await _collector.DisposeAsync();
                    await (_serviceProvider?.DisposeAsync() ?? ValueTask.CompletedTask);
                    _serviceProvider = null;
                    _collector = null;
                }
            }
        }
        finally
        {
            _lock.Release();
        }
    }
}
