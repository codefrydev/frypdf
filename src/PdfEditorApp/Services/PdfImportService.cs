using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using PdfEditorApp.Models;
using PdfEditorApp.Models.Elements;
using PdfEditorApp.Services.Tools;
using UglyToad.PdfPig;
using UglyToad.PdfPig.Content;
using UglyToad.PdfPig.Rendering.Skia;

namespace PdfEditorApp.Services;

public interface IPdfImportService
{
    Task<PdfDocumentModel> ImportPdfAsync(string filePath, string? password = null);
    Task<PdfDocumentModel> ImportPdfFromBytesAsync(byte[] pdfBytes, string title = "Imported_Document.pdf", string? password = null);
    Task<PdfDocumentModel> ImportPdfBytesAsync(byte[] pdfBytes, string title = "Imported_Document.pdf", string? password = null);
}

/// <summary>
/// Professional PDF Import and Deconstruction Engine.
/// Parses any standard or modern PDF file into an editable, multi-layered PdfDocumentModel.
/// Extracts text blocks, fonts, colors, embedded images, form fields, and page dimensions.
/// </summary>
public class PdfImportService : IPdfImportService
{
    public Task<PdfDocumentModel> ImportPdfBytesAsync(byte[] pdfBytes, string title = "Imported_Document.pdf", string? password = null)
        => ImportPdfFromBytesAsync(pdfBytes, title, password);

    public async Task<PdfDocumentModel> ImportPdfAsync(string filePath, string? password = null)
    {
        if (!File.Exists(filePath))
            throw new FileNotFoundException($"PDF file not found: {filePath}");

        byte[] bytes = await File.ReadAllBytesAsync(filePath);
        string title = Path.GetFileName(filePath);
        return await ImportPdfFromBytesAsync(bytes, title, password);
    }

