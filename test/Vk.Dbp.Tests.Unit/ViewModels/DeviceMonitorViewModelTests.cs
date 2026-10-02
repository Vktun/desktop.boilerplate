using FluentAssertions;
using Moq;
using Prism.Events;
using Vk.Dbp.Contracts.Industrial;
using Vk.Dbp.DeviceModule.Models;
using Vk.Dbp.DeviceModule.Services;
using Vk.Dbp.DeviceModule.ViewModels;
using Vk.Dbp.Tests.Unit.Services;
using Xunit;

namespace Vk.Dbp.Tests.Unit.ViewModels;

public sealed class DeviceMonitorViewModelTests : IDisposable
{
    private readonly Mock<IRealtimeDataService> _realtimeService = new();
    private readonly Mock<IDeviceCatalogService> _catalogService = new();
    private readonly Mock<IDeviceRuntimeService> _runtimeService = new();
    private readonly EventAggregator _eventAggregator = new();

    public DeviceMonitorViewModelTests()
    {
        // ThreadOption.UIThread 订阅要求非空同步上下文（AppNotificationViewModelTests 同款套路）
        SynchronizationContext.SetSynchronizationContext(new SynchronizationContext());

        _catalogService
            .Setup(service => service.GetDevicesAsync(It.IsAny<bool>()))
            .ReturnsAsync(new List<DeviceSummary>
            {
                new() { Id = 1, Code = "SIM-TEST-1", Name = "模拟设备", ProtocolType = "Simulated", IsEnabled = true }
            });
        _catalogService
            .Setup(service => service.GetPointsAsync(1))
            .ReturnsAsync(new List<PointDefinition>
            {
                new() { Id = 11, DeviceId = 1, Code = "TEMP-01", Name = "炉膛温度", DataType = PointDataType.Double, Address = "sine(20,80,60)", Unit = "℃", AlarmHigh = 75m, IsEnabled = true },
                new() { Id = 12, DeviceId = 1, Code = "LEVEL-01", Name = "料位", DataType = PointDataType.Double, Address = "const(62.5)", Unit = "%", IsEnabled = true }
            });
        _runtimeService
            .Setup(service => service.GetDeviceStatus(1))
            .Returns(DeviceRuntimeStatus.Running);
        _runtimeService
            .Setup(service => service.GetAllDeviceStatuses())
            .Returns(new Dictionary<int, DeviceRuntimeStatus> { [1] = DeviceRuntimeStatus.Running });
    }

    public void Dispose()
    {
        SynchronizationContext.SetSynchronizationContext(null);
    }

    private DeviceMonitorViewModel CreateViewModel()
    {
        return new DeviceMonitorViewModel(
            _realtimeService.Object,
            _catalogService.Object,
            _runtimeService.Object,
            _eventAggregator);
    }

    private static PointValueSnapshot Snapshot(string code, double value, DateTime? timestamp = null)
    {
        return new PointValueSnapshot
        {
            PointId = 11,
            PointCode = code,
            DeviceCode = "SIM-TEST-1",
            Value = value,
            ValueText = value.ToString("F2"),
            Quality = DataQuality.Good,
            Timestamp = timestamp ?? DateTime.Now
        };
    }

    [Fact]
    public async Task LoadDevicesAsync_WithCatalog_PopulatesDevicesAndPoints()
    {
        var viewModel = CreateViewModel();
        _realtimeService
            .Setup(service => service.GetDeviceSnapshots(1))
            .Returns(new[] { Snapshot("TEMP-01", 42.5) });

        viewModel.RefreshCommand.Execute();
        await TimingTestHelper.WaitUntilAsync(
            () => viewModel.Points.Count == 2,
            diagnostics: "刷新后应装载设备与点位");

        viewModel.Devices.Should().ContainSingle("应装载一台设备");
        viewModel.Devices[0].StatusText.Should().Be("运行中", "设备行应显示运行时状态");
        viewModel.SelectedDevice.Should().NotBeNull("装载后应默认选中第一台设备");
        viewModel.SelectedPoint.Should().NotBeNull("装载后应默认选中一个点位");
        viewModel.EngineStatusText.Should().Contain("1/1", "引擎状态摘要应统计运行设备");
        viewModel.Points[0].DisplayValue.Should().Be("42.5 ℃", "初值快照应填充实时值列");
    }

