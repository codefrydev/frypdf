namespace PdfEditorApp.Models.Elements;

public class PdfTextElement : PdfElementBase
{
    public override ElementKind Kind => ElementKind.Text;

    public string Text { get; set; } = "Enter text here";
    public string FontFamily { get; set; } = "Segoe UI";
    public double FontSize { get; set; } = 14;
    public bool IsBold { get; set; } = false;
    public bool IsItalic { get; set; } = false;
    public bool IsUnderline { get; set; } = false;
    public bool IsStrikethrough { get; set; } = false;
    public string TextColorHex { get; set; } = "#201F1E";
    public TextAlignmentMode Alignment { get; set; } = TextAlignmentMode.Left;
    public double LineHeight { get; set; } = 1.4;
    public double CharacterSpacing { get; set; } = 0;
    public double Padding { get; set; } = 4;
    public string BackgroundColorHex { get; set; } = "#00000000"; // Transparent by default
    public string BorderColorHex { get; set; } = "#00000000";
    public double BorderThickness { get; set; } = 0;
    public double CornerRadius { get; set; } = 0;

    public override PdfElementBase Clone()
    {
        var clone = (PdfTextElement)base.Clone();
        return clone;
    }
}
