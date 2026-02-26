using System.Windows;
using System.Windows.Controls;
using TaskDockr.Models;
using TaskDockr.Services;
using TaskDockr.ViewModels;

namespace TaskDockr.Views
{
    public partial class GroupPopoutMenu : UserControl
    {
        private GroupPopoutMenuViewModel ViewModel => (GroupPopoutMenuViewModel)DataContext;

        public GroupPopoutMenu()
        {
            InitializeComponent();
            DataContext = new GroupPopoutMenuViewModel();
        }

        public GroupPopoutMenu(GroupPopoutMenuViewModel viewModel)
        {
            InitializeComponent();
            DataContext = viewModel;
        }

        private void OnShortcutClick(object sender, RoutedEventArgs e)
        {
            if (sender is Button btn && btn.DataContext is Shortcut shortcut)
                ViewModel?.LaunchShortcut(shortcut);
        }

        private void OnEditGroupClick(object sender, RoutedEventArgs e)
        {
            if (ViewModel?.Group != null)
            {
                var form = new GroupEditForm(ViewModel.Group);
                form.ShowDialog();
            }
        }

        private void OnAddShortcutClick(object sender, RoutedEventArgs e)
        {
            // Navigate to shortcut management
            App.GetService<INavigationService>()
                .NavigateToAsync(NavigationTarget.ShortcutManagement, ViewModel?.Group);
        }
    }
}