    [Fact]
    public async Task PointValuesChanged_ForSelectedDevice_UpdatesRowAndTrend()
    {
        var viewModel = CreateViewModel();
        _realtimeService.Setup(service => service.GetDeviceSnapshots(1)).Returns(Array.Empty<PointValueSnapshot>());
        viewModel.RefreshCommand.Execute();
        await TimingTestHelper.WaitUntilAsync(() => viewModel.Points.Count == 2, diagnostics: "等待装载");

        var trendEvents = 0;
        viewModel.TrendDataChanged += (_, _) => trendEvents++;

        _eventAggregator.GetEvent<PointValuesChangedEvent>().Publish(new PointValuesBatchPayload
        {
            DeviceCode = "SIM-TEST-1",
            Samples = new[] { Snapshot("TEMP-01", 81.2) }
        });

        await TimingTestHelper.WaitUntilAsync(
            () => viewModel.Points[0].DisplayValue.StartsWith("81.2", StringComparison.Ordinal),
            diagnostics: "事件到达后应更新实时值列");
        viewModel.Points[0].IsHighAlarm.Should().BeTrue("超限值应触发高限高亮");
        trendEvents.Should().BeGreaterThanOrEqualTo(1, "选中点位有新样本时应触发趋势重绘");
        viewModel.CurrentTrend.Should().NotBeNull("趋势缓冲应有数据");
        viewModel.CurrentTrend!.Should().ContainSingle("首帧趋势应只有一个样本");
    }

    [Fact]
    public async Task PointValuesChanged_WhenPaused_IgnoresUpdates()
    {
        var viewModel = CreateViewModel();
        _realtimeService.Setup(service => service.GetDeviceSnapshots(1)).Returns(Array.Empty<PointValueSnapshot>());
        viewModel.RefreshCommand.Execute();
        await TimingTestHelper.WaitUntilAsync(() => viewModel.Points.Count == 2, diagnostics: "等待装载");

        viewModel.TogglePauseCommand.Execute();

        _eventAggregator.GetEvent<PointValuesChangedEvent>().Publish(new PointValuesBatchPayload
        {
            DeviceCode = "SIM-TEST-1",
            Samples = new[] { Snapshot("TEMP-01", 81.2) }
        });

        await Task.Delay(200);
        viewModel.Points[0].DisplayValue.Should().Be("--", "暂停后应忽略实时更新");
        viewModel.CurrentTrend.Should().BeNull("暂停后不应积累趋势数据");
    }

    [Fact]
    public async Task SelectedPoint_Change_RebuildsTrendFromRealtimeWindow()
    {
        var viewModel = CreateViewModel();
        _realtimeService.Setup(service => service.GetDeviceSnapshots(1)).Returns(Array.Empty<PointValueSnapshot>());
        _realtimeService
            .Setup(service => service.GetTrendWindow("LEVEL-01", It.IsAny<int>()))
            .Returns(new[] { Snapshot("LEVEL-01", 1, DateTime.Now.AddSeconds(-2)), Snapshot("LEVEL-01", 2, DateTime.Now) });
        viewModel.RefreshCommand.Execute();
        await TimingTestHelper.WaitUntilAsync(() => viewModel.Points.Count == 2, diagnostics: "等待装载");

        var trendEvents = 0;
        viewModel.TrendDataChanged += (_, _) => trendEvents++;
        viewModel.SelectedPoint = viewModel.Points.First(row => row.Code == "LEVEL-01");

        trendEvents.Should().BeGreaterThanOrEqualTo(1, "切换点位应触发趋势重建");
        viewModel.CurrentTrend.Should().HaveCount(2, "趋势应以实时仓滚动窗口为初值");
    }

    [Fact]
    public async Task Dispose_ThenPublish_DoesNotThrowAndIgnoresEvents()
    {
        var viewModel = CreateViewModel();
        _realtimeService.Setup(service => service.GetDeviceSnapshots(1)).Returns(Array.Empty<PointValueSnapshot>());
        viewModel.RefreshCommand.Execute();
        await TimingTestHelper.WaitUntilAsync(() => viewModel.Points.Count == 2, diagnostics: "等待装载");

        viewModel.Dispose();

        var act = () => _eventAggregator.GetEvent<PointValuesChangedEvent>().Publish(new PointValuesBatchPayload
        {
            DeviceCode = "SIM-TEST-1",
            Samples = new[] { Snapshot("TEMP-01", 50) }
        });

        act.Should().NotThrow("Dispose 后再发布事件不应抛异常（token 已释放）");
        await Task.Delay(200);
        viewModel.Points[0].DisplayValue.Should().Be("--", "Dispose 后事件不应再更新界面状态");
    }

    [Fact]
    public async Task DeviceStatusChanged_UpdatesDeviceRowStatus()
    {
        var viewModel = CreateViewModel();
        _realtimeService.Setup(service => service.GetDeviceSnapshots(1)).Returns(Array.Empty<PointValueSnapshot>());
        viewModel.RefreshCommand.Execute();
        await TimingTestHelper.WaitUntilAsync(() => viewModel.Devices.Count == 1, diagnostics: "等待装载");

        _eventAggregator.GetEvent<DeviceStatusChangedEvent>().Publish(new DeviceStatusPayload
        {
            DeviceId = 1,
            DeviceCode = "SIM-TEST-1",
            OldStatus = DeviceRuntimeStatus.Running,
            NewStatus = DeviceRuntimeStatus.Faulted,
            Message = "通讯故障"
        });

        await TimingTestHelper.WaitUntilAsync(
            () => viewModel.Devices[0].StatusText == "故障",
            diagnostics: "状态事件应更新设备行显示");
    }
}
