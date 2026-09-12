using System;
using System.Threading;
using System.Threading.Tasks;
using PdfEditorApp.Core.Models;
using PdfEditorApp.Core.Plugins.Loading;
using PdfEditorApp.Plugins.Loading;
using PdfEditorApp.Tests.Mocks;
using PdfEditorApp.ViewModels;
using Xunit;

namespace PdfEditorApp.Tests;

public class AppLifecycleAndStartupTests
{
    [Fact]
    public void StartupProgress_Options_And_PipelinePhases_Are_Correctly_Configured()
    {
        var service = new LoadingProgressService();
        var startupOptions = new LoadingProgressOptions
        {
            Title = "Starting FryPDF Studio",
            Category = "STARTUP & INITIALIZATION",
            StatusMessage = "Mounting 17 microkernel plugin bundles...",
            PipelinePhases = new[] { "Theme", "Plugins", "Engine", "Workspace" },
            ActivePhaseIndex = 1,
            IsCancellable = false,
            ProgressPercent = 30.0
        };

        using (var handle = service.Show(startupOptions))
        {
            Assert.True(service.IsActive);
            Assert.NotNull(service.CurrentOptions);
            Assert.Equal("Starting FryPDF Studio", service.CurrentOptions.Title);
            Assert.Equal("STARTUP & INITIALIZATION", service.CurrentOptions.Category);
            Assert.Equal(1, service.CurrentOptions.ActivePhaseIndex);
            Assert.Equal(4, service.CurrentOptions.PipelinePhases?.Count);
            Assert.False(service.CurrentOptions.IsCancellable);

            // Transition: Engine phase
            handle.UpdateStatus("Initializing Skia hardware accelerated canvas...", progressPercent: 65.0, activePhaseIndex: 2);
            Assert.Equal(2, service.CurrentOptions.ActivePhaseIndex);
            Assert.Equal(65.0, service.CurrentOptions.ProgressPercent);

            // Transition: Workspace phase
            handle.UpdateStatus("Setting up workspace & dashboard...", progressPercent: 90.0, activePhaseIndex: 3);
            Assert.Equal(3, service.CurrentOptions.ActivePhaseIndex);
            Assert.Equal(90.0, service.CurrentOptions.ProgressPercent);

            // Final Ready
            handle.UpdateStatus("Ready!", progressPercent: 100.0, activePhaseIndex: 3);
            Assert.Equal("Ready!", service.CurrentOptions.StatusMessage);
            Assert.Equal(100.0, service.CurrentOptions.ProgressPercent);
        }

        // After completion, the progress overlay is automatically dismissed
        Assert.False(service.IsActive);
        Assert.Null(service.CurrentOptions);
    }

    [Fact]
    public void ShutdownProgress_Options_And_PipelinePhases_Are_Correctly_Configured()
    {
        var service = new LoadingProgressService();
        var shutdownOptions = new LoadingProgressOptions
        {
            Title = "Saving & Closing FryPDF Studio",
            Category = "SHUTDOWN & PERSISTENCE",
            StatusMessage = "Checking active workspace and document state...",
            PipelinePhases = new[] { "Inspect", "AutoSave", "Preferences", "Shutdown" },
            ActivePhaseIndex = 0,
            IsCancellable = false,
            ProgressPercent = 15.0
        };

        using (var handle = service.Show(shutdownOptions))
        {
            Assert.True(service.IsActive);
            Assert.NotNull(service.CurrentOptions);
            Assert.Equal("Saving & Closing FryPDF Studio", service.CurrentOptions.Title);
            Assert.Equal("SHUTDOWN & PERSISTENCE", service.CurrentOptions.Category);
            Assert.Equal(0, service.CurrentOptions.ActivePhaseIndex);
            Assert.Equal(4, service.CurrentOptions.PipelinePhases?.Count);
            Assert.False(service.CurrentOptions.IsCancellable);

            handle.UpdateStatus("Preserving document recovery snapshot...", progressPercent: 50.0, activePhaseIndex: 1);
            Assert.Equal(1, service.CurrentOptions.ActivePhaseIndex);
            Assert.Equal(50.0, service.CurrentOptions.ProgressPercent);

            handle.UpdateStatus("Flushing diagnostics and persisting preferences...", progressPercent: 80.0, activePhaseIndex: 2);
            Assert.Equal(2, service.CurrentOptions.ActivePhaseIndex);
            Assert.Equal(80.0, service.CurrentOptions.ProgressPercent);

            handle.UpdateStatus("All work safely preserved. Goodbye!", progressPercent: 100.0, activePhaseIndex: 3);
            Assert.Equal(3, service.CurrentOptions.ActivePhaseIndex);
            Assert.Equal(100.0, service.CurrentOptions.ProgressPercent);
        }

        Assert.False(service.IsActive);
    }

    [Fact]
    public async Task NonBlockingShutdown_WithSlowResource_CompletesWithinBoundedTimeout()
    {
        var slowDisposable = new SlowAsyncDisposable(delayMilliseconds: 5000);
        var stopwatch = System.Diagnostics.Stopwatch.StartNew();

        using var cts = new CancellationTokenSource(TimeSpan.FromMilliseconds(200));
        var teardownTask = Task.Run(async () =>
        {
            try
            {
                await slowDisposable.DisposeAsync();
            }
            catch
            {
                // Ignored in shutdown
            }
        }, cts.Token);

        var completedTask = await Task.WhenAny(teardownTask, Task.Delay(TimeSpan.FromMilliseconds(200)));
        stopwatch.Stop();

        Assert.NotSame(teardownTask, completedTask);
        Assert.True(stopwatch.ElapsedMilliseconds < 2000, "Shutdown must not block the calling thread indefinitely.");
    }

