using System;
using CommunityToolkit.Mvvm.ComponentModel;
using PdfEditorApp.Core.Models;
using PdfEditorApp.Core.Models.Elements;
using PdfEditorApp.Models;

namespace PdfEditorApp.ViewModels.ElementViewModels;

public partial class MeasurementElementViewModel : ElementViewModelBase
{
    [ObservableProperty]
    private RulerUnit _unit = RulerUnit.Points;

    [ObservableProperty]
    private double _scaleFactor = 1.0;

    [ObservableProperty]
    private string _customLabel = "";

    [ObservableProperty]
    private string _strokeColorHex = "#DC2626";

    [ObservableProperty]
    private double _strokeThickness = 1.5;

    [ObservableProperty]
    private double _arrowSize = 6.0;

    [ObservableProperty]
    private double _extensionLineLength = 10.0;

    [ObservableProperty]
    private double _fontSize = 10.0;

    public override ElementKind Kind => ElementKind.Measurement;
    public override string DisplayName => "Measurement Dimension";

    public string FormattedDistance => GetFormattedDistance();

    partial void OnUnitChanged(RulerUnit value) => OnPropertyChanged(nameof(FormattedDistance));
    partial void OnCustomLabelChanged(string value) => OnPropertyChanged(nameof(FormattedDistance));

    public double CalculateDistance()
    {
        return Width;
    }

    public string GetFormattedDistance()
    {
        if (!string.IsNullOrWhiteSpace(CustomLabel))
            return CustomLabel;

        double distancePts = CalculateDistance();
        return Unit switch
        {
            RulerUnit.Inches => $"{distancePts / 72.0:F2} in",
            RulerUnit.Millimeters => $"{distancePts * 25.4 / 72.0:F1} mm",
            _ => $"{distancePts:F1} pt"
        };
    }

    public override PdfElementBase ToModel()
    {
        return new PdfMeasurementElement
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
            Unit = Unit,
            ScaleFactor = ScaleFactor,
            CustomLabel = CustomLabel,
            StrokeColorHex = StrokeColorHex,
            StrokeThickness = StrokeThickness,
            ArrowSize = ArrowSize,
            ExtensionLineLength = ExtensionLineLength,
            FontSize = FontSize
        };
    }

    public override void LoadFromModel(PdfElementBase model)
    {
        if (model is PdfMeasurementElement m)
        {
            Id = m.Id;
            X = m.X;
            Y = m.Y;
            Width = m.Width;
            Height = m.Height;
            ZIndex = m.ZIndex;
            Rotation = m.Rotation;
            Opacity = m.Opacity;
            IsLocked = m.IsLocked;

            Unit = m.Unit;
            ScaleFactor = m.ScaleFactor;
            CustomLabel = m.CustomLabel;
            StrokeColorHex = m.StrokeColorHex;
            StrokeThickness = m.StrokeThickness;
            ArrowSize = m.ArrowSize;
            ExtensionLineLength = m.ExtensionLineLength;
            FontSize = m.FontSize;
        }
    }
}
