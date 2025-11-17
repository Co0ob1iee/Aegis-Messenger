using System;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Data;

namespace Aegis.Desktop.Converters;

public class NullToVisibilityConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, string language)
    {
        if (value == null)
            return Visibility.Collapsed;

        if (value is int intValue)
            return intValue > 0 ? Visibility.Visible : Visibility.Collapsed;

        if (value is int? nullableValue)
            return nullableValue.HasValue ? Visibility.Visible : Visibility.Collapsed;

        return Visibility.Visible;
    }

    public object ConvertBack(object value, Type targetType, object parameter, string language)
    {
        throw new NotImplementedException();
    }
}
