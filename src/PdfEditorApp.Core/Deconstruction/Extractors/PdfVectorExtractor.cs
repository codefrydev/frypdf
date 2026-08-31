using System;
using System.Collections.Generic;
using Microsoft.Extensions.Logging;
using UglyToad.PdfPig.Graphics;
using PdfEditorApp.Core.Deconstruction.Utils;
using PdfEditorApp.Models;
using PdfEditorApp.Models.Elements;

namespace PdfEditorApp.Core.Deconstruction.Extractors;

/// <summary>
/// Subsystem for extracting vector graphics, dividers, and background containers from PDF paths.
/// Automatically groups excess micro-paths into a single resolution-independent <see cref="PdfSvgElement"/>
/// so no charts, diagrams, or illustrations are dropped.
/// </summary>
public static class PdfVectorExtractor
{
    /// <summary>
    /// Extracts vector shapes, divider lines, and grouped SVG clusters from the page paths.
    /// </summary>
    public static List<PdfElementBase> ExtractVectors(
        IReadOnlyList<PdfPath>? paths,
        HashSet<int> consumedPathIndices,
        int pageNumber,
        double pageWidth,
        double pageHeight,
        ref int bgZIndex,
        ref int shapeZIndex,
        PdfDeconstructionOptions options,
        ILogger? logger = null)
    {
        var elements = new List<PdfElementBase>();
        if (paths == null || paths.Count == 0) return elements;

        try
        {
            int extractedShapeCount = 0;
            var excessPaths = new List<PdfPath>();

            for (int pathIdx = 0; pathIdx < paths.Count; pathIdx++)
            {
                if (consumedPathIndices.Contains(pathIdx)) continue; // Swallowed by table

                var path = paths[pathIdx];
                var bbox = path.GetBoundingRectangle();
                if (!bbox.HasValue) continue;

                var b = bbox.Value;
                if (b.Width <= 0.5 && b.Height <= 0.5) continue;

                // Ignore full-page border clipping frames
                if (b.Width >= pageWidth * 0.98 && b.Height >= pageHeight * 0.98) continue;

                // If we have exceeded the maximum individual shape threshold, accumulate into excess group
                if (extractedShapeCount >= options.MaxVectorShapesPerPage)
                {
                    excessPaths.Add(path);
                    continue;
                }

                double canvasX = Math.Max(0, b.Left);
                double canvasY = Math.Max(0, pageHeight - b.Top);
                double canvasW = Math.Max(1.0, b.Width);
                double canvasH = Math.Max(1.0, b.Height);

                string strokeHex = "#0F172A";
                if (path.StrokeColor != null)
                {
                    var (r, g, bVal) = path.StrokeColor.ToRGBValues();
                    strokeHex = $"#{(int)(r * 255):X2}{(int)(g * 255):X2}{(int)(bVal * 255):X2}";
                }

                string fillHex = path.IsStroked && path.StrokeColor != null ? strokeHex : "#000000";
                if (path.FillColor != null)
                {
                    var (r, g, bVal) = path.FillColor.ToRGBValues();
                    fillHex = $"#{(int)(r * 255):X2}{(int)(g * 255):X2}{(int)(bVal * 255):X2}";
                }

                // 1. Check if it's a thin horizontal divider line
                if (canvasH <= options.DividerMaxHeight && canvasW >= options.DividerMinWidth)
                {
                    var divider = new PdfDividerElement
                    {
                        X = Math.Round(canvasX, 1),
                        Y = Math.Round(canvasY, 1),
                        Width = Math.Round(canvasW, 1),
                        Height = Math.Round(Math.Max(1.0, canvasH), 1),
                        Thickness = Math.Round(Math.Max(1.0, canvasH), 1),
                        IsVertical = false,
                        ColorHex = path.IsStroked ? strokeHex : (path.IsFilled ? fillHex : "#CBD5E1"),
                        ZIndex = shapeZIndex++
                    };
                    elements.Add(divider);
                    extractedShapeCount++;
                }
                // 2. Check if it's a thin vertical divider line
                else if (canvasW <= options.DividerMaxHeight && canvasH >= options.DividerMinWidth)
                {
                    var divider = new PdfDividerElement
                    {
                        X = Math.Round(canvasX, 1),
                        Y = Math.Round(canvasY, 1),
                        Width = Math.Round(Math.Max(1.0, canvasW), 1),
                        Height = Math.Round(canvasH, 1),
                        Thickness = Math.Round(Math.Max(1.0, canvasW), 1),
                        IsVertical = true,
                        ColorHex = path.IsStroked ? strokeHex : (path.IsFilled ? fillHex : "#CBD5E1"),
                        ZIndex = shapeZIndex++
                    };
                    elements.Add(divider);
                    extractedShapeCount++;
                }
                // 3. 2D shape / container / badge
                else if (canvasW >= options.MinShapeDimension && canvasH >= options.MinShapeDimension && (path.IsFilled || path.IsStroked))
                {
                    // Distinguish large background container cards vs foreground shapes
                    bool isLargeContainer = (canvasW >= options.LargeContainerMinWidth && canvasH >= options.LargeContainerMinHeight) &&
                                            (path.IsFilled || (path.IsStroked && !path.IsFilled));
                    int targetZIndex = isLargeContainer ? bgZIndex++ : shapeZIndex++;

                    var shape = new PdfShapeElement
                    {
                        X = Math.Round(canvasX, 1),
                        Y = Math.Round(canvasY, 1),
                        Width = Math.Round(canvasW, 1),
                        Height = Math.Round(canvasH, 1),
                        FillColorHex = path.IsFilled ? fillHex : "Transparent",
                        StrokeColorHex = path.IsStroked ? strokeHex : "Transparent",
                        StrokeThickness = path.IsStroked ? Math.Max(1.0, path.LineWidth) : 0,
                        CornerRadius = 0,
                        ShapeType = ShapeType.Rectangle,
                        ZIndex = targetZIndex
                    };
                    elements.Add(shape);
                    extractedShapeCount++;
                }
            }

            // Group excess vector paths into a single SVG element to guarantee zero data loss
            if (options.GroupExcessVectorsAsSvg && excessPaths.Count > 0)
            {
                double minX = double.MaxValue;
                double minY = double.MaxValue;
                double maxX = double.MinValue;
                double maxY = double.MinValue;

                foreach (var p in excessPaths)
                {
                    var bbox = p.GetBoundingRectangle();
                    if (bbox.HasValue)
                    {
                        var b = bbox.Value;
                        double cX = Math.Max(0, b.Left);
                        double cY = Math.Max(0, pageHeight - b.Top);
                        minX = Math.Min(minX, cX);
                        minY = Math.Min(minY, cY);
                        maxX = Math.Max(maxX, cX + b.Width);
                        maxY = Math.Max(maxY, cY + b.Height);
                    }
                }

                if (minX < maxX && minY < maxY)
                {
                    double groupW = Math.Max(10, maxX - minX);
                    double groupH = Math.Max(10, maxY - minY);
                    string svgMarkup = SvgPathBuilder.BuildSvgFromPaths(excessPaths, minX, minY, groupW, groupH, pageHeight);

                    var svgElement = new PdfSvgElement
                    {
                        X = Math.Round(minX, 1),
                        Y = Math.Round(minY, 1),
                        Width = Math.Round(groupW, 1),
                        Height = Math.Round(groupH, 1),
                        SvgSource = svgMarkup,
                        PresetName = "VectorIllustrationCluster",
                        ZIndex = shapeZIndex++
                    };
                    elements.Add(svgElement);
                }
            }
        }
        catch (Exception ex)
        {
            logger?.LogWarning(ex, "Failed to extract vector paths on page {PageNumber}", pageNumber);
        }

        return elements;
    }
}
