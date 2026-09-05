using System;
using System.Collections;
using System.Collections.Generic;
using System.ComponentModel;
using System.Globalization;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Text.RegularExpressions;
using Avalonia;
using Avalonia.Data.Converters;
using Avalonia.Media;

namespace PdfEditorApp.Converters;

#region 1. File Size & Formatting Converters

/// <summary>
/// Formats byte counts (long/int/double) into human-readable strings like "1.45 MB", "340 KB", "12 B", "2.1 GB".
/// Parameter can specify decimal places (e.g. "0", "1", "2"). Defaults to 1 decimal place.
/// </summary>
public class ByteSizeToStringConverter : IValueConverter
{
    public static readonly ByteSizeToStringConverter Instance = new();

    private static readonly string[] SizeSuffixes = { "B", "KB", "MB", "GB", "TB", "PB" };

    public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        if (value == null) return "0 B";

        double bytes;
        try
        {
            bytes = System.Convert.ToDouble(value, CultureInfo.InvariantCulture);
        }
        catch
        {
            return "0 B";
        }

        if (bytes < 0) return "-" + Convert(-bytes, targetType, parameter, culture);
        if (bytes == 0) return "0 B";

        int decimalPlaces = 1;
        if (parameter is string paramStr && int.TryParse(paramStr, out int dp))
        {
            decimalPlaces = Math.Clamp(dp, 0, 4);
        }

        int mag = (int)Math.Log(bytes, 1024);
        mag = Math.Clamp(mag, 0, SizeSuffixes.Length - 1);

        double adjustedSize = bytes / Math.Pow(1024, mag);

        // For Bytes ("B"), don't show fractional parts
        if (mag == 0)
        {
            return $"{(long)bytes} B";
        }

        string format = $"F{decimalPlaces}";
        return $"{adjustedSize.ToString(format, culture ?? CultureInfo.InvariantCulture)} {SizeSuffixes[mag]}";
    }

    public object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        if (value is string str && !string.IsNullOrWhiteSpace(str))
        {
            str = str.Trim();
            for (int i = SizeSuffixes.Length - 1; i >= 0; i--)
            {
                string suffix = SizeSuffixes[i];
                if (str.EndsWith(suffix, StringComparison.OrdinalIgnoreCase))
                {
                    string numPart = str[..^suffix.Length].Trim();
                    if (double.TryParse(numPart, NumberStyles.Float, culture ?? CultureInfo.InvariantCulture, out double num))
                    {
                        long result = (long)(num * Math.Pow(1024, i));
                        if (targetType == typeof(int)) return (int)result;
                        if (targetType == typeof(double)) return (double)result;
                        return result;
                    }
                }
            }
        }
        return 0L;
    }
}

/// <summary>
/// Formats DateTime / DateTimeOffset / DateOnly into customized date/time format strings.
/// Supports parameter strings like "yyyy-MM-dd", "g", "MMM dd, yyyy", "t", "Relative".
/// </summary>
public class DateTimeFormatConverter : IValueConverter
{
    public static readonly DateTimeFormatConverter Instance = new();

    public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        if (value == null) return string.Empty;

        DateTime dt;
        if (value is DateTime dateTime)
        {
            dt = dateTime;
        }
        else if (value is DateTimeOffset dto)
        {
            dt = dto.LocalDateTime;
        }
        else if (value is DateOnly dateOnly)
        {
            dt = dateOnly.ToDateTime(TimeOnly.MinValue);
        }
        else
        {
            return value.ToString();
        }

        string format = parameter as string ?? "g";

        if (format.Equals("Relative", StringComparison.OrdinalIgnoreCase))
        {
            var span = DateTime.Now - dt;
            if (span.TotalSeconds < 60) return "Just now";
            if (span.TotalMinutes < 60) return $"{(int)span.TotalMinutes}m ago";
            if (span.TotalHours < 24) return $"{(int)span.TotalHours}h ago";
            if (span.TotalDays < 7) return $"{(int)span.TotalDays}d ago";
            return dt.ToString("MMM dd, yyyy", culture ?? CultureInfo.InvariantCulture);
        }

