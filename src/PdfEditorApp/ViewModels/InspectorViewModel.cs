using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using PdfEditorApp.Models;
using PdfEditorApp.Services;
using PdfEditorApp.ViewModels.ElementViewModels;

namespace PdfEditorApp.ViewModels;

public class ColorSwatchItem
{
    public string Name { get; set; } = "Black";
    public string Hex { get; set; } = "#201F1E";
}

public partial class InspectorViewModel : ViewModelBase
{
    public UndoRedoService? UndoRedo { get; set; }

    [ObservableProperty]
    private ElementViewModelBase? _selectedElement;

    [ObservableProperty]
    private PageViewModel? _selectedPage;

    [ObservableProperty]
    private bool _hasSelectedElement;

    [ObservableProperty]
    private bool _hasMultiSelection;

    [ObservableProperty]
    private int _selectionCount;

    [ObservableProperty]
    private bool _isTextElement;

    [ObservableProperty]
    private bool _isShapeElement;

    [ObservableProperty]
    private bool _isImageElement;

    [ObservableProperty]
    private bool _isTableElement;

    [ObservableProperty]
    private bool _isChartElement;

    [ObservableProperty]
    private bool _isDividerElement;

    [ObservableProperty]
    private bool _isWatermarkElement;

    [ObservableProperty]
    private bool _isFormFieldElement;

    [ObservableProperty]
    private bool _isQrCodeElement;

    [ObservableProperty]
    private bool _isBarcodeElement;

    [ObservableProperty]
    private bool _isRedactionElement;

    [ObservableProperty]
    private bool _isStickyNoteElement;

    [ObservableProperty]
    private bool _isMeasurementElement;

    [ObservableProperty]
    private string _selectedFontFamily = "Arial";

    partial void OnSelectedFontFamilyChanged(string value)
    {
        if (TextElement != null && !string.IsNullOrEmpty(value) && TextElement.FontFamily != value)
        {
            var el = TextElement;
            string oldFont = el.FontFamily;
            el.FontFamily = value;
            UndoRedo?.RecordAction(
                $"Font: {value}",
                () => { el.FontFamily = oldFont; SelectedFontFamily = oldFont; },
                () => { el.FontFamily = value; SelectedFontFamily = value; }
            );
        }
    }

    [ObservableProperty]
    private double _selectedFontSize = 14;

    partial void OnSelectedFontSizeChanged(double value)
    {
        if (TextElement != null && value > 0 && Math.Abs(TextElement.FontSize - value) > 0.1)
        {
            var el = TextElement;
            double oldSize = el.FontSize;
            el.FontSize = value;
            UndoRedo?.RecordAction(
                $"Font Size: {value}pt",
                () => { el.FontSize = oldSize; SelectedFontSize = oldSize; },
                () => { el.FontSize = value; SelectedFontSize = value; }
            );
        }
    }

    public ObservableCollection<string> AvailableFontFamilies { get; } = new()
    {
        "Roboto",
        "Inter",
        "Open Sans",
        "Montserrat",
        "Source Sans 3",
        "Arial",
        "Helvetica",
        "Verdana",
        "Trebuchet MS",
        "Segoe UI",
        "Playfair Display",
        "Merriweather",
        "Lora",
        "Times New Roman",
        "Georgia",
        "Cinzel",
        "Palatino",
        "Fira Code",
        "Roboto Mono",
        "Courier New",
        "Menlo",
        "Consolas",
        "Comic Neue",
        "Dancing Script",
        "Pacifico",
        "Caveat",
        "Great Vibes",
        "Lobster",
        "Bebas Neue",
        "Oswald",
        "Orbitron",
        "Impact",
        "Comic Sans MS"
    };

    public ObservableCollection<double> AvailableFontSizes { get; } = new()
    {
        8, 9, 10, 11, 12, 13, 13.5, 14, 15, 16, 18, 20, 22, 24, 28, 32, 36, 48, 72
    };

    public ObservableCollection<ColorSwatchItem> Swatches { get; } = new()
    {
        new ColorSwatchItem { Name = "Black", Hex = "#201F1E" },
        new ColorSwatchItem { Name = "Charcoal", Hex = "#4B5563" },
        new ColorSwatchItem { Name = "Accent Blue", Hex = "#0F6CBD" },
        new ColorSwatchItem { Name = "Navy", Hex = "#1E3A8A" },
        new ColorSwatchItem { Name = "Green", Hex = "#16A34A" },
        new ColorSwatchItem { Name = "Red", Hex = "#DC2626" },
        new ColorSwatchItem { Name = "Orange", Hex = "#EA580C" },
        new ColorSwatchItem { Name = "Purple", Hex = "#7C3AED" },
        new ColorSwatchItem { Name = "White", Hex = "#FFFFFF" }
    };

    public ObservableCollection<RulerUnit> MeasurementUnits { get; } = new()
    {
        RulerUnit.Points,
        RulerUnit.Inches,
        RulerUnit.Millimeters
    };

    public TextElementViewModel? TextElement => SelectedElement as TextElementViewModel;
    public ShapeElementViewModel? ShapeElement => SelectedElement as ShapeElementViewModel;
    public ImageElementViewModel? ImageElement => SelectedElement as ImageElementViewModel;
    public TableElementViewModel? TableElement => SelectedElement as TableElementViewModel;
    public ChartElementViewModel? ChartElement => SelectedElement as ChartElementViewModel;
    public DividerElementViewModel? DividerElement => SelectedElement as DividerElementViewModel;
    public WatermarkElementViewModel? WatermarkElement => SelectedElement as WatermarkElementViewModel;
    public FormFieldElementViewModel? FormFieldElement => SelectedElement as FormFieldElementViewModel;
    public QrCodeElementViewModel? QrCodeElement => SelectedElement as QrCodeElementViewModel;
    public BarcodeElementViewModel? BarcodeElement => SelectedElement as BarcodeElementViewModel;
    public RedactionElementViewModel? RedactionElement => SelectedElement as RedactionElementViewModel;
    public InkElementViewModel? InkElement => SelectedElement as InkElementViewModel;
    public StickyNoteElementViewModel? StickyNoteElement => SelectedElement as StickyNoteElementViewModel;
    public MeasurementElementViewModel? MeasurementElement => SelectedElement as MeasurementElementViewModel;

    public string ActiveCategoryName => SelectedElement != null ? SelectedElement.Kind.ToString() : "Document";
    public ObservableCollection<ColorSwatchItem> ColorSwatches => Swatches;