    [Fact]
    public async Task NonBlockingShutdown_WithNormalResource_CompletesCleanly()
    {
        var normalDisposable = new SlowAsyncDisposable(delayMilliseconds: 20);

        using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(2));
        var teardownTask = Task.Run(async () =>
        {
            await normalDisposable.DisposeAsync();
        }, cts.Token);

        var completedTask = await Task.WhenAny(teardownTask, Task.Delay(TimeSpan.FromSeconds(2)));

        Assert.Same(teardownTask, completedTask);
        Assert.True(normalDisposable.IsDisposed);
    }

    [Fact]
    public void MainViewModel_HasUnsavedWork_Accurately_Detects_Changes()
    {
        var mockPersistence = new MockProjectPersistenceService();
        var mainVm = new MainViewModel(new PdfEditorApp.Services.PdfExportService(), new PdfEditorApp.Services.TemplateService(), mockPersistence);

        // Blank with 0 pages -> false
        mainVm.Pages.Clear();
        Assert.False(mainVm.HasUnsavedWork);

        // Add an empty page with 0 elements and no file path
        var page = new PageViewModel { PageNumber = 1, Width = 612, Height = 792 };
        mainVm.Pages.Add(page);
        Assert.False(mainVm.HasUnsavedWork);

        // Add an element to the page -> should detect unsaved work!
        page.AddElement(new PdfEditorApp.ViewModels.ElementViewModels.TextElementViewModel { Text = "Test Heading" });
        Assert.True(mainVm.HasUnsavedWork);

        // Or if file path is set -> should detect unsaved work
        page.Elements.Clear();
        mainVm.CurrentFilePath = "/Users/test/Documents/Document.pdf";
        Assert.True(mainVm.HasUnsavedWork);
    }

    [Fact]
    public async Task MainViewModel_AutoSaveCurrentDocumentAsync_Invokes_Persistence()
    {
        var mockPersistence = new MockProjectPersistenceService();
        var mainVm = new MainViewModel(new PdfEditorApp.Services.PdfExportService(), new PdfEditorApp.Services.TemplateService(), mockPersistence);

        mainVm.Pages.Clear();
        var page = new PageViewModel { PageNumber = 1, Width = 612, Height = 792 };
        page.AddElement(new PdfEditorApp.ViewModels.ElementViewModels.TextElementViewModel { Text = "Persistent Title" });
        mainVm.Pages.Add(page);
        mainVm.DocumentTitle = "My Work";
        mainVm.CurrentFilePath = "/path/to/MyWork.frypdf";

        await mainVm.AutoSaveCurrentDocumentAsync();

        Assert.Single(mockPersistence.AutoSavedProjects);
        var (savedModel, savedPath) = mockPersistence.AutoSavedProjects[0];
        Assert.Equal("/path/to/MyWork.frypdf", savedPath);
        Assert.Equal("My Work", savedModel.Title);
        Assert.Single(savedModel.Pages);
    }

    [Fact]
    public async Task MainViewModel_PrepareForShutdownAsync_Executes_Without_Hanging_And_Saves_Work()
    {
        var mockPersistence = new MockProjectPersistenceService();
        var mainVm = new MainViewModel(new PdfEditorApp.Services.PdfExportService(), new PdfEditorApp.Services.TemplateService(), mockPersistence);
        var progressService = new LoadingProgressService();

        var page = new PageViewModel { PageNumber = 1, Width = 612, Height = 792 };
        page.AddElement(new PdfEditorApp.ViewModels.ElementViewModels.TextElementViewModel { Text = "Work in progress" });
        mainVm.Pages.Add(page);

        using var handle = progressService.Show(new LoadingProgressOptions
        {
            Title = "Saving & Closing FryPDF Studio",
            PipelinePhases = new[] { "Inspect", "AutoSave", "Preferences", "Shutdown" }
        });

        await mainVm.PrepareForShutdownAsync(handle);

        // Verify autosave was performed
        Assert.Single(mockPersistence.AutoSavedProjects);
        // Verify final phase was reached
        Assert.Equal(3, progressService.CurrentOptions?.ActivePhaseIndex);
        Assert.Equal(100.0, progressService.CurrentOptions?.ProgressPercent);
    }

    [Fact]
    public async Task MainViewModel_CheckForRecoverableAutoSave_And_Restore()
    {
        var mockPersistence = new MockProjectPersistenceService
        {
            HasRecoverable = true,
            RecoverablePath = "/tmp/recovered.autosave.frypdf",
            ModelToReturnOnLoad = new PdfDocumentModel
            {
                Title = "Restored Project",
                Pages = { new PdfPageModel { PageNumber = 1, Width = 600, Height = 800 } }
            }
        };

        var mainVm = new MainViewModel(new PdfEditorApp.Services.PdfExportService(), new PdfEditorApp.Services.TemplateService(), mockPersistence);

        bool hasRecoverable = mainVm.CheckForRecoverableAutoSave(out var path);
        Assert.True(hasRecoverable);
        Assert.Equal("/tmp/recovered.autosave.frypdf", path);

        bool restored = await mainVm.RestoreAutoSaveAsync(path);
        Assert.True(restored);
        Assert.Equal("Restored Project", mainVm.DocumentTitle);
        Assert.Single(mainVm.Pages);
    }

    private class SlowAsyncDisposable : IAsyncDisposable
    {
        private readonly int _delayMilliseconds;
        public bool IsDisposed { get; private set; }

        public SlowAsyncDisposable(int delayMilliseconds)
        {
            _delayMilliseconds = delayMilliseconds;
        }

        public async ValueTask DisposeAsync()
        {
            await Task.Delay(_delayMilliseconds);
            IsDisposed = true;
        }
    }
}
