namespace PdfEditorApp.Core.Models.Elements;

public class PdfShapeElement : PdfElementBase
{
    public override ElementKind Kind => ElementKind.Shape;

    public ShapeType ShapeType { get; set; } = ShapeType.Rectangle;
    public string FillColorHex { get; set; } = "#F8F9FA";
    public string StrokeColorHex { get; set; } = "#0F6CBD";
    public double StrokeThickness { get; set; } = 1.5;
    public double CornerRadius { get; set; } = 4;
    public string? Label { get; set; }
    public string? LabelColorHex { get; set; } = "#201F1E";
    public double LabelFontSize { get; set; } = 12;

    public string? CustomPathData { get; set; }
    public string? SecondaryFillColorHex { get; set; }
    public string? SecondaryStrokeColorHex { get; set; }

    // Bézier Curves, Connectors & Line Caps
    public double BezierP0X { get; set; } = 0.0;
    public double BezierP0Y { get; set; } = 0.5;
    public double BezierP1X { get; set; } = 0.33;
    public double BezierP1Y { get; set; } = 0.10;
    public double BezierP2X { get; set; } = 0.67;
    public double BezierP2Y { get; set; } = 0.90;
    public double BezierP3X { get; set; } = 1.0;
    public double BezierP3Y { get; set; } = 0.5;

    public LineEndCap StartCap { get; set; } = LineEndCap.None;
    public LineEndCap EndCap { get; set; } = LineEndCap.None;
    public LineDashStyle DashStyle { get; set; } = LineDashStyle.Solid;
    public double WaveFrequency { get; set; } = 2.0;
    public double CurvatureDepth { get; set; } = 40.0;

    public override PdfElementBase Clone()
    {
        var clone = (PdfShapeElement)base.Clone();
        clone.CustomPathData = CustomPathData;
        clone.SecondaryFillColorHex = SecondaryFillColorHex;
        clone.SecondaryStrokeColorHex = SecondaryStrokeColorHex;
        return clone;
    }
}
