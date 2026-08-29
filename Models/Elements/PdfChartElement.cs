using System.Collections.Generic;

namespace PdfEditorApp.Models.Elements;

public class PdfChartElement : PdfElementBase
{
    public override ElementKind Kind => ElementKind.Chart;

    public string Title { get; set; } = "Revenue Growth (Q1-Q4)";
    public List<string> Categories { get; set; } = new() { "Q1", "Q2", "Q3", "Q4" };
    public List<double> Values { get; set; } = new() { 1.2, 1.8, 2.5, 3.1 };
    public List<string> ValueLabels { get; set; } = new() { "$1.2B", "$1.8B", "$2.5B", "$3.1B" };
    public List<string> BarColorsHex { get; set; } = new() { "#C7E0F4", "#82BDF0", "#3D95E6", "#0F6CBD" };
    public string BackgroundColorHex { get; set; } = "#F8F9FA";
    public string BorderColorHex { get; set; } = "#E1DFDD";

    public override PdfElementBase Clone()
    {
        var clone = (PdfChartElement)base.Clone();
        clone.Categories = new List<string>(Categories);
        clone.Values = new List<double>(Values);
        clone.ValueLabels = new List<string>(ValueLabels);
        clone.BarColorsHex = new List<string>(BarColorsHex);
        return clone;
    }
}
