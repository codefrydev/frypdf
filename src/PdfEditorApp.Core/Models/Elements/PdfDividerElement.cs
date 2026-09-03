namespace PdfEditorApp.Core.Models.Elements;

public class PdfDividerElement : PdfElementBase
{
    public override ElementKind Kind => ElementKind.Divider;

    public string ColorHex { get; set; } = "#0F6CBD";
    public double Thickness { get; set; } = 2;
    public bool IsVertical { get; set; } = false;
    public DividerStyle Style { get; set; } = DividerStyle.Straight;
    public double WaveAmplitude { get; set; } = 6.0;
    public double WaveFrequency { get; set; } = 4.0;
    public LineDashStyle DashStyle { get; set; } = LineDashStyle.Solid;

    public override PdfElementBase Clone()
    {
        var clone = (PdfDividerElement)base.Clone();
        return clone;
    }
}
