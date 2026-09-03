using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using PdfEditorApp.Core.Models;
using PdfEditorApp.Core.Models.Elements;
using PdfEditorApp.Models;

namespace PdfEditorApp.Services;

public class AuditIssueItem
{
    public string Id { get; set; } = Guid.NewGuid().ToString("N");
    public string Severity { get; set; } = "Info"; // "Success", "Warning", "Error", "Info"
    public string Category { get; set; } = "General"; // "Accessibility", "Typography", "Images", "PDF/A", "Security", "Structure"
    public string Title { get; set; } = "";
    public string Description { get; set; } = "";
    public int PageIndex { get; set; } = 1;
    public string? ElementId { get; set; }
    public string? RecommendedFix { get; set; }
    public bool CanAutoFix { get; set; }
    public double? MeasuredValue { get; set; }
    public double? RequiredThreshold { get; set; }
}

public class DocumentAuditReport
{
    public int HealthScore { get; set; } = 100;
    public string Grade { get; set; } = "A+";
    public int TotalWordCount { get; set; } = 0;
    public int EstimatedReadingTimeSeconds { get; set; } = 0;
    public int TotalPages { get; set; } = 0;
    public int TotalElements { get; set; } = 0;

    public int TextElementsCount { get; set; } = 0;
    public int ImageElementsCount { get; set; } = 0;
    public int ShapeElementsCount { get; set; } = 0;
    public int TableElementsCount { get; set; } = 0;
    public int ChartElementsCount { get; set; } = 0;
    public int FormFieldsCount { get; set; } = 0;
    public int RedactionsCount { get; set; } = 0;
    public int SignaturesCount { get; set; } = 0;

    public int AccessibilityIssuesCount { get; set; } = 0;
    public int PdfAComplianceScore { get; set; } = 100;
    public int PrintReadyScore { get; set; } = 100;

    public List<string> UniqueFontsUsed { get; set; } = new();
    public List<AuditIssueItem> Issues { get; set; } = new();

    public int WarningsCount => Issues.Count(i => i.Severity == "Warning");
    public int ErrorsCount => Issues.Count(i => i.Severity == "Error");
    public int PassedChecksCount => Issues.Count(i => i.Severity == "Success");

    public string ReadingTimeDisplay =>
        EstimatedReadingTimeSeconds < 60
            ? $"{Math.Max(1, EstimatedReadingTimeSeconds)} sec"
            : $"{EstimatedReadingTimeSeconds / 60} min {EstimatedReadingTimeSeconds % 60} sec";
}

public interface IDocumentAuditService
{
    DocumentAuditReport RunAudit(PdfDocumentModel document);
    Task<DocumentAuditReport> RunAuditAsync(PdfDocumentModel document, IProgress<double>? progress = null, CancellationToken ct = default);
    int AutoFixContrastIssues(PdfDocumentModel document);
    int AutoFixMetadataIssues(PdfDocumentModel document);
    int AutoFixMissingAltText(PdfDocumentModel document);
    int AutoFixAllIssues(PdfDocumentModel document);
}

public class DocumentAuditService : IDocumentAuditService
{
    public Task<DocumentAuditReport> RunAuditAsync(PdfDocumentModel document, IProgress<double>? progress = null, CancellationToken ct = default)
    {
        ct.ThrowIfCancellationRequested();
        return Task.Run(() =>
        {
            progress?.Report(0);
            ct.ThrowIfCancellationRequested();
            var report = RunAudit(document);
            progress?.Report(100);
            return report;
        }, ct);
    }

