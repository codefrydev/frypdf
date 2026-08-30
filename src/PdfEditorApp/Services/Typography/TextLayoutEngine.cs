using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using System.Xml.Linq;
using Avalonia;
using Avalonia.Media;
using PdfEditorApp.Models;
using PdfEditorApp.Models.Elements;

namespace PdfEditorApp.Services.Typography;

public record CurvedGlyphInfo(
    char Character,
    string Text,
    double X,
    double Y,
    double TangentAngleDeg,
    double Width,
    double Height,
    double BaselineOffset
);

public class CurvedLayoutResult
{
    public List<CurvedGlyphInfo> Glyphs { get; set; } = new();
    public Rect BoundingBox { get; set; }
    public double CenterX { get; set; }
    public double CenterY { get; set; }
    public double Radius { get; set; }
    public double StartAngleDeg { get; set; }
    public double SweepAngleDeg { get; set; }
}

public record NormalLayoutLine(
    string Text,
    double X,
    double Y,
    double Width,
    double Height,
    double BaselineY
);

public class NormalLayoutResult
{
    public List<NormalLayoutLine> Lines { get; set; } = new();
    public double TotalWidth { get; set; }
    public double TotalHeight { get; set; }
}

public static class TextLayoutEngine
{
    private static readonly CultureInfo Ci = CultureInfo.InvariantCulture;

    // Approximate character aspect ratio table for fast fallback measurement
    private static double EstimateCharWidth(char c, double fontSize, bool isBold)
    {
        double factor;
        if (char.IsWhiteSpace(c)) factor = 0.30;
        else if ("ijlI1|!.,:;'-`".Contains(c)) factor = 0.28;
        else if ("mwMW@%#&".Contains(c)) factor = 0.85;
        else if (char.IsUpper(c)) factor = 0.65;
        else if ("abcdeghknopqrstuvxyz023456789$?".Contains(c)) factor = 0.54;
        else factor = 0.50;

        if (isBold) factor *= 1.08;
        return factor * fontSize;
    }

    public static double MeasureGlyphWidth(char c, string fontFamily, double fontSize, bool isBold, bool isItalic)
    {
        try
        {
            var avaloniaFamily = FontHelper.CreateFontFamily(fontFamily);
            var typeface = new Typeface(
                avaloniaFamily,
                isItalic ? FontStyle.Italic : FontStyle.Normal,
                isBold ? FontWeight.Bold : FontWeight.Normal);

            var ft = new FormattedText(
                c.ToString(),
                CultureInfo.CurrentCulture,
                FlowDirection.LeftToRight,
                typeface,
                fontSize,
                Brushes.Black);

            return Math.Max(fontSize * 0.2, ft.WidthIncludingTrailingWhitespace);
        }
        catch
        {
            return Math.Max(fontSize * 0.2, EstimateCharWidth(c, fontSize, isBold));
        }
    }

    public static double[] MeasureAllGlyphWidths(string text, string fontFamily, double fontSize, bool isBold, bool isItalic, double charSpacing)
    {
        if (string.IsNullOrEmpty(text)) return Array.Empty<double>();

        var widths = new double[text.Length];
        for (int i = 0; i < text.Length; i++)
        {
            widths[i] = MeasureGlyphWidth(text[i], fontFamily, fontSize, isBold, isItalic) + charSpacing;
        }
        return widths;
    }

