using System;
using System.Globalization;
using System.Windows;
using System.Windows.Data;

namespace TaskDockr.Converters
{
    public class StringToVisibilityConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is string str)
            {
                bool isInverse = parameter as string == "inverse";
                bool isEmpty = string.IsNullOrEmpty(str);
                if (isInverse)
                    return isEmpty ? Visibility.Visible : Visibility.Collapsed;
                else
                    return !isEmpty ? Visibility.Visible : Visibility.Collapsed;
            }
            return Visibility.Collapsed;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}