    public DocumentAuditReport RunAudit(PdfDocumentModel document)
    {
        var report = new DocumentAuditReport
        {
            TotalPages = document.Pages.Count
        };

        var allFonts = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        int totalWords = 0;
        int accessibilityViolations = 0;
        int pdfAPenalties = 0;
        int printPenalties = 0;
        int smallFontCount = 0;
        int placeholderCount = 0;
        int emptyPagesCount = 0;
        int incompleteTablesCount = 0;
        int unsanitizedRedactionsCount = 0;

        // 1. PDF/A Metadata Validation
        if (string.IsNullOrWhiteSpace(document.Title) || document.Title.Equals("Untitled Document", StringComparison.OrdinalIgnoreCase))
        {
            report.Issues.Add(new AuditIssueItem
            {
                Severity = "Warning",
                Category = "PDF/A",
                Title = "Missing Document Title",
                Description = "A descriptive Title metadata entry is required for PDF/A archival and PDF/UA accessibility compliance.",
                PageIndex = 1,
                CanAutoFix = true,
                RecommendedFix = "Set Title to project or file name"
            });
            pdfAPenalties += 10;
        }
        else
        {
            report.Issues.Add(new AuditIssueItem
            {
                Severity = "Success",
                Category = "PDF/A",
                Title = "Document Title Present",
                Description = $"Document title '{document.Title}' conforms to metadata standards.",
                PageIndex = 1
            });
        }

        if (string.IsNullOrWhiteSpace(document.Author))
        {
            report.Issues.Add(new AuditIssueItem
            {
                Severity = "Info",
                Category = "PDF/A",
                Title = "Missing Author Metadata",
                Description = "Author metadata is not configured. Setting Author improves document attribution in enterprise archiving.",
                PageIndex = 1,
                CanAutoFix = true,
                RecommendedFix = "Set Author to current user or company name"
            });
            pdfAPenalties += 5;
        }

        // 2. Page & Element Inspections
        for (int pIdx = 0; pIdx < document.Pages.Count; pIdx++)
        {
            var page = document.Pages[pIdx];
            int pageNum = pIdx + 1;
            string pageBgHex = string.IsNullOrWhiteSpace(page.BackgroundColorHex) ? "#FFFFFF" : page.BackgroundColorHex;

            if (page.Elements.Count == 0)
            {
                report.Issues.Add(new AuditIssueItem
                {
                    Severity = "Warning",
                    Category = "Structure",
                    Title = "Empty Page Detected",
                    Description = $"Page {pageNum} contains zero elements.",
                    PageIndex = pageNum
                });
                emptyPagesCount++;
            }

            foreach (var element in page.Elements)
            {
                report.TotalElements++;

                switch (element)
                {
                    case PdfTextElement textEl:
                        report.TextElementsCount++;
                        if (!string.IsNullOrWhiteSpace(textEl.FontFamily))
                        {
                            allFonts.Add(textEl.FontFamily);
                        }

                        if (!string.IsNullOrWhiteSpace(textEl.Text))
                        {
                            var words = textEl.Text.Split(new[] { ' ', '\r', '\n', '\t' }, StringSplitOptions.RemoveEmptyEntries);
                            totalWords += words.Length;

                            if (textEl.Text.Contains("Lorem ipsum", StringComparison.OrdinalIgnoreCase) ||
                                textEl.Text.Contains("Enter text here", StringComparison.OrdinalIgnoreCase) ||
                                textEl.Text.Contains("Placeholder", StringComparison.OrdinalIgnoreCase))
                            {
                                report.Issues.Add(new AuditIssueItem
                                {
                                    Severity = "Warning",
                                    Category = "Typography",
                                    Title = "Placeholder / Sample Text",
                                    Description = $"Boilerplate placeholder found on Page {pageNum}: \"{GetTextSnippet(textEl.Text)}\".",
                                    PageIndex = pageNum,
                                    ElementId = textEl.Id
                                });
                                placeholderCount++;
                            }
                        }

                        // Font size checks
                        if (textEl.FontSize < 8.0)
                        {
                            report.Issues.Add(new AuditIssueItem
                            {
                                Severity = "Warning",
                                Category = "Typography",
                                Title = "Small Font Size (< 8pt)",
                                Description = $"Text with font size {textEl.FontSize:F1}pt on Page {pageNum} may be illegible when printed.",
                                PageIndex = pageNum,
                                ElementId = textEl.Id,
                                MeasuredValue = textEl.FontSize,
                                RequiredThreshold = 8.0
                            });
                            smallFontCount++;
                            printPenalties += 3;
                        }

                        // Mathematical WCAG 2.1 Contrast Ratio Check
                        string fgColor = string.IsNullOrWhiteSpace(textEl.TextColorHex) ? "#000000" : textEl.TextColorHex;
                        string effectiveBg = !string.IsNullOrWhiteSpace(textEl.BackgroundColorHex) && !textEl.BackgroundColorHex.Equals("Transparent", StringComparison.OrdinalIgnoreCase)
                            ? textEl.BackgroundColorHex
                            : pageBgHex;

                        double contrastRatio = CalculateContrastRatio(fgColor, effectiveBg);
                        bool isLargeText = textEl.FontSize >= 18.0 || (textEl.FontSize >= 14.0 && textEl.IsBold);
                        double requiredAaRatio = isLargeText ? 3.0 : 4.5;
                        double requiredAaaRatio = isLargeText ? 4.5 : 7.0;

                        if (contrastRatio < requiredAaRatio)
                        {
                            accessibilityViolations++;
                            report.Issues.Add(new AuditIssueItem
                            {
                                Severity = "Error",
                                Category = "Accessibility",
                                Title = "WCAG 2.1 AA Contrast Failure",
                                Description = $"Text \"{GetTextSnippet(textEl.Text)}\" on Page {pageNum} has a contrast ratio of {contrastRatio:F2}:1 (Required: {requiredAaRatio:F1}:1).",
                                PageIndex = pageNum,
                                ElementId = textEl.Id,
                                MeasuredValue = contrastRatio,
                                RequiredThreshold = requiredAaRatio,
                                CanAutoFix = true,
                                RecommendedFix = $"Change text color to {GetCompliantColor(fgColor, effectiveBg)}"
                            });
                            accessibilityViolations++;
                        }
                        else if (contrastRatio < requiredAaaRatio)
                        {
                            report.Issues.Add(new AuditIssueItem
                            {
                                Severity = "Info",
                                Category = "Accessibility",
                                Title = "WCAG 2.1 AAA Contrast Advisory",
                                Description = $"Text on Page {pageNum} meets AA standard ({contrastRatio:F2}:1) but falls below enhanced AAA standard ({requiredAaaRatio:F1}:1).",
                                PageIndex = pageNum,
                                ElementId = textEl.Id,
                                MeasuredValue = contrastRatio,
                                RequiredThreshold = requiredAaaRatio
                            });
                        }
                        break;

                    case PdfImageElement imgEl:
                        report.ImageElementsCount++;

                        // Effective DPI Analysis
                        var dims = TryGetImageDimensions(imgEl.ImagePath);
                        if (dims.HasValue && imgEl.Width > 0 && imgEl.Height > 0)
                        {
                            double dpiX = dims.Value.Width / (imgEl.Width / 72.0);
                            double dpiY = dims.Value.Height / (imgEl.Height / 72.0);
                            double minDpi = Math.Min(dpiX, dpiY);

                            if (minDpi < 150.0)
                            {
                                report.Issues.Add(new AuditIssueItem
                                {
                                    Severity = "Warning",
                                    Category = "Images",
                                    Title = "Low Image Resolution (< 150 DPI)",
                                    Description = $"Image on Page {pageNum} has an effective resolution of {minDpi:F0} DPI. Professional printing requires at least 300 DPI.",
                                    PageIndex = pageNum,
                                    ElementId = imgEl.Id,
                                    MeasuredValue = minDpi,
                                    RequiredThreshold = 300.0
                                });
                                printPenalties += 10;
                            }
                            else if (minDpi > 600.0)
                            {
                                report.Issues.Add(new AuditIssueItem
                                {
                                    Severity = "Info",
                                    Category = "Images",
                                    Title = "High Image Resolution (> 600 DPI)",
                                    Description = $"Image on Page {pageNum} is {minDpi:F0} DPI. Consider downsampling to 300 DPI to reduce PDF file size.",
                                    PageIndex = pageNum,
                                    ElementId = imgEl.Id,
                                    MeasuredValue = minDpi,
                                    RequiredThreshold = 600.0
                                });
                            }
                        }
                        else if (string.IsNullOrWhiteSpace(imgEl.ImagePath))
                        {
                            report.Issues.Add(new AuditIssueItem
                            {
                                Severity = "Info",
                                Category = "Images",
                                Title = "Placeholder Image Box",
                                Description = $"Image placeholder on Page {pageNum} has no image file loaded.",
                                PageIndex = pageNum,
                                ElementId = imgEl.Id
                            });
                        }
                        break;

                    case PdfShapeElement shapeEl:
                        report.ShapeElementsCount++;
                        if (shapeEl.ShapeType == ShapeType.RoundedRectangle && shapeEl.Label != null &&
                            (shapeEl.Label.Contains("SIGN", StringComparison.OrdinalIgnoreCase) || shapeEl.Label.Contains("APPROVED", StringComparison.OrdinalIgnoreCase)))
                        {
                            report.SignaturesCount++;
                        }
                        break;

                    case PdfTableElement tblEl:
                        report.TableElementsCount++;
                        if (tblEl.Rows.Count == 0 || tblEl.Headers.Count == 0)
                        {
                            report.Issues.Add(new AuditIssueItem
                            {
                                Severity = "Warning",
                                Category = "Structure",
                                Title = "Incomplete Table Structure",
                                Description = $"Table on Page {pageNum} has zero rows or column headers configured.",
                                PageIndex = pageNum,
                                ElementId = tblEl.Id
                            });
                            incompleteTablesCount++;
                        }
                        break;

                    case PdfChartElement:
                        report.ChartElementsCount++;
                        break;

                    case PdfFormFieldElement formEl:
                        report.FormFieldsCount++;
                        if (formEl.FieldType == FormFieldType.Signature || formEl.FieldType == FormFieldType.SignatureLine)
                        {
                            report.SignaturesCount++;
                        }
                        break;

                    case PdfRedactionElement redEl:
                        report.RedactionsCount++;
                        report.Issues.Add(new AuditIssueItem
                        {
                            Severity = "Warning",
                            Category = "Security",
                            Title = "Unsanitized Redaction Mark",
                            Description = $"Redaction overlay ({redEl.ExemptionCode}) on Page {pageNum} is active. Ensure redactions are permanently burned in before external sharing.",
                            PageIndex = pageNum,
                            ElementId = redEl.Id
                        });
                        unsanitizedRedactionsCount++;
                        break;
                }
            }
        }

        // 3. Positive Summary Checks
        if (allFonts.Count > 0 && allFonts.Count <= 3)
        {
            report.Issues.Add(new AuditIssueItem
            {
                Severity = "Success",
                Category = "Typography",
                Title = "Harmonious Font Hierarchy",
                Description = $"Document utilizes a concise set of {allFonts.Count} font families ({string.Join(", ", allFonts)}).",
                PageIndex = 1
            });
        }
        else if (allFonts.Count > 4)
        {
            report.Issues.Add(new AuditIssueItem
            {
                Severity = "Warning",
                Category = "Typography",
                Title = "Excessive Font Diversity",
                Description = $"Document uses {allFonts.Count} distinct fonts. Standard publishing guidelines recommend 2-3 fonts maximum.",
                PageIndex = 1
            });
        }

        if (document.SecuritySettings.IsPasswordProtected)
        {
            report.Issues.Add(new AuditIssueItem
            {
                Severity = "Success",
                Category = "Security",
                Title = "Document Encryption Active",
                Description = "Document is secured with password protection and restricted permissions.",
                PageIndex = 1
            });
        }

        if (document.Pages.Count > 0 && document.Pages.All(p => p.ShowHeaderFooter))
        {
            report.Issues.Add(new AuditIssueItem
            {
                Severity = "Success",
                Category = "Structure",
                Title = "Structured Pagination & Footers",
                Description = "All document pages have structured headers and sequential footers enabled.",
                PageIndex = 1
            });
        }

        report.UniqueFontsUsed = allFonts.OrderBy(f => f).ToList();
        report.TotalWordCount = totalWords;
        report.EstimatedReadingTimeSeconds = (int)Math.Ceiling(totalWords / 3.3); // ~200 WPM
        report.AccessibilityIssuesCount = accessibilityViolations;
        report.PdfAComplianceScore = Math.Clamp(100 - pdfAPenalties, 0, 100);
        report.PrintReadyScore = Math.Clamp(100 - printPenalties, 0, 100);

        int typographyPenalty = Math.Min(12, (allFonts.Count > 4 ? 4 : 0) + smallFontCount * 1 + placeholderCount * 2);
        int structurePenalty = Math.Min(12, emptyPagesCount * 5 + incompleteTablesCount * 4);
        int accessibilityPenalty = Math.Min(15, accessibilityViolations * 4);
        int securityPenalty = Math.Min(8, unsanitizedRedactionsCount * 3);
        int metadataPenalty = Math.Min(10, pdfAPenalties);

        int totalPenalty = typographyPenalty + structurePenalty + accessibilityPenalty + securityPenalty + metadataPenalty;
        int calculatedScore = Math.Clamp(100 - totalPenalty, 20, 100);
        report.HealthScore = calculatedScore;
        report.Grade = calculatedScore switch
        {
            >= 95 => "A+",
            >= 85 => "A",
            >= 75 => "B",
            >= 60 => "C",
            _ => "Needs Review"
        };

        return report;
    }

