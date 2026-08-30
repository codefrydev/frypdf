using System.Linq;
using CommunityToolkit.Mvvm.ComponentModel;
using PdfEditorApp.Models;
using PdfEditorApp.Models.Elements;

namespace PdfEditorApp.ViewModels.ElementViewModels;

public partial class TextElementViewModel : ElementViewModelBase
{
    [ObservableProperty]
    private string _text = "Enter text here";

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(AvaloniaFontFamily))]
    private string _fontFamily = "Arial";

    public Avalonia.Media.FontFamily AvaloniaFontFamily => PdfEditorApp.Services.FontHelper.CreateFontFamily(FontFamily);

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(ComputedLineHeight))]
    private double _fontSize = 14;

    [ObservableProperty]
    private bool _isBold;

    [ObservableProperty]
    private bool _isItalic;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(TextDecorations))]
    private bool _isUnderline;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(TextDecorations))]
    private bool _isStrikethrough;

    [ObservableProperty]
    private string _textColorHex = "#201F1E";

    [ObservableProperty]
    private TextAlignmentMode _alignment = TextAlignmentMode.Left;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(ComputedLineHeight))]
    private double _lineHeight = 1.4;

    [ObservableProperty]
    private double _characterSpacing = 0;

    [ObservableProperty]
    private double _padding = 6;

    [ObservableProperty]
    private string _backgroundColorHex = "#00000000";

    [ObservableProperty]
    private string _borderColorHex = "#00000000";

    [ObservableProperty]
    private double _borderThickness = 0;

    [ObservableProperty]
    private double _cornerRadius = 0;

    public Avalonia.Media.TextDecorationCollection? TextDecorations
    {
        get
        {
            if (IsUnderline && IsStrikethrough)
            {
                var decs = new Avalonia.Media.TextDecorationCollection();
                foreach (var d in Avalonia.Media.TextDecorations.Underline) decs.Add(d);
                foreach (var d in Avalonia.Media.TextDecorations.Strikethrough) decs.Add(d);
                return decs;
            }
            if (IsUnderline) return Avalonia.Media.TextDecorations.Underline;
            if (IsStrikethrough) return Avalonia.Media.TextDecorations.Strikethrough;
            return null;
        }
    }

    public double ComputedLineHeight => FontSize * (LineHeight > 0.1 ? LineHeight : 1.2);

    public override ElementKind Kind => ElementKind.Text;
    public override string DisplayName => Text.Length > 20 ? Text.Substring(0, 17) + "..." : (string.IsNullOrWhiteSpace(Text) ? "Text Box" : Text);

    public void TransformUppercase()
    {
        if (!string.IsNullOrEmpty(Text)) Text = Text.ToUpperInvariant();
    }

    public void TransformLowercase()
    {
        if (!string.IsNullOrEmpty(Text)) Text = Text.ToLowerInvariant();
    }

    public void TransformTitleCase()
    {
        if (!string.IsNullOrEmpty(Text))
        {
            var ti = System.Globalization.CultureInfo.CurrentCulture.TextInfo;
            Text = ti.ToTitleCase(Text.ToLower());
        }
    }

    public void ToggleBulletList()
    {
        if (string.IsNullOrEmpty(Text)) return;
        var lines = Text.Split('\n');
        bool allBulleted = lines.All(l => l.TrimStart().StartsWith("• "));
        if (allBulleted)
        {
            Text = string.Join('\n', lines.Select(l => l.TrimStart().StartsWith("• ") ? l.TrimStart().Substring(2) : l));
        }
        else
        {
            Text = string.Join('\n', lines.Select(l => string.IsNullOrWhiteSpace(l) ? l : (l.TrimStart().StartsWith("• ") ? l : $"• {l}")));
        }
    }

    public void ToggleNumberedList()
    {
        if (string.IsNullOrEmpty(Text)) return;
        var lines = Text.Split('\n');
        int count = 1;
        Text = string.Join('\n', lines.Select(l =>
        {
            if (string.IsNullOrWhiteSpace(l)) return l;
            var trimmed = l.TrimStart();
            int dotIdx = trimmed.IndexOf('.');
            if (dotIdx > 0 && int.TryParse(trimmed.Substring(0, dotIdx), out _))
            {
                return trimmed.Substring(dotIdx + 1).TrimStart();
            }
            return $"{count++}. {trimmed}";
        }));
    }

    public override PdfElementBase ToModel()
    {
        return new PdfTextElement
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
            Text = Text,
            FontFamily = FontFamily,
            FontSize = FontSize,
            IsBold = IsBold,
            IsItalic = IsItalic,
            IsUnderline = IsUnderline,
            IsStrikethrough = IsStrikethrough,
            TextColorHex = TextColorHex,
            Alignment = Alignment,
            LineHeight = LineHeight,
            CharacterSpacing = CharacterSpacing,
            Padding = Padding,
            BackgroundColorHex = BackgroundColorHex,
            BorderColorHex = BorderColorHex,
            BorderThickness = BorderThickness,
            CornerRadius = CornerRadius
        };
    }

    public override void LoadFromModel(PdfElementBase model)
    {
        if (model is PdfTextElement textModel)
        {
            Id = textModel.Id;
            X = textModel.X;
            Y = textModel.Y;
            Width = textModel.Width;
            Height = textModel.Height;
            ZIndex = textModel.ZIndex;
            Rotation = textModel.Rotation;
            Opacity = textModel.Opacity;
            IsLocked = textModel.IsLocked;

            Text = textModel.Text;
            FontFamily = textModel.FontFamily;
            FontSize = textModel.FontSize;
            IsBold = textModel.IsBold;
            IsItalic = textModel.IsItalic;
            IsUnderline = textModel.IsUnderline;
            IsStrikethrough = textModel.IsStrikethrough;
            TextColorHex = textModel.TextColorHex;
            Alignment = textModel.Alignment;
            LineHeight = textModel.LineHeight;
            CharacterSpacing = textModel.CharacterSpacing;
            Padding = textModel.Padding;
            BackgroundColorHex = textModel.BackgroundColorHex;
            BorderColorHex = textModel.BorderColorHex;
            BorderThickness = textModel.BorderThickness;
            CornerRadius = textModel.CornerRadius;
        }
    }
}
