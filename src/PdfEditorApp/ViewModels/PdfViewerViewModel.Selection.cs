using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Avalonia;
using Avalonia.Input.Platform;
using Avalonia.Media.Imaging;
using CommunityToolkit.Mvvm.Input;
using PdfEditorApp.Services;
using PdfEditorApp.Services.Ocr;
using SkiaSharp;

namespace PdfEditorApp.ViewModels;

public partial class PdfViewerViewModel
{
    // --- Interactive Selection & Clipboard Operations ---

    [RelayCommand]
    public async Task CopySelectedTextAsync()
    {
        if (string.IsNullOrWhiteSpace(ActiveSelectedText))
        {
            if (SelectedPage != null && !string.IsNullOrWhiteSpace(SelectedPage.ExtractedText))
            {
                await CopyPageTextAsync();
            }
            return;
        }

        if (Avalonia.Application.Current?.ApplicationLifetime is Avalonia.Controls.ApplicationLifetimes.IClassicDesktopStyleApplicationLifetime desktop && desktop.MainWindow != null)
        {
            var topLevel = Avalonia.Controls.TopLevel.GetTopLevel(desktop.MainWindow);
            if (topLevel?.Clipboard != null)
            {
                await topLevel.Clipboard.SetTextAsync(ActiveSelectedText);
                string snippet = ActiveSelectedText.Length > 40 ? ActiveSelectedText.Substring(0, 40) + "..." : ActiveSelectedText;
                ShowToastRequested?.Invoke($"Copied: \"{snippet}\"");
            }
        }
    }

    [RelayCommand]
    public async Task CopySelectedCitationAsync()
    {
        if (string.IsNullOrWhiteSpace(ActiveSelectedText)) return;

        string citation = $"\"{ActiveSelectedText}\"\n— Page {ActiveSelectedPageNumber}, {DocumentTitle}";
        if (Avalonia.Application.Current?.ApplicationLifetime is Avalonia.Controls.ApplicationLifetimes.IClassicDesktopStyleApplicationLifetime desktop && desktop.MainWindow != null)
        {
            var topLevel = Avalonia.Controls.TopLevel.GetTopLevel(desktop.MainWindow);
            if (topLevel?.Clipboard != null)
            {
                await topLevel.Clipboard.SetTextAsync(citation);
                ShowToastRequested?.Invoke("Copied citation with page reference");
            }
        }
    }

    [RelayCommand]
    public void SearchSelectedText()
    {
        if (string.IsNullOrWhiteSpace(ActiveSelectedText)) return;
        SearchQuery = ActiveSelectedText.Trim();
        IsSearchBarVisible = true;
        SelectedSidebarTab = PdfViewerSidebarTab.Search;
        IsSidebarOpen = true;
        PerformSearch();
    }

    [RelayCommand]
    public void SearchWebSelectedText()
    {
        if (string.IsNullOrWhiteSpace(ActiveSelectedText)) return;
        try
        {
            string query = Uri.EscapeDataString(ActiveSelectedText.Trim());
            string url = $"https://www.google.com/search?q={query}";
            var psi = new System.Diagnostics.ProcessStartInfo
            {
                FileName = url,
                UseShellExecute = true
            };
            System.Diagnostics.Process.Start(psi);
        }
        catch { }
    }

    [RelayCommand]
    public void SelectAllPageText()
    {
        var page = SelectedPage ?? Pages.FirstOrDefault(p => p.PageNumber == CurrentPageNumber);
        if (page == null) return;
        page.SelectAll();
        ActiveSelectedText = page.SelectedText;
        ActiveSelectedPageNumber = page.PageNumber;
        HasTextSelection = !string.IsNullOrEmpty(ActiveSelectedText);
        ShowToastRequested?.Invoke($"Selected all text on Page {page.PageNumber}");
    }

    [RelayCommand]
    public void ClearSelection()
    {
        ActiveSelectedText = string.Empty;
        HasTextSelection = false;
        LastSelectedAreaRect = null;
        foreach (var p in Pages)
        {
            p.ClearSelection();
        }
    }

    [RelayCommand]
    public void SetTextSelectionMode()
    {
        SelectionMode = PdfViewerSelectionMode.Text;
        ShowToastRequested?.Invoke("Text Selection Mode (I-Beam)");
    }

    [RelayCommand]
    public void SetAreaSelectionMode()
    {
        SelectionMode = PdfViewerSelectionMode.Area;
        ShowToastRequested?.Invoke("Area / Marquee Selection Mode (Box / Snapshot)");
    }

    [RelayCommand]
    public void ToggleSelectionMode()
    {
        if (SelectionMode == PdfViewerSelectionMode.Text)
            SetAreaSelectionMode();
        else
            SetTextSelectionMode();
    }

