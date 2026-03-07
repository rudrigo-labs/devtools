using System;
using System.Globalization;
using System.Windows;
using System.Windows.Data;

namespace DevTools.Presentation.Wpf.Components;

public class StringVisibilityConverter : IValueConverter
{
    public static readonly StringVisibilityConverter Instance = new();

    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
    {
        return string.IsNullOrWhiteSpace(value as string) ? Visibility.Collapsed : Visibility.Visible;
    }

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
    {
        throw new NotImplementedException();
    }
}
