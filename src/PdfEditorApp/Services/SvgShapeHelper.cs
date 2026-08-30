using System;
using System.Globalization;
using System.Text;
using PdfEditorApp.Models;
using PdfEditorApp.Models.Elements;

namespace PdfEditorApp.Services;

public static class SvgShapeHelper
{
    public static string GetVectorPath(ShapeType shapeType, double width, double height, double cornerRadius = 4, string? customPathData = null)
    {
        if (shapeType == ShapeType.CustomSvgPath && !string.IsNullOrWhiteSpace(customPathData))
        {
            return customPathData.Trim();
        }

        double w = Math.Max(10, width);
        double h = Math.Max(10, height);
        double r = Math.Min(cornerRadius > 0 ? cornerRadius : 8, Math.Min(w / 2, h / 2));

        var ci = CultureInfo.InvariantCulture;

        return shapeType switch
        {
            ShapeType.RoundedRectangle =>
                string.Format(ci, "M {0:F1},0 L {1:F1},0 A {0:F1},{0:F1} 0 0 1 {2:F1},{0:F1} L {2:F1},{3:F1} A {0:F1},{0:F1} 0 0 1 {1:F1},{4:F1} L {0:F1},{4:F1} A {0:F1},{0:F1} 0 0 1 0,{3:F1} L 0,{0:F1} A {0:F1},{0:F1} 0 0 1 {0:F1},0 Z",
                    r, w - r, w, h - r, h),

            ShapeType.Circle =>
                string.Format(ci, "M {0:F1},0 A {0:F1},{1:F1} 0 1 1 {0:F1},{2:F1} A {0:F1},{1:F1} 0 1 1 {0:F1},0 Z",
                    w / 2.0, h / 2.0, h),

            ShapeType.Triangle =>
                string.Format(ci, "M {0:F1},0 L {1:F1},{2:F1} L 0,{2:F1} Z",
                    w / 2.0, w, h),

            ShapeType.RightTriangle =>
                string.Format(ci, "M 0,0 L {0:F1},{1:F1} L 0,{1:F1} Z",
                    w, h),

            ShapeType.Diamond =>
                string.Format(ci, "M {0:F1},0 L {1:F1},{2:F1} L {0:F1},{3:F1} L 0,{2:F1} Z",
                    w / 2.0, w, h / 2.0, h),

            ShapeType.Pentagon =>
                string.Format(ci, "M {0:F1},0 L {1:F1},{2:F1} L {3:F1},{4:F1} L {5:F1},{4:F1} L 0,{2:F1} Z",
                    w / 2.0, w, h * 0.38, w * 0.81, h, w * 0.19),

            ShapeType.Hexagon =>
                string.Format(ci, "M {0:F1},0 L {1:F1},0 L {2:F1},{3:F1} L {1:F1},{4:F1} L {0:F1},{4:F1} L 0,{3:F1} Z",
                    w * 0.25, w * 0.75, w, h / 2.0, h),

            ShapeType.Octagon =>
                string.Format(ci, "M {0:F1},0 L {1:F1},0 L {2:F1},{3:F1} L {2:F1},{4:F1} L {1:F1},{5:F1} L {0:F1},{5:F1} L 0,{4:F1} L 0,{3:F1} Z",
                    w * 0.3, w * 0.7, w, h * 0.3, h * 0.7, h),

            ShapeType.Star5 or ShapeType.Star =>
                string.Format(ci, "M {0:F1},0 L {1:F1},{2:F1} L {3:F1},{2:F1} L {4:F1},{5:F1} L {6:F1},{7:F1} L {0:F1},{8:F1} L {9:F1},{7:F1} L {10:F1},{5:F1} L 0,{2:F1} L {11:F1},{2:F1} Z",
                    w * 0.5, w * 0.62, h * 0.35, w, w * 0.69, h * 0.57, w * 0.81, h, h * 0.75, w * 0.19, w * 0.31, w * 0.38),

            ShapeType.Star4Badge =>
                string.Format(ci, "M {0:F1},0 L {1:F1},{2:F1} L {3:F1},{4:F1} L {1:F1},{5:F1} L {0:F1},{6:F1} L {7:F1},{5:F1} L 0,{4:F1} L {7:F1},{2:F1} Z",
                    w * 0.5, w * 0.65, h * 0.35, w, h * 0.5, h * 0.65, h, w * 0.35),

            ShapeType.Star8Badge =>
                GenerateStarPath(w, h, 8, 0.6),

            ShapeType.Star12Seal or ShapeType.RosetteSeal =>
                GenerateStarPath(w, h, 16, 0.78),

            ShapeType.ShieldBadge =>
                string.Format(ci, "M 0,0 L {0:F1},0 L {0:F1},{1:F1} Q {0:F1},{2:F1} {3:F1},{2:F1} Q 0,{2:F1} 0,{1:F1} Z",
                    w, h * 0.6, h, w / 2.0),

            ShapeType.AwardBadge =>
                GenerateStarPath(w, h, 12, 0.72),

            ShapeType.LaurelWreathSeal =>
                string.Format(ci, "M {0:F1},0 A {0:F1},{1:F1} 0 1 1 {0:F1},{2:F1} A {0:F1},{1:F1} 0 1 1 {0:F1},0 Z M {0:F1},{3:F1} A {4:F1},{5:F1} 0 1 0 {0:F1},{6:F1} A {4:F1},{5:F1} 0 1 0 {0:F1},{3:F1} Z",
                    w / 2.0, h / 2.0, h, h * 0.12, (w * 0.38), (h * 0.38), h * 0.88),

            ShapeType.MedalRibbonBadge =>
                string.Format(ci, "M {0:F1},0 A {0:F1},{1:F1} 0 1 1 {0:F1},{2:F1} A {0:F1},{1:F1} 0 1 1 {0:F1},0 Z M {3:F1},{4:F1} L {5:F1},{6:F1} L {7:F1},{8:F1} L {9:F1},{4:F1} Z M {10:F1},{4:F1} L {11:F1},{8:F1} L {12:F1},{6:F1} L {13:F1},{4:F1} Z",
                    w / 2.0, h * 0.35, h * 0.7,
                    w * 0.25, h * 0.55, w * 0.12, h, w * 0.32, h * 0.88, w * 0.42,
                    w * 0.58, w * 0.68, w * 0.88, w * 0.75),

            ShapeType.RibbonBanner =>
                string.Format(ci, "M 0,0 L {0:F1},0 L {1:F1},{2:F1} L {0:F1},{3:F1} L 0,{3:F1} L {4:F1},{2:F1} Z",
                    w, w - 18.0, h / 2.0, h, 18.0),

            ShapeType.CornerPolygonalAccentTopLeft =>
                string.Format(ci, "M 0,0 L {0:F1},0 L {1:F1},{2:F1} L 0,{3:F1} Z",
                    w, w * 0.32, h * 0.48, h),

            ShapeType.CornerPolygonalAccentBottomRight =>
                string.Format(ci, "M {0:F1},{1:F1} L 0,{1:F1} L {2:F1},{3:F1} L {0:F1},0 Z",
                    w, h, w * 0.68, h * 0.52),

            ShapeType.CornerDiagonalWedge =>
                string.Format(ci, "M 0,0 L {0:F1},0 L 0,{1:F1} Z",
                    w, h),

            ShapeType.Chevron =>
                string.Format(ci, "M 0,0 L {0:F1},0 L {1:F1},{2:F1} L {0:F1},{3:F1} L 0,{3:F1} L {4:F1},{2:F1} Z",
                    w * 0.7, w, h / 2.0, h, w * 0.3),

            ShapeType.Trapezoid =>
                string.Format(ci, "M {0:F1},0 L {1:F1},0 L {2:F1},{3:F1} L 0,{3:F1} Z",
                    w * 0.2, w * 0.8, w, h),

            ShapeType.Parallelogram =>
                string.Format(ci, "M {0:F1},0 L {1:F1},0 L {2:F1},{3:F1} L 0,{3:F1} Z",
                    w * 0.25, w, w * 0.75, h),

            ShapeType.ArrowRight or ShapeType.Arrow =>
                string.Format(ci, "M 0,{0:F1} L {1:F1},{0:F1} L {1:F1},0 L {2:F1},{3:F1} L {1:F1},{4:F1} L {1:F1},{5:F1} L 0,{5:F1} Z",
                    h * 0.3, w * 0.6, w, h * 0.5, h, h * 0.7),

            ShapeType.ArrowLeft =>
                string.Format(ci, "M {0:F1},{1:F1} L {2:F1},{1:F1} L {2:F1},0 L 0,{3:F1} L {2:F1},{4:F1} L {2:F1},{5:F1} L {0:F1},{5:F1} Z",
                    w, h * 0.3, w * 0.4, h * 0.5, h, h * 0.7),

            ShapeType.Callout =>
                string.Format(ci, "M 0,0 L {0:F1},0 L {0:F1},{1:F1} L {2:F1},{1:F1} L {3:F1},{4:F1} L {5:F1},{1:F1} L 0,{1:F1} Z",
                    w, h * 0.75, w * 0.55, w * 0.3, h, w * 0.35),

            ShapeType.Heart =>
                string.Format(ci, "M {0:F1},{1:F1} C {2:F1},0 0,{3:F1} 0,{4:F1} C 0,{5:F1} {2:F1},{6:F1} {0:F1},{7:F1} C {8:F1},{6:F1} {9:F1},{5:F1} {9:F1},{4:F1} C {9:F1},{3:F1} {8:F1},0 {0:F1},{1:F1} Z",
                    w * 0.5, h * 0.25, w * 0.3, h * 0.2, h * 0.45, h * 0.7, h * 0.85, h, w * 0.7, w),

            ShapeType.Cloud =>
                string.Format(ci, "M {0:F1},{1:F1} A {2:F1},{3:F1} 0 0 1 {4:F1},{5:F1} A {6:F1},{5:F1} 0 0 1 {7:F1},{8:F1} A {9:F1},{10:F1} 0 0 1 {11:F1},{1:F1} Z",
                    w * 0.2, h * 0.7, w * 0.15, h * 0.2, w * 0.35, h * 0.3, w * 0.25, w * 0.75, h * 0.35, w * 0.18, h * 0.22, w * 0.9),

            ShapeType.Line =>
                string.Format(ci, "M 0,{0:F1} L {1:F1},{0:F1}",
                    h / 2.0, w),

            ShapeType.BezierCurve =>
                string.Format(ci, "M 0,{0:F1} C {1:F1},0 {2:F1},{3:F1} {4:F1},{0:F1}",
                    h * 0.5, w * 0.33, w * 0.67, h, w),

            ShapeType.CurvedArrow =>
                string.Format(ci, "M 0,{0:F1} C {1:F1},0 {2:F1},{3:F1} {4:F1},{0:F1} M {5:F1},{6:F1} L {4:F1},{0:F1} L {7:F1},{8:F1}",
                    h * 0.5, w * 0.33, w * 0.67, h, w - 8.0, w - 18.0, (h * 0.5) - 8.0, w - 18.0, (h * 0.5) + 8.0),

            ShapeType.SCurveConnector =>
                string.Format(ci, "M 0,{0:F1} C {1:F1},{0:F1} {2:F1},0 {3:F1},0",
                    h, w * 0.45, w * 0.55, w),

            ShapeType.WaveLine =>
                string.Format(ci, "M 0,{0:F1} C {1:F1},0 {2:F1},{3:F1} {4:F1},{0:F1} C {5:F1},0 {6:F1},{3:F1} {7:F1},{0:F1}",
                    h * 0.5, w * 0.125, w * 0.375, h, w * 0.5, w * 0.625, w * 0.875, w),

            ShapeType.ArcLine =>
                string.Format(ci, "M 0,{0:F1} Q {1:F1},0 {2:F1},{0:F1}",
                    h, w * 0.5, w),

            ShapeType.CurlyBrace =>
                string.Format(ci, "M {0:F1},0 C {1:F1},0 {1:F1},{2:F1} 0,{3:F1} C {1:F1},{4:F1} {1:F1},{5:F1} {0:F1},{5:F1}",
                    w, w * 0.45, h * 0.4, h * 0.5, h * 0.6, h),

            ShapeType.CurvedCallout =>
                string.Format(ci, "M {0:F1},0 L {1:F1},0 A {0:F1},{0:F1} 0 0 1 {2:F1},{0:F1} L {2:F1},{3:F1} A {0:F1},{0:F1} 0 0 1 {1:F1},{4:F1} L {5:F1},{4:F1} Q {6:F1},{4:F1} {7:F1},{8:F1} Q {9:F1},{10:F1} {9:F1},{4:F1} L {0:F1},{4:F1} A {0:F1},{0:F1} 0 0 1 0,{3:F1} L 0,{0:F1} A {0:F1},{0:F1} 0 0 1 {0:F1},0 Z",
                    r, w - r, w, h * 0.72 - r, h * 0.72, w * 0.5, w * 0.38, w * 0.22, h, w * 0.32, h * 0.82),

            ShapeType.Teardrop =>
                string.Format(ci, "M {0:F1},0 C {1:F1},{2:F1} {3:F1},{4:F1} {0:F1},{5:F1} C 0,{4:F1} {6:F1},{2:F1} {0:F1},0 Z",
                    w * 0.5, w * 0.85, h * 0.45, w, h * 0.75, h, w * 0.15),

            ShapeType.WaveRibbon =>
                string.Format(ci, "M 0,{0:F1} Q {1:F1},0 {2:F1},{0:F1} Q {3:F1},{4:F1} {5:F1},{0:F1} L {5:F1},{6:F1} Q {3:F1},{7:F1} {2:F1},{6:F1} Q {1:F1},{8:F1} 0,{6:F1} Z",
                    h * 0.2, w * 0.25, w * 0.5, w * 0.75, h * 0.4, w, h * 0.8, h, h * 0.6),

            ShapeType.OrganicBlob =>
                string.Format(ci, "M {0:F1},0 C {1:F1},{2:F1} {3:F1},{4:F1} {5:F1},{6:F1} C {0:F1},{7:F1} {8:F1},{6:F1} 0,{4:F1} C 0,{2:F1} {8:F1},0 {0:F1},0 Z",
                    w * 0.5, w * 0.9, h * 0.12, w, h * 0.6, w * 0.75, h * 0.9, h, w * 0.15),

            ShapeType.Card =>
                string.Format(ci, "M {0:F1},0 L {1:F1},0 A {0:F1},{0:F1} 0 0 1 {2:F1},{0:F1} L {2:F1},{3:F1} A {0:F1},{0:F1} 0 0 1 {1:F1},{4:F1} L {0:F1},{4:F1} A {0:F1},{0:F1} 0 0 1 0,{3:F1} L 0,{0:F1} A {0:F1},{0:F1} 0 0 1 {0:F1},0 Z",
                    r, w - r, w, h - r, h),

            ShapeType.StickyNote =>
                string.Format(ci, "M 0,0 L {0:F1},0 L {1:F1},18 L {1:F1},{2:F1} L 0,{2:F1} Z M {0:F1},0 L {0:F1},18 L {1:F1},18",
                    w - 18.0, w, h),

            _ => string.Format(ci, "M 0,0 L {0:F1},0 L {0:F1},{1:F1} L 0,{1:F1} Z", w, h)
        };
    }

