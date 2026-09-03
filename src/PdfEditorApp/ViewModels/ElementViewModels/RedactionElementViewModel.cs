using CommunityToolkit.Mvvm.ComponentModel;
using PdfEditorApp.Core.Models;
using PdfEditorApp.Core.Models.Elements;
using PdfEditorApp.Models;

namespace PdfEditorApp.ViewModels.ElementViewModels;

public partial class RedactionElementViewModel : ElementViewModelBase
{
    [ObservableProperty]
    private RedactionMode _mode = RedactionMode.Blackout;

    [ObservableProperty]
    private string _exemptionCode = "[REDACTED - (b)(4) PRIVILEGED]";

    [ObservableProperty]
    private string _fillColorHex = "#0F172A";

    [ObservableProperty]
    private string _textColorHex = "#FFFFFF";

    [ObservableProperty]
    private string _borderColorHex = "#DC2626";

    [ObservableProperty]
    private double _borderThickness = 1.5;

    [ObservableProperty]
    private bool _showOverlayText = true;

    public override ElementKind Kind => ElementKind.Redaction;
    public override string DisplayName => $"Redaction ({ExemptionCode})";

    public RedactionElementViewModel()
    {
        Width = 240;
        Height = 40;
    }

    public override PdfElementBase ToModel()
    {
        return new PdfRedactionElement
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
            Mode = Mode,
            ExemptionCode = ExemptionCode,
            FillColorHex = FillColorHex,
            TextColorHex = TextColorHex,
            BorderColorHex = BorderColorHex,
            BorderThickness = BorderThickness,
            ShowOverlayText = ShowOverlayText
        };
    }

    public override void LoadFromModel(PdfElementBase model)
    {
        if (model is PdfRedactionElement r)
        {
            Id = r.Id;
            X = r.X;
            Y = r.Y;
            Width = r.Width;
            Height = r.Height;
            ZIndex = r.ZIndex;
            Rotation = r.Rotation;
            Opacity = r.Opacity;
            IsLocked = r.IsLocked;

            Mode = r.Mode;
            ExemptionCode = r.ExemptionCode;
            FillColorHex = r.FillColorHex;
            TextColorHex = r.TextColorHex;
            BorderColorHex = r.BorderColorHex;
            BorderThickness = r.BorderThickness;
            ShowOverlayText = r.ShowOverlayText;
        }
    }
}
