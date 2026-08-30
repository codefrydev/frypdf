namespace PdfEditorApp.Models.Elements;

public class PdfRedactionElement : PdfElementBase
{
    public override ElementKind Kind => ElementKind.Redaction;

    public RedactionMode Mode { get; set; } = RedactionMode.Blackout;
    public string ExemptionCode { get; set; } = "[REDACTED - (b)(4) PRIVILEGED]";
    public string FillColorHex { get; set; } = "#0F172A";
    public string TextColorHex { get; set; } = "#FFFFFF";
    public string BorderColorHex { get; set; } = "#DC2626";
    public double BorderThickness { get; set; } = 1.5;
    public bool ShowOverlayText { get; set; } = true;

    public override PdfElementBase Clone()
    {
        return (PdfRedactionElement)base.Clone();
    }
}
