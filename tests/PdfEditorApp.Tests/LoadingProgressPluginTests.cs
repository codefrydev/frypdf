using System;
using System.Threading;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.Messaging;
using PdfEditorApp.Core.Plugins;
using PdfEditorApp.Core.Plugins.Loading;
using PdfEditorApp.Messages;
using PdfEditorApp.Plugins.Bundles;
using PdfEditorApp.Plugins.Loading;
using PdfEditorApp.ViewModels;
using Xunit;

namespace PdfEditorApp.Tests;

public class LoadingProgressPluginTests
{
    [Fact]
    public void LoadingProgressService_Show_Update_And_Hide_Lifecycle()
    {
        var service = new LoadingProgressService();
        int stateChangedCount = 0;
        service.StateChanged += () => stateChangedCount++;

        Assert.False(service.IsActive);
        Assert.Null(service.CurrentOptions);

        using (var handle = service.Show(new LoadingProgressOptions
        {
            Title = "Processing Document.pdf",
            Category = "TEST STUDIO",
            StatusMessage = "Initializing...",
            PipelinePhases = new[] { "Phase 1", "Phase 2", "Phase 3" },
            ActivePhaseIndex = 0
        }))
        {
            Assert.True(service.IsActive);
            Assert.NotNull(service.CurrentOptions);
            Assert.Equal("Processing Document.pdf", service.CurrentOptions.Title);
            Assert.Equal("TEST STUDIO", service.CurrentOptions.Category);
            Assert.Equal("Initializing...", service.CurrentOptions.StatusMessage);
            Assert.Equal(0, service.CurrentOptions.ActivePhaseIndex);

            // Update status via handle
            handle.UpdateStatus("Executing Phase 2...", progressPercent: 50.0, activePhaseIndex: 1);
            Assert.Equal("Executing Phase 2...", service.CurrentOptions.StatusMessage);
            Assert.Equal(50.0, service.CurrentOptions.ProgressPercent);
            Assert.Equal(1, service.CurrentOptions.ActivePhaseIndex);
        }

        // Handle disposed -> hidden
        Assert.False(service.IsActive);
        Assert.Null(service.CurrentOptions);
        Assert.True(stateChangedCount >= 3);
    }

    [Fact]
    public void LoadingProgressService_Cancellation_Triggers_Token_And_Callback()
    {
        var service = new LoadingProgressService();
        bool callbackFired = false;

        using var handle = service.Show(new LoadingProgressOptions
        {
            Title = "Heavy Operation",
            IsCancellable = true,
            OnCancel = () => callbackFired = true
        });

        Assert.False(handle.CancellationToken.IsCancellationRequested);

        service.Cancel();

        Assert.True(handle.CancellationToken.IsCancellationRequested);
        Assert.True(callbackFired);
        Assert.False(service.IsActive);
    }

    [Fact]
    public async Task LoadingProgressPlugin_Registers_In_Context_With_Reversible_Effect()
    {
        var rootContext = new FryPluginContext();
        var scope = new PluginScope();
        var pluginContext = rootContext.CreateScopedContext(scope);

        var plugin = new LoadingProgressPlugin();
        await plugin.ApplyAsync(pluginContext);

        Assert.True(rootContext.HasService<ILoadingProgressService>());
        var resolvedService = rootContext.GetLoadingProgressService();
        Assert.NotNull(resolvedService);

        // Show an operation
        resolvedService.Show(new LoadingProgressOptions { Title = "Plugin Task" });
        Assert.True(resolvedService.IsActive);

        // Unload plugin
        scope.Dispose();

        Assert.False(resolvedService.IsActive);
        Assert.False(rootContext.HasService<ILoadingProgressService>());
    }

    [Fact]
    public void ShellOverlaysBundle_Contains_LoadingProgressPlugin()
    {
        var bundle = new ShellOverlaysBundle();
        Assert.Contains(bundle.Plugins, p => p is LoadingProgressPlugin);
    }

    [Fact]
    public void LoadingProgressViewModel_Responds_To_PubSub_Messages()
    {
        var service = new LoadingProgressService();
        var vm = new LoadingProgressViewModel(service);

        Assert.False(vm.IsActive);

        // Send Show message
        WeakReferenceMessenger.Default.Send(new ShowLoadingProgressMessage(new LoadingProgressOptions
        {
            Title = "Rendering Book.pdf",
            Category = "PDF VIEWER",
            StatusMessage = "Deconstructing vectors...",
            FileSize = "12.4 MB",
            PipelinePhases = new[] { "Parse", "Render", "Cache" },
            ActivePhaseIndex = 1
        }));

        Assert.True(service.IsActive);

        // Send Update message
        WeakReferenceMessenger.Default.Send(new UpdateLoadingProgressMessage("Caching textures...", ProgressPercent: 90.0, ActivePhaseIndex: 2));
        Assert.Equal("Caching textures...", service.CurrentOptions?.StatusMessage);
        Assert.Equal(90.0, service.CurrentOptions?.ProgressPercent);
        Assert.Equal(2, service.CurrentOptions?.ActivePhaseIndex);

        // Send Hide message
        WeakReferenceMessenger.Default.Send(new HideLoadingProgressMessage());
        Assert.False(service.IsActive);
    }

    [Fact]
    public void LoadingProgressViewModel_CancelCommand_Cancels_And_Resets()
    {
        var service = new LoadingProgressService();
        var vm = new LoadingProgressViewModel(service);
        bool cancelExecuted = false;

        service.Show(new LoadingProgressOptions
        {
            Title = "Cancellable Job",
            IsCancellable = true,
            OnCancel = () => cancelExecuted = true
        });

        // Set VM state directly
        vm.IsActive = true;
        vm.IsCancellable = true;
        Assert.True(vm.CanCancel);

        // Execute cancel command
        vm.CancelCommand.Execute(null);

        Assert.True(cancelExecuted);
        Assert.False(vm.IsActive);
        Assert.False(service.IsActive);
    }
}
