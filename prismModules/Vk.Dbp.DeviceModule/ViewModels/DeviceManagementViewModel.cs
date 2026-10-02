using System.Collections.ObjectModel;
using Prism.Commands;
using Prism.Events;
using Prism.Mvvm;
using Prism.Navigation.Regions;
using Vk.Dbp.Contracts.Industrial;
using Vk.Dbp.DeviceModule.Models;
using Vk.Dbp.DeviceModule.Services;
using Vk.Dbp.DeviceModule.Views;
using Growl = HandyControl.Controls.Growl;

namespace Vk.Dbp.DeviceModule.ViewModels;

/// <summary>
/// 设备管理页视图模型：设备/点位/命令的增删改 + 命令执行。
/// 保存/删除/启停后经 <see cref="IDeviceRuntimeService.RestartDeviceAsync"/> reconcile 引擎，配置即时生效。
/// </summary>
public class DeviceManagementViewModel : BindableBase, INavigationAware, IDisposable
{
    private readonly IDeviceAdminService _adminService;
    private readonly IDeviceCatalogService _catalogService;
    private readonly IDeviceRuntimeService _runtimeService;
    private readonly IEventAggregator _eventAggregator;

    private readonly List<DeviceListItemViewModel> _allDevices = [];
    private SubscriptionToken? _statusChangedToken;
    private bool _isDisposed;

    private bool _isLoading;
    private DeviceListItemViewModel? _selectedDevice;
    private bool _hasSelectedDevice;
    private PointDefinition? _selectedPoint;
    private CommandDefinition? _selectedCommand;

    /// <summary>
    /// 构造设备管理视图模型
    /// </summary>
    public DeviceManagementViewModel(
        IDeviceAdminService adminService,
        IDeviceCatalogService catalogService,
        IDeviceRuntimeService runtimeService,
        IEventAggregator eventAggregator)
    {
        _adminService = adminService ?? throw new ArgumentNullException(nameof(adminService));
        _catalogService = catalogService ?? throw new ArgumentNullException(nameof(catalogService));
        _runtimeService = runtimeService ?? throw new ArgumentNullException(nameof(runtimeService));
        _eventAggregator = eventAggregator ?? throw new ArgumentNullException(nameof(eventAggregator));

        RefreshCommand = new DelegateCommand(async () => await LoadDevicesAsync());
        AddDeviceCommand = new DelegateCommand(() => _ = ShowDeviceDialogAsync(null));
        EditDeviceCommand = new DelegateCommand<DeviceListItemViewModel?>(async device => await ShowDeviceDialogAsync(device), device => device is not null);
        DeleteDeviceCommand = new DelegateCommand<DeviceListItemViewModel?>(async device => await DeleteDeviceAsync(device), device => device is not null);
        ToggleDeviceEnabledCommand = new DelegateCommand<DeviceListItemViewModel?>(async device => await ToggleDeviceEnabledAsync(device), device => device is not null);
        AddPointCommand = new DelegateCommand(() => _ = ShowPointDialogAsync(null), () => SelectedDevice is not null);
        EditPointCommand = new DelegateCommand<PointDefinition?>(async point => await ShowPointDialogAsync(point), point => point is not null);
        DeletePointCommand = new DelegateCommand<PointDefinition?>(async point => await DeletePointAsync(point), point => point is not null);
        AddCommandCommand = new DelegateCommand(() => _ = ShowCommandDialogAsync(null), () => SelectedDevice is not null);
        EditCommandCommand = new DelegateCommand<CommandDefinition?>(async command => await ShowCommandDialogAsync(command), command => command is not null);
        DeleteCommandCommand = new DelegateCommand<CommandDefinition?>(async command => await DeleteCommandAsync(command), command => command is not null);
        ExecuteCommandCommand = new DelegateCommand<CommandDefinition?>(async command => await ExecuteCommandDefinitionAsync(command), command => command is not null);

        // ThreadOption.UIThread 订阅要求构造时存在同步上下文（监控页同款约束）
        _statusChangedToken = _eventAggregator.GetEvent<DeviceStatusChangedEvent>()
            .Subscribe(OnDeviceStatusChanged, ThreadOption.UIThread, keepSubscriberReferenceAlive: true);
    }

    // —— 可测性旁路（ExportServiceTests 虚方法先例）：通知与确认在测试子类中替换为记录器 ——

    /// <summary>
    /// 成功通知（默认 Growl）
    /// </summary>
    protected virtual void NotifySuccess(string message)
    {
        Growl.Success(message);
    }

