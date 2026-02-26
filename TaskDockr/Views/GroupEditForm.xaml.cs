using System;
using System.ComponentModel;
using System.Windows;
using System.Windows.Media;
using FontAwesome6;
using FontAwesome6.Fonts.Extensions;
using TaskDockr.Models;
using TaskDockr.Services;
using TaskDockr.ViewModels;

namespace TaskDockr.Views
{
    public partial class GroupEditForm : Window
    {
        public GroupEditForm(Group group)
        {
            InitializeComponent();
            var groupService = App.GetService<IGroupService>();
            var vm = new GroupFormViewModel(groupService, group);
            vm.CloseAction = success => { DialogResult = success; Close(); };
            vm.OwnerWindow = this;
            DataContext = vm;

            vm.PropertyChanged += OnViewModelPropertyChanged;
            UpdateIconPreview(vm);
        }

        private void OnViewModelPropertyChanged(object? sender, PropertyChangedEventArgs e)
        {
            if (e.PropertyName is nameof(GroupFormViewModel.IconGlyph) or nameof(GroupFormViewModel.IconColor)
                or nameof(GroupFormViewModel.IconPath) or nameof(GroupFormViewModel.HasIconGlyph))
            {
                if (sender is GroupFormViewModel vm)
                    UpdateIconPreview(vm);
            }
        }

        private void UpdateIconPreview(GroupFormViewModel vm)
        {
            if (!string.IsNullOrEmpty(vm.IconGlyph) &&
                Enum.TryParse<EFontAwesomeIcon>(vm.IconGlyph, out var icon))
            {
                try
                {
                    IconPreviewGlyph.Text = icon.GetUnicode();
                    IconPreviewGlyph.FontFamily = icon.GetFontFamily();

                    if (!string.IsNullOrEmpty(vm.IconColor))
                    {
                        try
                        {
                            var color = (Color)ColorConverter.ConvertFromString(vm.IconColor);
                            IconPreviewGlyph.Foreground = new SolidColorBrush(color);
                        }
                        catch
                        {
                            IconPreviewGlyph.Foreground = Brushes.White;
                        }
                    }
                    else
                    {
                        IconPreviewGlyph.Foreground = Brushes.White;
                    }

                    IconPreviewGlyph.Visibility = Visibility.Visible;
                }
                catch
                {
                    IconPreviewGlyph.Visibility = Visibility.Collapsed;
                }
            }
            else
            {
                IconPreviewGlyph.Visibility = Visibility.Collapsed;
            }
        }
    }
}