    public int AutoFixContrastIssues(PdfDocumentModel document)
    {
        int fixedCount = 0;
        foreach (var page in document.Pages)
        {
            string pageBgHex = string.IsNullOrWhiteSpace(page.BackgroundColorHex) ? "#FFFFFF" : page.BackgroundColorHex;

            foreach (var element in page.Elements)
            {
                if (element is PdfTextElement textEl)
                {
                    string fgColor = string.IsNullOrWhiteSpace(textEl.TextColorHex) ? "#000000" : textEl.TextColorHex;
                    string effectiveBg = !string.IsNullOrWhiteSpace(textEl.BackgroundColorHex) && !textEl.BackgroundColorHex.Equals("Transparent", StringComparison.OrdinalIgnoreCase)
                        ? textEl.BackgroundColorHex
                        : pageBgHex;

                    double ratio = CalculateContrastRatio(fgColor, effectiveBg);
                    bool isLargeText = textEl.FontSize >= 18.0 || (textEl.FontSize >= 14.0 && textEl.IsBold);
                    double requiredRatio = isLargeText ? 3.0 : 4.5;

                    if (ratio < requiredRatio)
                    {
                        textEl.TextColorHex = GetCompliantColor(fgColor, effectiveBg);
                        fixedCount++;
                    }
                }
            }
        }
        return fixedCount;
    }

