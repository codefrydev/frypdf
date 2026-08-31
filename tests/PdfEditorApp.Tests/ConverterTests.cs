using System;
using System.Collections.Generic;
using System.Globalization;
using Avalonia.Media;
using PdfEditorApp.Converters;
using PdfEditorApp.Models;
using Xunit;

namespace PdfEditorApp.Tests;

public class ConverterTests
{
    [Fact]
    public void ByteSizeToStringConverter_FormatsAndParsesByteSizesAccurately()
    {
        var conv = ByteSizeToStringConverter.Instance;
        var culture = CultureInfo.InvariantCulture;

        // Conversion tests
        Assert.Equal("0 B", conv.Convert(0L, typeof(string), null, culture));
        Assert.Equal("0 B", conv.Convert(null, typeof(string), null, culture));
        Assert.Equal("512 B", conv.Convert(512L, typeof(string), null, culture));
        Assert.Equal("1.0 KB", conv.Convert(1024L, typeof(string), null, culture));
        Assert.Equal("1.5 KB", conv.Convert(1536L, typeof(string), null, culture));
        Assert.Equal("2.00 MB", conv.Convert(2 * 1024 * 1024L, typeof(string), "2", culture));
        Assert.Equal("1.5 GB", conv.Convert((long)(1.5 * 1024 * 1024 * 1024), typeof(string), null, culture));

        // ConvertBack tests
        Assert.Equal(1024L, conv.ConvertBack("1 KB", typeof(long), null, culture));
        Assert.Equal(2097152L, conv.ConvertBack("2 MB", typeof(long), null, culture));
        Assert.Equal(0L, conv.ConvertBack("invalid", typeof(long), null, culture));
    }

    [Fact]
    public void DateTimeFormatConverter_FormatsCustomAndRelativeDates()
    {
        var conv = DateTimeFormatConverter.Instance;
        var culture = CultureInfo.InvariantCulture;

        var dt = new DateTime(2026, 8, 15, 14, 30, 0);
        Assert.Equal("2026-08-15", conv.Convert(dt, typeof(string), "yyyy-MM-dd", culture));
        Assert.Equal("Aug 15, 2026", conv.Convert(dt, typeof(string), "MMM dd, yyyy", culture));

        // Relative formatting for recent time
        var recent = DateTime.Now.AddSeconds(-10);
        Assert.Equal("Just now", conv.Convert(recent, typeof(string), "Relative", culture));

        // ConvertBack
        var parsed = conv.ConvertBack("2026-08-15", typeof(DateTime), null, culture);
        Assert.NotNull(parsed);
        Assert.Equal(new DateTime(2026, 8, 15), (DateTime)parsed!);
    }

    [Fact]
    public void TimeSpanFormatConverter_FormatsDurations()
    {
        var conv = TimeSpanFormatConverter.Instance;
        var culture = CultureInfo.InvariantCulture;

        Assert.Equal("1:30", conv.Convert(TimeSpan.FromSeconds(90), typeof(string), null, culture));
        Assert.Equal("1:30", conv.Convert(90.0, typeof(string), null, culture));
        Assert.Equal("1:15:30", conv.Convert(TimeSpan.FromSeconds(4530), typeof(string), null, culture));
        Assert.Equal("1h 15m", conv.Convert(TimeSpan.FromSeconds(4530), typeof(string), "words", culture));
        Assert.Equal("45s", conv.Convert(TimeSpan.FromSeconds(45), typeof(string), "words", culture));
    }

    [Fact]
    public void MathConverters_PerformArithmeticAndRoundtrips()
    {
        var culture = CultureInfo.InvariantCulture;

        // MathAddConverter
        Assert.Equal(15.0, MathAddConverter.Instance.Convert(10.0, typeof(double), "5", culture));
        Assert.Equal(10.0, MathAddConverter.Instance.ConvertBack(15.0, typeof(double), "5", culture));

        // MathSubtractConverter
        Assert.Equal(5.0, MathSubtractConverter.Instance.Convert(10.0, typeof(double), "5", culture));
        Assert.Equal(10.0, MathSubtractConverter.Instance.ConvertBack(5.0, typeof(double), "5", culture));

        // MathMultiplyConverter
        Assert.Equal(50.0, MathMultiplyConverter.Instance.Convert(10.0, typeof(double), "5", culture));
        Assert.Equal(10.0, MathMultiplyConverter.Instance.ConvertBack(50.0, typeof(double), "5", culture));

        // MathDivideConverter
        Assert.Equal(2.0, MathDivideConverter.Instance.Convert(10.0, typeof(double), "5", culture));
        Assert.Equal(10.0, MathDivideConverter.Instance.ConvertBack(2.0, typeof(double), "5", culture));
    }

    [Fact]
    public void PercentageConverter_FormatsAndParsesPercentages()
    {
        var conv = PercentageConverter.Instance;
        var culture = CultureInfo.InvariantCulture;

        // 0..1 scale
        Assert.Equal("75%", conv.Convert(0.75, typeof(string), null, culture));
        Assert.Equal(0.75, (double)(conv.ConvertBack("75%", typeof(double), null, culture) ?? 0.0), 2);

        // 0..100 scale
        Assert.Equal("85%", conv.Convert(85.0, typeof(string), "0-100", culture));
        Assert.Equal(85.0, (double)(conv.ConvertBack("85%", typeof(double), "0-100", culture) ?? 0.0), 2);
    }

