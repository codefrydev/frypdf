namespace PdfEditorApp.Core.Deconstruction;

/// <summary>
/// Configurable options and geometric/typographic heuristics for the PDF Deconstruction Engine.
/// Replaces hardcoded magic numbers and enables tuning for diverse document archetypes (e.g. scanned IDs, textbooks, reports, forms).
/// </summary>
public class PdfDeconstructionOptions
{
    /// <summary>Default singleton instance with production-tuned heuristics.</summary>
    public static PdfDeconstructionOptions Default => new();

    /// <summary>Minimum percentage of page area an image must occupy to be classified as a scanned page canvas (0.0..1.0).</summary>
    public double PureScannedImageCoverageThreshold { get; set; } = 0.85;

    /// <summary>Maximum digital word count on a page to still treat it as a pure scanned document.</summary>
    public int PureScannedWordCountMax { get; set; } = 5;

    /// <summary>Minimum width percentage for an image to be classified as a document watermark (0.0..1.0).</summary>
    public double WatermarkWidthRatio { get; set; } = 0.65;

    /// <summary>Minimum height percentage for an image to be classified as a document watermark (0.0..1.0).</summary>
    public double WatermarkHeightRatio { get; set; } = 0.55;

    /// <summary>Opacity applied to detected watermark images (0.0..1.0).</summary>
    public double WatermarkOpacity { get; set; } = 0.10;

    /// <summary>Minimum width and height ratio for an image to be considered a full-page background underlay.</summary>
    public double FullPageBgRatio { get; set; } = 0.88;

    /// <summary>Minimum width in points for a filled vector rectangle to be considered a background container card.</summary>
    public double LargeContainerMinWidth { get; set; } = 120.0;

    /// <summary>Minimum height in points for a filled vector rectangle to be considered a background container card.</summary>
    public double LargeContainerMinHeight { get; set; } = 80.0;

    /// <summary>Maximum height in points for a vector path to be categorized as a horizontal divider line.</summary>
    public double DividerMaxHeight { get; set; } = 3.5;

    /// <summary>Minimum width in points for a vector path to be categorized as a horizontal divider line.</summary>
    public double DividerMinWidth { get; set; } = 6.0;

    /// <summary>Maximum number of individual vector shapes extracted before grouping remaining excess micro-paths into a combined SVG.</summary>
    public int MaxVectorShapesPerPage { get; set; } = 1000;

    /// <summary>Whether to group vector paths exceeding <see cref="MaxVectorShapesPerPage"/> into a single <see cref="Models.Elements.PdfSvgElement"/> instead of dropping them.</summary>
    public bool GroupExcessVectorsAsSvg { get; set; } = true;

    /// <summary>Minimum dimension (width and height in points) for a vector shape to be extracted as a standalone element.</summary>
    public double MinShapeDimension { get; set; } = 2.0;

    /// <summary>Minimum width in points for card/container grouping with inner text elements.</summary>
    public double MinContainerCardWidth { get; set; } = 60.0;

    /// <summary>Minimum height in points for card/container grouping with inner text elements.</summary>
    public double MinContainerCardHeight { get; set; } = 30.0;

    /// <summary>Default high-contrast text color hex when contrast against background shape is insufficient.</summary>
    public string HighContrastDarkTextColor { get; set; } = "#0F172A";

    /// <summary>Default high-contrast light text color hex when contrast against dark background shape is insufficient.</summary>
    public string HighContrastLightTextColor { get; set; } = "#FFFFFF";

    /// <summary>Minimum WCAG relative luminance contrast ratio before dynamic text contrast adjustment is applied.</summary>
    public double MinContrastRatio { get; set; } = 1.25;

    /// <summary>Column gap multiplier for landscape layout analysis.</summary>
    public double ColumnGapMultiplierLandscape { get; set; } = 1.5;

    /// <summary>Column gap multiplier for portrait layout analysis.</summary>
    public double ColumnGapMultiplierPortrait { get; set; } = 1.0;

    /// <summary>Initial Z-index for background container cards and full-page watermarks.</summary>
    public int InitialBgZIndex { get; set; } = 0;

    /// <summary>Initial Z-index for content images (photos, QR codes, emblems, logos).</summary>
    public int InitialImgZIndex { get; set; } = 100;

    /// <summary>Initial Z-index for structured tables and data grids.</summary>
    public int InitialTableZIndex { get; set; } = 500;

    /// <summary>Initial Z-index for foreground vector shapes, badges, and divider lines.</summary>
    public int InitialShapeZIndex { get; set; } = 600;

    /// <summary>Initial Z-index for text paragraphs and headings.</summary>
    public int InitialTextZIndex { get; set; } = 1000;

    /// <summary>Initial Z-index for interactive AcroForm form fields.</summary>
    public int InitialFormZIndex { get; set; } = 2000;
}
