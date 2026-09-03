using System;
using System.Globalization;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Media;
using PdfEditorApp.Core.Models;
using PdfEditorApp.Models;
using PdfEditorApp.Services.MathEngine;

namespace PdfEditorApp.Views;

public class MathFormulaControl : Control
{
    public static readonly StyledProperty<string> FormulaProperty =
        AvaloniaProperty.Register<MathFormulaControl, string>(nameof(Formula), defaultValue: @"\int_{-\infty}^{\infty} e^{-x^2} \, dx = \sqrt{\pi}");

    public static readonly StyledProperty<double> FontSizeProperty =
        AvaloniaProperty.Register<MathFormulaControl, double>(nameof(FontSize), defaultValue: 16.0);

    public static readonly StyledProperty<IBrush?> TextColorProperty =
        AvaloniaProperty.Register<MathFormulaControl, IBrush?>(nameof(TextColor), defaultValue: Brushes.Black);

    public static readonly StyledProperty<bool> ShowEquationNumberProperty =
        AvaloniaProperty.Register<MathFormulaControl, bool>(nameof(ShowEquationNumber), defaultValue: false);

    public static readonly StyledProperty<string> EquationNumberProperty =
        AvaloniaProperty.Register<MathFormulaControl, string>(nameof(EquationNumber), defaultValue: "(1)");

    public static readonly StyledProperty<TextAlignmentMode> AlignmentProperty =
        AvaloniaProperty.Register<MathFormulaControl, TextAlignmentMode>(nameof(Alignment), defaultValue: TextAlignmentMode.Center);

    public string Formula
    {
        get => GetValue(FormulaProperty);
        set => SetValue(FormulaProperty, value);
    }

    public double FontSize
    {
        get => GetValue(FontSizeProperty);
        set => SetValue(FontSizeProperty, value);
    }

    public IBrush? TextColor
    {
        get => GetValue(TextColorProperty);
        set => SetValue(TextColorProperty, value);
    }

    public bool ShowEquationNumber
    {
        get => GetValue(ShowEquationNumberProperty);
        set => SetValue(ShowEquationNumberProperty, value);
    }

    public string EquationNumber
    {
        get => GetValue(EquationNumberProperty);
        set => SetValue(EquationNumberProperty, value);
    }

    public TextAlignmentMode Alignment
    {
        get => GetValue(AlignmentProperty);
        set => SetValue(AlignmentProperty, value);
    }

    static MathFormulaControl()
    {
        AffectsRender<MathFormulaControl>(
            FormulaProperty,
            FontSizeProperty,
            TextColorProperty,
            ShowEquationNumberProperty,
            EquationNumberProperty,
            AlignmentProperty
        );
        AffectsMeasure<MathFormulaControl>(
            FormulaProperty,
            FontSizeProperty,
            ShowEquationNumberProperty,
            EquationNumberProperty
        );
    }

    private static readonly Typeface _serifTypeface = new("Times New Roman, Cambria Math, Latin Modern Math, serif", FontStyle.Normal, FontWeight.Normal);
    private static readonly Typeface _italicTypeface = new("Times New Roman, Cambria Math, Latin Modern Math, serif", FontStyle.Italic, FontWeight.Normal);
    private static readonly Typeface _boldTypeface = new("Times New Roman, Cambria Math, Latin Modern Math, serif", FontStyle.Normal, FontWeight.Bold);

    protected override Size MeasureOverride(Size availableSize)
    {
        try
        {
            string f = string.IsNullOrWhiteSpace(Formula) ? "f(x)" : Formula;
            var tokens = MathLayoutEngine.Tokenize(f);
            var parser = new MathLayoutEngine.MathParser(tokens);
            var root = parser.ParseExpression();

            var ctx = new MathLayoutEngine.LayoutContext(FontSize, "#000000", MathDisplayStyle.DisplayBlock);
            root.Measure(ctx);

            double w = root.Width;
            double h = root.Height;

            if (ShowEquationNumber && !string.IsNullOrWhiteSpace(EquationNumber))
            {
                w += (EquationNumber.Length * FontSize * 0.55) + 20;
            }

            return new Size(Math.Max(30, w), Math.Max(20, h));
        }
        catch
        {
            return new Size(120, 30);
        }
    }