    public void UpdateSelection(ElementViewModelBase? element, PageViewModel? page)
    {
        SelectedElement = element;
        SelectedPage = page;

        HasSelectedElement = element != null;
        HasMultiSelection = page != null && page.SelectedElements.Count > 1;
        SelectionCount = page != null ? page.SelectedElements.Count : (element != null ? 1 : 0);

        IsTextElement = element is TextElementViewModel;
        IsShapeElement = element is ShapeElementViewModel;
        IsImageElement = element is ImageElementViewModel;
        IsTableElement = element is TableElementViewModel;
        IsChartElement = element is ChartElementViewModel;
        IsDividerElement = element is DividerElementViewModel;
        IsWatermarkElement = element is WatermarkElementViewModel;
        IsFormFieldElement = element is FormFieldElementViewModel;
        IsQrCodeElement = element is QrCodeElementViewModel;
        IsBarcodeElement = element is BarcodeElementViewModel;
        IsRedactionElement = element is RedactionElementViewModel;
        IsStickyNoteElement = element is StickyNoteElementViewModel;
        IsMeasurementElement = element is MeasurementElementViewModel;

        if (element is TextElementViewModel textVm)
        {
            SelectedFontFamily = textVm.FontFamily;
            SelectedFontSize = textVm.FontSize;

            if (!AvailableFontSizes.Contains(textVm.FontSize))
            {
                AvailableFontSizes.Add(textVm.FontSize);
            }
        }

        OnPropertyChanged(nameof(TextElement));
        OnPropertyChanged(nameof(ShapeElement));
        OnPropertyChanged(nameof(ImageElement));
        OnPropertyChanged(nameof(TableElement));
        OnPropertyChanged(nameof(ChartElement));
        OnPropertyChanged(nameof(DividerElement));
        OnPropertyChanged(nameof(WatermarkElement));
        OnPropertyChanged(nameof(FormFieldElement));
        OnPropertyChanged(nameof(QrCodeElement));
        OnPropertyChanged(nameof(BarcodeElement));
        OnPropertyChanged(nameof(RedactionElement));
        OnPropertyChanged(nameof(InkElement));
        OnPropertyChanged(nameof(StickyNoteElement));
        OnPropertyChanged(nameof(MeasurementElement));
        OnPropertyChanged(nameof(ActiveCategoryName));
    }

    [RelayCommand]
    public void SetTextColor(string hex)
    {
        if (TextElement != null && TextElement.TextColorHex != hex)
        {
            var el = TextElement;
            string oldHex = el.TextColorHex;
            el.TextColorHex = hex;
            UndoRedo?.RecordAction(
                "Change Text Color",
                () => el.TextColorHex = oldHex,
                () => el.TextColorHex = hex
            );
        }
    }

    [RelayCommand]
    public void SetShapeFillColor(string hex)
    {
        if (ShapeElement != null && ShapeElement.FillColorHex != hex)
        {
            var el = ShapeElement;
            string oldHex = el.FillColorHex;
            el.FillColorHex = hex;
            UndoRedo?.RecordAction(
                "Change Fill Color",
                () => el.FillColorHex = oldHex,
                () => el.FillColorHex = hex
            );
        }
    }

    [RelayCommand]
    public void SetShapeStrokeColor(string hex)
    {
        if (ShapeElement != null && ShapeElement.StrokeColorHex != hex)
        {
            var el = ShapeElement;
            string oldHex = el.StrokeColorHex;
            el.StrokeColorHex = hex;
            UndoRedo?.RecordAction(
                "Change Stroke Color",
                () => el.StrokeColorHex = oldHex,
                () => el.StrokeColorHex = hex
            );
        }
    }

    [RelayCommand]
    public void SetAlignment(string alignmentStr)
    {
        if (TextElement != null && Enum.TryParse<TextAlignmentMode>(alignmentStr, true, out var mode) && TextElement.Alignment != mode)
        {
            var el = TextElement;
            var oldMode = el.Alignment;
            el.Alignment = mode;
            UndoRedo?.RecordAction(
                $"Align Text {mode}",
                () => el.Alignment = oldMode,
                () => el.Alignment = mode
            );
        }
    }

    [RelayCommand]
    public void ToggleBold()
    {
        if (TextElement != null)
        {
            var el = TextElement;
            bool oldVal = el.IsBold;
            bool newVal = !oldVal;
            el.IsBold = newVal;
            UndoRedo?.RecordAction(
                newVal ? "Format Bold" : "Remove Bold",
                () => el.IsBold = oldVal,
                () => el.IsBold = newVal
            );
        }
    }

    [RelayCommand]
    public void ToggleItalic()
    {
        if (TextElement != null)
        {
            var el = TextElement;
            bool oldVal = el.IsItalic;
            bool newVal = !oldVal;
            el.IsItalic = newVal;
            UndoRedo?.RecordAction(
                newVal ? "Format Italic" : "Remove Italic",
                () => el.IsItalic = oldVal,
                () => el.IsItalic = newVal
            );
        }
    }

    [RelayCommand]
    public void ToggleUnderline()
    {
        if (TextElement != null)
        {
            var el = TextElement;
            bool oldVal = el.IsUnderline;
            bool newVal = !oldVal;
            el.IsUnderline = newVal;
            UndoRedo?.RecordAction(
                newVal ? "Format Underline" : "Remove Underline",
                () => el.IsUnderline = oldVal,
                () => el.IsUnderline = newVal
            );
        }
    }

    [RelayCommand]
    public void DeleteSelectedElement()
    {
        if (SelectedPage == null) return;
        var page = SelectedPage;

        if (page.SelectedElements.Count > 1)
        {
            var targets = page.SelectedElements.ToList();
            foreach (var el in targets) page.RemoveElement(el);
            UpdateSelection(null, page);

            UndoRedo?.RecordAction(
                $"Delete {targets.Count} Elements",
                () => {
                    foreach (var el in targets) page.AddElement(el);
                    page.SelectElements(targets);
                    UpdateSelection(page.SelectedElement, page);
                },
                () => {
                    foreach (var el in targets) page.RemoveElement(el);
                    UpdateSelection(null, page);
                }
            );
        }
        else if (SelectedElement != null)
        {
            var el = SelectedElement;
            page.RemoveElement(el);
            UpdateSelection(null, page);

            UndoRedo?.RecordAction(
                $"Delete {el.DisplayName}",
                () => { page.AddElement(el); UpdateSelection(el, page); },
                () => { page.RemoveElement(el); UpdateSelection(null, page); }
            );
        }
    }

