using System;
using Prism.Commands;
using Prism.Mvvm;
using Vk.Dbp.DeviceModule.Models;
using Vk.Dbp.DeviceModule.Services;

namespace Vk.Dbp.DeviceModule.ViewModels;

/// <summary>
/// 设备编辑对话框 VM（RoleEditDialog 双段式范式：构造器只注服务，Initialize 传数据+回调，自落库）
/// </summary>
public sealed class DeviceEditDialogViewModel : BindableBase
{
    private readonly IDeviceAdminService _adminService;
    private DeviceEditModel _model = new();
    private bool _isAddMode = true;
    private string _dialogTitle = "新增设备";
    private string? _errorMessage;
    private bool _showError;
    private Action<bool>? _closeAction;

    /// <summary>
    /// 构造设备编辑对话框 VM
    /// </summary>
    public DeviceEditDialogViewModel(IDeviceAdminService adminService)
    {
        _adminService = adminService ?? throw new ArgumentNullException(nameof(adminService));

        ConfirmCommand = new DelegateCommand(async () => await ConfirmAsync());
        CancelCommand = new DelegateCommand(() => _closeAction?.Invoke(false));
    }

    /// <summary>
    /// 编辑数据
    /// </summary>
    public DeviceEditModel Model
    {
        get => _model;
        private set => SetProperty(ref _model, value);
    }

    /// <summary>
    /// 是否新增模式（编码可编辑）
    /// </summary>
    public bool IsAddMode
    {
        get => _isAddMode;
        private set => SetProperty(ref _isAddMode, value);
    }

    /// <summary>
    /// 对话框标题
    /// </summary>
    public string DialogTitle
    {
        get => _dialogTitle;
        private set => SetProperty(ref _dialogTitle, value);
    }

    /// <summary>
    /// 错误消息（保存失败时红字显示，不关窗）
    /// </summary>
    public string? ErrorMessage
    {
        get => _errorMessage;
        private set
        {
            SetProperty(ref _errorMessage, value);
            ShowError = !string.IsNullOrEmpty(value);
        }
    }

    /// <summary>
    /// 是否显示错误
    /// </summary>
    public bool ShowError
    {
        get => _showError;
        private set => SetProperty(ref _showError, value);
    }

    /// <summary>
    /// 协议下拉选项（与 ProtocolDriverFactory 支持的协议一致；OpcUa 暂无驱动不列出）
    /// </summary>
    public IReadOnlyList<string> ProtocolOptions { get; } = new[]
    {
        Vk.Dbp.Contracts.Industrial.ProtocolTypes.Simulated,
        Vk.Dbp.Contracts.Industrial.ProtocolTypes.ModbusTcp,
        Vk.Dbp.Contracts.Industrial.ProtocolTypes.ModbusRtu
    };

    /// <summary>
    /// 连接配置占位示例（随选中协议切换，展示真实键名）
    /// </summary>
    public string ConnectionConfigPlaceholder => Model.ProtocolType switch
    {
        Vk.Dbp.Contracts.Industrial.ProtocolTypes.ModbusTcp =>
            "{\"ip\":\"127.0.0.1\",\"port\":502,\"slaveId\":1,\"pollIntervalMs\":1000}",
        Vk.Dbp.Contracts.Industrial.ProtocolTypes.ModbusRtu =>
            "{\"serialPort\":\"COM3\",\"baudRate\":9600,\"slaveId\":1,\"pollIntervalMs\":1000}",
        _ => "{\"pollIntervalMs\":1000}"
    };

    /// <summary>
    /// 选中协议变化时刷新占位示例
    /// </summary>
    public void OnProtocolChanged()
    {
        RaisePropertyChanged(nameof(ConnectionConfigPlaceholder));
    }

    /// <summary>
    /// 确定命令
    /// </summary>
    public DelegateCommand ConfirmCommand { get; }

    /// <summary>
    /// 取消命令
    /// </summary>
    public DelegateCommand CancelCommand { get; }

    /// <summary>
    /// 初始化对话框
    /// </summary>
    public void Initialize(DeviceEditModel? model, bool isAddMode, Action<bool> closeAction)
    {
        _closeAction = closeAction;
        IsAddMode = isAddMode;
        DialogTitle = isAddMode ? "新增设备" : "编辑设备";
        Model = isAddMode
            ? new DeviceEditModel { ProtocolType = Vk.Dbp.Contracts.Industrial.ProtocolTypes.Simulated, IsEnabled = true }
            : model ?? throw new ArgumentNullException(nameof(model));
        ErrorMessage = null;
        OnProtocolChanged();
    }

    private async Task ConfirmAsync()
    {
        if (string.IsNullOrWhiteSpace(Model.Code) || string.IsNullOrWhiteSpace(Model.Name))
        {
            ErrorMessage = "设备编码与名称不能为空";
            return;
        }

        var result = await _adminService.SaveDeviceAsync(Model);
        if (result.Success)
        {
            _closeAction?.Invoke(true);
            return;
        }

        ErrorMessage = result.Message;
    }
}
