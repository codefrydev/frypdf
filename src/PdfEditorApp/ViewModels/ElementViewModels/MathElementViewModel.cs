using System;
using System.Collections.ObjectModel;
using System.Linq;
using CommunityToolkit.Mvvm.ComponentModel;
using PdfEditorApp.Services;
using CommunityToolkit.Mvvm.Input;
using PdfEditorApp.Core.Models;
using PdfEditorApp.Core.Models.Elements;
using PdfEditorApp.Models;
using PdfEditorApp.Services.MathEngine;

namespace PdfEditorApp.ViewModels.ElementViewModels;

public partial class MathElementViewModel : ElementViewModelBase
{
    [ObservableProperty]
    private string _formula = @"\int_{-\infty}^{\infty} e^{-x^2} \, dx = \sqrt{\pi}";

    [ObservableProperty]
    private double _fontSize = 16;

    [ObservableProperty]
    private string _textColorHex = "#0F172A";

    [ObservableProperty]
    private string _backgroundColorHex = "#00000000";

    [ObservableProperty]
    private string _borderColorHex = "#00000000";

    [ObservableProperty]
    private double _borderThickness = 0;

    [ObservableProperty]
    private double _cornerRadius = 4;

    [ObservableProperty]
    private double _padding = 8;

    [ObservableProperty]
    private bool _showBackground;

    [ObservableProperty]
    private bool _showBorder;

    [ObservableProperty]
    private bool _showEquationNumber;

    [ObservableProperty]
    private string _equationNumber = "(1)";

    [ObservableProperty]
    private TextAlignmentMode _alignment = TextAlignmentMode.Center;

    [ObservableProperty]
    private MathDisplayStyle _displayStyle = MathDisplayStyle.DisplayBlock;

    [ObservableProperty]
    private MathCategory _category = MathCategory.Calculus;

    [ObservableProperty]
    private string? _presetName = "Gaussian Integral";

    [ObservableProperty]
    private string? _description = "Euler-Poisson Gaussian Integral";

    [ObservableProperty]
    private string _svgSource = "";

    [ObservableProperty]
    private string _pathGeometryData = "";

    [ObservableProperty]
    private bool _hasError;

    [ObservableProperty]
    private string? _errorMessage;

    public override ElementKind Kind => ElementKind.Math;
    public override string DisplayName => !string.IsNullOrWhiteSpace(PresetName) ? $"Formula ({PresetName})" : "Math Equation";

    public ObservableCollection<MathPresetItem> AvailablePresets { get; } = new(MathPresetsLibrary.AllPresets);

    public MathElementViewModel()
    {
        _renderDebouncer = new UiDebouncer(RenderDebounceMs, RenderSvg);

        Width = 280;
        Height = 60;

        // Direct, not debounced: the element needs geometry immediately on creation, and the
        // debounced path needs a running dispatcher which headless hosts do not have.
        RenderSvg();
    }

    /// <summary>Coalesces formula re-layout; see <see cref="RenderSvg"/>.</summary>
    private readonly UiDebouncer _renderDebouncer;

    /// <summary>Quiet period before the formula is re-laid-out.</summary>
    private const int RenderDebounceMs = 180;

    // Fourteen properties triggered a full re-layout, and Formula is two-way bound to the
    // in-place editor TextBox (Views/DocumentCanvasView.axaml) — so every character retokenized
    // the formula, rebuilt the AST, re-measured it recursively and rebuilt the whole SVG
    // document with a StringBuilder. MathFormulaControl caches its AST by (Formula, FontSize);
    // this path never did. ToModel() carries only Formula, so the SVG is regenerated downstream
    // and coalescing it here cannot affect export.
    partial void OnFormulaChanged(string value) => _renderDebouncer.Request();
    partial void OnFontSizeChanged(double value) => _renderDebouncer.Request();
    partial void OnTextColorHexChanged(string value) => _renderDebouncer.Request();
    partial void OnBackgroundColorHexChanged(string value) => _renderDebouncer.Request();
    partial void OnBorderColorHexChanged(string value) => _renderDebouncer.Request();
    partial void OnBorderThicknessChanged(double value) => _renderDebouncer.Request();
    partial void OnCornerRadiusChanged(double value) => _renderDebouncer.Request();
    partial void OnPaddingChanged(double value) => _renderDebouncer.Request();
    partial void OnShowBackgroundChanged(bool value) => _renderDebouncer.Request();
    partial void OnShowBorderChanged(bool value) => _renderDebouncer.Request();
    partial void OnShowEquationNumberChanged(bool value) => _renderDebouncer.Request();
    partial void OnEquationNumberChanged(string value) => _renderDebouncer.Request();
    partial void OnAlignmentChanged(TextAlignmentMode value) => _renderDebouncer.Request();
    partial void OnDisplayStyleChanged(MathDisplayStyle value) => _renderDebouncer.Request();

