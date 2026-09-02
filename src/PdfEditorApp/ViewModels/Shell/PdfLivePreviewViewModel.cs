using System;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Avalonia.Threading;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using PdfEditorApp.Models;
using PdfEditorApp.Services.Tools;

namespace PdfEditorApp.ViewModels.Shell;

/// <summary>
/// Shared live-preview state (page navigation, zoom, rendered pages) for tool screens,
/// built on <see cref="PdfPageRenderer"/>. One instance is owned by
/// <see cref="PdfEditorApp.ViewModels.Tools.PdfToolViewModelBase"/> so every tool gets
/// the same reader-style preview instead of reimplementing rendering/zoom/page-nav
/// per tool (as RedactPdfToolViewModel previously did independently).
/// </summary>
public partial class PdfLivePreviewViewModel : ObservableObject
{
    private const float ThumbnailScale = 0.35f;
    private const double MinZoom = 0.5;
    private const double MaxZoom = 3.0;
    private const double ZoomStep = 0.25;

    private string? _loadedFilePath;
    private CancellationTokenSource? _loadCts;
    private CancellationTokenSource? _zoomRenderCts;

    public ObservableCollection<PdfToolPreviewPage> Pages { get; } = new();

    [ObservableProperty]
    private int _currentPageNumber = 1;

    [ObservableProperty]
    private int _totalPages;

    [ObservableProperty]
    private double _zoomLevel = 1.0;

    [ObservableProperty]
    private bool _isLoading;

    [ObservableProperty]
    private PdfToolPreviewPage? _selectedPage;

    public bool HasDocument => TotalPages > 0;
    public string ZoomPercentText => $"{ZoomLevel * 100:F0}%";
    public string PageIndicatorText => TotalPages > 0 ? $"Page {CurrentPageNumber} of {TotalPages}" : "No document";
    public bool CanGoToPreviousPage => CurrentPageNumber > 1;
    public bool CanGoToNextPage => CurrentPageNumber < TotalPages;
    public bool CanZoomIn => ZoomLevel < MaxZoom - 0.001;
    public bool CanZoomOut => ZoomLevel > MinZoom + 0.001;

    partial void OnCurrentPageNumberChanged(int value)
    {
        OnPropertyChanged(nameof(PageIndicatorText));
        OnPropertyChanged(nameof(CanGoToPreviousPage));
        OnPropertyChanged(nameof(CanGoToNextPage));

        foreach (var page in Pages)
        {
            page.IsSelected = page.PageNumber == value;
        }
        SelectedPage = Pages.FirstOrDefault(p => p.PageNumber == value);

        EnsurePageRendered(value);
    }

    partial void OnTotalPagesChanged(int value)
    {
        OnPropertyChanged(nameof(HasDocument));
        OnPropertyChanged(nameof(PageIndicatorText));
        OnPropertyChanged(nameof(CanGoToNextPage));
    }

    partial void OnZoomLevelChanged(double value)
    {
        OnPropertyChanged(nameof(ZoomPercentText));
        OnPropertyChanged(nameof(CanZoomIn));
        OnPropertyChanged(nameof(CanZoomOut));

        // Debounced so rapid zooming doesn't trigger a render per tick (matches the
        // PDF Reader's own zoom-debounce behavior).
        _zoomRenderCts?.Cancel();
        var cts = new CancellationTokenSource();
        _zoomRenderCts = cts;
        _ = RerenderCurrentPageAfterZoomDebounceAsync(cts.Token);
    }

    private async Task RerenderCurrentPageAfterZoomDebounceAsync(CancellationToken ct)
    {
        try { await Task.Delay(150, ct); }
        catch (OperationCanceledException) { return; }
        if (ct.IsCancellationRequested) return;
        EnsurePageRendered(CurrentPageNumber, forceRerender: true);
    }

    [RelayCommand]
    private void ZoomIn() => ZoomLevel = Math.Min(MaxZoom, Math.Round(ZoomLevel + ZoomStep, 2));

    [RelayCommand]
    private void ZoomOut() => ZoomLevel = Math.Max(MinZoom, Math.Round(ZoomLevel - ZoomStep, 2));

    [RelayCommand]
    private void ResetZoom() => ZoomLevel = 1.0;

    [RelayCommand]
    private void NextPage()
    {
        if (CanGoToNextPage) CurrentPageNumber++;
    }

    [RelayCommand]
    private void PreviousPage()
    {
        if (CanGoToPreviousPage) CurrentPageNumber--;
    }

