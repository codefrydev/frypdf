using System;
using System.Linq;
using System.Threading.Tasks;
using PdfEditorApp.Models;
using PdfEditorApp.ViewModels;
using Xunit;

namespace PdfEditorApp.Tests;

public class LicensingPageTests
{
    [Fact]
    public void LicensingPage_InitializesAll12ThirdPartyLibraries()
    {
        // Arrange & Act
        var vm = new HomeViewModel();

        // Assert
        Assert.NotEmpty(vm.AllLicenses);
        Assert.Equal(12, vm.AllLicenses.Count);

        var expectedPackages = new[]
        {
            "Avalonia UI",
            "QuestPDF",
            "PdfPig & Skia Rendering",
            "PdfSharpCore",
            "SkiaSharp",
            "Tabula Table Extractor",
            "DocumentFormat.OpenXml",
            "Material.Icons.Avalonia",
            "CommunityToolkit.Mvvm",
            "QRCoder",
            "Microsoft.Extensions.DependencyInjection",
            ".NET 10 Runtime & Base Libraries"
        };

        foreach (var expected in expectedPackages)
        {
            var item = vm.AllLicenses.FirstOrDefault(l => l.Name == expected);
            Assert.NotNull(item);
            Assert.False(string.IsNullOrWhiteSpace(item.Version), $"Version for {expected} should not be empty");
            Assert.False(string.IsNullOrWhiteSpace(item.LicenseType), $"LicenseType for {expected} should not be empty");
            Assert.False(string.IsNullOrWhiteSpace(item.Category), $"Category for {expected} should not be empty");
            Assert.False(string.IsNullOrWhiteSpace(item.Purpose), $"Purpose for {expected} should not be empty");
            Assert.False(string.IsNullOrWhiteSpace(item.Maintainer), $"Maintainer for {expected} should not be empty");
            Assert.False(string.IsNullOrWhiteSpace(item.ProjectUrl), $"ProjectUrl for {expected} should not be empty");
            Assert.False(string.IsNullOrWhiteSpace(item.LicenseText), $"LicenseText for {expected} should not be empty");
            Assert.False(string.IsNullOrWhiteSpace(item.IconKind), $"IconKind for {expected} should not be empty");
        }
    }

    [Fact]
    public void LicensingPage_CategoryFilter_FiltersLibrariesAccurately()
    {
        // Arrange
        var vm = new HomeViewModel();

        // Act 1: Filter PDF & Document Engines
        vm.SetLicenseCategoryCommand.Execute("PDF & Document Engines");
        Assert.Equal("PDF & Document Engines", vm.SelectedLicenseCategory);
        Assert.Equal(3, vm.FilteredLicenses.Count);
        Assert.Contains(vm.FilteredLicenses, l => l.Name == "QuestPDF");
        Assert.Contains(vm.FilteredLicenses, l => l.Name == "PdfPig & Skia Rendering");
        Assert.Contains(vm.FilteredLicenses, l => l.Name == "PdfSharpCore");

        // Act 2: Filter UI & Graphics Frameworks
        vm.SetLicenseCategoryCommand.Execute("UI & Graphics Frameworks");
        Assert.Equal(3, vm.FilteredLicenses.Count);
        Assert.Contains(vm.FilteredLicenses, l => l.Name == "Avalonia UI");
        Assert.Contains(vm.FilteredLicenses, l => l.Name == "SkiaSharp");
        Assert.Contains(vm.FilteredLicenses, l => l.Name == "Material.Icons.Avalonia");

        // Act 3: Filter Office & Data Formats
        vm.SetLicenseCategoryCommand.Execute("Office & Data Formats");
        Assert.Equal(3, vm.FilteredLicenses.Count);
        Assert.Contains(vm.FilteredLicenses, l => l.Name == "Tabula Table Extractor");
        Assert.Contains(vm.FilteredLicenses, l => l.Name == "DocumentFormat.OpenXml");
        Assert.Contains(vm.FilteredLicenses, l => l.Name == "QRCoder");

        // Act 4: Filter Architecture & Runtime
        vm.SetLicenseCategoryCommand.Execute("Architecture & Runtime");
        Assert.Equal(3, vm.FilteredLicenses.Count);
        Assert.Contains(vm.FilteredLicenses, l => l.Name == "CommunityToolkit.Mvvm");
        Assert.Contains(vm.FilteredLicenses, l => l.Name == "Microsoft.Extensions.DependencyInjection");
        Assert.Contains(vm.FilteredLicenses, l => l.Name == ".NET 10 Runtime & Base Libraries");

        // Act 5: Reset to All
        vm.SetLicenseCategoryCommand.Execute("All");
        Assert.Equal(12, vm.FilteredLicenses.Count);
    }

