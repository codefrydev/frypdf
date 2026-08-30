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
    private bool _isBlank;

    [ObservableProperty]
    private bool _isLandscape;

    [ObservableProperty]
    private bool _isFeatured;

    public double AspectRatio => PagePreview.Height > 0 ? PagePreview.Width / PagePreview.Height : 0.707;
}
