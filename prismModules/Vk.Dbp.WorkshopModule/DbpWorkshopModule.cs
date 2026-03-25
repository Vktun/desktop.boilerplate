
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Controls;
using Vk.Dbp.WorkshopModule.Views;

namespace Vk.Dbp.WorkshopModule
{
    public class DbpWorkshopModule : IModule
    {
        public void OnInitialized(IContainerProvider containerProvider)
        {

        }

        public void RegisterTypes(IContainerRegistry containerRegistry)
        {
            containerRegistry.RegisterForNavigation<Dashboard>();
            containerRegistry.RegisterForNavigation<Production>();
            containerRegistry.RegisterForNavigation<SelfCheck>();
            containerRegistry.RegisterForNavigation<AlarmRecord>();
            containerRegistry.RegisterForNavigation<AuditRecord>();
            containerRegistry.RegisterForNavigation<ProductionRecord>();
        }
    }
}
