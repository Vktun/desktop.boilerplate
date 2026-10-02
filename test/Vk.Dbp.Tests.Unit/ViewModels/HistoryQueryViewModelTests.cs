using FluentAssertions;
using Moq;
using Vk.Dbp.Contracts.Services;
using Vk.Dbp.DeviceModule.Models;
using Vk.Dbp.DeviceModule.Services;
using Vk.Dbp.DeviceModule.ViewModels;
using Vk.Dbp.Tests.Unit.Services;
using Xunit;

namespace Vk.Dbp.Tests.Unit.ViewModels;

public sealed class HistoryQueryViewModelTests
{
    private readonly Mock<IHistoryDataService> _historyService = new();
    private readonly Mock<IDeviceCatalogService> _catalogService = new();
    private readonly Mock<IExportService> _exportService = new();

    public HistoryQueryViewModelTests()
    {
        _catalogService
            .Setup(service => service.GetDevicesAsync(It.IsAny<bool>()))
            .ReturnsAsync(new List<DeviceSummary>
            {
                new() { Id = 1, Code = "SIM-A", Name = "设备A", ProtocolType = "Simulated", IsEnabled = true }
            });
        _catalogService
            .Setup(service => service.GetPointsAsync(1))
            .ReturnsAsync(new List<PointDefinition>
            {
                new() { Id = 11, DeviceId = 1, Code = "TEMP-01", Name = "温度", Address = "const(1)", IsEnabled = true },
                new() { Id = 12, DeviceId = 1, Code = "DISABLED-01", Name = "禁用点", Address = "const(2)", IsEnabled = false }
            });
    }

    private TestableHistoryQueryViewModel CreateViewModel()
    {
        return new TestableHistoryQueryViewModel(_historyService.Object, _catalogService.Object, _exportService.Object);
    }

    /// <summary>
    /// 测试子类：通知替换为记录器（DeviceManagementViewModelTests 同款旁路）
    /// </summary>
    private sealed class TestableHistoryQueryViewModel : HistoryQueryViewModel
    {
        public TestableHistoryQueryViewModel(
            IHistoryDataService historyService,
            IDeviceCatalogService catalogService,
            IExportService exportService)
            : base(historyService, catalogService, exportService)
        {
        }

        public List<string> Messages { get; } = [];

        protected override void NotifySuccess(string message)
        {
            Messages.Add($"[成功] {message}");
        }

        protected override void NotifyError(string message)
        {
            Messages.Add($"[错误] {message}");
        }

        protected override void NotifyWarning(string message)
        {
            Messages.Add($"[警告] {message}");
        }
    }

    [Fact]
    public async Task LoadDevicesAsync_WithCatalog_SelectsFirstDeviceAndPoint()
    {
        var viewModel = CreateViewModel();
        await viewModel.LoadDevicesAsync();

        await TimingTestHelper.WaitUntilAsync(
            () => viewModel.Devices.Count == 1 && viewModel.Points.Count == 1,
            diagnostics: "装载后应默认选中第一台设备与其点位");

        viewModel.Points.Should().NotContain(point => point.Code == "DISABLED-01", "禁用点位不应出现在趋势查询选项中");
        viewModel.SelectedPoint.Should().NotBeNull("应默认选中第一个点位");
        viewModel.QueryCommand.CanExecute().Should().BeTrue("选中齐备后查询应可用");
    }

    [Fact]
    public async Task QueryAsync_WithSelection_CallsHistoryServiceWithRangeAndCap()
    {
        _historyService
            .Setup(service => service.QueryAsync(It.IsAny<string>(), It.IsAny<DateTime>(), It.IsAny<DateTime>(), It.IsAny<int>()))
            .ReturnsAsync(new List<HistoryPoint>
            {
                new() { Timestamp = DateTime.Now.AddMinutes(-40), Value = 10 },
                new() { Timestamp = DateTime.Now.AddMinutes(-20), Value = 30 },
                new() { Timestamp = DateTime.Now.AddMinutes(-5), Value = 20 }
            });

        var viewModel = CreateViewModel();
        await viewModel.LoadDevicesAsync();
        await TimingTestHelper.WaitUntilAsync(() => viewModel.Points.Count == 1, diagnostics: "等待装载");

        viewModel.QueryCommand.Execute();
        await TimingTestHelper.WaitUntilAsync(
            () => viewModel.HasResult,
            diagnostics: "查询应产出结果");

        _historyService.Verify(
            service => service.QueryAsync(
                "TEMP-01",
                It.Is<DateTime>(start => start > DateTime.Now.AddHours(-1).AddMinutes(-1)),
                It.Is<DateTime>(end => end <= DateTime.Now.AddMinutes(1)),
                HistoryQueryViewModel.MaxChartPoints),
            Times.Once,
            "默认预设（近1小时）应换算为正确时间范围并带降采样上限");

        viewModel.MaxText.Should().Be("30", "统计应计算最大值");
        viewModel.MinText.Should().Be("10", "统计应计算最小值");
        viewModel.AvgText.Should().Be("20", "统计应计算平均值");
        viewModel.CountText.Should().Be("3", "统计应计算样本数");
        viewModel.CurrentSamples.Should().HaveCount(3, "查询结果应暴露给图表与明细表");
    }

