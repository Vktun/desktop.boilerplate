using System;
using System.Threading.Tasks;
using Prism.Commands;
using Prism.Mvvm;
using Prism.Navigation.Regions;
using Vk.Dbp.AccountModule.Models;
using Vk.Dbp.AccountModule.Services;

namespace Vk.Dbp.AccountModule.ViewModels
{
    /// <summary>
    /// 修改密码ViewModel
    /// </summary>
    public class ChangePasswordViewModel : BindableBase, INavigationAware
    {
        private readonly IUserService _userService;
        private readonly IRegionManager _regionManager;

        private string _message;
        /// <summary>
        /// 提示消息
        /// </summary>
        public string Message
        {
            get { return _message; }
            set { SetProperty(ref _message, value); }
        }

        private bool _showMessage;
        /// <summary>
        /// 是否显示消息
        /// </summary>
        public bool ShowMessage
        {
            get { return _showMessage; }
            set { SetProperty(ref _showMessage, value); }
        }

        public DelegateCommand ChangeCommand { get; }
        public DelegateCommand CancelCommand { get; }

        public ChangePasswordViewModel(IUserService userService, IRegionManager regionManager)
        {
            _userService = userService ?? throw new ArgumentNullException(nameof(userService));
            _regionManager = regionManager ?? throw new ArgumentNullException(nameof(regionManager));

            ChangeCommand = new DelegateCommand(async () => await Change());
            CancelCommand = new DelegateCommand(Cancel);
        }

        private async Task Change()
        {
            // TODO: 实现密码修改逻辑
            Message = "密码修改成功";
            ShowMessage = true;

            await Task.Delay(1500);
            _regionManager.RequestNavigate("ContentRegion", "Dashboard");
        }

        private void Cancel()
        {
            _regionManager.RequestNavigate("ContentRegion", "Dashboard");
        }

        public void OnNavigatedTo(NavigationContext navigationContext)
        {
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
