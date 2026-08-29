using System;
using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using PdfEditorApp.Models;
using PdfEditorApp.ViewModels.ElementViewModels;

namespace PdfEditorApp.ViewModels;

public class ColorSwatchItem
{
    public string Name { get; set; } = "Black";
    public string Hex { get; set; } = "#201F1E";
}

public partial class InspectorViewModel : ViewModelBase
{
    [ObservableProperty]
    private ElementViewModelBase? _selectedElement;

    [ObservableProperty]
    private PageViewModel? _selectedPage;

    [ObservableProperty]
    private bool _hasSelectedElement;

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

    public ObservableCollection<string> AvailableFontFamilies { get; } = new()
    {
        "Segoe UI",
        "Arial",
        "Times New Roman",
        "Georgia",
        "Roboto",
        "Helvetica",
        "Courier New",
        "Verdana",
        "Trebuchet MS",
        "Consolas"
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

    public string ActiveCategoryName => SelectedElement != null ? SelectedElement.Kind.ToString() : "Document";
    public ObservableCollection<ColorSwatchItem> ColorSwatches => Swatches;

    public void UpdateSelection(ElementViewModelBase? element, PageViewModel? page)
    {
        SelectedElement = element;
        SelectedPage = page;

        HasSelectedElement = element != null;
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

        if (element is TextElementViewModel textVm)
        {
            if (!AvailableFontSizes.Contains(textVm.FontSize))
            {
                AvailableFontSizes.Add(textVm.FontSize);
                var sorted = new ObservableCollection<double>(AvailableFontSizes);
                // keep list ordered
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
        OnPropertyChanged(nameof(ActiveCategoryName));
    }

    [RelayCommand]
    public void SetTextColor(string hex)
    {
        if (TextElement != null)
        {
            TextElement.TextColorHex = hex;
        }
    }

    [RelayCommand]
    public void SetShapeFillColor(string hex)
    {
        if (ShapeElement != null)
        {
            ShapeElement.FillColorHex = hex;
        }
    }

    [RelayCommand]
    public void SetShapeStrokeColor(string hex)
    {
        if (ShapeElement != null)
        {
            ShapeElement.StrokeColorHex = hex;
        }
    }

    [RelayCommand]
    public void SetAlignment(string alignmentStr)
    {
        if (TextElement != null && Enum.TryParse<TextAlignmentMode>(alignmentStr, true, out var mode))
        {
            TextElement.Alignment = mode;
        }
    }

    [RelayCommand]
    public void ToggleBold()
    {
        if (TextElement != null)
        {
            TextElement.IsBold = !TextElement.IsBold;
        }
    }

    [RelayCommand]
    public void ToggleItalic()
    {
        if (TextElement != null)
        {
            TextElement.IsItalic = !TextElement.IsItalic;
        }
    }

    [RelayCommand]
    public void ToggleUnderline()
    {
        if (TextElement != null)
        {
            TextElement.IsUnderline = !TextElement.IsUnderline;
        }
    }

    [RelayCommand]
    public void DeleteSelectedElement()
    {
        if (SelectedElement != null && SelectedPage != null)
        {
            SelectedPage.RemoveElement(SelectedElement);
            UpdateSelection(null, SelectedPage);
        }
    }

    [RelayCommand]
    public void DuplicateSelectedElement()
    {
        if (SelectedElement != null && SelectedPage != null)
        {
            var model = SelectedElement.ToModel();
            var clone = model.Clone();
            clone.Id = Guid.NewGuid().ToString();
            clone.X += 20;
            clone.Y += 20;

            ElementViewModelBase? newVm = clone.Kind switch
            {
                ElementKind.Text => new TextElementViewModel(),
                ElementKind.Heading => new TextElementViewModel(),
                ElementKind.Shape => new ShapeElementViewModel(),
                ElementKind.Image => new ImageElementViewModel(),
                ElementKind.Divider => new DividerElementViewModel(),
                ElementKind.Table => new TableElementViewModel(),
                ElementKind.Chart => new ChartElementViewModel(),
                ElementKind.Watermark => new WatermarkElementViewModel(),
                _ => new TextElementViewModel()
            };

            newVm.LoadFromModel(clone);
            SelectedPage.AddElement(newVm);
            UpdateSelection(newVm, SelectedPage);
        }
    }

    [RelayCommand]
    public void BringToFront()
    {
        if (SelectedElement != null && SelectedPage != null)
        {
            SelectedPage.BringToFront(SelectedElement);
        }
    }

    [RelayCommand]
    public void SendToBack()
    {
        if (SelectedElement != null && SelectedPage != null)
        {
            SelectedPage.SendToBack(SelectedElement);
        }
    }

    [RelayCommand]
    public void SetShapeType(string shapeTypeStr)
    {
        if (ShapeElement != null && Enum.TryParse<ShapeType>(shapeTypeStr, true, out var type))
        {
            ShapeElement.ShapeType = type;
            if (type == ShapeType.Circle)
            {
                ShapeElement.CornerRadius = ShapeElement.Width / 2;
            }
            else if (type == ShapeType.RoundedRectangle)
            {
                ShapeElement.CornerRadius = 12;
            }
            else
            {
                ShapeElement.CornerRadius = 0;
            }
        }
    }

    [RelayCommand]
    public void SetWatermarkColor(string hex)
    {
        if (WatermarkElement != null)
        {
            WatermarkElement.ColorHex = hex;
        }
    }

    [RelayCommand]
    public void AlignLeft()
    {
        if (SelectedElement != null)
        {
            SelectedElement.X = 60;
        }
    }

    [RelayCommand]
    public void AlignCenter()
    {
        if (SelectedElement != null && SelectedPage != null)
        {
            SelectedElement.X = Math.Max(0, (SelectedPage.Width - SelectedElement.Width) / 2);
        }
    }

    [RelayCommand]
    public void AlignRight()
    {
        if (SelectedElement != null && SelectedPage != null)
        {
            SelectedElement.X = Math.Max(0, SelectedPage.Width - SelectedElement.Width - 60);
        }
    }

    [RelayCommand]
    public void AlignTop()
    {
        if (SelectedElement != null)
        {
            SelectedElement.Y = 60;
        }
    }

    [RelayCommand]
    public void AlignMiddle()
    {
        if (SelectedElement != null && SelectedPage != null)
        {
            SelectedElement.Y = Math.Max(0, (SelectedPage.Height - SelectedElement.Height) / 2);
        }
    }

    [RelayCommand]
    public void AlignBottom()
    {
        if (SelectedElement != null && SelectedPage != null)
        {
            SelectedElement.Y = Math.Max(0, SelectedPage.Height - SelectedElement.Height - 60);
        }
    }

    [RelayCommand]
    public void ToggleLock()
    {
        if (SelectedElement != null)
        {
            SelectedElement.IsLocked = !SelectedElement.IsLocked;
        }
    }

    [RelayCommand]
    public void ApplyTableStyle(string styleStr)
    {
        TableElement?.ApplyPresetStyle(styleStr);
    }

    [RelayCommand]
    public void SetChartType(string typeStr)
    {
        ChartElement?.SetChartType(typeStr);
    }

    [RelayCommand]
    public void SetBarcodeFormat(string formatStr)
    {
        BarcodeElement?.SetFormat(formatStr);
    }

    [RelayCommand]
    public void ApplyQrPreset(string presetStr)
    {
        QrCodeElement?.ApplyPresetType(presetStr);
    }
}
