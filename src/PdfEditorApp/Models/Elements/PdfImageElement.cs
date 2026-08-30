namespace PdfEditorApp.Models.Elements;

public class PdfImageElement : PdfElementBase
{
    public override ElementKind Kind => ElementKind.Image;

    public string? ImagePath { get; set; }
    public string? Base64Data { get; set; }
    public bool KeepAspectRatio { get; set; } = true;
    public double CornerRadius { get; set; } = 4;
    public string BorderColorHex { get; set; } = "#E1DFDD";
    public double BorderThickness { get; set; } = 1;
    public string AltText { get; set; } = "Image";

    public override PdfElementBase Clone()
    {
        var clone = (PdfImageElement)base.Clone();
        return clone;
    }
}