    public void RenderSvg()
    {
        var options = new MathRenderOptions(
            FontSize: FontSize,
            TextColorHex: TextColorHex,
            BackgroundColorHex: BackgroundColorHex,
            BorderColorHex: BorderColorHex,
            BorderThickness: BorderThickness,
            CornerRadius: CornerRadius,
            Padding: Padding,
            ShowBackground: ShowBackground,
            ShowBorder: ShowBorder,
            ShowEquationNumber: ShowEquationNumber,
            EquationNumber: EquationNumber,
            Alignment: Alignment,
            DisplayStyle: DisplayStyle,
            TargetWidth: Width,
            TargetHeight: Height
        );

        var result = MathLayoutEngine.RenderToSvg(Formula, options);
        SvgSource = result.SvgXml;
        PathGeometryData = result.PathGeometryData;
        HasError = !result.IsSuccess;
        ErrorMessage = result.ErrorMessage;
    }

    [RelayCommand]
    public void ApplyPreset(string? presetId)
    {
        if (string.IsNullOrEmpty(presetId)) return;

        var preset = MathPresetsLibrary.FindById(presetId) ?? MathPresetsLibrary.FindByName(presetId);
        if (preset != null)
        {
            Formula = preset.Formula;
            PresetName = preset.Name;
            Description = preset.Description;
            Category = preset.Category;
            EquationNumber = preset.DefaultEquationNumber;
            Width = Math.Max(Width, preset.DefaultWidth);
            Height = Math.Max(Height, preset.DefaultHeight);
            RenderSvg();
        }
    }

    [RelayCommand]
    public void InsertSymbol(string snippet)
    {
        if (string.IsNullOrEmpty(snippet)) return;
        string resolved = MathPresetsLibrary.ResolveSnippet(snippet);
        Formula = string.IsNullOrEmpty(Formula) ? resolved : $"{Formula} {resolved}";
    }

    public override PdfElementBase ToModel()
    {
        return new PdfMathElement
        {
            Id = Id,
            X = X,
            Y = Y,
            Width = Width,
            Height = Height,
            ZIndex = ZIndex,
            Rotation = Rotation,
            Opacity = Opacity,
            IsLocked = IsLocked,
            Formula = Formula,
            FontSize = FontSize,
            TextColorHex = TextColorHex,
            BackgroundColorHex = BackgroundColorHex,
            BorderColorHex = BorderColorHex,
            BorderThickness = BorderThickness,
            CornerRadius = CornerRadius,
            Padding = Padding,
            ShowBackground = ShowBackground,
            ShowBorder = ShowBorder,
            ShowEquationNumber = ShowEquationNumber,
            EquationNumber = EquationNumber,
            Alignment = Alignment,
            DisplayStyle = DisplayStyle,
            Category = Category,
            PresetName = PresetName,
            Description = Description
        };
    }

    public override void LoadFromModel(PdfElementBase model)
    {
        if (model is PdfMathElement math)
        {
            Id = math.Id;
            X = math.X;
            Y = math.Y;
            Width = math.Width;
            Height = math.Height;
            ZIndex = math.ZIndex;
            Rotation = math.Rotation;
            Opacity = math.Opacity;
            IsLocked = math.IsLocked;

            Formula = math.Formula;
            FontSize = math.FontSize;
            TextColorHex = math.TextColorHex;
            BackgroundColorHex = math.BackgroundColorHex;
            BorderColorHex = math.BorderColorHex;
            BorderThickness = math.BorderThickness;
            CornerRadius = math.CornerRadius;
            Padding = math.Padding;
            ShowBackground = math.ShowBackground;
            ShowBorder = math.ShowBorder;
            ShowEquationNumber = math.ShowEquationNumber;
            EquationNumber = math.EquationNumber;
            Alignment = math.Alignment;
            DisplayStyle = math.DisplayStyle;
            Category = math.Category;
            PresetName = math.PresetName;
            Description = math.Description;

            RenderSvg();
        }
    }
}