        return dt.ToString(format, culture ?? CultureInfo.InvariantCulture);
    }

    public object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        if (value is string str && DateTime.TryParse(str, culture ?? CultureInfo.InvariantCulture, out var dt))
        {
            if (targetType == typeof(DateTimeOffset)) return new DateTimeOffset(dt);
            if (targetType == typeof(DateOnly)) return DateOnly.FromDateTime(dt);
            return dt;
        }
        return null;
    }
}

/// <summary>
/// Formats TimeSpan or seconds into human-readable duration strings like "01:23", "1h 30m", or "45s".
/// </summary>
public class TimeSpanFormatConverter : IValueConverter
{
    public static readonly TimeSpanFormatConverter Instance = new();

    public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        if (value == null) return "0:00";

        TimeSpan ts;
        if (value is TimeSpan t)
        {
            ts = t;
        }
        else
        {
            try
            {
                double seconds = System.Convert.ToDouble(value, CultureInfo.InvariantCulture);
                ts = TimeSpan.FromSeconds(seconds);
            }
            catch
            {
                return "0:00";
            }
        }

        string format = parameter as string ?? "timer";

        if (format.Equals("words", StringComparison.OrdinalIgnoreCase))
        {
            if (ts.TotalHours >= 1) return $"{(int)ts.TotalHours}h {ts.Minutes}m";
            if (ts.TotalMinutes >= 1) return $"{(int)ts.TotalMinutes}m {ts.Seconds}s";
            return $"{ts.Seconds}s";
        }

        if (ts.TotalHours >= 1)
        {
            return $"{(int)ts.TotalHours}:{ts.Minutes:D2}:{ts.Seconds:D2}";
        }
        return $"{ts.Minutes}:{ts.Seconds:D2}";
    }

    public object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture) => throw new NotSupportedException();
}

#endregion

#region 2. Math & Arithmetic Converters

/// <summary>
/// Adds a parameter numeric offset to the value: value + parameter.
/// </summary>
public class MathAddConverter : IValueConverter
{
    public static readonly MathAddConverter Instance = new();

    public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        if (value == null) return 0.0;
        try
        {
            double v = System.Convert.ToDouble(value, CultureInfo.InvariantCulture);
            double p = parameter != null ? System.Convert.ToDouble(parameter, CultureInfo.InvariantCulture) : 0.0;
            double res = v + p;
            if (targetType == typeof(int)) return (int)Math.Round(res);
            return res;
        }
        catch
        {
            return value;
        }
    }

    public object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        if (value == null) return 0.0;
        try
        {
            double v = System.Convert.ToDouble(value, CultureInfo.InvariantCulture);
            double p = parameter != null ? System.Convert.ToDouble(parameter, CultureInfo.InvariantCulture) : 0.0;
            double res = v - p;
            if (targetType == typeof(int)) return (int)Math.Round(res);
            return res;
        }
        catch
        {
            return value;
        }
    }
}

/// <summary>
/// Subtracts a parameter numeric offset from the value: value - parameter.
/// </summary>
public class MathSubtractConverter : IValueConverter
{
    public static readonly MathSubtractConverter Instance = new();

    public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        if (value == null) return 0.0;
        try
        {
            double v = System.Convert.ToDouble(value, CultureInfo.InvariantCulture);
            double p = parameter != null ? System.Convert.ToDouble(parameter, CultureInfo.InvariantCulture) : 0.0;
            double res = v - p;
            if (targetType == typeof(int)) return (int)Math.Round(res);
            return res;
        }
        catch
        {
            return value;
        }
    }

    public object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        if (value == null) return 0.0;
        try
        {
            double v = System.Convert.ToDouble(value, CultureInfo.InvariantCulture);
            double p = parameter != null ? System.Convert.ToDouble(parameter, CultureInfo.InvariantCulture) : 0.0;
            double res = v + p;
            if (targetType == typeof(int)) return (int)Math.Round(res);
            return res;
        }
        catch
        {
            return value;
        }
    }
}