    [RelayCommand]
    public void DuplicateSelectedElement()
    {
        if (SelectedPage == null) return;
        var page = SelectedPage;

        if (page.SelectedElements.Count > 1)
        {
            var targets = page.SelectedElements.ToList();
            var newElements = new List<ElementViewModelBase>();

            foreach (var el in targets)
            {
                var model = el.ToModel();
                var clone = model.Clone();
                clone.Id = Guid.NewGuid().ToString("N");
                clone.X += 20;
                clone.Y += 20;

                ElementViewModelBase newVm = clone.Kind switch
                {
                    ElementKind.Text => new TextElementViewModel(),
                    ElementKind.Heading => new TextElementViewModel(),
                    ElementKind.Shape => new ShapeElementViewModel(),
                    ElementKind.Image => new ImageElementViewModel(),
                    ElementKind.Divider => new DividerElementViewModel(),
                    ElementKind.Table => new TableElementViewModel(),
                    ElementKind.Chart => new ChartElementViewModel(),
                    ElementKind.Watermark => new WatermarkElementViewModel(),
                    ElementKind.FormField => new FormFieldElementViewModel(),
                    ElementKind.QrCode => new QrCodeElementViewModel(),
                    ElementKind.Barcode => new BarcodeElementViewModel(),
                    ElementKind.Redaction => new RedactionElementViewModel(),
                    ElementKind.Ink => new InkElementViewModel(),
                    ElementKind.StickyNote => new StickyNoteElementViewModel(),
                    ElementKind.Measurement => new MeasurementElementViewModel(),
                    _ => new TextElementViewModel()
                };

                newVm.LoadFromModel(clone);
                page.AddElement(newVm);
                newElements.Add(newVm);
            }

            page.SelectElements(newElements);
            UpdateSelection(page.SelectedElement, page);

            UndoRedo?.RecordAction(
                $"Duplicate {targets.Count} Elements",
                () => {
                    foreach (var el in newElements) page.RemoveElement(el);
                    page.SelectElements(targets);
                    UpdateSelection(page.SelectedElement, page);
                },
                () => {
                    foreach (var el in newElements) page.AddElement(el);
                    page.SelectElements(newElements);
                    UpdateSelection(page.SelectedElement, page);
                }
            );
        }
        else if (SelectedElement != null)
        {
            var model = SelectedElement.ToModel();
            var clone = model.Clone();
            clone.Id = Guid.NewGuid().ToString("N");
            clone.X += 20;
            clone.Y += 20;

            ElementViewModelBase newVm = clone.Kind switch
            {
                ElementKind.Text => new TextElementViewModel(),
                ElementKind.Heading => new TextElementViewModel(),
                ElementKind.Shape => new ShapeElementViewModel(),
                ElementKind.Image => new ImageElementViewModel(),
                ElementKind.Divider => new DividerElementViewModel(),
                ElementKind.Table => new TableElementViewModel(),
                ElementKind.Chart => new ChartElementViewModel(),
                ElementKind.Watermark => new WatermarkElementViewModel(),
                ElementKind.FormField => new FormFieldElementViewModel(),
                ElementKind.QrCode => new QrCodeElementViewModel(),
                ElementKind.Barcode => new BarcodeElementViewModel(),
                ElementKind.Redaction => new RedactionElementViewModel(),
                ElementKind.Ink => new InkElementViewModel(),
                ElementKind.StickyNote => new StickyNoteElementViewModel(),
                ElementKind.Measurement => new MeasurementElementViewModel(),
                _ => new TextElementViewModel()
            };

            newVm.LoadFromModel(clone);
            page.AddElement(newVm);
            UpdateSelection(newVm, page);

            UndoRedo?.RecordAction(
                $"Duplicate {newVm.DisplayName}",
                () => { page.RemoveElement(newVm); UpdateSelection(null, page); },
                () => { page.AddElement(newVm); UpdateSelection(newVm, page); }
            );
        }
    }

    [RelayCommand]
    public void BringToFront()
    {
        if (SelectedElement != null && SelectedPage != null)
        {
            var el = SelectedElement;
            var page = SelectedPage;
            int oldIdx = page.Elements.IndexOf(el);
            if (oldIdx < 0 || oldIdx == page.Elements.Count - 1) return;

            page.BringToFront(el);
            int newIdx = page.Elements.IndexOf(el);

            UndoRedo?.RecordAction(
                $"Bring {el.DisplayName} to Front",
                () => {
                    if (page.Elements.Contains(el))
                    {
                        int cur = page.Elements.IndexOf(el);
                        if (cur != oldIdx && oldIdx >= 0 && oldIdx < page.Elements.Count) page.Elements.Move(cur, oldIdx);
                        page.NormalizeZIndices();
                    }
                },
                () => {
                    if (page.Elements.Contains(el))
                    {
                        int cur = page.Elements.IndexOf(el);
                        if (cur != newIdx && newIdx >= 0 && newIdx < page.Elements.Count) page.Elements.Move(cur, newIdx);
                        page.NormalizeZIndices();
                    }
                }
            );
        }
    }

    [RelayCommand]
    public void SendToBack()
    {
        if (SelectedElement != null && SelectedPage != null)
        {
            var el = SelectedElement;
            var page = SelectedPage;
            int oldIdx = page.Elements.IndexOf(el);
            if (oldIdx <= 0) return;

            page.SendToBack(el);
            int newIdx = page.Elements.IndexOf(el);

            UndoRedo?.RecordAction(
                $"Send {el.DisplayName} to Back",
                () => {
                    if (page.Elements.Contains(el))
                    {
                        int cur = page.Elements.IndexOf(el);
                        if (cur != oldIdx && oldIdx >= 0 && oldIdx < page.Elements.Count) page.Elements.Move(cur, oldIdx);
                        page.NormalizeZIndices();
                    }
                },
                () => {
                    if (page.Elements.Contains(el))
                    {
                        int cur = page.Elements.IndexOf(el);
                        if (cur != newIdx && newIdx >= 0 && newIdx < page.Elements.Count) page.Elements.Move(cur, newIdx);
                        page.NormalizeZIndices();
                    }
                }
            );
        }
    }

