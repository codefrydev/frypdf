using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using PdfEditorApp.Models;
using PdfEditorApp.Services;
using PdfEditorApp.Services.Tools;
using PdfEditorApp.ViewModels;
using Xunit;

namespace PdfEditorApp.Tests;

public class HomeViewModelTests
{
    private class MockRecentService : IRecentDocumentsService
    {
        public List<RecentDocumentItem> Items { get; } = new();

        public List<RecentDocumentItem> Load() => new(Items);
        public void Add(RecentDocumentItem item) => Items.Insert(0, item);
        public void Remove(string filePath) => Items.RemoveAll(x => x.FilePath == filePath);
        public void Clear() => Items.Clear();
    }

    [Fact]
    public void HomeViewModel_InitializesAllTemplatesWithRealisticPreviews()
    {
        // Arrange & Act
        var vm = new HomeViewModel();

        // Assert
        Assert.NotEmpty(vm.AllTemplates);
        Assert.True(vm.AllTemplates.Count >= 18, "Should have at least 18 templates initialized including new resumes and research papers");

        // Verify each non-blank template has a live PagePreview with real elements
        foreach (var template in vm.AllTemplates)
        {
            Assert.NotNull(template.PagePreview);
            Assert.NotEmpty(template.Name);
            Assert.NotEmpty(template.Category);

            if (!template.IsBlank)
            {
                Assert.True(template.PagePreview.Elements.Count > 0, $"Template '{template.Name}' should have real page elements in preview");
            }
        }
    }

    [Fact]
    public void HomeViewModel_ToggleGallery_ExpandsAndCollapsesCorrectly()
    {
        // Arrange
        var vm = new HomeViewModel();
        Assert.False(vm.IsTemplateGalleryExpanded);

        // Act & Assert 1: Toggle expands
        vm.ToggleTemplateGalleryCommand.Execute(null);
        Assert.True(vm.IsTemplateGalleryExpanded);

        // Act & Assert 2: Toggle collapses
        vm.ToggleTemplateGalleryCommand.Execute(null);
        Assert.False(vm.IsTemplateGalleryExpanded);

        // Act & Assert 3: Direct collapse command
        vm.ToggleTemplateGalleryCommand.Execute(null); // expand first
        Assert.True(vm.IsTemplateGalleryExpanded);

        vm.CollapseTemplateGalleryCommand.Execute(null);
        Assert.False(vm.IsTemplateGalleryExpanded);
    }

    [Fact]
    public void HomeViewModel_CategoryFilter_FiltersTemplatesAccurately()
    {
        // Arrange
        var vm = new HomeViewModel();

        // Act 1: Filter Corporate
        vm.SetTemplateCategoryCommand.Execute("Corporate");
        Assert.Equal("Corporate", vm.SelectedTemplateCategory);
        Assert.All(vm.FilteredTemplates, t => Assert.Equal("Corporate", t.Category, ignoreCase: true));
        Assert.Contains(vm.FilteredTemplates, t => t.Id == "annualreport");

        // Act 2: Filter Finance
        vm.SetTemplateCategoryCommand.Execute("Finance");
        Assert.Equal("Finance", vm.SelectedTemplateCategory);
        Assert.All(vm.FilteredTemplates, t => Assert.Equal("Finance", t.Category, ignoreCase: true));
        Assert.Contains(vm.FilteredTemplates, t => t.Id == "invoice");
        Assert.Contains(vm.FilteredTemplates, t => t.Id == "financeresearch");

        // Act 3: Filter Career (All 4 Resume Types)
        vm.SetTemplateCategoryCommand.Execute("Career");
        Assert.Equal("Career", vm.SelectedTemplateCategory);
        Assert.All(vm.FilteredTemplates, t => Assert.Equal("Career", t.Category, ignoreCase: true));
        Assert.Contains(vm.FilteredTemplates, t => t.Id == "resume");
        Assert.Contains(vm.FilteredTemplates, t => t.Id == "resumemodern");
        Assert.Contains(vm.FilteredTemplates, t => t.Id == "resumecreative");
        Assert.Contains(vm.FilteredTemplates, t => t.Id == "resumeacademic");

        // Act 4: Filter Academic (All 4 Research Papers)
        vm.SetTemplateCategoryCommand.Execute("Academic");
        Assert.Equal("Academic", vm.SelectedTemplateCategory);
        Assert.All(vm.FilteredTemplates, t => Assert.Equal("Academic", t.Category, ignoreCase: true));
        Assert.Contains(vm.FilteredTemplates, t => t.Id == "academic");
        Assert.Contains(vm.FilteredTemplates, t => t.Id == "mathresearch");
        Assert.Contains(vm.FilteredTemplates, t => t.Id == "physicsresearch");
        Assert.Contains(vm.FilteredTemplates, t => t.Id == "historyresearch");

        // Act 5: Filter Certificates
        vm.SetTemplateCategoryCommand.Execute("Certificates");
        Assert.All(vm.FilteredTemplates, t => Assert.Equal("Certificates", t.Category, ignoreCase: true));
        Assert.Contains(vm.FilteredTemplates, t => t.Id == "certificate");
        Assert.Contains(vm.FilteredTemplates, t => t.Id == "certificatenavygold");
        Assert.Contains(vm.FilteredTemplates, t => t.Id == "diploma");

        // Act 6: Reset to All
        vm.SetTemplateCategoryCommand.Execute("All");
        Assert.Equal(vm.AllTemplates.Count, vm.FilteredTemplates.Count);
    }

