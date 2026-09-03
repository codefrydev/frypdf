using System;

namespace PdfEditorApp.Core.Models.Elements;

public class PdfMeasurementElement : PdfElementBase
{
    public override ElementKind Kind => ElementKind.Measurement;

    public double StartX { get; set; } = 0;
    public double StartY { get; set; } = 0;
    public double EndX { get; set; } = 200;
    public double EndY { get; set; } = 0;

    public RulerUnit Unit { get; set; } = RulerUnit.Points;
    public double ScaleFactor { get; set; } = 1.0; // 1 pt = ScaleFactor * Unit
    public string CustomLabel { get; set; } = "";
    public string StrokeColorHex { get; set; } = "#DC2626";
    public double StrokeThickness { get; set; } = 1.5;
    public double ArrowSize { get; set; } = 6.0;
    public double ExtensionLineLength { get; set; } = 10.0;
    public double FontSize { get; set; } = 10.0;

    public double CalculateDistance()
    {
        double dx = EndX - StartX;
        double dy = EndY - StartY;
        return Math.Sqrt(dx * dx + dy * dy);
    }

    public string GetFormattedDistance()
    {
        if (!string.IsNullOrWhiteSpace(CustomLabel))
            return CustomLabel;

        double distancePts = CalculateDistance();
        return Unit switch
        {
            RulerUnit.Inches => $"{distancePts / 72.0:F2} in",
            RulerUnit.Millimeters => $"{distancePts * 25.4 / 72.0:F1} mm",
            _ => $"{distancePts:F1} pt"
        };
    }
}
