using System.Globalization;
using System.Windows.Data;

namespace DevStats.Converters;

public class FileSizeConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
    {
        if (value is double d)
        {
            return FormatSize((long)d);
        }

        if (value is long l)
        {
            return FormatSize(l);
        }

        if (value is int i)
        {
            return FormatSize(i);
        }

        return "0 B";
    }

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
    {
        throw new NotImplementedException();
    }

    private static string FormatSize(long bytes)
    {
        if (bytes >= 1024 * 1024)
        {
            return $"{bytes / (1024 * 1024.0):F2} MB";
        }

        if (bytes >= 1024)
        {
            return $"{bytes / 1024.0:F2} KB";
        }

        return $"{bytes} B";
    }
}