    [RelayCommand]
    private void SelectPage(PdfToolPreviewPage? page)
    {
        if (page != null) CurrentPageNumber = page.PageNumber;
    }

    /// <summary>
    /// Loads (or clears, if <paramref name="filePath"/> is null/empty) the document the
    /// preview shows. Safe to call repeatedly as the tool's selected file changes.
    /// </summary>
    public async Task LoadDocumentAsync(string? filePath)
    {
        _loadCts?.Cancel();
        var cts = new CancellationTokenSource();
        _loadCts = cts;

        Pages.Clear();
        CurrentPageNumber = 1;
        TotalPages = 0;
        ZoomLevel = 1.0;
        SelectedPage = null;
        _loadedFilePath = filePath;

        if (string.IsNullOrEmpty(filePath) || !File.Exists(filePath))
        {
            return;
        }

        IsLoading = true;
        try
        {
            int total = await Task.Run(() => PdfFileHelper.InspectPageCountSafely(filePath), cts.Token);
            if (cts.Token.IsCancellationRequested || filePath != _loadedFilePath) return;

            TotalPages = total;
            for (int i = 1; i <= total; i++)
            {
                Pages.Add(new PdfToolPreviewPage { PageNumber = i, IsSelected = i == 1 });
            }
            SelectedPage = Pages.FirstOrDefault();

            if (total > 0)
            {
                await RenderPageCoreAsync(1, BaseScaleForZoom(ZoomLevel), cts.Token);
                _ = LoadThumbnailsInBackgroundAsync(filePath, cts.Token);
            }
        }
        finally
        {
            IsLoading = false;
        }
    }

    private async Task LoadThumbnailsInBackgroundAsync(string filePath, CancellationToken ct)
    {
        foreach (var page in Pages.ToList())
        {
            if (ct.IsCancellationRequested || filePath != _loadedFilePath) return;

            var (bitmap, widthPoints, heightPoints) = await Task.Run(
                () => PdfPageRenderer.RenderPageAtScale(filePath, page.PageNumber, ThumbnailScale), ct);

            if (ct.IsCancellationRequested || filePath != _loadedFilePath) return;

            if (widthPoints > 0) page.WidthPoints = widthPoints;
            if (heightPoints > 0) page.HeightPoints = heightPoints;
            if (bitmap != null) page.ThumbnailBitmap = bitmap;
        }
    }

    private float BaseScaleForZoom(double zoomLevel) => (float)Math.Clamp(1.75 * zoomLevel, 1.25, 4.0);

    /// <summary>Re-renders a page at a zoom-appropriate resolution if it hasn't been rendered yet, or the zoom level has moved enough to need a sharper bitmap.</summary>
    private void EnsurePageRendered(int pageNumber, bool forceRerender = false)
    {
        var page = Pages.FirstOrDefault(p => p.PageNumber == pageNumber);
        if (page == null || string.IsNullOrEmpty(_loadedFilePath)) return;

        float targetScale = BaseScaleForZoom(ZoomLevel);
        if (!forceRerender && page.Bitmap != null && Math.Abs(page.RenderedScale - targetScale) <= 0.4f) return;

        string filePath = _loadedFilePath;
        Task.Run(() =>
        {
            var (bitmap, widthPoints, heightPoints) = PdfPageRenderer.RenderPageAtScale(filePath, pageNumber, targetScale);
            if (bitmap == null || filePath != _loadedFilePath) return;

            Dispatcher.UIThread.Post(() =>
            {
                if (filePath != _loadedFilePath) return;
                page.Bitmap = bitmap;
                page.RenderedScale = targetScale;
                if (widthPoints > 0) page.WidthPoints = widthPoints;
                if (heightPoints > 0) page.HeightPoints = heightPoints;
            });
        });
    }

    private async Task RenderPageCoreAsync(int pageNumber, float scale, CancellationToken ct)
    {
        var page = Pages.FirstOrDefault(p => p.PageNumber == pageNumber);
        if (page == null) return;

        string? filePath = _loadedFilePath;
        if (string.IsNullOrEmpty(filePath)) return;

        var (bitmap, widthPoints, heightPoints) = await Task.Run(
            () => PdfPageRenderer.RenderPageAtScale(filePath, pageNumber, scale), ct);

        if (ct.IsCancellationRequested || filePath != _loadedFilePath) return;

        page.Bitmap = bitmap;
        page.RenderedScale = scale;
        if (widthPoints > 0) page.WidthPoints = widthPoints;
        if (heightPoints > 0) page.HeightPoints = heightPoints;
    }
}
