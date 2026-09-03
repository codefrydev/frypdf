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
using Xunit;

namespace PdfEditorApp.Tests.Tools.Conversion;

public class PdfToJpgToolTests : IClassFixture<ToolTestFixture>
{
    private readonly ToolTestFixture _fixture;

    public PdfToJpgToolTests(ToolTestFixture fixture)
    {
        _fixture = fixture;
    }

    [Fact]
    public void PdfToJpgTool_InstantiatesWithCorrectDefaults()
    {
        var vm = (PdfToJpgToolViewModel)_fixture.Factory.Create(PdfToolId.PdfToJpg);
        Assert.NotNull(vm);
        Assert.Equal(PdfToolId.PdfToJpg, vm.Tool.Id);
        Assert.Equal("jpg", vm.OutputFormat);
        Assert.Equal(300, vm.Dpi);
    }

    [Fact]
    public async Task PdfToJpgTool_ExportsPdfToImagesSuccessfully()
    {
        string sample = ToolTestFixture.CreateSamplePdf("PdfToJpgSample", 2);
        try
        {
            var vm = (PdfToJpgToolViewModel)_fixture.Factory.Create(PdfToolId.PdfToJpg);
            vm.SelectedFiles.Add(sample);
            vm.SyncPreviewItems();

            await vm.ExecuteToolCommand.ExecuteAsync(null);

            Assert.True(vm.IsComplete);
            Assert.False(vm.HasError);
        }
        finally
        {
            if (File.Exists(sample)) File.Delete(sample);
        }
    }

    [Fact]
    public async Task PdfToJpgTool_ExportedImage_ContainsRealPageContent_NotBlankCanvas()
    {
        string sample = ToolTestFixture.CreateSamplePdf("PdfToJpgContentSample", 1);
        string? imagesDir = null;
        try
        {
            var vm = (PdfToJpgToolViewModel)_fixture.Factory.Create(PdfToolId.PdfToJpg);
            vm.SelectedFiles.Add(sample);
            vm.SyncPreviewItems();
            vm.OutputFormat = "png";

            await vm.ExecuteToolCommand.ExecuteAsync(null);

            Assert.True(vm.IsComplete);
            Assert.False(vm.HasError);
            Assert.True(File.Exists(vm.LastOutputFilePath));
            imagesDir = Path.GetDirectoryName(vm.LastOutputFilePath);

            using var bitmap = SkiaSharp.SKBitmap.Decode(vm.LastOutputFilePath);
            Assert.NotNull(bitmap);

            // Regression guard: before the fix, every exported page was a blank white
            // canvas (the draw call was missing entirely). The sample PDF has a solid
            // SteelBlue header bar across its top, so some pixels must be non-white.
            bool foundNonWhitePixel = false;
            for (int y = 0; y < bitmap!.Height && !foundNonWhitePixel; y += 4)
            {
                for (int x = 0; x < bitmap.Width; x += 4)
                {
                    var pixel = bitmap.GetPixel(x, y);
                    if (pixel.Red != 255 || pixel.Green != 255 || pixel.Blue != 255)
                    {
                        foundNonWhitePixel = true;
                        break;
                    }
                }
            }
            Assert.True(foundNonWhitePixel, "Exported page image was entirely blank white — PDF content was not rendered.");
        }
        finally
        {
            if (File.Exists(sample)) File.Delete(sample);
            if (imagesDir != null && Directory.Exists(imagesDir)) Directory.Delete(imagesDir, recursive: true);
        }
    }
}
