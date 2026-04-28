using System.Configuration;
using System.Data;
using System.IO;
using System.Windows;

namespace Dabp.Tools
{
    /// <summary>
    /// Interaction logic for App.xaml
    /// </summary>
    public partial class App : Application
    {
        protected override void OnStartup(StartupEventArgs e)
        {
            base.OnStartup(e);
            AppDomain.CurrentDomain.UnhandledException += (s, ex) =>
            {
                File.WriteAllText("crash.log", ex.ExceptionObject.ToString());
            };
            var bootstrapper = new Bootstrapper();
            bootstrapper.Run();
        }
    }

}