    public int AutoFixMetadataIssues(PdfDocumentModel document)
    {
        int fixedCount = 0;
        if (string.IsNullOrWhiteSpace(document.Title) || document.Title.Equals("Untitled Document", StringComparison.OrdinalIgnoreCase))
        {
            document.Title = "Document Publication";
            fixedCount++;
        }
        if (string.IsNullOrWhiteSpace(document.Author))
        {
            document.Author = Environment.UserName;
            fixedCount++;
        }
        return fixedCount;
    }

    public int AutoFixMissingAltText(PdfDocumentModel document)
    {
        int fixedCount = 0;
        int imgIndex = 1;
        foreach (var page in document.Pages)
        {
            foreach (var element in page.Elements)
            {
                if (element is PdfImageElement imgEl && (string.IsNullOrWhiteSpace(imgEl.AltText) || imgEl.AltText.Equals("Image", StringComparison.OrdinalIgnoreCase)))
                {
                    imgEl.AltText = $"Document Figure {imgIndex++}";
                    fixedCount++;
                }
            }
        }
        return fixedCount;
    }

    public int AutoFixAllIssues(PdfDocumentModel document)
    {
        int total = 0;
        total += AutoFixContrastIssues(document);
        total += AutoFixMetadataIssues(document);
        total += AutoFixMissingAltText(document);
        return total;
    }

