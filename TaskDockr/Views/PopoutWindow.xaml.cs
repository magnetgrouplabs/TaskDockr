using System;
using System.Windows;
using System.Windows.Controls;
using TaskDockr.Models;
using TaskDockr.Services;
using TaskDockr.ViewModels;

namespace TaskDockr.Views
{
    public partial class PopoutWindow : Window
    {
        private PopoutViewModel ViewModel => (PopoutViewModel)DataContext;

        public string? GroupId { get; }

        private readonly INavigationService _navigationService;
        private bool _isClosing;
        private bool _suppressClose;

        public PopoutWindow() : this(group: null) { }

        public PopoutWindow(Group? group)
        {
            GroupId = group?.Id;
            DataContext = new PopoutViewModel(group);
            InitializeComponent();

            _navigationService = App.GetService<INavigationService>();
            _navigationService.RegisterWindow(this, NavigationTarget.PopoutWindow);

            // Allow ViewModel to temporarily suppress auto-close while an edit dialog is open
            ViewModel.SuppressClose = () => _suppressClose = true;
            ViewModel.RestoreClose  = () => _suppressClose = false;

            // Suppress auto-close while any context menu inside this window is open
            AddHandler(ContextMenuService.ContextMenuOpeningEvent,
                new ContextMenuEventHandler((s, e) => _suppressClose = true));
            AddHandler(ContextMenuService.ContextMenuClosingEvent,
                new ContextMenuEventHandler((s, e) =>
                    Dispatcher.BeginInvoke(System.Windows.Threading.DispatcherPriority.Background,
                        new Action(() => _suppressClose = false))));

            Deactivated += (_, _) =>
            {
                Dispatcher.BeginInvoke(System.Windows.Threading.DispatcherPriority.Background,
                    new Action(() =>
                    {
                        if (!IsLoaded || _isClosing || IsActive || _suppressClose) return;
                        _isClosing = true;
                        Close();
                    }));
            };
        }

        public void RefreshData() => ViewModel?.RefreshData();

        private void OnGearClick(object sender, RoutedEventArgs e)
        {
            // Launch the main TaskDockr GUI (new process without --group args)
            try
            {
                var exePath = System.Diagnostics.Process.GetCurrentProcess().MainModule?.FileName;
                if (!string.IsNullOrEmpty(exePath))
                    System.Diagnostics.Process.Start(exePath);
            }
            catch { }
            Close();
        }

        // ── Shortcut context menu ─────────────────────────────────────────

        private Shortcut? GetContextShortcut(object sender)
        {
            if (sender is MenuItem mi &&
                mi.Parent is ContextMenu cm &&
                cm.PlacementTarget is FrameworkElement fe &&
                fe.DataContext is Shortcut s)
                return s;
            return null;
        }

        private void OnContextLaunch(object sender, RoutedEventArgs e)
        {
            var s = GetContextShortcut(sender);
            if (s != null) ViewModel?.LaunchShortcutCommand.Execute(s);
        }

        private void OnContextEdit(object sender, RoutedEventArgs e)
        {
            var s = GetContextShortcut(sender);
            if (s != null) ViewModel?.EditShortcutCommand.Execute(s);
        }

        private void OnContextDelete(object sender, RoutedEventArgs e)
        {
            var s = GetContextShortcut(sender);
            if (s != null) ViewModel?.DeleteShortcutCommand.Execute(s);
        }
    }
}
