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

    public string PageLabel => $"Page {PageNumber}";
}