/// <summary>
/// Multiplies value by a parameter numeric factor: value * parameter.
/// </summary>
public class MathMultiplyConverter : IValueConverter
{
    public static readonly MathMultiplyConverter Instance = new();

    public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        if (value == null) return 0.0;
        try
        {
            double v = System.Convert.ToDouble(value, CultureInfo.InvariantCulture);
            double p = parameter != null ? System.Convert.ToDouble(parameter, CultureInfo.InvariantCulture) : 1.0;
            double res = v * p;
            if (targetType == typeof(int)) return (int)Math.Round(res);
            return res;
        }
        catch
        {
            return value;
        }
    }

    public object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        if (value == null) return 0.0;
        try
        {
            double v = System.Convert.ToDouble(value, CultureInfo.InvariantCulture);
            double p = parameter != null ? System.Convert.ToDouble(parameter, CultureInfo.InvariantCulture) : 1.0;
            if (Math.Abs(p) < 0.000001) return 0.0;
            double res = v / p;
            if (targetType == typeof(int)) return (int)Math.Round(res);
            return res;
        }
        catch
        {
            return value;
        }
    }
}

/// <summary>
/// Divides value by a parameter numeric divisor: value / parameter.
/// </summary>
public class MathDivideConverter : IValueConverter
{
    public static readonly MathDivideConverter Instance = new();

    public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        if (value == null) return 0.0;
        try
        {
            double v = System.Convert.ToDouble(value, CultureInfo.InvariantCulture);
            double p = parameter != null ? System.Convert.ToDouble(parameter, CultureInfo.InvariantCulture) : 1.0;
            if (Math.Abs(p) < 0.000001) return 0.0;
            double res = v / p;
            if (targetType == typeof(int)) return (int)Math.Round(res);
            return res;
        }
        catch
        {
            return value;
        }
    }

    public object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        if (value == null) return 0.0;
        try
        {
            double v = System.Convert.ToDouble(value, CultureInfo.InvariantCulture);
            double p = parameter != null ? System.Convert.ToDouble(parameter, CultureInfo.InvariantCulture) : 1.0;
            double res = v * p;
            if (targetType == typeof(int)) return (int)Math.Round(res);
            return res;
        }
        catch
        {
            return value;
        }
    }
}

/// <summary>
/// Converts double (0..1 or 0..100) to formatted percentage string ("75%") and roundtrips.
/// Parameter "0-1" interprets input as 0..1 scale (default). Parameter "0-100" interprets as 0..100 scale.
/// </summary>
public class PercentageConverter : IValueConverter
{
    public static readonly PercentageConverter Instance = new();

    public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        if (value == null) return "0%";
        try
        {
            double d = System.Convert.ToDouble(value, CultureInfo.InvariantCulture);
            bool isHundredScale = parameter is string p && p.Contains("100");
            double pct = isHundredScale ? d : (d * 100.0);
            return $"{(int)Math.Round(pct)}%";
        }
        catch
        {
            return "0%";
        }
    }

    public object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        if (value is string str)
        {
            string clean = str.Replace("%", "").Trim();
            if (double.TryParse(clean, NumberStyles.Float, culture ?? CultureInfo.InvariantCulture, out double parsed))
            {
                bool isHundredScale = parameter is string p && p.Contains("100");
                double res = isHundredScale ? parsed : (parsed / 100.0);
                if (targetType == typeof(int)) return (int)Math.Round(res);
                return res;
            }
        }
        return 0.0;
    }
}

#endregion

#region 3. Units & Dimension Converters (PDF Studio)

