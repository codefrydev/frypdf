using System.IO;
using System.Linq;
using System.Threading.Tasks;
using PdfEditorApp.Models;
using PdfEditorApp.ViewModels.Tools;
using PdfSharpCore.Pdf.IO;
using Xunit;

namespace PdfEditorApp.Tests.Tools;

public class ProtectPdfToolTests : IClassFixture<ToolTestFixture>
{
    private readonly ToolTestFixture _fixture;

    public ProtectPdfToolTests(ToolTestFixture fixture)
    {
        _fixture = fixture;
    }

    [Fact]
    public void ProtectPdfTool_InstantiatesWithCorrectDefaults()
    {
        var vm = (ProtectPdfToolViewModel)_fixture.Factory.Create(PdfToolId.ProtectPdf);
        Assert.NotNull(vm);
        Assert.Equal(PdfToolId.ProtectPdf, vm.Tool.Id);
        Assert.True(vm.AllowPrinting);
        Assert.False(vm.AllowCopying);
    }

    [Fact]
    public async Task ProtectPdfTool_ProtectsWithPasswordSuccessfully()
    {
        string sample = ToolTestFixture.CreateSamplePdf("ProtectSample", 2);
        try
        {
            var vm = (ProtectPdfToolViewModel)_fixture.Factory.Create(PdfToolId.ProtectPdf);
            vm.SelectedFiles.Add(sample);
            vm.SyncPreviewItems();
            vm.UserPassword = "Password123";

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
    public async Task ProtectPdfTool_OwnerPasswordIsNotDerivedFromUserPassword()
    {
        // Regression guard: the owner password used to be `{userPassword}_admin` — a fixed,
        // guessable pattern. Anyone who had the "open" (user) password could compute full
        // owner rights and strip every permission restriction. It must now be unguessable.
        string sample = ToolTestFixture.CreateSamplePdf("ProtectSecuritySample", 1);
        string? outputPath = null;
        try
        {
            var vm = (ProtectPdfToolViewModel)_fixture.Factory.Create(PdfToolId.ProtectPdf);
            vm.SelectedFiles.Add(sample);
            vm.SyncPreviewItems();
            vm.UserPassword = "OpenMe123";
            vm.AllowModifying = false;
            vm.AllowCopying = false;

            await vm.ExecuteToolCommand.ExecuteAsync(null);

            Assert.True(vm.IsComplete);
            Assert.False(vm.HasError);
            outputPath = vm.LastOutputFilePath;
            Assert.True(File.Exists(outputPath));

            // The old vulnerable derivation must no longer grant owner access.
            string guessedOwnerPassword = "OpenMe123_admin";
            Assert.Throws<PdfReaderException>(() =>
                PdfReader.Open(outputPath, guessedOwnerPassword, PdfDocumentOpenMode.Modify));

            // The genuinely-generated owner password (surfaced in the result message) must work.
            string realOwnerPassword = vm.ResultSummaryMessage.Split(':').Last().Trim();
            Assert.NotEmpty(realOwnerPassword);
            Assert.NotEqual(guessedOwnerPassword, realOwnerPassword);
            using var doc = PdfReader.Open(outputPath, realOwnerPassword, PdfDocumentOpenMode.Modify);
            Assert.True(doc.SecuritySettings.HasOwnerPermissions);
        }
        finally
        {
            if (File.Exists(sample)) File.Delete(sample);
            if (outputPath != null && File.Exists(outputPath)) File.Delete(outputPath);
        }
    }
}