    [RelayCommand]
    public void DismissScannedBanner()
    {
        IsScannedBannerDismissed = true;
    }

    [RelayCommand]
    public void CancelOcr()
    {
        _ocrCts?.Cancel();
        IsOcrRunning = false;
        OcrStatusText = "Cancelled";
        ShowToastRequested?.Invoke("OCR cancelled.");
    }

    [RelayCommand]
    public async Task RecognizeActivePageOcrAsync()
    {
        var page = SelectedPage ?? Pages.FirstOrDefault(p => p.PageNumber == CurrentPageNumber) ?? Pages.FirstOrDefault();
        if (page == null || IsOcrRunning) return;

        _ocrCts?.Cancel();
        _ocrCts = new CancellationTokenSource();
        var ct = _ocrCts.Token;

        IsOcrRunning = true;
        OcrProgress = 15;
        OcrStatusText = $"Recognizing text on Page {page.PageNumber}...";

        try
        {
            await Task.Run(() =>
            {
                double width = page.WidthPoints > 0 ? page.WidthPoints : 612;
                double height = page.HeightPoints > 0 ? page.HeightPoints : 792;
                var (ocrText, ocrWords, ocrLines) = ExtractOcrPageTextGeometry(page.PageNumber, width, height);

                if (ocrWords.Count > 0)
                {
                    page.Words = ocrWords;
                    page.TextLines = ocrLines;
                    page.ExtractedText = ocrText;
                }
            }, ct);

            OcrProgress = 100;
            IsCurrentPageScanned = page.Words.Count == 0;
            page.NotifySelectionChanged();
            ShowToastRequested?.Invoke($"OCR Complete: {page.Words.Count} words recognized on Page {page.PageNumber}.");
        }
        catch (OperationCanceledException)
        {
            ShowToastRequested?.Invoke("OCR cancelled.");
        }
        catch (Exception ex)
        {
            ShowToastRequested?.Invoke($"OCR failed: {ex.Message}");
        }
        finally
        {
            IsOcrRunning = false;
            OcrStatusText = string.Empty;
        }
    }

    [RelayCommand]
    public async Task RecognizeDocumentOcrAsync()
    {
        if (Pages.Count == 0 || IsOcrRunning) return;

        _ocrCts?.Cancel();
        _ocrCts = new CancellationTokenSource();
        var ct = _ocrCts.Token;

        IsOcrRunning = true;
        int total = Pages.Count;
        int recognizedPages = 0;
        int totalWords = 0;

        try
        {
            for (int i = 0; i < total; i++)
            {
                ct.ThrowIfCancellationRequested();
                var page = Pages[i];
                int pageNum = page.PageNumber;
                OcrProgress = (double)(i + 1) / total * 100.0;
                OcrStatusText = $"Recognizing text... Page {pageNum} of {total} ({(int)OcrProgress}%)";

                if (page.Words.Count == 0)
                {
                    await Task.Run(() =>
                    {
                        double width = page.WidthPoints > 0 ? page.WidthPoints : 612;
                        double height = page.HeightPoints > 0 ? page.HeightPoints : 792;
                        var (ocrText, ocrWords, ocrLines) = ExtractOcrPageTextGeometry(pageNum, width, height);
                        if (ocrWords.Count > 0)
                        {
                            page.Words = ocrWords;
                            page.TextLines = ocrLines;
                            page.ExtractedText = ocrText;
                            Interlocked.Add(ref totalWords, ocrWords.Count);
                            Interlocked.Increment(ref recognizedPages);
                        }
                    }, ct);

                    page.NotifySelectionChanged();
                }
            }

            IsScannedDocument = false;
            if (SelectedPage != null)
            {
                IsCurrentPageScanned = SelectedPage.Words.Count == 0;
            }
            ShowToastRequested?.Invoke($"OCR Complete: {totalWords} words recognized across {recognizedPages} scanned page(s).");
        }
        catch (OperationCanceledException)
        {
            ShowToastRequested?.Invoke("OCR processing cancelled.");
        }
        catch (Exception ex)
        {
            ShowToastRequested?.Invoke($"OCR error: {ex.Message}");
        }
        finally
        {
            IsOcrRunning = false;
            OcrStatusText = string.Empty;
        }
    }

