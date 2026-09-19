using System;
using System.Collections.Generic;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using PdfEditorApp.Core.Plugins;
using PdfEditorApp.Core.Plugins.Descriptors;
using PdfEditorApp.Models;
using PdfEditorApp.Services;
using PdfEditorApp.Services.Tools;
using PdfEditorApp.Services.Tools.Conversion;
using PdfEditorApp.Services.Tools.Core;
using PdfEditorApp.Services.Tools.Intelligence;
using PdfEditorApp.Services.Tools.Organize;
using PdfEditorApp.Services.Tools.Security;
using PdfEditorApp.ViewModels;
using PdfEditorApp.ViewModels.ElementViewModels;
using PdfEditorApp.ViewModels.Tools;
using PdfEditorApp.ViewModels.Tools.Core;
using Xunit;

namespace PdfEditorApp.Tests;

public class QualityAndBugFixTests
{
    [Fact]
    public void MainViewModel_ImplementsIServiceProvider_AndDoesNotThrowInvalidCastException()
    {
        var mainVm = new MainViewModel();
        Assert.IsAssignableFrom<IServiceProvider>(mainVm);

        var sp = (IServiceProvider)mainVm;
        Assert.NotNull(sp);

        // Resolving self
        var self = sp.GetService(typeof(MainViewModel));
        Assert.Same(mainVm, self);

        // Resolving unknown type before plugin host initialization returns null gracefully without exception
        var unknown = sp.GetService(typeof(QualityAndBugFixTests));
        Assert.Null(unknown);

        mainVm.Dispose();
    }

    [Fact]
    public void UndoRedoService_RecordAction_DiscardsRedoStackSafely()
    {
        var undoRedo = new UndoRedoService();
        bool action1Discarded = false;
        bool action2Discarded = false;

        // Record first action
        undoRedo.RecordAction(
            "Action 1",
            () => { },
            () => { },
            () => { action1Discarded = true; });

        // Record second action
        undoRedo.RecordAction(
            "Action 2",
            () => { },
            () => { },
            () => { action2Discarded = true; });

        // Undo action 2 -> now action 2 is on the redo stack
        var undone = undoRedo.Undo();
        Assert.Equal("Action 2", undone);
        Assert.False(action2Discarded, "Action 2 should not be discarded when undone");

        // Record a new action 3 -> this clears the redo stack; Action 2's OnDiscarded MUST be called
        undoRedo.RecordAction("Action 3", () => { }, () => { });

        Assert.True(action2Discarded, "Action 2 on redo stack should have been discarded when a new action was recorded");
        Assert.False(action1Discarded, "Action 1 is still on the undo list and should not be discarded");
    }

    [Fact]
    public void PdfViewerViewModel_Dispose_CleansUpResourcesSafely()
    {
        var viewerVm = new PdfViewerViewModel();

        // Simulate loaded dummy page
        viewerVm.Pages.Add(new PdfViewerPageItem { PageNumber = 1, WidthPoints = 595, HeightPoints = 842 });
        Assert.NotEmpty(viewerVm.Pages);

        // Dispose should empty pages and not throw
        viewerVm.Dispose();
        Assert.Empty(viewerVm.Pages);

        // Second call should be a safe no-op
        viewerVm.Dispose();
    }

    [Fact]
    public async Task PdfDocumentOperationsService_ExecutesCustomToolViaExecutionHandler()
    {
        var context = new FryPluginContext();
        var registry = new PdfToolRegistry(context);

        bool customHandlerCalled = false;
        var customToolId = (PdfToolId)9999;
        var customToolDef = new PdfToolDefinition
        {
            Id = customToolId,
            Name = "Unit Test Dynamic Tool",
            Description = "Custom tool with execution handler",
            Category = PdfToolCategory.EditAndForms,
            ExecutionHandler = (options, progress, ct) =>
            {
                customHandlerCalled = true;
                progress?.Report(100.0);
                return Task.FromResult(new ToolExecutionResult
                {
                    Success = true,
                    Message = "Custom plugin execution succeeded."
                });
            }
        };

        registry.RegisterTool(customToolDef);

        var opsService = new PdfDocumentOperationsService(
            registry,
            new PdfPageService(),
            new PdfOptimizationService(),
            new PdfSecurityService(),
            new PdfConversionService(),
            new PdfOcrService(),
            new PdfFormService(),
            new AiDocumentService(),
            new DocumentTranslationService(),
            new PdfWorkflowEngine(),
            context);

        var result = await opsService.ExecuteToolAsync(customToolId, new { DummyParam = 42 });

        Assert.True(customHandlerCalled, "Custom ExecutionHandler on PdfToolDefinition should have been executed");
        Assert.True(result.Success);
        Assert.Equal("Custom plugin execution succeeded.", result.Message);
    }

    [Fact]
    public void ImageElementViewModel_DefaultCornerRadius_IsM3Small8()
    {
        var imageVm = new ImageElementViewModel();
        Assert.Equal(8.0, imageVm.CornerRadius);
    }
}
