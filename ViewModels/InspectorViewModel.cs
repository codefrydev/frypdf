using System;
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
        if (SelectedElement != null && SelectedPage != null)
        {
            var el = SelectedElement;
            var page = SelectedPage;
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
                ElementKind.FormField => new FormFieldElementViewModel(),
                ElementKind.QrCode => new QrCodeElementViewModel(),
                ElementKind.Barcode => new BarcodeElementViewModel(),
                ElementKind.Redaction => new RedactionElementViewModel(),
                ElementKind.Ink => new InkElementViewModel(),
                ElementKind.StickyNote => new StickyNoteElementViewModel(),
                _ => new TextElementViewModel()
            };

            newVm.LoadFromModel(clone);
            var page = SelectedPage;
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
        if (SelectedElement != null)
        {
            var el = SelectedElement;
            double oldX = el.X;
            el.X = 60;
            double newX = el.X;
            UndoRedo?.RecordAction(
                "Align Left",
                () => el.X = oldX,
                () => el.X = newX
            );
        }
    }

    [RelayCommand]
    public void AlignCenter()
    {
        if (SelectedElement != null && SelectedPage != null)
        {
            var el = SelectedElement;
            double oldX = el.X;
            el.X = Math.Max(0, (SelectedPage.Width - el.Width) / 2);
            double newX = el.X;
            UndoRedo?.RecordAction(
                "Align Center",
                () => el.X = oldX,
                () => el.X = newX
            );
        }
    }

    [RelayCommand]
    public void AlignRight()
    {
        if (SelectedElement != null && SelectedPage != null)
        {
            var el = SelectedElement;
            double oldX = el.X;
            el.X = Math.Max(0, SelectedPage.Width - el.Width - 60);
            double newX = el.X;
            UndoRedo?.RecordAction(
                "Align Right",
                () => el.X = oldX,
                () => el.X = newX
            );
        }
    }

    [RelayCommand]
    public void AlignTop()
    {
        if (SelectedElement != null)
        {
            var el = SelectedElement;
            double oldY = el.Y;
            el.Y = 60;
            double newY = el.Y;
            UndoRedo?.RecordAction(
                "Align Top",
                () => el.Y = oldY,
                () => el.Y = newY
            );
        }
    }

    [RelayCommand]
    public void AlignMiddle()
    {
        if (SelectedElement != null && SelectedPage != null)
        {
            var el = SelectedElement;
            double oldY = el.Y;
            el.Y = Math.Max(0, (SelectedPage.Height - el.Height) / 2);
            double newY = el.Y;
            UndoRedo?.RecordAction(
                "Align Middle",
                () => el.Y = oldY,
                () => el.Y = newY
            );
        }
    }

    [RelayCommand]
    public void AlignBottom()
    {
        if (SelectedElement != null && SelectedPage != null)
        {
            var el = SelectedElement;
            double oldY = el.Y;
            el.Y = Math.Max(0, SelectedPage.Height - el.Height - 60);
            double newY = el.Y;
            UndoRedo?.RecordAction(
                "Align Bottom",
                () => el.Y = oldY,
                () => el.Y = newY
            );
        }
    }

    [RelayCommand]
    public void ToggleLock()
    {
        if (SelectedElement != null)
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

    // --- SMART DISTRIBUTION & MATCH SIZES ---

    [RelayCommand]
    public void DistributeHorizontally()
    {
        if (SelectedPage != null && SelectedPage.Elements.Count >= 3)
        {
            var elements = SelectedPage.Elements.OrderBy(e => e.X).ToList();
            double minX = elements.First().X;
            double maxX = elements.Last().X + elements.Last().Width;
            double totalElementWidth = elements.Sum(e => e.Width);
            double totalSpace = maxX - minX - totalElementWidth;
            double gap = totalSpace / (elements.Count - 1);

            double currentX = minX;
            foreach (var el in elements)
            {
                el.X = currentX;
                currentX += el.Width + gap;
            }
        }
    }

    [RelayCommand]
    public void DistributeVertically()
    {
        if (SelectedPage != null && SelectedPage.Elements.Count >= 3)
        {
            var elements = SelectedPage.Elements.OrderBy(e => e.Y).ToList();
            double minY = elements.First().Y;
            double maxY = elements.Last().Y + elements.Last().Height;
            double totalElementHeight = elements.Sum(e => e.Height);
            double totalSpace = maxY - minY - totalElementHeight;
            double gap = totalSpace / (elements.Count - 1);

            double currentY = minY;
            foreach (var el in elements)
            {
                el.Y = currentY;
                currentY += el.Height + gap;
            }
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
}
