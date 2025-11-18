using System;
using Microsoft.UI.Xaml.Data;

namespace Aegis.Desktop.Converters;

public class SecondsToTimeConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, string language)
    {
        if (value is int seconds)
        {
            return FormatSeconds(seconds);
        }
        return string.Empty;
    }

    public object ConvertBack(object value, Type targetType, object parameter, string language)
    {
        throw new NotImplementedException();
    }

    private string FormatSeconds(int seconds)
    {
        if (seconds < 60)
            return $"{seconds}s";
        if (seconds < 3600)
            return $"{seconds / 60}m";
        if (seconds < 86400)
            return $"{seconds / 3600}h";
        if (seconds < 604800)
            return $"{seconds / 86400}d";
        return $"{seconds / 604800}w";
    }
}
