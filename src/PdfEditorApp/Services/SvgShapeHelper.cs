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

            ShapeType.Card =>
                string.Format(ci, "M {0:F1},0 L {1:F1},0 A {0:F1},{0:F1} 0 0 1 {2:F1},{0:F1} L {2:F1},{3:F1} A {0:F1},{0:F1} 0 0 1 {1:F1},{4:F1} L {0:F1},{4:F1} A {0:F1},{0:F1} 0 0 1 0,{3:F1} L 0,{0:F1} A {0:F1},{0:F1} 0 0 1 {0:F1},0 Z",
                    r, w - r, w, h - r, h),

            ShapeType.StickyNote =>
                string.Format(ci, "M 0,0 L {0:F1},0 L {1:F1},18 L {1:F1},{2:F1} L 0,{2:F1} Z M {0:F1},0 L {0:F1},18 L {1:F1},18",
                    w - 18.0, w, h),

            _ => string.Format(ci, "M 0,0 L {0:F1},0 L {0:F1},{1:F1} L 0,{1:F1} Z", w, h)
        };
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
            sb.AppendFormat(ci, @"<path d=""{0}"" fill=""{1}"" stroke=""{2}"" stroke-width=""{3:F1}"" stroke-linejoin=""round"" stroke-linecap=""round"" />",
                pathData, fill, stroke, strokeThickness);
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
}
