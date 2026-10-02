using System;
using System.Windows;
using Serilog;

namespace Dabp.WpfWindow
{
    public partial class App : Application
    {
        protected override void OnStartup(StartupEventArgs e)
        {
            // 全局兜底：未处理的 UI 线程异常至少留下日志与提示，避免无痕闪退。
            // 不设置 e.Handled —— 记录后仍按默认策略终止，避免带病运行。
            DispatcherUnhandledException += OnDispatcherUnhandledException;

            try
            {
                base.OnStartup(e);

                var bootstrapper = new Bootstrapper();
                bootstrapper.Run();
            }
            catch (Exception ex)
            {
                // 配置缺失（ConfigurationValidator）等启动异常在此统一落地：
                // RegisterTypes 内 ConfigureLogging 先于 Validate 执行，日志通常已就绪；
                // 若异常发生在日志初始化之前，Serilog 静默无操作，MessageBox 仍是可靠出口。
                Log.Fatal(ex, "应用程序启动失败");
                MessageBox.Show(
                    $"应用程序启动失败：{ex.Message}\n\n" +
                    "请检查 appsettings.local.json / 环境变量配置（详见上方提示），" +
                    "详细信息见日志目录 %LOCALAPPDATA%\\<应用名>\\Logs。",
                    "启动错误",
                    MessageBoxButton.OK,
                    MessageBoxImage.Error);
                Shutdown(-1);
            }
        }

        private void OnDispatcherUnhandledException(object sender, System.Windows.Threading.DispatcherUnhandledExceptionEventArgs e)
        {
            Log.Fatal(e.Exception, "未处理的 UI 线程异常");
            MessageBox.Show(
                $"发生未处理的错误，程序即将退出：{e.Exception.Message}\n\n详细信息见日志目录 %LOCALAPPDATA%\\<应用名>\\Logs。",
                "严重错误",
                MessageBoxButton.OK,
                MessageBoxImage.Error);
        }
    }
}