    /// <summary>
    /// 失败通知（默认 Growl）
    /// </summary>
    protected virtual void NotifyError(string message)
    {
        Growl.Error(message);
    }

    /// <summary>
    /// 危险操作确认（默认 MessageBox YesNo）
    /// </summary>
    protected virtual bool Confirm(string message, string title)
    {
        return System.Windows.MessageBox.Show(message, title, System.Windows.MessageBoxButton.YesNo, System.Windows.MessageBoxImage.Warning)
               == System.Windows.MessageBoxResult.Yes;
    }

    /// <summary>
    /// 是否正在加载
    /// </summary>
    public bool IsLoading
    {
        get => _isLoading;
        private set => SetProperty(ref _isLoading, value);
    }

    /// <summary>
    /// 设备列表（含禁用行）
    /// </summary>
    public ObservableCollection<DeviceListItemViewModel> Devices { get; } = [];

    /// <summary>
    /// 选中设备
    /// </summary>
    public DeviceListItemViewModel? SelectedDevice
    {
        get => _selectedDevice;
        set
        {
            if (SetProperty(ref _selectedDevice, value))
            {
                HasSelectedDevice = value is not null;
                RaiseCanExecuteChanged();
                _ = LoadChildrenAsync();
            }
        }
    }

    /// <summary>
    /// 是否有选中设备（子表区启用开关）
    /// </summary>
    public bool HasSelectedDevice
    {
        get => _hasSelectedDevice;
        private set => SetProperty(ref _hasSelectedDevice, value);
    }

    /// <summary>
    /// 选中设备的点位集合
    /// </summary>
    public ObservableCollection<PointDefinition> Points { get; } = [];

    /// <summary>
    /// 选中点位
    /// </summary>
    public PointDefinition? SelectedPoint
    {
        get => _selectedPoint;
        set => SetProperty(ref _selectedPoint, value);
    }

    /// <summary>
    /// 选中设备的命令集合
    /// </summary>
    public ObservableCollection<CommandDefinition> Commands { get; } = [];

    /// <summary>
    /// 选中命令
    /// </summary>
    public CommandDefinition? SelectedCommand
    {
        get => _selectedCommand;
        set => SetProperty(ref _selectedCommand, value);
    }

    /// <summary>
    /// 刷新命令
    /// </summary>
    public DelegateCommand RefreshCommand { get; }

    /// <summary>
    /// 新增设备
    /// </summary>
    public DelegateCommand AddDeviceCommand { get; private set; } = null!;

    /// <summary>
    /// 编辑设备
    /// </summary>
    public DelegateCommand<DeviceListItemViewModel?> EditDeviceCommand { get; private set; } = null!;

    /// <summary>
    /// 删除设备
    /// </summary>
    public DelegateCommand<DeviceListItemViewModel?> DeleteDeviceCommand { get; private set; } = null!;

    /// <summary>
    /// 启用/停用设备
    /// </summary>
    public DelegateCommand<DeviceListItemViewModel?> ToggleDeviceEnabledCommand { get; private set; } = null!;

    /// <summary>
    /// 新增点位
    /// </summary>
    public DelegateCommand AddPointCommand { get; private set; } = null!;

    /// <summary>
    /// 编辑点位
    /// </summary>
    public DelegateCommand<PointDefinition?> EditPointCommand { get; private set; } = null!;

    /// <summary>
    /// 删除点位
    /// </summary>
    public DelegateCommand<PointDefinition?> DeletePointCommand { get; private set; } = null!;

    /// <summary>
    /// 新增命令
    /// </summary>
    public DelegateCommand AddCommandCommand { get; private set; } = null!;

    /// <summary>
    /// 编辑命令
    /// </summary>
    public DelegateCommand<CommandDefinition?> EditCommandCommand { get; private set; } = null!;

    /// <summary>
    /// 删除命令
    /// </summary>
    public DelegateCommand<CommandDefinition?> DeleteCommandCommand { get; private set; } = null!;

    /// <summary>
    /// 执行命令（命令回写）
    /// </summary>
    public DelegateCommand<CommandDefinition?> ExecuteCommandCommand { get; private set; } = null!;

    /// <inheritdoc />
    public bool IsNavigationTarget(NavigationContext navigationContext)
    {
        return true;
    }

    /// <inheritdoc />
    public async void OnNavigatedTo(NavigationContext navigationContext)
    {
        await LoadDevicesAsync();
    }

    /// <inheritdoc />
    public void OnNavigatedFrom(NavigationContext navigationContext)
    {
    }

    /// <inheritdoc />
    public void Dispose()
    {
        if (_isDisposed)
        {
            return;
        }

        _isDisposed = true;
        _statusChangedToken?.Dispose();
        _statusChangedToken = null;
    }