    [Fact]
    public void HomeViewModel_SearchQuery_FiltersByKeywords()
    {
        // Arrange
        var vm = new HomeViewModel();

        // Act: Search for "quantum"
        vm.SearchQuery = "quantum";

        // Assert
        Assert.Contains(vm.FilteredTemplates, t => t.Id == "physicsresearch");
        Assert.DoesNotContain(vm.FilteredTemplates, t => t.Id == "annualreport");

        // Act 2: Search for "hodge"
        vm.SearchQuery = "hodge";
        Assert.Contains(vm.FilteredTemplates, t => t.Id == "mathresearch");

        // Act 3: Search for "mediterranean"
        vm.SearchQuery = "mediterranean";
        Assert.Contains(vm.FilteredTemplates, t => t.Id == "historyresearch");

        // Clear search
        vm.ClearTemplateSearchCommand.Execute(null);
        Assert.Equal("", vm.SearchQuery);
        Assert.Equal(vm.AllTemplates.Count, vm.FilteredTemplates.Count);
    }

    [Fact]
    public void HomeViewModel_SearchWithNoMatches_SetsHasNoMatchingTemplates()
    {
        // Arrange
        var vm = new HomeViewModel();

        // Act
        vm.SearchQuery = "NonExistentZebraTemplate12345";

        // Assert
        Assert.Equal(0, vm.MatchingTemplatesCount);
        Assert.True(vm.HasNoMatchingTemplates);
    }

    [Fact]
    public void HomeViewModel_SelectTemplateCommand_FiresOpenTemplateRequestedEvent()
    {
        // Arrange
        var vm = new HomeViewModel();
        string? requestedTemplate = null;
        vm.OpenTemplateRequested += t => requestedTemplate = t;

        // Act
        vm.SelectTemplateCommand.Execute("resumemodern");

        // Assert
        Assert.Equal("resumemodern", requestedTemplate);
    }

    [Fact]
    public void HomeViewModel_RecentDocuments_LoadsAndClearsCorrectly()
    {
        // Arrange
        var mockRecent = new MockRecentService();
        mockRecent.Add(new RecentDocumentItem { Title = "Doc1.pdf", FilePath = "/path/Doc1.pdf" });
        mockRecent.Add(new RecentDocumentItem { Title = "Doc2.frypdf", FilePath = "/path/Doc2.frypdf" });

        var vm = new HomeViewModel(mockRecent, new TemplateService(), new ProjectPersistenceService(), new PdfToolRegistry());

        // Assert
        Assert.True(vm.HasRecentDocuments);
        Assert.Equal(2, vm.RecentDocuments.Count);

        // Act: Clear
        vm.ClearRecentCommand.Execute(null);

        // Assert
        Assert.False(vm.HasRecentDocuments);
        Assert.Empty(vm.RecentDocuments);
    }

    [Fact]
    public void HomeViewModel_OpenToolPage_OpensFullPageWithoutDialog()
    {
        // Arrange
        var toolRegistry = new PdfToolRegistry();
        var operationsService = new PdfDocumentOperationsService();
        var factory = new PdfToolViewModelFactory(operationsService, toolRegistry);
        var vm = new HomeViewModel(new MockRecentService(), new TemplateService(), new ProjectPersistenceService(), toolRegistry, null, factory);

        Assert.False(vm.IsToolPageActive);
        Assert.Null(vm.ActiveToolViewModel);

        // Act: Open OCR PDF tool
        vm.OpenToolPage(PdfToolId.OcrPdf);

        // Assert: Full page is active with initialized Tool ViewModel
        Assert.True(vm.IsToolPageActive);
        Assert.NotNull(vm.ActiveToolViewModel);
        Assert.Equal(PdfToolId.OcrPdf, vm.ActiveToolViewModel.Tool.Id);

        // Act: Go back to tools
        vm.BackToToolsCommand.Execute(null);

        // Assert: Full page closed, back to home tools grid
        Assert.False(vm.IsToolPageActive);
        Assert.Null(vm.ActiveToolViewModel);
    }
}
