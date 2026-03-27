using System;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;
using Prism.Commands;
using Prism.Mvvm;
using Vk.Dbp.AccountModule.Models;
using Vk.Dbp.AccountModule.Services;

namespace Vk.Dbp.WpfWindow.ViewModels
{
    public class AppNotificationViewModel : BindableBase
    {
        private readonly INotificationService _notificationService;

        private ObservableCollection<Notification> _notifications;
        public ObservableCollection<Notification> Notifications
        {
            get { return _notifications; }
            set { SetProperty(ref _notifications, value); }
        }

        private Notification _selectedNotification;
        public Notification SelectedNotification
        {
            get { return _selectedNotification; }
            set { SetProperty(ref _selectedNotification, value); }
        }

        private int _unreadCount;
        public int UnreadCount
        {
            get { return _unreadCount; }
            set { SetProperty(ref _unreadCount, value); }
        }

        private bool _isLoading;
        public bool IsLoading
        {
            get { return _isLoading; }
            set { SetProperty(ref _isLoading, value); }
        }

        public DelegateCommand LoadCommand { get; }
        public DelegateCommand<Notification> MarkAsReadCommand { get; }
        public DelegateCommand MarkAllAsReadCommand { get; }
        public DelegateCommand<Notification> DeleteNotificationCommand { get; }

        public AppNotificationViewModel(INotificationService notificationService)
        {
            _notificationService = notificationService ?? throw new ArgumentNullException(nameof(notificationService));

            Notifications = new ObservableCollection<Notification>();

            LoadCommand = new DelegateCommand(async () => await LoadNotifications());
            MarkAsReadCommand = new DelegateCommand<Notification>(async n => await MarkAsRead(n), CanMarkAsRead);
            MarkAllAsReadCommand = new DelegateCommand(async () => await MarkAllAsRead(), CanMarkAllAsRead);
            DeleteNotificationCommand = new DelegateCommand<Notification>(async n => await DeleteNotification(n), CanDeleteNotification);
        }

        private async Task LoadNotifications()
        {
            IsLoading = true;
            try
            {
                var notifications = await _notificationService.GetNotificationsByUserIdAsync(1);
                Notifications = new ObservableCollection<Notification>(notifications);
                UnreadCount = notifications.Count(n => !n.IsRead);
            }
            finally
            {
                IsLoading = false;
            }
        }

        private async Task MarkAsRead(Notification notification)
        {
            if (notification == null)
                return;

            await _notificationService.MarkAsReadAsync(notification.Id);
            notification.IsRead = true;
            await UpdateUnreadCount();
        }

        private bool CanMarkAsRead(Notification notification)
        {
            return notification != null && !notification.IsRead;
        }

        private async Task MarkAllAsRead()
        {
            await _notificationService.MarkAllAsReadAsync(1);
            foreach (var notification in Notifications)
            {
                notification.IsRead = true;
            }
            UnreadCount = 0;
        }

        private bool CanMarkAllAsRead()
        {
            return UnreadCount > 0;
        }

        private async Task DeleteNotification(Notification notification)
        {
            if (notification == null)
                return;

            await _notificationService.DeleteNotificationAsync(notification.Id);
            Notifications.Remove(notification);
            await UpdateUnreadCount();
        }

        private bool CanDeleteNotification(Notification notification)
        {
            return notification != null;
        }

        private async Task UpdateUnreadCount()
        {
            UnreadCount = await _notificationService.GetUnreadCountAsync(1);
        }
    }
}