    public static NormalLayoutResult CalculateNormalLayout(
        string text,
        string fontFamily,
        double fontSize,
        bool isBold,
        bool isItalic,
        double availableWidth,
        double lineHeightMultiplier,
        double charSpacing,
        double wordSpacing,
        double paragraphSpacing,
        TextAlignmentMode alignment,
        TextVerticalAlignment vAlign,
        double boxHeight,
        bool wrap,
        double padding = 0)
    {
        var result = new NormalLayoutResult();
        if (string.IsNullOrEmpty(text))
        {
            result.TotalHeight = Math.Max(fontSize + (2 * padding), 20);
            result.TotalWidth = Math.Max(40, availableWidth);
            return result;
        }

        double effectiveLineHeight = fontSize * (lineHeightMultiplier > 0.1 ? lineHeightMultiplier : 1.35);
        double usableWidth = Math.Max(20, availableWidth - (2 * padding));

        var paragraphs = text.Replace("\r\n", "\n").Replace('\r', '\n').Split('\n');
        var computedLines = new List<(string LineText, double LineWidth, bool IsParagraphEnd)>();

        foreach (var paragraph in paragraphs)
        {
            if (string.IsNullOrEmpty(paragraph))
            {
                computedLines.Add(("", 0, true));
                continue;
            }

            if (!wrap)
            {
                double w = MeasureStringWidth(paragraph, fontFamily, fontSize, isBold, isItalic, charSpacing, wordSpacing);
                computedLines.Add((paragraph, w, true));
                continue;
            }

            // Word wrap
            var words = paragraph.Split(' ');
            var currentLine = new StringBuilder();
            double currentLineWidth = 0;

            for (int i = 0; i < words.Length; i++)
            {
                var word = words[i];
                double wordWidth = MeasureStringWidth(word, fontFamily, fontSize, isBold, isItalic, charSpacing, 0);
                double spaceWidth = (MeasureGlyphWidth(' ', fontFamily, fontSize, isBold, isItalic) + charSpacing + wordSpacing);

                if (currentLine.Length == 0)
                {
                    currentLine.Append(word);
                    currentLineWidth = wordWidth;
                }
                else if (currentLineWidth + spaceWidth + wordWidth <= usableWidth)
                {
                    currentLine.Append(' ').Append(word);
                    currentLineWidth += spaceWidth + wordWidth;
                }
                else
                {
                    computedLines.Add((currentLine.ToString(), currentLineWidth, false));
                    currentLine.Clear();
                    currentLine.Append(word);
                    currentLineWidth = wordWidth;
                }
            }

            if (currentLine.Length > 0)
            {
                computedLines.Add((currentLine.ToString(), currentLineWidth, true));
            }
        }

        // Calculate heights with paragraph spacing
        double totalTextHeight = 0;
        for (int i = 0; i < computedLines.Count; i++)
        {
            totalTextHeight += effectiveLineHeight;
            if (computedLines[i].IsParagraphEnd && i < computedLines.Count - 1 && paragraphSpacing > 0)
            {
                totalTextHeight += paragraphSpacing;
            }
        }

        double maxLineWidth = computedLines.Count > 0 ? computedLines.Max(l => l.LineWidth) : 0;
        result.TotalWidth = maxLineWidth + (2 * padding);
        result.TotalHeight = totalTextHeight + (2 * padding);

        // Vertical Alignment starting Y
        double startY = padding;
        if (boxHeight > result.TotalHeight)
        {
            if (vAlign == TextVerticalAlignment.Center)
            {
                startY = padding + ((boxHeight - result.TotalHeight) / 2.0);
            }
            else if (vAlign == TextVerticalAlignment.Bottom)
            {
                startY = boxHeight - result.TotalHeight + padding;
            }
        }

        double currentY = startY;
        for (int i = 0; i < computedLines.Count; i++)
        {
            var line = computedLines[i];
            double lineX = padding;

            if (alignment == TextAlignmentMode.Center)
            {
                lineX = padding + Math.Max(0, (usableWidth - line.LineWidth) / 2.0);
            }
            else if (alignment == TextAlignmentMode.Right)
            {
                lineX = padding + Math.Max(0, usableWidth - line.LineWidth);
            }

            double baselineY = currentY + (fontSize * 0.85);
            result.Lines.Add(new NormalLayoutLine(line.LineText, lineX, currentY, line.LineWidth, effectiveLineHeight, baselineY));

            currentY += effectiveLineHeight;
            if (line.IsParagraphEnd && i < computedLines.Count - 1 && paragraphSpacing > 0)
            {
                currentY += paragraphSpacing;
            }
        }

        return result;
    }

    public static double MeasureStringWidth(string text, string fontFamily, double fontSize, bool isBold, bool isItalic, double charSpacing, double wordSpacing)
    {
        if (string.IsNullOrEmpty(text)) return 0;

        double total = 0;
        for (int i = 0; i < text.Length; i++)
        {
            char c = text[i];
            total += MeasureGlyphWidth(c, fontFamily, fontSize, isBold, isItalic) + charSpacing;
            if (c == ' ' && wordSpacing != 0) total += wordSpacing;
        }
        return Math.Max(0, total);
    }