    [RelayCommand]
    public void BringForward()
    {
        if (SelectedElement != null && SelectedPage != null)
        {
            var el = SelectedElement;
            var page = SelectedPage;
            int oldIdx = page.Elements.IndexOf(el);
            if (oldIdx < 0 || oldIdx >= page.Elements.Count - 1) return;

            page.BringForward(el);
            int newIdx = page.Elements.IndexOf(el);

            UndoRedo?.RecordAction(
                $"Bring {el.DisplayName} Forward",
                () => {
                    if (page.Elements.Contains(el))
                    {
                        int cur = page.Elements.IndexOf(el);
                        if (cur != oldIdx && oldIdx >= 0 && oldIdx < page.Elements.Count) page.Elements.Move(cur, oldIdx);
                        page.NormalizeZIndices();
                    }
                },
                () => {
                    if (page.Elements.Contains(el))
                    {
                        int cur = page.Elements.IndexOf(el);
                        if (cur != newIdx && newIdx >= 0 && newIdx < page.Elements.Count) page.Elements.Move(cur, newIdx);
                        page.NormalizeZIndices();
                    }
                }
            );
        }
    }

    [RelayCommand]
    public void SendBackward()
    {
        if (SelectedElement != null && SelectedPage != null)
        {
            var el = SelectedElement;
            var page = SelectedPage;
            int oldIdx = page.Elements.IndexOf(el);
            if (oldIdx <= 0) return;

            page.SendBackward(el);
            int newIdx = page.Elements.IndexOf(el);

            UndoRedo?.RecordAction(
                $"Send {el.DisplayName} Backward",
                () => {
                    if (page.Elements.Contains(el))
                    {
                        int cur = page.Elements.IndexOf(el);
                        if (cur != oldIdx && oldIdx >= 0 && oldIdx < page.Elements.Count) page.Elements.Move(cur, oldIdx);
                        page.NormalizeZIndices();
                    }
                },
                () => {
                    if (page.Elements.Contains(el))
                    {
                        int cur = page.Elements.IndexOf(el);
                        if (cur != newIdx && newIdx >= 0 && newIdx < page.Elements.Count) page.Elements.Move(cur, newIdx);
                        page.NormalizeZIndices();
                    }
                }
            );
        }
    }

    [RelayCommand]
    public void SetShapeType(string shapeTypeStr)
    {
        if (ShapeElement != null && Enum.TryParse<ShapeType>(shapeTypeStr, true, out var type) && ShapeElement.ShapeType != type)
        {
            var el = ShapeElement;
            var oldType = el.ShapeType;
            double oldRadius = el.CornerRadius;
            double newRadius = type == ShapeType.Circle ? el.Width / 2 : (type == ShapeType.RoundedRectangle ? 12 : 0);

            el.ShapeType = type;
            el.CornerRadius = newRadius;

            UndoRedo?.RecordAction(
                $"Shape: {type}",
                () => { el.ShapeType = oldType; el.CornerRadius = oldRadius; },
                () => { el.ShapeType = type; el.CornerRadius = newRadius; }
            );
        }
    }

    [RelayCommand]
    public void SetWatermarkColor(string hex)
    {
        if (WatermarkElement != null && WatermarkElement.ColorHex != hex)
        {
            var el = WatermarkElement;
            string oldHex = el.ColorHex;
            el.ColorHex = hex;
            UndoRedo?.RecordAction(
                "Change Watermark Color",
                () => el.ColorHex = oldHex,
                () => el.ColorHex = hex
            );
        }
    }

    [RelayCommand]
    public void AlignLeft()
    {
        if (SelectedPage == null) return;
        var page = SelectedPage;

        if (page.SelectedElements.Count > 1)
        {
            var targets = page.SelectedElements.ToList();
            double minX = targets.Min(e => e.X);
            var prevPositions = targets.Select(e => (Element: e, e.X, e.Y)).ToList();

            foreach (var el in targets) el.X = minX;
            page.UpdateSelectionBoundingBox();

            UndoRedo?.RecordAction(
                "Align Left",
                () => { foreach (var item in prevPositions) item.Element.X = item.X; page.UpdateSelectionBoundingBox(); },
                () => { foreach (var el in targets) el.X = minX; page.UpdateSelectionBoundingBox(); }
            );
        }
        else if (SelectedElement != null)
        {
            var el = SelectedElement;
            double oldX = el.X;
            double newX = 60.0;
            el.X = newX;
            page.UpdateSelectionBoundingBox();

            UndoRedo?.RecordAction(
                "Align Left to Margin",
                () => { el.X = oldX; page.UpdateSelectionBoundingBox(); },
                () => { el.X = newX; page.UpdateSelectionBoundingBox(); }
            );
        }
    }

    [RelayCommand]
    public void AlignCenter()
    {
        if (SelectedPage == null) return;
        var page = SelectedPage;

        if (page.SelectedElements.Count > 1)
        {
            var targets = page.SelectedElements.ToList();
            double minX = targets.Min(e => e.X);
            double maxX = targets.Max(e => e.X + Math.Max(1, e.Width));
            double centerX = (minX + maxX) / 2.0;
            var prevPositions = targets.Select(e => (Element: e, e.X, e.Y)).ToList();

            foreach (var el in targets) el.X = centerX - (el.Width / 2.0);
            page.UpdateSelectionBoundingBox();

            UndoRedo?.RecordAction(
                "Align Center",
                () => { foreach (var item in prevPositions) item.Element.X = item.X; page.UpdateSelectionBoundingBox(); },
                () => { foreach (var el in targets) el.X = centerX - (el.Width / 2.0); page.UpdateSelectionBoundingBox(); }
            );
        }
        else if (SelectedElement != null)
        {
            var el = SelectedElement;
            double oldX = el.X;
            double newX = Math.Max(0, (page.Width - el.Width) / 2.0);
            el.X = newX;
            page.UpdateSelectionBoundingBox();

            UndoRedo?.RecordAction(
                "Align Center on Page",
                () => { el.X = oldX; page.UpdateSelectionBoundingBox(); },
                () => { el.X = newX; page.UpdateSelectionBoundingBox(); }
            );
        }
    }

