using System;
using System.Collections.Generic;
using System.Linq;
using PdfEditorApp.Models;
using PdfEditorApp.Models.Elements;

namespace PdfEditorApp.Services;

public class AuditIssueItem
{
    public string Severity { get; set; } = "Info"; // "Success", "Warning", "Error", "Info"
    public string Category { get; set; } = "General"; // "Typography", "Images", "Accessibility", "Security", "Structure"
    public string Title { get; set; } = "";
    public string Description { get; set; } = "";
    public int PageIndex { get; set; } = 1;
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
}

public class DocumentAuditService : IDocumentAuditService
{
    public DocumentAuditReport RunAudit(PdfDocumentModel document)
    {
        var report = new DocumentAuditReport
        {
            TotalPages = document.Pages.Count
        };

        var allFonts = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        int totalWords = 0;
        int penaltyScore = 0;

        for (int pIdx = 0; pIdx < document.Pages.Count; pIdx++)
        {
            var page = document.Pages[pIdx];
            int pageNum = pIdx + 1;

            if (page.Elements.Count == 0)
            {
                report.Issues.Add(new AuditIssueItem
                {
                    Severity = "Warning",
                    Category = "Structure",
                    Title = $"Empty Page Detected",
                    Description = $"Page {pageNum} contains no text, shape, or graphic elements.",
                    PageIndex = pageNum
                });
                penaltyScore += 5;
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
                                    Description = $"Unedited boilerplate found in text box on Page {pageNum}.",
                                    PageIndex = pageNum
                                });
                                penaltyScore += 3;
                            }
                        }

                        if (textEl.FontSize < 8.0)
                        {
                            report.Issues.Add(new AuditIssueItem
                            {
                                Severity = "Warning",
                                Category = "Typography",
                                Title = "Small Font Size (< 8pt)",
                                Description = $"Text with font size {textEl.FontSize:F1}pt may be difficult to read when printed.",
                                PageIndex = pageNum
                            });
                            penaltyScore += 2;
                        }

                        // Contrast check for light text on white backgrounds
                        if (page.BackgroundColorHex.Equals("#FFFFFF", StringComparison.OrdinalIgnoreCase) ||
                            page.BackgroundColorHex.Equals("#FFF", StringComparison.OrdinalIgnoreCase))
                        {
                            if (textEl.TextColorHex.Equals("#FFFFFF", StringComparison.OrdinalIgnoreCase) ||
                                textEl.TextColorHex.Equals("#FEFEFE", StringComparison.OrdinalIgnoreCase) ||
                                textEl.TextColorHex.StartsWith("#F", StringComparison.OrdinalIgnoreCase))
                            {
                                report.Issues.Add(new AuditIssueItem
                                {
                                    Severity = "Error",
                                    Category = "Accessibility",
                                    Title = "Low Text Contrast (WCAG)",
                                    Description = $"Light text on white canvas background on Page {pageNum} violates WCAG AA readability standards.",
                                    PageIndex = pageNum
                                });
                                penaltyScore += 8;
                            }
                        }
                        break;

                    case PdfImageElement imgEl:
                        report.ImageElementsCount++;
                        if (string.IsNullOrWhiteSpace(imgEl.ImagePath))
                        {
                            report.Issues.Add(new AuditIssueItem
                            {
                                Severity = "Info",
                                Category = "Images",
                                Title = "Placeholder Image Element",
                                Description = $"Graphic box on Page {pageNum} is using a placeholder rendering.",
                                PageIndex = pageNum
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
                                Title = "Empty Table Structure",
                                Description = $"Table on Page {pageNum} has zero rows or columns configured.",
                                PageIndex = pageNum
                            });
                            penaltyScore += 4;
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
                            Severity = "Info",
                            Category = "Security",
                            Title = "Active Redaction Overlay",
                            Description = $"Redaction marked ({redEl.ExemptionCode}) on Page {pageNum}. Ensure redactions are permanently applied before public distribution.",
                            PageIndex = pageNum
                        });
                        break;
                }
            }
        }

        // Summary Positive Checks
        if (allFonts.Count > 0 && allFonts.Count <= 3)
        {
            report.Issues.Add(new AuditIssueItem
            {
                Severity = "Success",
                Category = "Typography",
                Title = "Consistent Font Hierarchy",
                Description = $"Document utilizes a clean palette of {allFonts.Count} font families ({string.Join(", ", allFonts)}).",
                PageIndex = 1
            });
        }
        else if (allFonts.Count > 4)
        {
            report.Issues.Add(new AuditIssueItem
            {
                Severity = "Warning",
                Category = "Typography",
                Title = "Excessive Font Variations",
                Description = $"Document uses {allFonts.Count} distinct fonts. Standard publishing guidelines recommend 2-3 fonts maximum.",
                PageIndex = 1
            });
            penaltyScore += 4;
        }

        if (document.SecuritySettings.IsPasswordProtected)
        {
            report.Issues.Add(new AuditIssueItem
            {
                Severity = "Success",
                Category = "Security",
                Title = "Document Encryption Configured",
                Description = "Document is protected with password security and restricted permissions.",
                PageIndex = 1
            });
        }

        if (document.Pages.All(p => p.ShowHeaderFooter))
        {
            report.Issues.Add(new AuditIssueItem
            {
                Severity = "Success",
                Category = "Structure",
                Title = "Pagination & Header Continuity",
                Description = "All document pages have structured headers and sequential footers enabled.",
                PageIndex = 1
            });
        }

        report.UniqueFontsUsed = allFonts.OrderBy(f => f).ToList();
        report.TotalWordCount = totalWords;
        report.EstimatedReadingTimeSeconds = (int)Math.Ceiling(totalWords / 3.3); // ~200 WPM

        int calculatedScore = Math.Clamp(100 - penaltyScore, 20, 100);
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
}