    [RelayCommand]
    public async Task RecognizeSelectedTextOcrAsync()
    {
        var page = SelectedPage ?? Pages.FirstOrDefault(p => p.PageNumber == CurrentPageNumber);
        if (page == null || IsOcrRunning) return;

        // Determine bounding box of selection
        Rect targetRect = default;
        if (LastSelectedAreaRect.HasValue && LastSelectedAreaRect.Value.Width > 5 && LastSelectedAreaRect.Value.Height > 5)
        {
            targetRect = LastSelectedAreaRect.Value;
        }
        else if (page.SelectionRects.Count > 0)
        {
            double minX = page.SelectionRects.Min(r => r.Left);
            double minY = page.SelectionRects.Min(r => r.Top);
            double maxX = page.SelectionRects.Max(r => r.Right);
            double maxY = page.SelectionRects.Max(r => r.Bottom);
            targetRect = new Rect(minX, minY, Math.Max(5, maxX - minX), Math.Max(5, maxY - minY));
        }

        if (targetRect.Width <= 2 || targetRect.Height <= 2)
        {
            await RecognizeActivePageOcrAsync();
            return;
        }

        IsOcrRunning = true;
        OcrStatusText = "Recognizing selection with OCR...";
        OcrProgress = 30;

        try
        {
            string ocrText = await ExtractOcrTextFromRegionAsync(page, targetRect, forceOcr: true);
            if (!string.IsNullOrWhiteSpace(ocrText))
            {
                page.SelectedText = ocrText;
                ActiveSelectedText = ocrText;
                HasTextSelection = true;
                ShowToastRequested?.Invoke("Text recognized with OCR.");
            }
            else
            {
                ShowToastRequested?.Invoke("OCR did not detect text in the selected area.");
            }
        }
        catch (Exception ex)
        {
            ShowToastRequested?.Invoke($"OCR failed: {ex.Message}");
        }
        finally
        {
            IsOcrRunning = false;
            OcrStatusText = string.Empty;
        }
    }

    public async Task<string> ExtractOcrTextFromRegionAsync(PdfViewerPageItem page, Rect region, bool forceOcr = false)
    {
        if (page == null || region.Width <= 2 || region.Height <= 2) return string.Empty;

        // If words already exist and not forced, check if they are valid readable text
        if (!forceOcr && page.Words.Count > 0)
        {
            var intersectingWords = page.Words
                .Where(w => w.Bounds.Intersects(region))
                .OrderBy(w => w.Bounds.Top)
                .ThenBy(w => w.Bounds.Left)
                .ToList();

            if (intersectingWords.Count > 0)
            {
                string combined = string.Join(" ", intersectingWords.Select(w => w.Text));
                if (!IsGarbledText(combined))
                {
                    return combined;
                }
            }
        }

        // Targeted OCR on cropped rectangle
        try
        {
            var ocrProvider = CompositeOcrProvider.Default;
            if (!ocrProvider.IsAvailable) return string.Empty;

            const float ocrScale = 2.0f;
            byte[]? pagePng = RenderPageBytesAtScale(page.PageNumber, ocrScale);
            if (pagePng == null || pagePng.Length == 0) return string.Empty;

            byte[]? croppedBytes = await Task.Run(() =>
            {
                using var ms = new MemoryStream(pagePng);
                using var codec = SKCodec.Create(ms);
                if (codec == null) return null;
                using var origBitmap = SKBitmap.Decode(codec);
                if (origBitmap == null) return null;

                int cropX = Math.Clamp((int)(region.X * ocrScale), 0, Math.Max(0, origBitmap.Width - 1));
                int cropY = Math.Clamp((int)(region.Y * ocrScale), 0, Math.Max(0, origBitmap.Height - 1));
                int cropW = Math.Clamp((int)(region.Width * ocrScale), 1, origBitmap.Width - cropX);
                int cropH = Math.Clamp((int)(region.Height * ocrScale), 1, origBitmap.Height - cropY);

                using var cropped = new SKBitmap();
                origBitmap.ExtractSubset(cropped, new SKRectI(cropX, cropY, cropX + cropW, cropY + cropH));
                using var image = SKImage.FromBitmap(cropped);
                using var data = image.Encode(SKEncodedImageFormat.Png, 100);
                return data.ToArray();
            });

            if (croppedBytes == null || croppedBytes.Length == 0) return string.Empty;

            var ocrResult = await ocrProvider.RecognizeTextAsync(croppedBytes);
            if (ocrResult.Success && !string.IsNullOrWhiteSpace(ocrResult.FullText))
            {
                // Register newly extracted words into page geometry so they become permanently selectable
                int wIdx = page.Words.Count;
                foreach (var ow in ocrResult.Words)
                {
                    double wx = region.X + (ow.NormalizedBounds.X * region.Width);
                    double wy = region.Y + (ow.NormalizedBounds.Y * region.Height);
                    double ww = Math.Max(1, ow.NormalizedBounds.Width * region.Width);
                    double wh = Math.Max(1, ow.NormalizedBounds.Height * region.Height);

                    page.Words.Add(new PdfViewerWordItem
                    {
                        Text = ow.Text,
                        Bounds = new Rect(wx, wy, ww, wh),
                        WordIndex = wIdx++
                    });
                }

                return ocrResult.FullText.Trim();
            }
        }
        catch { }

        return string.Empty;
    }

