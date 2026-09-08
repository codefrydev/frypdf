using Avalonia.Media.Imaging;
using CommunityToolkit.Mvvm.ComponentModel;

namespace PdfEditorApp.Models;

/// <summary>
/// One rendered page in a tool screen's live input-document preview.
/// </summary>
public partial class PdfToolPreviewPage : ObservableObject
{
    [ObservableProperty]
    private int _pageNumber = 1;

    [ObservableProperty]
    private double _widthPoints = 595;

    [ObservableProperty]
    private double _heightPoints = 842;

    [ObservableProperty]
    private Bitmap? _bitmap;

    [ObservableProperty]
    private Bitmap? _thumbnailBitmap;

    [ObservableProperty]
    private bool _isSelected;

    /// <summary>Scale the current <see cref="Bitmap"/> was rendered at, so zoom changes only re-render when the resolution actually needs to change.</summary>
    [ObservableProperty]
    private float _renderedScale;

    /// <summary>
    /// Disposes the bitmap being replaced. Avalonia's <see cref="Bitmap"/> holds native Skia
    /// memory that the GC does not account for, and the live preview assigns a fresh bitmap on
    /// every zoom re-render — at 4x scale that is tens of MB per discarded page.
    /// </summary>
    /// <remarks>
    /// Guarded against the two properties holding the same instance, which happens while a
    /// full-resolution render is still pending and the thumbnail stands in for it.
    /// </remarks>
    partial void OnBitmapChanging(Bitmap? oldValue, Bitmap? newValue)
        => DisposeIfUnreferenced(oldValue, newValue, ThumbnailBitmap);

    partial void OnThumbnailBitmapChanging(Bitmap? oldValue, Bitmap? newValue)
        => DisposeIfUnreferenced(oldValue, newValue, Bitmap);

    private static void DisposeIfUnreferenced(Bitmap? oldValue, Bitmap? newValue, Bitmap? stillReferenced)
    {
        if (oldValue == null) return;
        if (ReferenceEquals(oldValue, newValue)) return;
        if (ReferenceEquals(oldValue, stillReferenced)) return;

        oldValue.Dispose();
    }

    public string PageLabel => $"Page {PageNumber}";
}
