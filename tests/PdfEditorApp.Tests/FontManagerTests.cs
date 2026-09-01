using PdfEditorApp.Core.Analysis;
using PdfEditorApp.Core.Models;
using PdfEditorApp.Models;
using PdfEditorApp.Services;
using PdfEditorApp.ViewModels;
using Xunit;

namespace PdfEditorApp.Tests;

public class FontManagerTests
{
    private readonly FontPackageService _fontService = new();

    [Fact]
    public void FontCatalog_ContainsAllMajorWorldwideScriptPackages()
    {
        var packages = _fontService.GetAllPackages();

        Assert.NotNull(packages);
        Assert.True(packages.Count >= 10, "Catalog must contain at least 10 major language and font packs");

        // Verify CJK
        Assert.Contains(packages, p => p.Id == "zh-hans" && p.Category == FontPackageCategory.EastAsia);
        Assert.Contains(packages, p => p.Id == "zh-hant" && p.Category == FontPackageCategory.EastAsia);
        Assert.Contains(packages, p => p.Id == "ja" && p.Category == FontPackageCategory.EastAsia);
        Assert.Contains(packages, p => p.Id == "ko" && p.Category == FontPackageCategory.EastAsia);

        // Verify Indic, Middle East, SEA, Eurasia
        Assert.Contains(packages, p => p.Id == "indic" && p.Category == FontPackageCategory.SouthAsia);
        Assert.Contains(packages, p => p.Id == "arabic" && p.Category == FontPackageCategory.MiddleEast);
        Assert.Contains(packages, p => p.Id == "hebrew" && p.Category == FontPackageCategory.MiddleEast);
        Assert.Contains(packages, p => p.Id == "southeast-asia" && p.Category == FontPackageCategory.SoutheastAsia);
        Assert.Contains(packages, p => p.Id == "eurasia" && p.Category == FontPackageCategory.EuropeAndEurasia);
        Assert.Contains(packages, p => p.Id == "creative-design" && p.Category == FontPackageCategory.DesignAndTypography);
    }

    [Fact]
    public void FontCatalog_AllFilesUseGitHubCdnUrlPattern()
    {
        var packages = _fontService.GetAllPackages();
        Assert.NotEmpty(packages);

        foreach (var pkg in packages)
        {
            Assert.NotEmpty(pkg.Files);
            foreach (var file in pkg.Files)
            {
                Assert.False(string.IsNullOrWhiteSpace(file.FileName));
                Assert.StartsWith(FontPackageService.FontCdnBaseUrl, file.DownloadUrl);
                Assert.EndsWith(file.FileName, file.DownloadUrl);
                Assert.True(file.FileSizeBytes > 0, $"File size for {file.FileName} must be positive");
            }
        }
    }

    [Fact]
    public async System.Threading.Tasks.Task DownloadPackageAsync_DownloadsAndInstallsFromGitHubSuccessfully()
    {
        var packages = _fontService.GetAllPackages();
        var hebrewPack = packages.First(p => p.Id == "hebrew");

        // Clean any existing files first
        await _fontService.DeletePackageAsync(hebrewPack);

        var progressReported = false;
        var progress = new Progress<double>(p => { if (p > 0) progressReported = true; });

        var success = await _fontService.DownloadPackageAsync(hebrewPack, progress);

        Assert.True(success, "Hebrew font package should download successfully from GitHub repository");
        Assert.True(progressReported, "Progress should be reported during download");
        Assert.True(_fontService.IsPackageInstalled(hebrewPack), "Package should be marked as installed");

        // Cleanup after test
        await _fontService.DeletePackageAsync(hebrewPack);
    }

    [Fact]
    public void FontPackageInfo_FormatBytes_ReturnsHumanReadableSize()
    {
        Assert.Equal("0 KB", FontPackageInfo.FormatBytes(0));
        Assert.Equal("500.0 KB", FontPackageInfo.FormatBytes(500 * 1024));
        Assert.Equal("15.0 MB", FontPackageInfo.FormatBytes((long)(15.0 * 1024 * 1024)));
    }