    #region Mathematical WCAG 2.1 Calculations

    public static (double R, double G, double B) ParseRgb(string hex)
    {
        if (string.IsNullOrWhiteSpace(hex)) return (0, 0, 0);
        hex = hex.Trim().TrimStart('#');
        if (hex.Length == 3)
        {
            hex = $"{hex[0]}{hex[0]}{hex[1]}{hex[1]}{hex[2]}{hex[2]}";
        }
        else if (hex.Length == 8)
        {
            hex = hex.Substring(2, 6);
        }
        else if (hex.Length != 6)
        {
            return (0, 0, 0);
        }

        if (int.TryParse(hex.Substring(0, 2), NumberStyles.HexNumber, CultureInfo.InvariantCulture, out int r) &&
            int.TryParse(hex.Substring(2, 2), NumberStyles.HexNumber, CultureInfo.InvariantCulture, out int g) &&
            int.TryParse(hex.Substring(4, 2), NumberStyles.HexNumber, CultureInfo.InvariantCulture, out int b))
        {
            return (r / 255.0, g / 255.0, b / 255.0);
        }
        return (0, 0, 0);
    }

    public static double CalculateRelativeLuminance(string hex)
    {
        var (r, g, b) = ParseRgb(hex);

        static double ToLinear(double c) => (c <= 0.04045) ? c / 12.92 : Math.Pow((c + 0.055) / 1.055, 2.4);

        double rLinear = ToLinear(r);
        double gLinear = ToLinear(g);
        double bLinear = ToLinear(b);

        return 0.2126 * rLinear + 0.7152 * gLinear + 0.0722 * bLinear;
    }