/// <summary>
/// Converts PDF points (72 pt/inch) to millimeters (25.4 mm/inch): 1 pt = 0.352778 mm.
/// Two-way binding supported: mm converted back to points accurately.
/// </summary>
public class PointsToMillimetersConverter : IValueConverter
{
    public static readonly PointsToMillimetersConverter Instance = new();
    private const double PtToMmFactor = 25.4 / 72.0; // ~0.3527777777777778

    public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        if (value == null) return 0.0;
        try
        {
            double pt = System.Convert.ToDouble(value, CultureInfo.InvariantCulture);
            double mm = pt * PtToMmFactor;

            if (parameter is string paramStr && paramStr.Equals("format", StringComparison.OrdinalIgnoreCase))
            {
                return $"{mm:F1} mm";
            }
            return Math.Round(mm, 2);
        }
        catch
        {
            return 0.0;
        }
    }

    public object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        if (value == null) return 0.0;
        try
        {
            string str = value.ToString()?.Replace("mm", "", StringComparison.OrdinalIgnoreCase).Trim() ?? "0";
            double mm = double.Parse(str, CultureInfo.InvariantCulture);
            double pt = mm / PtToMmFactor;
            return Math.Round(pt, 2);
        }
        catch
        {
            return 0.0;
        }
    }
}

/// <summary>
/// Converts PDF points (72 pt/inch) to inches: 1 pt = 1/72 inch.
/// Two-way binding supported.
/// </summary>
public class PointsToInchesConverter : IValueConverter
{
    public static readonly PointsToInchesConverter Instance = new();
    private const double PtToInchFactor = 1.0 / 72.0;

    public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        if (value == null) return 0.0;
        try
        {
            double pt = System.Convert.ToDouble(value, CultureInfo.InvariantCulture);
            double inches = pt * PtToInchFactor;

            if (parameter is string paramStr && paramStr.Equals("format", StringComparison.OrdinalIgnoreCase))
            {
                return $"{inches:F2} in";
            }
            return Math.Round(inches, 3);
        }
        catch
        {
            return 0.0;
        }
    }

    public object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        if (value == null) return 0.0;
        try
        {
            string str = value.ToString()?.Replace("in", "", StringComparison.OrdinalIgnoreCase).Replace("\"", "").Trim() ?? "0";
            double inches = double.Parse(str, CultureInfo.InvariantCulture);
            double pt = inches * 72.0;
            return Math.Round(pt, 2);
        }
        catch
        {
            return 0.0;
        }
    }
}

/// <summary>
/// Converts PDF points (72 pt/inch) to screen/display pixels at a specified DPI (default 96 DPI).
/// </summary>
public class PointsToPixelsConverter : IValueConverter
{
    public static readonly PointsToPixelsConverter Instance = new();

    public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        if (value == null) return 0.0;
        try
        {
            double pt = System.Convert.ToDouble(value, CultureInfo.InvariantCulture);
            double dpi = parameter != null ? System.Convert.ToDouble(parameter, CultureInfo.InvariantCulture) : 96.0;
            return pt * (dpi / 72.0);
        }
        catch
        {
            return 0.0;
        }
    }

    public object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        if (value == null) return 0.0;
        try
        {
            double px = System.Convert.ToDouble(value, CultureInfo.InvariantCulture);
            double dpi = parameter != null ? System.Convert.ToDouble(parameter, CultureInfo.InvariantCulture) : 96.0;
            return px * (72.0 / dpi);
        }
        catch
        {
            return 0.0;
        }
    }
}

#endregion

#region 4. Logic, Boolean & Collection Converters

/// <summary>
/// Inverts boolean values: true -> false, false -> true. Two-way supported.
/// </summary>
public class InverseBooleanConverter : IValueConverter
{
    public static readonly InverseBooleanConverter Instance = new();

    public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        return value is not true;
    }

    public object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        return value is not true;
    }
}

/// <summary>
/// Returns true if a count or numeric value is greater than 0 (or greater than a parameter threshold).
/// Parameter "invert" or "-1" inverts the condition.
/// </summary>
public class CountToBooleanConverter : IValueConverter
{
    public static readonly CountToBooleanConverter Instance = new();

