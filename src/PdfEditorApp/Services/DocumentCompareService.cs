using System;
using System.Linq;
using PdfEditorApp.Models;
using PdfEditorApp.Models.Elements;

namespace PdfEditorApp.Services;

public interface IDocumentCompareService
{
    DocumentComparisonReport CompareDocuments(PdfDocumentModel baseDoc, PdfDocumentModel comparedDoc);
}

public class DocumentCompareService : IDocumentCompareService
{
    public DocumentComparisonReport CompareDocuments(PdfDocumentModel baseDoc, PdfDocumentModel comparedDoc)
    {
        var report = new DocumentComparisonReport
        {
            BaseDocumentTitle = baseDoc.Title,
            ComparedDocumentTitle = comparedDoc.Title,
            BasePageCount = baseDoc.Pages.Count,
            ComparedPageCount = comparedDoc.Pages.Count,
            ComparisonTimestamp = DateTime.UtcNow
        };

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
                report.AdditionsCount += comparedDoc.Pages[p].Elements.Count;
                report.Differences.Add(new DocumentDifferenceItem
                {
                    PageNumber = pageNum,
                    DiffType = CompareDiffType.ElementAdded,
                    Description = $"Added new Page {pageNum} with {comparedDoc.Pages[p].Elements.Count} elements",
                    OldValue = "(None)",
                    NewValue = $"Page {pageNum}",
                    ElementKindDisplay = "Page"
                });
                continue;
            }

            if (p >= comparedDoc.Pages.Count)
            {
                report.DeletionsCount += baseDoc.Pages[p].Elements.Count;
                report.Differences.Add(new DocumentDifferenceItem
                {
                    PageNumber = pageNum,
                    DiffType = CompareDiffType.ElementRemoved,
                    Description = $"Deleted Page {pageNum} containing {baseDoc.Pages[p].Elements.Count} elements",
                    OldValue = $"Page {pageNum}",
                    NewValue = "(Deleted)",
                    ElementKindDisplay = "Page"
                });
                continue;
            }

            var basePage = baseDoc.Pages[p];
            var comparedPage = comparedDoc.Pages[p];

            // Compare page orientation/format
            if (basePage.Orientation != comparedPage.Orientation || basePage.Format != comparedPage.Format)
            {
                report.ModificationsCount++;
                report.Differences.Add(new DocumentDifferenceItem
                {
                    PageNumber = pageNum,
                    DiffType = CompareDiffType.ElementModified,
                    Description = $"Page {pageNum} layout changed: {basePage.Format} ({basePage.Orientation}) -> {comparedPage.Format} ({comparedPage.Orientation})",
                    OldValue = $"{basePage.Format} {basePage.Orientation}",
                    NewValue = $"{comparedPage.Format} {comparedPage.Orientation}",
                    ElementKindDisplay = "Page Setup"
                });
            }

            // Compare elements by ID and Content
            var baseElMap = basePage.Elements.ToDictionary(e => e.Id, e => e);
            var compElMap = comparedPage.Elements.ToDictionary(e => e.Id, e => e);

            // Deleted elements
            foreach (var baseEl in basePage.Elements)
            {
                if (!compElMap.ContainsKey(baseEl.Id))
                {
                    report.DeletionsCount++;
                    string textDesc = baseEl is PdfTextElement txt ? $": \"{txt.Text}\"" : "";
                    report.Differences.Add(new DocumentDifferenceItem
                    {
                        PageNumber = pageNum,
                        DiffType = CompareDiffType.ElementRemoved,
                        Description = $"Removed {baseEl.Kind} on Page {pageNum}{textDesc}",
                        OldValue = $"{baseEl.Kind} (X:{baseEl.X:F0}, Y:{baseEl.Y:F0})",
                        NewValue = "(Removed)",
                        ElementKindDisplay = baseEl.Kind.ToString()
                    });
                }
            }

            // Added elements
            foreach (var compEl in comparedPage.Elements)
            {
                if (!baseElMap.ContainsKey(compEl.Id))
                {
                    report.AdditionsCount++;
                    string textDesc = compEl is PdfTextElement txt ? $": \"{txt.Text}\"" : "";
                    report.Differences.Add(new DocumentDifferenceItem
                    {
                        PageNumber = pageNum,
                        DiffType = CompareDiffType.ElementAdded,
                        Description = $"Added new {compEl.Kind} on Page {pageNum}{textDesc}",
                        OldValue = "(None)",
                        NewValue = $"{compEl.Kind} (X:{compEl.X:F0}, Y:{compEl.Y:F0})",
                        ElementKindDisplay = compEl.Kind.ToString()
                    });
                }
                else
                {
                    // Existing element: Check geometry & text
                    var baseEl = baseElMap[compEl.Id];
                    if (baseEl is PdfTextElement bTxt && compEl is PdfTextElement cTxt && bTxt.Text != cTxt.Text)
                    {
                        report.ModificationsCount++;
                        report.Differences.Add(new DocumentDifferenceItem
                        {
                            PageNumber = pageNum,
                            DiffType = CompareDiffType.TextModified,
                            Description = $"Modified text on Page {pageNum}",
                            OldValue = bTxt.Text,
                            NewValue = cTxt.Text,
                            ElementKindDisplay = "Text Content"
                        });
                    }
                    else if (Math.Abs(baseEl.X - compEl.X) > 1 || Math.Abs(baseEl.Y - compEl.Y) > 1 || Math.Abs(baseEl.Width - compEl.Width) > 1 || Math.Abs(baseEl.Height - compEl.Height) > 1)
                    {
                        report.ModificationsCount++;
                        report.Differences.Add(new DocumentDifferenceItem
                        {
                            PageNumber = pageNum,
                            DiffType = CompareDiffType.ElementModified,
                            Description = $"Repositioned/Resized {compEl.Kind} on Page {pageNum}",
                            OldValue = $"X:{baseEl.X:F0}, Y:{baseEl.Y:F0}, W:{baseEl.Width:F0}, H:{baseEl.Height:F0}",
                            NewValue = $"X:{compEl.X:F0}, Y:{compEl.Y:F0}, W:{compEl.Width:F0}, H:{compEl.Height:F0}",
                            ElementKindDisplay = compEl.Kind.ToString()
                        });
                    }
                }
            }
        }

        return report;
    }
}
