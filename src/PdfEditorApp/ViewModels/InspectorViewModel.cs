using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Globalization;
using System.Linq;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using PdfEditorApp.Models;
using PdfEditorApp.Models.Elements;
using PdfEditorApp.Services;
using PdfEditorApp.Services.Typography;
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

    public static Action<TextElementViewModel>? OnActiveTextFormattingApplied { get; set; }

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
    private bool _isSvgElement;

    [ObservableProperty]
    private bool _isMathElement;

    // Collapsible Inspector Sections / Accordions (Default true for instant discovery)
    [ObservableProperty]
    private bool _isPageSetupExpanded = true;

    [ObservableProperty]
    private bool _isPresetGeometryExpanded = true;

    [ObservableProperty]
    private bool _isLineStrokePatternExpanded = true;

    [ObservableProperty]
    private bool _isColorsExpanded = true;

    [ObservableProperty]
    private bool _isDimensionsExpanded = true;

    [ObservableProperty]
    private bool _isTypographyExpanded = true;

    [ObservableProperty]
    private bool _isParagraphExpanded = true;

    [ObservableProperty]
    private bool _isTransformExpanded = true;

    [ObservableProperty]
    private bool _isShadowExpanded = true;

    [ObservableProperty]
    private bool _isImageAdjustmentsExpanded = true;

    [ObservableProperty]
    private bool _isTablePropertiesExpanded = true;

    [ObservableProperty]
    private bool _isMathFormulaExpanded = true;

    [RelayCommand]
    public void TogglePresetGeometryExpanded() => IsPresetGeometryExpanded = !IsPresetGeometryExpanded;

    [RelayCommand]
    public void ToggleLineStrokePatternExpanded() => IsLineStrokePatternExpanded = !IsLineStrokePatternExpanded;

    [RelayCommand]
    public void ToggleColorsExpanded() => IsColorsExpanded = !IsColorsExpanded;

    [RelayCommand]
    public void ToggleDimensionsExpanded() => IsDimensionsExpanded = !IsDimensionsExpanded;

    [RelayCommand]
    public void ToggleTypographyExpanded() => IsTypographyExpanded = !IsTypographyExpanded;

    [RelayCommand]
    public void ToggleParagraphExpanded() => IsParagraphExpanded = !IsParagraphExpanded;

    [RelayCommand]
    public void ToggleTransformExpanded() => IsTransformExpanded = !IsTransformExpanded;

    [RelayCommand]
    public void ToggleShadowExpanded() => IsShadowExpanded = !IsShadowExpanded;

    [RelayCommand]
    public void TogglePageSetupExpanded() => IsPageSetupExpanded = !IsPageSetupExpanded;

    [RelayCommand]
    public void ToggleImageAdjustmentsExpanded() => IsImageAdjustmentsExpanded = !IsImageAdjustmentsExpanded;

    [RelayCommand]
    public void ToggleTablePropertiesExpanded() => IsTablePropertiesExpanded = !IsTablePropertiesExpanded;

    [RelayCommand]
    public void ToggleMathFormulaExpanded() => IsMathFormulaExpanded = !IsMathFormulaExpanded;

    [RelayCommand]
    public void ExpandAllSections()
    {
        IsPageSetupExpanded = true;
        IsPresetGeometryExpanded = true;
        IsLineStrokePatternExpanded = true;
        IsColorsExpanded = true;
        IsDimensionsExpanded = true;
        IsTypographyExpanded = true;
        IsParagraphExpanded = true;
        IsTransformExpanded = true;
        IsShadowExpanded = true;
        IsImageAdjustmentsExpanded = true;
        IsTablePropertiesExpanded = true;
        IsMathFormulaExpanded = true;
    }

    [RelayCommand]
    public void CollapseAllSections()
    {
        IsPageSetupExpanded = false;
        IsPresetGeometryExpanded = false;
        IsLineStrokePatternExpanded = false;
        IsColorsExpanded = false;
        IsDimensionsExpanded = false;
        IsTypographyExpanded = false;
        IsParagraphExpanded = false;
        IsTransformExpanded = false;
        IsShadowExpanded = false;
        IsImageAdjustmentsExpanded = false;
        IsTablePropertiesExpanded = false;
        IsMathFormulaExpanded = false;
    }

    [ObservableProperty]
    private string _selectedFontFamily = "Arial";

    partial void OnSelectedFontFamilyChanged(string value)
    {
        if (TextElement != null && !string.IsNullOrEmpty(value))
        {
            if (TryApplyInlineFormatting(InlineFormatType.Font, $"Font: {value}", value)) return;

            if (TextElement.FontFamily != value)
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
    }

    [ObservableProperty]
    private double _selectedFontSize = 14;

    partial void OnSelectedFontSizeChanged(double value)
    {
        if (TextElement != null && value > 0)
        {
            if (TryApplyInlineFormatting(InlineFormatType.Size, $"Font Size: {value}pt", value.ToString("0.#", CultureInfo.InvariantCulture))) return;

            if (Math.Abs(TextElement.FontSize - value) > 0.1)
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
    }

    public ObservableCollection<string> AvailableFontFamilies { get; } = new()
    {
        // ── Sans-Serif (Modern) ─────────────────────────────────────────────
        "Inter",
        "Roboto",
        "Open Sans",
        "Lato",
        "Poppins",
        "Nunito",
        "Raleway",
        "Montserrat",
        "Source Sans 3",
        "Ubuntu",
        "Cabin",
        "Josefin Sans",
        "Titillium Web",
        "Exo 2",
        "Noto Sans",
        "Oswald",

        // ── Sans-Serif (System) ─────────────────────────────────────────────
        "Arial",
        "Verdana",
        "Segoe UI",
        "Helvetica",
        "Trebuchet MS",

        // ── Serif ───────────────────────────────────────────────────────────
        "Playfair Display",
        "Merriweather",
        "Lora",
        "PT Serif",
        "Noto Serif",
        "Crimson Text",
        "Libre Baskerville",
        "Libre Franklin",
        "Cinzel",
        "Times New Roman",
        "Georgia",
        "Palatino",

        // ── Monospace ───────────────────────────────────────────────────────
        "Fira Code",
        "Roboto Mono",
        "Courier New",
        "Consolas",
        "Menlo",

        // ── Display & Decorative ────────────────────────────────────────────
        "Bebas Neue",
        "Orbitron",
        "Lobster",
        "Pacifico",
        "Impact",

        // ── Handwriting & Script ────────────────────────────────────────────
        "Dancing Script",
        "Caveat",
        "Great Vibes",
        "Comic Neue",

        // ── Indian Scripts ──────────────────────────────────────────────────
        "Noto Sans Devanagari",
        "Tiro Devanagari Hindi",
        "Noto Sans Tamil",
        "Noto Sans Telugu",
        "Noto Sans Bengali",
        "Noto Sans Gujarati",
        "Noto Sans Kannada",
        "Noto Sans Malayalam",
        "Noto Sans Sinhala",

        // ── CJK — Chinese ───────────────────────────────────────────────────
        "Noto Sans SC",           // Simplified Chinese
        "Noto Sans TC",           // Traditional Chinese

        // ── CJK — Japanese ──────────────────────────────────────────────────
        "Noto Sans JP",
        "Noto Serif JP",

        // ── CJK — Korean ────────────────────────────────────────────────────
        "Noto Sans KR",
        "Nanum Gothic",

        // ── Southeast Asian ──────────────────────────────────────────────────
        "Noto Sans Thai",
        "Sarabun",
        "Noto Sans Myanmar",
        "Noto Sans Khmer",
        "Noto Sans Lao",
        "Be Vietnam Pro",

        // ── Middle Eastern / RTL ─────────────────────────────────────────────
        "Noto Sans Arabic",
        "Vazirmatn",
        "Noto Nastaliq Urdu",
        "Noto Sans Hebrew",
        "Heebo",

        // ── Eurasian ─────────────────────────────────────────────────────────
        "Golos Text",
        "Russo One",
        "GFS Neohellenic",
        "Noto Sans Georgian",
        "Noto Sans Armenian",
        "Noto Sans Ethiopic",
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
    public SvgElementViewModel? SvgElement => SelectedElement as SvgElementViewModel;
    public MathElementViewModel? MathElement => SelectedElement as MathElementViewModel;

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
        IsSvgElement = element is SvgElementViewModel;
        IsMathElement = element is MathElementViewModel;

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
        OnPropertyChanged(nameof(SvgElement));
        OnPropertyChanged(nameof(MathElement));
        OnPropertyChanged(nameof(ActiveCategoryName));
    }

    private bool TryApplyInlineFormatting(InlineFormatType formatType, string actionName, string? argument = null)
    {
        if (TextElement != null && TextElement.HasTextSelection)
        {
            var el = TextElement;
            string oldText = el.Text;
            int oldSelStart = el.ActiveSelectionStart;
            int oldSelLen = el.ActiveSelectionLength;
            if (el.ApplyInlineFormatting(formatType, argument))
            {
                string newText = el.Text;
                int newSelStart = el.ActiveSelectionStart;
                int newSelLen = el.ActiveSelectionLength;
                OnActiveTextFormattingApplied?.Invoke(el);
                UndoRedo?.RecordAction(
                    actionName,
                    () =>
                    {
                        el.Text = oldText;
                        int s = Math.Min(oldSelStart, el.Text.Length);
                        int l = Math.Min(oldSelLen, el.Text.Length - s);
                        el.UpdateTextSelection(s, l, el.Text.Substring(s, l));
                        OnActiveTextFormattingApplied?.Invoke(el);
                    },
                    () =>
                    {
                        el.Text = newText;
                        int s = Math.Min(newSelStart, el.Text.Length);
                        int l = Math.Min(newSelLen, el.Text.Length - s);
                        el.UpdateTextSelection(s, l, el.Text.Substring(s, l));
                        OnActiveTextFormattingApplied?.Invoke(el);
                    }
                );
                return true;
            }
        }
        return false;
    }

    [RelayCommand]
    public void SetTextColor(string hex)
    {
        if (string.IsNullOrEmpty(hex) || TextElement == null) return;

        if (TryApplyInlineFormatting(InlineFormatType.Color, "Format Color Selection", hex)) return;

        if (TextElement.TextColorHex != hex)
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
            if (TryApplyInlineFormatting(InlineFormatType.Bold, "Format Bold Selection")) return;

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
            if (TryApplyInlineFormatting(InlineFormatType.Italic, "Format Italic Selection")) return;

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
            if (TryApplyInlineFormatting(InlineFormatType.Underline, "Format Underline Selection")) return;

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
    public void AutoFitTextHeight()
    {
        if (TextElement != null)
        {
            var el = TextElement;
            double oldH = el.Height;
            double newH = el.CalculateRequiredHeight();
            if (Math.Abs(newH - oldH) > 0.5)
            {
                el.Height = newH;
                UndoRedo?.RecordAction(
                    "Auto-Fit Text Height",
                    () => el.Height = oldH,
                    () => el.Height = newH
                );
            }
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
                    ElementKind.Svg => new SvgElementViewModel(),
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
                ElementKind.Svg => new SvgElementViewModel(),
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
    public void SetChartPalette(string paletteStr)
    {
        if (ChartElement != null && Enum.TryParse<ChartPalette>(paletteStr, true, out var parsed) && ChartElement.Palette != parsed)
        {
            var el = ChartElement;
            var oldPal = el.Palette;
            el.SetPalette(paletteStr);
            UndoRedo?.RecordAction(
                $"Change Palette to {parsed}",
                () => el.Palette = oldPal,
                () => el.Palette = parsed
            );
        }
    }

    [RelayCommand]
    public void SetChartLegendPosition(string posStr)
    {
        if (ChartElement != null && Enum.TryParse<ChartLegendPosition>(posStr, true, out var parsed) && ChartElement.LegendPosition != parsed)
        {
            var el = ChartElement;
            var oldPos = el.LegendPosition;
            el.SetLegendPosition(posStr);
            UndoRedo?.RecordAction(
                $"Change Legend to {parsed}",
                () => el.LegendPosition = oldPos,
                () => el.LegendPosition = parsed
            );
        }
    }

    public PdfEditorApp.ViewModels.DataStudio.DataStudioViewModel? DataStudio { get; set; }

    [RelayCommand]
    public void OpenDataStudioForChart()
    {
        if (ChartElement != null && DataStudio != null)
        {
            DataStudio.OpenForChart(ChartElement, SelectedPage);
        }
    }

    [RelayCommand]
    public void OpenDataStudioForTable()
    {
        if (TableElement != null && DataStudio != null)
        {
            DataStudio.OpenForTable(TableElement, SelectedPage);
        }
    }

    [RelayCommand]
    public void ConvertTableToChart()
    {
        if (TableElement != null && SelectedPage != null)
        {
            var bindingService = new PdfEditorApp.Core.Data.DataBindingService();
            var chartModel = bindingService.ConvertTableToChart((PdfTableElement)TableElement.ToModel());
            var chartVm = new ChartElementViewModel();
            chartVm.LoadFromModel(chartModel);

            var oldTable = TableElement;
            var page = SelectedPage;
            int idx = page.Elements.IndexOf(oldTable);

            page.Elements.Remove(oldTable);
            page.Elements.Insert(Math.Max(0, idx), chartVm);
            page.SelectedElement = chartVm;

            UndoRedo?.RecordAction(
                "Convert Table to Chart",
                () =>
                {
                    int cIdx = page.Elements.IndexOf(chartVm);
                    if (cIdx >= 0) page.Elements.RemoveAt(cIdx);
                    page.Elements.Insert(Math.Max(0, idx), oldTable);
                    page.SelectedElement = oldTable;
                },
                () =>
                {
                    int tIdx = page.Elements.IndexOf(oldTable);
                    if (tIdx >= 0) page.Elements.RemoveAt(tIdx);
                    page.Elements.Insert(Math.Max(0, idx), chartVm);
                    page.SelectedElement = chartVm;
                }
            );
        }
    }

    [RelayCommand]
    public void ConvertChartToTable()
    {
        if (ChartElement != null && SelectedPage != null)
        {
            var bindingService = new PdfEditorApp.Core.Data.DataBindingService();
            var tableModel = bindingService.ConvertChartToTable((PdfChartElement)ChartElement.ToModel());
            var tableVm = new TableElementViewModel();
            tableVm.LoadFromModel(tableModel);

            var oldChart = ChartElement;
            var page = SelectedPage;
            int idx = page.Elements.IndexOf(oldChart);

            page.Elements.Remove(oldChart);
            page.Elements.Insert(Math.Max(0, idx), tableVm);
            page.SelectedElement = tableVm;

            UndoRedo?.RecordAction(
                "Convert Chart to Table",
                () =>
                {
                    int tIdx = page.Elements.IndexOf(tableVm);
                    if (tIdx >= 0) page.Elements.RemoveAt(tIdx);
                    page.Elements.Insert(Math.Max(0, idx), oldChart);
                    page.SelectedElement = oldChart;
                },
                () =>
                {
                    int cIdx = page.Elements.IndexOf(oldChart);
                    if (cIdx >= 0) page.Elements.RemoveAt(cIdx);
                    page.Elements.Insert(Math.Max(0, idx), tableVm);
                    page.SelectedElement = tableVm;
                }
            );
        }
    }

    [RelayCommand]
    public async Task QuickPasteDataToChart()
    {
        if (ChartElement != null && DataStudio != null)
        {
            await DataStudio.PasteFromClipboardAsync();
            DataStudio.OpenForChart(ChartElement, SelectedPage);
            DataStudio.ApplyData();
        }
    }

    [RelayCommand]
    public async Task QuickPasteDataToTable()
    {
        if (TableElement != null && DataStudio != null)
        {
            await DataStudio.PasteFromClipboardAsync();
            DataStudio.OpenForTable(TableElement, SelectedPage);
            DataStudio.ApplyData();
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
            if (TryApplyInlineFormatting(InlineFormatType.Strikethrough, "Format Strikethrough Selection")) return;

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
    public void ToggleDoubleUnderline()
    {
        if (TextElement != null)
        {
            var el = TextElement;
            bool oldVal = el.IsDoubleUnderline;
            bool newVal = !oldVal;
            el.IsDoubleUnderline = newVal;
            UndoRedo?.RecordAction(
                newVal ? "Format Double Underline" : "Remove Double Underline",
                () => el.IsDoubleUnderline = oldVal,
                () => el.IsDoubleUnderline = newVal
            );
        }
    }

    [RelayCommand]
    public void SetVerticalAlignment(string vAlignStr)
    {
        if (TextElement != null && Enum.TryParse<TextVerticalAlignment>(vAlignStr, true, out var vAlign) && TextElement.VerticalAlignment != vAlign)
        {
            var el = TextElement;
            var oldVal = el.VerticalAlignment;
            el.VerticalAlignment = vAlign;
            UndoRedo?.RecordAction(
                $"Vertical Align {vAlign}",
                () => el.VerticalAlignment = oldVal,
                () => el.VerticalAlignment = vAlign
            );
        }
    }

    [RelayCommand]
    public void SetTextShapeMode(string modeStr)
    {
        if (TextElement != null && Enum.TryParse<TextShapeMode>(modeStr, true, out var mode) && TextElement.ShapeMode != mode)
        {
            var el = TextElement;
            var oldVal = el.ShapeMode;
            el.ShapeMode = mode;
            UndoRedo?.RecordAction(
                $"Change Text Shape {mode}",
                () => el.ShapeMode = oldVal,
                () => el.ShapeMode = mode
            );
        }
    }

    [RelayCommand]
    public void SetCircularPlacement(string placementStr)
    {
        if (TextElement != null && Enum.TryParse<CircularTextPlacement>(placementStr, true, out var placement) && TextElement.CircularPlacement != placement)
        {
            var el = TextElement;
            var oldVal = el.CircularPlacement;
            el.CircularPlacement = placement;
            UndoRedo?.RecordAction(
                $"Circular Placement {placement}",
                () => el.CircularPlacement = oldVal,
                () => el.CircularPlacement = placement
            );
        }
    }

    [RelayCommand]
    public void SetBezierPreset(string presetStr)
    {
        if (TextElement != null && Enum.TryParse<BezierCurvePreset>(presetStr, true, out var preset))
        {
            var el = TextElement;
            var oldPreset = el.BezierPreset;
            double p0x = el.BezierP0X, p0y = el.BezierP0Y;
            double p1x = el.BezierP1X, p1y = el.BezierP1Y;
            double p2x = el.BezierP2X, p2y = el.BezierP2Y;
            double p3x = el.BezierP3X, p3y = el.BezierP3Y;

            el.ApplyBezierPreset(preset);

            UndoRedo?.RecordAction(
                $"Bézier Curve Preset: {preset}",
                () =>
                {
                    el.BezierPreset = oldPreset;
                    el.BezierP0X = p0x; el.BezierP0Y = p0y;
                    el.BezierP1X = p1x; el.BezierP1Y = p1y;
                    el.BezierP2X = p2x; el.BezierP2Y = p2y;
                    el.BezierP3X = p3x; el.BezierP3Y = p3y;
                },
                () => el.ApplyBezierPreset(preset)
            );
        }
    }

    [RelayCommand]
    public void ResetBezierControlPoints()
    {
        if (TextElement != null)
        {
            var el = TextElement;
            el.ApplyBezierPreset(BezierCurvePreset.Wave);
        }
    }

    [RelayCommand]
    public void ApplyTypographyPreset(string presetName)
    {
        if (TextElement != null)
        {
            var el = TextElement;
            var oldModel = (PdfTextElement)el.ToModel();
            el.ApplyTypographyPreset(presetName);
            var newModel = (PdfTextElement)el.ToModel();

            UndoRedo?.RecordAction(
                $"Typography Preset: {presetName}",
                () => el.LoadFromModel(oldModel),
                () => el.LoadFromModel(newModel)
            );
        }
    }

    [RelayCommand]
    public void ToggleTextStroke()
    {
        if (TextElement != null)
        {
            var el = TextElement;
            bool oldVal = el.HasStroke;
            bool newVal = !oldVal;
            el.HasStroke = newVal;
            UndoRedo?.RecordAction(
                newVal ? "Enable Text Outline" : "Disable Text Outline",
                () => el.HasStroke = oldVal,
                () => el.HasStroke = newVal
            );
        }
    }

    [RelayCommand]
    public void SetTextStrokeColor(string hex)
    {
        if (TextElement != null && TextElement.StrokeColorHex != hex)
        {
            var el = TextElement;
            string oldHex = el.StrokeColorHex;
            el.StrokeColorHex = hex;
            el.HasStroke = true;
            UndoRedo?.RecordAction(
                "Change Text Stroke Color",
                () => el.StrokeColorHex = oldHex,
                () => el.StrokeColorHex = hex
            );
        }
    }

    [RelayCommand]
    public void ToggleTextShadow()
    {
        if (TextElement != null)
        {
            var el = TextElement;
            bool oldVal = el.HasShadow;
            bool newVal = !oldVal;
            el.HasShadow = newVal;
            UndoRedo?.RecordAction(
                newVal ? "Enable Text Shadow" : "Disable Text Shadow",
                () => el.HasShadow = oldVal,
                () => el.HasShadow = newVal
            );
        }
    }

    [RelayCommand]
    public void SetTextShadowColor(string hex)
    {
        if (TextElement != null && TextElement.ShadowColorHex != hex)
        {
            var el = TextElement;
            string oldHex = el.ShadowColorHex;
            el.ShadowColorHex = hex;
            el.HasShadow = true;
            UndoRedo?.RecordAction(
                "Change Text Shadow Color",
                () => el.ShadowColorHex = oldHex,
                () => el.ShadowColorHex = hex
            );
        }
    }

    [RelayCommand]
    public void ToggleCurveDirection()
    {
        if (TextElement != null)
        {
            var el = TextElement;
            bool oldVal = el.CurveClockwise;
            bool newVal = !oldVal;
            el.CurveClockwise = newVal;
            UndoRedo?.RecordAction(
                newVal ? "Curve Clockwise" : "Curve Counter-Clockwise",
                () => el.CurveClockwise = oldVal,
                () => el.CurveClockwise = newVal
            );
        }
    }

    [RelayCommand]
    public void ToggleCurveInvert()
    {
        if (TextElement != null)
        {
            var el = TextElement;
            bool oldVal = el.CurveInvert;
            bool newVal = !oldVal;
            el.CurveInvert = newVal;
            UndoRedo?.RecordAction(
                newVal ? "Invert Text Orientation" : "Normal Text Orientation",
                () => el.CurveInvert = oldVal,
                () => el.CurveInvert = newVal
            );
        }
    }

    [RelayCommand]
    public void AutoFitTextWidth()
    {
        if (TextElement != null)
        {
            var el = TextElement;
            double oldW = el.Width;
            double newW = el.CalculateRequiredWidth();
            if (Math.Abs(newW - oldW) > 0.5)
            {
                el.Width = newW;
                UndoRedo?.RecordAction(
                    "Auto-Fit Text Width",
                    () => el.Width = oldW,
                    () => el.Width = newW
                );
            }
        }
    }

    [RelayCommand]
    public void AutoFitTextBoth()
    {
        if (TextElement != null)
        {
            var el = TextElement;
            double oldW = el.Width, oldH = el.Height;
            double newW = el.CalculateRequiredWidth();
            double newH = el.CalculateRequiredHeight();
            el.Width = newW;
            el.Height = newH;
            UndoRedo?.RecordAction(
                "Auto-Fit Text Dimensions",
                () => { el.Width = oldW; el.Height = oldH; },
                () => { el.Width = newW; el.Height = newH; }
            );
        }
    }

    [RelayCommand]
    public void IncreaseFontSize()
    {
        if (TextElement != null)
        {
            double step = TextElement.FontSize < 24 ? 1.0 : (TextElement.FontSize < 48 ? 2.0 : 4.0);
            double newSize = Math.Min(288, Math.Round(TextElement.FontSize + step));
            SelectedFontSize = newSize;
        }
    }

    [RelayCommand]
    public void DecreaseFontSize()
    {
        if (TextElement != null)
        {
            double step = TextElement.FontSize <= 24 ? 1.0 : (TextElement.FontSize <= 48 ? 2.0 : 4.0);
            double newSize = Math.Max(4, Math.Round(TextElement.FontSize - step));
            SelectedFontSize = newSize;
        }
    }

    [RelayCommand]
    public void StartEditMode()
    {
        if (SelectedElement != null)
        {
            SelectedElement.IsInEditMode = true;
        }
    }

    [RelayCommand]
    public void ToggleEditMode()
    {
        if (SelectedElement != null)
        {
            SelectedElement.IsInEditMode = !SelectedElement.IsInEditMode;
        }
    }

    [RelayCommand]
    public void FinishEditMode()
    {
        if (SelectedElement != null)
        {
            SelectedElement.IsInEditMode = false;
        }
    }

    [RelayCommand]
    public void ResetTextTransforms()
    {
        if (TextElement != null)
        {
            var el = TextElement;
            double sx = el.ScaleX, sy = el.ScaleY, bShift = el.BaselineShift, cRot = el.CharacterRotation;
            bool fx = el.FlipX, fy = el.FlipY;

            el.ResetTransforms();
            UndoRedo?.RecordAction(
                "Reset Text Transforms",
                () => { el.ScaleX = sx; el.ScaleY = sy; el.FlipX = fx; el.FlipY = fy; el.BaselineShift = bShift; el.CharacterRotation = cRot; },
                () => { el.ResetTransforms(); }
            );
        }
    }

    [RelayCommand]
    public void ToggleTextFlipX()
    {
        if (TextElement != null)
        {
            var el = TextElement;
            bool oldVal = el.FlipX;
            bool newVal = !oldVal;
            el.FlipX = newVal;
            UndoRedo?.RecordAction(
                newVal ? "Flip Text Horizontally" : "Unflip Text Horizontally",
                () => el.FlipX = oldVal,
                () => el.FlipX = newVal
            );
        }
    }

    [RelayCommand]
    public void ToggleTextFlipY()
    {
        if (TextElement != null)
        {
            var el = TextElement;
            bool oldVal = el.FlipY;
            bool newVal = !oldVal;
            el.FlipY = newVal;
            UndoRedo?.RecordAction(
                newVal ? "Flip Text Vertically" : "Unflip Text Vertically",
                () => el.FlipY = oldVal,
                () => el.FlipY = newVal
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

    [RelayCommand]
    public void SetDividerStyle(string styleStr)
    {
        if (DividerElement != null && Enum.TryParse<DividerStyle>(styleStr, true, out var style))
        {
            var el = DividerElement;
            var oldStyle = el.Style;
            el.Style = style;
            UndoRedo?.RecordAction($"Divider Style: {style}", () => el.Style = oldStyle, () => el.Style = style);
        }
    }

    [RelayCommand]
    public void SetDividerDashStyle(string dashStr)
    {
        if (DividerElement != null && Enum.TryParse<LineDashStyle>(dashStr, true, out var dash))
        {
            var el = DividerElement;
            var oldDash = el.DashStyle;
            el.DashStyle = dash;
            UndoRedo?.RecordAction($"Divider Pattern: {dash}", () => el.DashStyle = oldDash, () => el.DashStyle = dash);
        }
    }

    [RelayCommand]
    public void SetShapeDashStyle(string dashStr)
    {
        if (ShapeElement != null && Enum.TryParse<LineDashStyle>(dashStr, true, out var dash))
        {
            var el = ShapeElement;
            var oldDash = el.DashStyle;
            el.DashStyle = dash;
            UndoRedo?.RecordAction($"Line Pattern: {dash}", () => el.DashStyle = oldDash, () => el.DashStyle = dash);
        }
    }

    [RelayCommand]
    public void SetShapeStartCap(string capStr)
    {
        if (ShapeElement != null && Enum.TryParse<LineEndCap>(capStr, true, out var cap))
        {
            var el = ShapeElement;
            var oldCap = el.StartCap;
            el.StartCap = cap;
            UndoRedo?.RecordAction($"Start Cap: {cap}", () => el.StartCap = oldCap, () => el.StartCap = cap);
        }
    }

    [RelayCommand]
    public void SetShapeEndCap(string capStr)
    {
        if (ShapeElement != null && Enum.TryParse<LineEndCap>(capStr, true, out var cap))
        {
            var el = ShapeElement;
            var oldCap = el.EndCap;
            el.EndCap = cap;
            UndoRedo?.RecordAction($"End Cap: {cap}", () => el.EndCap = oldCap, () => el.EndCap = cap);
        }
    }

    [RelayCommand]
    public void ToggleInkSmoothSpline()
    {
        if (InkElement != null)
        {
            var el = InkElement;
            bool oldVal = el.IsSmoothSpline;
            bool newVal = !oldVal;
            el.IsSmoothSpline = newVal;
            UndoRedo?.RecordAction(newVal ? "Enable Bézier Ink Smoothing" : "Disable Bézier Ink Smoothing",
                () => el.IsSmoothSpline = oldVal, () => el.IsSmoothSpline = newVal);
        }
    }

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
                page.HeaderLeft = "CodeFryDev";
                page.HeaderRight = DateTime.Now.ToString("yyyy-MM-dd");
                page.FooterLeft = "INTERNAL USE ONLY";
                page.FooterRight = $"Page {page.PageNumber}";
            }
        }
    }

    [RelayCommand]
    public void SetSvgPreset(string preset)
    {
        if (SvgElement != null)
        {
            var el = SvgElement;
            string? oldPreset = el.PresetName;
            string oldSource = el.SvgSource;
            el.ApplyPreset(preset);
            UndoRedo?.RecordAction(
                $"SVG Preset: {preset}",
                () => { el.PresetName = oldPreset; el.SvgSource = oldSource; },
                () => el.ApplyPreset(preset)
            );
        }
    }

    [RelayCommand]
    public void SetSvgTintColor(string hex)
    {
        if (SvgElement != null)
        {
            var el = SvgElement;
            string? oldTint = el.TintColorHex;
            string oldSource = el.SvgSource;
            el.TintColorHex = hex;
            if (!string.IsNullOrEmpty(el.PresetName))
            {
                el.SvgSource = SvgOrnamentLibrary.GetSvg(el.PresetName, hex);
            }
            UndoRedo?.RecordAction(
                "Change SVG Tint Color",
                () => { el.TintColorHex = oldTint; el.SvgSource = oldSource; },
                () => {
                    el.TintColorHex = hex;
                    if (!string.IsNullOrEmpty(el.PresetName))
                        el.SvgSource = SvgOrnamentLibrary.GetSvg(el.PresetName, hex);
                }
            );
        }
    }
}
