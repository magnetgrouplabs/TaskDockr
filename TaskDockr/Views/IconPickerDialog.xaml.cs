using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using FontAwesome6;
using FontAwesome6.Fonts.Extensions;
using TaskDockr.Services;
using TaskDockr.ViewModels;

namespace TaskDockr.Views
{
    public partial class IconPickerDialog : Window
    {
        private readonly IconPickerViewModel _vm;

        public IconPickerDialog()
        {
            InitializeComponent();

            var catalogService = App.GetService<IIconCatalogService>();
            _vm = new IconPickerViewModel(catalogService);
            _vm.CloseAction = success =>
            {
                DialogResult = success;
                Close();
            };
            DataContext = _vm;
        }

        public string ResultIconGlyph => _vm.ResultIconGlyph;
        public string ResultIconColor => _vm.ResultIconColor;
        public string ResultIconPath => _vm.ResultIconPath;

        private void OnIconClick(object sender, MouseButtonEventArgs e)
        {
            if (sender is FrameworkElement fe && fe.DataContext is IconCatalogEntry entry)
            {
                _vm.SelectedIcon = entry;
            }
        }

        private void OnColorClick(object sender, MouseButtonEventArgs e)
        {
            if (sender is FrameworkElement fe && fe.DataContext is string color)
            {
                _vm.SelectedColor = color;
            }
        }

        /// <summary>
        /// Sets the correct FontAwesome font family and unicode glyph on each icon tile.
        /// </summary>
        private void OnGlyphLoaded(object sender, RoutedEventArgs e)
        {
            if (sender is TextBlock tb && tb.DataContext is IconCatalogEntry entry)
            {
                try
                {
                    if (Enum.TryParse<EFontAwesomeIcon>(entry.UnicodeCodePoint, out var icon))
                    {
                        var unicode = icon.GetUnicode();
                        var fontFamily = icon.GetFontFamily();

                        if (!string.IsNullOrEmpty(unicode) && fontFamily != null)
                        {
                            tb.Text = unicode;
                            tb.FontFamily = fontFamily;
                        }
                        else
                        {
                            tb.Text = "?";
                        }
                    }
                }
                catch
                {
                    tb.Text = "?";
                }
            }
        }
    }
}
