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

public sealed class DeviceManagementViewModelTests : IDisposable
{
    private readonly Mock<IDeviceAdminService> _adminService = new();
    private readonly Mock<IDeviceCatalogService> _catalogService = new();
    private readonly Mock<IDeviceRuntimeService> _runtimeService = new();
    private readonly EventAggregator _eventAggregator = new();

    public DeviceManagementViewModelTests()
    {
        // ThreadOption.UIThread 订阅要求非空同步上下文（DeviceMonitorViewModelTests 同款套路）
        SynchronizationContext.SetSynchronizationContext(new SynchronizationContext());

        _catalogService
            .Setup(service => service.GetDevicesAsync(It.IsAny<bool>()))
            .ReturnsAsync(new List<DeviceSummary>
            {
                new() { Id = 1, Code = "SIM-A", Name = "设备A", ProtocolType = "Simulated", IsEnabled = true },
                new() { Id = 2, Code = "SIM-B", Name = "设备B", ProtocolType = "Simulated", IsEnabled = false }
            });
        _catalogService
            .Setup(service => service.GetPointsAsync(1))
            .ReturnsAsync(new List<PointDefinition>
            {
                new() { Id = 11, DeviceId = 1, Code = "TEMP-01", Name = "温度", Address = "const(1)", IsEnabled = true }
            });
        _catalogService
            .Setup(service => service.GetCommandsAsync(1))
            .ReturnsAsync(new List<CommandDefinition>
            {
                new() { Id = 21, DeviceId = 1, Code = "CMD-01", Name = "开阀", TargetPointCode = "TEMP-01", WriteValue = "1", IsEnabled = true }
            });
        _runtimeService
            .Setup(service => service.GetAllDeviceStatuses())
            .Returns(new Dictionary<int, DeviceRuntimeStatus> { [1] = DeviceRuntimeStatus.Running });
    }

    public void Dispose()
    {
        SynchronizationContext.SetSynchronizationContext(null);
    }

    private TestableDeviceManagementViewModel CreateViewModel()
    {
        return new TestableDeviceManagementViewModel(
            _adminService.Object, _catalogService.Object, _runtimeService.Object, _eventAggregator);
    }

    [Fact]
    public async Task LoadDevicesAsync_WithCatalog_LoadsDevicesIncludingDisabled()
    {
        var viewModel = CreateViewModel();

        viewModel.RefreshCommand.Execute();
        await TimingTestHelper.WaitUntilAsync(
            () => viewModel.Devices.Count == 2,
            diagnostics: "刷新后应装载含禁用设备在内的全部设备");

        _catalogService.Verify(service => service.GetDevicesAsync(true), Times.Once, "管理页应请求含禁用设备的清单");
        viewModel.Devices.Should().Contain(device => device.Code == "SIM-B" && !device.IsEnabled, "禁用设备应出现在管理列表");
        viewModel.SelectedDevice.Should().NotBeNull("装载后应默认选中第一台设备");
        viewModel.HasSelectedDevice.Should().BeTrue("选中后子表区应可用");
    }

    [Fact]
    public async Task SelectedDevice_Change_LoadsPointsAndCommands()
    {
        var viewModel = CreateViewModel();
        viewModel.RefreshCommand.Execute();
        await TimingTestHelper.WaitUntilAsync(() => viewModel.Devices.Count == 2, diagnostics: "等待装载");

        viewModel.SelectedDevice = viewModel.Devices.First(device => device.Id == 1);

        await TimingTestHelper.WaitUntilAsync(
            () => viewModel.Points.Count == 1 && viewModel.Commands.Count == 1,
            diagnostics: "选中设备后应装载点位与命令子表");
        _catalogService.Verify(service => service.GetPointsAsync(1), Times.AtLeastOnce, "应按设备 ID 加载点位");
        _catalogService.Verify(service => service.GetCommandsAsync(1), Times.AtLeastOnce, "应按设备 ID 加载命令");
    }