    public static CurvedLayoutResult CalculateCurvedGlyphs(
        string text,
        string fontFamily,
        double fontSize,
        bool isBold,
        bool isItalic,
        double boxWidth,
        double boxHeight,
        double radius,
        double arcAngleDeg,
        double startAngleDeg,
        bool clockwise,
        bool invert,
        double charSpacing,
        CircularTextPlacement circularPlacement = CircularTextPlacement.TopArc,
        TextShapeMode shapeMode = TextShapeMode.Curved,
        double baselineShift = 0)
    {
        var result = new CurvedLayoutResult();
        if (string.IsNullOrEmpty(text)) return result;

        double w = Math.Max(40, boxWidth);
        double h = Math.Max(40, boxHeight);
        double cx = w / 2.0;
        double cy = h / 2.0;

        // Determine effective radius & angles based on mode
        double effectiveRadius = Math.Max(15, radius);

        if (shapeMode == TextShapeMode.Circular)
        {
            // Auto fit radius to box if needed
            if (radius <= 0 || double.IsNaN(radius))
            {
                effectiveRadius = Math.Max(20, Math.Min(w, h) / 2.0 - fontSize);
            }

            switch (circularPlacement)
            {
                case CircularTextPlacement.TopArc:
                    startAngleDeg = -90 + startAngleDeg; // -90 is Top (12 o'clock)
                    arcAngleDeg = Math.Clamp(arcAngleDeg > 0 ? arcAngleDeg : 180, 10, 360);
                    break;
                case CircularTextPlacement.BottomArc:
                    startAngleDeg = 90 + startAngleDeg; // 90 is Bottom (6 o'clock)
                    arcAngleDeg = Math.Clamp(arcAngleDeg > 0 ? arcAngleDeg : 180, 10, 360);
                    break;
                case CircularTextPlacement.FullCircle:
                    startAngleDeg = -90 + startAngleDeg;
                    arcAngleDeg = 360;
                    break;
                case CircularTextPlacement.CustomArc:
                    break;
            }
        }
        else // Curved / Arch mode
        {
            // Center of curvature
            if (radius <= 0)
            {
                effectiveRadius = Math.Max(30, (w * w + 4 * h * h) / (8 * Math.Max(10, h)));
            }

            if (clockwise)
            {
                // Arch up: center is below the text box
                cy = (h / 2.0) + (effectiveRadius * 0.7);
                startAngleDeg = -90 + startAngleDeg;
            }
            else
            {
                // Arch down: center is above the text box
                cy = (h / 2.0) - (effectiveRadius * 0.7);
                startAngleDeg = 90 + startAngleDeg;
            }
        }

        result.CenterX = cx;
        result.CenterY = cy;
        result.Radius = effectiveRadius;
        result.StartAngleDeg = startAngleDeg;
        result.SweepAngleDeg = arcAngleDeg;

        // Measure all glyphs
        var glyphWidths = MeasureAllGlyphWidths(text, fontFamily, fontSize, isBold, isItalic, charSpacing);
        double totalTextWidth = glyphWidths.Sum();
        if (totalTextWidth <= 0) return result;

        // Angular progression per character: deltaAngle_rad = width / radius
        double[] glyphAnglesRad = new double[text.Length];
        double totalAngularSpanRad = 0;

        for (int i = 0; i < text.Length; i++)
        {
            double dAngle = glyphWidths[i] / effectiveRadius;
            glyphAnglesRad[i] = dAngle;
            totalAngularSpanRad += dAngle;
        }

        // Limit or expand to specified arc angle if arcAngleDeg is set strictly
        double maxArcRad = (arcAngleDeg * Math.PI) / 180.0;
        double scaleFactor = 1.0;
        if (shapeMode == TextShapeMode.Circular && circularPlacement == CircularTextPlacement.FullCircle)
        {
            scaleFactor = (2.0 * Math.PI) / Math.Max(0.01, totalAngularSpanRad);
        }
        else if (totalAngularSpanRad > maxArcRad && maxArcRad > 0.05)
        {
            scaleFactor = maxArcRad / totalAngularSpanRad;
        }

        double startRad = (startAngleDeg * Math.PI) / 180.0;
        // Center the text symmetrically around the start angle
        double effectiveTotalSpanRad = totalAngularSpanRad * scaleFactor;
        double currentAngleRad = startRad - (effectiveTotalSpanRad / 2.0);

        double minX = double.MaxValue, minY = double.MaxValue, maxX = double.MinValue, maxY = double.MinValue;

        double rEffective = effectiveRadius + baselineShift;

        for (int i = 0; i < text.Length; i++)
        {
            char c = text[i];
            double charAngleSpan = glyphAnglesRad[i] * scaleFactor;
            double midCharAngle = currentAngleRad + (charAngleSpan / 2.0);

            // Compute (x, y) on circle
            double gx = cx + (rEffective * Math.Cos(midCharAngle));
            double gy = cy + (rEffective * Math.Sin(midCharAngle));

            // Tangent angle in degrees:
            // Tangent direction is perpendicular to the radial vector
            double tangentRad = midCharAngle + (Math.PI / 2.0);
            if (!clockwise || invert)
            {
                tangentRad += Math.PI; // Flip orientation 180 degrees
            }

            double tangentDeg = (tangentRad * 180.0) / Math.PI;

            var glyphInfo = new CurvedGlyphInfo(
                Character: c,
                Text: c.ToString(),
                X: gx,
                Y: gy,
                TangentAngleDeg: tangentDeg,
                Width: glyphWidths[i],
                Height: fontSize,
                BaselineOffset: fontSize * 0.35
            );

            result.Glyphs.Add(glyphInfo);

            // Update bounds
            double halfW = glyphWidths[i] / 2.0;
            double halfH = fontSize / 2.0;
            minX = Math.Min(minX, gx - halfW);
            maxX = Math.Max(maxX, gx + halfW);
            minY = Math.Min(minY, gy - halfH);
            maxY = Math.Max(maxY, gy + halfH);

            currentAngleRad += charAngleSpan;
        }

        if (result.Glyphs.Count > 0)
        {
            result.BoundingBox = new Rect(minX, minY, Math.Max(10, maxX - minX), Math.Max(10, maxY - minY));
        }
        else
        {
            result.BoundingBox = new Rect(0, 0, w, h);
        }

        return result;
    }

