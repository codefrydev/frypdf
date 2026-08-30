namespace PdfEditorApp.Models.Elements;

/// <summary>
/// Native vector SVG element for logos, icons, ceremonial crests, garlands, and complex illustrations.
/// </summary>
public class PdfSvgElement : PdfElementBase
{
    public override ElementKind Kind => ElementKind.Svg;

    /// <summary>Raw SVG XML string markup.</summary>
    public string SvgSource { get; set; } = "<svg viewBox=\"0 0 100 100\"><circle cx=\"50\" cy=\"50\" r=\"40\" fill=\"#D97706\"/></svg>";

    /// <summary>Optional file path if loaded from external disk file.</summary>
    public string? FilePath { get; set; }

    /// <summary>Optional color override / tint (if specified, replaces default fills with this color).</summary>
    public string? TintColorHex { get; set; }

    /// <summary>Preset ornament identifier if selected from built-in library.</summary>
    public string? PresetName { get; set; }

    public bool KeepAspectRatio { get; set; } = true;
    public double CornerRadius { get; set; } = 0;
    public string? BorderColorHex { get; set; }
    public double BorderThickness { get; set; } = 0;

    public override PdfElementBase Clone()
    {
        return (PdfSvgElement)base.Clone();
    }
}
