using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.Extensions.Logging;
using UglyToad.PdfPig.Content;
using PdfEditorApp.Core.Analysis;
using PdfEditorApp.Core.Deconstruction.Utils;
using PdfEditorApp.Models;
using PdfEditorApp.Models.Elements;

namespace PdfEditorApp.Core.Deconstruction.Extractors;

/// <summary>
/// Subsystem for extracting, clustering, and formatting text elements from PDF pages.
/// Implements WCAG 2.1 relative luminance dynamic contrast protection and card/container grouping.
/// </summary>
public static class PdfTextExtractor
{
    /// <summary>
    /// Clusters raw PDF words into structured paragraphs and maps them to <see cref="PdfTextElement"/> models.
    /// </summary>
    public static List<PdfTextElement> ExtractTextElements(
        Page page,
        List<ExtractedPdfParagraph> paragraphs,
        HashSet<ExtractedPdfParagraph> consumedParagraphs,
        IReadOnlyList<PdfElementBase> currentElements,
        int pageNumber,
        double pageWidth,
        double pageHeight,
        ref int textZIndex,
        PdfDeconstructionOptions options,
        ILogger? logger = null)
    {
        var textElements = new List<PdfTextElement>();

        try
        {
            var shapeElements = currentElements.OfType<PdfShapeElement>().ToList();

            foreach (var para in paragraphs)
            {
                if (consumedParagraphs.Contains(para)) continue; // Swallowed by table
                if (string.IsNullOrWhiteSpace(para.Text)) continue;

                // Dynamic WCAG Contrast Protection against underlying shape fills
                var underlyingShape = shapeElements
                    .Where(s => s.X <= para.CanvasX + 10 &&
                                s.Y <= para.CanvasY + 10 &&
                                s.X + s.Width >= para.CanvasX + para.CanvasWidth - 10 &&
                                s.Y + s.Height >= para.CanvasY + para.CanvasHeight - 10 &&
                                !string.Equals(s.FillColorHex, "Transparent", StringComparison.OrdinalIgnoreCase))
                    .OrderByDescending(s => s.ZIndex)
                    .FirstOrDefault();

                string underlyingBgHex = underlyingShape?.FillColorHex ?? "#FFFFFF";
                string finalColor = ColorContrastHelper.EnsureLegibleContrast(
                    para.ColorHex,
                    underlyingBgHex,
                    options.MinContrastRatio,
                    options.HighContrastDarkTextColor,
                    options.HighContrastLightTextColor);

                var textElement = new PdfTextElement
                {
                    X = para.CanvasX,
                    Y = para.CanvasY,
                    Width = para.CanvasWidth,
                    Height = para.CanvasHeight,
                    Text = para.Text,
                    FontSize = para.FontSize,
                    FontFamily = para.FontFamily,
                    IsBold = para.IsBold,
                    IsItalic = para.IsItalic,
                    TextColorHex = finalColor,
                    LineHeight = para.LineHeight,
                    Rotation = para.Rotation,
                    Alignment = para.Alignment,
                    TextWrap = false,
                    ZIndex = textZIndex++
                };

                if (para.Spans != null && para.Spans.Count > 1)
                {
                    textElement.Spans = new List<PdfTextSpan>(para.Spans.Count);
                    foreach (var span in para.Spans)
                    {
                        var spanClone = span.Clone();
                        if (string.Equals(spanClone.TextColorHex, para.ColorHex, StringComparison.OrdinalIgnoreCase))
                        {
                            spanClone.TextColorHex = finalColor;
                        }
                        textElement.Spans.Add(spanClone);
                    }
                }

                textElements.Add(textElement);
            }

            // Card & Container Box Grouping: Associate background shapes with their contained text
            GroupContainersAndText(shapeElements, textElements, options);
        }
        catch (Exception ex)
        {
            logger?.LogWarning(ex, "Failed to extract structured text on page {PageNumber}", pageNumber);

            // Fallback: single page text element if unusual encodings prevent grouping
            if (textElements.Count == 0 && !string.IsNullOrWhiteSpace(page.Text))
            {
                var fallbackText = new PdfTextElement
                {
                    X = 36,
                    Y = 36,
                    Width = pageWidth - 72,
                    Height = pageHeight - 72,
                    Text = page.Text,
                    FontSize = 11,
                    FontFamily = "Arial",
                    TextColorHex = options.HighContrastDarkTextColor,
                    ZIndex = textZIndex++
                };
                textElements.Add(fallbackText);
            }
        }

        return textElements;
    }

    /// <summary>
    /// Groups card/container shapes with their contained text elements by assigning a shared GroupId.
    /// </summary>
    public static void GroupContainersAndText(
        List<PdfShapeElement> shapeElements,
        List<PdfTextElement> textElements,
        PdfDeconstructionOptions options)
    {
        var backgroundShapes = shapeElements
            .Where(s => s.Width >= options.MinContainerCardWidth &&
                        s.Height >= options.MinContainerCardHeight &&
                        !string.Equals(s.FillColorHex, "Transparent", StringComparison.OrdinalIgnoreCase))
            .ToList();

        foreach (var shape in backgroundShapes)
        {
            var innerTexts = textElements
                .Where(t => t.X >= shape.X - 5 && t.Y >= shape.Y - 5 &&
                            t.X + t.Width <= shape.X + shape.Width + 5 &&
                            t.Y + t.Height <= shape.Y + shape.Height + 5)
                .ToList();

            if (innerTexts.Count > 0)
            {
                string containerGroupId = Guid.NewGuid().ToString("N");
                shape.GroupId = containerGroupId;
                foreach (var t in innerTexts)
                {
                    t.GroupId = containerGroupId;
                }
            }
        }
    }
}