    public static (Point P0, Point P1, Point P2, Point P3) GetPresetBezierControlPoints(BezierCurvePreset preset)
    {
        return preset switch
        {
            BezierCurvePreset.Wave => (new Point(0.0, 0.5), new Point(0.3, 0.1), new Point(0.7, 0.9), new Point(1.0, 0.5)),
            BezierCurvePreset.SCurve => (new Point(0.05, 0.8), new Point(0.35, 0.05), new Point(0.65, 0.95), new Point(0.95, 0.2)),
            BezierCurvePreset.Bridge => (new Point(0.05, 0.85), new Point(0.3, 0.15), new Point(0.7, 0.15), new Point(0.95, 0.85)),
            BezierCurvePreset.Valley => (new Point(0.05, 0.15), new Point(0.3, 0.85), new Point(0.7, 0.85), new Point(0.95, 0.15)),
            BezierCurvePreset.Rise => (new Point(0.05, 0.85), new Point(0.35, 0.7), new Point(0.65, 0.3), new Point(0.95, 0.15)),
            _ => (new Point(0.0, 0.5), new Point(0.33, 0.1), new Point(0.67, 0.9), new Point(1.0, 0.5))
        };
    }

    public static CurvedLayoutResult CalculateBezierGlyphs(
        string text,
        string fontFamily,
        double fontSize,
        bool isBold,
        bool isItalic,
        double boxWidth,
        double boxHeight,
        Point p0Norm,
        Point p1Norm,
        Point p2Norm,
        Point p3Norm,
        bool invert = false,
        double charSpacing = 0,
        double baselineShift = 0)
    {
        var result = new CurvedLayoutResult();
        if (string.IsNullOrEmpty(text)) return result;

        double w = Math.Max(20, boxWidth);
        double h = Math.Max(20, boxHeight);

        // Absolute control points
        Point p0 = new Point(p0Norm.X * w, p0Norm.Y * h);
        Point p1 = new Point(p1Norm.X * w, p1Norm.Y * h);
        Point p2 = new Point(p2Norm.X * w, p2Norm.Y * h);
        Point p3 = new Point(p3Norm.X * w, p3Norm.Y * h);

        // Cubic Bézier point evaluation
        Point EvalBezier(double t)
        {
            double u = 1.0 - t;
            double tt = t * t;
            double uu = u * u;
            double uuu = uu * u;
            double ttt = tt * t;

            double x = uuu * p0.X + 3 * uu * t * p1.X + 3 * u * tt * p2.X + ttt * p3.X;
            double y = uuu * p0.Y + 3 * uu * t * p1.Y + 3 * u * tt * p2.Y + ttt * p3.Y;
            return new Point(x, y);
        }

        // Cubic Bézier tangent derivative evaluation
        Point EvalBezierDerivative(double t)
        {
            double u = 1.0 - t;
            double uu = u * u;
            double tt = t * t;
            double ut = u * t;

            double dx = 3 * uu * (p1.X - p0.X) + 6 * ut * (p2.X - p1.X) + 3 * tt * (p3.X - p2.X);
            double dy = 3 * uu * (p1.Y - p0.Y) + 6 * ut * (p2.Y - p1.Y) + 3 * tt * (p3.Y - p2.Y);
            return new Point(dx, dy);
        }

        // Discretize for accurate arc-length lookup table
        const int Samples = 250;
        double[] arcLengths = new double[Samples + 1];
        Point prevPt = EvalBezier(0.0);
        arcLengths[0] = 0.0;

        for (int i = 1; i <= Samples; i++)
        {
            double t = (double)i / Samples;
            Point pt = EvalBezier(t);
            double segDist = Math.Sqrt((pt.X - prevPt.X) * (pt.X - prevPt.X) + (pt.Y - prevPt.Y) * (pt.Y - prevPt.Y));
            arcLengths[i] = arcLengths[i - 1] + segDist;
            prevPt = pt;
        }

        double totalArcLength = arcLengths[Samples];
        if (totalArcLength <= 0.001) return result;

        // Function to find t for given distance s
        double GetTForDistance(double s)
        {
            s = Math.Clamp(s, 0.0, totalArcLength);
            int low = 0, high = Samples;
            while (low < high - 1)
            {
                int mid = (low + high) / 2;
                if (arcLengths[mid] <= s) low = mid;
                else high = mid;
            }

            double segmentLen = arcLengths[high] - arcLengths[low];
            double frac = segmentLen > 0.00001 ? (s - arcLengths[low]) / segmentLen : 0;
            double tLow = (double)low / Samples;
            double tHigh = (double)high / Samples;
            return tLow + frac * (tHigh - tLow);
        }

        var glyphWidths = MeasureAllGlyphWidths(text, fontFamily, fontSize, isBold, isItalic, charSpacing);
        double totalTextWidth = glyphWidths.Sum();

        double startDist = 0;
        double stepScale = 1.0;

        if (totalTextWidth < totalArcLength)
        {
            startDist = (totalArcLength - totalTextWidth) / 2.0;
        }
        else if (totalTextWidth > totalArcLength && totalTextWidth > 0.01)
        {
            stepScale = totalArcLength / totalTextWidth;
        }

        double currentDist = startDist;
        double minX = double.MaxValue, minY = double.MaxValue, maxX = double.MinValue, maxY = double.MinValue;

        for (int i = 0; i < text.Length; i++)
        {
            char c = text[i];
            double gWidth = glyphWidths[i] * stepScale;
            double midDist = currentDist + (gWidth / 2.0);
            double t = GetTForDistance(midDist);

            Point pt = EvalBezier(t);
            Point deriv = EvalBezierDerivative(t);

            double tangentAngleRad = Math.Atan2(deriv.Y, deriv.X);
            double tangentAngleDeg = (tangentAngleRad * 180.0) / Math.PI;

            if (invert)
            {
                tangentAngleDeg += 180.0;
            }

            double gx = pt.X;
            double gy = pt.Y;

            if (Math.Abs(baselineShift) > 0.001)
            {
                double normAngleRad = tangentAngleRad + (invert ? -Math.PI / 2.0 : Math.PI / 2.0);
                gx += baselineShift * Math.Cos(normAngleRad);
                gy += baselineShift * Math.Sin(normAngleRad);
            }

            var glyphInfo = new CurvedGlyphInfo(
                Character: c,
                Text: c.ToString(),
                X: gx,
                Y: gy,
                TangentAngleDeg: tangentAngleDeg,
                Width: glyphWidths[i],
                Height: fontSize,
                BaselineOffset: fontSize * 0.35
            );

            result.Glyphs.Add(glyphInfo);

            double halfW = glyphWidths[i] / 2.0;
            double halfH = fontSize / 2.0;
            minX = Math.Min(minX, gx - halfW);
            maxX = Math.Max(maxX, gx + halfW);
            minY = Math.Min(minY, gy - halfH);
            maxY = Math.Max(maxY, gy + halfH);

            currentDist += gWidth;
        }

        if (result.Glyphs.Count > 0)
        {
            result.BoundingBox = new Rect(minX, minY, Math.Max(10, maxX - minX), Math.Max(10, maxY - minY));
        }
        else
        {
            result.BoundingBox = new Rect(0, 0, w, h);
        }

        return result;
    }

