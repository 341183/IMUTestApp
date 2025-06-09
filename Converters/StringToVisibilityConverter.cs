using System;
using System.Globalization;
using System.Windows;
using System.Windows.Data;

namespace IMUTestApp.Converters
{
    public class StringToVisibilityConverter : IValueConverter
    {
        public static readonly StringToVisibilityConverter Instance = new();
        
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is string str)
            {
                return string.IsNullOrEmpty(str) ? Visibility.Collapsed : Visibility.Visible;
            }
            return Visibility.Collapsed; // 返回默认值而不是 null
        }
        
        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            // 返回合理的默认值而不是抛出异常
            return value is Visibility visibility && visibility == Visibility.Visible ? "" : null;
        }
    }
}