    [Fact]
    public void UnitConverters_PointsToMmInchesAndPixels()
    {
        var culture = CultureInfo.InvariantCulture;

        // 72 points = 1.0 inch = 25.4 mm
        double pt = 72.0;

        // Points to MM
        var mm = PointsToMillimetersConverter.Instance.Convert(pt, typeof(double), null, culture);
        Assert.Equal(25.4, (double)(mm ?? 0.0), 1);
        var ptFromMm = PointsToMillimetersConverter.Instance.ConvertBack(25.4, typeof(double), null, culture);
        Assert.Equal(72.0, (double)(ptFromMm ?? 0.0), 1);

        // Formatted MM
        Assert.Equal("25.4 mm", PointsToMillimetersConverter.Instance.Convert(pt, typeof(string), "format", culture));

        // Points to Inches
        var inches = PointsToInchesConverter.Instance.Convert(pt, typeof(double), null, culture);
        Assert.Equal(1.0, (double)(inches ?? 0.0), 2);
        var ptFromIn = PointsToInchesConverter.Instance.ConvertBack(1.0, typeof(double), null, culture);
        Assert.Equal(72.0, (double)(ptFromIn ?? 0.0), 1);

        // Points to Pixels (96 DPI default -> 72pt * 96/72 = 96px)
        var px = PointsToPixelsConverter.Instance.Convert(pt, typeof(double), null, culture);
        Assert.Equal(96.0, (double)(px ?? 0.0), 1);
    }

    [Fact]
    public void BooleanAndLogicConverters_EvaluateConditionsAccurately()
    {
        var culture = CultureInfo.InvariantCulture;

        // InverseBooleanConverter
        Assert.False((bool)(InverseBooleanConverter.Instance.Convert(true, typeof(bool), null, culture) ?? true));
        Assert.True((bool)(InverseBooleanConverter.Instance.Convert(false, typeof(bool), null, culture) ?? false));
        Assert.False((bool)(InverseBooleanConverter.Instance.ConvertBack(true, typeof(bool), null, culture) ?? true));

        // CountToBooleanConverter
        var list = new List<int> { 1, 2, 3 };
        Assert.True((bool)(CountToBooleanConverter.Instance.Convert(list, typeof(bool), null, culture) ?? false));
        Assert.False((bool)(CountToBooleanConverter.Instance.Convert(new List<int>(), typeof(bool), null, culture) ?? true));
        Assert.False((bool)(CountToBooleanConverter.Instance.Convert(list, typeof(bool), "invert", culture) ?? true));
        Assert.True((bool)(CountToBooleanConverter.Instance.Convert(5, typeof(bool), "2", culture) ?? false)); // 5 > 2

        // NullOrEmptyToBooleanConverter
        Assert.True((bool)(NullOrEmptyToBooleanConverter.Instance.Convert("", typeof(bool), null, culture) ?? false));
        Assert.True((bool)(NullOrEmptyToBooleanConverter.Instance.Convert(null, typeof(bool), null, culture) ?? false));
        Assert.False((bool)(NullOrEmptyToBooleanConverter.Instance.Convert("hello", typeof(bool), null, culture) ?? true));
        Assert.True((bool)(NullOrEmptyToBooleanConverter.Instance.Convert("hello", typeof(bool), "invert", culture) ?? false));

        // ComparisonToBooleanConverter
        Assert.True((bool)(ComparisonToBooleanConverter.Instance.Convert(10, typeof(bool), ">5", culture) ?? false));
        Assert.False((bool)(ComparisonToBooleanConverter.Instance.Convert(3, typeof(bool), ">=5", culture) ?? true));
        Assert.True((bool)(ComparisonToBooleanConverter.Instance.Convert(5, typeof(bool), "<=5", culture) ?? false));
        Assert.True((bool)(ComparisonToBooleanConverter.Instance.Convert(42, typeof(bool), "==42", culture) ?? false));
        Assert.True((bool)(ComparisonToBooleanConverter.Instance.Convert(42, typeof(bool), "!=0", culture) ?? false));

        // CountToStringConverter
        Assert.Equal("1 page", CountToStringConverter.Instance.Convert(1, typeof(string), "page|pages", culture));
        Assert.Equal("5 pages", CountToStringConverter.Instance.Convert(5, typeof(string), "page|pages", culture));
        Assert.Equal("0 pages", CountToStringConverter.Instance.Convert(0, typeof(string), "page|pages", culture));
    }