    [Fact]
    public async Task DeviceStatusChanged_UpdatesDeviceRowStatus()
    {
        var viewModel = CreateViewModel();
        viewModel.RefreshCommand.Execute();
        await TimingTestHelper.WaitUntilAsync(() => viewModel.Devices.Count == 2, diagnostics: "等待装载");

        _eventAggregator.GetEvent<DeviceStatusChangedEvent>().Publish(new DeviceStatusPayload
        {
            DeviceId = 1,
            DeviceCode = "SIM-A",
            OldStatus = DeviceRuntimeStatus.Running,
            NewStatus = DeviceRuntimeStatus.Faulted,
            Message = "通讯故障"
        });

        await TimingTestHelper.WaitUntilAsync(
            () => viewModel.Devices.First(device => device.Id == 1).StatusText == "故障",
            diagnostics: "状态事件应更新设备行显示");
    }

    [Fact]
    public async Task ApplyDeviceSavedAsync_CallsEngineRestartAndReloads()
    {
        var viewModel = CreateViewModel();
        viewModel.RefreshCommand.Execute();
        await TimingTestHelper.WaitUntilAsync(() => viewModel.Devices.Count == 2, diagnostics: "等待装载");

        await viewModel.ApplyDeviceSavedAsync(1);

        _runtimeService.Verify(service => service.RestartDeviceAsync(1), Times.Once, "设备保存后应 reconcile 引擎");
        viewModel.SuccessMessages.Should().Contain(message => message.Contains("已保存"), "保存成功应发出成功通知");
    }

    [Fact]
    public async Task ExecuteCommandCommand_WhenConfirmed_PassesThroughResult()
    {
        var viewModel = CreateViewModel();
        viewModel.RefreshCommand.Execute();
        await TimingTestHelper.WaitUntilAsync(() => viewModel.Devices.Count == 2, diagnostics: "等待装载");
        viewModel.SelectedDevice = viewModel.Devices.First(device => device.Id == 1);
        await TimingTestHelper.WaitUntilAsync(() => viewModel.Commands.Count == 1, diagnostics: "等待命令装载");

        viewModel.NextConfirmResult = true;
        _runtimeService
            .Setup(service => service.ExecuteCommandAsync(21))
            .ReturnsAsync(new CommandResult(false, "设备未在运行，无法执行命令"));

        var command = viewModel.Commands[0];
        viewModel.ExecuteCommandCommand.Execute(command);

        await TimingTestHelper.WaitUntilAsync(
            () => viewModel.ErrorMessages.Count > 0,
            diagnostics: "执行失败应发出错误通知");
        _runtimeService.Verify(service => service.ExecuteCommandAsync(21), Times.Once, "确认后应调用引擎执行命令");
        viewModel.ErrorMessages.Should().Contain("设备未在运行，无法执行命令", "引擎返回的消息应透传给用户");
    }

    [Fact]
    public async Task ExecuteCommandCommand_WhenNotConfirmed_DoesNotCallEngine()
    {
        var viewModel = CreateViewModel();
        viewModel.RefreshCommand.Execute();
        await TimingTestHelper.WaitUntilAsync(() => viewModel.Devices.Count == 2, diagnostics: "等待装载");
        viewModel.SelectedDevice = viewModel.Devices.First(device => device.Id == 1);
        await TimingTestHelper.WaitUntilAsync(() => viewModel.Commands.Count == 1, diagnostics: "等待命令装载");

        viewModel.NextConfirmResult = false;
        viewModel.ExecuteCommandCommand.Execute(viewModel.Commands[0]);

        await Task.Delay(150);
        _runtimeService.Verify(service => service.ExecuteCommandAsync(It.IsAny<int>()), Times.Never, "未确认时不应调用引擎");
    }

    /// <summary>
    /// 测试子类：通知与确认替换为记录器（ExportServiceTests 虚方法旁路先例）
    /// </summary>
    private sealed class TestableDeviceManagementViewModel : DeviceManagementViewModel
    {
        public TestableDeviceManagementViewModel(
            IDeviceAdminService adminService,
            IDeviceCatalogService catalogService,
            IDeviceRuntimeService runtimeService,
            IEventAggregator eventAggregator)
            : base(adminService, catalogService, runtimeService, eventAggregator)
        {
        }

        public List<string> SuccessMessages { get; } = [];

        public List<string> ErrorMessages { get; } = [];

        public bool? NextConfirmResult { get; set; }

        protected override void NotifySuccess(string message)
        {
            SuccessMessages.Add(message);
        }

        protected override void NotifyError(string message)
        {
            ErrorMessages.Add(message);
        }

        protected override bool Confirm(string message, string title)
        {
            return NextConfirmResult ?? false;
        }
    }
}
