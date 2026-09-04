using System;
using System.IO;
using System.Text.Json;
using PdfEditorApp.Core.Models;
using PdfEditorApp.Models;
using PdfEditorApp.Services;
using PdfEditorApp.Services.Tools.Core;
using PdfEditorApp.Tests.Mocks;
using PdfEditorApp.ViewModels;
using Xunit;

namespace PdfEditorApp.Tests;

public class ThemeServiceTests : IDisposable
{
    private readonly string _settingsFilePath;
    private readonly string? _originalBackup;

    public ThemeServiceTests()
    {
        _settingsFilePath = ThemeService.GetSettingsFilePath();
        if (File.Exists(_settingsFilePath))
        {
            _originalBackup = File.ReadAllText(_settingsFilePath);
        }
    }

    public void Dispose()
    {
        try
        {
            if (_originalBackup != null)
            {
                File.WriteAllText(_settingsFilePath, _originalBackup);
            }
            else if (File.Exists(_settingsFilePath))
            {
                File.Delete(_settingsFilePath);
            }
        }
        catch
        {
            // Clean up best effort
        }
    }

    [Fact]
    public void ThemeService_InitialState_LoadsFromDiskOrDefaultsToLight()
    {
        // Arrange: remove file if present
        if (File.Exists(_settingsFilePath)) File.Delete(_settingsFilePath);

        // Act
        var service = new ThemeService();

        // Assert
        Assert.Equal(AppThemeMode.Light, service.CurrentTheme);
        Assert.Equal(PdfReaderTheme.Default, service.ReadingTheme);
        Assert.False(service.IsDarkMode);
    }

    [Fact]
    public void ThemeService_SetTheme_ChangesThemeAndRaisesEvent()
    {
        // Arrange
        var service = new ThemeService();
        var targetTheme = service.CurrentTheme == AppThemeMode.Dark ? AppThemeMode.Light : AppThemeMode.Dark;
        AppThemeMode? receivedTheme = null;
        service.ThemeChanged += (t) => receivedTheme = t;

        // Act
        service.SetTheme(targetTheme);

        // Assert
        Assert.Equal(targetTheme, service.CurrentTheme);
        Assert.Equal(targetTheme == AppThemeMode.Dark, service.IsDarkMode);
        Assert.Equal(targetTheme, receivedTheme);
    }

    [Fact]
    public void ThemeService_ToggleTheme_CyclesBetweenLightAndDark()
    {
        // Arrange
        var service = new ThemeService();
        service.SetTheme(AppThemeMode.Light);
        Assert.Equal(AppThemeMode.Light, service.CurrentTheme);

        // Act 1: Toggle to Dark
        service.ToggleTheme();
        Assert.Equal(AppThemeMode.Dark, service.CurrentTheme);
        Assert.True(service.IsDarkMode);

        // Act 2: Toggle back to Light
        service.ToggleTheme();
        Assert.Equal(AppThemeMode.Light, service.CurrentTheme);
        Assert.False(service.IsDarkMode);
    }

    [Fact]
    public void ThemeService_PersistsTheme_ToDisk_AndRestoresUponReopening()
    {
        // Arrange: instantiate service and save Dark mode
        var service1 = new ThemeService();
        service1.SetTheme(AppThemeMode.Dark);

        // Verify file was written
        Assert.True(File.Exists(_settingsFilePath));
        string savedJson = File.ReadAllText(_settingsFilePath);
        Assert.Contains("Dark", savedJson);

        // Act: create brand new ThemeService instance (simulating app restart)
        var service2 = new ThemeService();

        // Assert: should restore Dark theme immediately from disk
        Assert.Equal(AppThemeMode.Dark, service2.CurrentTheme);
        Assert.True(service2.IsDarkMode);
    }

    [Fact]
    public void ThemeService_PersistsReadingTheme_ToDisk_AndRestoresUponReopening()
    {
        // Arrange: instantiate service and set reading theme to Sepia
        var service1 = new ThemeService();
        service1.SetReadingTheme(PdfReaderTheme.Sepia);

        // Verify file was written
        Assert.True(File.Exists(_settingsFilePath));
        string savedJson = File.ReadAllText(_settingsFilePath);
        Assert.Contains("Sepia", savedJson);

        // Act: create brand new ThemeService instance (simulating app restart)
        var service2 = new ThemeService();

        // Assert: should restore Sepia reading theme immediately from disk
        Assert.Equal(PdfReaderTheme.Sepia, service2.ReadingTheme);
    }

    [Fact]
    public void ThemeService_HandlesCaseInsensitiveAndLegacyJson_Gracefully()
    {
        // Arrange: manually write lowercase / legacy json
        var legacyJson = "{\n  \"theme\": \"dark\",\n  \"readingTheme\": \"highcontrast\",\n  \"isDarkMode\": true\n}";
        var dir = ThemeService.GetSettingsDirectory();
        Directory.CreateDirectory(dir);
        File.WriteAllText(_settingsFilePath, legacyJson);

        // Act: instantiate new ThemeService
        var service = new ThemeService();

        // Assert
        Assert.Equal(AppThemeMode.Dark, service.CurrentTheme);
        Assert.Equal(PdfReaderTheme.HighContrast, service.ReadingTheme);
        Assert.True(service.IsDarkMode);
    }