    public static string? GetDashArray(LineDashStyle style, double strokeThickness)
    {
        double st = Math.Max(1.0, strokeThickness);
        var ci = CultureInfo.InvariantCulture;
        return style switch
        {
            LineDashStyle.Dashed => string.Format(ci, "{0:F1},{1:F1}", st * 3.5, st * 2.0),
            LineDashStyle.Dotted => string.Format(ci, "{0:F1},{1:F1}", st, st * 2.0),
            LineDashStyle.DashDot => string.Format(ci, "{0:F1},{1:F1},{2:F1},{1:F1}", st * 4.0, st * 2.0, st),
            _ => null
        };
    }

    public static string GenerateSmoothInkSvgPath(string pointsData, bool isSmoothSpline = true)
    {
        if (string.IsNullOrWhiteSpace(pointsData)) return "M 0,0";

        var ci = CultureInfo.InvariantCulture;
        var rawParts = pointsData.Trim().Split(new[] { ' ', ';', '\n', '\r', '\t' }, StringSplitOptions.RemoveEmptyEntries);
        var points = new System.Collections.Generic.List<(double X, double Y)>();

        foreach (var p in rawParts)
        {
            var coords = p.Split(',');
            if (coords.Length == 2 &&
                double.TryParse(coords[0], NumberStyles.Float, CultureInfo.InvariantCulture, out double x) &&
                double.TryParse(coords[1], NumberStyles.Float, CultureInfo.InvariantCulture, out double y))
            {
                points.Add((x, y));
            }
        }

        if (points.Count == 0) return "M 0,0";
        if (points.Count == 1) return string.Format(ci, "M {0:F1},{1:F1} L {0:F1},{1:F1}", points[0].X, points[0].Y);
        if (points.Count == 2 || !isSmoothSpline)
        {
            var sbPoly = new StringBuilder();
            sbPoly.AppendFormat(ci, "M {0:F1},{1:F1}", points[0].X, points[0].Y);
            for (int i = 1; i < points.Count; i++)
            {
                sbPoly.AppendFormat(ci, " L {0:F1},{1:F1}", points[i].X, points[i].Y);
            }
            return sbPoly.ToString();
        }

        // Catmull-Rom to Cubic Bézier Spline Conversion
        var sb = new StringBuilder();
        sb.AppendFormat(ci, "M {0:F1},{1:F1}", points[0].X, points[0].Y);

        int n = points.Count;
        for (int i = 0; i < n - 1; i++)
        {
            var p0 = i > 0 ? points[i - 1] : points[i];
            var p1 = points[i];
            var p2 = points[i + 1];
            var p3 = i < n - 2 ? points[i + 2] : p2;

            // Catmull-Rom tangents: T1 = (p2 - p0)/2, T2 = (p3 - p1)/2
            double t1x = (p2.X - p0.X) / 2.0;
            double t1y = (p2.Y - p0.Y) / 2.0;
            double t2x = (p3.X - p1.X) / 2.0;
            double t2y = (p3.Y - p1.Y) / 2.0;

            // Cubic Bézier control points: C1 = p1 + T1/3, C2 = p2 - T2/3
            double c1x = p1.X + (t1x / 3.0);
            double c1y = p1.Y + (t1y / 3.0);
            double c2x = p2.X - (t2x / 3.0);
            double c2y = p2.Y - (t2y / 3.0);

            sb.AppendFormat(ci, " C {0:F1},{1:F1} {2:F1},{3:F1} {4:F1},{5:F1}", c1x, c1y, c2x, c2y, p2.X, p2.Y);
        }

        return sb.ToString();
    }