    public override void Render(DrawingContext context)
    {
        base.Render(context);

        double w = Bounds.Width;
        double h = Bounds.Height;
        if (w <= 0 || h <= 0) return;

        var brush = TextColor ?? Brushes.Black;

        try
        {
            string f = string.IsNullOrWhiteSpace(Formula) ? "f(x)" : Formula;
            var tokens = MathLayoutEngine.Tokenize(f);
            var parser = new MathLayoutEngine.MathParser(tokens);
            var root = parser.ParseExpression();

            var ctx = new MathLayoutEngine.LayoutContext(FontSize, "#000000", MathDisplayStyle.DisplayBlock);
            root.Measure(ctx);

            double contentW = root.Width;
            double contentH = root.Height;
            double ascent = root.Ascent;

            double eqNumWidth = 0;
            if (ShowEquationNumber && !string.IsNullOrWhiteSpace(EquationNumber))
            {
                eqNumWidth = (EquationNumber.Length * FontSize * 0.55) + 15;
            }

            double startX = 4;
            if (Alignment == TextAlignmentMode.Center)
            {
                startX = Math.Max(4, (w - contentW - eqNumWidth) / 2);
            }
            else if (Alignment == TextAlignmentMode.Right)
            {
                startX = Math.Max(4, w - contentW - eqNumWidth - 4);
            }

            double baselineY = ascent + Math.Max(0, (h - contentH) / 2);

            // Render AST Recursively onto DrawingContext
            RenderAstNode(context, root, startX, baselineY, FontSize, brush);

            // Render Equation Number on Right
            if (ShowEquationNumber && !string.IsNullOrWhiteSpace(EquationNumber))
            {
                var eqNumFormatted = new FormattedText(
                    EquationNumber,
                    CultureInfo.InvariantCulture,
                    FlowDirection.LeftToRight,
                    _boldTypeface,
                    FontSize * 0.9,
                    brush
                );
                context.DrawText(eqNumFormatted, new Point(w - eqNumFormatted.Width - 4, baselineY - (eqNumFormatted.Height * 0.75)));
            }
        }
        catch (Exception)
        {
            var errText = new FormattedText(
                Formula,
                CultureInfo.InvariantCulture,
                FlowDirection.LeftToRight,
                _serifTypeface,
                11,
                Brushes.Red
            );
            context.DrawText(errText, new Point(4, (h - errText.Height) / 2));
        }
    }

