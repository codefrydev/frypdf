using System;
using System.IO;
using System.Linq;
using CommunityToolkit.Mvvm.Input;
using CommunityToolkit.Mvvm.Messaging;
using PdfEditorApp.Core.Models;
using PdfEditorApp.Messages;
using PdfEditorApp.Models;
using PdfEditorApp.Services;

namespace PdfEditorApp.ViewModels;

public partial class PdfViewerViewModel
{
    // --- Navigation & Page Selection ---

    public void RequestScrollToPage(int pageNumber)
    {
        if (pageNumber >= 1 && pageNumber <= TotalPagesCount)
        {
            ScrollToPageRequested?.Invoke(pageNumber);
        }
    }


    [RelayCommand]
    public void SelectPage(PdfViewerPageItem? page)
    {
        if (page == null) return;
        SelectedPage = page;
        CurrentPageNumber = page.PageNumber;
        RequestScrollToPage(page.PageNumber);
    }

    [RelayCommand]
    public void SelectSpread(PdfViewerPageSpreadItem? spread)
    {
        if (spread == null) return;
        SelectedSpread = spread;
        if (spread.LeftPage != null)
        {
            CurrentPageNumber = spread.LeftPage.PageNumber;
        }
        else if (spread.RightPage != null)
        {
            CurrentPageNumber = spread.RightPage.PageNumber;
        }
        RequestScrollToPage(CurrentPageNumber);
    }

    [RelayCommand]
    public void JumpToBookmark(PdfViewerBookmarkItem? bookmark)
    {
        if (bookmark == null) return;
        int pageIndex = bookmark.PageNumber - 1;
        if (pageIndex >= 0 && pageIndex < Pages.Count)
        {
            SelectedPage = Pages[pageIndex];
            CurrentPageNumber = bookmark.PageNumber;
            RequestScrollToPage(bookmark.PageNumber);
        }
    }

    [RelayCommand]
    public void JumpToAnnotation(PdfViewerAnnotationItem? ann)
    {
        if (ann == null) return;
        int pageIndex = ann.PageNumber - 1;
        if (pageIndex >= 0 && pageIndex < Pages.Count)
        {
            SelectedPage = Pages[pageIndex];
            CurrentPageNumber = ann.PageNumber;
            RequestScrollToPage(ann.PageNumber);
        }
    }

    [RelayCommand]
    public void CommitJumpPage()
    {
        if (int.TryParse(JumpPageText, out int target) && target >= 1 && target <= TotalPagesCount)
        {
            CurrentPageNumber = target;
            RequestScrollToPage(target);
        }
        else
        {
            JumpPageText = CurrentPageNumber.ToString();
        }
    }

    [RelayCommand]
    public void NextPage()
    {
        if (IsTwoPageSpreadMode)
        {
            if (CurrentPageNumber + 2 <= TotalPagesCount)
            {
                CurrentPageNumber += 2;
            }
            else if (CurrentPageNumber < TotalPagesCount)
            {
                CurrentPageNumber = TotalPagesCount;
            }
        }
        else
        {
            if (CurrentPageNumber < TotalPagesCount)
            {
                CurrentPageNumber++;
            }
        }
        RequestScrollToPage(CurrentPageNumber);
    }

    [RelayCommand]
    public void PreviousPage()
    {
        if (IsTwoPageSpreadMode)
        {
            if (CurrentPageNumber - 2 >= 1)
            {
                CurrentPageNumber -= 2;
            }
            else
            {
                CurrentPageNumber = 1;
            }
        }
        else
        {
            if (CurrentPageNumber > 1)
            {
                CurrentPageNumber--;
            }
        }
        RequestScrollToPage(CurrentPageNumber);
    }

    [RelayCommand]
    public void FirstPage()
    {
        if (TotalPagesCount > 0)
        {
            CurrentPageNumber = 1;
            RequestScrollToPage(1);
        }
    }

    [RelayCommand]
    public void LastPage()
    {
        if (TotalPagesCount > 0)
        {
            CurrentPageNumber = TotalPagesCount;
            RequestScrollToPage(TotalPagesCount);
        }
    }


    // --- Layout & View Modes ---

    [RelayCommand]
    public void SetContinuousScrollLayout()
    {
        SetLayoutMode("ContinuousScroll");
    }

    [RelayCommand]
    public void SetSinglePageLayout()
    {
        SetLayoutMode("SinglePage");
    }

    [RelayCommand]
    public void SetTwoPageSpreadLayout()
    {
        SetLayoutMode("TwoPageSpread");
    }

