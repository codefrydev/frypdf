using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.Extensions.Logging;
using UglyToad.PdfPig;
using UglyToad.PdfPig.Content;
using PdfEditorApp.Core.Analysis;
using PdfEditorApp.Core.Deconstruction.Extractors;
using PdfEditorApp.Core.Utils;
using PdfEditorApp.Models;
using PdfEditorApp.Models.Elements;

namespace PdfEditorApp.Core.Deconstruction;

/// <summary>
/// Professional PDF Deconstruction Engine (Adobe Acrobat &amp; Wondershare PDFelement Architecture).
/// Orchestrates modular extractors to parse any standard or complex PDF into clean, editable, multi-layered document models.
/// </summary>
public static class PdfDeconstructionEngine
{
    /// <summary>
    /// Deconstructs a binary PDF stream into a fully editable <see cref="PdfDocumentModel"/>.
    /// If the PDF contains an embedded FryPDF native model, restores it losslessly.
    /// Otherwise, performs intelligent deconstruction using default options.
    /// </summary>
    public static PdfDocumentModel Deconstruct(
        byte[] pdfBytes,
        string title = "Imported_Document.pdf",
        string? password = null)
    {
        return Deconstruct(pdfBytes, PdfDeconstructionOptions.Default, null, title, password);
    }

    /// <summary>
    /// Deconstructs a binary PDF stream with custom options and diagnostic logging.
    /// </summary>
    public static PdfDocumentModel Deconstruct(
        byte[] pdfBytes,
        PdfDeconstructionOptions options,
        ILogger? logger = null,
        string title = "Imported_Document.pdf",
        string? password = null)
    {
        // 1. Check if the PDF contains an embedded native FryPDF project model for 100% lossless roundtrip
        if (FryPdfEmbeddingHelper.TryExtractEmbeddedModel(pdfBytes, out var embeddedModel) && embeddedModel != null)
        {
            if (!string.IsNullOrWhiteSpace(title) && string.Equals(embeddedModel.Title, "Untitled.pdf", StringComparison.OrdinalIgnoreCase))
            {
                embeddedModel.Title = title;
            }
            return embeddedModel;
        }

        byte[] sanitized = PdfDocumentSanitizer.SanitizePdfBytes(pdfBytes);

        var parsingOptions = new ParsingOptions();
        if (!string.IsNullOrEmpty(password))
        {
            parsingOptions.Password = password;
        }

        PdfDocument? doc = null;
        try
        {
            doc = PdfDocument.Open(sanitized, parsingOptions);
        }
        catch (Exception ex)
        {
            logger?.LogDebug(ex, "Primary PDF open failed; attempting salvage and repair");
            try
            {
                byte[] repaired = PdfDocumentSanitizer.SalvageAndRepairPdfBytes(sanitized);
                doc = PdfDocument.Open(repaired, parsingOptions);
            }
            catch
            {
                doc = PdfDocument.Open(pdfBytes, parsingOptions);
            }
        }

        using (doc)
        {
            return DeconstructDocument(doc, title, options, logger);
        }
    }

    /// <summary>
    /// Deconstructs an opened PdfPig document into a structured <see cref="PdfDocumentModel"/>.
    /// </summary>
    public static PdfDocumentModel DeconstructDocument(
        PdfDocument doc,
        string defaultTitle,
        PdfDeconstructionOptions? options = null,
        ILogger? logger = null)
    {
        options ??= PdfDeconstructionOptions.Default;

        var model = new PdfDocumentModel
        {
            Title = string.IsNullOrWhiteSpace(doc.Information.Title) ? defaultTitle : doc.Information.Title,
            Author = doc.Information.Author ?? "Unknown Author",
            Subject = doc.Information.Subject ?? "Imported PDF Document",
            Keywords = doc.Information.Keywords ?? "",
            Creator = string.IsNullOrWhiteSpace(doc.Information.Creator) ? "FryPDF" : doc.Information.Creator,
            Producer = string.IsNullOrWhiteSpace(doc.Information.Producer) ? "codefrydev.in" : doc.Information.Producer,
            CreatedDate = DateTime.Now,
            ModifiedDate = DateTime.Now
        };

        int totalPages = doc.NumberOfPages;
        for (int pageNum = 1; pageNum <= totalPages; pageNum++)
        {
            var page = doc.GetPage(pageNum);
            var pageModel = DeconstructPage(page, pageNum, doc, options, logger);
            model.Pages.Add(pageModel);
        }

        return model;
    }