    [RelayCommand]
    public void AlignRight()
    {
        if (SelectedPage == null) return;
        var page = SelectedPage;

        if (page.SelectedElements.Count > 1)
        {
            var targets = page.SelectedElements.ToList();
            double maxX = targets.Max(e => e.X + Math.Max(1, e.Width));
            var prevPositions = targets.Select(e => (Element: e, e.X, e.Y)).ToList();

            foreach (var el in targets) el.X = maxX - el.Width;
            page.UpdateSelectionBoundingBox();

            UndoRedo?.RecordAction(
                "Align Right",
                () => { foreach (var item in prevPositions) item.Element.X = item.X; page.UpdateSelectionBoundingBox(); },
                () => { foreach (var el in targets) el.X = maxX - el.Width; page.UpdateSelectionBoundingBox(); }
            );
        }
        else if (SelectedElement != null)
        {
            var el = SelectedElement;
            double oldX = el.X;
            double newX = Math.Max(0, page.Width - el.Width - 60.0);
            el.X = newX;
            page.UpdateSelectionBoundingBox();

            UndoRedo?.RecordAction(
                "Align Right to Margin",
                () => { el.X = oldX; page.UpdateSelectionBoundingBox(); },
                () => { el.X = newX; page.UpdateSelectionBoundingBox(); }
            );
        }
    }

    [RelayCommand]
    public void AlignTop()
    {
        if (SelectedPage == null) return;
        var page = SelectedPage;

        if (page.SelectedElements.Count > 1)
        {
            var targets = page.SelectedElements.ToList();
            double minY = targets.Min(e => e.Y);
            var prevPositions = targets.Select(e => (Element: e, e.X, e.Y)).ToList();

            foreach (var el in targets) el.Y = minY;
            page.UpdateSelectionBoundingBox();

            UndoRedo?.RecordAction(
                "Align Top",
                () => { foreach (var item in prevPositions) item.Element.Y = item.Y; page.UpdateSelectionBoundingBox(); },
                () => { foreach (var el in targets) el.Y = minY; page.UpdateSelectionBoundingBox(); }
            );
        }
        else if (SelectedElement != null)
        {
            var el = SelectedElement;
            double oldY = el.Y;
            double newY = 60.0;
            el.Y = newY;
            page.UpdateSelectionBoundingBox();

            UndoRedo?.RecordAction(
                "Align Top to Margin",
                () => { el.Y = oldY; page.UpdateSelectionBoundingBox(); },
                () => { el.Y = newY; page.UpdateSelectionBoundingBox(); }
            );
        }
    }

    [RelayCommand]
    public void AlignMiddle()
    {
        if (SelectedPage == null) return;
        var page = SelectedPage;

        if (page.SelectedElements.Count > 1)
        {
            var targets = page.SelectedElements.ToList();
            double minY = targets.Min(e => e.Y);
            double maxY = targets.Max(e => e.Y + Math.Max(1, e.Height));
            double centerY = (minY + maxY) / 2.0;
            var prevPositions = targets.Select(e => (Element: e, e.X, e.Y)).ToList();

            foreach (var el in targets) el.Y = centerY - (el.Height / 2.0);
            page.UpdateSelectionBoundingBox();

            UndoRedo?.RecordAction(
                "Align Middle",
                () => { foreach (var item in prevPositions) item.Element.Y = item.Y; page.UpdateSelectionBoundingBox(); },
                () => { foreach (var el in targets) el.Y = centerY - (el.Height / 2.0); page.UpdateSelectionBoundingBox(); }
            );
        }
        else if (SelectedElement != null)
        {
            var el = SelectedElement;
            double oldY = el.Y;
            double newY = Math.Max(0, (page.Height - el.Height) / 2.0);
            el.Y = newY;
            page.UpdateSelectionBoundingBox();

            UndoRedo?.RecordAction(
                "Align Middle on Page",
                () => { el.Y = oldY; page.UpdateSelectionBoundingBox(); },
                () => { el.Y = newY; page.UpdateSelectionBoundingBox(); }
            );
        }
    }

    [RelayCommand]
    public void AlignBottom()
    {
        if (SelectedPage == null) return;
        var page = SelectedPage;

        if (page.SelectedElements.Count > 1)
        {
            var targets = page.SelectedElements.ToList();
            double maxY = targets.Max(e => e.Y + Math.Max(1, e.Height));
            var prevPositions = targets.Select(e => (Element: e, e.X, e.Y)).ToList();

            foreach (var el in targets) el.Y = maxY - el.Height;
            page.UpdateSelectionBoundingBox();

            UndoRedo?.RecordAction(
                "Align Bottom",
                () => { foreach (var item in prevPositions) item.Element.Y = item.Y; page.UpdateSelectionBoundingBox(); },
                () => { foreach (var el in targets) el.Y = maxY - el.Height; page.UpdateSelectionBoundingBox(); }
            );
        }
        else if (SelectedElement != null)
        {
            var el = SelectedElement;
            double oldY = el.Y;
            double newY = Math.Max(0, page.Height - el.Height - 60.0);
            el.Y = newY;
            page.UpdateSelectionBoundingBox();

            UndoRedo?.RecordAction(
                "Align Bottom to Margin",
                () => { el.Y = oldY; page.UpdateSelectionBoundingBox(); },
                () => { el.Y = newY; page.UpdateSelectionBoundingBox(); }
            );
        }
    }

    [RelayCommand]
    public void DistributeHorizontally()
    {
        if (SelectedPage == null) return;
        var page = SelectedPage;

        var targets = page.SelectedElements.Count >= 3 ? page.SelectedElements.ToList() : page.Elements.ToList();
        if (targets.Count < 3) return;

        var sorted = targets.OrderBy(e => e.X).ToList();
        double firstX = sorted.First().X;
        double lastRight = sorted.Last().X + sorted.Last().Width;
        double totalElementsWidth = sorted.Sum(e => e.Width);
        double totalSpan = lastRight - firstX;
        double totalGaps = totalSpan - totalElementsWidth;
        double gap = Math.Max(0, totalGaps / (sorted.Count - 1));

        var prevPositions = sorted.Select(e => (Element: e, e.X, e.Y)).ToList();
        double curX = firstX;
        var newPositions = new List<(ElementViewModelBase Element, double X)>();

        for (int i = 0; i < sorted.Count; i++)
        {
            var el = sorted[i];
            if (i == 0)
            {
                newPositions.Add((el, firstX));
                curX += el.Width + gap;
            }
            else if (i == sorted.Count - 1)
            {
                newPositions.Add((el, lastRight - el.Width));
            }
            else
            {
                newPositions.Add((el, curX));
                curX += el.Width + gap;
            }
        }

        foreach (var p in newPositions) p.Element.X = p.X;
        page.UpdateSelectionBoundingBox();

        UndoRedo?.RecordAction(
            "Distribute Horizontally",
            () => { foreach (var p in prevPositions) p.Element.X = p.X; page.UpdateSelectionBoundingBox(); },
            () => { foreach (var p in newPositions) p.Element.X = p.X; page.UpdateSelectionBoundingBox(); }
        );
    }

