using System;
using CommunityToolkit.Mvvm.ComponentModel;

namespace PdfEditorApp.Models;

/// <summary>
/// Representation of an individual page in a merged or output PDF for in-app page strip browsing.
/// </summary>
public partial class PdfPagePreviewThumbnail : ObservableObject
{
    [ObservableProperty]
    private int _pageNumber = 1;

    [ObservableProperty]
    private string _pageLabel = "Page 1";

    [ObservableProperty]
    private double _widthPoints = 595;

    [ObservableProperty]
    private double _heightPoints = 842;

    [ObservableProperty]
    private bool _isLandscape;

    [ObservableProperty]
    private string _dimensionsText = "595 × 842 pt";

    [ObservableProperty]
    private string _pageSummary = string.Empty;

    [ObservableProperty]
    private bool _isSelected;
}
