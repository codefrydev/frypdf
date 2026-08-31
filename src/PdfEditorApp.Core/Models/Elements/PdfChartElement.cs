using System.Collections.Generic;

namespace PdfEditorApp.Models.Elements;

public class ChartSeriesItem
{
    public string Name { get; set; } = "Series 1";
    public List<double> Values { get; set; } = new();
    public string? ColorHex { get; set; }

    public ChartSeriesItem Clone() => new()
    {
        Name = Name,
        Values = new List<double>(Values),
        ColorHex = ColorHex
    };
}

public class PdfChartElement : PdfElementBase
{
    public override ElementKind Kind => ElementKind.Chart;

    public string Title { get; set; } = "Revenue Growth (Q1-Q4)";
    public ChartType ChartType { get; set; } = ChartType.BarColumn;
    public ChartPalette Palette { get; set; } = ChartPalette.CorporateBlue;
    public ChartLegendPosition LegendPosition { get; set; } = ChartLegendPosition.Top;

    public List<string> Categories { get; set; } = new() { "Q1", "Q2", "Q3", "Q4" };
    public List<double> Values { get; set; } = new() { 1.2, 1.8, 2.5, 3.1 };
    public List<string> ValueLabels { get; set; } = new() { "$1.2B", "$1.8B", "$2.5B", "$3.1B" };
    public List<string> BarColorsHex { get; set; } = new() { "#C7E0F4", "#82BDF0", "#3D95E6", "#0F6CBD" };
    public List<ChartSeriesItem> MultiSeries { get; set; } = new();

    public bool ShowDataLabels { get; set; } = true;
    public bool ShowGridlines { get; set; } = true;
    public double DonutHoleRatio { get; set; } = 0.6;
    public double CurveSmoothness { get; set; } = 0.65;
    public double StrokeThickness { get; set; } = 3.0;
    public string? XAxisTitle { get; set; }
    public string? YAxisTitle { get; set; }

    public string BackgroundColorHex { get; set; } = "#F8F9FA";
    public string BorderColorHex { get; set; } = "#E1DFDD";

    public override PdfElementBase Clone()
    {
        var clone = (PdfChartElement)base.Clone();
        clone.Categories = new List<string>(Categories);
        clone.Values = new List<double>(Values);
        clone.ValueLabels = new List<string>(ValueLabels);
        clone.BarColorsHex = new List<string>(BarColorsHex);
        clone.MultiSeries = new List<ChartSeriesItem>();
        foreach (var s in MultiSeries)
        {
            clone.MultiSeries.Add(new ChartSeriesItem
            {
                Name = s.Name,
                Values = new List<double>(s.Values),
                ColorHex = s.ColorHex
            });
        }
        return clone;
    }
}

