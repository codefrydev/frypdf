using System;

namespace PdfEditorApp.Models.Elements;

/// <summary>
/// Native mathematical equation & formula element supporting standard LaTeX / Math syntax,
/// vector SVG rendering, equation numbering tags, and full typography customization.
/// </summary>
public class PdfMathElement : PdfElementBase
{
    public override ElementKind Kind => ElementKind.Math;

    /// <summary>The mathematical expression in standard LaTeX syntax.</summary>
    public string Formula { get; set; } = @"\int_{-\infty}^{\infty} e^{-x^2} \, dx = \sqrt{\pi}";

    /// <summary>Base typography font size for rendered math symbols.</summary>
    public double FontSize { get; set; } = 16;

    /// <summary>Hex color for mathematical symbols, text, and operators.</summary>
    public string TextColorHex { get; set; } = "#0F172A";

    /// <summary>Optional background color fill for equation card/callout.</summary>
    public string BackgroundColorHex { get; set; } = "#00000000";

    /// <summary>Border stroke color.</summary>
    public string BorderColorHex { get; set; } = "#00000000";

    /// <summary>Border stroke thickness.</summary>
    public double BorderThickness { get; set; } = 0;

    /// <summary>Corner radius for rounded equation card borders.</summary>
    public double CornerRadius { get; set; } = 4;

    /// <summary>Internal padding around the rendered equation.</summary>
    public double Padding { get; set; } = 8;

    /// <summary>Whether to render a visible background box.</summary>
    public bool ShowBackground { get; set; } = false;

    /// <summary>Whether to render a visible border around the equation.</summary>
    public bool ShowBorder { get; set; } = false;

    /// <summary>Whether to render an equation number tag (e.g. (1), (2.3)) aligned to the right.</summary>
    public bool ShowEquationNumber { get; set; } = false;

    /// <summary>Equation numbering label (e.g. "(1)", "(2.4)", "(IV.1)").</summary>
    public string EquationNumber { get; set; } = "(1)";

    /// <summary>Alignment of the equation within its bounding box (Left, Center, Right).</summary>
    public TextAlignmentMode Alignment { get; set; } = TextAlignmentMode.Center;

    /// <summary>Display mode (DisplayBlock for standalone centered equations, Inline for compact formulas).</summary>
    public MathDisplayStyle DisplayStyle { get; set; } = MathDisplayStyle.DisplayBlock;

    /// <summary>Subject category classification (Algebra, Calculus, Physics, Quantum, Finance, etc.).</summary>
    public MathCategory Category { get; set; } = MathCategory.Calculus;

    /// <summary>Optional preset name if created from template library.</summary>
    public string? PresetName { get; set; } = "Gaussian Integral";

    /// <summary>Optional user explanation or theorem title.</summary>
    public string? Description { get; set; } = "Euler-Poisson Gaussian Integral";

    public override PdfElementBase Clone()
    {
        return (PdfMathElement)base.Clone();
    }
}
