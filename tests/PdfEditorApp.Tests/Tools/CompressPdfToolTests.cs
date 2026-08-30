using System.IO;
using System.Threading.Tasks;
using PdfEditorApp.Models;
using PdfEditorApp.ViewModels.Tools;
using Xunit;

namespace PdfEditorApp.Tests.Tools;

public class CompressPdfToolTests : IClassFixture<ToolTestFixture>
{
    private readonly ToolTestFixture _fixture;

    public CompressPdfToolTests(ToolTestFixture fixture)
    {
        _fixture = fixture;
    }

    [Fact]
    public void CompressPdfTool_InstantiatesWithCorrectDefaults()
    {
        var vm = (CompressPdfToolViewModel)_fixture.Factory.Create(PdfToolId.CompressPdf);
        Assert.NotNull(vm);
        Assert.Equal(PdfToolId.CompressPdf, vm.Tool.Id);
        Assert.Equal(PdfCompressionLevel.Balanced, vm.CompressionLevel);
        Assert.Equal(150, vm.ImageQualityDpi);
    }

    [Fact]
    public async Task CompressPdfTool_ExecutesCompressionSuccessfully()
    {
        string sample = ToolTestFixture.CreateSamplePdf("CompressSample", 3);
        try
        {
            var vm = (CompressPdfToolViewModel)_fixture.Factory.Create(PdfToolId.CompressPdf);
            vm.SelectedFiles.Add(sample);
            vm.SyncPreviewItems();
            vm.CompressionLevel = PdfCompressionLevel.MaximumCompression;

            await vm.ExecuteToolCommand.ExecuteAsync(null);

            Assert.True(vm.IsComplete);
            Assert.False(vm.HasError);
            Assert.True(File.Exists(vm.LastOutputFilePath));
        }
        finally
        {
            if (File.Exists(sample)) File.Delete(sample);
        }
    }

    [Fact]
    public void CompressPdfTool_CustomMode_ClampsValuesAndProvidesHelpfulFeedback()
    {
        var vm = (CompressPdfToolViewModel)_fixture.Factory.Create(PdfToolId.CompressPdf);
        
        // Test Preset Selection
        vm.SelectPreset("extreme");
        Assert.True(vm.IsExtremeCompression);
        Assert.Equal(96, vm.ImageQualityDpi);
        Assert.Equal(52, vm.JpegQuality);

        vm.SelectPreset("recommended");
        Assert.True(vm.IsRecommendedCompression);
        Assert.Equal(125, vm.ImageQualityDpi);
        Assert.Equal(66, vm.JpegQuality);

        // Test Custom Mode & Dynamic Warning on low DPI
        vm.SelectPreset("custom");
        Assert.True(vm.IsCustomMode);
        
        vm.ImageQualityDpi = 40; // Should be clamped to 50
        Assert.Equal(50, vm.ImageQualityDpi);
        Assert.Equal("Warning", vm.QualityFeedbackSeverity);
        Assert.Contains("Low DPI", vm.QualityFeedbackMessage);

        // Test Low JPEG Quality Warning
        vm.ImageQualityDpi = 150;
        vm.JpegQuality = 30;
        Assert.Equal("Warning", vm.QualityFeedbackSeverity);
        Assert.Contains("Low JPEG Quality", vm.QualityFeedbackMessage);

        // Test Grayscale feedback
        vm.JpegQuality = 75;
        vm.ConvertToGrayscale = true;
        Assert.Equal("Success", vm.QualityFeedbackSeverity);
        Assert.Contains("Grayscale", vm.QualityFeedbackMessage);
    }

    [Fact]
    public async Task CompressPdfTool_ExecutesCustomCompressionWithGrayscaleSuccessfully()
    {
        string sample = ToolTestFixture.CreateSamplePdf("CompressCustomSample", 2);
        try
        {
            var vm = (CompressPdfToolViewModel)_fixture.Factory.Create(PdfToolId.CompressPdf);
            vm.SelectedFiles.Add(sample);
            vm.SyncPreviewItems();
            vm.SelectPreset("custom");
            vm.ImageQualityDpi = 100;
            vm.JpegQuality = 55;
            vm.ConvertToGrayscale = true;

            await vm.ExecuteToolCommand.ExecuteAsync(null);

            Assert.True(vm.IsComplete);
            Assert.False(vm.HasError);
            Assert.True(File.Exists(vm.LastOutputFilePath));
        }
        finally
        {
            if (File.Exists(sample)) File.Delete(sample);
        }
    }
}
