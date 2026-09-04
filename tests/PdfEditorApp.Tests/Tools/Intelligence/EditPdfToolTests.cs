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
using System.IO;
using System.Threading.Tasks;
using PdfEditorApp.Tests.Tools.Core;
using CommunityToolkit.Mvvm.Messaging;
using PdfEditorApp.Messages;
using PdfEditorApp.Models;
using PdfEditorApp.ViewModels.Tools;
using Xunit;

namespace PdfEditorApp.Tests.Tools.Intelligence;

public class EditPdfToolTests : IClassFixture<ToolTestFixture>
{
    private readonly ToolTestFixture _fixture;

    public EditPdfToolTests(ToolTestFixture fixture)
    {
        _fixture = fixture;
    }

    [Fact]
    public void EditPdfTool_InstantiatesWithCorrectDefaults()
    {
        var vm = (EditPdfToolViewModel)_fixture.Factory.Create(PdfToolId.EditPdf);
        Assert.NotNull(vm);
        Assert.Equal(PdfToolId.EditPdf, vm.Tool.Id);
    }

    [Fact]
    public async Task EditPdfTool_OpensEditorSessionSuccessfully()
    {
        string sample = ToolTestFixture.CreateSamplePdf("EditSample", 1);
        try
        {
            var vm = (EditPdfToolViewModel)_fixture.Factory.Create(PdfToolId.EditPdf);
            vm.SelectedFiles.Add(sample);
            vm.SyncPreviewItems();

            bool openInEditorTriggered = false;
            string targetPath = "";
            WeakReferenceMessenger.Default.Register<EditPdfToolTests, OpenInEditorMessage>(this, (r, m) =>
            {
                openInEditorTriggered = true;
                targetPath = m.FilePath;
            });

            try
            {
                await vm.ExecuteToolCommand.ExecuteAsync(null);

                Assert.True(vm.IsComplete);
                Assert.False(vm.HasError);
                Assert.True(openInEditorTriggered);
                Assert.Equal(sample, targetPath);
            }
            finally
            {
                WeakReferenceMessenger.Default.Unregister<OpenInEditorMessage>(this);
            }
        }
        finally
        {
            if (File.Exists(sample)) File.Delete(sample);
        }
    }
}
