using System.Windows;
using TaskDockr.Models;
using TaskDockr.Services;
using TaskDockr.ViewModels;

namespace TaskDockr.Views
{
    public partial class ShortcutEditForm : Window
    {
        public ShortcutEditForm(Group group, Shortcut shortcut)
        {
            InitializeComponent();

            var shortcutService = App.GetService<IShortcutService>();
            var errorHandlingService = App.GetService<IErrorHandlingService>();

            var viewModel = new ShortcutFormViewModel(shortcutService, errorHandlingService, group, shortcut);
            viewModel.CloseAction = success =>
            {
                DialogResult = success;
                Close();
            };

            DataContext = viewModel;
        }
    }
}
