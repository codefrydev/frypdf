using System;
using System.Collections.Generic;
using System.Globalization;
using System.Text;
using UglyToad.PdfPig.Core;
using UglyToad.PdfPig.Graphics;

namespace PdfEditorApp.Core.Deconstruction.Utils;

/// <summary>
/// Converts collections of raw PdfPig vector paths into optimized standard SVG markup.
/// Used to group excess vector paths into a single resolution-independent <see cref="Models.Elements.PdfSvgElement"/>.
/// </summary>
public static class SvgPathBuilder
{
    /// <summary>
    /// Builds an SVG string containing all the provided paths with their respective fill, stroke, and geometry commands.
    /// Coordinates are transformed from PDF bottom-left to standard SVG top-left coordinates.
    /// </summary>
    public static string BuildSvgFromPaths(
        IReadOnlyList<PdfPath> paths,
        double minX,
        double minY,
        double width,
        double height,
        double pageHeight)
    {
        var sb = new StringBuilder();
        var inv = CultureInfo.InvariantCulture;

        sb.Append($"<svg xmlns=\"http://www.w3.org/2000/svg\" viewBox=\"0 0 {width.ToString("F1", inv)} {height.ToString("F1", inv)}\" width=\"{width.ToString("F1", inv)}\" height=\"{height.ToString("F1", inv)}\">\n");

        foreach (var path in paths)
        {
            string pathData = BuildPathData(path, minX, minY, pageHeight);
            if (string.IsNullOrWhiteSpace(pathData)) continue;

            string strokeHex = "none";
            double strokeWidth = 0;
            if (path.IsStroked)
            {
                strokeWidth = Math.Max(0.5, path.LineWidth);
                if (path.StrokeColor != null)
                {
                    var (r, g, bVal) = path.StrokeColor.ToRGBValues();
                    strokeHex = $"#{(int)(r * 255):X2}{(int)(g * 255):X2}{(int)(bVal * 255):X2}";
                }
                else
                {
                    strokeHex = "#0F172A";
                }
            }

            string fillHex = "none";
            if (path.IsFilled)
            {
                if (path.FillColor != null)
                {
                    var (r, g, bVal) = path.FillColor.ToRGBValues();
                    fillHex = $"#{(int)(r * 255):X2}{(int)(g * 255):X2}{(int)(bVal * 255):X2}";
                }
                else
                {
                    fillHex = strokeHex != "none" ? strokeHex : "#000000";
                }
            }

            sb.Append($"  <path d=\"{pathData}\" fill=\"{fillHex}\" stroke=\"{strokeHex}\" stroke-width=\"{strokeWidth.ToString("F1", inv)}\" stroke-linecap=\"round\" stroke-linejoin=\"round\" />\n");
        }

        sb.Append("</svg>");
        return sb.ToString();
    }

    private static string BuildPathData(PdfPath path, double offsetX, double offsetY, double pageHeight)
    {
        var sb = new StringBuilder();
        var inv = CultureInfo.InvariantCulture;

        for (int sIdx = 0; sIdx < path.Count; sIdx++)
        {
            var subpath = path[sIdx];
            if (subpath.Commands == null) continue;

            foreach (var cmd in subpath.Commands)
            {
                switch (cmd)
                {
                    case PdfSubpath.Move move:
                        double mx = move.Location.X - offsetX;
                        double my = (pageHeight - move.Location.Y) - offsetY;
                        sb.Append($"M {mx.ToString("F1", inv)} {my.ToString("F1", inv)} ");
                        break;

                    case PdfSubpath.Line line:
                        double lx = line.To.X - offsetX;
                        double ly = (pageHeight - line.To.Y) - offsetY;
                        sb.Append($"L {lx.ToString("F1", inv)} {ly.ToString("F1", inv)} ");
                        break;

                    case PdfSubpath.BezierCurve bezier:
                        var lines = bezier.ToLines(8);
                        foreach (var l in lines)
                        {
                            double ex = l.To.X - offsetX;
                            double ey = (pageHeight - l.To.Y) - offsetY;
                            sb.Append($"L {ex.ToString("F1", inv)} {ey.ToString("F1", inv)} ");
                        }
                        break;

                    case PdfSubpath.Close:
                        sb.Append("Z ");
                        break;
                }
            }
        }

        return sb.ToString().TrimEnd();
    }
}