    private void RenderAstNode(DrawingContext dc, MathLayoutEngine.MathAstNode node, double x, double y, double fontSize, IBrush brush)
    {
        switch (node)
        {
            case MathLayoutEngine.MathSequenceNode seq:
                double curX = x;
                foreach (var child in seq.Children)
                {
                    RenderAstNode(dc, child, curX, y, fontSize, brush);
                    curX += child.Width;
                }
                break;

            case MathLayoutEngine.MathTextNode textNode:
                var tf = textNode.IsBold ? _boldTypeface : (textNode.IsVariable ? _italicTypeface : _serifTypeface);
                var formatted = new FormattedText(
                    textNode.Text,
                    CultureInfo.InvariantCulture,
                    FlowDirection.LeftToRight,
                    tf,
                    fontSize,
                    brush
                );
                dc.DrawText(formatted, new Point(x, y - (fontSize * 0.78)));
                break;

            case MathLayoutEngine.MathSymbolNode symNode:
                var symFormatted = new FormattedText(
                    symNode.Symbol,
                    CultureInfo.InvariantCulture,
                    FlowDirection.LeftToRight,
                    _serifTypeface,
                    fontSize,
                    brush
                );
                dc.DrawText(symFormatted, new Point(x + symNode.ExtraSpacing, y - (fontSize * 0.8)));
                break;

            case MathLayoutEngine.MathFractionNode frac:
                double childSize = fontSize * 0.82;
                double barY = y - (fontSize * 0.28);
                double barGap = fontSize * 0.22;

                // Numerator
                double numX = x + ((frac.Width - frac.Numerator.Width) / 2);
                double numY = barY - barGap - frac.Numerator.Descent;
                RenderAstNode(dc, frac.Numerator, numX, numY, childSize, brush);

                // Fraction Bar Line
                double lineThick = Math.Max(1.0, fontSize * 0.07);
                var pen = new Pen(brush, lineThick, lineCap: PenLineCap.Round);
                dc.DrawLine(pen, new Point(x, barY), new Point(x + frac.Width, barY));

                // Denominator
                double denX = x + ((frac.Width - frac.Denominator.Width) / 2);
                double denY = barY + barGap + frac.Denominator.Ascent;
                RenderAstNode(dc, frac.Denominator, denX, denY, childSize, brush);
                break;

            case MathLayoutEngine.MathRadicalNode rad:
                double degW = 0;
                if (rad.Degree != null)
                {
                    double degSize = fontSize * 0.58;
                    RenderAstNode(dc, rad.Degree, x, y - (fontSize * 0.5), degSize, brush);
                    degW = rad.Degree.Width * 0.4;
                }

                double radX = x + degW;
                double topY = y - rad.Ascent + (fontSize * 0.05);
                double bottomY = y + rad.Descent;
                double radStartX = radX + (fontSize * 0.6);
                double radEndX = x + rad.Width;

                // Draw checkmark + overbar geometry
                var geom = new StreamGeometry();
                using (var gc = geom.Open())
                {
                    gc.BeginFigure(new Point(radX, y - (fontSize * 0.2)), false);
                    gc.LineTo(new Point(radX + (fontSize * 0.28), bottomY));
                    gc.LineTo(new Point(radStartX, topY));
                    gc.LineTo(new Point(radEndX, topY));
                }

                var radPen = new Pen(brush, Math.Max(1.0, fontSize * 0.065), lineJoin: PenLineJoin.Miter);
                dc.DrawGeometry(null, radPen, geom);

                // Render Radicand
                RenderAstNode(dc, rad.Radicand, radStartX + (fontSize * 0.08), y, fontSize, brush);
                break;

            case MathLayoutEngine.MathSubSuperNode subSuper:
                RenderAstNode(dc, subSuper.BaseNode, x, y, fontSize, brush);

                double scriptX = x + subSuper.BaseNode.Width + (fontSize * 0.04);
                double scriptSize = fontSize * 0.72;

                if (subSuper.Superscript != null)
                {
                    double supY = y - (fontSize * 0.42);
                    RenderAstNode(dc, subSuper.Superscript, scriptX, supY, scriptSize, brush);
                }

                if (subSuper.Subscript != null)
                {
                    double subY = y + (fontSize * 0.28);
                    RenderAstNode(dc, subSuper.Subscript, scriptX, subY, scriptSize, brush);
                }
                break;

            case MathLayoutEngine.MathLargeOperatorNode opNode:
                double opScale = opNode.IsDisplayMode ? 1.4 : 1.1;
                double opSize = fontSize * opScale;
                double scriptScaleSize = fontSize * 0.68;

                bool isInt = opNode.OperatorName.Contains("int");
                string sym = opNode.OperatorName switch
                {
                    "sum" => "∑",
                    "prod" => "∏",
                    "coprod" => "∐",
                    "int" => "∫",
                    "iint" => "∬",
                    "iiint" => "∭",
                    "oint" => "∮",
                    "lim" => "lim",
                    _ => "∑"
                };

                if (isInt || !opNode.IsDisplayMode)
                {
                    var opFormatted = new FormattedText(
                        sym,
                        CultureInfo.InvariantCulture,
                        FlowDirection.LeftToRight,
                        _serifTypeface,
                        opSize,
                        brush
                    );
                    dc.DrawText(opFormatted, new Point(x, y - (opSize * 0.75)));

                    double limitX = x + (fontSize * 0.75 * opScale) + (fontSize * 0.05);
                    if (opNode.UpperLimit != null)
                    {
                        RenderAstNode(dc, opNode.UpperLimit, limitX, y - (fontSize * opScale * 0.55), scriptScaleSize, brush);
                    }
                    if (opNode.LowerLimit != null)
                    {
                        RenderAstNode(dc, opNode.LowerLimit, limitX, y + (fontSize * opScale * 0.35), scriptScaleSize, brush);
                    }
                }
                else
                {
                    double opW = sym == "lim" ? fontSize * 1.3 : fontSize * 1.0 * opScale;
                    double opX = x + ((opNode.Width - opW) / 2);

                    if (opNode.UpperLimit != null)
                    {
                        double upX = x + ((opNode.Width - opNode.UpperLimit.Width) / 2);
                        double upY = y - (fontSize * opScale * 0.65) - opNode.UpperLimit.Descent;
                        RenderAstNode(dc, opNode.UpperLimit, upX, upY, scriptScaleSize, brush);
                    }

                    var opFormatted = new FormattedText(
                        sym,
                        CultureInfo.InvariantCulture,
                        FlowDirection.LeftToRight,
                        _serifTypeface,
                        opSize,
                        brush
                    );
                    dc.DrawText(opFormatted, new Point(opX, y - (opSize * 0.75)));

                    if (opNode.LowerLimit != null)
                    {
                        double lowX = x + ((opNode.Width - opNode.LowerLimit.Width) / 2);
                        double lowY = y + (fontSize * opScale * 0.3) + opNode.LowerLimit.Ascent;
                        RenderAstNode(dc, opNode.LowerLimit, lowX, lowY, scriptScaleSize, brush);
                    }
                }
                break;

            case MathLayoutEngine.MathDelimiterNode delim:
                double scale = Math.Max(1.0, delim.Content.Height / (fontSize * 1.1));
                double delimCurX = x;

                if (delim.LeftDelim != "." && !string.IsNullOrEmpty(delim.LeftDelim))
                {
                    RenderDelimText(dc, delim.LeftDelim, delimCurX, y, fontSize * scale, brush);
                    delimCurX += fontSize * 0.45;
                }

                RenderAstNode(dc, delim.Content, delimCurX, y, fontSize, brush);
                delimCurX += delim.Content.Width;

                if (delim.RightDelim != "." && !string.IsNullOrEmpty(delim.RightDelim))
                {
                    RenderDelimText(dc, delim.RightDelim, delimCurX, y, fontSize * scale, brush);
                }
                break;

            case MathLayoutEngine.MathMatrixNode mat:
                double mStartX = x + (mat.MatrixType == "cases" ? fontSize * 0.6 : (mat.MatrixType != "matrix" ? fontSize * 0.4 : 0));
                double mCurY = y - mat.Ascent + (fontSize * 0.75);

                if (mat.MatrixType == "pmatrix")
                {
                    RenderDelimText(dc, "(", x, y, mat.Height * 0.95, brush);
                    RenderDelimText(dc, ")", x + mat.Width - (fontSize * 0.35), y, mat.Height * 0.95, brush);
                }
                else if (mat.MatrixType == "bmatrix")
                {
                    RenderDelimText(dc, "[", x, y, mat.Height * 0.95, brush);
                    RenderDelimText(dc, "]", x + mat.Width - (fontSize * 0.35), y, mat.Height * 0.95, brush);
                }
                else if (mat.MatrixType == "cases")
                {
                    RenderDelimText(dc, "{", x, y, mat.Height * 0.95, brush);
                }

                for (int r = 0; r < mat.Rows.Count; r++)
                {
                    var row = mat.Rows[r];
                    double colX = mStartX;

                    for (int c = 0; c < row.Count; c++)
                    {
                        RenderAstNode(dc, row[c], colX, mCurY, fontSize, brush);
                        colX += row[c].Width + (fontSize * 0.5);
                    }

                    mCurY += (mat.Height / Math.Max(1, mat.Rows.Count));
                }
                break;

            case MathLayoutEngine.MathAccentNode acc:
                RenderAstNode(dc, acc.BaseNode, x, y, fontSize, brush);
                double accX = x + (acc.BaseNode.Width / 2);
                double accY = y - acc.BaseNode.Ascent - (fontSize * 0.05);
                string accSym = acc.AccentType switch
                {
                    "hat" => "^",
                    "bar" or "overline" => "¯",
                    "vec" => "→",
                    "dot" => "˙",
                    "ddot" => "¨",
                    "tilde" => "~",
                    _ => "^"
                };
                var accFormatted = new FormattedText(
                    accSym,
                    CultureInfo.InvariantCulture,
                    FlowDirection.LeftToRight,
                    _serifTypeface,
                    fontSize * 0.85,
                    brush
                );
                dc.DrawText(accFormatted, new Point(accX - (accFormatted.Width / 2), accY - (accFormatted.Height * 0.7)));
                break;
        }
    }

    private void RenderDelimText(DrawingContext dc, string delim, double x, double y, double size, IBrush brush)
    {
        string str = delim switch
        {
            "\\{" => "{",
            "\\}" => "}",
            "\\langle" or "langle" => "⟨",
            "\\rangle" or "rangle" => "⟩",
            _ => delim
        };
        var formatted = new FormattedText(
            str,
            CultureInfo.InvariantCulture,
            FlowDirection.LeftToRight,
            _serifTypeface,
            size,
            brush
        );
        dc.DrawText(formatted, new Point(x, y - (size * 0.75)));
    }
}