    public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        if (value == null) return false;

        double count = 0;
        if (value is ICollection coll)
        {
            count = coll.Count;
        }
        else if (value is IEnumerable enumr && value is not string)
        {
            count = enumr.Cast<object>().Count();
        }
        else
        {
            try
            {
                count = System.Convert.ToDouble(value, CultureInfo.InvariantCulture);
            }
            catch
            {
                count = 0;
            }
        }

        double threshold = 0.0;
        bool invert = false;

        if (parameter is string paramStr)
        {
            if (paramStr.Equals("invert", StringComparison.OrdinalIgnoreCase))
            {
                invert = true;
            }
            else if (double.TryParse(paramStr, NumberStyles.Float, CultureInfo.InvariantCulture, out double t))
            {
                threshold = t;
            }
        }

        bool result = count > threshold;
        return invert ? !result : result;
    }

    public object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture) => throw new NotSupportedException();
}

/// <summary>
/// Checks if a string or collection is null or empty. Returns true if empty, or false if populated.
/// Parameter "invert" reverses logic (true when has content).
/// </summary>
public class NullOrEmptyToBooleanConverter : IValueConverter
{
    public static readonly NullOrEmptyToBooleanConverter Instance = new();

    public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        bool isEmpty = false;

        if (value == null)
        {
            isEmpty = true;
        }
        else if (value is string s)
        {
            isEmpty = string.IsNullOrWhiteSpace(s);
        }
        else if (value is ICollection c)
        {
            isEmpty = c.Count == 0;
        }
        else if (value is IEnumerable e)
        {
            isEmpty = !e.Cast<object>().Any();
        }

        bool invert = parameter is string p && p.Equals("invert", StringComparison.OrdinalIgnoreCase);
        return invert ? !isEmpty : isEmpty;
    }

    public object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture) => throw new NotSupportedException();
}

/// <summary>
/// Evaluates comparison expressions against a value: ">0", ">=1", "<10", "<=5", "==10", "!=0".
/// </summary>
public class ComparisonToBooleanConverter : IValueConverter
{
    public static readonly ComparisonToBooleanConverter Instance = new();

    public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        if (value == null || parameter == null) return false;

        try
        {
            double v = System.Convert.ToDouble(value, CultureInfo.InvariantCulture);
            string expr = parameter.ToString()?.Trim() ?? string.Empty;

            if (expr.StartsWith(">="))
            {
                double target = double.Parse(expr[2..].Trim(), CultureInfo.InvariantCulture);
                return v >= target;
            }
            if (expr.StartsWith("<="))
            {
                double target = double.Parse(expr[2..].Trim(), CultureInfo.InvariantCulture);
                return v <= target;
            }
            if (expr.StartsWith(">"))
            {
                double target = double.Parse(expr[1..].Trim(), CultureInfo.InvariantCulture);
                return v > target;
            }
            if (expr.StartsWith("<"))
            {
                double target = double.Parse(expr[1..].Trim(), CultureInfo.InvariantCulture);
                return v < target;
            }
            if (expr.StartsWith("=="))
            {
                double target = double.Parse(expr[2..].Trim(), CultureInfo.InvariantCulture);
                return Math.Abs(v - target) < 0.0001;
            }
            if (expr.StartsWith("!="))
            {
                double target = double.Parse(expr[2..].Trim(), CultureInfo.InvariantCulture);
                return Math.Abs(v - target) >= 0.0001;
            }

            if (double.TryParse(expr, NumberStyles.Float, CultureInfo.InvariantCulture, out double eqTarget))
            {
                return Math.Abs(v - eqTarget) < 0.0001;
            }
        }
        catch
        {
            // fallback string equality
            return string.Equals(value?.ToString(), parameter?.ToString(), StringComparison.OrdinalIgnoreCase);
        }

