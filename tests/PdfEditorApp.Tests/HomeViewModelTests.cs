using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using PdfEditorApp.Models;
using PdfEditorApp.Services;
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
        Assert.True(vm.AllTemplates.Count >= 10, "Should have at least 10 templates initialized");

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

        // Act & Assert 3: Direct expand and collapse commands
        vm.ExpandTemplateGalleryCommand.Execute(null);
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
        Assert.All(vm.FilteredTemplates, t => Assert.Equal("Finance", t.Category, ignoreCase: true));
        Assert.Contains(vm.FilteredTemplates, t => t.Id == "invoice");

        // Act 3: Filter Certificates
        vm.SetTemplateCategoryCommand.Execute("Certificates");
        Assert.All(vm.FilteredTemplates, t => Assert.Equal("Certificates", t.Category, ignoreCase: true));
        Assert.Contains(vm.FilteredTemplates, t => t.Id == "certificate");
        Assert.Contains(vm.FilteredTemplates, t => t.Id == "certificatenavygold");
        Assert.Contains(vm.FilteredTemplates, t => t.Id == "diploma");

        // Act 4: Reset to All
        vm.SetTemplateCategoryCommand.Execute("All");
        Assert.Equal(vm.AllTemplates.Count, vm.FilteredTemplates.Count);
    }

    [Fact]
    public void HomeViewModel_SearchQuery_FiltersByKeywords()
    {
        // Arrange
        var vm = new HomeViewModel();

        // Act: Search for "invoice"
        vm.TemplateSearchQuery = "invoice";

        // Assert
        Assert.Contains(vm.FilteredTemplates, t => t.Id == "invoice");
        Assert.DoesNotContain(vm.FilteredTemplates, t => t.Id == "annualreport");

        // Clear search
        vm.ClearTemplateSearchCommand.Execute(null);
        Assert.Equal("", vm.TemplateSearchQuery);
        Assert.Equal(vm.AllTemplates.Count, vm.FilteredTemplates.Count);
    }

    [Fact]
    public void HomeViewModel_SearchWithNoMatches_SetsHasNoMatchingTemplates()
    {
        // Arrange
        var vm = new HomeViewModel();

        // Act
        vm.TemplateSearchQuery = "NonExistentZebraTemplate12345";

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
        vm.SelectTemplateCommand.Execute("invoice");

        // Assert
        Assert.Equal("invoice", requestedTemplate);
    }

    [Fact]
    public void HomeViewModel_RecentDocuments_LoadsAndClearsCorrectly()
    {
        // Arrange
        var mockRecent = new MockRecentService();
        mockRecent.Add(new RecentDocumentItem { Title = "Doc1.pdf", FilePath = "/path/Doc1.pdf" });
        mockRecent.Add(new RecentDocumentItem { Title = "Doc2.frypdf", FilePath = "/path/Doc2.frypdf" });

        var vm = new HomeViewModel(mockRecent);

        // Assert
        Assert.True(vm.HasRecentDocuments);
        Assert.Equal(2, vm.RecentDocuments.Count);

        // Act: Clear
        vm.ClearRecentCommand.Execute(null);

        // Assert
        Assert.False(vm.HasRecentDocuments);
        Assert.Empty(vm.RecentDocuments);
    }
}
