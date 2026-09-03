using System;
using System.IO;
using Avalonia.Layout;
using PdfEditorApp.Core.Models;
using PdfEditorApp.Models;
using PdfEditorApp.Services;
using PdfEditorApp.ViewModels;
using Xunit;

namespace PdfEditorApp.Tests;

public class UiSettingsTests : IDisposable
{
    private readonly string _tempSettingsDir;
    private readonly string _tempSettingsFile;

    public UiSettingsTests()
    {
        _tempSettingsDir = Path.Combine(Path.GetTempPath(), "FryPdf_UiSettingsTests_" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(_tempSettingsDir);
        _tempSettingsFile = Path.Combine(_tempSettingsDir, "test_ui_settings.json");
    }

    public void Dispose()
    {
        try
        {
            if (Directory.Exists(_tempSettingsDir))
            {
                Directory.Delete(_tempSettingsDir, true);
            }
        }
        catch
        {
            // Best effort cleanup
        }
    }

    [Fact]
    public void UiSettingsService_InitializesWithDefaults_WhenFileDoesNotExist()
    {
        var service = new UiSettingsService(_tempSettingsFile);

        Assert.Equal(ToastPosition.BottomCenter, service.Settings.ToastPosition);
        Assert.Equal(ToastStyleVariant.Solid, service.Settings.ToastStyleVariant);
        Assert.Equal(3500, service.Settings.ToastDurationMs);
        Assert.True(service.Settings.ToastShowCloseButton);
        Assert.False(service.Settings.ToastSoundEnabled);
        Assert.Equal(AppThemeMode.Light, service.Settings.ThemeMode);
        Assert.Equal(PdfReaderTheme.Default, service.Settings.ReadingTheme);
        Assert.False(service.Settings.ShowGridByDefault);
        Assert.False(service.Settings.SnapToGridByDefault);
    }

    [Fact]
    public void UiSettingsService_UpdateSettings_PersistsToDiskAndFiresEvent()
    {
        var service = new UiSettingsService(_tempSettingsFile);
        UiSettingsModel? received = null;
        service.SettingsChanged += (s) => received = s;

        service.UpdateSettings(s =>
        {
            s.ToastPosition = ToastPosition.TopRight;
            s.ToastStyleVariant = ToastStyleVariant.Subtle;
            s.ToastDurationMs = 6000;
            s.ToastShowCloseButton = false;
        });

        Assert.True(File.Exists(_tempSettingsFile));
        Assert.NotNull(received);
        Assert.Equal(ToastPosition.TopRight, received.ToastPosition);
        Assert.Equal(ToastStyleVariant.Subtle, received.ToastStyleVariant);
        Assert.Equal(6000, received.ToastDurationMs);
        Assert.False(received.ToastShowCloseButton);

        // Verify persistence by loading in new instance
        var reloadedService = new UiSettingsService(_tempSettingsFile);
        Assert.Equal(ToastPosition.TopRight, reloadedService.Settings.ToastPosition);
        Assert.Equal(ToastStyleVariant.Subtle, reloadedService.Settings.ToastStyleVariant);
        Assert.Equal(6000, reloadedService.Settings.ToastDurationMs);
        Assert.False(reloadedService.Settings.ToastShowCloseButton);
    }

    [Fact]
    public void UiSettingsService_ResetToDefaults_RestoresDefaultsAndSaves()
    {
        var service = new UiSettingsService(_tempSettingsFile);
        service.UpdateSettings(s =>
        {
            s.ToastPosition = ToastPosition.TopLeft;
            s.ToastDurationMs = 2000;
        });

        Assert.Equal(ToastPosition.TopLeft, service.Settings.ToastPosition);

        service.ResetToDefaults();

        Assert.Equal(ToastPosition.BottomCenter, service.Settings.ToastPosition);
        Assert.Equal(3500, service.Settings.ToastDurationMs);

        var reloaded = new UiSettingsService(_tempSettingsFile);
        Assert.Equal(ToastPosition.BottomCenter, reloaded.Settings.ToastPosition);
    }

    [Fact]
    public void UiSettingsService_CorruptJson_GracefullyFallsBackToDefaults()
    {
        File.WriteAllText(_tempSettingsFile, "{ broken-json: invalid-content");

        var service = new UiSettingsService(_tempSettingsFile);

        Assert.NotNull(service.Settings);
        Assert.Equal(ToastPosition.BottomCenter, service.Settings.ToastPosition);
    }

    [Theory]
    [InlineData("Could not open file: unrecognized format", null, ToastNotificationType.Danger)]
    [InlineData("Failed to export document", null, ToastNotificationType.Danger)]
    [InlineData("Corrupt or invalid PDF file", null, ToastNotificationType.Danger)]
    [InlineData("Something broke", "AlertCircleOutline", ToastNotificationType.Danger)]
    [InlineData("Warning: document contains unsaved edits", null, ToastNotificationType.Warning)]
    [InlineData("Nothing to undo", null, ToastNotificationType.Warning)]
    [InlineData("Saved successfully to /path/test.pdf", null, ToastNotificationType.Success)]
    [InlineData("Copied 3 Elements", null, ToastNotificationType.Success)]
    [InlineData("Pasted 1 Element", null, ToastNotificationType.Success)]
    [InlineData("Created new document from Blank template", null, ToastNotificationType.Success)]
    [InlineData("Switched to Dark Studio Theme", null, ToastNotificationType.Primary)]
    [InlineData("Active Tool: Text", null, ToastNotificationType.Primary)]
    [InlineData("Zoom: 125%", null, ToastNotificationType.Primary)]
    [InlineData("Fit to Width", null, ToastNotificationType.Primary)]
    [InlineData("General studio notice", null, ToastNotificationType.General)]
    public void MainViewModel_InferToastType_ClassifiesCorrectly(string message, string? icon, ToastNotificationType expected)
    {
        var result = MainViewModel.InferToastType(message, icon);
        Assert.Equal(expected, result);
    }

    [Theory]
    [InlineData(ToastPosition.TopLeft, HorizontalAlignment.Left, VerticalAlignment.Top, 32, 54, 0, 0)]
    [InlineData(ToastPosition.TopCenter, HorizontalAlignment.Center, VerticalAlignment.Top, 0, 54, 0, 0)]
    [InlineData(ToastPosition.TopRight, HorizontalAlignment.Right, VerticalAlignment.Top, 0, 54, 32, 0)]
    [InlineData(ToastPosition.BottomLeft, HorizontalAlignment.Left, VerticalAlignment.Bottom, 32, 0, 0, 48)]
    [InlineData(ToastPosition.BottomCenter, HorizontalAlignment.Center, VerticalAlignment.Bottom, 0, 0, 0, 48)]
    [InlineData(ToastPosition.BottomRight, HorizontalAlignment.Right, VerticalAlignment.Bottom, 0, 0, 32, 48)]
    public void MainViewModel_ToastPlacement_CalculatesCoordinatesProperly(
        ToastPosition position,
        HorizontalAlignment expectedHoriz,
        VerticalAlignment expectedVert,
        double left, double top, double right, double bottom)
    {
        var vm = new MainViewModel();
        vm.ToastPosition = position;

        Assert.Equal(expectedHoriz, vm.ToastHorizontalAlignment);
        Assert.Equal(expectedVert, vm.ToastVerticalAlignment);
        Assert.Equal(left, vm.ToastMargin.Left);
        Assert.Equal(top, vm.ToastMargin.Top);
        Assert.Equal(right, vm.ToastMargin.Right);
        Assert.Equal(bottom, vm.ToastMargin.Bottom);
    }

    [Fact]
    public void MainViewModel_DismissToast_HidesNotification()
    {
        var vm = new MainViewModel();
        vm.ShowToast("Test message", ToastNotificationType.Success);
        Assert.True(vm.IsToastVisible);

        vm.DismissToastCommand.Execute(null);

        Assert.False(vm.IsToastVisible);
    }

    [Fact]
    public void SettingsViewModel_TestPlaygroundCommands_TriggerCorrectNotifications()
    {
        var service = new UiSettingsService(_tempSettingsFile);
        var vm = new SettingsViewModel(service);

        string? receivedMsg = null;
        ToastNotificationType? receivedType = null;
        string? receivedIcon = null;

        vm.TriggerToastRequested += (msg, type, icon) =>
        {
            receivedMsg = msg;
            receivedType = type;
            receivedIcon = icon;
        };

        // Test Primary
        vm.TestPrimaryNotificationCommand.Execute(null);
        Assert.Equal("Primary message comes here", receivedMsg);
        Assert.Equal(ToastNotificationType.Primary, receivedType);
        Assert.Equal("InformationOutline", receivedIcon);

        // Test Success
        vm.TestSuccessNotificationCommand.Execute(null);
        Assert.Equal("Success message comes here", receivedMsg);
        Assert.Equal(ToastNotificationType.Success, receivedType);
        Assert.Equal("CheckCircleOutline", receivedIcon);

        // Test Danger
        vm.TestDangerNotificationCommand.Execute(null);
        Assert.Equal("Danger message comes here", receivedMsg);
        Assert.Equal(ToastNotificationType.Danger, receivedType);
        Assert.Equal("AlertOctagonOutline", receivedIcon);

        // Test Warning
        vm.TestWarningNotificationCommand.Execute(null);
        Assert.Equal("Warning message comes here", receivedMsg);
        Assert.Equal(ToastNotificationType.Warning, receivedType);
        Assert.Equal("AlertOutline", receivedIcon);

        // Test General
        vm.TestGeneralNotificationCommand.Execute(null);
        Assert.Equal("General message comes here", receivedMsg);
        Assert.Equal(ToastNotificationType.General, receivedType);
        Assert.Equal("InformationOutline", receivedIcon);
    }

    [Fact]
    public void SettingsViewModel_PositionAndStyleCommands_MutateStateAndService()
    {
        var service = new UiSettingsService(_tempSettingsFile);
        var vm = new SettingsViewModel(service);

        vm.SetToastPositionCommand.Execute(ToastPosition.TopCenter);
        Assert.Equal(ToastPosition.TopCenter, vm.ToastPosition);
        Assert.Equal(ToastPosition.TopCenter, service.Settings.ToastPosition);

        vm.SetToastStyleVariantCommand.Execute(ToastStyleVariant.Subtle);
        Assert.Equal(ToastStyleVariant.Subtle, vm.ToastStyleVariant);
        Assert.Equal(ToastStyleVariant.Subtle, service.Settings.ToastStyleVariant);

        vm.SetToastDurationCommand.Execute(6000);
        Assert.Equal(6000, vm.ToastDurationMs);
        Assert.Equal(6000, service.Settings.ToastDurationMs);

        vm.ToggleToastCloseButtonCommand.Execute(null);
        Assert.False(vm.ToastShowCloseButton);
        Assert.False(service.Settings.ToastShowCloseButton);
    }

    [Fact]
    public void SettingsViewModel_Commands_AcceptStringParametersFromXaml()
    {
        var service = new UiSettingsService(_tempSettingsFile);
        var vm = new SettingsViewModel(service);

        // Position via string (from XAML CommandParameter="TopLeft")
        Assert.True(vm.SetToastPositionCommand.CanExecute("TopLeft"));
        vm.SetToastPositionCommand.Execute("TopLeft");
        Assert.Equal(ToastPosition.TopLeft, vm.ToastPosition);
        Assert.Equal(ToastPosition.TopLeft, service.Settings.ToastPosition);

        Assert.True(vm.SetToastPositionCommand.CanExecute("BottomRight"));
        vm.SetToastPositionCommand.Execute("BottomRight");
        Assert.Equal(ToastPosition.BottomRight, vm.ToastPosition);

        // Style variant via string (from XAML CommandParameter="Subtle")
        Assert.True(vm.SetToastStyleVariantCommand.CanExecute("Subtle"));
        vm.SetToastStyleVariantCommand.Execute("Subtle");
        Assert.Equal(ToastStyleVariant.Subtle, vm.ToastStyleVariant);
        Assert.False(vm.PreviewIsSolid);

        // Duration via string (from XAML CommandParameter="2000")
        Assert.True(vm.SetToastDurationCommand.CanExecute("2000"));
        vm.SetToastDurationCommand.Execute("2000");
        Assert.Equal(2000, vm.ToastDurationMs);

        // Theme mode via string
        Assert.True(vm.SetThemeModeCommand.CanExecute("Dark"));
        vm.SetThemeModeCommand.Execute("Dark");
        Assert.Equal(AppThemeMode.Dark, vm.ThemeMode);

        // Reading theme via string
        Assert.True(vm.SetReadingThemeCommand.CanExecute("Sepia"));
        vm.SetReadingThemeCommand.Execute("Sepia");
        Assert.Equal(PdfReaderTheme.Sepia, vm.ReadingTheme);

        // Zoom mode via string
        Assert.True(vm.SetDefaultZoomModeCommand.CanExecute("FitWidth"));
        vm.SetDefaultZoomModeCommand.Execute("FitWidth");
        Assert.Equal(PdfViewerZoomMode.FitWidth, vm.DefaultZoomMode);
    }

    [Fact]
    public void SettingsViewModel_LivePreviewBrushes_UpdateCorrectly()
    {
        var service = new UiSettingsService(_tempSettingsFile);
        var vm = new SettingsViewModel(service);

        // Solid Primary
        vm.SetToastStyleVariantCommand.Execute(ToastStyleVariant.Solid);
        vm.TestPrimaryNotificationCommand.Execute(null);
        Assert.True(vm.PreviewIsSolid);
        Assert.NotNull(vm.PreviewBackgroundBrush);
        Assert.NotNull(vm.PreviewForegroundBrush);

        // Subtle Danger
        vm.SetToastStyleVariantCommand.Execute(ToastStyleVariant.Subtle);
        vm.TestDangerNotificationCommand.Execute(null);
        Assert.False(vm.PreviewIsSolid);
        Assert.Equal(ToastNotificationType.Danger, vm.PreviewToastType);
        Assert.NotNull(vm.PreviewBackgroundBrush);
        Assert.NotNull(vm.PreviewBorderBrush);
    }

    [Fact]
    public void MainViewModel_FullPipeline_PlaygroundButtonTriggersMainToast()
    {
        var mainVm = new MainViewModel();

        // Trigger Success test from settings playground
        mainVm.Home.Settings.TestSuccessNotificationCommand.Execute(null);

        Assert.True(mainVm.IsToastVisible);
        Assert.Equal(ToastNotificationType.Success, mainVm.ToastType);
        Assert.Equal("Success message comes here", mainVm.ToastMessage);
        Assert.Equal("CheckCircleOutline", mainVm.ToastIcon);
    }

    [Fact]
    public void HomeViewModel_SettingsNavigation_WorksCleanly()
    {
        var home = new HomeViewModel();
        Assert.False(home.IsSettingsSection);

        home.SelectNavSectionCommand.Execute("Settings");

        Assert.Equal(HomeNavSection.Settings, home.SelectedNavSection);
        Assert.True(home.IsSettingsSection);
        Assert.False(home.IsHomeSection);
        Assert.NotNull(home.Settings);
    }
}
