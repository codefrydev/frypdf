namespace PdfEditorApp.Models.Elements;

public class PdfWatermarkElement : PdfElementBase
{
    public override ElementKind Kind => ElementKind.Watermark;

    public string Text { get; set; } = "CONFIDENTIAL";
    public double FontSize { get; set; } = 48;
    public string ColorHex { get; set; } = "#CC0000";
    public new double Opacity { get; set; } = 0.15;
    public double Angle { get; set; } = -35;

    public override PdfElementBase Clone()
    {
        var clone = (PdfWatermarkElement)base.Clone();
        return clone;
    }
}