        return false;
    }

    public object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture) => throw new NotSupportedException();
}

/// <summary>
/// Formats an integer count with pluralization: e.g. Parameter="page|pages" -> "0 pages", "1 page", "5 pages".
/// </summary>
public class CountToStringConverter : IValueConverter
{
    public static readonly CountToStringConverter Instance = new();

    public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        int count = 0;
        if (value is int i) count = i;
        else if (value is long l) count = (int)l;
        else if (value is ICollection coll) count = coll.Count;
        else if (value != null && int.TryParse(value.ToString(), out int parsed)) count = parsed;

        if (parameter is string paramStr)
        {
            var parts = paramStr.Split('|');
            string singular = parts.Length > 0 ? parts[0] : "item";
            string plural = parts.Length > 1 ? parts[1] : (singular + "s");

            return count == 1 ? $"{count} {singular}" : $"{count} {plural}";
        }

        return count.ToString();
    }

    public object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture) => throw new NotSupportedException();
}

#endregion

#region 5. Graphics, Color & Transform Converters

/// <summary>
/// Converts Avalonia Color to SolidColorBrush and back.
/// </summary>
public class ColorToBrushConverter : IValueConverter
{
    public static readonly ColorToBrushConverter Instance = new();

    public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        if (value is Color color)
        {
            return new SolidColorBrush(color);
        }
        return Brushes.Transparent;
    }

    public object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        if (value is ISolidColorBrush brush)
        {
            return brush.Color;
        }
        return Colors.Transparent;
    }
}

/// <summary>
/// Dynamically scales an element via ScaleTransform(scaleX, scaleY).
/// Parameter can specify uniform scale or "scaleX,scaleY".
/// </summary>
public class ScaleToTransformConverter : IValueConverter
{
    public static readonly ScaleToTransformConverter Instance = new();

    public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        if (value == null) return null;

        try
        {
            double scaleX = 1.0;
            double scaleY = 1.0;

            if (value is double d)
            {
                scaleX = scaleY = d;
            }
            else if (value is string s)
            {
                var parts = s.Split(',');
                if (parts.Length == 1 && double.TryParse(parts[0], NumberStyles.Float, CultureInfo.InvariantCulture, out double uniform))
                {
                    scaleX = scaleY = uniform;
                }
                else if (parts.Length >= 2 &&
                         double.TryParse(parts[0], NumberStyles.Float, CultureInfo.InvariantCulture, out double sx) &&
                         double.TryParse(parts[1], NumberStyles.Float, CultureInfo.InvariantCulture, out double sy))
                {
                    scaleX = sx;
                    scaleY = sy;
                }
            }

            if (Math.Abs(scaleX - 1.0) < 0.001 && Math.Abs(scaleY - 1.0) < 0.001)
            {
                return null; // Identity -> avoid transform overhead
            }

            return new ScaleTransform(scaleX, scaleY);
        }
        catch
        {
            return null;
        }
    }

    public object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        if (value is ScaleTransform st)
        {
            return st.ScaleX;
        }
        return 1.0;
    }
}

/// <summary>
/// Converts numeric X,Y offset values or comma-separated "X,Y" string to TranslateTransform.
/// </summary>
public class TranslateToTransformConverter : IValueConverter
{
    public static readonly TranslateToTransformConverter Instance = new();

    public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        if (value == null) return null;

        try
        {
            double x = 0.0;
            double y = 0.0;

            if (value is string s)
            {
                var parts = s.Split(',');
                if (parts.Length >= 2 &&
                    double.TryParse(parts[0], NumberStyles.Float, CultureInfo.InvariantCulture, out double px) &&
                    double.TryParse(parts[1], NumberStyles.Float, CultureInfo.InvariantCulture, out double py))
                {
                    x = px;
                    y = py;
                }
            }
            else if (value is double d)
            {
                x = d;
            }

            if (Math.Abs(x) < 0.001 && Math.Abs(y) < 0.001) return null;
            return new TranslateTransform(x, y);
        }
        catch
        {
            return null;
        }
    }

    public object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        if (value is TranslateTransform tt) return tt.X;
        return 0.0;
    }
}

