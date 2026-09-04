using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Avalonia;
using Avalonia.Input.Platform;
using Avalonia.Media.Imaging;
using Avalonia.Platform.Storage;
using CommunityToolkit.Mvvm.Input;
using CommunityToolkit.Mvvm.Messaging;
using PdfEditorApp.Core.Models;
using PdfEditorApp.Messages;
using PdfEditorApp.Models;
using PdfEditorApp.Services;
using PdfEditorApp.Services.Ocr;
using PdfEditorApp.Services.Tools.Core;
using UglyToad.PdfPig;
using UglyToad.PdfPig.Content;
using UglyToad.PdfPig.Outline;
using UglyToad.PdfPig.Rendering.Skia;

namespace PdfEditorApp.ViewModels;

public partial class PdfViewerViewModel
{
    // --- Core Document Loading & Text Geometry Extraction ---

    public static (string text, List<PdfViewerWordItem> words, List<PdfViewerTextLineItem> lines) ExtractPageTextGeometry(Page page)
    {
        double pageHeight = Math.Max(100, page.Height);
        var rawWords = page.GetWords().Where(w => !string.IsNullOrWhiteSpace(w.Text)).ToList();
        if (rawWords.Count == 0)
        {
            return (page.Text ?? string.Empty, new List<PdfViewerWordItem>(), new List<PdfViewerTextLineItem>());
        }

        var wordItems = new List<PdfViewerWordItem>();
        int wordIdx = 0;
        foreach (var w in rawWords)
        {
            double x = Math.Max(0, w.BoundingBox.Left);
            double y = Math.Max(0, pageHeight - w.BoundingBox.Top);
            double width = Math.Max(1, w.BoundingBox.Width);
            double height = Math.Max(1, w.BoundingBox.Height);

            var wordItem = new PdfViewerWordItem
            {
                Text = w.Text,
                Bounds = new Rect(x, y, width, height),
                WordIndex = wordIdx++
            };

            if (w.Letters != null && w.Letters.Count > 0)
            {
                foreach (var letter in w.Letters)
                {
                    if (string.IsNullOrEmpty(letter.Value)) continue;
                    double lx = Math.Max(0, letter.BoundingBox.Left);
                    double ly = Math.Max(0, pageHeight - letter.BoundingBox.Top);
                    double lw = Math.Max(0.5, letter.BoundingBox.Width);
                    double lh = Math.Max(1, letter.BoundingBox.Height);

                    wordItem.Glyphs.Add(new PdfViewerGlyphItem
                    {
                        Character = letter.Value[0],
                        Bounds = new Rect(lx, ly, lw, lh)
                    });
                }
            }

            wordItems.Add(wordItem);
        }

        // Group words into lines based on vertical overlap
        var sortedWords = wordItems.OrderBy(w => w.Bounds.Top).ThenBy(w => w.Bounds.Left).ToList();
        var lineList = new List<PdfViewerTextLineItem>();

        foreach (var word in sortedWords)
        {
            var line = lineList.FirstOrDefault(l =>
            {
                double overlapTop = Math.Max(l.Bounds.Top, word.Bounds.Top);
                double overlapBottom = Math.Min(l.Bounds.Bottom, word.Bounds.Bottom);
                double overlap = overlapBottom - overlapTop;
                return overlap > Math.Min(l.Bounds.Height, word.Bounds.Height) * 0.45;
            });

            if (line == null)
            {
                line = new PdfViewerTextLineItem
                {
                    LineIndex = lineList.Count,
                    Bounds = word.Bounds
                };
                line.Words.Add(word);
                lineList.Add(line);
            }
            else
            {
                line.Words.Add(word);
                line.Bounds = line.Bounds.Union(word.Bounds);
            }
        }

        // Sort words in each line horizontally and build line text
        var sb = new StringBuilder();
        int lineIdx = 0;
        foreach (var line in lineList.OrderBy(l => l.Bounds.Top))
        {
            line.LineIndex = lineIdx++;
            var sortedLineWords = line.Words.OrderBy(w => w.Bounds.Left).ToList();
            line.Words.Clear();
            line.Words.AddRange(sortedLineWords);

            foreach (var w in line.Words)
            {
                w.LineIndex = line.LineIndex;
            }

            line.Text = string.Join(" ", line.Words.Select(w => w.Text));
            sb.AppendLine(line.Text);
        }

        string fullText = sb.ToString().TrimEnd();
        if (string.IsNullOrWhiteSpace(fullText))
        {
            fullText = page.Text ?? string.Empty;
        }

        return (fullText, wordItems, lineList.OrderBy(l => l.Bounds.Top).ToList());
    }