    public static Size CalculateRequiredDimensions(PdfTextElement el)
    {
        if (el.ShapeMode == TextShapeMode.Normal)
        {
            var normalLayout = CalculateNormalLayout(
                el.Text,
                el.FontFamily,
                el.FontSize,
                el.IsBold,
                el.IsItalic,
                el.Width,
                el.LineHeight,
                el.CharacterSpacing,
                el.WordSpacing,
                el.ParagraphSpacing,
                el.Alignment,
                el.VerticalAlignment,
                el.Height,
                el.TextWrap,
                el.Padding
            );

            return new Size(
                Math.Max(30, Math.Ceiling(normalLayout.TotalWidth)),
                Math.Max(20, Math.Ceiling(normalLayout.TotalHeight))
            );
        }
        else if (el.ShapeMode == TextShapeMode.BezierCurve)
        {
            var bezierLayout = CalculateBezierGlyphs(
                el.Text,
                el.FontFamily,
                el.FontSize,
                el.IsBold,
                el.IsItalic,
                el.Width,
                el.Height,
                new Point(el.BezierP0X, el.BezierP0Y),
                new Point(el.BezierP1X, el.BezierP1Y),
                new Point(el.BezierP2X, el.BezierP2Y),
                new Point(el.BezierP3X, el.BezierP3Y),
                el.CurveInvert,
                el.CharacterSpacing,
                el.BaselineShift
            );

            double w = Math.Max(40, bezierLayout.BoundingBox.Width + (2 * el.Padding) + (2 * el.BorderThickness) + 10);
            double h = Math.Max(30, bezierLayout.BoundingBox.Height + (2 * el.Padding) + (2 * el.BorderThickness) + 10);
            return new Size(Math.Ceiling(w), Math.Ceiling(h));
        }
        else
        {
            var curvedLayout = CalculateCurvedGlyphs(
                el.Text,
                el.FontFamily,
                el.FontSize,
                el.IsBold,
                el.IsItalic,
                el.Width,
                el.Height,
                el.CurveRadius,
                el.CurveArcAngle,
                el.CurveStartAngle,
                el.CurveClockwise,
                el.CurveInvert,
                el.CharacterSpacing,
                el.CircularPlacement,
                el.ShapeMode,
                el.BaselineShift
            );

            double w = Math.Max(40, curvedLayout.BoundingBox.Width + (2 * el.Padding) + (2 * el.BorderThickness) + 10);
            double h = Math.Max(30, curvedLayout.BoundingBox.Height + (2 * el.Padding) + (2 * el.BorderThickness) + 10);
            return new Size(Math.Ceiling(w), Math.Ceiling(h));
        }
    }

