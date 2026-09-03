using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using PdfEditorApp.Core.Models.Elements;
using UglyToad.PdfPig.Content;
using UglyToad.PdfPig.Core;
using UglyToad.PdfPig.Graphics;

namespace PdfEditorApp.Core.Analysis;

/// <summary>
/// Result of table detection containing the reconstructed PdfTableElement
/// and the indices/bounds of swallowed lines and text blocks.
/// </summary>
public class DetectedTableResult
{
    public PdfTableElement TableElement { get; set; } = new();
    public List<int> ConsumedPathIndices { get; } = new();
    public List<ExtractedPdfParagraph> ConsumedParagraphs { get; } = new();
    public PdfRectangle BoundingBox { get; set; }
}

/// <summary>
/// Advanced Table Grid Detector and Structure Reconstructor.
/// Detects tabular grids from raw vector lines and cell alignments in 3rd-party PDFs,
/// synthesizing cohesive, editable PdfTableElement models.
/// </summary>
public static class TableGridDetector
{
    public static List<DetectedTableResult> DetectTables(
        IReadOnlyList<PdfPath>? paths,
        List<ExtractedPdfParagraph> paragraphs,
        double pageWidth,
        double pageHeight)
    {
        var results = new List<DetectedTableResult>();
        if (paths == null || paths.Count == 0 || paragraphs == null || paragraphs.Count == 0)
            return results;

        // Step 1: Extract horizontal and vertical line segments from paths
        var hLines = new List<(double Y, double Left, double Right, int PathIndex)>();
        var vLines = new List<(double X, double Bottom, double Top, int PathIndex)>();

        for (int i = 0; i < paths.Count; i++)
        {
            var p = paths[i];
            var bbox = p.GetBoundingRectangle();
            if (!bbox.HasValue) continue;

            var b = bbox.Value;
            if (b.Width >= pageWidth * 0.98 && b.Height >= pageHeight * 0.98) continue; // Skip page border

            // Horizontal line segment: width >= 40, height <= 4
            if (b.Width >= 40.0 && b.Height <= 4.0)
            {
                double canvasY = pageHeight - b.Top;
                hLines.Add((canvasY, b.Left, b.Right, i));
            }
            // Vertical line segment: height >= 20, width <= 4
            else if (b.Height >= 20.0 && b.Width <= 4.0)
            {
                double canvasTop = pageHeight - b.Top;
                double canvasBottom = pageHeight - b.Bottom;
                vLines.Add((b.Left, canvasTop, canvasBottom, i));
            }
        }

        // Need at least 2 horizontal lines to form a table
        if (hLines.Count < 2) return results;

        // Group horizontal lines by proximity and overlapping X spans to find table clusters
        var sortedHLines = hLines.OrderBy(h => h.Y).ToList();
        var tableHLinesGroups = new List<List<(double Y, double Left, double Right, int PathIndex)>>();
        var currentGroup = new List<(double Y, double Left, double Right, int PathIndex)>();

        foreach (var hl in sortedHLines)
        {
            if (currentGroup.Count == 0)
            {
                currentGroup.Add(hl);
                continue;
            }

            var prev = currentGroup.Last();
            double yGap = hl.Y - prev.Y;
            bool xOverlap = (hl.Left < prev.Right + 10) && (hl.Right > prev.Left - 10);

            // Table rows typically have pitch between 12pt and 60pt
            if (yGap <= 65.0 && xOverlap)
            {
                currentGroup.Add(hl);
            }
            else
            {
                if (currentGroup.Count >= 3)
                {
                    tableHLinesGroups.Add(new List<(double Y, double Left, double Right, int PathIndex)>(currentGroup));
                }
                currentGroup = new List<(double Y, double Left, double Right, int PathIndex)> { hl };
            }
        }

        if (currentGroup.Count >= 3)
        {
            tableHLinesGroups.Add(currentGroup);
        }

        // Step 2: For each table line cluster, construct a structured PdfTableElement
        foreach (var group in tableHLinesGroups)
        {
            double tableTopY = group.Min(h => h.Y);
            double tableBottomY = group.Max(h => h.Y);
            double tableLeftX = group.Min(h => h.Left);
            double tableRightX = group.Max(h => h.Right);
            double tableW = tableRightX - tableLeftX;
            double tableH = tableBottomY - tableTopY;

            if (tableW < 80.0 || tableH < 30.0) continue;

            // Find all text paragraphs within table bounds
            var tableParas = paragraphs.Where(p =>
                p.CanvasX >= tableLeftX - 10 &&
                p.CanvasX + p.CanvasWidth <= tableRightX + 10 &&
                p.CanvasY >= tableTopY - 10 &&
                p.CanvasY + p.CanvasHeight <= tableBottomY + 10).ToList();

            if (tableParas.Count < 4) continue; // Need at least 4 text items to be a valid multi-cell table

            // Detect column divisions from text X positions
            var distinctColXs = tableParas
                .Select(p => Math.Round(p.CanvasX, -1)) // Round to nearest 10pt
                .Distinct()
                .OrderBy(x => x)
                .ToList();

            if (distinctColXs.Count < 2) continue; // Must have at least 2 columns

            // Group distinct rows by Y pitch
            var distinctRowYs = group
                .Select(h => h.Y)
                .OrderBy(y => y)
                .ToList();

            int rowCount = distinctRowYs.Count - 1;
            int colCount = distinctColXs.Count;
            if (rowCount < 2 || colCount < 2) continue;

            var headers = new List<string>();
            var rows = new List<List<string>>();

            // Populate header row (Row 0)
            double row0Top = distinctRowYs[0];
            double row0Bottom = distinctRowYs[1];

            for (int c = 0; c < colCount; c++)
            {
                double colLeft = distinctColXs[c] - 15;
                double colRight = (c + 1 < colCount) ? distinctColXs[c + 1] - 5 : tableRightX + 5;

                var cellParas = tableParas.Where(p =>
                    p.CanvasY >= row0Top - 5 && p.CanvasY <= row0Bottom + 5 &&
                    p.CanvasX >= colLeft && p.CanvasX < colRight)
                    .OrderBy(p => p.CanvasX)
                    .ToList();

                string headerText = string.Join(" ", cellParas.Select(p => p.Text.Trim()));
                headers.Add(string.IsNullOrWhiteSpace(headerText) ? $"Col {c + 1}" : headerText);
            }

            // Populate body rows (Row 1..N)
            for (int r = 1; r < rowCount; r++)
            {
                double rTop = distinctRowYs[r];
                double rBottom = distinctRowYs[r + 1];
                var rowData = new List<string>();

                for (int c = 0; c < colCount; c++)
                {
                    double colLeft = distinctColXs[c] - 15;
                    double colRight = (c + 1 < colCount) ? distinctColXs[c + 1] - 5 : tableRightX + 5;

                    var cellParas = tableParas.Where(p =>
                        p.CanvasY >= rTop - 5 && p.CanvasY <= rBottom + 5 &&
                        p.CanvasX >= colLeft && p.CanvasX < colRight)
                        .OrderBy(p => p.CanvasX)
                        .ToList();

                    string cellText = string.Join(" ", cellParas.Select(p => p.Text.Trim()));
                    rowData.Add(cellText);
                }

                rows.Add(rowData);
            }

            if (headers.Count > 0 && rows.Count > 0)
            {
                var tableElement = new PdfTableElement
                {
                    X = Math.Round(tableLeftX, 1),
                    Y = Math.Round(tableTopY, 1),
                    Width = Math.Round(tableW, 1),
                    Height = Math.Round(tableH, 1),
                    Headers = headers,
                    Rows = rows,
                    HeaderBackgroundHex = "#0F6CBD",
                    HeaderTextHex = "#FFFFFF",
                    AlternateRowBackgroundHex = "#F8FAFC",
                    BorderColorHex = "#CBD5E1"
                };

                var detected = new DetectedTableResult
                {
                    TableElement = tableElement,
                    BoundingBox = new PdfRectangle(tableLeftX, tableBottomY, tableRightX, tableTopY)
                };

                detected.ConsumedPathIndices.AddRange(group.Select(h => h.PathIndex));
                detected.ConsumedParagraphs.AddRange(tableParas);

                results.Add(detected);
            }
        }

        return results;
    }
}
