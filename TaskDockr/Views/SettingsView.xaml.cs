using System.Windows;
using System.Windows.Controls;
using TaskDockr.Models;
using TaskDockr.Services;
using TaskDockr.ViewModels;

namespace TaskDockr.Views
{
    public partial class SettingsView : UserControl
    {
        public SettingsView()
        {
            InitializeComponent();
        }

        private void OnBackClick(object sender, RoutedEventArgs e)
        {
            App.GetService<INavigationService>().NavigateBackAsync();
        }

        private async void OnThemeChanged(object sender, SelectionChangedEventArgs e)
        {
            if (DataContext is SettingsViewModel vm && e.AddedItems.Count > 0)
            {
                // SelectedThemeIndex is already updated via binding
                // Just save the config and apply the theme
                await App.GetService<IConfigurationService>().SaveConfigAsync(vm.Config);

                // Apply theme
                var themePreference = (ThemePreference)ThemeCombo.SelectedIndex;
                var theme = themePreference switch
                {
                    ThemePreference.Light => Wpf.Ui.Appearance.ApplicationTheme.Light,
                    ThemePreference.Dark => Wpf.Ui.Appearance.ApplicationTheme.Dark,
                    ThemePreference.Auto => Wpf.Ui.Appearance.ApplicationThemeManager.GetSystemTheme() switch
                    {
                        Wpf.Ui.Appearance.SystemTheme.Light => Wpf.Ui.Appearance.ApplicationTheme.Light,
                        Wpf.Ui.Appearance.SystemTheme.Glow => Wpf.Ui.Appearance.ApplicationTheme.Light,
                        _ => Wpf.Ui.Appearance.ApplicationTheme.Dark
                    },
                    _ => Wpf.Ui.Appearance.ApplicationTheme.Dark
                };
                
                Dispatcher.Invoke(() =>
                {
                    Wpf.Ui.Appearance.ApplicationThemeManager.Apply(theme);
                });
            }
        }
    }
}
