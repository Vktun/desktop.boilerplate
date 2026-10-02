using System.Windows;
using System.Windows.Controls;
using Vk.Dbp.DeviceModule.ViewModels;

namespace Vk.Dbp.DeviceModule.Views;

/// <summary>
/// 实时监控页。code-behind 仅承担 ScottPlot 桥接（纯视觉行为，LoginView 先例豁免）：
/// 订阅 VM 的 TrendDataChanged 事件重绘趋势图，VM 本身不依赖任何绘图库。
/// </summary>
public partial class DeviceMonitorView : UserControl
{
    private DeviceMonitorViewModel? _subscribedViewModel;

    /// <summary>
    /// 构造视图
    /// </summary>
    public DeviceMonitorView()
    {
        InitializeComponent();

        DataContextChanged += OnDataContextChanged;
        Unloaded += OnUnloaded;
    }

    private void OnDataContextChanged(object sender, DependencyPropertyChangedEventArgs e)
    {
        if (ReferenceEquals(_subscribedViewModel, e.NewValue))
        {
            return;
        }

        if (_subscribedViewModel is not null)
        {
            _subscribedViewModel.TrendDataChanged -= OnTrendDataChanged;
            _subscribedViewModel = null;
        }

        if (e.NewValue is DeviceMonitorViewModel viewModel)
        {
            _subscribedViewModel = viewModel;
            viewModel.TrendDataChanged += OnTrendDataChanged;
            RenderTrend(viewModel);
        }
    }

    private void OnUnloaded(object sender, RoutedEventArgs e)
    {
        // 页面离开视觉树只解绑重绘回调；VM 是长生命周期单例语义（IsNavigationTarget=true），
        // 重新进入时 DataContext 不变，仍由 DataContextChanged 兜底重绘
        if (_subscribedViewModel is not null)
        {
            _subscribedViewModel.TrendDataChanged -= OnTrendDataChanged;
        }
    }

    private void OnTrendDataChanged(object? sender, EventArgs e)
    {
        if (_subscribedViewModel is null)
        {
            return;
        }

        // 事件由 UIThread 订阅方触发（VM 以 UIThread 选项订阅引擎事件），此处已在 UI 线程
        RenderTrend(_subscribedViewModel);
    }

    private void RenderTrend(DeviceMonitorViewModel viewModel)
    {
        var plot = TrendPlot.Plot;
        plot.Clear();

        if (viewModel.CurrentTrend is { Count: > 1 } samples)
        {
            var xs = new double[samples.Count];
            var ys = new double[samples.Count];
            var hasValue = false;
            for (var i = 0; i < samples.Count; i++)
            {
                xs[i] = samples[i].Timestamp.ToOADate();
                ys[i] = samples[i].Value ?? double.NaN;
                if (samples[i].Value is not null)
                {
                    hasValue = true;
                }
            }

            if (hasValue)
            {
                plot.Add.Scatter(xs, ys);
                plot.Axes.DateTimeTicksBottom();
            }
        }

        TrendPlot.Refresh();
    }
}