/// <summary>
/// Parses SVG path data string (e.g. "M 0,0 L 100,100 Z") into an Avalonia Geometry.
/// </summary>
public class StringToGeometryConverter : IValueConverter
{
    public static readonly StringToGeometryConverter Instance = new();

    public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        if (value is string pathData && !string.IsNullOrWhiteSpace(pathData))
        {
            try
            {
                return Geometry.Parse(pathData);
            }
            catch
            {
                try
                {
                    return StreamGeometry.Parse(pathData);
                }
                catch
                {
                    return null;
                }
            }
        }
        if (value is Geometry g) return g;
        return null;
    }

    public object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        if (value is Geometry g) return g.ToString();
        return null;
    }
}

#endregion

#region 6. String & Text Converters

/// <summary>
/// Modifies string casing: "upper", "lower", "title", "pascal", "camel".
/// </summary>
public class StringCaseConverter : IValueConverter
{
    public static readonly StringCaseConverter Instance = new();

    public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        if (value == null) return string.Empty;
        string str = value.ToString() ?? string.Empty;
        string mode = parameter as string ?? "upper";

        return mode.ToLowerInvariant() switch
        {
            "upper" => str.ToUpper(culture ?? CultureInfo.CurrentCulture),
            "lower" => str.ToLower(culture ?? CultureInfo.CurrentCulture),
            "title" => (culture ?? CultureInfo.CurrentCulture).TextInfo.ToTitleCase(str.ToLower(culture ?? CultureInfo.CurrentCulture)),
            "capitalize" => str.Length > 0 ? char.ToUpper(str[0], culture ?? CultureInfo.CurrentCulture) + str[1..] : str,
            _ => str
        };
    }

    public object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture) => throw new NotSupportedException();
}

/// <summary>
/// Truncates long strings to a specified maximum length and appends an ellipsis.
/// Parameter specifies max characters (default 30).
/// </summary>
public class StringTruncateConverter : IValueConverter
{
    public static readonly StringTruncateConverter Instance = new();

    public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        if (value == null) return string.Empty;
        string str = value.ToString() ?? string.Empty;

        int maxLength = 30;
        if (parameter is string paramStr && int.TryParse(paramStr, out int max))
        {
            maxLength = Math.Max(3, max);
        }

        if (str.Length <= maxLength) return str;
        return str[..(maxLength - 3)] + "...";
    }

    public object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture) => throw new NotSupportedException();
}

/// <summary>
/// Formats a value with prefix and/or suffix: Parameter="{0} pt" or "Page {0}".
/// </summary>
public class StringPrefixSuffixConverter : IValueConverter
{
    public static readonly StringPrefixSuffixConverter Instance = new();

    public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        if (value == null) return string.Empty;
        string format = parameter as string ?? "{0}";
        if (!format.Contains("{0}")) format = $"{format}{{0}}";
        return string.Format(culture ?? CultureInfo.InvariantCulture, format, value);
    }

    public object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture) => throw new NotSupportedException();
}

/// <summary>
/// Converts Enum values to human-friendly description strings with space separated words.
/// e.g. TextAlignmentMode.Center -> "Center", PdfPageSize.A4Portrait -> "A4 Portrait".
/// </summary>
public class EnumToDescriptionConverter : IValueConverter
{
    public static readonly EnumToDescriptionConverter Instance = new();

    public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        if (value == null) return string.Empty;

        Type type = value.GetType();
        if (!type.IsEnum) return value.ToString();

        string name = value.ToString()!;
        var field = type.GetField(name);
        if (field != null)
        {
            var attr = field.GetCustomAttribute<DescriptionAttribute>();
            if (attr != null) return attr.Description;
        }