    [RelayCommand]
    public async Task CopySelectedAreaImageAsync()
    {
        var page = SelectedPage ?? Pages.FirstOrDefault(p => p.PageNumber == ActiveSelectedPageNumber);
        if (page == null || page.SelectionRects.Count == 0) return;

        var region = page.SelectionRects[0];
        if (region.Width <= 2 || region.Height <= 2) return;

        const float snapshotScale = 2.0f;
        byte[]? pagePng = RenderPageBytesAtScale(page.PageNumber, snapshotScale);
        if (pagePng == null || pagePng.Length == 0) return;

        byte[]? croppedBytes = await Task.Run(() =>
        {
            try
            {
                using var ms = new MemoryStream(pagePng);
                using var codec = SKCodec.Create(ms);
                if (codec == null) return null;
                using var origBitmap = SKBitmap.Decode(codec);
                if (origBitmap == null) return null;

                int cropX = Math.Clamp((int)(region.X * snapshotScale), 0, Math.Max(0, origBitmap.Width - 1));
                int cropY = Math.Clamp((int)(region.Y * snapshotScale), 0, Math.Max(0, origBitmap.Height - 1));
                int cropW = Math.Clamp((int)(region.Width * snapshotScale), 1, origBitmap.Width - cropX);
                int cropH = Math.Clamp((int)(region.Height * snapshotScale), 1, origBitmap.Height - cropY);

                using var cropped = new SKBitmap();
                origBitmap.ExtractSubset(cropped, new SKRectI(cropX, cropY, cropX + cropW, cropY + cropH));
                using var image = SKImage.FromBitmap(cropped);
                using var data = image.Encode(SKEncodedImageFormat.Png, 100);
                return data.ToArray();
            }
            catch { return null; }
        });

        if (croppedBytes != null && croppedBytes.Length > 0)
        {
            if (Avalonia.Application.Current?.ApplicationLifetime is Avalonia.Controls.ApplicationLifetimes.IClassicDesktopStyleApplicationLifetime desktop && desktop.MainWindow != null)
            {
                var topLevel = Avalonia.Controls.TopLevel.GetTopLevel(desktop.MainWindow);
                if (topLevel?.Clipboard != null)
                {
                    string tempPng = Path.Combine(Path.GetTempPath(), $"PdfSnapshot_{Guid.NewGuid():N}.png");
                    await File.WriteAllBytesAsync(tempPng, croppedBytes);
                    await topLevel.Clipboard.SetTextAsync(tempPng);
                    ShowToastRequested?.Invoke($"Snapshot saved to {Path.GetFileName(tempPng)}");
                }
            }
        }
    }

    [RelayCommand]
    public async Task CopyPageTextAsync()
    {
        if (SelectedPage == null || string.IsNullOrWhiteSpace(SelectedPage.ExtractedText))
        {
            ShowToastRequested?.Invoke("No text on current page to copy.");
            return;
        }

        if (Avalonia.Application.Current?.ApplicationLifetime is Avalonia.Controls.ApplicationLifetimes.IClassicDesktopStyleApplicationLifetime desktop && desktop.MainWindow != null)
        {
            var topLevel = Avalonia.Controls.TopLevel.GetTopLevel(desktop.MainWindow);
            if (topLevel?.Clipboard != null)
            {
                await topLevel.Clipboard.SetTextAsync(SelectedPage.ExtractedText);
                ShowToastRequested?.Invoke($"Copied all text from Page {SelectedPage.PageNumber} to Clipboard.");
            }
        }
    }

    [RelayCommand]
    public async Task CopyAllDocumentTextAsync()
    {
        var sb = new StringBuilder();
        foreach (var page in Pages)
        {
            if (!string.IsNullOrWhiteSpace(page.ExtractedText))
            {
                sb.AppendLine($"--- Page {page.PageNumber} ---");
                sb.AppendLine(page.ExtractedText);
                sb.AppendLine();
            }
        }

        if (sb.Length == 0)
        {
            ShowToastRequested?.Invoke("No text found in document.");
            return;
        }

        if (Avalonia.Application.Current?.ApplicationLifetime is Avalonia.Controls.ApplicationLifetimes.IClassicDesktopStyleApplicationLifetime desktop && desktop.MainWindow != null)
        {
            var topLevel = Avalonia.Controls.TopLevel.GetTopLevel(desktop.MainWindow);
            if (topLevel?.Clipboard != null)
            {
                await topLevel.Clipboard.SetTextAsync(sb.ToString());
                ShowToastRequested?.Invoke($"Copied text from all {Pages.Count} pages to Clipboard.");
            }
        }
    }

}