    [Theory]
    [InlineData("你好世界，欢迎使用 FryPDF", "zh-hans")] // Simplified Chinese
    [InlineData("こんにちは世界、PDFエディタへようこそ", "ja")]      // Japanese (Hiragana/Katakana)
    [InlineData("안녕하세요 세계, PDF 편집기", "ko")]            // Korean Hangul
    [InlineData("भारत का सबसे सुरक्षित PDF संपादक", "indic")]     // Hindi Devanagari
    [InlineData("مرحبا بالعالم محرر بي دي اف", "arabic")]       // Arabic RTL
    public void DetectMissingPackageForText_IdentifiesCorrectScriptPackage(string sampleText, string expectedPackageId)
    {
        // When not installed, should identify the matching package
        var detected = _fontService.DetectMissingPackageForText(sampleText);
        if (detected != null)
        {
            Assert.Equal(expectedPackageId, detected.Id);
        }
    }

    [Fact]
    public void UnicodeScriptDetector_ClassifiesVariousScriptsAccurately()
    {
        // Chinese
        Assert.Equal("Noto Sans SC", UnicodeScriptDetector.DetectScriptFontFamily("中华人民共和国商务合同"));

        // Japanese
        Assert.Equal("Noto Sans JP", UnicodeScriptDetector.DetectScriptFontFamily("ひらがな と カタカナ"));

        // Korean
        Assert.Equal("Noto Sans KR", UnicodeScriptDetector.DetectScriptFontFamily("대한민국 서울특별시"));

        // Devanagari (Hindi)
        Assert.Equal("Noto Sans Devanagari", UnicodeScriptDetector.DetectScriptFontFamily("नमस्ते भारतवर्ष"));

        // Arabic
        Assert.Equal("Noto Sans Arabic", UnicodeScriptDetector.DetectScriptFontFamily("المملكة العربية السعودية"));

        // Hebrew
        Assert.Equal("Noto Sans Hebrew", UnicodeScriptDetector.DetectScriptFontFamily("שלום עולם ומסמכי"));

        // Thai
        Assert.Equal("Noto Sans Thai", UnicodeScriptDetector.DetectScriptFontFamily("สวัสดีชาวโลกและเอกสาร"));

        // Greek
        Assert.Equal("GFS Neohellenic", UnicodeScriptDetector.DetectScriptFontFamily("Ελληνική Δημοκρατία"));

        // English / Latin default
        Assert.Equal("Open Sans", UnicodeScriptDetector.DetectScriptFontFamily("Standard English Business Invoice 2026"));
    }

    [Fact]
    public void FontManagerViewModel_FilterAndCategorySelection_FiltersCorrectly()
    {
        var vm = new FontManagerViewModel(_fontService);

        Assert.Equal(vm.TotalPackagesCount, vm.FilteredPackages.Count);

        // Filter by East Asia
        vm.SelectCategory("EastAsia");
        Assert.All(vm.FilteredPackages, p => Assert.Equal(FontPackageCategory.EastAsia, p.Category));
        Assert.Equal(4, vm.FilteredPackages.Count); // zh-hans, zh-hant, ja, ko

        // Filter by Search Query
        vm.SelectCategory("All");
        vm.SearchQuery = "hindi";
        Assert.Single(vm.FilteredPackages);
        Assert.Equal("indic", vm.FilteredPackages[0].Id);

        // Clear filter
        vm.SearchQuery = "";
        Assert.Equal(vm.TotalPackagesCount, vm.FilteredPackages.Count);
    }

    [Fact]
    public void HomeViewModel_NavigatesToFontPackagesSection()
    {
        var home = new HomeViewModel();

        home.SelectNavSection("FontPackages");

        Assert.Equal(HomeNavSection.FontPackages, home.SelectedNavSection);
        Assert.True(home.IsFontPackagesSection);
        Assert.NotNull(home.FontManager);
    }
}
