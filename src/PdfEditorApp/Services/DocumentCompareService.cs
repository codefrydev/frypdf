using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using PdfEditorApp.Models;
using PdfEditorApp.Models.Elements;

namespace PdfEditorApp.Services;

public interface IDocumentCompareService
{
    DocumentComparisonReport CompareDocuments(PdfDocumentModel baseDoc, PdfDocumentModel comparedDoc);
    Task<DocumentComparisonReport> CompareDocumentsAsync(PdfDocumentModel baseDoc, PdfDocumentModel comparedDoc, CancellationToken ct = default);
}

public class DocumentCompareService : IDocumentCompareService
{
    public Task<DocumentComparisonReport> CompareDocumentsAsync(PdfDocumentModel baseDoc, PdfDocumentModel comparedDoc, CancellationToken ct = default)
    {
        ct.ThrowIfCancellationRequested();
        return Task.Run(() => CompareDocuments(baseDoc, comparedDoc), ct);
    }

    public DocumentComparisonReport CompareDocuments(PdfDocumentModel baseDoc, PdfDocumentModel comparedDoc)
    {
        var report = new DocumentComparisonReport
        {
            BaseDocumentTitle = string.IsNullOrWhiteSpace(baseDoc.Title) ? "Base Document" : baseDoc.Title,
            ComparedDocumentTitle = string.IsNullOrWhiteSpace(comparedDoc.Title) ? "Revised Document" : comparedDoc.Title,
            BasePageCount = baseDoc.Pages.Count,
            ComparedPageCount = comparedDoc.Pages.Count,
            ComparisonTimestamp = DateTime.UtcNow
        };

        // 1. Document Metadata Comparison
        if (baseDoc.Title != comparedDoc.Title)
        {
            report.Differences.Add(new DocumentDifferenceItem
            {
                PageNumber = 0,
                DiffType = CompareDiffType.MetadataModified,
                Description = $"Document Title changed from \"{baseDoc.Title}\" to \"{comparedDoc.Title}\"",
                OldValue = baseDoc.Title,
                NewValue = comparedDoc.Title,
                ElementKindDisplay = "Document Metadata"
            });
        }

        if (baseDoc.Author != comparedDoc.Author)
        {
            report.Differences.Add(new DocumentDifferenceItem
            {
                PageNumber = 0,
                DiffType = CompareDiffType.MetadataModified,
                Description = $"Document Author changed from \"{baseDoc.Author}\" to \"{comparedDoc.Author}\"",
                OldValue = baseDoc.Author,
                NewValue = comparedDoc.Author,
                ElementKindDisplay = "Document Metadata"
            });
        }

        if (baseDoc.SecuritySettings.IsPasswordProtected != comparedDoc.SecuritySettings.IsPasswordProtected)
        {
            report.Differences.Add(new DocumentDifferenceItem
            {
                PageNumber = 0,
                DiffType = CompareDiffType.SecurityModified,
                Description = $"Security settings changed: Protected={comparedDoc.SecuritySettings.IsPasswordProtected}",
                OldValue = baseDoc.SecuritySettings.IsPasswordProtected ? "Password Protected" : "Standard (Unprotected)",
                NewValue = comparedDoc.SecuritySettings.IsPasswordProtected ? "Password Protected" : "Standard (Unprotected)",
                ElementKindDisplay = "Security"
            });
        }

        // 2. Page Count Comparison
        if (baseDoc.Pages.Count != comparedDoc.Pages.Count)
        {
            report.Differences.Add(new DocumentDifferenceItem
            {
                PageNumber = 0,
                DiffType = CompareDiffType.PageCountChanged,
                Description = $"Page count changed from {baseDoc.Pages.Count} to {comparedDoc.Pages.Count}",
                OldValue = $"{baseDoc.Pages.Count} pages",
                NewValue = $"{comparedDoc.Pages.Count} pages",
                ElementKindDisplay = "Document Structure"
            });
        }

        int maxPages = Math.Max(baseDoc.Pages.Count, comparedDoc.Pages.Count);
        for (int p = 0; p < maxPages; p++)
        {
            int pageNum = p + 1;

            if (p >= baseDoc.Pages.Count)
            {
                var addedPage = comparedDoc.Pages[p];
                report.AdditionsCount += Math.Max(1, addedPage.Elements.Count);
                report.Differences.Add(new DocumentDifferenceItem
                {
                    PageNumber = pageNum,
                    DiffType = CompareDiffType.ElementAdded,
                    Description = $"Added new Page {pageNum} with {addedPage.Elements.Count} elements",
                    OldValue = "(None)",
                    NewValue = $"Page {pageNum} ({addedPage.Format})",
                    ElementKindDisplay = "Page"
                });
                continue;
            }

            if (p >= comparedDoc.Pages.Count)
            {
                var removedPage = baseDoc.Pages[p];
                report.DeletionsCount += Math.Max(1, removedPage.Elements.Count);
                report.Differences.Add(new DocumentDifferenceItem
                {
                    PageNumber = pageNum,
                    DiffType = CompareDiffType.ElementRemoved,
                    Description = $"Deleted Page {pageNum} containing {removedPage.Elements.Count} elements",
                    OldValue = $"Page {pageNum}",
                    NewValue = "(Deleted)",
                    ElementKindDisplay = "Page"
                });
                continue;
            }

            var basePage = baseDoc.Pages[p];
            var compPage = comparedDoc.Pages[p];

            // Compare page dimensions and orientation
            if (basePage.Orientation != compPage.Orientation || basePage.Format != compPage.Format ||
                Math.Abs(basePage.Width - compPage.Width) > 1 || Math.Abs(basePage.Height - compPage.Height) > 1)
            {
                report.ModificationsCount++;
                report.Differences.Add(new DocumentDifferenceItem
                {
                    PageNumber = pageNum,
                    DiffType = CompareDiffType.ElementModified,
                    Description = $"Page {pageNum} geometry changed: {basePage.Format} ({basePage.Width:F0}x{basePage.Height:F0}) -> {compPage.Format} ({compPage.Width:F0}x{compPage.Height:F0})",
                    OldValue = $"{basePage.Format} ({basePage.Width:F0}x{basePage.Height:F0})",
                    NewValue = $"{compPage.Format} ({compPage.Width:F0}x{compPage.Height:F0})",
                    ElementKindDisplay = "Page Setup"
                });
            }

            // Compare elements on page
            ComparePageElements(basePage, compPage, pageNum, report);
        }

        return report;
    }