    [Fact]
    public async Task QueryAsync_EmptyResult_ShowsNoDataStatus()
    {
        _historyService
            .Setup(service => service.QueryAsync(It.IsAny<string>(), It.IsAny<DateTime>(), It.IsAny<DateTime>(), It.IsAny<int>()))
            .ReturnsAsync(new List<HistoryPoint>());

        var viewModel = CreateViewModel();
        await viewModel.LoadDevicesAsync();
        await TimingTestHelper.WaitUntilAsync(() => viewModel.Points.Count == 1, diagnostics: "等待装载");

        viewModel.QueryCommand.Execute();
        await TimingTestHelper.WaitUntilAsync(
            () => !viewModel.HasResult,
            diagnostics: "空结果应清除 HasResult");

        viewModel.StatusText.Should().Contain("无历史数据", "空结果应给出明确状态提示");
        viewModel.ExportCommand.CanExecute().Should().BeFalse("无结果时导出应不可用");
    }

    [Fact]
    public async Task ExportAsync_WithResult_ExportsExcelWithChineseColumns()
    {
        _historyService
            .Setup(service => service.QueryAsync(It.IsAny<string>(), It.IsAny<DateTime>(), It.IsAny<DateTime>(), It.IsAny<int>()))
            .ReturnsAsync(new List<HistoryPoint> { new() { Timestamp = DateTime.Now, Value = 1.5 } });
        _exportService
            .Setup(service => service.ExportToExcelAsync(
                It.IsAny<IEnumerable<HistoryQueryViewModel.TrendExportRow>>(),
                It.IsAny<string>(),
                It.IsAny<ExcelExportOptions?>()))
            .ReturnsAsync("C:\\temp\\fake.xlsx");

        var viewModel = CreateViewModel();
        await viewModel.LoadDevicesAsync();
        await TimingTestHelper.WaitUntilAsync(() => viewModel.Points.Count == 1, diagnostics: "等待装载");
        viewModel.QueryCommand.Execute();
        await TimingTestHelper.WaitUntilAsync(() => viewModel.HasResult, diagnostics: "等待查询结果");

        viewModel.ExportCommand.Execute();
        await TimingTestHelper.WaitUntilAsync(
            () => _exportService.Invocations.Any(invocation => invocation.Method.Name == nameof(IExportService.OpenExportedFileAsync)),
            diagnostics: "导出流程应完成到打开文件一步");

        _exportService.Verify(
            service => service.ExportToExcelAsync(
                It.IsAny<IEnumerable<HistoryQueryViewModel.TrendExportRow>>(),
                It.Is<string>(name => name.StartsWith("TEMP-01-历史趋势-", StringComparison.Ordinal)),
                It.Is<ExcelExportOptions?>(options =>
                    options!.ColumnDisplayNames!["Timestamp"] == "时间"
                    && options.ColumnDisplayNames["Value"] == "数值")),
            Times.Once,
            "导出应带中文列名映射与点位编码文件名");
        _exportService.Verify(service => service.OpenExportedFileAsync(It.IsAny<string>()), Times.Once, "导出成功后应尝试打开文件");
    }

    [Fact]
    public void TryGetRange_CustomPreset_ValidatesOrdering()
    {
        var viewModel = CreateViewModel();

        viewModel.SelectedRangePreset = "自定义";
        viewModel.CustomStart = DateTime.Now.AddHours(-2);
        viewModel.CustomEnd = DateTime.Now;
        viewModel.TryGetRange(out var start, out var end).Should().BeTrue("开始早于结束的自定义范围应有效");
        start.Should().BeBefore(end);

        viewModel.CustomStart = DateTime.Now;
        viewModel.CustomEnd = DateTime.Now.AddHours(-2);
        viewModel.TryGetRange(out _, out _).Should().BeFalse("开始晚于结束的自定义范围应被拒绝");
    }

    [Fact]
    public void TryGetRange_Presets_MapToExpectedSpans()
    {
        var viewModel = CreateViewModel();
        var before = DateTime.Now;

        foreach (var (preset, expectedSpan) in new[]
                 {
                     ("近1小时", TimeSpan.FromHours(1)),
                     ("近8小时", TimeSpan.FromHours(8)),
                     ("近24小时", TimeSpan.FromHours(24)),
                     ("近7天", TimeSpan.FromDays(7))
                 })
        {
            viewModel.SelectedRangePreset = preset;
            viewModel.TryGetRange(out var start, out var end).Should().BeTrue($"预设 {preset} 应有效");
            (end - start).Should().BeCloseTo(expectedSpan, TimeSpan.FromMinutes(1), $"预设 {preset} 的跨度应正确");
        }

        viewModel.SelectedRangePreset = "今日";
        viewModel.TryGetRange(out var startToday, out var endToday).Should().BeTrue("今日预设应有效");
        startToday.Should().Be(before.Date, "今日预设的起点应为当天零点");
        endToday.Should().BeAfter(startToday, "今日预设终点应为当前时刻");
    }
}