    [RelayCommand]
    public void DistributeVertically()
    {
        if (SelectedPage == null) return;
        var page = SelectedPage;

        var targets = page.SelectedElements.Count >= 3 ? page.SelectedElements.ToList() : page.Elements.ToList();
        if (targets.Count < 3) return;

        var sorted = targets.OrderBy(e => e.Y).ToList();
        double firstY = sorted.First().Y;
        double lastBottom = sorted.Last().Y + sorted.Last().Height;
        double totalElementsHeight = sorted.Sum(e => e.Height);
        double totalSpan = lastBottom - firstY;
        double totalGaps = totalSpan - totalElementsHeight;
        double gap = Math.Max(0, totalGaps / (sorted.Count - 1));

        var prevPositions = sorted.Select(e => (Element: e, e.X, e.Y)).ToList();
        double curY = firstY;
        var newPositions = new List<(ElementViewModelBase Element, double Y)>();

        for (int i = 0; i < sorted.Count; i++)
        {
            var el = sorted[i];
            if (i == 0)
            {
                newPositions.Add((el, firstY));
                curY += el.Height + gap;
            }
            else if (i == sorted.Count - 1)
            {
                newPositions.Add((el, lastBottom - el.Height));
            }
            else
            {
                newPositions.Add((el, curY));
                curY += el.Height + gap;
            }
        }

        foreach (var p in newPositions) p.Element.Y = p.Y;
        page.UpdateSelectionBoundingBox();

        UndoRedo?.RecordAction(
            "Distribute Vertically",
            () => { foreach (var p in prevPositions) p.Element.Y = p.Y; page.UpdateSelectionBoundingBox(); },
            () => { foreach (var p in newPositions) p.Element.Y = p.Y; page.UpdateSelectionBoundingBox(); }
        );
    }

    [RelayCommand]
    public void ToggleLock()
    {
        if (SelectedPage == null) return;
        var page = SelectedPage;

        if (page.SelectedElements.Count > 1)
        {
            var targets = page.SelectedElements.ToList();
            bool allLocked = targets.All(e => e.IsLocked);
            bool targetLock = !allLocked;
            var prevStates = targets.Select(e => (Element: e, e.IsLocked)).ToList();

            foreach (var el in targets) el.IsLocked = targetLock;

            UndoRedo?.RecordAction(
                targetLock ? $"Lock {targets.Count} Elements" : $"Unlock {targets.Count} Elements",
                () => { foreach (var item in prevStates) item.Element.IsLocked = item.IsLocked; },
                () => { foreach (var el in targets) el.IsLocked = targetLock; }
            );
        }
        else if (SelectedElement != null)
        {
            var el = SelectedElement;
            bool oldLock = el.IsLocked;
            bool newLock = !oldLock;
            el.IsLocked = newLock;
            UndoRedo?.RecordAction(
                newLock ? "Lock Element" : "Unlock Element",
                () => el.IsLocked = oldLock,
                () => el.IsLocked = newLock
            );
        }
    }

    [RelayCommand]
    public void ApplyTableStyle(string styleStr)
    {
        if (TableElement != null)
        {
            var el = TableElement;
            string oldHeader = el.HeaderBackgroundHex;
            string oldAlt = el.AlternateRowBackgroundHex;
            string oldBorder = el.BorderColorHex;

            el.ApplyPresetStyle(styleStr);

            string newHeader = el.HeaderBackgroundHex;
            string newAlt = el.AlternateRowBackgroundHex;
            string newBorder = el.BorderColorHex;

            UndoRedo?.RecordAction(
                $"Apply Table Style ({styleStr})",
                () => { el.HeaderBackgroundHex = oldHeader; el.AlternateRowBackgroundHex = oldAlt; el.BorderColorHex = oldBorder; },
                () => { el.HeaderBackgroundHex = newHeader; el.AlternateRowBackgroundHex = newAlt; el.BorderColorHex = newBorder; }
            );
        }
    }

    [RelayCommand]
    public void SetChartType(string typeStr)
    {
        if (ChartElement != null && Enum.TryParse<ChartType>(typeStr, true, out var parsed) && ChartElement.ChartType != parsed)
        {
            var el = ChartElement;
            var oldType = el.ChartType;
            el.SetChartType(typeStr);
            UndoRedo?.RecordAction(
                $"Change Chart to {parsed}",
                () => el.ChartType = oldType,
                () => el.ChartType = parsed
            );
        }
    }

    [RelayCommand]
    public void SetBarcodeFormat(string formatStr)
    {
        if (BarcodeElement != null && BarcodeElement.BarcodeFormat != formatStr)
        {
            var el = BarcodeElement;
            string oldFmt = el.BarcodeFormat;
            el.SetFormat(formatStr);
            UndoRedo?.RecordAction(
                $"Barcode Format: {formatStr}",
                () => el.BarcodeFormat = oldFmt,
                () => el.BarcodeFormat = formatStr
            );
        }
    }

    [RelayCommand]
    public void ApplyQrPreset(string presetStr)
    {
        if (QrCodeElement != null)
        {
            var el = QrCodeElement;
            string oldContent = el.Content;
            string oldLabel = el.Label;
            el.ApplyPresetType(presetStr);
            string newContent = el.Content;
            string newLabel = el.Label;

            UndoRedo?.RecordAction(
                $"QR Preset: {presetStr}",
                () => { el.Content = oldContent; el.Label = oldLabel; },
                () => { el.Content = newContent; el.Label = newLabel; }
            );
        }
    }

    // --- TYPOGRAPHY EXTENSIONS ---

    [RelayCommand]
    public void ToggleStrikethrough()
    {
        if (TextElement != null)
        {
            var el = TextElement;
            bool oldVal = el.IsStrikethrough;
            bool newVal = !oldVal;
            el.IsStrikethrough = newVal;
            UndoRedo?.RecordAction(
                newVal ? "Format Strikethrough" : "Remove Strikethrough",
                () => el.IsStrikethrough = oldVal,
                () => el.IsStrikethrough = newVal
            );
        }
    }

