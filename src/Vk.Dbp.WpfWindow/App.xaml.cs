using Dabp.WpfWindow.Services;
using Prism.Ioc;
using Prism.Unity;
using Serilog;
using Serilog.Events;
using System.Data;
using System.Text;
using System.Windows;

namespace Dabp.WpfWindow
{
    public partial class App : Application
    {
        protected override void OnStartup(StartupEventArgs e)
        {
            base.OnStartup(e);

            var bootstrapper = new Bootstrapper();
            bootstrapper.Run();
        }

    }

  
}
