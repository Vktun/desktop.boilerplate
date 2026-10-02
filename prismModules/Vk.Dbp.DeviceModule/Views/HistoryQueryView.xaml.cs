using System.Windows;
using System.Windows.Controls;
using Vk.Dbp.DeviceModule.ViewModels;

namespace Vk.Dbp.DeviceModule.Views;

/// <summary>
/// 趋势历史查询页。code-behind 仅承担 ScottPlot 桥接（DeviceMonitorView 同款模式）。
/// </summary>
public partial class HistoryQueryView : UserControl
{
    private HistoryQueryViewModel? _subscribedViewModel;

    /// <summary>
    /// 构造视图
    /// </summary>
    public HistoryQueryView()
    {
        InitializeComponent();

        DataContextChanged += OnDataContextChanged;
    }

    private void OnDataContextChanged(object sender, DependencyPropertyChangedEventArgs e)
    {
        if (_subscribedViewModel is not null)
        {
            _subscribedViewModel.SamplesChanged -= OnSamplesChanged;
            _subscribedViewModel = null;
        }

        if (e.NewValue is HistoryQueryViewModel viewModel)
        {
            _subscribedViewModel = viewModel;
            viewModel.SamplesChanged += OnSamplesChanged;
            RenderTrend(viewModel);
        }
    }

    private void OnSamplesChanged(object? sender, EventArgs e)
    {
        if (_subscribedViewModel is not null)
        {
            RenderTrend(_subscribedViewModel);
        }
    }

    private void RenderTrend(HistoryQueryViewModel viewModel)
    {
        var plot = TrendPlot.Plot;
        plot.Clear();

        if (viewModel.CurrentSamples is { Count: > 1 } samples)
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
