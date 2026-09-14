using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Avalonia.Input;
using PdfEditorApp.Core.Plugins.Descriptors;
using PdfEditorApp.Models;
using PdfEditorApp.Services;
using PdfEditorApp.Services.Shortcuts;
using PdfEditorApp.ViewModels;
using PdfEditorApp.ViewModels.Shortcuts;
using Xunit;

namespace PdfEditorApp.Tests;

public class ShortcutRegistryTests : IDisposable
{
    private readonly string _tempDir;
    private readonly string _tempFile;

    public ShortcutRegistryTests()
    {
        _tempDir = Path.Combine(Path.GetTempPath(), "FryPdf_ShortcutTests_" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(_tempDir);
        _tempFile = Path.Combine(_tempDir, "test_ui_settings.json");
    }

    public void Dispose()
    {
        try
        {
            if (Directory.Exists(_tempDir))
            {
                Directory.Delete(_tempDir, true);
            }
        }
        catch
        {
            // Best effort cleanup
        }
    }

    [Fact]
    public void ShortcutRegistry_SeedsBuiltInShortcutsOnInitialization()
    {
        var settingsService = new UiSettingsService(_tempFile);
        var registry = new ShortcutRegistry(null, settingsService);

        var all = registry.GetAllShortcuts();
        Assert.NotEmpty(all);

        Assert.NotNull(registry.GetShortcut("file.new"));
        Assert.NotNull(registry.GetShortcut("file.open"));
        Assert.NotNull(registry.GetShortcut("file.save"));
        Assert.NotNull(registry.GetShortcut("edit.undo"));
        Assert.NotNull(registry.GetShortcut("edit.redo"));
        Assert.NotNull(registry.GetShortcut("zoom.in"));
        Assert.NotNull(registry.GetShortcut("zoom.out"));
    }

    [Fact]
    public void ShortcutRegistry_GetEffectiveGesture_HonorsCustomSettings()
    {
        var settingsService = new UiSettingsService(_tempFile);
        settingsService.UpdateSettings(s =>
        {
            s.CustomShortcuts["file.new"] = "Ctrl+Shift+Alt+N";
        });

        var registry = new ShortcutRegistry(null, settingsService);

        var effective = registry.GetEffectiveGesture("file.new");
        Assert.Equal("Ctrl+Shift+Alt+N", effective);
    }

    [Fact]
    public void ShortcutRegistry_RegistersAndUnregistersDynamically()
    {
        var settingsService = new UiSettingsService(_tempFile);
        var registry = new ShortcutRegistry(null, settingsService);

        bool eventFired = false;
        registry.ShortcutsChanged += () => eventFired = true;

        var descriptor = new ShortcutDescriptor
        {
            Id = "custom.plugin.action",
            Title = "Custom Action",
            Category = "Plugins",
            DefaultGesture = "Ctrl+K",
            Scope = ShortcutScope.Global,
            Action = _ => { }
        };

        var token = registry.RegisterShortcut(descriptor);
        Assert.True(eventFired);
        Assert.NotNull(registry.GetShortcut("custom.plugin.action"));

        eventFired = false;
        token.Dispose();
        Assert.True(eventFired);
        Assert.Null(registry.GetShortcut("custom.plugin.action"));
    }

    [Fact]
    public void ShortcutRegistry_TryDispatch_ExecutesGlobalShortcut()
    {
        var settingsService = new UiSettingsService(_tempFile);
        var registry = new ShortcutRegistry(null, settingsService);

        bool executed = false;
        registry.RegisterShortcut(new ShortcutDescriptor
        {
            Id = "test.global.ping",
            Title = "Test Ping",
            DefaultGesture = "F12",
            Scope = ShortcutScope.Global,
            Action = _ => executed = true
        });

        bool handled = registry.TryDispatch("F12", ShortcutModifiers.None, activeContextId: "RandomContext");
        Assert.True(handled);
        Assert.True(executed);
    }

    [Fact]
    public void ShortcutRegistry_TryDispatch_HonorsContextScope()
    {
        var settingsService = new UiSettingsService(_tempFile);
        var registry = new ShortcutRegistry(null, settingsService);

        bool executed = false;
        registry.RegisterShortcut(new ShortcutDescriptor
        {
            Id = "test.context.ping",
            Title = "Context Ping",
            DefaultGesture = "Ctrl+R",
            Scope = ShortcutScope.Context,
            ContextId = "CSharpStudio",
            Action = _ => executed = true
        });

        // 1. Wrong context -> should NOT execute
        bool handledWrong = registry.TryDispatch("R", ShortcutModifiers.Control, activeContextId: "PdfEditor");
        Assert.False(handledWrong);
        Assert.False(executed);

        // 2. Correct context -> SHOULD execute
        bool handledCorrect = registry.TryDispatch("R", ShortcutModifiers.Control, activeContextId: "CSharpStudio");
        Assert.True(handledCorrect);
        Assert.True(executed);
    }

    [Fact]
    public void ShortcutRegistry_TryDispatch_GuardsWithCanExecute()
    {
        var settingsService = new UiSettingsService(_tempFile);
        var registry = new ShortcutRegistry(null, settingsService);

        bool executed = false;
        bool canRun = false;

        registry.RegisterShortcut(new ShortcutDescriptor
        {
            Id = "test.guarded.action",
            Title = "Guarded Action",
            DefaultGesture = "F8",
            Scope = ShortcutScope.Global,
            CanExecute = _ => canRun,
            Action = _ => executed = true
        });

        // 1. CanExecute == false
        bool resultBlocked = registry.TryDispatch("F8", ShortcutModifiers.None, null);
        Assert.False(resultBlocked);
        Assert.False(executed);

        // 2. CanExecute == true
        canRun = true;
        bool resultPassed = registry.TryDispatch("F8", ShortcutModifiers.None, null);
        Assert.True(resultPassed);
        Assert.True(executed);
    }

    [Fact]
    public void ShortcutExtensions_ParseKeyModifiers_MapsCorrectly()
    {
        var mods1 = ShortcutExtensions.ParseKeyModifiers(KeyModifiers.Control | KeyModifiers.Shift);
        Assert.True(mods1.HasFlag(ShortcutModifiers.Control));
        Assert.True(mods1.HasFlag(ShortcutModifiers.Shift));
        Assert.False(mods1.HasFlag(ShortcutModifiers.Alt));

        var mods2 = ShortcutExtensions.ParseKeyModifiers(KeyModifiers.Meta | KeyModifiers.Alt);
        Assert.True(mods2.HasFlag(ShortcutModifiers.Meta));
        Assert.True(mods2.HasFlag(ShortcutModifiers.Alt));
    }

    [Fact]
    public void SettingsViewModel_Shortcuts_DetectsAndReportsConflicts()
    {
        var settingsService = new UiSettingsService(_tempFile);
        var registry = new ShortcutRegistry(null, settingsService);

        // Register two conflicting global shortcuts
        registry.RegisterShortcut(new ShortcutDescriptor
        {
            Id = "conflict.item.1",
            Title = "Action One",
            Category = "Test",
            DefaultGesture = "Ctrl+Shift+Z",
            Scope = ShortcutScope.Global
        });

        registry.RegisterShortcut(new ShortcutDescriptor
        {
            Id = "conflict.item.2",
            Title = "Action Two",
            Category = "Test",
            DefaultGesture = "Ctrl+Shift+Z",
            Scope = ShortcutScope.Global
        });

        var vm = new SettingsViewModel(settingsService, null, registry);
        vm.ReloadShortcuts();

        Assert.True(vm.HasConflict);

        var item1 = vm.ShortcutItems.FirstOrDefault(s => s.Id == "conflict.item.1");
        var item2 = vm.ShortcutItems.FirstOrDefault(s => s.Id == "conflict.item.2");

        Assert.NotNull(item1);
        Assert.NotNull(item2);
        Assert.True(item1.HasConflict);
        Assert.True(item2.HasConflict);
        Assert.Contains("Action Two", item1.ConflictMessage);
        Assert.Contains("Action One", item2.ConflictMessage);
    }

    [Fact]
    public void SettingsViewModel_Shortcuts_FiltersBySearchAndCategory()
    {
        var settingsService = new UiSettingsService(_tempFile);
        var registry = new ShortcutRegistry(null, settingsService);

        var vm = new SettingsViewModel(settingsService, null, registry);
        vm.ReloadShortcuts();

        Assert.NotEmpty(vm.ShortcutItems);

        // Filter by text
        vm.ShortcutSearchQuery = "Save";
        Assert.All(vm.FilteredShortcutItems, item =>
            Assert.True(
                item.Title.Contains("Save", StringComparison.OrdinalIgnoreCase) ||
                item.Description.Contains("Save", StringComparison.OrdinalIgnoreCase) ||
                item.EffectiveGesture.Contains("Save", StringComparison.OrdinalIgnoreCase)));

        // Clear text, filter by category
        vm.ShortcutSearchQuery = "";
        vm.SelectedCategoryFilter = "Zoom";
        Assert.All(vm.FilteredShortcutItems, item =>
            Assert.Equal("Zoom", item.Category, ignoreCase: true));
    }

    [Fact]
    public void SettingsViewModel_CommitRecordedGesture_PersistsAndReloads()
    {
        var settingsService = new UiSettingsService(_tempFile);
        var registry = new ShortcutRegistry(null, settingsService);

        var vm = new SettingsViewModel(settingsService, null, registry);
        vm.ReloadShortcuts();

        var item = vm.ShortcutItems.First(s => s.Id == "file.save");
        vm.StartRecording(item);
        vm.CommitRecordedGesture("Ctrl+Alt+S");

        Assert.Equal("Ctrl+Alt+S", settingsService.Settings.CustomShortcuts["file.save"]);

        // Reset single shortcut
        vm.ResetShortcut(item);
        Assert.False(settingsService.Settings.CustomShortcuts.ContainsKey("file.save"));
    }
}
