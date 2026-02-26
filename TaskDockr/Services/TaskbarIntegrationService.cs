using System;
using System.Diagnostics;
using System.Threading.Tasks;
using System.Windows;

namespace TaskDockr.Services
{
    public interface ITaskbarIntegrationService
    {
        Task InitializeTrayIconAsync();
        Task UpdateTrayIconAsync(string iconPath);
        Task ShowNotificationAsync(string title, string message, NotificationType type = NotificationType.Info);
        Task HideTrayIconAsync();
        Task ShowTrayIconAsync();
        Task CleanupTrayIconAsync();
        bool IsTrayIconVisible { get; }
    }

    // The actual tray icon is the <tb:TaskbarIcon> declared in App.xaml.
    // This service handles lifecycle coordination and balloon notifications.
    public class TaskbarIntegrationService : ITaskbarIntegrationService
    {
        private readonly IConfigurationService _configService;
        private bool _isInitialized;
        private bool _isTrayIconVisible;

        public bool IsTrayIconVisible => _isTrayIconVisible;

        public TaskbarIntegrationService(IConfigurationService configService)
        {
            _configService = configService;
        }

        public Task InitializeTrayIconAsync()
        {
            if (_isInitialized) return Task.CompletedTask;
            _isInitialized    = true;
            _isTrayIconVisible = true;
            Debug.WriteLine("Tray icon service initialized (icon declared in App.xaml)");
            return Task.CompletedTask;
        }

        public Task UpdateTrayIconAsync(string iconPath)
        {
            // Icon is set at design time in App.xaml; runtime update could be added later.
            return Task.CompletedTask;
        }

        public Task ShowNotificationAsync(string title, string message, NotificationType type = NotificationType.Info)
        {
            try
            {
                // Access the TaskbarIcon declared in App.xaml resources
                if (Application.Current.Resources["TrayIcon"] is Hardcodet.Wpf.TaskbarNotification.TaskbarIcon trayIcon)
                {
                    var icon = type switch
                    {
                        NotificationType.Warning => Hardcodet.Wpf.TaskbarNotification.BalloonIcon.Warning,
                        NotificationType.Error   => Hardcodet.Wpf.TaskbarNotification.BalloonIcon.Error,
                        _                        => Hardcodet.Wpf.TaskbarNotification.BalloonIcon.Info
                    };
                    trayIcon.ShowBalloonTip(title, message, icon);
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Failed to show balloon notification: {ex.Message}");
            }
            return Task.CompletedTask;
        }

        public Task HideTrayIconAsync()
        {
            if (Application.Current.Resources["TrayIcon"] is Hardcodet.Wpf.TaskbarNotification.TaskbarIcon trayIcon)
                trayIcon.Visibility = Visibility.Collapsed;
            _isTrayIconVisible = false;
            return Task.CompletedTask;
        }

        public Task ShowTrayIconAsync()
        {
            if (Application.Current.Resources["TrayIcon"] is Hardcodet.Wpf.TaskbarNotification.TaskbarIcon trayIcon)
                trayIcon.Visibility = Visibility.Visible;
            _isTrayIconVisible = true;
            return Task.CompletedTask;
        }

        public Task CleanupTrayIconAsync()
        {
            _isInitialized    = false;
            _isTrayIconVisible = false;
            return Task.CompletedTask;
        }
    }

    public enum NotificationType
    {
        Info,
        Warning,
        Error,
        Success
    }
}
