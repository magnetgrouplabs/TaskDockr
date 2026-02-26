using System.Windows;
using TaskDockr.Models;
using TaskDockr.Services;
using TaskDockr.ViewModels;

namespace TaskDockr.Views
{
    public partial class ShortcutCreationForm : Window
    {
        public ShortcutCreationForm(Group group)
        {
            InitializeComponent();

            var shortcutService = App.GetService<IShortcutService>();
            var errorHandlingService = App.GetService<IErrorHandlingService>();

            var viewModel = new ShortcutFormViewModel(shortcutService, errorHandlingService, group);
            viewModel.CloseAction = success =>
            {
                DialogResult = success;
                Close();
            };

            DataContext = viewModel;
        }
    }
}
