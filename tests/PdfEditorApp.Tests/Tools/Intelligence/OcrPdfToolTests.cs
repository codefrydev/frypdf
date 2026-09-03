using PdfEditorApp.Services.Tools.Core;
using PdfEditorApp.Services.Tools.Organize;
using PdfEditorApp.Services.Tools.Security;
using PdfEditorApp.Services.Tools.Conversion;
using PdfEditorApp.Services.Tools.Intelligence;
using PdfEditorApp.ViewModels.Tools.Core;
using PdfEditorApp.ViewModels.Tools.Organize;
using PdfEditorApp.ViewModels.Tools.Security;
using PdfEditorApp.ViewModels.Tools.Conversion;
using PdfEditorApp.ViewModels.Tools.Intelligence;
using PdfEditorApp.Tests.Tools.Core;
using System.IO;
using System.Threading.Tasks;
using PdfEditorApp.Models;
using PdfEditorApp.ViewModels.Tools;
using PdfSharpCore.Drawing;
using PdfSharpCore.Pdf;
using Xunit;

namespace PdfEditorApp.Tests.Tools.Intelligence;

[Collection("OcrTests")]
public class OcrPdfToolTests : IClassFixture<ToolTestFixture>
{
    private readonly ToolTestFixture _fixture;

    public OcrPdfToolTests(ToolTestFixture fixture)
    {
        _fixture = fixture;
    }

    /// <summary>Creates a PDF page containing only an image and no drawn text, simulating a scan.</summary>
    private static string CreateImageOnlyPdf(string name)
    {
        string path = Path.Combine(Path.GetTempPath(), $"{name}_{System.Guid.NewGuid():N}.pdf");
        string image = ToolTestFixture.CreateSampleImage(name + "_img");
        try
        {
            using var doc = new PdfDocument();
            var page = doc.AddPage();
            using var gfx = XGraphics.FromPdfPage(page);
            using var xImage = XImage.FromFile(image);
            gfx.DrawImage(xImage, 0, 0, page.Width.Point, page.Height.Point);
            doc.Save(path);
        }
        finally
        {
            if (File.Exists(image)) File.Delete(image);
        }
        return path;
    }

    [Fact]
    public void OcrPdfTool_InstantiatesWithCorrectDefaults()
    {
        var vm = (OcrPdfToolViewModel)_fixture.Factory.Create(PdfToolId.OcrPdf);
        Assert.NotNull(vm);
        Assert.Equal(PdfToolId.OcrPdf, vm.Tool.Id);
        Assert.Equal("eng", vm.Language);
        Assert.True(vm.GenerateSearchablePdf);
    }

    [Fact]
    public async Task OcrPdfTool_ExecutesOcrSuccessfully()
    {
        string sample = ToolTestFixture.CreateSamplePdf("OcrSample", 1);
        try
        {
            var vm = (OcrPdfToolViewModel)_fixture.Factory.Create(PdfToolId.OcrPdf);
            vm.SelectedFiles.Add(sample);
            vm.SyncPreviewItems();

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
    public async Task OcrPdfTool_ScannedImageOnlyPdf_ReportsHonestFailure_NotFalseSuccess()
    {
        // Regression guard: before the fix, a page with zero recognized words still
        // produced a "Completed... Output is fully searchable" success message — this
        // tool has no real image-to-text engine, so that claim was always false for
        // genuinely scanned input. It must now say so clearly instead of claiming success.
        string sample = CreateImageOnlyPdf("OcrScanned");
        try
        {
            var vm = (OcrPdfToolViewModel)_fixture.Factory.Create(PdfToolId.OcrPdf);
            vm.SelectedEngine = PdfEditorApp.Core.Models.OcrEngineType.Tesseract;
            vm.SelectedFiles.Add(sample);
            vm.SyncPreviewItems();

            await vm.ExecuteToolCommand.ExecuteAsync(null);

            Assert.True(vm.HasError);
            Assert.False(vm.IsComplete);
            Assert.Contains("does not yet perform true image-to-text OCR", vm.ErrorMessage);
        }
        finally
        {
            PdfEditorApp.Services.Ocr.CompositeOcrProvider.Default.PreferredEngine = PdfEditorApp.Core.Models.OcrEngineType.Auto;
            if (File.Exists(sample)) File.Delete(sample);
        }
    }
}