    public async Task<PdfDocumentModel> ImportPdfFromBytesAsync(byte[] pdfBytes, string title = "Imported_Document.pdf", string? password = null)
    {
        return await Task.Run(() =>
        {
            byte[] sanitizedBytes = PdfFileHelper.SanitizePdfBytes(pdfBytes);

            ParsingOptions parsingOptions = new ParsingOptions();
            if (!string.IsNullOrEmpty(password))
            {
                parsingOptions.Password = password;
            }

            PdfDocument? doc = null;
            try
            {
                doc = PdfDocument.Open(sanitizedBytes, parsingOptions);
            }
            catch
            {
                try
                {
                    byte[] repaired = PdfFileHelper.SalvageAndRepairPdfBytes(sanitizedBytes);
                    doc = PdfDocument.Open(repaired, parsingOptions);
                }
                catch
                {
                    // Fallback to original bytes
                    doc = PdfDocument.Open(pdfBytes, parsingOptions);
                }
            }

            using (doc)
            {
                // Register Skia rendering extension on document
                try
                {
                    PdfPigExtensions.AddSkiaPageFactory(doc);
                }
                catch { }

                var model = new PdfDocumentModel
                {
                    Title = string.IsNullOrWhiteSpace(doc.Information.Title) ? title : doc.Information.Title,
                    Author = doc.Information.Author ?? "Unknown Author",
                    Subject = doc.Information.Subject ?? "Imported PDF Document",
                    Keywords = doc.Information.Keywords ?? "",
                    Creator = string.IsNullOrWhiteSpace(doc.Information.Creator) ? "FryPDF" : doc.Information.Creator,
                    Producer = string.IsNullOrWhiteSpace(doc.Information.Producer) ? "codefrydev.in" : doc.Information.Producer,
                    CreatedDate = DateTime.Now,
                    ModifiedDate = DateTime.Now
                };

                int pageCount = doc.NumberOfPages;
                for (int pageNumber = 1; pageNumber <= pageCount; pageNumber++)
                {
                    var page = doc.GetPage(pageNumber);
                    double pageWidth = Math.Max(100, page.Width);
                    double pageHeight = Math.Max(100, page.Height);

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

                    int zIndexCounter = 1;

                    // 1. High-Fidelity Page Background Underlay
                    // We render a crisp base image of the page to ensure 100% visual layout fidelity
                    try
                    {
                        using var pngStream = PdfPigExtensions.GetPageAsPng(doc, pageNumber, 2.75f, 100);
                        if (pngStream != null && pngStream.Length > 0)
                        {
                            byte[] bgBytes = pngStream.ToArray();
                            var bgElement = new PdfImageElement
                            {
                                X = 0,
                                Y = 0,
                                Width = pageWidth,
                                Height = pageHeight,
                                Base64Data = Convert.ToBase64String(bgBytes),
                                Opacity = 1.0,
                                ZIndex = 0,
                                IsLocked = true,
                                CornerRadius = 0,
                                BorderThickness = 0,
                                BorderColorHex = "Transparent",
                                AltText = $"Page {pageNumber} Background Canvas"
                            };
                            pageModel.Elements.Add(bgElement);
                        }
                    }
                    catch
                    {
                        // Background render fallback: clean white canvas
                    }

                    // 2. Extract Embedded Images
                    try
                    {
                        foreach (var img in page.GetImages())
                        {
                            byte[]? imgBytes = null;
                            if (img.TryGetPng(out var pngBytes) && pngBytes != null && pngBytes.Length > 0)
                            {
                                imgBytes = pngBytes;
                            }
                            else if (!img.RawBytes.IsEmpty && img.RawBytes.Length > 0)
                            {
                                imgBytes = img.RawBytes.ToArray();
                            }

                            if (imgBytes != null && imgBytes.Length > 0)
                            {
                                double imgX = Math.Max(0, img.BoundingBox.Left);
                                double imgY = Math.Max(0, pageHeight - img.BoundingBox.Top);
                                double imgW = Math.Max(10, img.BoundingBox.Width);
                                double imgH = Math.Max(10, img.BoundingBox.Height);

                                if (imgW < pageWidth && imgH < pageHeight)
                                {
                                    var imgElement = new PdfImageElement
                                    {
                                        X = Math.Round(imgX, 1),
                                        Y = Math.Round(imgY, 1),
                                        Width = Math.Round(imgW, 1),
                                        Height = Math.Round(imgH, 1),
                                        Base64Data = Convert.ToBase64String(imgBytes),
                                        ZIndex = zIndexCounter++,
                                        CornerRadius = 0,
                                        BorderThickness = 0,
                                        BorderColorHex = "Transparent",
                                        AltText = $"Embedded Image ({Math.Round(imgW):F0}x{Math.Round(imgH):F0})"
                                    };
                                    pageModel.Elements.Add(imgElement);
                                }
                            }
                        }
                    }
                    catch
                    {
                        // Best effort image extraction
                    }

                    // 3. Extract and Group Text Elements
                    try
                    {
                        var textBlocks = GroupPageWordsIntoBlocks(page, pageHeight);
                        foreach (var block in textBlocks)
                        {
                            var textElement = new PdfTextElement
                            {
                                X = Math.Round(block.X, 1),
                                Y = Math.Round(block.Y, 1),
                                Width = Math.Round(Math.Max(30, block.Width), 1),
                                Height = Math.Round(Math.Max(16, block.Height), 1),
                                Text = block.Text,
                                FontSize = Math.Round(Math.Max(6, block.FontSize), 1),
                                FontFamily = block.FontFamily ?? "Helvetica",
                                IsBold = block.IsBold,
                                IsItalic = block.IsItalic,
                                TextColorHex = block.ColorHex ?? "#0F172A",
                                ZIndex = zIndexCounter++
                            };
                            pageModel.Elements.Add(textElement);
                        }
                    }
                    catch
                    {
                        // Fallback: extract single text block if word grouping encounters non-standard font structures
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
                                FontFamily = "Helvetica",
                                TextColorHex = "#0F172A",
                                ZIndex = zIndexCounter++
                            };
                            pageModel.Elements.Add(fallbackText);
                        }
                    }

                    // 4. Extract AcroForm Interactive Fields if any
                    try
                    {
                        if (doc.TryGetForm(out var form) && form != null && form.Fields != null)
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
                                        ZIndex = zIndexCounter++
                                    };
                                    pageModel.Elements.Add(formElement);
                                }
                            }
                        }
                    }
                    catch
                    {
                        // Best effort AcroForm extraction
                    }

                    model.Pages.Add(pageModel);
                }

                return model;
            }
        });
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

    private static PageFormat DeterminePageFormat(double width, double height)
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

    private class ExtractedTextBlock
    {
        public double X { get; set; }
        public double Y { get; set; }
        public double Width { get; set; }
        public double Height { get; set; }
        public string Text { get; set; } = string.Empty;
        public double FontSize { get; set; } = 11;
        public string? FontFamily { get; set; } = "Helvetica";
        public bool IsBold { get; set; }
        public bool IsItalic { get; set; }
        public string? ColorHex { get; set; } = "#0F172A";
    }

    private static List<ExtractedTextBlock> GroupPageWordsIntoBlocks(Page page, double pageHeight)
    {
        var blocks = new List<ExtractedTextBlock>();
        var words = page.GetWords().ToList();
        if (words.Count == 0) return blocks;

        // Sort words in reading order: top-to-bottom, left-to-right
        var sortedWords = words
            .OrderByDescending(w => w.BoundingBox.Top)
            .ThenBy(w => w.BoundingBox.Left)
            .ToList();

        // Group into logical lines / blocks with horizontal proximity check
        List<Word> currentLine = new List<Word>();
        double currentLineTop = sortedWords[0].BoundingBox.Top;
        double currentLineBottom = sortedWords[0].BoundingBox.Bottom;

        foreach (var word in sortedWords)
        {
            double wordMidY = (word.BoundingBox.Top + word.BoundingBox.Bottom) / 2.0;
            double lineMidY = (currentLineTop + currentLineBottom) / 2.0;
            double wordHeight = word.BoundingBox.Height;

            bool sameLineY = currentLine.Count == 0 || Math.Abs(wordMidY - lineMidY) <= Math.Max(3.5, wordHeight * 0.65);
            bool nearHorizontally = true;
            if (currentLine.Count > 0)
            {
                double lastRight = currentLine.Max(w => w.BoundingBox.Right);
                double gap = word.BoundingBox.Left - lastRight;
                // If the word is across a multi-column gap or positioned before the line starts, break to separate block
                if (gap > Math.Max(24.0, wordHeight * 2.5) || (word.BoundingBox.Right < currentLine.Min(w => w.BoundingBox.Left) - 10))
                {
                    nearHorizontally = false;
                }
            }

            if (sameLineY && nearHorizontally)
            {
                currentLine.Add(word);
                currentLineTop = Math.Max(currentLineTop, word.BoundingBox.Top);
                currentLineBottom = Math.Min(currentLineBottom, word.BoundingBox.Bottom);
            }
            else
            {
                var block = CreateBlockFromWords(currentLine, pageHeight);
                if (block != null) blocks.Add(block);

                currentLine = new List<Word> { word };
                currentLineTop = word.BoundingBox.Top;
                currentLineBottom = word.BoundingBox.Bottom;
            }
        }

        if (currentLine.Count > 0)
        {
            var block = CreateBlockFromWords(currentLine, pageHeight);
            if (block != null) blocks.Add(block);
        }

        return blocks;
    }

    private static ExtractedTextBlock? CreateBlockFromWords(List<Word> lineWords, double pageHeight)
    {
        if (lineWords == null || lineWords.Count == 0) return null;

        var ordered = lineWords.OrderBy(w => w.BoundingBox.Left).ToList();
        var sb = new StringBuilder();
        double minX = ordered.Min(w => w.BoundingBox.Left);
        double maxX = ordered.Max(w => w.BoundingBox.Right);
        double topY = ordered.Max(w => w.BoundingBox.Top);
        double bottomY = ordered.Min(w => w.BoundingBox.Bottom);

        for (int i = 0; i < ordered.Count; i++)
        {
            if (i > 0) sb.Append(" ");
            sb.Append(ordered[i].Text);
        }

        string text = sb.ToString().Trim();
        if (string.IsNullOrWhiteSpace(text)) return null;

        var firstWord = ordered[0];
        var firstLetter = firstWord.Letters.FirstOrDefault();

        double fontSize = 11;
        string fontFamily = "Helvetica";
        bool isBold = false;
        bool isItalic = false;
        string colorHex = "#0F172A";

        if (firstLetter != null)
        {
            fontSize = Math.Max(7, firstLetter.PointSize);
            if (!string.IsNullOrEmpty(firstLetter.FontName))
            {
                string fn = firstLetter.FontName.ToLowerInvariant();
                isBold = fn.Contains("bold") || fn.Contains("black") || fn.Contains("heavy");
                isItalic = fn.Contains("italic") || fn.Contains("oblique");

                if (fn.Contains("arial")) fontFamily = "Arial";
                else if (fn.Contains("times")) fontFamily = "Times New Roman";
                else if (fn.Contains("courier")) fontFamily = "Courier New";
                else if (fn.Contains("segoe")) fontFamily = "Segoe UI";
                else if (fn.Contains("roboto")) fontFamily = "Roboto";
                else if (fn.Contains("inter")) fontFamily = "Inter";
                else if (fn.Contains("nirmala") || fn.Contains("mangal") || fn.Contains("devanagari")) fontFamily = "Nirmala UI";
            }

            if (firstLetter.Color != null)
            {
                var (r, g, b) = firstLetter.Color.ToRGBValues();
                colorHex = $"#{(int)(r * 255):X2}{(int)(g * 255):X2}{(int)(b * 255):X2}";
            }
        }

        double canvasY = Math.Max(0, pageHeight - topY);
        double canvasX = Math.Max(0, minX);
        double width = Math.Max(20, maxX - minX);
        double height = Math.Max(12, topY - bottomY);

        return new ExtractedTextBlock
        {
            X = canvasX,
            Y = canvasY,
            Width = width,
            Height = height,
            Text = text,
            FontSize = fontSize,
            FontFamily = fontFamily,
            IsBold = isBold,
            IsItalic = isItalic,
            ColorHex = colorHex
        };
    }
}