    public static string GenerateSvgMarkup(PdfTextElement el)
    {
        double w = Math.Max(10, el.Width);
        double h = Math.Max(10, el.Height);

        var sb = new StringBuilder();
        sb.AppendFormat(Ci, "<svg xmlns=\"http://www.w3.org/2000/svg\" viewBox=\"0 0 {0:F1} {1:F1}\" width=\"{0:F1}\" height=\"{1:F1}\">", w, h);

        // Definitions: Shadow Filter & Gradients
        sb.Append("<defs>");
        if (el.HasShadow && el.ShadowOpacity > 0.01)
        {
            string shadowColor = !string.IsNullOrEmpty(el.ShadowColorHex) ? el.ShadowColorHex : "#80000000";
            sb.AppendFormat(Ci, "<filter id=\"dropShadow\" x=\"-30%\" y=\"-30%\" width=\"160%\" height=\"160%\">" +
                                "<feDropShadow dx=\"{0:F1}\" dy=\"{1:F1}\" stdDeviation=\"{2:F1}\" flood-color=\"{3}\" flood-opacity=\"{4:F2}\" />" +
                                "</filter>",
                el.ShadowOffsetX, el.ShadowOffsetY, Math.Max(0.5, el.ShadowBlurRadius / 2.0), shadowColor, el.ShadowOpacity);
        }
        sb.Append("</defs>");

        // Background Box (if present)
        bool hasBg = !string.IsNullOrEmpty(el.BackgroundColorHex) &&
                     el.BackgroundColorHex != "#00000000" &&
                     !el.BackgroundColorHex.Equals("Transparent", StringComparison.OrdinalIgnoreCase);

        bool hasBorder = el.BorderThickness > 0 &&
                         !string.IsNullOrEmpty(el.BorderColorHex) &&
                         el.BorderColorHex != "#00000000" &&
                         !el.BorderColorHex.Equals("Transparent", StringComparison.OrdinalIgnoreCase);

        if (hasBg || hasBorder)
        {
            double pad = Math.Max(0, el.Padding / 2.0);
            double rw = Math.Max(2, w - (2 * pad));
            double rh = Math.Max(2, h - (2 * pad));
            string fill = hasBg ? el.BackgroundColorHex : "none";
            string stroke = hasBorder ? el.BorderColorHex : "none";
            double strkW = hasBorder ? el.BorderThickness : 0;
            double cr = Math.Max(0, el.CornerRadius);

            sb.AppendFormat(Ci, "<rect x=\"{0:F1}\" y=\"{1:F1}\" width=\"{2:F1}\" height=\"{3:F1}\" rx=\"{4:F1}\" ry=\"{4:F1}\" fill=\"{5}\" stroke=\"{6}\" stroke-width=\"{7:F1}\" />",
                pad, pad, rw, rh, cr, fill, stroke, strkW);
        }

        // Main Text / Glyph Group
        sb.Append("<g");
        if (el.Opacity < 0.99)
        {
            sb.AppendFormat(Ci, " opacity=\"{0:F2}\"", el.Opacity);
        }
        if (el.HasShadow)
        {
            sb.Append(" filter=\"url(#dropShadow)\"");
        }
        sb.Append(">");

        string fontWeight = el.IsBold ? "bold" : "normal";
        string fontStyle = el.IsItalic ? "italic" : "normal";
        string fontColor = !string.IsNullOrEmpty(el.TextColorHex) ? el.TextColorHex : "#201F1E";
        string fontFamily = !string.IsNullOrEmpty(el.FontFamily) ? el.FontFamily : "Arial";

        string strokeAttr = "";
        if (el.HasStroke && el.StrokeWidth > 0 && !string.IsNullOrEmpty(el.StrokeColorHex) && el.StrokeColorHex != "#00000000")
        {
            strokeAttr = string.Format(Ci, " stroke=\"{0}\" stroke-width=\"{1:F1}\" paint-order=\"stroke fill\"", el.StrokeColorHex, el.StrokeWidth);
        }

        if (el.ShapeMode == TextShapeMode.Normal)
        {
            var normalLayout = CalculateNormalLayout(
                el.Text,
                el.FontFamily,
                el.FontSize,
                el.IsBold,
                el.IsItalic,
                el.Width,
                el.LineHeight,
                el.CharacterSpacing,
                el.WordSpacing,
                el.ParagraphSpacing,
                el.Alignment,
                el.VerticalAlignment,
                el.Height,
                el.TextWrap,
                el.Padding
            );

            foreach (var line in normalLayout.Lines)
            {
                if (string.IsNullOrEmpty(line.Text)) continue;

                string textAnchor = "start";
                double tx = line.X;
                if (el.Alignment == TextAlignmentMode.Center)
                {
                    textAnchor = "middle";
                    tx = line.X + (line.Width / 2.0);
                }
                else if (el.Alignment == TextAlignmentMode.Right)
                {
                    textAnchor = "end";
                    tx = line.X + line.Width;
                }

                string escText = EscapeXml(line.Text);
                sb.AppendFormat(Ci, "<text x=\"{0:F1}\" y=\"{1:F1}\" font-family=\"{2}\" font-size=\"{3:F1}\" font-weight=\"{4}\" font-style=\"{5}\" fill=\"{6}\" text-anchor=\"{7}\"{8}",
                    tx, line.BaselineY, fontFamily, el.FontSize, fontWeight, fontStyle, fontColor, textAnchor, strokeAttr);

                if (el.CharacterSpacing != 0)
                {
                    sb.AppendFormat(Ci, " letter-spacing=\"{0:F1}px\"", el.CharacterSpacing);
                }
                if (el.WordSpacing != 0)
                {
                    sb.AppendFormat(Ci, " word-spacing=\"{0:F1}px\"", el.WordSpacing);
                }

                // Text decorations
                if (el.IsUnderline && el.IsStrikethrough) sb.Append(" text-decoration=\"underline line-through\"");
                else if (el.IsUnderline) sb.Append(" text-decoration=\"underline\"");
                else if (el.IsStrikethrough) sb.Append(" text-decoration=\"line-through\"");

                sb.AppendFormat(">{0}</text>", escText);

                // Double underline vector line if requested
                if (el.IsDoubleUnderline)
                {
                    double uY1 = line.BaselineY + 2;
                    double uY2 = line.BaselineY + 5;
                    sb.AppendFormat(Ci, "<line x1=\"{0:F1}\" y1=\"{1:F1}\" x2=\"{2:F1}\" y2=\"{1:F1}\" stroke=\"{3}\" stroke-width=\"1\" />", line.X, uY1, line.X + line.Width, fontColor);
                    sb.AppendFormat(Ci, "<line x1=\"{0:F1}\" y1=\"{1:F1}\" x2=\"{2:F1}\" y2=\"{1:F1}\" stroke=\"{3}\" stroke-width=\"1\" />", line.X, uY2, line.X + line.Width, fontColor);
                }
            }
        }
        else if (el.ShapeMode == TextShapeMode.BezierCurve)
        {
            var bezierLayout = CalculateBezierGlyphs(
                el.Text,
                el.FontFamily,
                el.FontSize,
                el.IsBold,
                el.IsItalic,
                el.Width,
                el.Height,
                new Point(el.BezierP0X, el.BezierP0Y),
                new Point(el.BezierP1X, el.BezierP1Y),
                new Point(el.BezierP2X, el.BezierP2Y),
                new Point(el.BezierP3X, el.BezierP3Y),
                el.CurveInvert,
                el.CharacterSpacing,
                el.BaselineShift
            );

            foreach (var g in bezierLayout.Glyphs)
            {
                string escChar = EscapeXml(g.Text);
                sb.AppendFormat(Ci, "<text x=\"{0:F1}\" y=\"{1:F1}\" font-family=\"{2}\" font-size=\"{3:F1}\" font-weight=\"{4}\" font-style=\"{5}\" fill=\"{6}\" text-anchor=\"middle\" dominant-baseline=\"central\" transform=\"rotate({7:F1} {0:F1} {1:F1})\"{8}",
                    g.X, g.Y, fontFamily, el.FontSize, fontWeight, fontStyle, fontColor, g.TangentAngleDeg + el.CharacterRotation, strokeAttr);

                if (el.IsUnderline) sb.Append(" text-decoration=\"underline\"");
                else if (el.IsStrikethrough) sb.Append(" text-decoration=\"line-through\"");

                sb.AppendFormat(">{0}</text>", escChar);
            }
        }
        else // Curved or Circular Mode
        {
            var curvedLayout = CalculateCurvedGlyphs(
                el.Text,
                el.FontFamily,
                el.FontSize,
                el.IsBold,
                el.IsItalic,
                el.Width,
                el.Height,
                el.CurveRadius,
                el.CurveArcAngle,
                el.CurveStartAngle,
                el.CurveClockwise,
                el.CurveInvert,
                el.CharacterSpacing,
                el.CircularPlacement,
                el.ShapeMode,
                el.BaselineShift
            );

            foreach (var g in curvedLayout.Glyphs)
            {
                string escChar = EscapeXml(g.Text);
                sb.AppendFormat(Ci, "<text x=\"{0:F1}\" y=\"{1:F1}\" font-family=\"{2}\" font-size=\"{3:F1}\" font-weight=\"{4}\" font-style=\"{5}\" fill=\"{6}\" text-anchor=\"middle\" dominant-baseline=\"central\" transform=\"rotate({7:F1} {0:F1} {1:F1})\"{8}",
                    g.X, g.Y, fontFamily, el.FontSize, fontWeight, fontStyle, fontColor, g.TangentAngleDeg + el.CharacterRotation, strokeAttr);

                if (el.IsUnderline) sb.Append(" text-decoration=\"underline\"");
                else if (el.IsStrikethrough) sb.Append(" text-decoration=\"line-through\"");

                sb.AppendFormat(">{0}</text>", escChar);
            }
        }

        sb.Append("</g>");
        sb.Append("</svg>");

        return sb.ToString();
    }

    private static string EscapeXml(string unescaped)
    {
        if (string.IsNullOrEmpty(unescaped)) return "";
        return unescaped
            .Replace("&", "&amp;")
            .Replace("<", "&lt;")
            .Replace(">", "&gt;")
            .Replace("\"", "&quot;")
            .Replace("'", "&apos;");
    }
}
