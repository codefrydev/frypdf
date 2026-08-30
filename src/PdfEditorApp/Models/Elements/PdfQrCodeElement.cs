namespace PdfEditorApp.Models.Elements;

public class PdfQrCodeElement : PdfElementBase
{
    public override ElementKind Kind => ElementKind.QrCode;

    public string Content { get; set; } = "https://github.com/PrashantUnity/PDFCreator";
    public string DarkColorHex { get; set; } = "#0F172A";
    public string LightColorHex { get; set; } = "#FFFFFF";
    public string Label { get; set; } = "SCAN TO VERIFY CREDENTIAL";

    public override PdfElementBase Clone()
    {
        return (PdfQrCodeElement)base.Clone();
    }
}
