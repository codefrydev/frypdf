using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UglyToad.PdfPig;
using UglyToad.PdfPig.Content;
using SkiaSharp;
using PdfEditorApp.Core.Analysis;
using PdfEditorApp.Core.Utils;
using PdfEditorApp.Models;
using PdfEditorApp.Models.Elements;

namespace PdfEditorApp.Core.Deconstruction;

/// <summary>
/// Professional PDF Deconstruction Engine (Adobe Acrobat &amp; Wondershare PDFelement Architecture).
/// Parses any standard or complex PDF into clean, editable, multi-layered document models without
/// raster text ghosting or duplicated underlays.
/// </summary>
public static class PdfDeconstructionEngine
{
    /// <summary>
    /// Deconstructs a binary PDF stream into a fully editable PdfDocumentModel.
    /// If the PDF was exported by FryPDF, restores the 100% lossless original model (tables, charts, formulas, shapes).
    /// Otherwise, performs intelligent AI deconstruction on standard 3rd-party PDFs.
    /// </summary>
    public static PdfDocumentModel Deconstruct(byte[] pdfBytes, string title = "Imported_Document.pdf", string? password = null)
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
        catch
        {
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
            return DeconstructDocument(doc, title);
        }
    }

    /// <summary>
    /// Deconstructs an opened PdfPig document into a structured PdfDocumentModel.
    /// </summary>
    public static PdfDocumentModel DeconstructDocument(PdfDocument doc, string defaultTitle)
    {
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
            var pageModel = DeconstructPage(page, pageNum, doc);
            model.Pages.Add(pageModel);
        }

        return model;
    }

    /// <summary>
    /// Deconstructs an individual PDF page into vector shapes, images, grouped text paragraphs, and form fields.
    /// </summary>
    public static PdfPageModel DeconstructPage(Page page, int pageNumber, PdfDocument? doc = null)
    {
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

        int bgZIndex = 0;
        int imgZIndex = 100;
        int tableZIndex = 500;
        int shapeZIndex = 600;
        int textZIndex = 1000;
        int formZIndex = 2000;

        var words = page.GetWords()
            .Where(w => !string.IsNullOrWhiteSpace(w.Text))
            .ToList();

        var images = page.GetImages().ToList();

        // 1. Detect page type:
        //    - Pure scanned: image covers >85% of page AND <5 words total
        //    - Mixed (e.g. Aadhaar card, government ID): has both meaningful images AND digital text
        //    - Born-digital vector: mostly text, no large background images
        bool hasMassiveImageCover = images.Any(img =>
            img.BoundingBox.Width >= pageWidth * 0.85 &&
            img.BoundingBox.Height >= pageHeight * 0.85);

        bool isPureScanned = words.Count < 5 && images.Count > 0 && hasMassiveImageCover;
        bool hasSufficientText = words.Count >= 5;

        if (isPureScanned)
        {
            // Pure Scanned Page Mode: Preserve high-res scanned image as base canvas layer
            foreach (var img in images)
            {
                byte[]? imgBytes = ExtractImageBytes(img);
                if (imgBytes != null && imgBytes.Length > 0)
                {
                    double imgX = Math.Max(0, img.BoundingBox.Left);
                    double imgY = Math.Max(0, pageHeight - img.BoundingBox.Top);
                    double imgW = Math.Max(10, img.BoundingBox.Width);
                    double imgH = Math.Max(10, img.BoundingBox.Height);

                    var scannedImgElement = new PdfImageElement
                    {
                        X = Math.Round(imgX, 1),
                        Y = Math.Round(imgY, 1),
                        Width = Math.Round(imgW, 1),
                        Height = Math.Round(imgH, 1),
                        Base64Data = Convert.ToBase64String(imgBytes),
                        ZIndex = bgZIndex++,
                        IsLocked = false,
                        CornerRadius = 0,
                        BorderThickness = 0,
                        BorderColorHex = "Transparent",
                        AltText = $"Scanned Page {pageNumber} Canvas"
                    };
                    pageModel.Elements.Add(scannedImgElement);
                }
            }
        }
        else
        {
            // Born-Digital / Mixed Document Mode:
            // Extract images, vector paths, and cluster text cleanly — ZERO ghost underlays!

            pageModel.BackgroundColorHex = "#FFFFFF";

            // A. Extract Embedded Images (Photos, Logos, Figures, Icons, Watermarks)
            try
            {
                foreach (var img in images)
                {
                    double imgW = Math.Max(10, img.BoundingBox.Width);
                    double imgH = Math.Max(10, img.BoundingBox.Height);

                    byte[]? imgBytes = ExtractImageBytes(img);
                    if (imgBytes != null && imgBytes.Length > 0)
                    {
                        double imgX = Math.Max(0, img.BoundingBox.Left);
                        double imgY = Math.Max(0, pageHeight - img.BoundingBox.Top);

                        // Check if this image is a full-page background or centered document watermark
                        bool isFullPageBg = imgW >= pageWidth * 0.88 && imgH >= pageHeight * 0.88 && hasSufficientText;
                        bool isWatermark = (imgW >= pageWidth * 0.65 && imgH >= pageHeight * 0.55) && hasSufficientText;

                        var imgElement = new PdfImageElement
                        {
                            X = Math.Round(imgX, 1),
                            Y = Math.Round(imgY, 1),
                            Width = Math.Round(imgW, 1),
                            Height = Math.Round(imgH, 1),
                            Base64Data = Convert.ToBase64String(imgBytes),
                            ZIndex = (isFullPageBg || isWatermark) ? bgZIndex++ : imgZIndex++,
                            IsLocked = isFullPageBg || isWatermark,
                            Opacity = isWatermark ? 0.35 : 1.0,
                            CornerRadius = 0,
                            BorderThickness = 0,
                            BorderColorHex = "Transparent",
                            AltText = (isFullPageBg || isWatermark) ? $"Watermark Background ({Math.Round(imgW):F0}x{Math.Round(imgH):F0})" : $"Embedded Image ({Math.Round(imgW):F0}x{Math.Round(imgH):F0})"
                        };
                        pageModel.Elements.Add(imgElement);
                    }
                }
            }
            catch
            {
                // Best-effort image extraction
            }

            // B. Extract and Cluster Text Paragraphs first
            var paragraphs = new List<ExtractedPdfParagraph>();
            try
            {
                bool isLandscape = pageWidth > pageHeight;
                double columnGapMultiplier = isLandscape ? 1.5 : 1.0;
                paragraphs = PdfLayoutAnalyzer.AnalyzeAndGroupPageText(page, pageHeight, columnGapMultiplier);
            }
            catch
            {
                // Fallback handled below
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
            catch
            {
                // Best-effort table detection
            }

            // D. Extract Vector Paths & Geometric Shapes (Dividers, Rectangles, Borders, Diagrams)
            try
            {
                if (page.Paths != null && page.Paths.Count > 0)
                {
                    int extractedShapeCount = 0;
                    for (int pathIdx = 0; pathIdx < page.Paths.Count; pathIdx++)
                    {
                        if (consumedPathIndices.Contains(pathIdx)) continue; // Swallowed by table

                        var path = page.Paths[pathIdx];
                        var bbox = path.GetBoundingRectangle();
                        if (!bbox.HasValue) continue;

                        var b = bbox.Value;
                        if (b.Width <= 0.5 && b.Height <= 0.5) continue;

                        // Ignore full-page border clipping frames
                        if (b.Width >= pageWidth * 0.98 && b.Height >= pageHeight * 0.98) continue;

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

                        // Check if it's a thin horizontal divider line
                        if (canvasH <= 3.5 && canvasW >= 6.0)
                        {
                            var divider = new PdfDividerElement
                            {
                                X = Math.Round(canvasX, 1),
                                Y = Math.Round(canvasY, 1),
                                Width = Math.Round(canvasW, 1),
                                Height = Math.Round(Math.Max(1.0, canvasH), 1),
                                Thickness = Math.Round(Math.Max(1.0, canvasH), 1),
                                ColorHex = path.IsStroked ? strokeHex : (path.IsFilled ? fillHex : "#CBD5E1"),
                                ZIndex = shapeZIndex++
                            };
                            pageModel.Elements.Add(divider);
                            extractedShapeCount++;
                        }
                        else if (canvasW >= 2.0 && canvasH >= 2.0 && (path.IsFilled || path.IsStroked))
                        {
                            // Distinguish large background container cards vs foreground shapes
                            bool isLargeContainer = (canvasW >= 120.0 && canvasH >= 80.0) && (path.IsFilled || (path.IsStroked && !path.IsFilled));
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
                            pageModel.Elements.Add(shape);
                            extractedShapeCount++;
                        }

                        // Cap individual micro-paths per page to 300 to maintain smooth 60fps rendering
                        if (extractedShapeCount >= 300) break;
                    }
                }
            }
            catch
            {
                // Best-effort vector path extraction
            }

            // E. Add Unconsumed Text Elements
            try
            {
                foreach (var para in paragraphs)
                {
                    if (consumedParagraphs.Contains(para)) continue; // Swallowed by table
                    if (string.IsNullOrWhiteSpace(para.Text)) continue;

                    string finalColor = para.ColorHex;
                    // Contrast protection: if text is pure white or near white, ensure it is readable
                    if (string.Equals(finalColor, "#FFFFFF", StringComparison.OrdinalIgnoreCase) ||
                        string.Equals(finalColor, "#FEFEFE", StringComparison.OrdinalIgnoreCase) ||
                        string.Equals(finalColor, "#FDFDFD", StringComparison.OrdinalIgnoreCase))
                    {
                        // If no dark shape is present at this position, flip to high-contrast dark text
                        bool hasDarkShapeUnderneath = pageModel.Elements.OfType<PdfShapeElement>().Any(s =>
                            s.X <= para.CanvasX + 10 &&
                            s.Y <= para.CanvasY + 10 &&
                            s.X + s.Width >= para.CanvasX + para.CanvasWidth - 10 &&
                            s.Y + s.Height >= para.CanvasY + para.CanvasHeight - 10 &&
                            !string.Equals(s.FillColorHex, "Transparent", StringComparison.OrdinalIgnoreCase) &&
                            !string.Equals(s.FillColorHex, "#FFFFFF", StringComparison.OrdinalIgnoreCase));

                        if (!hasDarkShapeUnderneath)
                        {
                            finalColor = "#0F172A";
                        }
                    }

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
                        ZIndex = textZIndex++
                    };
                    pageModel.Elements.Add(textElement);
                }

                // F. Recognize Card & Container Boxes: Group background shapes with their contained text
                var backgroundShapes = pageModel.Elements.OfType<PdfShapeElement>()
                    .Where(s => s.Width >= 60.0 && s.Height >= 30.0 && !string.Equals(s.FillColorHex, "Transparent", StringComparison.OrdinalIgnoreCase))
                    .ToList();

                foreach (var shape in backgroundShapes)
                {
                    var innerTexts = pageModel.Elements.OfType<PdfTextElement>()
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
            catch
            {
                // Fallback: single page text element if grouping encounters unusual font encodings
                if (!string.IsNullOrWhiteSpace(page.Text))
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
                        TextColorHex = "#0F172A",
                        ZIndex = textZIndex++
                    };
                    pageModel.Elements.Add(fallbackText);
                }
            }
        }

        // 3. Extract AcroForm Form Fields (Text Boxes, Checkboxes, Signatures, Dropdowns)
        try
        {
            if (doc != null && doc.TryGetForm(out var form) && form != null && form.Fields != null)
            {
                foreach (var field in form.Fields)
                {
                    if (field.Bounds.HasValue && field.Bounds.Value.Width > 0 && field.Bounds.Value.Height > 0)
                    {
                        var b = field.Bounds.Value;
                        double fX = Math.Max(0, b.Left);
                        double fY = Math.Max(0, pageHeight - b.Top);
                        double fW = Math.Max(20, b.Width);
                        double fH = Math.Max(14, b.Height);

                        string fieldName = field.GetType().GetProperty("Name")?.GetValue(field)?.ToString()
                            ?? field.GetType().GetProperty("FieldName")?.GetValue(field)?.ToString()
                            ?? "FormField";

                        string fieldValue = field.GetType().GetProperty("Value")?.GetValue(field)?.ToString() ?? "";

                        var formElement = new PdfFormFieldElement
                        {
                            X = Math.Round(fX, 1),
                            Y = Math.Round(fY, 1),
                            Width = Math.Round(fW, 1),
                            Height = Math.Round(fH, 1),
                            FieldName = fieldName,
                            Value = fieldValue,
                            DefaultValue = fieldValue,
                            FieldType = MapAcroFieldType(field),
                            IsReadOnly = false,
                            ZIndex = formZIndex++
                        };
                        pageModel.Elements.Add(formElement);
                    }
                }
            }
        }
        catch
        {
            // Best-effort form field extraction
        }

        return pageModel;
    }

    private static byte[]? ExtractImageBytes(IPdfImage img)
    {
        // 1. Try native PNG extraction from PdfPig
        try
        {
            if (img.TryGetPng(out var pngBytes) && pngBytes != null && pngBytes.Length >= 8)
            {
                // Verify standard PNG magic header (0x89, 'P', 'N', 'G')
                if (pngBytes[0] == 0x89 && pngBytes[1] == 0x50 && pngBytes[2] == 0x4E && pngBytes[3] == 0x47)
                {
                    return pngBytes;
                }
            }
        }
        catch { }

        // 2. Try decoding raw image bytes (JPEG / JPEG2000 / WEBP / BMP) via SkiaSharp and encode as clean PNG
        try
        {
            if (!img.RawBytes.IsEmpty && img.RawBytes.Length > 0)
            {
                var rawArray = img.RawBytes.ToArray();
                using var skData = SKData.CreateCopy(rawArray);
                using var skImg = SKImage.FromEncodedData(skData);
                if (skImg != null)
                {
                    using var encoded = skImg.Encode(SKEncodedImageFormat.Png, 100);
                    if (encoded != null && encoded.Size > 0)
                    {
                        return encoded.ToArray();
                    }
                }

                // Check for JPEG direct header
                if (rawArray.Length > 3 && rawArray[0] == 0xFF && rawArray[1] == 0xD8)
                {
                    using var skBmp = SKBitmap.Decode(rawArray);
                    if (skBmp != null)
                    {
                        using var imgFromBmp = SKImage.FromBitmap(skBmp);
                        using var encoded = imgFromBmp.Encode(SKEncodedImageFormat.Png, 100);
                        if (encoded != null && encoded.Size > 0)
                        {
                            return encoded.ToArray();
                        }
                    }
                }
            }
        }
        catch { }

        // 3. Try raw pixel samples conversion via SkiaSharp
        try
        {
            int w = img.WidthInSamples;
            int h = img.HeightInSamples;

            if (w > 0 && h > 0 && img.TryGetBytesAsMemory(out var pixelMem) && pixelMem.Length > 0)
            {
                var rawPixels = pixelMem.ToArray();

                // Case A: 24-bit RGB (3 bytes per pixel)
                if (rawPixels.Length == w * h * 3)
                {
                    using var bitmap = new SKBitmap(w, h, SKColorType.Rgba8888, SKAlphaType.Opaque);
                    var pixelSpan = bitmap.GetPixelSpan();
                    for (int i = 0, p = 0; i + 2 < rawPixels.Length && p + 3 < pixelSpan.Length; i += 3, p += 4)
                    {
                        pixelSpan[p] = rawPixels[i];         // R
                        pixelSpan[p + 1] = rawPixels[i + 1]; // G
                        pixelSpan[p + 2] = rawPixels[i + 2]; // B
                        pixelSpan[p + 3] = 255;             // A
                    }

                    using var image = SKImage.FromBitmap(bitmap);
                    using var encoded = image.Encode(SKEncodedImageFormat.Png, 100);
                    if (encoded != null && encoded.Size > 0)
                    {
                        return encoded.ToArray();
                    }
                }
                // Case B: 8-bit Grayscale (1 byte per pixel)
                else if (rawPixels.Length == w * h)
                {
                    using var bitmap = new SKBitmap(w, h, SKColorType.Rgba8888, SKAlphaType.Opaque);
                    var pixelSpan = bitmap.GetPixelSpan();
                    for (int i = 0, p = 0; i < rawPixels.Length && p + 3 < pixelSpan.Length; i++, p += 4)
                    {
                        byte g = rawPixels[i];
                        pixelSpan[p] = g;
                        pixelSpan[p + 1] = g;
                        pixelSpan[p + 2] = g;
                        pixelSpan[p + 3] = 255;
                    }

                    using var image = SKImage.FromBitmap(bitmap);
                    using var encoded = image.Encode(SKEncodedImageFormat.Png, 100);
                    if (encoded != null && encoded.Size > 0)
                    {
                        return encoded.ToArray();
                    }
                }
                // Case C: 32-bit CMYK (4 bytes per pixel)
                else if (rawPixels.Length == w * h * 4)
                {
                    using var bitmap = new SKBitmap(w, h, SKColorType.Rgba8888, SKAlphaType.Opaque);
                    var pixelSpan = bitmap.GetPixelSpan();
                    for (int i = 0, p = 0; i + 3 < rawPixels.Length && p + 3 < pixelSpan.Length; i += 4, p += 4)
                    {
                        float c = rawPixels[i] / 255f;
                        float m = rawPixels[i + 1] / 255f;
                        float y = rawPixels[i + 2] / 255f;
                        float k = rawPixels[i + 3] / 255f;
                        pixelSpan[p] = (byte)(255 * (1 - c) * (1 - k));
                        pixelSpan[p + 1] = (byte)(255 * (1 - m) * (1 - k));
                        pixelSpan[p + 2] = (byte)(255 * (1 - y) * (1 - k));
                        pixelSpan[p + 3] = 255;
                    }

                    using var image = SKImage.FromBitmap(bitmap);
                    using var encoded = image.Encode(SKEncodedImageFormat.Png, 100);
                    if (encoded != null && encoded.Size > 0)
                    {
                        return encoded.ToArray();
                    }
                }
                // Case D: 1-bit Monochrome (e.g. stamps, fax/signatures)
                else if (img.BitsPerComponent == 1)
                {
                    using var bitmap = new SKBitmap(w, h, SKColorType.Rgba8888, SKAlphaType.Opaque);
                    var pixelSpan = bitmap.GetPixelSpan();
                    int rowStride = (w + 7) / 8;
                    for (int y = 0; y < h; y++)
                    {
                        for (int x = 0; x < w; x++)
                        {
                            int byteIdx = (y * rowStride) + (x / 8);
                            if (byteIdx < rawPixels.Length)
                            {
                                int bitIdx = 7 - (x % 8);
                                bool isWhite = ((rawPixels[byteIdx] >> bitIdx) & 1) == 1;
                                byte val = isWhite ? (byte)255 : (byte)0;
                                int p = (y * w + x) * 4;
                                if (p + 3 < pixelSpan.Length)
                                {
                                    pixelSpan[p] = val;
                                    pixelSpan[p + 1] = val;
                                    pixelSpan[p + 2] = val;
                                    pixelSpan[p + 3] = 255;
                                }
                            }
                        }
                    }

                    using var image = SKImage.FromBitmap(bitmap);
                    using var encoded = image.Encode(SKEncodedImageFormat.Png, 100);
                    if (encoded != null && encoded.Size > 0)
                    {
                        return encoded.ToArray();
                    }
                }
            }
        }
        catch { }

        // 4. Raw bytes fallback
        try
        {
            if (!img.RawBytes.IsEmpty && img.RawBytes.Length > 0)
            {
                return img.RawBytes.ToArray();
            }
        }
        catch { }

        return null;
    }

    private static FormFieldType MapAcroFieldType(object field)
    {
        string typeName = field.GetType().Name.ToLowerInvariant();
        if (typeName.Contains("button") || typeName.Contains("check")) return FormFieldType.Checkbox;
        if (typeName.Contains("radio")) return FormFieldType.Radio;
        if (typeName.Contains("choice") || typeName.Contains("combo") || typeName.Contains("list")) return FormFieldType.Dropdown;
        if (typeName.Contains("sign")) return FormFieldType.Signature;
        return FormFieldType.Text;
    }

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
