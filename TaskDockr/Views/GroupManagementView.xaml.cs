using System.Windows;
using System.Windows.Controls;
using TaskDockr.Services;

namespace TaskDockr.Views
{
    public partial class GroupManagementView : UserControl
    {
        public GroupManagementView()
        {
            InitializeComponent();
        }

        private void OnBackClick(object sender, RoutedEventArgs e)
        {
            App.GetService<INavigationService>().NavigateBackAsync();
        }
    }
}