    private static void ComparePageElements(PdfPageModel basePage, PdfPageModel compPage, int pageNum, DocumentComparisonReport report)
    {
        var unmatchedComp = new List<PdfElementBase>(compPage.Elements);
        var matchedCompIds = new HashSet<string>();

        // 1. Pass: Match by Id
        var baseElById = basePage.Elements.ToDictionary(e => e.Id, e => e);
        var compElById = compPage.Elements.ToDictionary(e => e.Id, e => e);

        foreach (var baseEl in basePage.Elements)
        {
            if (compElById.TryGetValue(baseEl.Id, out var compEl))
            {
                matchedCompIds.Add(compEl.Id);
                unmatchedComp.Remove(compEl);
                CompareMatchedPair(baseEl, compEl, pageNum, report);
            }
        }

        // 2. Pass: Spatial/Kind matching for elements without matching ID
        var unmatchedBase = basePage.Elements.Where(e => !matchedCompIds.Contains(e.Id)).ToList();
        var stillUnmatchedBase = new List<PdfElementBase>();

        foreach (var baseEl in unmatchedBase)
        {
            var match = unmatchedComp.FirstOrDefault(c => c.Kind == baseEl.Kind && Math.Abs(c.X - baseEl.X) < 15 && Math.Abs(c.Y - baseEl.Y) < 15);
            if (match != null)
            {
                unmatchedComp.Remove(match);
                CompareMatchedPair(baseEl, match, pageNum, report);
            }
            else
            {
                stillUnmatchedBase.Add(baseEl);
            }
        }

        // 3. Deletions
        foreach (var baseEl in stillUnmatchedBase)
        {
            report.DeletionsCount++;
            string desc = baseEl is PdfTextElement txt ? $": \"{GetSnippet(txt.Text)}\"" : "";
            report.Differences.Add(new DocumentDifferenceItem
            {
                PageNumber = pageNum,
                DiffType = CompareDiffType.ElementRemoved,
                Description = $"Removed {baseEl.Kind} on Page {pageNum}{desc}",
                OldValue = $"{baseEl.Kind} at (X:{baseEl.X:F0}, Y:{baseEl.Y:F0})",
                NewValue = "(Removed)",
                ElementKindDisplay = baseEl.Kind.ToString()
            });
        }

        // 4. Additions
        foreach (var compEl in unmatchedComp)
        {
            report.AdditionsCount++;
            string desc = compEl is PdfTextElement txt ? $": \"{GetSnippet(txt.Text)}\"" : "";
            report.Differences.Add(new DocumentDifferenceItem
            {
                PageNumber = pageNum,
                DiffType = CompareDiffType.ElementAdded,
                Description = $"Added new {compEl.Kind} on Page {pageNum}{desc}",
                OldValue = "(None)",
                NewValue = $"{compEl.Kind} at (X:{compEl.X:F0}, Y:{compEl.Y:F0})",
                ElementKindDisplay = compEl.Kind.ToString()
            });
        }
    }