    [RelayCommand]
    public void TransformTextCase(string caseType)
    {
        if (TextElement != null)
        {
            var el = TextElement;
            string oldText = el.Text;
            if (caseType.Equals("Upper", StringComparison.OrdinalIgnoreCase)) el.TransformUppercase();
            else if (caseType.Equals("Lower", StringComparison.OrdinalIgnoreCase)) el.TransformLowercase();
            else if (caseType.Equals("Title", StringComparison.OrdinalIgnoreCase)) el.TransformTitleCase();
            string newText = el.Text;

            UndoRedo?.RecordAction(
                $"Case: {caseType}",
                () => el.Text = oldText,
                () => el.Text = newText
            );
        }
    }

    [RelayCommand]
    public void ToggleBullets()
    {
        if (TextElement != null)
        {
            var el = TextElement;
            string oldText = el.Text;
            el.ToggleBulletList();
            string newText = el.Text;

            UndoRedo?.RecordAction(
                "Toggle Bullet List",
                () => el.Text = oldText,
                () => el.Text = newText
            );
        }
    }

    [RelayCommand]
    public void ToggleNumbering()
    {
        if (TextElement != null)
        {
            var el = TextElement;
            string oldText = el.Text;
            el.ToggleNumberedList();
            string newText = el.Text;

            UndoRedo?.RecordAction(
                "Toggle Numbered List",
                () => el.Text = oldText,
                () => el.Text = newText
            );
        }
    }

    [RelayCommand]
    public void SetTextBackground(string hex)
    {
        if (TextElement != null)
        {
            var el = TextElement;
            string oldHex = el.BackgroundColorHex;
            el.BackgroundColorHex = hex;
            UndoRedo?.RecordAction(
                "Text Background Color",
                () => el.BackgroundColorHex = oldHex,
                () => el.BackgroundColorHex = hex
            );
        }
    }

    // --- FORM FIELD VALIDATION ---

    [RelayCommand]
    public void SetFormFieldValidation(string valTypeStr)
    {
        if (FormFieldElement != null && Enum.TryParse<FormValidationType>(valTypeStr, true, out var valType))
        {
            var el = FormFieldElement;
            var oldType = el.ValidationType;
            el.ValidationType = valType;
            UndoRedo?.RecordAction(
                $"Field Validation: {valType}",
                () => el.ValidationType = oldType,
                () => el.ValidationType = valType
            );
        }
    }

    // --- ADDITIONAL ELEMENT COLOR & PROPERTY COMMANDS ---

    [RelayCommand]
    public void SetDividerColor(string hex)
    {
        if (DividerElement != null)
        {
            var el = DividerElement;
            string oldHex = el.ColorHex;
            el.ColorHex = hex;
            UndoRedo?.RecordAction("Change Divider Color", () => el.ColorHex = oldHex, () => el.ColorHex = hex);
        }
    }

    [RelayCommand]
    public void SetInkColor(string hex)
    {
        if (InkElement != null)
        {
            var el = InkElement;
            string oldHex = el.StrokeColorHex;
            el.StrokeColorHex = hex;
            UndoRedo?.RecordAction("Change Ink Color", () => el.StrokeColorHex = oldHex, () => el.StrokeColorHex = hex);
        }
    }

    [RelayCommand]
    public void SetStickyNoteColor(string hex)
    {
        if (StickyNoteElement != null)
        {
            var el = StickyNoteElement;
            string oldHex = el.ColorHex;
            el.ColorHex = hex;
            UndoRedo?.RecordAction("Change Note Color", () => el.ColorHex = oldHex, () => el.ColorHex = hex);
        }
    }

    [RelayCommand]
    public void SetRedactionFillColor(string hex)
    {
        if (RedactionElement != null)
        {
            var el = RedactionElement;
            string oldHex = el.FillColorHex;
            el.FillColorHex = hex;
            UndoRedo?.RecordAction("Change Redaction Fill", () => el.FillColorHex = oldHex, () => el.FillColorHex = hex);
        }
    }

    [RelayCommand]
    public void SetImageBorderColor(string hex)
    {
        if (ImageElement != null)
        {
            var el = ImageElement;
            string oldHex = el.BorderColorHex;
            el.BorderColorHex = hex;
            UndoRedo?.RecordAction("Change Image Border Color", () => el.BorderColorHex = oldHex, () => el.BorderColorHex = hex);
        }
    }

    [RelayCommand]
    public void SetFormFieldBackgroundColor(string hex)
    {
        if (FormFieldElement != null)
        {
            var el = FormFieldElement;
            string oldHex = el.BackgroundColorHex;
            el.BackgroundColorHex = hex;
            UndoRedo?.RecordAction("Change Field Background", () => el.BackgroundColorHex = oldHex, () => el.BackgroundColorHex = hex);
        }
    }

    [RelayCommand]
    public void SetFormFieldBorderColor(string hex)
    {
        if (FormFieldElement != null)
        {
            var el = FormFieldElement;
            string oldHex = el.BorderColorHex;
            el.BorderColorHex = hex;
            UndoRedo?.RecordAction("Change Field Border", () => el.BorderColorHex = oldHex, () => el.BorderColorHex = hex);
        }
    }

    [RelayCommand]
    public void SetFormFieldType(string typeStr)
    {
        if (FormFieldElement != null && Enum.TryParse<FormFieldType>(typeStr, true, out var type))
        {
            var el = FormFieldElement;
            var oldType = el.FieldType;
            el.FieldType = type;
            UndoRedo?.RecordAction($"Change Field to {type}", () => el.FieldType = oldType, () => el.FieldType = type);
        }
    }

    [RelayCommand]
    public void SetShapeLabelColor(string hex)
    {
        if (ShapeElement != null)
        {
            var el = ShapeElement;
            string? oldHex = el.LabelColorHex;
            el.LabelColorHex = hex;
            UndoRedo?.RecordAction("Change Shape Label Color", () => el.LabelColorHex = oldHex, () => el.LabelColorHex = hex);
        }
    }

    [RelayCommand]
    public void SetQrCodeDarkColor(string hex)
    {
        if (QrCodeElement != null)
        {
            var el = QrCodeElement;
            string oldHex = el.DarkColorHex;
            el.DarkColorHex = hex;
            UndoRedo?.RecordAction("Change QR Color", () => el.DarkColorHex = oldHex, () => el.DarkColorHex = hex);
        }
    }