        // Split CamelCase words into spaced words: "A4Portrait" -> "A4 Portrait"
        return Regex.Replace(name, "(\\B[A-Z])", " $1");
    }

    public object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        if (value is string str && targetType.IsEnum)
        {
            string clean = str.Replace(" ", "");
            if (Enum.TryParse(targetType, clean, true, out var result))
            {
                return result;
            }
        }
        return null;
    }
}

#endregion

#region 7. Multi-Value Converters

/// <summary>
/// Multi-value converter that returns true only if ALL bound values are true.
/// </summary>
public class AllTrueMultiConverter : IMultiValueConverter
{
    public static readonly AllTrueMultiConverter Instance = new();

    public object? Convert(IList<object?> values, Type targetType, object? parameter, CultureInfo culture)
    {
        if (values == null || values.Count == 0) return false;
        return values.All(v => v is true);
    }
}

/// <summary>
/// Multi-value converter that returns true if ANY bound value is true.
/// </summary>
public class AnyTrueMultiConverter : IMultiValueConverter
{
    public static readonly AnyTrueMultiConverter Instance = new();

    public object? Convert(IList<object?> values, Type targetType, object? parameter, CultureInfo culture)
    {
        if (values == null || values.Count == 0) return false;
        return values.Any(v => v is true);
    }
}

/// <summary>
/// Multi-value converter that returns true if the first two bound values are equal (case-insensitive string comparison).
/// </summary>
public class ValuesEqualMultiConverter : IMultiValueConverter
{
    public static readonly ValuesEqualMultiConverter Instance = new();

    public object? Convert(IList<object?> values, Type targetType, object? parameter, CultureInfo culture)
    {
        if (values == null || values.Count < 2) return false;
        if (values[0] == null && values[1] == null) return true;
        if (values[0] == null || values[1] == null) return false;
        return string.Equals(values[0]?.ToString(), values[1]?.ToString(), StringComparison.OrdinalIgnoreCase);
    }
}

/// <summary>
/// Concatenates multiple string values together with an optional delimiter parameter.
/// </summary>
public class StringConcatMultiConverter : IMultiValueConverter
{
    public static readonly StringConcatMultiConverter Instance = new();

    public object? Convert(IList<object?> values, Type targetType, object? parameter, CultureInfo culture)
    {
        if (values == null || values.Count == 0) return string.Empty;
        string delimiter = parameter as string ?? string.Empty;
        return string.Join(delimiter, values.Where(v => v != null).Select(v => v!.ToString()));
    }
}

/// <summary>
/// Returns the maximum value among numeric multi-bindings.
/// </summary>
public class MathMaxMultiConverter : IMultiValueConverter
{
    public static readonly MathMaxMultiConverter Instance = new();

    public object? Convert(IList<object?> values, Type targetType, object? parameter, CultureInfo culture)
    {
        if (values == null || values.Count == 0) return 0.0;
        double max = double.MinValue;
        bool found = false;

        foreach (var v in values)
        {
            if (v != null && double.TryParse(v.ToString(), NumberStyles.Float, CultureInfo.InvariantCulture, out double d))
            {
                if (d > max) max = d;
                found = true;
            }
        }

        return found ? max : 0.0;
    }
}

/// <summary>
/// Returns the minimum value among numeric multi-bindings.
/// </summary>
public class MathMinMultiConverter : IMultiValueConverter
{
    public static readonly MathMinMultiConverter Instance = new();

    public object? Convert(IList<object?> values, Type targetType, object? parameter, CultureInfo culture)
    {
        if (values == null || values.Count == 0) return 0.0;
        double min = double.MaxValue;
        bool found = false;

        foreach (var v in values)
        {
            if (v != null && double.TryParse(v.ToString(), NumberStyles.Float, CultureInfo.InvariantCulture, out double d))
            {
                if (d < min) min = d;
                found = true;
            }
        }

        return found ? min : 0.0;
    }
}

#endregion