    public static string GenerateDividerSvgPath(DividerStyle style, double width, double height, double amplitude = 6.0, double frequency = 4.0, bool isVertical = false)
    {
        double w = Math.Max(10, width);
        double h = Math.Max(4, height);
        var ci = CultureInfo.InvariantCulture;

        if (isVertical)
        {
            double midX = w / 2.0;
            return style switch
            {
                DividerStyle.Wave => string.Format(ci, "M {0:F1},0 C {1:F1},{2:F1} {3:F1},{4:F1} {0:F1},{5:F1} C {1:F1},{6:F1} {3:F1},{7:F1} {0:F1},{8:F1}",
                    midX, midX - amplitude, h * 0.15, midX + amplitude, h * 0.35, h * 0.5, h * 0.65, h * 0.85, h),
                DividerStyle.Arch => string.Format(ci, "M {0:F1},0 Q {1:F1},{2:F1} {0:F1},{3:F1}",
                    midX, midX - amplitude, h * 0.5, h),
                _ => string.Format(ci, "M {0:F1},0 L {0:F1},{1:F1}", midX, h)
            };
        }

        double midY = h / 2.0;
        double amp = Math.Min(midY, Math.Max(2.0, amplitude));
        int cycles = Math.Max(1, (int)Math.Round(frequency));

        return style switch
        {
            DividerStyle.Wave => GenerateHarmonicWavePath(w, midY, amp, cycles),
            DividerStyle.SCurve => string.Format(ci, "M 0,{0:F1} C {1:F1},{2:F1} {3:F1},{4:F1} {5:F1},{0:F1}",
                midY, w * 0.35, midY - amp, w * 0.65, midY + amp, w),
            DividerStyle.Arch => string.Format(ci, "M 0,{0:F1} Q {1:F1},{2:F1} {3:F1},{0:F1}",
                midY, w * 0.5, midY - amp, w),
            DividerStyle.DoubleWave => GenerateDoubleWavePath(w, midY, amp, cycles),
            DividerStyle.CalligraphicFlourish => string.Format(ci, "M 0,{0:F1} C {1:F1},{2:F1} {3:F1},{4:F1} {5:F1},{0:F1} L {6:F1},{0:F1} L {7:F1},{2:F1} L {8:F1},{0:F1} L {9:F1},{0:F1} C {10:F1},{2:F1} {11:F1},{4:F1} {12:F1},{0:F1}",
                midY, w * 0.1, midY - amp, w * 0.25, midY + amp, w * 0.45, w * 0.48, w * 0.5, w * 0.52, w * 0.55, w * 0.75, w * 0.9, w),
            _ => string.Format(ci, "M 0,{0:F1} L {1:F1},{0:F1}", midY, w)
        };
    }

