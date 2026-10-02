using System;
using Prism.Commands;
using Prism.Mvvm;
using Vk.Dbp.Contracts.Industrial;
using Vk.Dbp.DeviceModule.Models;
using Vk.Dbp.DeviceModule.Services;

namespace Vk.Dbp.DeviceModule.ViewModels;

/// <summary>
/// 点位编辑对话框 VM（RoleEditDialog 双段式范式）
/// </summary>
public sealed class PointEditDialogViewModel : BindableBase
{
    private readonly IDeviceAdminService _adminService;
    private PointEditModel _model = new();
    private bool _isAddMode = true;
    private string _dialogTitle = "新增点位";
    private string? _errorMessage;
    private bool _showError;
    private Action<bool>? _closeAction;

    /// <summary>
    /// 构造点位编辑对话框 VM
    /// </summary>
    public PointEditDialogViewModel(IDeviceAdminService adminService)
    {
        _adminService = adminService ?? throw new ArgumentNullException(nameof(adminService));

        ConfirmCommand = new DelegateCommand(async () => await ConfirmAsync());
        CancelCommand = new DelegateCommand(() => _closeAction?.Invoke(false));
    }

    /// <summary>
    /// 编辑数据
    /// </summary>
    public PointEditModel Model
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
    /// 错误消息
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
    /// 数据类型下拉选项（手工列举，AlarmRecordViewModel 同款）
    /// </summary>
    public IReadOnlyList<PointDataType> DataTypeOptions { get; } = new[]
    {
        PointDataType.Boolean,
        PointDataType.Int32,
        PointDataType.Float,
        PointDataType.Double,
        PointDataType.String
    };

    /// <summary>
    /// 确定命令
    /// </summary>
    public DelegateCommand ConfirmCommand { get; }

    /// <summary>
    /// 取消命令
    /// </summary>
    public DelegateCommand CancelCommand { get; }

    /// <summary>
    /// 初始化对话框（新增模式的 DeviceId 由调用方预填）
    /// </summary>
    public void Initialize(PointEditModel? model, bool isAddMode, Action<bool> closeAction)
    {
        _closeAction = closeAction;
        IsAddMode = isAddMode;
        DialogTitle = isAddMode ? "新增点位" : "编辑点位";
        Model = isAddMode
            ? new PointEditModel { DataType = PointDataType.Double, IsEnabled = true }
            : model ?? throw new ArgumentNullException(nameof(model));
        ErrorMessage = null;
    }

    private async Task ConfirmAsync()
    {
        if (string.IsNullOrWhiteSpace(Model.Code) || string.IsNullOrWhiteSpace(Model.Name)
            || string.IsNullOrWhiteSpace(Model.Address))
        {
            ErrorMessage = "点位编码、名称与驱动地址不能为空";
            return;
        }

        var result = await _adminService.SavePointAsync(Model);
        if (result.Success)
        {
            _closeAction?.Invoke(true);
            return;
        }

        ErrorMessage = result.Message;
    }
}