    [Fact]
    public void GraphicsAndTransformConverters_CreateVisualElements()
    {
        var culture = CultureInfo.InvariantCulture;

        // ColorToBrushConverter
        var brush = ColorToBrushConverter.Instance.Convert(Colors.Red, typeof(IBrush), null, culture);
        Assert.NotNull(brush);
        var scb = Assert.IsType<SolidColorBrush>(brush);
        Assert.Equal(Colors.Red, scb.Color);

        // ScaleToTransformConverter
        var st = ScaleToTransformConverter.Instance.Convert(1.5, typeof(ITransform), null, culture);
        Assert.NotNull(st);
        var scale = Assert.IsType<ScaleTransform>(st);
        Assert.Equal(1.5, scale.ScaleX);
        Assert.Equal(1.5, scale.ScaleY);
        Assert.Null(ScaleToTransformConverter.Instance.Convert(1.0, typeof(ITransform), null, culture)); // Identity returns null

        // TranslateToTransformConverter
        var tt = TranslateToTransformConverter.Instance.Convert("10,20", typeof(ITransform), null, culture);
        Assert.NotNull(tt);
        var trans = Assert.IsType<TranslateTransform>(tt);
        Assert.Equal(10.0, trans.X);
        Assert.Equal(20.0, trans.Y);

        // StringToGeometryConverter
        Assert.Null(StringToGeometryConverter.Instance.Convert(null, typeof(Geometry), null, culture));
        Assert.Null(StringToGeometryConverter.Instance.Convert("", typeof(Geometry), null, culture));
        // If an already parsed Geometry is passed, it returns it directly
        // Test ConvertBack
        Assert.Null(StringToGeometryConverter.Instance.ConvertBack(null, typeof(string), null, culture));
    }

    [Fact]
    public void StringAndTextConverters_FormatStringsCorrectly()
    {
        var culture = CultureInfo.InvariantCulture;

        // StringCaseConverter
        Assert.Equal("HELLO WORLD", StringCaseConverter.Instance.Convert("hello world", typeof(string), "upper", culture));
        Assert.Equal("hello world", StringCaseConverter.Instance.Convert("HELLO WORLD", typeof(string), "lower", culture));
        Assert.Equal("Hello World", StringCaseConverter.Instance.Convert("hello world", typeof(string), "title", culture));
        Assert.Equal("Hello", StringCaseConverter.Instance.Convert("hello", typeof(string), "capitalize", culture));

        // StringTruncateConverter
        Assert.Equal("This is a...", StringTruncateConverter.Instance.Convert("This is a very long string that should be truncated", typeof(string), "12", culture));
        Assert.Equal("Short", StringTruncateConverter.Instance.Convert("Short", typeof(string), "20", culture));

        // StringPrefixSuffixConverter
        Assert.Equal("Page 5", StringPrefixSuffixConverter.Instance.Convert(5, typeof(string), "Page {0}", culture));
        Assert.Equal("72 pt", StringPrefixSuffixConverter.Instance.Convert(72, typeof(string), "{0} pt", culture));

        // EnumToDescriptionConverter
        Assert.Equal("Left", EnumToDescriptionConverter.Instance.Convert(TextAlignmentMode.Left, typeof(string), null, culture));
        Assert.Equal("Top Arc", EnumToDescriptionConverter.Instance.Convert(CircularTextPlacement.TopArc, typeof(string), null, culture));
        Assert.Equal("A4", EnumToDescriptionConverter.Instance.Convert(PageFormat.A4, typeof(string), null, culture));
        Assert.Equal("Corporate Blue", EnumToDescriptionConverter.Instance.Convert(ChartPalette.CorporateBlue, typeof(string), null, culture));
    }

    [Fact]
    public void MultiValueConverters_EvaluateAggregationsCorrectly()
    {
        var culture = CultureInfo.InvariantCulture;

        // AllTrueMultiConverter
        Assert.True((bool)(AllTrueMultiConverter.Instance.Convert(new object?[] { true, true, true }, typeof(bool), null, culture) ?? false));
        Assert.False((bool)(AllTrueMultiConverter.Instance.Convert(new object?[] { true, false, true }, typeof(bool), null, culture) ?? true));

        // AnyTrueMultiConverter
        Assert.True((bool)(AnyTrueMultiConverter.Instance.Convert(new object?[] { false, true, false }, typeof(bool), null, culture) ?? false));
        Assert.False((bool)(AnyTrueMultiConverter.Instance.Convert(new object?[] { false, false, false }, typeof(bool), null, culture) ?? true));

        // StringConcatMultiConverter
        Assert.Equal("Page 1 of 10", StringConcatMultiConverter.Instance.Convert(new object?[] { "Page", " 1 ", "of", " 10" }, typeof(string), null, culture));
        Assert.Equal("A-B-C", StringConcatMultiConverter.Instance.Convert(new object?[] { "A", "B", "C" }, typeof(string), "-", culture));

        // MathMaxMultiConverter & MathMinMultiConverter
        Assert.Equal(42.0, (double)(MathMaxMultiConverter.Instance.Convert(new object?[] { 10.0, 42.0, 5.0 }, typeof(double), null, culture) ?? 0.0));
        Assert.Equal(5.0, (double)(MathMinMultiConverter.Instance.Convert(new object?[] { 10.0, 42.0, 5.0 }, typeof(double), null, culture) ?? 0.0));
    }
}