    private static string GenerateHarmonicWavePath(double w, double midY, double amp, int cycles)
    {
        var ci = CultureInfo.InvariantCulture;
        var sb = new StringBuilder();
        sb.AppendFormat(ci, "M 0,{0:F1}", midY);

        double cycleWidth = w / cycles;
        for (int i = 0; i < cycles; i++)
        {
            double startX = i * cycleWidth;
            double endX = startX + cycleWidth;
            double cp1x = startX + (cycleWidth * 0.25);
            double cp2x = startX + (cycleWidth * 0.75);

            sb.AppendFormat(ci, " C {0:F1},{1:F1} {2:F1},{3:F1} {4:F1},{5:F1}",
                cp1x, midY - amp, cp2x, midY + amp, endX, midY);
        }

        return sb.ToString();
    }

    private static string GenerateDoubleWavePath(double w, double midY, double amp, int cycles)
    {
        var ci = CultureInfo.InvariantCulture;
        string wave1 = GenerateHarmonicWavePath(w, midY - (amp * 0.4), amp * 0.6, cycles);
        string wave2 = GenerateHarmonicWavePath(w, midY + (amp * 0.4), amp * 0.6, cycles);
        return wave1 + " " + wave2;
    }

    private static string GenerateStarPath(double w, double h, int points, double innerRadiusRatio)
    {
        var ci = CultureInfo.InvariantCulture;
        var sb = new StringBuilder();

        double cx = w / 2.0;
        double cy = h / 2.0;
        double rx = w / 2.0;
        double ry = h / 2.0;
        double innerRx = rx * innerRadiusRatio;
        double innerRy = ry * innerRadiusRatio;

        int totalVertices = points * 2;
        double angleStep = Math.PI * 2.0 / totalVertices;
        double startAngle = -Math.PI / 2.0;

        for (int i = 0; i < totalVertices; i++)
        {
            double angle = startAngle + (i * angleStep);
            bool isOuter = (i % 2 == 0);
            double currentRx = isOuter ? rx : innerRx;
            double currentRy = isOuter ? ry : innerRy;

            double x = cx + (currentRx * Math.Cos(angle));
            double y = cy + (currentRy * Math.Sin(angle));

            if (i == 0)
                sb.AppendFormat(ci, "M {0:F1},{1:F1} ", x, y);
            else
                sb.AppendFormat(ci, "L {0:F1},{1:F1} ", x, y);
        }

        sb.Append("Z");
        return sb.ToString();
    }

