using System;
using System.Linq;
using PdfEditorApp.Core.Models;
using PdfEditorApp.Core.Models.Elements;
using PdfEditorApp.Models;
using PdfEditorApp.Services;
using Xunit;

namespace PdfEditorApp.Tests;

public class DocumentCompareServiceTests
{
    [Fact]
    public void CompareDocuments_IdenticalDocs_ReportsZeroDifferences()
    {
        // Arrange
        var service = new DocumentCompareService();
        var doc1 = new PdfDocumentModel { Title = "Budget 2026", Author = "Finance Team" };
        var page1 = new PdfPageModel { PageNumber = 1, Width = 600, Height = 800 };
        page1.Elements.Add(new PdfTextElement { Id = "t1", Text = "Revenue $1M", X = 50, Y = 50, Width = 200, Height = 30 });
        doc1.Pages.Add(page1);

        var doc2 = new PdfDocumentModel { Title = "Budget 2026", Author = "Finance Team" };
        var page2 = new PdfPageModel { PageNumber = 1, Width = 600, Height = 800 };
        page2.Elements.Add(new PdfTextElement { Id = "t1", Text = "Revenue $1M", X = 50, Y = 50, Width = 200, Height = 30 });
        doc2.Pages.Add(page2);

        // Act
        var report = service.CompareDocuments(doc1, doc2);

        // Assert
        Assert.Equal(0, report.TotalDifferencesCount);
        Assert.Equal(0, report.AdditionsCount);
        Assert.Equal(0, report.DeletionsCount);
        Assert.Equal(0, report.ModificationsCount);
    }

    [Fact]
    public void CompareDocuments_DetectsMetadataAndSecurityChanges()
    {
        // Arrange
        var service = new DocumentCompareService();
        var doc1 = new PdfDocumentModel
        {
            Title = "Draft V1.pdf",
            Author = "Intern",
            SecuritySettings = new PdfSecuritySettings { IsPasswordProtected = false }
        };
        doc1.Pages.Add(new PdfPageModel { PageNumber = 1 });

        var doc2 = new PdfDocumentModel
        {
            Title = "Final V2.pdf",
            Author = "Lead Director",
            SecuritySettings = new PdfSecuritySettings { IsPasswordProtected = true, OpenPassword = "123" }
        };
        doc2.Pages.Add(new PdfPageModel { PageNumber = 1 });

        // Act
        var report = service.CompareDocuments(doc1, doc2);

        // Assert
        Assert.Contains(report.Differences, d => d.DiffType == CompareDiffType.MetadataModified && d.Description.Contains("Title"));
        Assert.Contains(report.Differences, d => d.DiffType == CompareDiffType.MetadataModified && d.Description.Contains("Author"));
        Assert.Contains(report.Differences, d => d.DiffType == CompareDiffType.SecurityModified);
    }

    [Fact]
    public void CompareDocuments_DetectsTextModificationsAndFormattingChanges()
    {
        // Arrange
        var service = new DocumentCompareService();
        var doc1 = new PdfDocumentModel { Title = "Memo" };
        var page1 = new PdfPageModel { PageNumber = 1 };
        page1.Elements.Add(new PdfTextElement
        {
            Id = "txt-content",
            Text = "Draft policy v1.0",
            FontSize = 12,
            TextColorHex = "#000000"
        });
        doc1.Pages.Add(page1);

        var doc2 = new PdfDocumentModel { Title = "Memo" };
        var page2 = new PdfPageModel { PageNumber = 1 };
        page2.Elements.Add(new PdfTextElement
        {
            Id = "txt-content",
            Text = "Approved policy v2.0",
            FontSize = 14,
            TextColorHex = "#0F6CBD"
        });
        doc2.Pages.Add(page2);

        // Act
        var report = service.CompareDocuments(doc1, doc2);

        // Assert
        Assert.Contains(report.Differences, d => d.DiffType == CompareDiffType.TextModified && d.OldValue == "Draft policy v1.0" && d.NewValue == "Approved policy v2.0");
        Assert.Contains(report.Differences, d => d.DiffType == CompareDiffType.FormattingModified);
    }

    [Fact]
    public void CompareDocuments_DetectsPageAdditionsAndDeletions()
    {
        // Arrange
        var service = new DocumentCompareService();
        var doc1 = new PdfDocumentModel { Title = "Report" };
        doc1.Pages.Add(new PdfPageModel { PageNumber = 1 });
        doc1.Pages.Add(new PdfPageModel { PageNumber = 2 });

        var doc2 = new PdfDocumentModel { Title = "Report" };
        doc2.Pages.Add(new PdfPageModel { PageNumber = 1 });
        doc2.Pages.Add(new PdfPageModel { PageNumber = 2 });
        doc2.Pages.Add(new PdfPageModel { PageNumber = 3 }); // Added 3rd page

        // Act
        var report = service.CompareDocuments(doc1, doc2);

        // Assert
        Assert.Contains(report.Differences, d => d.DiffType == CompareDiffType.PageCountChanged);
        Assert.Contains(report.Differences, d => d.DiffType == CompareDiffType.ElementAdded && d.PageNumber == 3);
    }
}
