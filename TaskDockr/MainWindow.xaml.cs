using System;
using System.Windows;
using TaskDockr.Services;
using TaskDockr.ViewModels;

namespace TaskDockr
{
    public partial class MainWindow : Window
    {
        private readonly INavigationService _navigationService;
        private readonly ITaskbarService    _taskbarService;

        public MainWindow()
        {
            InitializeComponent();
            DataContext = App.GetService<MainViewModel>();


            _navigationService = App.GetService<INavigationService>();
            _taskbarService    = App.GetService<ITaskbarService>();

            _navigationService.RegisterWindow(this, NavigationTarget.MainWindow);
            _navigationService.RegisterContentControl(MainContent);

            Closed += OnWindowClosed;
        }

        private void OnWindowClosed(object? sender, EventArgs e)
        {
            _taskbarService.CleanupAsync().GetAwaiter().GetResult();
        }

        private void OnManageGroupsClick(object sender, RoutedEventArgs e)
            => _navigationService.NavigateToAsync(NavigationTarget.GroupManagement);

        private void OnSettingsClick(object sender, RoutedEventArgs e)
            => _navigationService.NavigateToAsync(NavigationTarget.Settings);
    }
}
