using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Vk.Dbp.WorkshopModule.ViewModels
{
    public class AuditRecordViewModel : BindableBase, INavigationAware
    {
        private string _title = "ViewA";
        public string Title
        {
            get { return _title; }
            set { SetProperty(ref _title, value); }
        }

        private int _pageViews;
        public int PageViews
        {
            get { return _pageViews; }
            set { SetProperty(ref _pageViews, value); }
        }

        public AuditRecordViewModel()
        {

        }

        public void OnNavigatedTo(NavigationContext navigationContext)
        {
            PageViews++;
        }

        public bool IsNavigationTarget(NavigationContext navigationContext)
        {
            return true;
        }

        public void OnNavigatedFrom(NavigationContext navigationContext)
        {

        }
    }
}