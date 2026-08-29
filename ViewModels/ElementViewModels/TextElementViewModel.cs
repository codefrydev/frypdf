using CommunityToolkit.Mvvm.ComponentModel;
using PdfEditorApp.Models;
using PdfEditorApp.Models.Elements;

namespace PdfEditorApp.ViewModels.ElementViewModels;

public partial class TextElementViewModel : ElementViewModelBase
{
    [ObservableProperty]
    private string _text = "Enter text here";

    [ObservableProperty]
    private string _fontFamily = "Segoe UI";

    [ObservableProperty]
    private double _fontSize = 14;

    [ObservableProperty]
    private bool _isBold;

    [ObservableProperty]
    private bool _isItalic;

    [ObservableProperty]
    private bool _isUnderline;

    [ObservableProperty]
    private string _textColorHex = "#201F1E";

    [ObservableProperty]
    private TextAlignmentMode _alignment = TextAlignmentMode.Left;

    [ObservableProperty]
    private double _lineHeight = 1.4;

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

    public override ElementKind Kind => ElementKind.Text;
    public override string DisplayName => Text.Length > 20 ? Text.Substring(0, 17) + "..." : (string.IsNullOrWhiteSpace(Text) ? "Text Box" : Text);

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
            TextColorHex = TextColorHex,
            Alignment = Alignment,
            LineHeight = LineHeight,
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
            TextColorHex = textModel.TextColorHex;
            Alignment = textModel.Alignment;
            LineHeight = textModel.LineHeight;
            Padding = textModel.Padding;
            BackgroundColorHex = textModel.BackgroundColorHex;
            BorderColorHex = textModel.BorderColorHex;
            BorderThickness = textModel.BorderThickness;
            CornerRadius = textModel.CornerRadius;
        }
    }
}