    /// <summary>
    /// Performs OCR on a rendered page image if vector text extraction yields no words (e.g. scanned documents).
    /// </summary>
    public (string text, List<PdfViewerWordItem> words, List<PdfViewerTextLineItem> lines) ExtractOcrPageTextGeometry(int pageNumber, double width, double height)
    {
        try
        {
            var ocrProvider = CompositeOcrProvider.Default;
            if (!ocrProvider.IsAvailable)
            {
                return (string.Empty, new List<PdfViewerWordItem>(), new List<PdfViewerTextLineItem>());
            }

            byte[]? pagePng = RenderPageBytesAtScale(pageNumber, 1.5f);
            if (pagePng == null || pagePng.Length == 0)
            {
                return (string.Empty, new List<PdfViewerWordItem>(), new List<PdfViewerTextLineItem>());
            }

            var ocrResult = ocrProvider.RecognizeTextAsync(pagePng).GetAwaiter().GetResult();
            if (!ocrResult.Success || ocrResult.Words.Count == 0)
            {
                return (string.Empty, new List<PdfViewerWordItem>(), new List<PdfViewerTextLineItem>());
            }

            var words = new List<PdfViewerWordItem>();
            int wIdx = 0;
            foreach (var ow in ocrResult.Words)
            {
                double wx = Math.Max(0, ow.NormalizedBounds.X * width);
                double wy = Math.Max(0, ow.NormalizedBounds.Y * height);
                double ww = Math.Max(1, ow.NormalizedBounds.Width * width);
                double wh = Math.Max(1, ow.NormalizedBounds.Height * height);

                var wordItem = new PdfViewerWordItem
                {
                    Text = ow.Text,
                    Bounds = new Rect(wx, wy, ww, wh),
                    WordIndex = wIdx++
                };

                double charW = ww / Math.Max(1, ow.Text.Length);
                for (int ci = 0; ci < ow.Text.Length; ci++)
                {
                    wordItem.Glyphs.Add(new PdfViewerGlyphItem
                    {
                        Character = ow.Text[ci],
                        Bounds = new Rect(wx + ci * charW, wy, Math.Max(0.5, charW), wh)
                    });
                }

                words.Add(wordItem);
            }

            var lines = new List<PdfViewerTextLineItem>();
            int lIdx = 0;
            foreach (var ol in ocrResult.Lines)
            {
                double lx = Math.Max(0, ol.NormalizedBounds.X * width);
                double ly = Math.Max(0, ol.NormalizedBounds.Y * height);
                double lw = Math.Max(1, ol.NormalizedBounds.Width * width);
                double lh = Math.Max(1, ol.NormalizedBounds.Height * height);

                var lineItem = new PdfViewerTextLineItem
                {
                    LineIndex = lIdx++,
                    Bounds = new Rect(lx, ly, lw, lh)
                };

                var lineWords = words
                    .Where(w => w.Bounds.Top >= ly - 4 && w.Bounds.Bottom <= ly + lh + 4)
                    .OrderBy(w => w.Bounds.Left)
                    .ToList();

                foreach (var lw_item in lineWords)
                {
                    lw_item.LineIndex = lineItem.LineIndex;
                }

                lineItem.Words.AddRange(lineWords);
                lineItem.Text = string.Join(" ", lineItem.Words.Select(w => w.Text));
                lines.Add(lineItem);
            }

            return (ocrResult.FullText, words, lines);
        }
        catch
        {
            return (string.Empty, new List<PdfViewerWordItem>(), new List<PdfViewerTextLineItem>());
        }
    }

    public byte[]? CurrentPdfBytes => _currentPdfBytes;