    public static string GenerateSvgMarkup(PdfShapeElement shape)
    {
        double w = Math.Max(10, shape.Width);
        double h = Math.Max(10, shape.Height);
        var ci = CultureInfo.InvariantCulture;

        string fill = string.IsNullOrWhiteSpace(shape.FillColorHex) || shape.FillColorHex.Equals("Transparent", StringComparison.OrdinalIgnoreCase) || shape.FillColorHex == "#00000000"
            ? "none"
            : shape.FillColorHex;

        string stroke = string.IsNullOrWhiteSpace(shape.StrokeColorHex) || shape.StrokeColorHex.Equals("Transparent", StringComparison.OrdinalIgnoreCase) || shape.StrokeColorHex == "#00000000"
            ? "none"
            : shape.StrokeColorHex;

        double strokeThickness = shape.StrokeThickness > 0 ? shape.StrokeThickness : 0;
        string? dashArray = GetDashArray(shape.DashStyle, strokeThickness);
        string dashAttr = !string.IsNullOrEmpty(dashArray) ? string.Format(ci, " stroke-dasharray=\"{0}\"", dashArray) : "";

        var sb = new StringBuilder();
        sb.AppendFormat(ci, @"<svg xmlns=""http://www.w3.org/2000/svg"" viewBox=""0 0 {0:F1} {1:F1}"" width=""{0:F1}"" height=""{1:F1}"">", w, h);

        if (shape.ShapeType == ShapeType.MedalRibbonBadge)
        {
            string ribbonFill = !string.IsNullOrEmpty(shape.SecondaryFillColorHex) ? shape.SecondaryFillColorHex : "#990000";
            string medalFill = fill != "none" ? fill : "#F59E0B";
            string medalStroke = stroke != "none" ? stroke : "#B45309";

            // Left Ribbon Tail
            sb.AppendFormat(ci, @"<path d=""M {0:F1},{1:F1} L {2:F1},{3:F1} L {4:F1},{5:F1} L {6:F1},{7:F1} Z"" fill=""{8}"" stroke=""{9}"" stroke-width=""{10:F1}"" />",
                w * 0.35, h * 0.45, w * 0.15, h, w * 0.35, h * 0.85, w * 0.48, h * 0.55, ribbonFill, medalStroke, Math.Max(1, strokeThickness * 0.7));

            // Right Ribbon Tail
            sb.AppendFormat(ci, @"<path d=""M {0:F1},{1:F1} L {2:F1},{3:F1} L {4:F1},{5:F1} L {6:F1},{7:F1} Z"" fill=""{8}"" stroke=""{9}"" stroke-width=""{10:F1}"" />",
                w * 0.52, h * 0.55, w * 0.65, h * 0.85, w * 0.85, h, w * 0.65, h * 0.45, ribbonFill, medalStroke, Math.Max(1, strokeThickness * 0.7));

            // Outer Medal Rim
            sb.AppendFormat(ci, @"<circle cx=""{0:F1}"" cy=""{1:F1}"" r=""{2:F1}"" fill=""{3}"" stroke=""{4}"" stroke-width=""{5:F1}"" />",
                w / 2.0, h * 0.38, w * 0.38, medalFill, medalStroke, strokeThickness > 0 ? strokeThickness : 2.5);

            // Inner Laurel Garland Embossed Ring
            sb.AppendFormat(ci, @"<circle cx=""{0:F1}"" cy=""{1:F1}"" r=""{2:F1}"" fill=""none"" stroke=""{3}"" stroke-width=""1.5"" stroke-dasharray=""3,3"" />",
                w / 2.0, h * 0.38, w * 0.30, "#FEF3C7");

            sb.AppendFormat(ci, @"<circle cx=""{0:F1}"" cy=""{1:F1}"" r=""{2:F1}"" fill=""none"" stroke=""{3}"" stroke-width=""1.0"" />",
                w / 2.0, h * 0.38, w * 0.24, medalStroke);
        }
        else
        {
            string pathData = GetVectorPath(shape.ShapeType, w, h, shape.CornerRadius, shape.CustomPathData);
            sb.AppendFormat(ci, @"<path d=""{0}"" fill=""{1}"" stroke=""{2}"" stroke-width=""{3:F1}"" stroke-linejoin=""round"" stroke-linecap=""round""{4} />",
                pathData, fill, stroke, strokeThickness, dashAttr);
        }

        if (!string.IsNullOrWhiteSpace(shape.Label))
        {
            string labelColor = !string.IsNullOrEmpty(shape.LabelColorHex) ? shape.LabelColorHex : "#201F1E";
            double labelSize = shape.LabelFontSize > 0 ? shape.LabelFontSize : 12;
            sb.AppendFormat(ci, @"<text x=""{0:F1}"" y=""{1:F1}"" fill=""{2}"" font-family=""Arial, sans-serif"" font-size=""{3:F1}"" font-weight=""bold"" text-anchor=""middle"" dominant-baseline=""middle"">{4}</text>",
                w / 2.0, (shape.ShapeType == ShapeType.MedalRibbonBadge ? h * 0.38 : h / 2.0), labelColor, labelSize, System.Security.SecurityElement.Escape(shape.Label));
        }

        sb.Append("</svg>");
        return sb.ToString();
    }