    private async Task LoadDevicesAsync()
    {
        if (_isDisposed)
        {
            return;
        }

        IsLoading = true;
        try
        {
            var devices = await _catalogService.GetDevicesAsync(includeDisabled: true);
            var statuses = _runtimeService.GetAllDeviceStatuses();

            _allDevices.Clear();
            _allDevices.AddRange(devices.Select(device =>
            {
                var item = new DeviceListItemViewModel
                {
                    Id = device.Id,
                    Code = device.Code,
                    Name = device.Name,
                    ProtocolTypeText = device.ProtocolType,
                    IsEnabled = device.IsEnabled
                };
                item.ApplyStatus(statuses.TryGetValue(device.Id, out var status) ? status : DeviceRuntimeStatus.Stopped);
                return item;
            }));

            var previouslySelected = SelectedDevice;
            Devices.Clear();
            foreach (var item in _allDevices)
            {
                Devices.Add(item);
            }

            SelectedDevice = previouslySelected is not null
                ? Devices.FirstOrDefault(device => device.Id == previouslySelected.Id) ?? Devices.FirstOrDefault()
                : Devices.FirstOrDefault();
        }
        finally
        {
            IsLoading = false;
        }
    }

    private async Task LoadChildrenAsync()
    {
        if (_isDisposed)
        {
            return;
        }

        var device = SelectedDevice;
        Points.Clear();
        Commands.Clear();
        SelectedPoint = null;
        SelectedCommand = null;

        if (device is null)
        {
            return;
        }

        var points = await _catalogService.GetPointsAsync(device.Id);
        if (SelectedDevice?.Id != device.Id)
        {
            return;
        }

        foreach (var point in points)
        {
            Points.Add(point);
        }

        var commands = await _catalogService.GetCommandsAsync(device.Id);
        if (SelectedDevice?.Id != device.Id)
        {
            return;
        }

        foreach (var command in commands)
        {
            Commands.Add(command);
        }
    }

    private async Task ShowDeviceDialogAsync(DeviceListItemViewModel? deviceItem)
    {
        DeviceEditModel? model = null;
        if (deviceItem is not null)
        {
            model = await _adminService.GetDeviceAsync(deviceItem.Id);
            if (model is null)
            {
                NotifyError($"设备 {deviceItem.Code} 已不存在（可能被其他用户删除）");
                await LoadDevicesAsync();
                return;
            }
        }

        var dialog = new DeviceEditDialog();
        if (dialog.DataContext is not DeviceEditDialogViewModel viewModel)
        {
            return;
        }

        viewModel.Initialize(model, isAddMode: model is null, async confirmed =>
        {
            if (confirmed)
            {
                await ApplyDeviceSavedAsync(model?.Id ?? 0);
                dialog.Close();
            }
            else
            {
                dialog.Close();
            }
        });

        dialog.ShowDialog();
    }

    private async Task ShowPointDialogAsync(PointDefinition? point)
    {
        if (SelectedDevice is null)
        {
            return;
        }

        PointEditModel? model = null;
        if (point is not null)
        {
            model = await _adminService.GetPointAsync(point.Id);
            if (model is null)
            {
                NotifyError($"点位 {point.Code} 已不存在");
                await LoadChildrenAsync();
                return;
            }
        }
        else
        {
            model = new PointEditModel { DeviceId = SelectedDevice.Id };
        }

        var dialog = new PointEditDialog();
        if (dialog.DataContext is not PointEditDialogViewModel viewModel)
        {
            return;
        }

        viewModel.Initialize(model, isAddMode: point is null, async confirmed =>
        {
            dialog.Close();
            if (confirmed)
            {
                await ApplyPointSavedAsync(model.DeviceId);
            }
        });

        dialog.ShowDialog();
    }

    private async Task ShowCommandDialogAsync(CommandDefinition? command)
    {
        if (SelectedDevice is null)
        {
            return;
        }

        CommandEditModel? model = null;
        if (command is not null)
        {
            model = await _adminService.GetCommandAsync(command.Id);
            if (model is null)
            {
                NotifyError($"命令 {command.Code} 已不存在");
                await LoadChildrenAsync();
                return;
            }
        }
        else
        {
            model = new CommandEditModel { DeviceId = SelectedDevice.Id };
        }

        var targetPointCodes = Points.Select(point => point.Code).ToList();

        var dialog = new CommandEditDialog();
        if (dialog.DataContext is not CommandEditDialogViewModel viewModel)
        {
            return;
        }

        viewModel.Initialize(model, isAddMode: command is null, targetPointCodes, async confirmed =>
        {
            dialog.Close();
            if (confirmed)
            {
                await ApplyCommandSavedAsync(model.DeviceId);
            }
        });

        dialog.ShowDialog();
    }

