using System.ComponentModel;
using CommunityToolkit.Mvvm.ComponentModel;
using PdfEditorApp.Core.Models;
using PdfEditorApp.Core.Models.Elements;
using PdfEditorApp.Models;
using PdfEditorApp.Services;

namespace PdfEditorApp.ViewModels.ElementViewModels;

public partial class DividerElementViewModel : ElementViewModelBase
{
    [ObservableProperty]
    private string _colorHex = "#0F6CBD";

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(PathData))]
    [NotifyPropertyChangedFor(nameof(DashArrayString))]
    private double _thickness = 2;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(PathData))]
    private bool _isVertical;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(PathData))]
    private DividerStyle _style = DividerStyle.Straight;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(PathData))]
    private double _waveAmplitude = 6.0;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(PathData))]
    private double _waveFrequency = 4.0;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(DashArrayString))]
    private LineDashStyle _dashStyle = LineDashStyle.Solid;

    public override ElementKind Kind => ElementKind.Divider;
    public override string DisplayName => $"Divider ({Style})";

    public string PathData => SvgShapeHelper.GenerateDividerSvgPath(Style, Width, Height > 0 ? Height : 16, WaveAmplitude, WaveFrequency, IsVertical);
    public string? DashArrayString => SvgShapeHelper.GetDashArray(DashStyle, Thickness);

    protected override void OnPropertyChanged(PropertyChangedEventArgs e)
    {
        base.OnPropertyChanged(e);
        if (e.PropertyName is nameof(Width) or nameof(Height) or nameof(Thickness) or nameof(Style) or nameof(WaveAmplitude) or nameof(WaveFrequency) or nameof(IsVertical) or nameof(DashStyle))
        {
            OnPropertyChanged(nameof(PathData));
            OnPropertyChanged(nameof(DashArrayString));
        }
    }

    public override PdfElementBase ToModel()
    {
        return new PdfDividerElement
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
            ColorHex = ColorHex,
            Thickness = Thickness,
            IsVertical = IsVertical,
            Style = Style,
            WaveAmplitude = WaveAmplitude,
            WaveFrequency = WaveFrequency,
            DashStyle = DashStyle
        };
    }

    public override void LoadFromModel(PdfElementBase model)
    {
        if (model is PdfDividerElement div)
        {
            Id = div.Id;
            X = div.X;
            Y = div.Y;
            Width = div.Width;
            Height = div.Height;
            ZIndex = div.ZIndex;
            Rotation = div.Rotation;
            Opacity = div.Opacity;
            IsLocked = div.IsLocked;

            ColorHex = div.ColorHex;
            Thickness = div.Thickness;
            IsVertical = div.IsVertical;
            Style = div.Style;
            WaveAmplitude = div.WaveAmplitude;
            WaveFrequency = div.WaveFrequency;
            DashStyle = div.DashStyle;
        }
    }
}