    public static string GenerateDividerSvgMarkup(PdfDividerElement div)
    {
        double w = Math.Max(10, div.Width);
        double h = Math.Max(4, div.Height);
        var ci = CultureInfo.InvariantCulture;

        string stroke = !string.IsNullOrWhiteSpace(div.ColorHex) ? div.ColorHex : "#0F6CBD";
        double thickness = Math.Max(1.0, div.Thickness);
        string pathData = GenerateDividerSvgPath(div.Style, w, h, div.WaveAmplitude, div.WaveFrequency, div.IsVertical);
        string? dashArray = GetDashArray(div.DashStyle, thickness);
        string dashAttr = !string.IsNullOrEmpty(dashArray) ? string.Format(ci, " stroke-dasharray=\"{0}\"", dashArray) : "";

        return string.Format(ci,
            @"<svg xmlns=""http://www.w3.org/2000/svg"" viewBox=""0 0 {0:F1} {1:F1}"" width=""{0:F1}"" height=""{1:F1}""><path d=""{2}"" fill=""none"" stroke=""{3}"" stroke-width=""{4:F1}"" stroke-linecap=""round"" stroke-linejoin=""round""{5} /></svg>",
            w, h, pathData, stroke, thickness, dashAttr);
    }

    public static string GenerateInkSvgMarkup(PdfInkElement ink)
    {
        double w = Math.Max(10, ink.Width);
        double h = Math.Max(6, ink.Height);
        var ci = CultureInfo.InvariantCulture;

        string stroke = !string.IsNullOrWhiteSpace(ink.StrokeColorHex) ? ink.StrokeColorHex : "#0F6CBD";
        double thickness = Math.Max(1.0, ink.StrokeThickness);
        double opacity = Math.Clamp(ink.Opacity, 0.05, 1.0);
        string pathData = GenerateSmoothInkSvgPath(ink.PointsData, ink.IsSmoothSpline);

        return string.Format(ci,
            @"<svg xmlns=""http://www.w3.org/2000/svg"" viewBox=""0 0 {0:F1} {1:F1}"" width=""{0:F1}"" height=""{1:F1}""><path d=""{2}"" fill=""none"" stroke=""{3}"" stroke-width=""{4:F1}"" stroke-linecap=""round"" stroke-linejoin=""round"" opacity=""{5:F2}"" /></svg>",
            w, h, pathData, stroke, thickness, opacity);
    }
}

