using System;

namespace PdfEditorApp.Models.Elements;

/// <summary>
/// Specifies the vertical script alignment for inline rich text (e.g. chemical formulas H₂O, exponents x²).
/// </summary>
public enum TextScriptMode
{
    Normal = 0,
    Superscript = 1,
    Subscript = 2
}

/// <summary>
/// Represents an inline text span (run) within a <see cref="PdfTextElement"/>,
/// allowing granular control over fonts, weights, colors, styles, scripts, and links.
/// </summary>
public class PdfTextSpan
{
    /// <summary>
    /// The plain text content of this inline span.
    /// </summary>
    public string Text { get; set; } = string.Empty;

    /// <summary>
    /// Optional font family override. If null, inherits from parent <see cref="PdfTextElement.FontFamily"/>.
    /// </summary>
    public string? FontFamily { get; set; }

    /// <summary>
    /// Optional font size override in points. If null, inherits from parent <see cref="PdfTextElement.FontSize"/>.
    /// </summary>
    public double? FontSize { get; set; }

    /// <summary>
    /// Optional bold override. If null, inherits from parent <see cref="PdfTextElement.IsBold"/>.
    /// </summary>
    public bool? IsBold { get; set; }

    /// <summary>
    /// Optional italic override. If null, inherits from parent <see cref="PdfTextElement.IsItalic"/>.
    /// </summary>
    public bool? IsItalic { get; set; }

    /// <summary>
    /// Optional underline override. If null, inherits from parent <see cref="PdfTextElement.IsUnderline"/>.
    /// </summary>
    public bool? IsUnderline { get; set; }

    /// <summary>
    /// Optional strikethrough override. If null, inherits from parent <see cref="PdfTextElement.IsStrikethrough"/>.
    /// </summary>
    public bool? IsStrikethrough { get; set; }

    /// <summary>
    /// Optional text color hex override (e.g. #0F6CBD). If null, inherits from parent <see cref="PdfTextElement.TextColorHex"/>.
    /// </summary>
    public string? TextColorHex { get; set; }

    /// <summary>
    /// Optional background highlight color hex (e.g. #FFEB3B). If null or empty, transparent.
    /// </summary>
    public string? HighlightColorHex { get; set; }

    /// <summary>
    /// Vertical script alignment (Normal, Superscript, Subscript).
    /// </summary>
    public TextScriptMode Script { get; set; } = TextScriptMode.Normal;

    /// <summary>
    /// Optional clickable hyperlink URI (e.g. https://example.com).
    /// </summary>
    public string? LinkUrl { get; set; }

    /// <summary>
    /// Creates a deep clone of this text span.
    /// </summary>
    public PdfTextSpan Clone()
    {
        return new PdfTextSpan
        {
            Text = Text,
            FontFamily = FontFamily,
            FontSize = FontSize,
            IsBold = IsBold,
            IsItalic = IsItalic,
            IsUnderline = IsUnderline,
            IsStrikethrough = IsStrikethrough,
            TextColorHex = TextColorHex,
            HighlightColorHex = HighlightColorHex,
            Script = Script,
            LinkUrl = LinkUrl
        };
    }
}