    [RelayCommand]
    public void SetLayoutMode(string? modeStr)
    {
        if (Enum.TryParse<PdfViewLayoutMode>(modeStr, true, out var mode))
        {
            SelectedLayoutMode = mode;
            ShowToast(mode switch
            {
                PdfViewLayoutMode.ContinuousScroll => "Continuous Scroll Layout",
                PdfViewLayoutMode.SinglePage => "Single Page Fit Layout",
                PdfViewLayoutMode.TwoPageSpread => "Two-Page Book Spread Layout",
                _ => "Layout Changed"
            });
        }
    }

    // --- Reading Themes ---

    [RelayCommand]
    public void SetDefaultReadingTheme()
    {
        SetReadingTheme("Default");
    }

    [RelayCommand]
    public void SetSepiaReadingTheme()
    {
        SetReadingTheme("Sepia");
    }

    [RelayCommand]
    public void SetDarkReadingTheme()
    {
        SetReadingTheme("Dark");
    }

    [RelayCommand]
    public void SetHighContrastReadingTheme()
    {
        SetReadingTheme("HighContrast");
    }

    [RelayCommand]
    public void SetReadingTheme(string? themeStr)
    {
        if (Enum.TryParse<PdfReaderTheme>(themeStr, true, out var theme))
        {
            ReadingTheme = theme;
            ShowToast(theme switch
            {
                PdfReaderTheme.Sepia => "Warm Sepia Book Reading Mode",
                PdfReaderTheme.Dark => "Dark Night Reading Mode",
                PdfReaderTheme.HighContrast => "High Contrast Reading Mode",
                _ => "Standard Daylight Reading Mode"
            });
        }
    }

    // --- Zoom Controls ---

    [RelayCommand]
    public void ZoomIn()
    {
        ZoomMode = PdfViewerZoomMode.Custom;
        ZoomLevel = Math.Min(5.0, Math.Round(ZoomLevel + 0.25, 2));
    }

    [RelayCommand]
    public void ZoomOut()
    {
        ZoomMode = PdfViewerZoomMode.Custom;
        ZoomLevel = Math.Max(0.25, Math.Round(ZoomLevel - 0.25, 2));
    }

    [RelayCommand]
    public void ResetZoom()
    {
        ZoomMode = PdfViewerZoomMode.Custom;
        ZoomLevel = 1.0;
    }

    [RelayCommand]
    public void FitToWidth()
    {
        if (ViewportSizeProvider != null)
        {
            var dims = ViewportSizeProvider();
            if (dims.ViewportWidth > 100)
            {
                FitToWidthDynamic(dims.ViewportWidth, dims.HorizontalPadding);
                return;
            }
        }

        _isApplyingFitZoom = true;
        try
        {
            ZoomMode = PdfViewerZoomMode.FitWidth;
            ZoomLevel = 1.35;
        }
        finally
        {
            _isApplyingFitZoom = false;
        }
    }

    public void FitToWidthDynamic(double viewportWidth, double horizontalPadding = 64.0)
    {
        if (viewportWidth <= 100) return;

        var page = SelectedPage ?? Pages.FirstOrDefault(p => p.PageNumber == CurrentPageNumber) ?? Pages.FirstOrDefault();
        if (page == null)
        {
            _isApplyingFitZoom = true;
            try
            {
                ZoomMode = PdfViewerZoomMode.FitWidth;
                ZoomLevel = 1.35;
            }
            finally
            {
                _isApplyingFitZoom = false;
            }
            return;
        }

        double availableWidth = Math.Max(100.0, viewportWidth - horizontalPadding - 8.0);
        double targetWidth = (page.RotationAngle % 180 == 0) ? page.WidthPoints : page.HeightPoints;

        if (IsTwoPageSpreadMode && SelectedSpread != null)
        {
            if (SelectedSpread.LeftPage != null && SelectedSpread.RightPage != null)
            {
                double leftW = (SelectedSpread.LeftPage.RotationAngle % 180 == 0) ? SelectedSpread.LeftPage.WidthPoints : SelectedSpread.LeftPage.HeightPoints;
                double rightW = (SelectedSpread.RightPage.RotationAngle % 180 == 0) ? SelectedSpread.RightPage.WidthPoints : SelectedSpread.RightPage.HeightPoints;
                targetWidth = leftW + rightW;
                availableWidth = Math.Max(100.0, availableWidth - 16.0); // 16px gap between pages
            }
            else
            {
                var single = SelectedSpread.LeftPage ?? SelectedSpread.RightPage ?? page;
                targetWidth = (single.RotationAngle % 180 == 0) ? single.WidthPoints : single.HeightPoints;
            }
        }

        if (targetWidth > 10.0)
        {
            double calculatedZoom = Math.Clamp(Math.Round(availableWidth / targetWidth, 2), 0.25, 5.0);
            if (calculatedZoom * targetWidth > availableWidth && calculatedZoom > 0.25)
            {
                calculatedZoom = Math.Max(0.25, Math.Round(calculatedZoom - 0.01, 2));
            }

            _isApplyingFitZoom = true;
            try
            {
                ZoomMode = PdfViewerZoomMode.FitWidth;
                ZoomLevel = calculatedZoom;
            }
            finally
            {
                _isApplyingFitZoom = false;
            }

            StatusMessage = $"Fit to Width ({(int)Math.Round(ZoomLevel * 100)}%)";
            ShowToast($"Fit to Width ({(int)Math.Round(ZoomLevel * 100)}%)");
        }
    }

