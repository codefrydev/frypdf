using System;
using PdfEditorApp.Models;
using PdfEditorApp.Services;
using PdfEditorApp.ViewModels;
using Xunit;

namespace PdfEditorApp.Tests;

public class ThemeServiceTests
{
    [Fact]
    public void ThemeService_InitialState_DefaultsToLight()
    {
        // Arrange
        var service = new ThemeService();

        // Assert
        Assert.Equal(AppThemeMode.Light, service.CurrentTheme);
        Assert.False(service.IsDarkMode);
    }

    [Fact]
    public void ThemeService_SetTheme_ChangesThemeAndRaisesEvent()
    {
        // Arrange
        var service = new ThemeService();
        AppThemeMode? receivedTheme = null;
        service.ThemeChanged += (t) => receivedTheme = t;

        // Act
        service.SetTheme(AppThemeMode.Dark);

        // Assert
        Assert.Equal(AppThemeMode.Dark, service.CurrentTheme);
        Assert.True(service.IsDarkMode);
        Assert.Equal(AppThemeMode.Dark, receivedTheme);
    }

    [Fact]
    public void ThemeService_ToggleTheme_CyclesBetweenLightAndDark()
    {
        // Arrange
        var service = new ThemeService();
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
    public void HomeViewModel_ToggleThemeCommand_SynchronizesWithThemeService()
    {
        // Arrange
        var themeService = new ThemeService();
        var homeVm = new HomeViewModel(
            new RecentDocumentsService(),
            new TemplateService(),
            new ProjectPersistenceService(),
            new Services.Tools.PdfToolRegistry(),
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
