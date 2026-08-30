using System;
using System.IO;
using CommunityToolkit.Mvvm.ComponentModel;
using PdfEditorApp.ViewModels;

namespace PdfEditorApp.Models;

/// <summary>
/// Rich metadata and preview representation for a PDF document selected in a tool workspace.
/// </summary>
public partial class PdfFilePreviewItem : ObservableObject
{
    [ObservableProperty]
    private string _filePath = string.Empty;

    [ObservableProperty]
    private string _fileName = string.Empty;

    [ObservableProperty]
    private string _directoryPath = string.Empty;

    [ObservableProperty]
    private long _fileSizeBytes;

    [ObservableProperty]
    private string _fileSizeFormatted = "0 KB";

    [ObservableProperty]
    private int _pageCount = 1;

    [ObservableProperty]
    private string _pageCountText = "1 Page";

    [ObservableProperty]
    private int _orderIndex = 1;

    [ObservableProperty]
    private string _orderIndexText = "#1";

    [ObservableProperty]
    private string _pageDimensionsText = "A4 · Standard";

    [ObservableProperty]
    private bool _isCorrupted;

    [ObservableProperty]
    private PageViewModel? _pagePreview;

    public bool HasLivePreview => PagePreview != null;

    public static string FormatBytes(long bytes)
    {
        if (bytes < 1024) return $"{bytes} B";
        if (bytes < 1024 * 1024) return $"{bytes / 1024.0:F1} KB";
        return $"{bytes / (1024.0 * 1024.0):F1} MB";
    }

    public static PdfFilePreviewItem CreateFromFile(string filePath, int orderIndex = 1, int pageCount = 1)
    {
        var fi = new FileInfo(filePath);
        long size = fi.Exists ? fi.Length : 0;
        string name = Path.GetFileName(filePath);
        string dir = Path.GetDirectoryName(filePath) ?? string.Empty;

        return new PdfFilePreviewItem
        {
            FilePath = filePath,
            FileName = name,
            DirectoryPath = dir,
            FileSizeBytes = size,
            FileSizeFormatted = FormatBytes(size),
            PageCount = Math.Max(1, pageCount),
            PageCountText = pageCount == 1 ? "1 Page" : $"{pageCount} Pages",
            OrderIndex = orderIndex,
            OrderIndexText = $"#{orderIndex}",
            PageDimensionsText = "Standard PDF",
            IsCorrupted = false
        };
    }
}
