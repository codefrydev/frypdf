using System;
using System.Globalization;
using Avalonia.Data.Converters;
using Avalonia.Media;

namespace PdfEditorApp.Converters;

public class HexToBrushConverter : IValueConverter
{
    public static readonly HexToBrushConverter Instance = new();

    public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        if (value is string hex && !string.IsNullOrWhiteSpace(hex))
        {
            if (Color.TryParse(hex, out var color))
            {
                return new SolidColorBrush(color);
            }
        }
        return Brushes.Transparent;
    }

    public object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        if (value is ISolidColorBrush brush)
        {
            return brush.Color.ToString();
        }
        return "#00000000";
    }
}

public class HexToColorConverter : IValueConverter
{
    public static readonly HexToColorConverter Instance = new();

    public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        if (value is string hex && !string.IsNullOrWhiteSpace(hex))
        {
            if (Color.TryParse(hex, out var color))
            {
                return color;
            }
        }
        return Colors.Black;
    }

    public object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        if (value is Color color)
        {
            if (color.A == 255)
            {
                return $"#{color.R:X2}{color.G:X2}{color.B:X2}";
            }
            return $"#{color.A:X2}{color.R:X2}{color.G:X2}{color.B:X2}";
        }
        return "#201F1E";
    }
}