    private static void CompareMatchedPair(PdfElementBase baseEl, PdfElementBase compEl, int pageNum, DocumentComparisonReport report)
    {
        // Check Text modifications
        if (baseEl is PdfTextElement bTxt && compEl is PdfTextElement cTxt)
        {
            if (bTxt.Text != cTxt.Text)
            {
                report.ModificationsCount++;
                report.Differences.Add(new DocumentDifferenceItem
                {
                    PageNumber = pageNum,
                    DiffType = CompareDiffType.TextModified,
                    Description = $"Modified text on Page {pageNum}: \"{GetSnippet(bTxt.Text)}\" -> \"{GetSnippet(cTxt.Text)}\"",
                    OldValue = bTxt.Text,
                    NewValue = cTxt.Text,
                    ElementKindDisplay = "Text Content"
                });
            }

            if (Math.Abs(bTxt.FontSize - cTxt.FontSize) > 0.5 || bTxt.IsBold != cTxt.IsBold || bTxt.IsItalic != cTxt.IsItalic || bTxt.TextColorHex != cTxt.TextColorHex)
            {
                report.ModificationsCount++;
                report.Differences.Add(new DocumentDifferenceItem
                {
                    PageNumber = pageNum,
                    DiffType = CompareDiffType.FormattingModified,
                    Description = $"Formatting changed for text on Page {pageNum}",
                    OldValue = $"{bTxt.FontFamily} {bTxt.FontSize:F0}pt {bTxt.TextColorHex}",
                    NewValue = $"{cTxt.FontFamily} {cTxt.FontSize:F0}pt {cTxt.TextColorHex}",
                    ElementKindDisplay = "Typography"
                });
            }
        }

        // Check geometry displacement/resizing
        if (Math.Abs(baseEl.X - compEl.X) > 2 || Math.Abs(baseEl.Y - compEl.Y) > 2 ||
            Math.Abs(baseEl.Width - compEl.Width) > 2 || Math.Abs(baseEl.Height - compEl.Height) > 2)
        {
            report.ModificationsCount++;
            report.Differences.Add(new DocumentDifferenceItem
            {
                PageNumber = pageNum,
                DiffType = CompareDiffType.ElementModified,
                Description = $"Moved / resized {compEl.Kind} on Page {pageNum}",
                OldValue = $"Pos ({baseEl.X:F0}, {baseEl.Y:F0}) Size ({baseEl.Width:F0}x{baseEl.Height:F0})",
                NewValue = $"Pos ({compEl.X:F0}, {compEl.Y:F0}) Size ({compEl.Width:F0}x{compEl.Height:F0})",
                ElementKindDisplay = compEl.Kind.ToString()
            });
        }
    }

    private static string GetSnippet(string? text, int max = 25)
    {
        if (string.IsNullOrWhiteSpace(text)) return "";
        string clean = text.Trim().Replace('\r', ' ').Replace('\n', ' ');
        return clean.Length <= max ? clean : $"{clean.Substring(0, max)}...";
    }
}
