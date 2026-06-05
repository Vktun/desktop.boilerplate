using System;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;
using Prism.Commands;
using Prism.Events;
using Prism.Mvvm;
using Vk.Dbp.AccountModule.Models;
using Vk.Dbp.AccountModule.Services;
using Vk.Dbp.Contracts.Events;
using Vk.Dbp.Services.Session;

namespace Vk.Dbp.WpfWindow.ViewModels
{
    public class AppNotificationViewModel : BindableBase, IDisposable
    {
        private readonly INotificationService _notificationService;
        private readonly IUserSession _userSession;
        private readonly IEventAggregator _eventAggregator;
        private SubscriptionToken? _notificationToken;
        private SubscriptionToken? _countChangedToken;
        private bool _isDisposed;

        private ObservableCollection<Notification> _notifications = new();
        public ObservableCollection<Notification> Notifications
        {
            get { return _notifications; }
            set { SetProperty(ref _notifications, value); }
        }

        private Notification? _selectedNotification;
        public Notification? SelectedNotification
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
        public DelegateCommand<Notification?> MarkAsReadCommand { get; }
        public DelegateCommand MarkAllAsReadCommand { get; }
        public DelegateCommand<Notification?> DeleteNotificationCommand { get; }

        public AppNotificationViewModel(
            INotificationService notificationService,
            IUserSession userSession,
            IEventAggregator eventAggregator)
        {
            _notificationService = notificationService ?? throw new ArgumentNullException(nameof(notificationService));
            _userSession = userSession ?? throw new ArgumentNullException(nameof(userSession));
            _eventAggregator = eventAggregator ?? throw new ArgumentNullException(nameof(eventAggregator));

            Notifications = new ObservableCollection<Notification>();

            LoadCommand = new DelegateCommand(async () => await LoadNotifications());
            MarkAsReadCommand = new DelegateCommand<Notification?>(async n => await MarkAsRead(n), CanMarkAsRead);
            MarkAllAsReadCommand = new DelegateCommand(async () => await MarkAllAsRead(), CanMarkAllAsRead);
            DeleteNotificationCommand = new DelegateCommand<Notification?>(async n => await DeleteNotification(n), CanDeleteNotification);

            // Subscribe to global notification events
            SubscribeToEvents();
        }

        /// <summary>
        /// Subscribe to notification events from EventAggregator
        /// </summary>
        private void SubscribeToEvents()
        {
            _notificationToken = _eventAggregator.GetEvent<GlobalNotificationEvent>()
                .Subscribe(OnNotificationReceived,
                    ThreadOption.UIThread,
                    keepSubscriberReferenceAlive: true);

            _countChangedToken = _eventAggregator.GetEvent<NotificationCountChangedEvent>()
                .Subscribe(OnNotificationCountChanged,
                    ThreadOption.UIThread,
                    keepSubscriberReferenceAlive: true);
        }

        /// <summary>
        /// Handle incoming global notification
        /// </summary>
        private void OnNotificationReceived(GlobalNotificationPayload payload)
        {
            if (_isDisposed) return;
            // Filter: only show notifications for current user or broadcast notifications
            if (!_userSession.IsLoggedIn)
                return;

            if (payload.TargetUserId.HasValue && payload.TargetUserId != _userSession.UserId)
                return; // This notification is for another user

            // Convert payload to Notification model
            var notification = new Notification
            {
                Id = payload.Id,
                Title = payload.Title,
                Content = payload.Content,
                Type = payload.Type.ToString(),
                IsRead = payload.IsRead,
                CreatedTime = payload.CreatedTime,
                UserId = payload.TargetUserId ?? _userSession.UserId
            };

            // Insert at the top of the list
            Notifications.Insert(0, notification);

            // Update unread count if this is a new notification
            if (!notification.IsRead)
            {
                UnreadCount++;
            }
        }

        /// <summary>
        /// Handle notification count changed event
        /// </summary>
        private void OnNotificationCountChanged(NotificationCountChangedPayload payload)
        {
            if (_isDisposed) return;
            // Only update if this event is for the current user
            if (_userSession.IsLoggedIn && payload.UserId == _userSession.UserId)
            {
                UnreadCount = payload.UnreadCount;
            }
        }

        private async Task LoadNotifications()
        {
            IsLoading = true;
            try
            {
                if (!_userSession.IsLoggedIn)
                {
                    Notifications = new ObservableCollection<Notification>();
                    UnreadCount = 0;
                    return;
                }

                var notifications = await _notificationService.GetNotificationsByUserIdAsync(_userSession.UserId);
                Notifications = new ObservableCollection<Notification>(notifications);
                UnreadCount = notifications.Count(n => !n.IsRead);
            }
            finally
            {
                IsLoading = false;
            }
        }

        private async Task MarkAsRead(Notification? notification)
        {
            if (notification == null)
                return;

            await _notificationService.MarkAsReadAsync(notification.Id);
            notification.IsRead = true;
            await UpdateUnreadCount();
        }

        private bool CanMarkAsRead(Notification? notification)
        {
            return notification != null && !notification.IsRead;
        }

        private async Task MarkAllAsRead()
        {
            if (!_userSession.IsLoggedIn)
                return;

            await _notificationService.MarkAllAsReadAsync(_userSession.UserId);
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

        private async Task DeleteNotification(Notification? notification)
        {
            if (notification == null)
                return;

            await _notificationService.DeleteNotificationAsync(notification.Id);
            Notifications.Remove(notification);
            await UpdateUnreadCount();
        }

        private bool CanDeleteNotification(Notification? notification)
        {
            return notification != null;
        }

        private async Task UpdateUnreadCount()
        {
            if (!_userSession.IsLoggedIn)
                return;

            UnreadCount = await _notificationService.GetUnreadCountAsync(_userSession.UserId);
        }

        public void Dispose()
        {
            if (_isDisposed) return;

            _notificationToken?.Dispose();
            _countChangedToken?.Dispose();

            _isDisposed = true;
        }
    }
}
