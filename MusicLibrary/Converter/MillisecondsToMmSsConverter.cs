using System;
using System.Globalization;
using System.Windows.Data;

namespace MusicLibrary.Converter;

public class MillisecondsToMmSsConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
    {
        if (value is not int ms || ms < 0) return "";
        var ts = TimeSpan.FromMilliseconds(ms);
        return $"{(int)ts.TotalMinutes}:{ts.Seconds:D2}";
    }

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        => Binding.DoNothing;
}