    /// <summary>
    /// Deconstructs an individual PDF page into vector shapes, images, grouped text paragraphs, and form fields.
    /// </summary>
    public static PdfPageModel DeconstructPage(
        Page page,
        int pageNumber,
        PdfDocument? doc = null,
        PdfDeconstructionOptions? options = null,
        ILogger? logger = null)
    {
        options ??= PdfDeconstructionOptions.Default;

        double pageWidth = Math.Max(100.0, page.Width);
        double pageHeight = Math.Max(100.0, page.Height);

        var pageModel = new PdfPageModel
        {
            PageNumber = pageNumber,
            Width = pageWidth,
            Height = pageHeight,
            Format = DeterminePageFormat(pageWidth, pageHeight),
            Orientation = pageWidth > pageHeight ? PageOrientation.Landscape : PageOrientation.Portrait,
            RotationAngle = (int)page.Rotation.Value,
            BackgroundColorHex = "#FFFFFF",
            ShowHeaderFooter = false
        };

        int bgZIndex = options.InitialBgZIndex;
        int imgZIndex = options.InitialImgZIndex;
        int tableZIndex = options.InitialTableZIndex;
        int shapeZIndex = options.InitialShapeZIndex;
        int textZIndex = options.InitialTextZIndex;
        int formZIndex = options.InitialFormZIndex;

        var words = page.GetWords()
            .Where(w => !string.IsNullOrWhiteSpace(w.Text))
            .ToList();

        var images = page.GetImages().ToList();

        // 1. Detect page type:
        //    - Pure scanned: image covers >= threshold of page AND <= max word count
        //    - Born-digital / Mixed: has both digital text and images/vectors
        bool hasMassiveImageCover = images.Any(img =>
            img.BoundingBox.Width >= pageWidth * options.PureScannedImageCoverageThreshold &&
            img.BoundingBox.Height >= pageHeight * options.PureScannedImageCoverageThreshold);

        bool isPureScanned = words.Count <= options.PureScannedWordCountMax && images.Count > 0 && hasMassiveImageCover;
        bool hasSufficientText = words.Count > options.PureScannedWordCountMax;

        if (isPureScanned)
        {
            // Pure Scanned Page Mode: Extract high-res image as base canvas layer
            var scannedImages = PdfImageExtractor.ExtractScannedCanvasImages(images, pageNumber, pageHeight, ref bgZIndex, options, logger);
            pageModel.Elements.AddRange(scannedImages);
        }
        else
        {
            // Born-Digital / Mixed Document Mode:
            pageModel.BackgroundColorHex = "#FFFFFF";

            // A. Extract Embedded Images (Photos, Logos, Figures, Icons, Watermarks)
            var embeddedImages = PdfImageExtractor.ExtractEmbeddedImages(
                images, pageNumber, pageWidth, pageHeight, hasSufficientText, ref bgZIndex, ref imgZIndex, options, logger);
            pageModel.Elements.AddRange(embeddedImages);

            // B. Extract and Cluster Text Paragraphs
            var paragraphs = new List<ExtractedPdfParagraph>();
            try
            {
                bool isLandscape = pageWidth > pageHeight;
                double colGapMultiplier = isLandscape ? options.ColumnGapMultiplierLandscape : options.ColumnGapMultiplierPortrait;
                paragraphs = PdfLayoutAnalyzer.AnalyzeAndGroupPageText(page, pageHeight, colGapMultiplier);
            }
            catch (Exception ex)
            {
                logger?.LogWarning(ex, "Failed to analyze page text layout on page {PageNumber}", pageNumber);
            }

            // C. Detect Structured Tables from intersecting vector lines and cell alignments
            var consumedPathIndices = new HashSet<int>();
            var consumedParagraphs = new HashSet<ExtractedPdfParagraph>();

            try
            {
                var detectedTables = TableGridDetector.DetectTables(page.Paths, paragraphs, pageWidth, pageHeight);
                foreach (var tableResult in detectedTables)
                {
                    tableResult.TableElement.ZIndex = tableZIndex++;
                    pageModel.Elements.Add(tableResult.TableElement);

                    foreach (var idx in tableResult.ConsumedPathIndices)
                        consumedPathIndices.Add(idx);

                    foreach (var para in tableResult.ConsumedParagraphs)
                        consumedParagraphs.Add(para);
                }
            }
            catch (Exception ex)
            {
                logger?.LogWarning(ex, "Failed to detect tables on page {PageNumber}", pageNumber);
            }

            // D. Extract Vector Paths & Geometric Shapes (Dividers, Rectangles, Borders, SVG Clusters)
            var vectorElements = PdfVectorExtractor.ExtractVectors(
                page.Paths, consumedPathIndices, pageNumber, pageWidth, pageHeight, ref bgZIndex, ref shapeZIndex, options, logger);
            pageModel.Elements.AddRange(vectorElements);

            // E. Add Unconsumed Text Elements with Dynamic Contrast and Container Grouping
            var textElements = PdfTextExtractor.ExtractTextElements(
                page, paragraphs, consumedParagraphs, pageModel.Elements, pageNumber, pageWidth, pageHeight, ref textZIndex, options, logger);
            pageModel.Elements.AddRange(textElements);
        }

        // 3. Extract AcroForm Form Fields (Text Boxes, Checkboxes, Signatures, Dropdowns)
        var formElements = PdfFormExtractor.ExtractFormFields(doc, pageNumber, pageHeight, ref formZIndex, options, logger);
        pageModel.Elements.AddRange(formElements);

        return pageModel;
    }

    /// <summary>
    /// Determines standard page format (A4, Letter, Legal, A3, A5) based on physical point dimensions.
    /// </summary>
    public static PageFormat DeterminePageFormat(double width, double height)
    {
        double maxDim = Math.Max(width, height);
        double minDim = Math.Min(width, height);

        if (Math.Abs(minDim - 595.28) < 20 && Math.Abs(maxDim - 841.89) < 20) return PageFormat.A4;
        if (Math.Abs(minDim - 612.0) < 20 && Math.Abs(maxDim - 792.0) < 20) return PageFormat.Letter;
        if (Math.Abs(minDim - 612.0) < 20 && Math.Abs(maxDim - 1008.0) < 20) return PageFormat.Legal;
        if (Math.Abs(minDim - 841.89) < 30 && Math.Abs(maxDim - 1190.55) < 30) return PageFormat.A3;
        if (Math.Abs(minDim - 419.53) < 20 && Math.Abs(maxDim - 595.28) < 20) return PageFormat.A5;

        return PageFormat.A4;
    }
}
