namespace PdfEditorApp.Models.Elements;

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

    public override PdfElementBase Clone()
    {
        var clone = (PdfShapeElement)base.Clone();
        clone.CustomPathData = CustomPathData;
        clone.SecondaryFillColorHex = SecondaryFillColorHex;
        clone.SecondaryStrokeColorHex = SecondaryStrokeColorHex;
        return clone;
    }
}
