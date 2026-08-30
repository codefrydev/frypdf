using System;
using System.IO;
using System.Linq;
using PdfEditorApp.Models;
using PdfEditorApp.Models.Elements;
using PdfEditorApp.Services;
using Xunit;

namespace PdfEditorApp.Tests;

public class DocumentAuditServiceTests
{
    [Fact]
    public void Wcag21_RelativeLuminanceAndContrastRatio_CalculatesAccurately()
    {
        // Black luminance is 0.0
        double blackLum = DocumentAuditService.CalculateRelativeLuminance("#000000");
        Assert.Equal(0.0, blackLum, 3);

        // White luminance is 1.0
        double whiteLum = DocumentAuditService.CalculateRelativeLuminance("#FFFFFF");
        Assert.Equal(1.0, whiteLum, 3);

        // Black on White contrast ratio is 21.0:1
        double maxContrast = DocumentAuditService.CalculateContrastRatio("#000000", "#FFFFFF");
        Assert.Equal(21.0, maxContrast);

        // White on White contrast ratio is 1.0:1
        double minContrast = DocumentAuditService.CalculateContrastRatio("#FFFFFF", "#FFFFFF");
        Assert.Equal(1.0, minContrast);

        // Gray #777777 on White #FFFFFF is ~4.47:1 (fails normal AA 4.5:1, passes large text AA 3.0:1)
        double grayContrast = DocumentAuditService.CalculateContrastRatio("#777777", "#FFFFFF");
        Assert.True(grayContrast >= 4.4 && grayContrast <= 4.55);

        // Fluent Brand Blue #0F6CBD on White #FFFFFF passes AA
        double brandBlueContrast = DocumentAuditService.CalculateContrastRatio("#0F6CBD", "#FFFFFF");
        Assert.True(brandBlueContrast >= 4.5);
    }

    [Fact]
    public void DocumentAuditService_DetectsLowContrastAndAutoFixes()
    {
        // Arrange
        var auditService = new DocumentAuditService();
        var doc = new PdfDocumentModel { Title = "Quarterly Compliance Report", Author = "Financial Audit Team" };
        var page = new PdfPageModel { Width = 595, Height = 842, BackgroundColorHex = "#FFFFFF" };

        // Low contrast text element (white text on white page)
        var lowContrastText = new PdfTextElement
        {
            Id = "txt-low-contrast",
            X = 50,
            Y = 50,
            Width = 200,
            Height = 30,
            Text = "Hidden Low Contrast Disclaimer",
            FontSize = 10,
            TextColorHex = "#FFFFFF",
            BackgroundColorHex = "Transparent"
        };
        page.Elements.Add(lowContrastText);
        doc.Pages.Add(page);

        // Act - Initial Audit
        var report1 = auditService.RunAudit(doc);

        // Assert - Error detected
        var contrastIssue = report1.Issues.FirstOrDefault(i => i.Category == "Accessibility" && i.Severity == "Error");
        Assert.NotNull(contrastIssue);
        Assert.Contains("WCAG 2.1 AA Contrast Failure", contrastIssue.Title);
        Assert.True(contrastIssue.CanAutoFix);

        // Act - Auto-Fix
        int fixedCount = auditService.AutoFixContrastIssues(doc);
        Assert.Equal(1, fixedCount);
        Assert.Equal("#0F172A", lowContrastText.TextColorHex); // Dark high-contrast replacement

        // Act - Re-Audit
        var report2 = auditService.RunAudit(doc);
        var remainingErrors = report2.Issues.Where(i => i.Category == "Accessibility" && i.Severity == "Error");
        Assert.Empty(remainingErrors);
        Assert.True(report2.HealthScore > report1.HealthScore);
    }

    [Fact]
    public void DocumentAuditService_ValidatesPdfAMetadataAndAutoFixes()
    {
        // Arrange
        var auditService = new DocumentAuditService();
        var doc = new PdfDocumentModel
        {
            Title = "", // Missing Title
            Author = "" // Missing Author
        };
        var page = new PdfPageModel { Width = 595, Height = 842 };
        page.Elements.Add(new PdfTextElement { X = 50, Y = 50, Text = "Valid Content", FontSize = 12, TextColorHex = "#000000" });
        doc.Pages.Add(page);

        // Act
        var report1 = auditService.RunAudit(doc);

        // Assert - Missing title issue found
        var titleIssue = report1.Issues.FirstOrDefault(i => i.Title == "Missing Document Title");
        Assert.NotNull(titleIssue);
        Assert.True(report1.PdfAComplianceScore < 100);

        // Act - Auto Fix Metadata
        int fixedCount = auditService.AutoFixMetadataIssues(doc);
        Assert.True(fixedCount >= 1);
        Assert.False(string.IsNullOrWhiteSpace(doc.Title));

        // Act - Re-Audit
        var report2 = auditService.RunAudit(doc);
        Assert.Null(report2.Issues.FirstOrDefault(i => i.Title == "Missing Document Title"));
        Assert.True(report2.PdfAComplianceScore > report1.PdfAComplianceScore);
    }

    [Fact]
    public void DocumentAuditService_AutoFixMissingAltText_PopulatesFigureLabels()
    {
        // Arrange
        var auditService = new DocumentAuditService();
        var doc = new PdfDocumentModel { Title = "Catalog", Author = "Design Dept" };
        var page = new PdfPageModel { Width = 595, Height = 842 };

        var img1 = new PdfImageElement { X = 50, Y = 100, Width = 200, Height = 150, AltText = "" };
        var img2 = new PdfImageElement { X = 50, Y = 300, Width = 200, Height = 150, AltText = "" };
        page.Elements.Add(img1);
        page.Elements.Add(img2);
        doc.Pages.Add(page);

        // Act - Auto Fix Alt Text
        int fixedCount = auditService.AutoFixMissingAltText(doc);
        Assert.Equal(2, fixedCount);
        Assert.Equal("Document Figure 1", img1.AltText);
        Assert.Equal("Document Figure 2", img2.AltText);
    }
}
