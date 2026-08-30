using System;
using System.Collections.Generic;
using System.Linq;
using PdfEditorApp.Models;

namespace PdfEditorApp.Services;

public enum PageFilterTarget
{
    All,
    EvenPages,
    OddPages,
    LandscapePages,
    PortraitPages
}

public interface IPageOrganizerService
{
    List<PdfDocumentModel> SplitEveryNPages(PdfDocumentModel doc, int pagesPerDoc);
    List<PdfDocumentModel> SplitByRanges(PdfDocumentModel doc, string rangeExpression);
    PdfDocumentModel ExtractPages(PdfDocumentModel doc, IEnumerable<int> pageIndexes, string? newTitle = null);
    PdfDocumentModel MergeDocuments(IEnumerable<PdfDocumentModel> docs, string mergedTitle);
    int BatchRotatePages(PdfDocumentModel doc, PageFilterTarget target, int angleDegrees);
    List<int> ParsePageRanges(string rangeExpression, int totalPages);
}

public class PageOrganizerService : IPageOrganizerService
{
    public List<PdfDocumentModel> SplitEveryNPages(PdfDocumentModel doc, int pagesPerDoc)
    {
        if (doc.Pages.Count == 0) return new List<PdfDocumentModel>();
        int interval = Math.Max(1, pagesPerDoc);
        var result = new List<PdfDocumentModel>();

        int totalPages = doc.Pages.Count;
        int partIndex = 1;

        for (int i = 0; i < totalPages; i += interval)
        {
            int count = Math.Min(interval, totalPages - i);
            var partDoc = new PdfDocumentModel
            {
                Title = $"{System.IO.Path.GetFileNameWithoutExtension(doc.Title)}_Part{partIndex}.pdf",
                Author = doc.Author,
                Subject = $"{doc.Subject} (Part {partIndex})",
                CreatedDate = doc.CreatedDate,
                SecuritySettings = doc.SecuritySettings
            };

            for (int j = 0; j < count; j++)
            {
                var page = doc.Pages[i + j];
                var clonedPage = (PdfPageModel)page.Clone();
                clonedPage.PageNumber = j + 1;
                partDoc.Pages.Add(clonedPage);
            }

            result.Add(partDoc);
            partIndex++;
        }

        return result;
    }

    public List<PdfDocumentModel> SplitByRanges(PdfDocumentModel doc, string rangeExpression)
    {
        var result = new List<PdfDocumentModel>();
        if (doc.Pages.Count == 0 || string.IsNullOrWhiteSpace(rangeExpression)) return result;

        var parts = rangeExpression.Split(new[] { ',', ';' }, StringSplitOptions.RemoveEmptyEntries);
        int partIndex = 1;

        foreach (var part in parts)
        {
            var pageIndexes = ParseSingleRange(part.Trim(), doc.Pages.Count);
            if (pageIndexes.Count == 0) continue;

            var partDoc = new PdfDocumentModel
            {
                Title = $"{System.IO.Path.GetFileNameWithoutExtension(doc.Title)}_Range_{part.Trim().Replace(' ', '_')}.pdf",
                Author = doc.Author,
                Subject = $"{doc.Subject} (Pages {part.Trim()})",
                CreatedDate = doc.CreatedDate
            };

            int pageNum = 1;
            foreach (int pIdx in pageIndexes)
            {
                var page = doc.Pages[pIdx];
                var clonedPage = (PdfPageModel)page.Clone();
                clonedPage.PageNumber = pageNum++;
                partDoc.Pages.Add(clonedPage);
            }

            result.Add(partDoc);
            partIndex++;
        }

        return result;
    }

    public PdfDocumentModel ExtractPages(PdfDocumentModel doc, IEnumerable<int> pageIndexes, string? newTitle = null)
    {
        var extractedDoc = new PdfDocumentModel
        {
            Title = newTitle ?? $"{System.IO.Path.GetFileNameWithoutExtension(doc.Title)}_Extracted.pdf",
            Author = doc.Author,
            Subject = $"Extracted pages from {doc.Title}",
            CreatedDate = DateTime.Now,
            SecuritySettings = doc.SecuritySettings
        };

        int pageNum = 1;
        foreach (int pIdx in pageIndexes)
        {
            if (pIdx >= 0 && pIdx < doc.Pages.Count)
            {
                var page = doc.Pages[pIdx];
                var clonedPage = (PdfPageModel)page.Clone();
                clonedPage.PageNumber = pageNum++;
                extractedDoc.Pages.Add(clonedPage);
            }
        }

        return extractedDoc;
    }

    public PdfDocumentModel MergeDocuments(IEnumerable<PdfDocumentModel> docs, string mergedTitle)
    {
        var mergedDoc = new PdfDocumentModel
        {
            Title = mergedTitle,
            CreatedDate = DateTime.Now
        };

        int pageNum = 1;
        foreach (var doc in docs)
        {
            if (string.IsNullOrWhiteSpace(mergedDoc.Author) && !string.IsNullOrWhiteSpace(doc.Author))
            {
                mergedDoc.Author = doc.Author;
            }

            foreach (var page in doc.Pages)
            {
                var clonedPage = (PdfPageModel)page.Clone();
                clonedPage.PageNumber = pageNum++;
                mergedDoc.Pages.Add(clonedPage);
            }
        }

        return mergedDoc;
    }

    public int BatchRotatePages(PdfDocumentModel doc, PageFilterTarget target, int angleDegrees)
    {
        int count = 0;
        for (int i = 0; i < doc.Pages.Count; i++)
        {
            int pageNum = i + 1;
            var page = doc.Pages[i];

            bool shouldRotate = target switch
            {
                PageFilterTarget.All => true,
                PageFilterTarget.EvenPages => (pageNum % 2 == 0),
                PageFilterTarget.OddPages => (pageNum % 2 != 0),
                PageFilterTarget.LandscapePages => page.Width > page.Height,
                PageFilterTarget.PortraitPages => page.Height >= page.Width,
                _ => true
            };

            if (shouldRotate)
            {
                page.RotationAngle = (page.RotationAngle + angleDegrees + 360) % 360;
                count++;
            }
        }
        return count;
    }

    public List<int> ParsePageRanges(string rangeExpression, int totalPages)
    {
        var result = new HashSet<int>();
        if (string.IsNullOrWhiteSpace(rangeExpression) || totalPages <= 0) return result.ToList();

        var parts = rangeExpression.Split(new[] { ',', ';' }, StringSplitOptions.RemoveEmptyEntries);
        foreach (var part in parts)
        {
            foreach (var idx in ParseSingleRange(part.Trim(), totalPages))
            {
                result.Add(idx);
            }
        }

        return result.OrderBy(i => i).ToList();
    }

    private static List<int> ParseSingleRange(string range, int totalPages)
    {
        var list = new List<int>();
        if (string.IsNullOrWhiteSpace(range)) return list;

        if (range.Contains('-'))
        {
            var bounds = range.Split('-');
            if (bounds.Length == 2 &&
                int.TryParse(bounds[0].Trim(), out int start) &&
                int.TryParse(bounds[1].Trim(), out int end))
            {
                int min = Math.Clamp(Math.Min(start, end), 1, totalPages);
                int max = Math.Clamp(Math.Max(start, end), 1, totalPages);
                for (int i = min; i <= max; i++)
                {
                    list.Add(i - 1);
                }
            }
        }
        else if (int.TryParse(range.Trim(), out int single))
        {
            if (single >= 1 && single <= totalPages)
            {
                list.Add(single - 1);
            }
        }

        return list;
    }
}
