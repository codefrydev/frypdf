namespace PdfEditorApp.Core.Models.Elements;

public class PdfTextElement : PdfElementBase
{
    public override ElementKind Kind => ElementKind.Text;

    // Content & Typography
    public string Text { get; set; } = "Enter text here";
    public string FontFamily { get; set; } = "Segoe UI";
    public double FontSize { get; set; } = 14;
    public bool IsBold { get; set; } = false;
    public bool IsItalic { get; set; } = false;
    public bool IsUnderline { get; set; } = false;
    public bool IsDoubleUnderline { get; set; } = false;
    public bool IsStrikethrough { get; set; } = false;
    public string TextColorHex { get; set; } = "#201F1E";
    public double TextOpacity { get; set; } = 1.0;

    // Paragraph & Spacing
    public TextAlignmentMode Alignment { get; set; } = TextAlignmentMode.Left;
    public TextVerticalAlignment VerticalAlignment { get; set; } = TextVerticalAlignment.Top;
    public double LineHeight { get; set; } = 1.4;
    public double CharacterSpacing { get; set; } = 0;
    public double WordSpacing { get; set; } = 0;
    public double ParagraphSpacing { get; set; } = 0;
    public bool TextWrap { get; set; } = true;

    // Stroke / Outline
    public bool HasStroke { get; set; } = false;
    public string StrokeColorHex { get; set; } = "#000000";
    public double StrokeWidth { get; set; } = 1.0;

    // Shadow & Glow
    public bool HasShadow { get; set; } = false;
    public string ShadowColorHex { get; set; } = "#80000000";
    public double ShadowOffsetX { get; set; } = 2.0;
    public double ShadowOffsetY { get; set; } = 2.0;
    public double ShadowBlurRadius { get; set; } = 4.0;
    public double ShadowOpacity { get; set; } = 0.5;

    // Box Background, Border & Padding
    public double Padding { get; set; } = 0;
    public string BackgroundColorHex { get; set; } = "#00000000"; // Transparent by default
    public string BorderColorHex { get; set; } = "#00000000";
    public double BorderThickness { get; set; } = 0;
    public double CornerRadius { get; set; } = 0;

    // Curved & Circular Typography
    public TextShapeMode ShapeMode { get; set; } = TextShapeMode.Normal;
    public double CurveRadius { get; set; } = 120;
    public double CurveArcAngle { get; set; } = 180; // Sweep angle in degrees (e.g. 30 to 360)
    public double CurveStartAngle { get; set; } = 0; // Center offset angle in degrees
    public bool CurveClockwise { get; set; } = true;
    public bool CurveInvert { get; set; } = false; // Invert text along curve (inside vs outside)
    public CircularTextPlacement CircularPlacement { get; set; } = CircularTextPlacement.TopArc;

    // Per-Character & Transform Offsets
    public double BaselineShift { get; set; } = 0;
    public double CharacterRotation { get; set; } = 0;
    public double ScaleX { get; set; } = 1.0;
    public double ScaleY { get; set; } = 1.0;
    public bool FlipX { get; set; } = false;
    public bool FlipY { get; set; } = false;

    // Bézier Curve Typography (Normalized 0.0 to 1.0 relative coordinates)
    public BezierCurvePreset BezierPreset { get; set; } = BezierCurvePreset.Wave;
    public double BezierP0X { get; set; } = 0.0;
    public double BezierP0Y { get; set; } = 0.5;
    public double BezierP1X { get; set; } = 0.33;
    public double BezierP1Y { get; set; } = 0.10;
    public double BezierP2X { get; set; } = 0.67;
    public double BezierP2Y { get; set; } = 0.90;
    public double BezierP3X { get; set; } = 1.0;
    public double BezierP3Y { get; set; } = 0.5;

    // Multi-Span Rich Text Runs (Granular inline formatting)
    public System.Collections.Generic.List<PdfTextSpan>? Spans { get; set; }

    /// <summary>
    /// Returns the effective plain text for search, display titles, and fallback rendering.
    /// Concatenates inline spans if present, otherwise returns <see cref="Text"/>.
    /// </summary>
    public string GetEffectivePlainText()
    {
        if (Spans == null || Spans.Count == 0)
        {
            return Text ?? string.Empty;
        }

        var sb = new System.Text.StringBuilder();
        foreach (var span in Spans)
        {
            sb.Append(span.Text);
        }
        return sb.ToString();
    }

    /// <summary>
    /// Synchronizes <see cref="Text"/> with the concatenated content of <see cref="Spans"/>.
    /// </summary>
    public void SynchronizePlainTextFromSpans()
    {
        if (Spans != null && Spans.Count > 0)
        {
            Text = GetEffectivePlainText();
        }
    }

    public override PdfElementBase Clone()
    {
        var clone = (PdfTextElement)base.Clone();
        if (Spans != null)
        {
            clone.Spans = new System.Collections.Generic.List<PdfTextSpan>(Spans.Count);
            foreach (var span in Spans)
            {
                clone.Spans.Add(span.Clone());
            }
        }
        return clone;
    }
}