    [Fact]
    public void ThemeService_HandlesNumericEnumJson_Gracefully()
    {
        // Arrange: numeric enum values (0 = System, 1 = Light, 2 = Dark)
        var numericJson = "{\n  \"Theme\": 2,\n  \"ReadingTheme\": 1\n}";
        var dir = ThemeService.GetSettingsDirectory();
        Directory.CreateDirectory(dir);
        File.WriteAllText(_settingsFilePath, numericJson);

        // Act: instantiate new ThemeService
        var service = new ThemeService();

        // Assert
        Assert.Equal(AppThemeMode.Dark, service.CurrentTheme);
        Assert.Equal(PdfReaderTheme.Sepia, service.ReadingTheme);
    }

    [Fact]
    public void ThemeService_HandlesCorruptedJson_FallsBackToDefaults()
    {
        // Arrange: corrupt json
        var corruptJson = "{ INVALID JSON !! @@ }}";
        var dir = ThemeService.GetSettingsDirectory();
        Directory.CreateDirectory(dir);
        File.WriteAllText(_settingsFilePath, corruptJson);

        // Act: instantiate new ThemeService
        var service = new ThemeService();

        // Assert
        Assert.Equal(AppThemeMode.Light, service.CurrentTheme);
        Assert.Equal(PdfReaderTheme.Default, service.ReadingTheme);
    }

    [Fact]
    public void HomeViewModel_ToggleThemeCommand_SynchronizesWithThemeService()
    {
        // Arrange
        var themeService = new ThemeService();
        themeService.SetTheme(AppThemeMode.Light);

        var homeVm = new HomeViewModel(
            new MockRecentDocumentsService(),
            new TemplateService(),
            new ProjectPersistenceService(),
            new PdfToolRegistry(),
            themeService: themeService);

        Assert.False(homeVm.IsDarkMode);

        // Act 1: Toggle via HomeViewModel
        homeVm.ToggleThemeCommand.Execute(null);

        // Assert
        Assert.True(homeVm.IsDarkMode);
        Assert.True(themeService.IsDarkMode);

        // Act 2: Toggle back
        homeVm.ToggleThemeCommand.Execute(null);

        // Assert
        Assert.False(homeVm.IsDarkMode);
        Assert.False(themeService.IsDarkMode);
    }

    [Fact]
    public void MainViewModel_ThemeCommands_SwitchThemesAndSyncHomeVm()
    {
        // Arrange
        var themeService = new ThemeService();
        themeService.SetTheme(AppThemeMode.Light);

        var mainVm = new MainViewModel(
            new PdfExportService(),
            new TemplateService(),
            new ProjectPersistenceService(),
            themeService: themeService);

        Assert.False(mainVm.IsDarkMode);
        Assert.False(mainVm.Home.IsDarkMode);

        // Act 1: Set Dark Theme
        mainVm.SetDarkThemeCommand.Execute(null);
        Assert.True(mainVm.IsDarkMode);
        Assert.True(mainVm.Home.IsDarkMode);
        Assert.Equal(AppThemeMode.Dark, themeService.CurrentTheme);

        // Act 2: Set Light Theme
        mainVm.SetLightThemeCommand.Execute(null);
        Assert.False(mainVm.IsDarkMode);
        Assert.False(mainVm.Home.IsDarkMode);
        Assert.Equal(AppThemeMode.Light, themeService.CurrentTheme);

        // Act 3: Toggle Theme
        mainVm.ToggleThemeCommand.Execute(null);
        Assert.True(mainVm.IsDarkMode);
        Assert.True(mainVm.Home.IsDarkMode);
    }

    [Fact]
    public void MainViewModel_PdfViewer_SyncsAndPersistsReadingTheme()
    {
        // Arrange
        var themeService = new ThemeService();
        var mainVm = new MainViewModel(
            new PdfExportService(),
            new TemplateService(),
            new ProjectPersistenceService(),
            themeService: themeService);

        // Act: User selects Dark reading mode in PDF viewer
        mainVm.PdfViewer.SetReadingThemeCommand.Execute("Dark");

        // Assert: MainViewModel, ThemeService and PdfViewer are all synced
        Assert.Equal(PdfReaderTheme.Dark, mainVm.PdfViewer.ReadingTheme);
        Assert.Equal(PdfReaderTheme.Dark, themeService.ReadingTheme);

        // Simulating reopen
        var reopenedService = new ThemeService();
        Assert.Equal(PdfReaderTheme.Dark, reopenedService.ReadingTheme);
    }

    [Fact]
    public void CommandPalette_ContainsThemeCommands()
    {
        // Arrange
        var mainVm = new MainViewModel(
            new PdfExportService(),
            new TemplateService(),
            new ProjectPersistenceService());

        // Act
        mainVm.FilterPaletteCommands("Theme");

        // Assert
        Assert.NotEmpty(mainVm.FilteredPaletteCommands);
        Assert.Contains(mainVm.FilteredPaletteCommands, c => c.Title.Contains("Toggle Dark / Light Theme"));
        Assert.Contains(mainVm.FilteredPaletteCommands, c => c.Title.Contains("Switch to Dark Theme"));
        Assert.Contains(mainVm.FilteredPaletteCommands, c => c.Title.Contains("Switch to Light Theme"));
    }
}