    /// <summary>
    /// Populates a page's word/line geometry for hit-testing (hover cursor, click-to-select-word).
    /// Runs off the UI thread — the first hover/click on a page the background sweep hasn't
    /// reached yet used to open and fully parse the PDF synchronously on the dispatcher thread.
    /// </summary>
    public void EnsurePageGeometry(PdfViewerPageItem page)
    {
        if (page.Words.Count > 0 || page.IsGeometryLoading || _currentPdfBytes == null || _currentPdfBytes.Length == 0) return;

        page.IsGeometryLoading = true;
        int pageNumber = page.PageNumber;

        Task.Run(() =>
        {
            string? text = null;
            List<PdfViewerWordItem>? words = null;
            List<PdfViewerTextLineItem>? lines = null;
            double width = 0, height = 0;

            try
            {
                lock (_renderLock)
                {
                    var doc = OpenOrReuseDocument();
                    if (doc != null && pageNumber >= 1 && pageNumber <= doc.NumberOfPages)
                    {
                        var p = doc.GetPage(pageNumber);
                        (text, words, lines) = ExtractPageTextGeometry(p);
                        if (p.Width > 0 && p.Height > 0)
                        {
                            width = p.Width;
                            height = p.Height;
                        }
                    }
                }
            }
            catch { }

            // Automatic OCR fallback for scanned pages with zero vector text
            if ((words == null || words.Count == 0) && width > 0 && height > 0)
            {
                var (ocrText, ocrWords, ocrLines) = ExtractOcrPageTextGeometry(pageNumber, width, height);
                if (ocrWords.Count > 0)
                {
                    text = ocrText;
                    words = ocrWords;
                    lines = ocrLines;
                }
            }

            // Assign geometry data directly so both UI and headless test runners receive words immediately
            if (text != null && words != null && lines != null)
            {
                page.ExtractedText = text;
                page.Words = words;
                page.TextLines = lines;
                if (width > 0 && height > 0)
                {
                    page.WidthPoints = width;
                    page.HeightPoints = height;
                }
            }
            page.IsGeometryLoading = false;

            try
            {
                Avalonia.Threading.Dispatcher.UIThread.Post(() =>
                {
                    if (SelectedPage != null && SelectedPage.PageNumber == pageNumber)
                    {
                        IsCurrentPageScanned = (page.Words.Count == 0 && string.IsNullOrWhiteSpace(page.ExtractedText));
                    }
                    page.NotifySelectionChanged();
                });
            }
            catch { }
        });
    }

    public async Task LoadDocumentAsync(string filePath, string? password = null)
    {
        if (!File.Exists(filePath))
        {
            StatusMessage = $"File not found: {filePath}";
            return;
        }

        try
        {
            byte[] bytes = await File.ReadAllBytesAsync(filePath);
            await LoadDocumentFromBytesAsync(bytes, filePath, password);
        }
        catch (Exception ex)
        {
            StatusMessage = $"Error loading document: {ex.Message}";
        }
    }

    public Task LoadDocumentBytesAsync(byte[] pdfBytes, string? documentTitle = null, string? password = null)
        => LoadDocumentFromBytesAsync(pdfBytes, documentTitle ?? "Document.pdf", password);

