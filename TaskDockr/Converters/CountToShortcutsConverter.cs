using System;
using System.Globalization;
using System.Windows.Data;

namespace TaskDockr.Converters
{
    public class CountToShortcutsTextConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is int count)
                return count == 1 ? "1 shortcut" : $"{count} shortcuts";
            return "0 shortcuts";
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}
