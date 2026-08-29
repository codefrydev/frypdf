using System;
using System.Globalization;
using Avalonia;
using Avalonia.Data.Converters;
using Avalonia.Media;
using PdfEditorApp.Models;

namespace PdfEditorApp.Converters;

public class EqualityToBooleanConverter : IValueConverter
{
    public static readonly EqualityToBooleanConverter Instance = new();

    public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        if (value == null && parameter == null) return true;
        if (value == null || parameter == null) return false;
        return string.Equals(value.ToString(), parameter.ToString(), StringComparison.OrdinalIgnoreCase);
    }

    public object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        throw new NotSupportedException();
    }
}

public class ZoomPercentageConverter : IValueConverter
{
    public static readonly ZoomPercentageConverter Instance = new();

    public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        if (value is double zoom)
        {
            return $"{(int)Math.Round(zoom * 100)}%";
        }
        return "100%";
    }

    public object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        throw new NotSupportedException();
    }
}

public class NullToBooleanConverter : IValueConverter
{
    public static readonly NullToBooleanConverter Instance = new();

    public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        bool isNull = value == null;
        if (parameter is string paramStr && paramStr.Equals("invert", StringComparison.OrdinalIgnoreCase))
        {
            return isNull;
        }
        return !isNull;
    }

    public object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        throw new NotSupportedException();
    }
}

public class BooleanToBrushConverter : IValueConverter
{
    public static readonly BooleanToBrushConverter Instance = new();

    public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        bool isTrue = value is true;
        if (parameter is string paramStr)
        {
            var parts = paramStr.Split('|');
            string trueHex = parts.Length > 0 ? parts[0] : "#0F6CBD";
            string falseHex = parts.Length > 1 ? parts[1] : "#E2E8F0";

            string selectedHex = isTrue ? trueHex : falseHex;
            if (Color.TryParse(selectedHex, out var col))
            {
                return new SolidColorBrush(col);
            }
        }
        return isTrue ? new SolidColorBrush(Color.Parse("#0F6CBD")) : new SolidColorBrush(Color.Parse("#E2E8F0"));
    }

    public object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        throw new NotSupportedException();
    }
}

public class BooleanToThicknessConverter : IValueConverter
{
    public static readonly BooleanToThicknessConverter Instance = new();

    public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        bool isTrue = value is true;
        if (parameter is string paramStr)
        {
            var parts = paramStr.Split('|');
            double trueVal = parts.Length > 0 && double.TryParse(parts[0], out var t) ? t : 2.0;
            double falseVal = parts.Length > 1 && double.TryParse(parts[1], out var f) ? f : 1.0;
            return new Thickness(isTrue ? trueVal : falseVal);
        }
        return new Thickness(isTrue ? 2.0 : 1.0);
    }

    public object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        throw new NotSupportedException();
    }
}

public class DoubleToThicknessConverter : IValueConverter
{
    public static readonly DoubleToThicknessConverter Instance = new();

    public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        if (value is double d)
        {
            return new Thickness(d);
        }
        if (value is int i)
        {
            return new Thickness(i);
        }
        if (value is float f)
        {
            return new Thickness(f);
        }
        return new Thickness(0);
    }

    public object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        if (value is Thickness t)
        {
            return t.Left;
        }
        return 0.0;
    }
}

public class DoubleToCornerRadiusConverter : IValueConverter
{
    public static readonly DoubleToCornerRadiusConverter Instance = new();

    public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        if (value is double d)
        {
            return new CornerRadius(d);
        }
        if (value is int i)
        {
            return new CornerRadius(i);
        }
        if (value is float f)
        {
            return new CornerRadius(f);
        }
        return new CornerRadius(0);
    }

    public object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        if (value is CornerRadius c)
        {
            return c.TopLeft;
        }
        return 0.0;
    }
}

public class TextAlignmentConverter : IValueConverter
{
    public static readonly TextAlignmentConverter Instance = new();

    public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        if (value is TextAlignmentMode mode)
        {
            return mode switch
            {
                TextAlignmentMode.Left => Avalonia.Media.TextAlignment.Left,
                TextAlignmentMode.Center => Avalonia.Media.TextAlignment.Center,
                TextAlignmentMode.Right => Avalonia.Media.TextAlignment.Right,
                TextAlignmentMode.Justify => Avalonia.Media.TextAlignment.Justify,
                _ => Avalonia.Media.TextAlignment.Left
            };
        }
        return Avalonia.Media.TextAlignment.Left;
    }

    public object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        throw new NotSupportedException();
    }
}

public class BooleanToFontWeightConverter : IValueConverter
{
    public static readonly BooleanToFontWeightConverter Instance = new();

    public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        bool isBold = value is true;
        return isBold ? FontWeight.Bold : FontWeight.Normal;
    }

    public object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        return value is FontWeight weight && weight >= FontWeight.Bold;
    }
}

public class BooleanToFontStyleConverter : IValueConverter
{
    public static readonly BooleanToFontStyleConverter Instance = new();

    public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        bool isItalic = value is true;
        return isItalic ? FontStyle.Italic : FontStyle.Normal;
    }

    public object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        return value is FontStyle style && style == FontStyle.Italic;
    }
}

public class BooleanToTextDecorationsConverter : IValueConverter
{
    public static readonly BooleanToTextDecorationsConverter Instance = new();

    public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        bool isUnderline = value is true;
        return isUnderline ? TextDecorations.Underline : null;
    }

    public object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        return value is TextDecorationCollection decs && decs.Count > 0;
    }
}
