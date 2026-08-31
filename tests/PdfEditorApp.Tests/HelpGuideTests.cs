using System;
using System.Linq;
using PdfEditorApp.Models;
using PdfEditorApp.Services;
using PdfEditorApp.ViewModels;
using Xunit;

namespace PdfEditorApp.Tests;

public class HelpGuideTests
{
    [Fact]
    public void HelpGuideService_InitializesAllCoreCategoriesAndGuides()
    {
        // Arrange
        var service = new HelpGuideService();

        // Act
        var guides = service.GetAllGuides();
        var categories = service.GetAllCategories();

        // Assert
        Assert.NotEmpty(guides);
        Assert.True(guides.Count >= 20, $"Expected at least 20 guides, but found {guides.Count}");
        Assert.Contains("All Guides", categories);
        Assert.Contains("Getting Started", categories);
        Assert.Contains("32 PDF Tools", categories);
        Assert.Contains("Live Editor", categories);
        Assert.Contains("Automation", categories);
        Assert.Contains("Language & Fonts", categories);
        Assert.Contains("Shortcuts", categories);
        Assert.Contains("FAQ & Support", categories);

        // Verify each guide has non-empty required fields
        foreach (var guide in guides)
        {
            Assert.False(string.IsNullOrWhiteSpace(guide.Id), "Guide Id must not be empty");
            Assert.False(string.IsNullOrWhiteSpace(guide.Title), $"Title for {guide.Id} must not be empty");
            Assert.False(string.IsNullOrWhiteSpace(guide.Category), $"Category for {guide.Id} must not be empty");
            Assert.False(string.IsNullOrWhiteSpace(guide.Summary), $"Summary for {guide.Id} must not be empty");
            Assert.False(string.IsNullOrWhiteSpace(guide.Description), $"Description for {guide.Id} must not be empty");
            Assert.False(string.IsNullOrWhiteSpace(guide.IconKind), $"IconKind for {guide.Id} must not be empty");
            Assert.False(string.IsNullOrWhiteSpace(guide.IconColorHex), $"IconColorHex for {guide.Id} must not be empty");
            Assert.False(string.IsNullOrWhiteSpace(guide.BackgroundAccentHex), $"BackgroundAccentHex for {guide.Id} must not be empty");
            Assert.False(string.IsNullOrWhiteSpace(guide.Badge), $"Badge for {guide.Id} must not be empty");
            Assert.NotEmpty(guide.Steps);
        }
    }

    [Theory]
    [InlineData("Getting Started")]
    [InlineData("32 PDF Tools")]
    [InlineData("Live Editor")]
    [InlineData("Automation")]
    [InlineData("Language & Fonts")]
    [InlineData("Shortcuts")]
    [InlineData("FAQ & Support")]
    public void HelpGuideService_GetGuidesByCategory_ReturnsMatchingGuides(string category)
    {
        // Arrange
        var service = new HelpGuideService();

        // Act
        var filtered = service.GetGuidesByCategory(category);

        // Assert
        Assert.NotEmpty(filtered);
        Assert.All(filtered, g => Assert.Equal(category, g.Category, ignoreCase: true));
    }