    public async Task LoadDocumentFromBytesAsync(byte[] pdfBytes, string sourceFilePath = "", string? password = null)
    {
        _renderCts?.Cancel();
        _renderCts = new CancellationTokenSource();
        var ct = _renderCts.Token;

        _backgroundRenderCts?.Cancel();
        _backgroundRenderCts = new CancellationTokenSource();

        IsLoading = true;
        IsOpeningDocument = true;
        HasDocument = true;
        StatusMessage = "Opening PDF document...";
        _currentPdfBytes = pdfBytes;
        _currentPassword = password;
        CurrentFilePath = sourceFilePath;
        DocumentTitle = string.IsNullOrWhiteSpace(sourceFilePath) ? "Document.pdf" : Path.GetFileName(sourceFilePath);

        // Release the previous document's rendered bitmaps before replacing it — this
        // ViewModel is a long-lived singleton reused across every document open, so
        // nothing else ever frees this memory otherwise.
        foreach (var oldPage in Pages)
        {
            oldPage.Dispose();
        }
        _lastVisibleFirstPage = -1;
        _lastVisibleLastPage = -1;

        lock (_renderLock)
        {
            _openDocument?.Dispose();
            _openDocument = null;
        }

        Pages.Clear();
        PageSpreads.Clear();
        Bookmarks.Clear();
        Annotations.Clear();
        MetadataItems.Clear();
        SearchResults.Clear();

        try
        {
            byte[] sanitizedBytes = PdfFileHelper.SanitizePdfBytes(pdfBytes);
            _currentPdfBytes = sanitizedBytes;

            var (metaList, pagesList, total) = await Task.Run(() =>
            {
                var parsingOptions = new ParsingOptions();
                if (!string.IsNullOrEmpty(password))
                {
                    parsingOptions.Password = password;
                }

                PdfDocument? doc = null;
                try
                {
                    doc = PdfDocument.Open(sanitizedBytes, parsingOptions);
                }
                catch
                {
                    try
                    {
                        byte[] repaired = PdfFileHelper.SalvageAndRepairPdfBytes(sanitizedBytes);
                        doc = PdfDocument.Open(repaired, parsingOptions);
                    }
                    catch
                    {
                        doc = PdfDocument.Open(pdfBytes, parsingOptions);
                    }
                }

                try
                {
                    try
                    {
                        PdfPigExtensions.AddSkiaPageFactory(doc);
                    }
                    catch { }

                    int total = doc.NumberOfPages;
                    if (total == 0)
                    {
                        doc.Dispose();
                        return (new List<PdfViewerMetadataItem>(), new List<PdfViewerPageItem>(), 0);
                    }

                    // 1. Fast Page 1 extraction & immediate render
                    var firstPage = doc.GetPage(1);
                    double defaultWidth = Math.Max(100, firstPage.Width);
                    double defaultHeight = Math.Max(100, firstPage.Height);
                    int defaultRot = (int)firstPage.Rotation.Value;
                    var (firstPageText, firstWords, firstLines) = ExtractPageTextGeometry(firstPage);
                    string firstPageSummary = "";
                    if (!string.IsNullOrWhiteSpace(firstPageText))
                    {
                        var firstLine = firstPageText.Split(new[] { '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries).FirstOrDefault() ?? "";
                        firstPageSummary = firstLine.Length > 50 ? firstLine.Substring(0, 50) + "..." : firstLine;
                    }

                    Bitmap? bmp1 = null;
                    try
                    {
                        using var pngStream = PdfPigExtensions.GetPageAsPng(doc, 1, PdfViewerPageItem.BasePageRenderScale, 100);
                        if (pngStream != null && pngStream.Length > 0)
                        {
                            pngStream.Position = 0;
                            bmp1 = new Bitmap(pngStream);
                        }
                    }
                    catch { }

                    // 2. Instant Skeleton Generation, with REAL per-page dimensions. Reading a
                    // page's declared size is cheap (the page dictionary's MediaBox) — nothing
                    // like the cost of full word/text extraction — so it's worth doing for every
                    // page right now rather than defaulting every page to page 1's size until
                    // the much slower progressive background sweep happens to reach it. Every
                    // scroll-position and click-to-navigate calculation sums page heights across
                    // however many preceding pages there are, so a wrong default anywhere in
                    // that chain drifts every page after it out of sync until the real value
                    // loads — for a large document, that's most of the book for a long time.
                    var pagesList = new List<PdfViewerPageItem>(total);
                    for (int i = 1; i <= total; i++)
                    {
                        double pageWidth = defaultWidth;
                        double pageHeight = defaultHeight;
                        int pageRot = (i == 1) ? defaultRot : 0;

                        if (i > 1)
                        {
                            try
                            {
                                var pg = doc.GetPage(i);
                                if (pg.Width > 0) pageWidth = pg.Width;
                                if (pg.Height > 0) pageHeight = pg.Height;
                                pageRot = (int)pg.Rotation.Value;
                            }
                            catch { }
                        }

                        pagesList.Add(new PdfViewerPageItem
                        {
                            PageNumber = i,
                            WidthPoints = pageWidth,
                            HeightPoints = pageHeight,
                            RotationAngle = pageRot,
                            ExtractedText = (i == 1) ? firstPageText : "",
                            Words = (i == 1) ? firstWords : new List<PdfViewerWordItem>(),
                            TextLines = (i == 1) ? firstLines : new List<PdfViewerTextLineItem>(),
                            PageSummary = (i == 1) ? firstPageSummary : "",
                            Bitmap = (i == 1) ? bmp1 : null,
                            RenderedScale = PdfViewerPageItem.BasePageRenderScale,
                            IsSelected = (i == 1)
                        });
                    }

                    // 3. Metadata
                    var info = doc.Information;
                    string dimsInches = $"{defaultWidth / 72.0:F1}\" × {defaultHeight / 72.0:F1}\"";
                    string dimsMm = $"{defaultWidth * 25.4 / 72.0:F0} × {defaultHeight * 25.4 / 72.0:F0} mm";

                    var metaList = new List<PdfViewerMetadataItem>
                    {
                        new PdfViewerMetadataItem { Label = "File Name", Value = DocumentTitle, IconKind = "FileDocumentOutline" },
                        new PdfViewerMetadataItem { Label = "File Size", Value = PdfFilePreviewItem.FormatBytes(pdfBytes.Length), IconKind = "DatabaseOutline" },
                        new PdfViewerMetadataItem { Label = "Total Pages", Value = $"{total} Pages", IconKind = "BookOpenPageVariantOutline" },
                        new PdfViewerMetadataItem { Label = "Page Dimensions", Value = $"{dimsInches} ({dimsMm})", IconKind = "AspectRatio" },
                        new PdfViewerMetadataItem { Label = "Title", Value = string.IsNullOrWhiteSpace(info.Title) ? "Untitled Document" : info.Title, IconKind = "FormatTitle" },
                        new PdfViewerMetadataItem { Label = "Author", Value = string.IsNullOrWhiteSpace(info.Author) ? "Unknown Author" : info.Author, IconKind = "AccountOutline" },
                        new PdfViewerMetadataItem { Label = "Subject", Value = string.IsNullOrWhiteSpace(info.Subject) ? "None specified" : info.Subject, IconKind = "Subject" },
                        new PdfViewerMetadataItem { Label = "Keywords", Value = string.IsNullOrWhiteSpace(info.Keywords) ? "None" : info.Keywords, IconKind = "TagOutline" },
                        new PdfViewerMetadataItem { Label = "Creator Application", Value = string.IsNullOrWhiteSpace(info.Creator) ? "FryPDF" : info.Creator, IconKind = "CogOutline" },
                        new PdfViewerMetadataItem { Label = "PDF Producer", Value = string.IsNullOrWhiteSpace(info.Producer) ? "codefrydev.in" : info.Producer, IconKind = "ApplicationOutline" },
                        new PdfViewerMetadataItem { Label = "PDF Version", Value = $"PDF {doc.Version}", IconKind = "ShieldCheckOutline" },
                        new PdfViewerMetadataItem { Label = "Security Status", Value = doc.IsEncrypted ? "Password Protected (Encrypted)" : "Standard (No Security)", IconKind = doc.IsEncrypted ? "LockOutline" : "LockOpenOutline" }
                    };

                    // Keep this document open for reuse by on-demand rendering/geometry
                    // extraction instead of parsing the whole PDF from scratch again on the
                    // very first page request right after this.
                    lock (_renderLock)
                    {
                        _openDocument?.Dispose();
                        _openDocument = doc;
                    }

                    return (metaList, pagesList, total);
                }
                catch
                {
                    doc.Dispose();
                    throw;
                }
            }, ct);

            foreach (var m in metaList) MetadataItems.Add(m);
            foreach (var p in pagesList) Pages.Add(p);

            TotalPagesCount = total;
            CurrentPageNumber = 1;
            JumpPageText = "1";
            SelectedPage = Pages.FirstOrDefault();
            HasDocument = true;
            IsLoading = false;
            IsOpeningDocument = false;
            StatusMessage = $"Ready • {total} pages";

            bool page1HasNoText = (SelectedPage != null && SelectedPage.Words.Count == 0 && string.IsNullOrWhiteSpace(SelectedPage.ExtractedText));
            IsScannedDocument = page1HasNoText;
            IsCurrentPageScanned = page1HasNoText;
            IsScannedBannerDismissed = false;

            RebuildPageSpreads();

            // Start progressive background worker to render remaining pages, thumbnails, and bookmarks
            StartBackgroundWorker(_backgroundRenderCts.Token);
        }
        catch (OperationCanceledException)
        {
            StatusMessage = "Loading cancelled.";
            IsLoading = false;
            IsOpeningDocument = false;
        }
        catch (Exception ex)
        {
            StatusMessage = $"Error: {ex.Message}";
            IsLoading = false;
            IsOpeningDocument = false;
        }
    }

    /// <summary>Renders a specific page at the specified scale directly from PDF bytes using Skia.</summary>
    /// <summary>Renders a specific page at the specified scale directly to image bytes.</summary>

    private static void ExtractBookmarksRecursive(IEnumerable<BookmarkNode> nodes, IList<PdfViewerBookmarkItem> targetList)
    {
        if (nodes == null) return;
        foreach (var node in nodes)
        {
            if (node is DocumentBookmarkNode docNode)
            {
                var bm = new PdfViewerBookmarkItem
                {
                    Title = docNode.Title ?? "Section",
                    PageNumber = Math.Max(1, docNode.PageNumber)
                };
                if (docNode.Children != null && docNode.Children.Count > 0)
                {
                    ExtractBookmarksRecursive(docNode.Children, bm.Children);
                }
                targetList.Add(bm);
            }
        }
    }


    [RelayCommand]
    public void RenameDocument()
    {
        if (!string.IsNullOrEmpty(CurrentFilePath))
        {
            WeakReferenceMessenger.Default.Send(new PromptRenameMessage(CurrentFilePath));
        }
    }

    [RelayCommand]
    public void DeleteDocument()
    {
        if (!string.IsNullOrEmpty(CurrentFilePath))
        {
            WeakReferenceMessenger.Default.Send(new PromptDeleteMessage(CurrentFilePath));
        }
    }

    [RelayCommand]
    public void DuplicateDocument()
    {
        if (!string.IsNullOrEmpty(CurrentFilePath))
        {
            if (FileOperationHelper.DuplicateFile(CurrentFilePath, out var newPath, out var error))
            {
                ShowToast($"Created duplicate: {Path.GetFileName(newPath!)}");
            }
            else
            {
                ShowToast($"Could not duplicate: {error}");
            }
        }
    }

    [RelayCommand]
    public void RevealInFileManager()
    {
        if (!string.IsNullOrEmpty(CurrentFilePath))
        {
            if (FileOperationHelper.RevealInFileManager(CurrentFilePath, out var error))
            {
                ShowToast("Opened containing folder");
            }
            else
            {
                ShowToast($"Could not reveal file: {error}");
            }
        }
    }

    [RelayCommand]
    public async Task CopyPath()
    {
        if (!string.IsNullOrEmpty(CurrentFilePath))
        {
            try
            {
                if (Avalonia.Application.Current?.ApplicationLifetime is Avalonia.Controls.ApplicationLifetimes.IClassicDesktopStyleApplicationLifetime desktop && desktop.MainWindow?.Clipboard != null)
                {
                    await desktop.MainWindow.Clipboard.SetTextAsync(CurrentFilePath);
                }
            }
            catch { }
            ShowToast("Copied file path to clipboard");
        }
    }



    [RelayCommand]
    public async Task OpenAnotherPdfAsync()
    {
        if (StorageProvider == null)
        {
            WeakReferenceMessenger.Default.Send(new OpenPdfPickerMessage());
            return;
        }

        var files = await StorageProvider.OpenFilePickerAsync(new FilePickerOpenOptions
        {
            Title = "Open PDF Document to Read",
            AllowMultiple = false,
            FileTypeFilter = new[]
            {
                new FilePickerFileType("PDF Documents (*.pdf)")
                {
                    Patterns = new[] { "*.pdf" }
                }
            }
        });

        if (files.Count > 0)
        {
            string chosenPath = files[0].Path.LocalPath;
            await LoadDocumentAsync(chosenPath);
            ShowToast($"Reading: {Path.GetFileName(chosenPath)}");
        }
    }

    [RelayCommand]
    public async Task SaveAsAsync()
    {
        if (_currentPdfBytes == null || _currentPdfBytes.Length == 0 || StorageProvider == null) return;

        var file = await StorageProvider.SaveFilePickerAsync(new FilePickerSaveOptions
        {
            Title = "Save PDF As",
            DefaultExtension = "pdf",
            SuggestedFileName = DocumentTitle,
            FileTypeChoices = new[]
            {
                new FilePickerFileType("PDF Documents (*.pdf)")
                {
                    Patterns = new[] { "*.pdf" }
                }
            }
        });

        if (file != null)
        {
            try
            {
                await File.WriteAllBytesAsync(file.Path.LocalPath, _currentPdfBytes);
                ShowToast($"Saved copy: {Path.GetFileName(file.Path.LocalPath)}");
            }
            catch (IOException)
            {
                ShowToast($"Cannot save: '{Path.GetFileName(file.Path.LocalPath)}' is open in another program.");
            }
            catch (Exception ex)
            {
                ShowToast($"Save failed: {ex.Message}");
            }
        }
    }

}
