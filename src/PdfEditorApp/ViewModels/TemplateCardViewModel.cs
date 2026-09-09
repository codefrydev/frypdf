using Avalonia.Media.Imaging;
using CommunityToolkit.Mvvm.ComponentModel;
using PdfEditorApp.Models;

namespace PdfEditorApp.ViewModels;

/// <summary>
/// Represents a template card in the Home View and Template Gallery,
/// holding its metadata and high-fidelity live <see cref="PageViewModel"/> preview.
/// </summary>
public partial class TemplateCardViewModel : ViewModelBase
{
    [ObservableProperty]
    private string _id = "";

    [ObservableProperty]
    private string _name = "";

    [ObservableProperty]
    private string _category = "General";

    [ObservableProperty]
    private string _subtitle = "";

    [ObservableProperty]
    private string _description = "";

    [ObservableProperty]
    private string _badge = "";

    [ObservableProperty]
    private string _accentColorHex = "#0F6CBD";

    [ObservableProperty]
    private string _iconKind = "FileDocumentOutline";

    [ObservableProperty]
    private PageViewModel _pagePreview = new();

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(ShowPreviewPlaceholder))]
    private bool _isBlank;

    [ObservableProperty]
    private bool _isLandscape;

    [ObservableProperty]
    private bool _isFeatured;

    public double AspectRatio => PagePreview.Height > 0 ? PagePreview.Width / PagePreview.Height : 0.707;

    /// <summary>
    /// The page preview rendered once to a bitmap; null until it has been rasterized.
    /// </summary>
    /// <remarks>
    /// Cards bind an <c>&lt;Image&gt;</c> to this rather than building the page as a live visual
    /// tree. See <see cref="Services.TemplatePreviewRasterizer"/> for why.
    /// </remarks>
    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(HasPreviewImage))]
    [NotifyPropertyChangedFor(nameof(ShowPreviewPlaceholder))]
    private Bitmap? _previewImage;

    private Bitmap? _previousPreviewImage;

    /// <summary>Disposes the outgoing bitmap — it holds native Skia memory the GC does not track.</summary>
    partial void OnPreviewImageChanged(Bitmap? value)
    {
        if (_previousPreviewImage != null && _previousPreviewImage != value)
        {
            _previousPreviewImage.Dispose();
        }
        _previousPreviewImage = value;
    }

    /// <summary>True once a preview has been rendered, so the card can drop its placeholder.</summary>
    public bool HasPreviewImage => PreviewImage != null;

    /// <summary>
    /// True while a non-blank template has no preview bitmap — either not rasterized yet, or
    /// rasterization failed. Without this the card would simply render as an empty box.
    /// </summary>
    public bool ShowPreviewPlaceholder => PreviewImage == null && !IsBlank;
}
