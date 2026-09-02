using System;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using PdfEditorApp.Core.Models;
using PdfEditorApp.Models;
using PdfEditorApp.Services.Ocr;
using PdfEditorApp.ViewModels;
using Xunit;

namespace PdfEditorApp.Tests;

public class TesseractManagerTests
{
    private readonly TesseractModelService _modelService = new();

    [Fact]
    public void TesseractCatalog_ContainsAllMajorWorldwideLanguagePacks()
    {
        var languages = _modelService.AvailableLanguages;

        Assert.NotNull(languages);
        Assert.True(languages.Count >= 30, "Catalog must contain at least 30 major language and script models");

        // European / Latin
        Assert.Contains(languages, l => l.Code == "eng" && l.Category == "Latin & European");
        Assert.Contains(languages, l => l.Code == "spa" && l.Category == "Latin & European");
        Assert.Contains(languages, l => l.Code == "fra" && l.Category == "Latin & European");
        Assert.Contains(languages, l => l.Code == "deu" && l.Category == "Latin & European");
        Assert.Contains(languages, l => l.Code == "rus" && l.Category == "Latin & European");

        // East Asia (CJK)
        Assert.Contains(languages, l => l.Code == "chi_sim" && l.Category == "East Asia (CJK)");
        Assert.Contains(languages, l => l.Code == "chi_tra" && l.Category == "East Asia (CJK)");
        Assert.Contains(languages, l => l.Code == "jpn" && l.Category == "East Asia (CJK)");
        Assert.Contains(languages, l => l.Code == "kor" && l.Category == "East Asia (CJK)");

        // South Asia (Indic)
        Assert.Contains(languages, l => l.Code == "hin" && l.Category == "South Asia (Indic)");
        Assert.Contains(languages, l => l.Code == "ben" && l.Category == "South Asia (Indic)");
        Assert.Contains(languages, l => l.Code == "tam" && l.Category == "South Asia (Indic)");
        Assert.Contains(languages, l => l.Code == "tel" && l.Category == "South Asia (Indic)");
        Assert.Contains(languages, l => l.Code == "san" && l.Category == "South Asia (Indic)");

        // Middle East & RTL
        Assert.Contains(languages, l => l.Code == "ara" && l.Category == "Middle East & RTL");
        Assert.Contains(languages, l => l.Code == "heb" && l.Category == "Middle East & RTL");
        Assert.Contains(languages, l => l.Code == "fas" && l.Category == "Middle East & RTL");

        // Southeast Asia
        Assert.Contains(languages, l => l.Code == "vie" && l.Category == "Southeast Asia");
        Assert.Contains(languages, l => l.Code == "tha" && l.Category == "Southeast Asia");
        Assert.Contains(languages, l => l.Code == "ind" && l.Category == "Southeast Asia");

        // Specialized
        Assert.Contains(languages, l => l.Code == "osd" && l.Category == "Specialized & Other");
        Assert.Contains(languages, l => l.Code == "equ" && l.Category == "Specialized & Other");
    }

    [Fact]
    public void TesseractCatalog_AllFilesUseTessdataFastGitHubUrlPattern()
    {
        var languages = _modelService.AvailableLanguages;
        Assert.NotEmpty(languages);

        foreach (var lang in languages)
        {
            Assert.False(string.IsNullOrWhiteSpace(lang.Code));
            Assert.False(string.IsNullOrWhiteSpace(lang.DisplayName));
            Assert.False(string.IsNullOrWhiteSpace(lang.NativeName));
            Assert.False(string.IsNullOrWhiteSpace(lang.FlagEmoji));
            Assert.False(string.IsNullOrWhiteSpace(lang.Description));
            Assert.False(string.IsNullOrWhiteSpace(lang.SampleText));
            Assert.StartsWith("https://raw.githubusercontent.com/tesseract-ocr/tessdata_fast/main/", lang.DownloadUrl);
            Assert.EndsWith($"{lang.Code}.traineddata", lang.DownloadUrl);
            Assert.True(lang.EstimatedSizeBytes > 0, $"Estimated size for {lang.Code} must be positive");
            Assert.Equal($"{lang.Code}.traineddata", lang.FileName);
        }
    }

