using HandyControl.Controls;

namespace Vk.Dbp.DeviceModule.Views;

/// <summary>
/// 设备编辑对话框
/// </summary>
public partial class DeviceEditDialog : Window
{
    /// <summary>
    /// 构造对话框
    /// </summary>
    public DeviceEditDialog()
    {
        InitializeComponent();
    }

    private void Protocol_SelectionChanged(object sender, System.Windows.Controls.SelectionChangedEventArgs e)
    {
        (DataContext as ViewModels.DeviceEditDialogViewModel)?.OnProtocolChanged();
    }
}