    public static double CalculateContrastRatio(string fgHex, string bgHex)
    {
        double l1 = CalculateRelativeLuminance(fgHex);
        double l2 = CalculateRelativeLuminance(bgHex);

        double lighter = Math.Max(l1, l2);
        double darker = Math.Min(l1, l2);

        return Math.Round((lighter + 0.05) / (darker + 0.05), 2);
    }

    public static string GetCompliantColor(string currentFgHex, string bgHex, double targetRatio = 4.5)
    {
        double bgLuminance = CalculateRelativeLuminance(bgHex);
        return bgLuminance > 0.5 ? "#0F172A" : "#FFFFFF";
    }

    #endregion

    #region Fast Image Dimensions Header Inspector

    public static (int Width, int Height)? TryGetImageDimensions(string? path)
    {
        if (string.IsNullOrWhiteSpace(path) || !File.Exists(path)) return null;

        try
        {
            using var stream = File.OpenRead(path);
            using var reader = new BinaryReader(stream);

            byte[] magic = reader.ReadBytes(8);
            if (magic.Length < 8) return null;

            // PNG magic: 89 50 4E 47 0D 0A 1A 0A
            if (magic[0] == 0x89 && magic[1] == 0x50 && magic[2] == 0x4E && magic[3] == 0x47)
            {
                stream.Seek(16, SeekOrigin.Begin);
                byte[] wBytes = reader.ReadBytes(4);
                byte[] hBytes = reader.ReadBytes(4);
                if (BitConverter.IsLittleEndian)
                {
                    Array.Reverse(wBytes);
                    Array.Reverse(hBytes);
                }
                int w = BitConverter.ToInt32(wBytes, 0);
                int h = BitConverter.ToInt32(hBytes, 0);
                return (w, h);
            }

            // GIF: "GIF87a" or "GIF89a"
            if (magic[0] == 'G' && magic[1] == 'I' && magic[2] == 'F')
            {
                stream.Seek(6, SeekOrigin.Begin);
                int w = reader.ReadUInt16();
                int h = reader.ReadUInt16();
                return (w, h);
            }

            // BMP: "BM"
            if (magic[0] == 'B' && magic[1] == 'M')
            {
                stream.Seek(18, SeekOrigin.Begin);
                int w = reader.ReadInt32();
                int h = Math.Abs(reader.ReadInt32());
                return (w, h);
            }

            // JPEG
            if (magic[0] == 0xFF && magic[1] == 0xD8)
            {
                stream.Seek(2, SeekOrigin.Begin);
                while (stream.Position < stream.Length)
                {
                    byte markerPrefix = reader.ReadByte();
                    if (markerPrefix != 0xFF) break;
                    byte marker = reader.ReadByte();
                    if (marker == 0xC0 || marker == 0xC1 || marker == 0xC2)
                    {
                        reader.ReadUInt16(); // length
                        reader.ReadByte();   // precision
                        int h = (reader.ReadByte() << 8) | reader.ReadByte();
                        int w = (reader.ReadByte() << 8) | reader.ReadByte();
                        return (w, h);
                    }
                    else if (marker == 0xD9 || marker == 0xDA)
                    {
                        break;
                    }
                    else
                    {
                        int length = (reader.ReadByte() << 8) | reader.ReadByte();
                        if (length < 2) break;
                        stream.Seek(length - 2, SeekOrigin.Current);
                    }
                }
            }
        }
        catch
        {
            // Ignore format parse failures
        }

        return null;
    }

    private static string GetTextSnippet(string? text, int maxLen = 30)
    {
        if (string.IsNullOrWhiteSpace(text)) return "";
        string clean = text.Trim().Replace('\r', ' ').Replace('\n', ' ');
        return clean.Length <= maxLen ? clean : $"{clean.Substring(0, maxLen)}...";
    }

    #endregion
}