    [RelayCommand]
    public void FitToPage()
    {
        if (ViewportSizeProvider != null)
        {
            var dims = ViewportSizeProvider();
            if (dims.ViewportWidth > 100 && dims.ViewportHeight > 100)
            {
                FitToPageDynamic(dims.ViewportWidth, dims.ViewportHeight, dims.HorizontalPadding, dims.VerticalPadding);
                return;
            }
        }

        _isApplyingFitZoom = true;
        try
        {
            ZoomMode = PdfViewerZoomMode.FitPage;
            ZoomLevel = 0.95;
        }
        finally
        {
            _isApplyingFitZoom = false;
        }
    }

    public void FitToPageDynamic(double viewportWidth, double viewportHeight, double horizontalPadding = 64.0, double verticalPadding = 64.0)
    {
        if (viewportWidth <= 100 || viewportHeight <= 100) return;

        var page = SelectedPage ?? Pages.FirstOrDefault(p => p.PageNumber == CurrentPageNumber) ?? Pages.FirstOrDefault();
        if (page == null)
        {
            _isApplyingFitZoom = true;
            try
            {
                ZoomMode = PdfViewerZoomMode.FitPage;
                ZoomLevel = 0.95;
            }
            finally
            {
                _isApplyingFitZoom = false;
            }
            return;
        }

        double availableWidth = Math.Max(100.0, viewportWidth - horizontalPadding - 8.0);
        // Reserve 36px for page footnote indicator and card vertical spacing
        double availableHeight = Math.Max(100.0, viewportHeight - verticalPadding - 36.0);

        double targetWidth = (page.RotationAngle % 180 == 0) ? page.WidthPoints : page.HeightPoints;
        double targetHeight = (page.RotationAngle % 180 == 0) ? page.HeightPoints : page.WidthPoints;

        if (IsTwoPageSpreadMode && SelectedSpread != null)
        {
            if (SelectedSpread.LeftPage != null && SelectedSpread.RightPage != null)
            {
                double leftW = (SelectedSpread.LeftPage.RotationAngle % 180 == 0) ? SelectedSpread.LeftPage.WidthPoints : SelectedSpread.LeftPage.HeightPoints;
                double leftH = (SelectedSpread.LeftPage.RotationAngle % 180 == 0) ? SelectedSpread.LeftPage.HeightPoints : SelectedSpread.LeftPage.WidthPoints;
                double rightW = (SelectedSpread.RightPage.RotationAngle % 180 == 0) ? SelectedSpread.RightPage.WidthPoints : SelectedSpread.RightPage.HeightPoints;
                double rightH = (SelectedSpread.RightPage.RotationAngle % 180 == 0) ? SelectedSpread.RightPage.HeightPoints : SelectedSpread.RightPage.WidthPoints;
                targetWidth = leftW + rightW;
                targetHeight = Math.Max(leftH, rightH);
                availableWidth = Math.Max(100.0, availableWidth - 16.0);
            }
            else
            {
                var single = SelectedSpread.LeftPage ?? SelectedSpread.RightPage ?? page;
                targetWidth = (single.RotationAngle % 180 == 0) ? single.WidthPoints : single.HeightPoints;
                targetHeight = (single.RotationAngle % 180 == 0) ? single.HeightPoints : single.WidthPoints;
            }
        }

        if (targetWidth > 10.0 && targetHeight > 10.0)
        {
            double scaleX = availableWidth / targetWidth;
            double scaleY = availableHeight / targetHeight;
            double calculatedZoom = Math.Clamp(Math.Round(Math.Min(scaleX, scaleY), 2), 0.25, 5.0);
            if ((calculatedZoom * targetWidth > availableWidth || calculatedZoom * targetHeight > availableHeight) && calculatedZoom > 0.25)
            {
                calculatedZoom = Math.Max(0.25, Math.Round(calculatedZoom - 0.01, 2));
            }

            _isApplyingFitZoom = true;
            try
            {
                ZoomMode = PdfViewerZoomMode.FitPage;
                ZoomLevel = calculatedZoom;
            }
            finally
            {
                _isApplyingFitZoom = false;
            }

            StatusMessage = $"Fit to Page ({(int)Math.Round(ZoomLevel * 100)}%)";
            ShowToast($"Fit to Page ({(int)Math.Round(ZoomLevel * 100)}%)");
        }
    }

