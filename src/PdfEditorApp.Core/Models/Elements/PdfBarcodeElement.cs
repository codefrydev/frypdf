namespace PdfEditorApp.Core.Models.Elements;

public class PdfBarcodeElement : PdfElementBase
{
    public override ElementKind Kind => ElementKind.Barcode;

    public string CodeValue { get; set; } = "DOC-2026-984210";
    public string BarcodeFormat { get; set; } = "Code128";
    public string BarColorHex { get; set; } = "#0F172A";
    public string BackgroundColorHex { get; set; } = "#FFFFFF";
    public bool ShowText { get; set; } = true;

    public override PdfElementBase Clone()
    {
        return (PdfBarcodeElement)base.Clone();
    }
}