    [Fact]
    public void HelpGuideService_GetGuideById_FindsSpecificTopics()
    {
        // Arrange
        var service = new HelpGuideService();

        // Act & Assert
        var mergeGuide = service.GetGuideById("tool-merge");
        Assert.NotNull(mergeGuide);
        Assert.Equal("Merge PDF Files", mergeGuide.Title);
        Assert.Equal(PdfToolId.MergePdf, mergeGuide.RelatedToolId);

        var ocrGuide = service.GetGuideById("tool-ocr-pdf");
        Assert.NotNull(ocrGuide);
        Assert.Equal("OCR Text Recognition", ocrGuide.Title);
        Assert.Equal(PdfToolId.OcrPdf, ocrGuide.RelatedToolId);

        var mathGuide = service.GetGuideById("editor-math-latex");
        Assert.NotNull(mathGuide);
        Assert.Equal("LaTeX Math Equation Studio", mathGuide.Title);

        var richTextGuide = service.GetGuideById("editor-rich-text");
        Assert.NotNull(richTextGuide);
        Assert.Equal("Rich Text & Inline Markdown Formatting", richTextGuide.Title);
        Assert.Equal("Live Editor", richTextGuide.Category);
        Assert.True(richTextGuide.IsFeatured);
        Assert.Contains("markdown", richTextGuide.Keywords, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void HelpGuideService_GetGuideByToolId_FindsAssociatedToolGuides()
    {
        // Arrange
        var service = new HelpGuideService();

        // Act & Assert
        var compress = service.GetGuideByToolId(PdfToolId.CompressPdf);
        Assert.NotNull(compress);
        Assert.Equal("Compress & Optimize PDF", compress.Title);

        var sign = service.GetGuideByToolId(PdfToolId.SignPdf);
        Assert.NotNull(sign);
        Assert.Equal("Digital Signature Studio", sign.Title);

        var protect = service.GetGuideByToolId(PdfToolId.ProtectPdf);
        Assert.NotNull(protect);
        Assert.Equal("Protect PDF (AES-256 Encryption)", protect.Title);
    }

    [Fact]
    public void HelpGuideViewModel_SearchQuery_FiltersGuidesAcrossKeywords()
    {
        // Arrange
        var vm = new HelpGuideViewModel();

        // Act 1: Search "merge"
        vm.SearchQuery = "merge";
        Assert.True(vm.FilteredGuides.Count > 0);
        Assert.Contains(vm.FilteredGuides, g => g.Title.Contains("Merge", StringComparison.OrdinalIgnoreCase) || g.Keywords.Contains("merge", StringComparison.OrdinalIgnoreCase));

        // Act 2: Search "latex"
        vm.SearchQuery = "latex";
        Assert.True(vm.FilteredGuides.Count > 0);
        Assert.Contains(vm.FilteredGuides, g => g.Id == "editor-math-latex");

        // Act 3: Search "ocr"
        vm.SearchQuery = "ocr";
        Assert.True(vm.FilteredGuides.Count > 0);
        Assert.Contains(vm.FilteredGuides, g => g.Id == "tool-ocr-pdf");

        // Act 4: Non-existent term
        vm.SearchQuery = "xyznonexistentquery12345";
        Assert.Empty(vm.FilteredGuides);
        Assert.True(vm.HasNoMatchingGuides);

        // Act 5: Clear search
        vm.ClearSearchCommand.Execute(null);
        Assert.Empty(vm.SearchQuery);
        Assert.False(vm.HasNoMatchingGuides);
        Assert.Equal(vm.AllGuides.Count, vm.FilteredGuides.Count);
    }

    [Fact]
    public void HelpGuideViewModel_CategorySelection_UpdatesFilteredList()
    {
        // Arrange
        var vm = new HelpGuideViewModel();

        // Act
        vm.SetCategoryCommand.Execute("Live Editor");

        // Assert
        Assert.Equal("Live Editor", vm.SelectedCategory);
        Assert.NotEmpty(vm.FilteredGuides);
        Assert.All(vm.FilteredGuides, g => Assert.Equal("Live Editor", g.Category));
        Assert.False(vm.IsDetailViewActive);
        Assert.Null(vm.SelectedGuide);
    }

    [Fact]
    public void HelpGuideViewModel_SelectGuideAndBackToGrid_TogglesDetailView()
    {
        // Arrange
        var vm = new HelpGuideViewModel();
        var targetGuide = vm.AllGuides.First();

        // Act 1: Select guide
        vm.SelectGuideCommand.Execute(targetGuide);
        Assert.True(vm.IsDetailViewActive);
        Assert.Equal(targetGuide, vm.SelectedGuide);

        // Act 2: Back to grid
        vm.BackToGridCommand.Execute(null);
        Assert.False(vm.IsDetailViewActive);
        Assert.Null(vm.SelectedGuide);
    }

    [Fact]
    public void HelpGuideViewModel_LaunchTool_FiresToolLaunchRequestedEvent()
    {
        // Arrange
        var vm = new HelpGuideViewModel();
        var mergeGuide = vm.AllGuides.FirstOrDefault(g => g.RelatedToolId == PdfToolId.MergePdf);
        Assert.NotNull(mergeGuide);

        PdfToolId? launchedToolId = null;
        vm.ToolLaunchRequested += (id) => launchedToolId = id;

        // Act
        vm.LaunchToolCommand.Execute(mergeGuide);

        // Assert
        Assert.Equal(PdfToolId.MergePdf, launchedToolId);
    }

    [Fact]
    public void HomeViewModel_HelpSection_NavigationAndIntegration()
    {
        // Arrange
        var home = new HomeViewModel();

        // Act 1: Select Help section
        home.SelectNavSectionCommand.Execute("Help");

        // Assert 1
        Assert.Equal(HomeNavSection.Help, home.SelectedNavSection);
        Assert.True(home.IsHelpSection);
        Assert.NotNull(home.HelpGuide);

        // Act 2: Open specific help topic
        home.OpenHelpGuideCommand.Execute("tool-compress");

        // Assert 2
        Assert.Equal(HomeNavSection.Help, home.SelectedNavSection);
        Assert.True(home.HelpGuide.IsDetailViewActive);
        Assert.NotNull(home.HelpGuide.SelectedGuide);
        Assert.Equal("tool-compress", home.HelpGuide.SelectedGuide.Id);

        // Act 3: Open help for specific tool
        home.OpenHelpForToolCommand.Execute(PdfToolId.SignPdf);

        // Assert 3
        Assert.Equal(HomeNavSection.Help, home.SelectedNavSection);
        Assert.True(home.HelpGuide.IsDetailViewActive);
        Assert.NotNull(home.HelpGuide.SelectedGuide);
        Assert.Equal(PdfToolId.SignPdf, home.HelpGuide.SelectedGuide.RelatedToolId);
    }
}