    [RelayCommand]
    public void SetZoomPreset(string? preset)
    {
        if (string.IsNullOrWhiteSpace(preset)) return;
        string clean = preset.Replace("%", "").Trim();
        if (double.TryParse(clean, out double val))
        {
            ZoomMode = PdfViewerZoomMode.Custom;
            ZoomLevel = Math.Clamp(val / 100.0, 0.25, 5.0);
        }
    }

    // --- Page Operations & Visual Rotation ---

    [RelayCommand]
    public void RotateClockwise()
    {
        var page = SelectedPage ?? Pages.FirstOrDefault(p => p.PageNumber == CurrentPageNumber) ?? Pages.FirstOrDefault();
        if (page != null)
        {
            SelectedPage = page;
            page.RotationAngle = (page.RotationAngle + 90) % 360;
            StatusMessage = $"Page {page.PageNumber} rotated 90° CW ({page.RotationAngle}°).";
            ShowToast($"Page {page.PageNumber} rotated 90° CW ({page.RotationAngle}°)");

            if (IsFitToPageActive)
            {
                FitToPage();
            }
            else if (IsFitToWidthActive)
            {
                FitToWidth();
            }
        }
    }

    [RelayCommand]
    public void RotateCounterClockwise()
    {
        var page = SelectedPage ?? Pages.FirstOrDefault(p => p.PageNumber == CurrentPageNumber) ?? Pages.FirstOrDefault();
        if (page != null)
        {
            SelectedPage = page;
            page.RotationAngle = (page.RotationAngle + 270) % 360;
            StatusMessage = $"Page {page.PageNumber} rotated 90° CCW ({page.RotationAngle}°).";
            ShowToast($"Page {page.PageNumber} rotated 90° CCW ({page.RotationAngle}°)");

            if (IsFitToPageActive)
            {
                FitToPage();
            }
            else if (IsFitToWidthActive)
            {
                FitToWidth();
            }
        }
    }

    [RelayCommand]
    public void RotateAllPagesClockwise()
    {
        foreach (var p in Pages)
        {
            p.RotationAngle = (p.RotationAngle + 90) % 360;
        }
        StatusMessage = "Rotated all pages 90° clockwise.";
        ShowToast("Rotated all pages 90° CW");

        if (IsFitToPageActive)
        {
            FitToPage();
        }
        else if (IsFitToWidthActive)
        {
            FitToWidth();
        }
    }


    // --- Sidebar & Layout Commands ---

    [RelayCommand]
    public void SelectSidebarTab(string tabName)
    {
        if (Enum.TryParse<PdfViewerSidebarTab>(tabName, true, out var tab))
        {
            SelectedSidebarTab = tab;
            IsSidebarOpen = true;
        }
    }

    [RelayCommand]
    public void ToggleSidebar()
    {
        IsSidebarOpen = !IsSidebarOpen;
    }

    [RelayCommand]
    public void ToggleFullscreen()
    {
        IsFullscreen = !IsFullscreen;
        string msg = IsFullscreen ? "Entered Presentation Reading Mode (Esc to exit)" : "Exited Fullscreen Mode";
        ShowToast(msg);
    }

    // --- Bridge to Studio & Tools ---

    [RelayCommand]
    public void EditInStudio()
    {
        string? targetPath = null;
        if (!string.IsNullOrEmpty(CurrentFilePath) && File.Exists(CurrentFilePath))
        {
            targetPath = CurrentFilePath;
        }
        else if (_currentPdfBytes != null)
        {
            string tempPath = Path.Combine(Path.GetTempPath(), DocumentTitle);
            File.WriteAllBytes(tempPath, _currentPdfBytes);
            targetPath = tempPath;
        }

        if (targetPath != null)
        {
            WeakReferenceMessenger.Default.Send(new OpenInEditorMessage(targetPath));
        }
    }

    [RelayCommand]
    public void RunToolOnDocument(string? toolIdStr)
    {
        if (!string.IsNullOrEmpty(CurrentFilePath) && File.Exists(CurrentFilePath) && Enum.TryParse<PdfToolId>(toolIdStr, true, out var toolId))
        {
            WeakReferenceMessenger.Default.Send(new RunToolMessage(toolId, CurrentFilePath));
        }
    }

    [RelayCommand]
    public void BackToHome()
    {
        WeakReferenceMessenger.Default.Send(new NavigateToHomeMessage());
    }
}