    [Fact]
    public void LicensingPage_SearchQuery_FiltersLibrariesByKeywords()
    {
        // Arrange
        var vm = new HomeViewModel();

        // Act 1: Search by name
        vm.SearchQuery = "QuestPDF";
        Assert.Single(vm.FilteredLicenses);
        Assert.Equal("QuestPDF", vm.FilteredLicenses[0].Name);

        // Act 2: Search by license type
        vm.SearchQuery = "Apache";
        Assert.Single(vm.FilteredLicenses);
        Assert.Equal("PdfPig & Skia Rendering", vm.FilteredLicenses[0].Name);

        // Act 3: Search by maintainer
        vm.SearchQuery = "Microsoft";
        Assert.True(vm.FilteredLicenses.Count >= 3);
        Assert.All(vm.FilteredLicenses, l => Assert.Contains("Microsoft", l.Maintainer + l.Name, StringComparison.OrdinalIgnoreCase));

        // Act 4: Clear search
        vm.ClearLicenseSearchCommand.Execute(null);
        Assert.Equal("", vm.SearchQuery);
        Assert.Equal("All", vm.SelectedLicenseCategory);
        Assert.Equal(12, vm.FilteredLicenses.Count);
    }

    [Fact]
    public void LicensingPage_SearchWithNoMatches_SetsHasNoMatchingLicenses()
    {
        // Arrange
        var vm = new HomeViewModel();

        // Act
        vm.SearchQuery = "NonExistentPackageXYZ999";

        // Assert
        Assert.Equal(0, vm.MatchingLicensesCount);
        Assert.True(vm.HasNoMatchingLicenses);
    }

    [Fact]
    public void LicensingPage_ToggleLicenseExpand_TogglesState()
    {
        // Arrange
        var vm = new HomeViewModel();
        var item = vm.AllLicenses.First();
        Assert.False(item.IsExpanded);

        // Act 1: Toggle on
        vm.ToggleLicenseExpandCommand.Execute(item);
        Assert.True(item.IsExpanded);

        // Act 2: Toggle off
        vm.ToggleLicenseExpandCommand.Execute(item);
        Assert.False(item.IsExpanded);
    }

    [Fact]
    public void LicensingPage_Navigation_SelectsLicensingSection()
    {
        // Arrange
        var vm = new HomeViewModel();
        Assert.False(vm.IsLicensingSection);

        // Act
        vm.SelectNavSectionCommand.Execute("Licensing");

        // Assert
        Assert.Equal(HomeNavSection.Licensing, vm.SelectedNavSection);
        Assert.True(vm.IsLicensingSection);
    }

    [Fact]
    public void MainViewModel_NavigateToLicensing_SwitchesToHomeLicensingSection()
    {
        // Arrange
        var mainVm = new MainViewModel();
        mainVm.OpenAboutDialogCommand.Execute(null);
        Assert.True(mainVm.IsAboutDialogOpen);

        // Act
        mainVm.NavigateToLicensingCommand.Execute(null);

        // Assert
        Assert.False(mainVm.IsAboutDialogOpen);
        Assert.True(mainVm.IsHomePageVisible);
        Assert.False(mainVm.IsEditorVisible);
        Assert.Equal(HomeNavSection.Licensing, mainVm.Home.SelectedNavSection);
        Assert.True(mainVm.Home.IsLicensingSection);
    }
}
