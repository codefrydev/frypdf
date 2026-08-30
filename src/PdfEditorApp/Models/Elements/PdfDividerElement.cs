namespace PdfEditorApp.Models.Elements;

public class PdfDividerElement : PdfElementBase
{
    public override ElementKind Kind => ElementKind.Divider;

    public string ColorHex { get; set; } = "#0F6CBD";
    public double Thickness { get; set; } = 2;
    public bool IsVertical { get; set; } = false;

    public override PdfElementBase Clone()
    {
        var clone = (PdfDividerElement)base.Clone();
        return clone;
    }
}
