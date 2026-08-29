using System;
using System.Collections.ObjectModel;
using System.Linq;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using PdfEditorApp.Models;
using PdfEditorApp.Models.Elements;
using PdfEditorApp.ViewModels.ElementViewModels;

namespace PdfEditorApp.ViewModels;

public partial class PageViewModel : ViewModelBase
{
    [ObservableProperty]
    private string _id = Guid.NewGuid().ToString("N");

    [ObservableProperty]
    private int _pageNumber = 1;

    [ObservableProperty]
    private PageFormat _format = PageFormat.A4;

    [ObservableProperty]
    private PageOrientation _orientation = PageOrientation.Portrait;

    [ObservableProperty]
    private int _rotationAngle = 0;

    [ObservableProperty]
    private double _width = 800;

    [ObservableProperty]
    private double _height = 1131;

    [ObservableProperty]
    private string _backgroundColorHex = "#FFFFFF";

    [ObservableProperty]
    private bool _showHeaderFooter = true;

    [ObservableProperty]
    private string? _headerLeft;

    [ObservableProperty]
    private string? _headerRight;

    [ObservableProperty]
    private string? _footerLeft = "CONFIDENTIAL & PROPRIETARY";

    [ObservableProperty]
    private string? _footerRight = "Page 1 of 1";

    [ObservableProperty]
    private bool _isSelected;

    [ObservableProperty]
    private ElementViewModelBase? _selectedElement;

    public ObservableCollection<ElementViewModelBase> Elements { get; } = new();

    public event Action<ElementViewModelBase?>? SelectionChanged;

    public void SelectElement(ElementViewModelBase? element)
    {
        foreach (var el in Elements)
        {
            el.IsSelected = (el == element);
            if (!el.IsSelected) el.IsInEditMode = false;
        }

        SelectedElement = element;
        SelectionChanged?.Invoke(element);
    }

    public void ClearSelection()
    {
        SelectElement(null);
    }

    public void AddElement(ElementViewModelBase element)
    {
        int maxZ = Elements.Count > 0 ? Elements.Max(e => e.ZIndex) + 1 : 1;
        element.ZIndex = maxZ;
        Elements.Add(element);
        SelectElement(element);
    }

    public void RemoveElement(ElementViewModelBase element)
    {
        if (SelectedElement == element)
        {
            SelectElement(null);
        }
        Elements.Remove(element);
    }

    public void BringToFront(ElementViewModelBase element)
    {
        if (Elements.Count <= 1) return;
        int maxZ = Elements.Max(e => e.ZIndex);
        element.ZIndex = maxZ + 1;
    }

    public void SendToBack(ElementViewModelBase element)
    {
        if (Elements.Count <= 1) return;
        int minZ = Elements.Min(e => e.ZIndex);
        element.ZIndex = Math.Max(0, minZ - 1);
    }

    [RelayCommand]
    public void RotateClockwise()
    {
        RotationAngle = (RotationAngle + 90) % 360;
    }

    public PdfPageModel ToModel()
    {
        var model = new PdfPageModel
        {
            Id = Id,
            PageNumber = PageNumber,
            Format = Format,
            Orientation = Orientation,
            RotationAngle = RotationAngle,
            Width = Width,
            Height = Height,
            BackgroundColorHex = BackgroundColorHex,
            ShowHeaderFooter = ShowHeaderFooter,
            HeaderLeft = HeaderLeft,
            HeaderRight = HeaderRight,
            FooterLeft = FooterLeft,
            FooterRight = FooterRight
        };

        foreach (var el in Elements)
        {
            model.Elements.Add(el.ToModel());
        }

        return model;
    }

    public void LoadFromModel(PdfPageModel model)
    {
        Id = model.Id;
        PageNumber = model.PageNumber;
        Format = model.Format;
        Orientation = model.Orientation;
        RotationAngle = model.RotationAngle;
        Width = model.Width;
        Height = model.Height;
        BackgroundColorHex = model.BackgroundColorHex;
        ShowHeaderFooter = model.ShowHeaderFooter;
        HeaderLeft = model.HeaderLeft;
        HeaderRight = model.HeaderRight;
        FooterLeft = model.FooterLeft;
        FooterRight = model.FooterRight;

        Elements.Clear();
        foreach (var elModel in model.Elements)
        {
            ElementViewModelBase vm = elModel switch
            {
                PdfTextElement txt => new TextElementViewModel(),
                PdfImageElement img => new ImageElementViewModel(),
                PdfShapeElement shp => new ShapeElementViewModel(),
                PdfDividerElement div => new DividerElementViewModel(),
                PdfChartElement ch => new ChartElementViewModel(),
                PdfTableElement tbl => new TableElementViewModel(),
                PdfWatermarkElement wm => new WatermarkElementViewModel(),
                _ => new TextElementViewModel()
            };
            vm.LoadFromModel(elModel);
            Elements.Add(vm);
        }
    }
}
