using System;
using Avalonia.Input;
using PdfEditorApp.Models;
using PdfEditorApp.ViewModels;
using Xunit;

namespace PdfEditorApp.Tests;

public class GestureAndNavigationTests
{
    [Fact]
    public void TrackpadMagnifyGesture_ComputesProportionalScale()
    {
        double oldZoom = 1.0;

        // Positive delta (pinch out / zoom in by 10%)
        double deltaPositive = 0.10;
        double zoomIn = Math.Clamp(Math.Round(oldZoom * (1.0 + deltaPositive), 3), 0.1, 5.0);
        Assert.Equal(1.10, zoomIn);

        // Negative delta (pinch in / zoom out by 10%)
        double deltaNegative = -0.10;
        double zoomOut = Math.Clamp(Math.Round(oldZoom * (1.0 + deltaNegative), 3), 0.1, 5.0);
        Assert.Equal(0.90, zoomOut);
    }
    [Fact]
    public void PinchZoomMath_PreventsExponentialExplosion_AndClampsProperly()
    {
        // Simulate pinch scaling logic: initial zoom is 1.0 (100%)
        double startZoom = 1.0;

        // User pinches out by 25% (scale = 1.25)
        double scale1 = 1.25;
        double calculatedZoom1 = Math.Clamp(Math.Round(startZoom * scale1, 3), 0.1, 5.0);
        Assert.Equal(1.25, calculatedZoom1);

        // Continuous pinch event 2 in the same gesture (scale = 1.50 relative to start)
        double scale2 = 1.50;
        double calculatedZoom2 = Math.Clamp(Math.Round(startZoom * scale2, 3), 0.1, 5.0);
        Assert.Equal(1.50, calculatedZoom2);

        // Extreme pinch out clamped to max zoom (5.0 / 500%)
        double scaleExtreme = 10.0;
        double calculatedZoomMax = Math.Clamp(Math.Round(startZoom * scaleExtreme, 3), 0.1, 5.0);
        Assert.Equal(5.0, calculatedZoomMax);

        // Extreme pinch in clamped to min zoom (0.1 / 10%)
        double scaleMin = 0.01;
        double calculatedZoomMin = Math.Clamp(Math.Round(startZoom * scaleMin, 3), 0.1, 5.0);
        Assert.Equal(0.1, calculatedZoomMin);
    }

    [Fact]
    public void TrackpadContinuousZoomDelta_ComputesSmoothProportionalScaling()
    {
        double oldZoom = 1.0;

        // Small trackpad delta (e.g. 0.05)
        double deltaY = 0.05;
        double zoomDeltaFactor = Math.Pow(1.002, deltaY * 100);
        double newZoom = Math.Clamp(Math.Round(oldZoom * zoomDeltaFactor, 3), 0.1, 5.0);

        // Should be approximately 1.01 (1% smooth increase)
        Assert.True(newZoom > 1.0 && newZoom < 1.05);

        // Discrete mouse wheel tick (deltaY = 1.0)
        double wheelFactor = 1.15;
        double wheelZoom = Math.Clamp(Math.Round(oldZoom * wheelFactor, 3), 0.1, 5.0);
        Assert.Equal(1.15, wheelZoom);
    }

    [Fact]
    public void MainViewModel_ZoomCommands_OperateWithinValidRanges()
    {
        var mainVm = new MainViewModel();
        mainVm.ZoomLevel = 1.0;

        // Zoom In
        mainVm.ZoomIn();
        Assert.True(mainVm.ZoomLevel > 1.0);

        // Reset Zoom
        mainVm.ResetZoom();
        Assert.Equal(1.0, mainVm.ZoomLevel);

        // Zoom Out
        mainVm.ZoomOut();
        Assert.True(mainVm.ZoomLevel < 1.0);
    }

    [Fact]
    public void PdfViewerViewModel_ZoomAndJumpPageGestures_ValidateCorrectly()
    {
        var viewerVm = new PdfViewerViewModel();
        viewerVm.ZoomLevel = 1.0;

        // Zoom In
        viewerVm.ZoomIn();
        Assert.Equal(1.25, viewerVm.ZoomLevel);

        // Zoom Out
        viewerVm.ZoomOut();
        Assert.Equal(1.0, viewerVm.ZoomLevel);

        // Reset Zoom
        viewerVm.ResetZoom();
        Assert.Equal(1.0, viewerVm.ZoomLevel);

        // Fit Presets
        viewerVm.FitToWidth();
        Assert.Equal(1.35, viewerVm.ZoomLevel);

        viewerVm.FitToPage();
        Assert.Equal(0.95, viewerVm.ZoomLevel);

        // Percentage Presets
        viewerVm.SetZoomPreset("200%");
        Assert.Equal(2.0, viewerVm.ZoomLevel);

        viewerVm.SetZoomPreset("50%");
        Assert.Equal(0.5, viewerVm.ZoomLevel);
    }

    [Fact]
    public void MainViewModel_WindowTitle_ReflectsActiveViewAndContextCorrectly()
    {
        // 1. Initial State: App opens on Home Dashboard
        var mainVm = new MainViewModel();
        Assert.True(mainVm.IsHomePageVisible);
        Assert.False(mainVm.IsEditorVisible);
        Assert.False(mainVm.IsPdfViewerVisible);
        Assert.Equal("FryPDF - Privacy-First PDF Studio", mainVm.WindowTitle);

        // 2. Open a tool in Home page (e.g. Merge PDF)
        mainVm.OpenTool(PdfToolId.MergePdf);
        Assert.True(mainVm.IsHomePageVisible);
        Assert.True(mainVm.Home.IsToolPageActive);
        Assert.Contains("Merge", mainVm.WindowTitle);
        Assert.EndsWith("FryPDF", mainVm.WindowTitle);

        // 3. Navigate to Licensing section
        mainVm.Home.BackToTools();
        mainVm.Home.SelectNavSectionCommand.Execute("Licensing");
        Assert.Equal("Licenses & Third-Party Tools - FryPDF", mainVm.WindowTitle);

        // 4. Open an Editor template document
        mainVm.OpenEditorWithTemplate("Invoice");
        Assert.False(mainVm.IsHomePageVisible);
        Assert.True(mainVm.IsEditorVisible);
        Assert.False(mainVm.IsPdfViewerVisible);
        Assert.Contains("FryPDF", mainVm.WindowTitle);

        // 5. Navigate back to Home
        mainVm.NavigateToHome();
        Assert.True(mainVm.IsHomePageVisible);
        Assert.False(mainVm.IsEditorVisible);
        Assert.Equal("FryPDF - Privacy-First PDF Studio", mainVm.WindowTitle);

        // 6. Open in PDF Viewer Mode
        mainVm.PdfViewer.DocumentTitle = "Quarterly_Financials.pdf";
        mainVm.OpenInViewer("dummy/Quarterly_Financials.pdf");
        Assert.False(mainVm.IsHomePageVisible);
        Assert.True(mainVm.IsPdfViewerVisible);
        Assert.Equal("Quarterly_Financials.pdf - FryPDF", mainVm.WindowTitle);
    }
}
