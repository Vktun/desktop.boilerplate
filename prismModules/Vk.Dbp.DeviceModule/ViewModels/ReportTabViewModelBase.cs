using System.IO;
using Prism.Mvvm;
using Vk.Dbp.Contracts.Services;
using Growl = HandyControl.Controls.Growl;

namespace Vk.Dbp.DeviceModule.ViewModels;

/// <summary>
/// 报表中心子页 VM 基类：导出落盘链路 + 可测性通知旁路（各子 VM 共用）。
/// </summary>
public abstract class ReportTabViewModelBase : BindableBase
{
    /// <summary>
    /// 构造子页 VM
    /// </summary>
    protected ReportTabViewModelBase(IExportService exportService)
    {
        ExportService = exportService ?? throw new ArgumentNullException(nameof(exportService));
    }

    /// <summary>
    /// 导出服务（对话框与打开产物）
    /// </summary>
    protected IExportService ExportService { get; }

    /// <summary>
    /// 成功通知（默认 Growl；测试子类替换）
    /// </summary>
    protected virtual void NotifySuccess(string message)
    {
        Growl.Success(message);
    }

    /// <summary>
    /// 失败通知（默认 Growl；测试子类替换）
    /// </summary>
    protected virtual void NotifyError(string message)
    {
        Growl.Error(message);
    }

    /// <summary>
    /// 导出字节到用户选择的文件并尝试打开；用户取消对话框时静默返回
    /// </summary>
    protected async Task ExportBytesAsync(byte[] bytes, string defaultName, string expectedExtension)
    {
        var filePath = ExportService.ShowSaveFileDialog(defaultName, $"{expectedExtension.TrimStart('.').ToUpperInvariant()}文件|*{expectedExtension}");
        if (string.IsNullOrEmpty(filePath))
        {
            return;
        }

        if (!filePath.EndsWith(expectedExtension, StringComparison.OrdinalIgnoreCase))
        {
            filePath += expectedExtension;
        }

        await File.WriteAllBytesAsync(filePath, bytes);
        NotifySuccess("报表已导出");
        await ExportService.OpenExportedFileAsync(filePath);
    }
}
