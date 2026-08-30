namespace PdfEditorApp.Models.Elements;

public class PdfInkElement : PdfElementBase
{
    public override ElementKind Kind => ElementKind.Ink;

    public string PointsData { get; set; } = "10,10 50,40 100,20 180,60 220,30";
    public string StrokeColorHex { get; set; } = "#0F6CBD";
    public double StrokeThickness { get; set; } = 3.0;
    public bool IsHighlighter { get; set; } = false;

    public override PdfElementBase Clone()
    {
        return (PdfInkElement)base.Clone();
    }
}
