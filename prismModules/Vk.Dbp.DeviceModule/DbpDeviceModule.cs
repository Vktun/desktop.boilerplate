using Prism.Ioc;
using Prism.Modularity;
using Vk.Dbp.Contracts.Extensions;
using Vk.Dbp.Contracts.Industrial;
using Vk.Dbp.DeviceModule.Services;
using Vk.Dbp.DeviceModule.Views;

namespace Vk.Dbp.DeviceModule
{
    /// <summary>
    /// 工业运行时内核模块：设备采集引擎 + 实时监控页。
    /// 引擎全家桶注册在本模块（AccountModule 同款模式），shell 只做模块目录挂载与启动接线。
    /// </summary>
    public class DbpDeviceModule : IModule
    {
        public void OnInitialized(IContainerProvider containerProvider)
        {
        }

        public void RegisterTypes(IContainerRegistry containerRegistry)
        {
            containerRegistry.RegisterForNavigation<DeviceMonitorView>();
            containerRegistry.RegisterForNavigation<DeviceManagementView>();
            containerRegistry.RegisterForNavigation<HistoryQueryView>();
            containerRegistry.RegisterForNavigation<ReportCenterView>();

            // 时间源抽象：引擎/驱动/历史刷盘统一走 TimeProvider，测试注入 FakeTimeProvider
            containerRegistry.RegisterSingleton<TimeProvider>(_ => TimeProvider.System);

            // 实时数据仓：具体类单例（引擎写侧依赖具体类型），接口只读面向消费者，
            // 工厂转发到同一实例避免出现两份状态
            containerRegistry.RegisterSingleton<RealtimeDataStore>();
            containerRegistry.Register<IRealtimeDataService>(provider => provider.Resolve<RealtimeDataStore>());

            containerRegistry.RegisterSingleton<IHistoryDataService, HistoryDataService>();

            // vktun.iot.connector 采集运行时宿主（多设备共享一个 IIoTDataCollector，引用计数管理启停）
            containerRegistry.RegisterSingleton<IotCollectorHost>();
            containerRegistry.RegisterSingleton<IProtocolDriverFactory, ProtocolDriverFactory>();
            containerRegistry.RegisterSingleton<IDeviceCatalogService, DeviceCatalogService>();
            containerRegistry.RegisterSingleton<IDeviceRuntimeService, DeviceRuntimeService>();
            containerRegistry.RegisterSingleton<IDeviceAdminService, DeviceAdminService>();

            // 报表生成器：具体类型供报表中心页直注；同时按 ReportType 命名注册 IReportGenerator 扩展点
            // （Unity 同接口无名注册会相互覆盖，故接口注册必须带名）
            containerRegistry.RegisterSingleton<AlarmSummaryReportGenerator>();
            containerRegistry.RegisterSingleton<ShiftReportGenerator>();
            containerRegistry.RegisterSingleton<AuditLogReportGenerator>();
            containerRegistry.RegisterSingleton<IReportGenerator, AlarmSummaryReportGenerator>(AlarmSummaryReportGenerator.ReportTypeValue);
            containerRegistry.RegisterSingleton<IReportGenerator, ShiftReportGenerator>(ShiftReportGenerator.ReportTypeValue);
            containerRegistry.RegisterSingleton<IReportGenerator, AuditLogReportGenerator>(AuditLogReportGenerator.ReportTypeValue);
        }
    }
}