    [Fact]
    public void TesseractLanguagePackageInfo_FormatBytes_ReturnsHumanReadableSize()
    {
        Assert.Equal("0 KB", TesseractLanguagePackageInfo.FormatBytes(0));
        Assert.Equal("500.0 KB", TesseractLanguagePackageInfo.FormatBytes(500 * 1024));
        Assert.Equal("15.0 MB", TesseractLanguagePackageInfo.FormatBytes((long)(15.0 * 1024 * 1024)));
    }

    [Fact]
    public void TesseractManagerViewModel_FilterAndCategorySelection_FiltersCorrectly()
    {
        var vm = new TesseractManagerViewModel(_modelService);

        Assert.Equal(vm.TotalLanguagesCount, vm.FilteredLanguages.Count);

        // Filter by East Asia
        vm.SelectCategory("EastAsia");
        Assert.All(vm.FilteredLanguages, l => Assert.Contains("East Asia", l.Category));
        Assert.Equal(4, vm.FilteredLanguages.Count); // chi_sim, chi_tra, jpn, kor

        // Filter by South Asia (Indic)
        vm.SelectCategory("SouthAsia");
        Assert.All(vm.FilteredLanguages, l => Assert.Contains("South Asia", l.Category));
        Assert.True(vm.FilteredLanguages.Count >= 6);

        // Filter by Search Query
        vm.SelectCategory("All");
        vm.SearchQuery = "hindi";
        Assert.Single(vm.FilteredLanguages);
        Assert.Equal("hin", vm.FilteredLanguages[0].Code);

        // Search by code
        vm.SearchQuery = "jpn";
        Assert.Single(vm.FilteredLanguages);
        Assert.Equal("jpn", vm.FilteredLanguages[0].Code);

        // Clear filter
        vm.SearchQuery = "";
        Assert.Equal(vm.TotalLanguagesCount, vm.FilteredLanguages.Count);
    }

    [Fact]
    public void HomeViewModel_NavigatesToTesseractDataSection()
    {
        var home = new HomeViewModel();

        home.SelectNavSection("TesseractData");

        Assert.Equal(HomeNavSection.TesseractData, home.SelectedNavSection);
        Assert.True(home.IsTesseractDataSection);
        Assert.NotNull(home.TesseractManager);
        Assert.True(home.TesseractManager.TotalLanguagesCount >= 30);
    }

    [Fact]
    public async Task TesseractModelService_MockDirectoryLifecycle_HandlesInstallDeleteAndClear()
    {
        string tempDir = Path.Combine(Path.GetTempPath(), "frypdf_test_tessdata_" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(tempDir);

        try
        {
            var service = new TesseractModelService(tempDir);
            Assert.False(service.IsLanguageInstalled("eng"));

            // Create a fake traineddata file
            string engPath = Path.Combine(tempDir, "eng.traineddata");
            await File.WriteAllBytesAsync(engPath, new byte[2048]);

            Assert.True(service.IsLanguageInstalled("eng"));
            Assert.Equal(engPath, service.GetInstalledLanguagePath("eng"));

            long totalBytes = await service.GetTotalCacheSizeBytesAsync();
            Assert.Equal(2048, totalBytes);

            // Delete language
            bool deleted = await service.DeleteLanguageAsync("eng");
            Assert.True(deleted);
            Assert.False(service.IsLanguageInstalled("eng"));
            Assert.False(File.Exists(engPath));

            // Create two fake files and clear all
            await File.WriteAllBytesAsync(Path.Combine(tempDir, "fra.traineddata"), new byte[1500]);
            await File.WriteAllBytesAsync(Path.Combine(tempDir, "deu.traineddata"), new byte[1500]);
            Assert.True(service.IsLanguageInstalled("fra"));
            Assert.True(service.IsLanguageInstalled("deu"));

            await service.ClearAllCacheAsync();
            Assert.False(service.IsLanguageInstalled("fra"));
            Assert.False(service.IsLanguageInstalled("deu"));
        }
        finally
        {
            try { Directory.Delete(tempDir, recursive: true); } catch { }
        }
    }
}