    // internal 便于 VM 测试直调（保存/删除后的联动链路）
    internal async Task ApplyDeviceSavedAsync(int deviceId)
    {
        if (deviceId > 0)
        {
            await _runtimeService.RestartDeviceAsync(deviceId);
        }

        await LoadDevicesAsync();
        NotifySuccess("设备配置已保存，采集引擎已同步");
    }

    internal async Task ApplyPointSavedAsync(int deviceId)
    {
        await _runtimeService.RestartDeviceAsync(deviceId);
        await LoadChildrenAsync();
        NotifySuccess("点位配置已保存，采集引擎已同步");
    }

    internal async Task ApplyCommandSavedAsync(int deviceId)
    {
        await _runtimeService.RestartDeviceAsync(deviceId);
        await LoadChildrenAsync();
        NotifySuccess("命令配置已保存");
    }

    private async Task DeleteDeviceAsync(DeviceListItemViewModel? device)
    {
        if (device is null)
        {
            return;
        }

        if (!Confirm($"确定删除设备 \"{device.Code}\" 吗？\n将级联删除其全部点位与命令，历史数据保留。",
                "确认删除"))
        {
            return;
        }

        var result = await _adminService.DeleteDeviceAsync(device.Id);
        if (!result.Success)
        {
            NotifyError(result.Message);
            return;
        }

        await _runtimeService.RestartDeviceAsync(device.Id);
        await LoadDevicesAsync();
        NotifySuccess(result.Message);
    }

    private async Task DeletePointAsync(PointDefinition? point)
    {
        if (point is null || SelectedDevice is null)
        {
            return;
        }

        if (!Confirm($"确定删除点位 \"{point.Code}\" 吗？\n引用该点位的命令将被一并删除，历史数据保留。",
                "确认删除"))
        {
            return;
        }

        var result = await _adminService.DeletePointAsync(point.Id);
        if (!result.Success)
        {
            NotifyError(result.Message);
            return;
        }

        await _runtimeService.RestartDeviceAsync(SelectedDevice.Id);
        await LoadChildrenAsync();
        NotifySuccess(result.Message);
    }

    private async Task DeleteCommandAsync(CommandDefinition? command)
    {
        if (command is null || SelectedDevice is null)
        {
            return;
        }

        if (!Confirm($"确定删除命令 \"{command.Code}\" 吗？", "确认删除"))
        {
            return;
        }

        var result = await _adminService.DeleteCommandAsync(command.Id);
        if (!result.Success)
        {
            NotifyError(result.Message);
            return;
        }

        await _runtimeService.RestartDeviceAsync(SelectedDevice.Id);
        await LoadChildrenAsync();
        NotifySuccess(result.Message);
    }

    private async Task ToggleDeviceEnabledAsync(DeviceListItemViewModel? device)
    {
        if (device is null)
        {
            return;
        }

        var model = await _adminService.GetDeviceAsync(device.Id);
        if (model is null)
        {
            NotifyError($"设备 {device.Code} 已不存在");
            await LoadDevicesAsync();
            return;
        }

        model.IsEnabled = !model.IsEnabled;
        var result = await _adminService.SaveDeviceAsync(model);
        if (!result.Success)
        {
            NotifyError(result.Message);
            return;
        }

        await ApplyDeviceSavedAsync(model.Id);
    }

    private async Task ExecuteCommandDefinitionAsync(CommandDefinition? command)
    {
        if (command is null)
        {
            return;
        }

        if (!Confirm($"确定执行命令 \"{command.Name}\" 吗？\n将向点位 {command.TargetPointCode} 写入 {command.WriteValue}。",
                "确认执行"))
        {
            return;
        }

        var result = await _runtimeService.ExecuteCommandAsync(command.Id);
        if (result.Success)
        {
            NotifySuccess(result.Message);
        }
        else
        {
            NotifyError(result.Message);
        }
    }

    private void OnDeviceStatusChanged(DeviceStatusPayload payload)
    {
        if (_isDisposed)
        {
            return;
        }

        var item = _allDevices.FirstOrDefault(device => device.Id == payload.DeviceId);
        item?.ApplyStatus(payload.NewStatus);
    }

    private void RaiseCanExecuteChanged()
    {
        AddPointCommand.RaiseCanExecuteChanged();
        AddCommandCommand.RaiseCanExecuteChanged();
    }
}