    [RelayCommand]
    public void SetQrCodeLightColor(string hex)
    {
        if (QrCodeElement != null)
        {
            var el = QrCodeElement;
            string oldHex = el.LightColorHex;
            el.LightColorHex = hex;
            UndoRedo?.RecordAction("Change QR Background", () => el.LightColorHex = oldHex, () => el.LightColorHex = hex);
        }
    }

    [RelayCommand]
    public void SetQrCodeEccLevel(string eccStr)
    {
        if (QrCodeElement != null && Enum.TryParse<QrCodeEccLevel>(eccStr, true, out var ecc))
        {
            var el = QrCodeElement;
            var oldEcc = el.EccLevel;
            el.EccLevel = ecc;
            UndoRedo?.RecordAction($"Change QR ECC: {ecc}", () => el.EccLevel = oldEcc, () => el.EccLevel = ecc);
        }
    }

    [RelayCommand]
    public void SetBarcodeColor(string hex)
    {
        if (BarcodeElement != null)
        {
            var el = BarcodeElement;
            string oldHex = el.BarColorHex;
            el.BarColorHex = hex;
            UndoRedo?.RecordAction("Change Barcode Color", () => el.BarColorHex = oldHex, () => el.BarColorHex = hex);
        }
    }

    [RelayCommand]
    public void SetFormFieldCalculation(string calcStr)
    {
        if (FormFieldElement != null && Enum.TryParse<CalculationFormula>(calcStr, true, out var calc))
        {
            var el = FormFieldElement;
            var oldCalc = el.CalculationFormula;
            el.CalculationFormula = calc;
            SelectedPage?.RecalculateFormFields();
            UndoRedo?.RecordAction($"Set Formula to {calc}", () => { el.CalculationFormula = oldCalc; SelectedPage?.RecalculateFormFields(); }, () => { el.CalculationFormula = calc; SelectedPage?.RecalculateFormFields(); });
        }
    }

    [RelayCommand]
    public void SetMeasurementColor(string hex)
    {
        if (MeasurementElement != null)
        {
            var el = MeasurementElement;
            string oldHex = el.StrokeColorHex;
            el.StrokeColorHex = hex;
            UndoRedo?.RecordAction("Change Measurement Color", () => el.StrokeColorHex = oldHex, () => el.StrokeColorHex = hex);
        }
    }

    // --- DOCUMENT & PAGE LEVEL INSPECTOR COMMANDS ---

    [RelayCommand]
    public void RotatePageClockwise()
    {
        if (SelectedPage != null)
        {
            var page = SelectedPage;
            int oldAngle = page.RotationAngle;
            int newAngle = (oldAngle + 90) % 360;
            page.RotationAngle = newAngle;
            UndoRedo?.RecordAction("Rotate Page 90° CW", () => page.RotationAngle = oldAngle, () => page.RotationAngle = newAngle);
        }
    }

    [RelayCommand]
    public void RotatePageCounterClockwise()
    {
        if (SelectedPage != null)
        {
            var page = SelectedPage;
            int oldAngle = page.RotationAngle;
            int newAngle = (oldAngle + 270) % 360;
            page.RotationAngle = newAngle;
            UndoRedo?.RecordAction("Rotate Page 90° CCW", () => page.RotationAngle = oldAngle, () => page.RotationAngle = newAngle);
        }
    }

    [RelayCommand]
    public void SetPageFormat(string formatStr)
    {
        if (SelectedPage != null && Enum.TryParse<PageFormat>(formatStr, true, out var fmt))
        {
            var page = SelectedPage;
            var oldFmt = page.Format;
            double oldW = page.Width;
            double oldH = page.Height;

            (double w, double h) = fmt switch
            {
                PageFormat.A4 => (800.0, 1131.0),
                PageFormat.Letter => (816.0, 1056.0),
                PageFormat.Legal => (816.0, 1344.0),
                PageFormat.A3 => (1131.0, 1600.0),
                PageFormat.A5 => (565.0, 800.0),
                PageFormat.Tabloid => (1056.0, 1632.0),
                _ => (800.0, 1131.0)
            };

            if (page.Orientation == PageOrientation.Landscape)
            {
                (w, h) = (h, w);
            }

            page.Format = fmt;
            page.Width = w;
            page.Height = h;

            UndoRedo?.RecordAction(
                $"Page Format: {fmt}",
                () => { page.Format = oldFmt; page.Width = oldW; page.Height = oldH; },
                () => { page.Format = fmt; page.Width = w; page.Height = h; }
            );
        }
    }

    [RelayCommand]
    public void SetPageOrientation(string orientStr)
    {
        if (SelectedPage != null && Enum.TryParse<PageOrientation>(orientStr, true, out var orient))
        {
            var page = SelectedPage;
            if (page.Orientation == orient) return;

            var oldOrient = page.Orientation;
            double oldW = page.Width;
            double oldH = page.Height;

            double newW = oldH;
            double newH = oldW;

            page.Orientation = orient;
            page.Width = newW;
            page.Height = newH;

            UndoRedo?.RecordAction(
                $"Orientation: {orient}",
                () => { page.Orientation = oldOrient; page.Width = oldW; page.Height = oldH; },
                () => { page.Orientation = orient; page.Width = newW; page.Height = newH; }
            );
        }
    }

    [RelayCommand]
    public void SetPageBackgroundColor(string hex)
    {
        if (SelectedPage != null && SelectedPage.BackgroundColorHex != hex)
        {
            var page = SelectedPage;
            string oldHex = page.BackgroundColorHex;
            page.BackgroundColorHex = hex;
            UndoRedo?.RecordAction("Change Page Background Color", () => page.BackgroundColorHex = oldHex, () => page.BackgroundColorHex = hex);
        }
    }

    [RelayCommand]
    public void ApplyHeaderFooterPreset(string preset)
    {
        if (SelectedPage != null)
        {
            var page = SelectedPage;
            page.ShowHeaderFooter = true;
            if (preset == "Standard")
            {
                page.FooterLeft = "CONFIDENTIAL & PROPRIETARY";
                page.FooterRight = $"Page {page.PageNumber}";
            }
            else if (preset == "Minimal")
            {
                page.FooterLeft = "";
                page.FooterRight = $"{page.PageNumber}";
            }
            else if (preset == "Corporate")
            {
                page.HeaderLeft = "ACME CORPORATION";
                page.HeaderRight = DateTime.Now.ToString("yyyy-MM-dd");
                page.FooterLeft = "INTERNAL USE ONLY";
                page.FooterRight = $"Page {page.PageNumber}";
            }
        }
    }
}
