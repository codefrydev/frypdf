using System;
using System.IO;
using System.Text.RegularExpressions;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using PdfEditorApp.Models;
using PdfEditorApp.Models.Elements;
using PdfEditorApp.Services;

namespace PdfEditorApp.ViewModels.ElementViewModels;

public partial class SvgElementViewModel : ElementViewModelBase
{
    [ObservableProperty]
    private string _svgSource = SvgOrnamentLibrary.GetGaneshaCrestSvg();

    [ObservableProperty]
    private string? _filePath;

    [ObservableProperty]
    private string? _tintColorHex;

    [ObservableProperty]
    private string? _presetName = "GaneshaCrest";

    [ObservableProperty]
    private bool _keepAspectRatio = true;

    [ObservableProperty]
    private double _cornerRadius = 0;

    [ObservableProperty]
    private string? _borderColorHex;

    [ObservableProperty]
    private double _borderThickness = 0;

    [ObservableProperty]
    private string _pathGeometryData = "";

    public override ElementKind Kind => ElementKind.Svg;
    public override string DisplayName => !string.IsNullOrEmpty(PresetName) ? $"SVG ({PresetName})" : (!string.IsNullOrEmpty(FilePath) ? Path.GetFileName(FilePath) : "Vector SVG");

    public SvgElementViewModel()
    {
        Width = 160;
        Height = 160;
        UpdatePathGeometry();
    }

    partial void OnSvgSourceChanged(string value) => UpdatePathGeometry();
    partial void OnPresetNameChanged(string? value) => UpdatePathGeometry();
    partial void OnTintColorHexChanged(string? value) => UpdatePathGeometry();

    public void UpdatePathGeometry()
    {
        if (string.IsNullOrWhiteSpace(SvgSource))
        {
            PathGeometryData = "";
            return;
        }

        // Extract the primary or combined path 'd' attributes from the SVG markup for Avalonia Vector rendering
        var match = Regex.Match(SvgSource, @"<path[^>]*\sd=[""']([^""']+)[""']", RegexOptions.IgnoreCase);
        if (match.Success)
        {
            PathGeometryData = match.Groups[1].Value;
        }
        else
        {
            PathGeometryData = "M 10,10 L 90,10 L 90,90 L 10,90 Z";
        }
    }

    [RelayCommand]
    public void ApplyPreset(string preset)
    {
        PresetName = preset;
        SvgSource = SvgOrnamentLibrary.GetSvg(preset, TintColorHex);
        FilePath = null;
    }

    public void LoadFromFile(string path)
    {
        if (File.Exists(path))
        {
            FilePath = path;
            SvgSource = File.ReadAllText(path);
            PresetName = Path.GetFileNameWithoutExtension(path);
        }
    }

    public override PdfElementBase ToModel()
    {
        return new PdfSvgElement
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
            SvgSource = SvgSource,
            FilePath = FilePath,
            TintColorHex = TintColorHex,
            PresetName = PresetName,
            KeepAspectRatio = KeepAspectRatio,
            CornerRadius = CornerRadius,
            BorderColorHex = BorderColorHex,
            BorderThickness = BorderThickness
        };
    }

    public override void LoadFromModel(PdfElementBase model)
    {
        if (model is PdfSvgElement svg)
        {
            Id = svg.Id;
            X = svg.X;
            Y = svg.Y;
            Width = svg.Width;
            Height = svg.Height;
            ZIndex = svg.ZIndex;
            Rotation = svg.Rotation;
            Opacity = svg.Opacity;
            IsLocked = svg.IsLocked;

            SvgSource = svg.SvgSource;
            FilePath = svg.FilePath;
            TintColorHex = svg.TintColorHex;
            PresetName = svg.PresetName;
            KeepAspectRatio = svg.KeepAspectRatio;
            CornerRadius = svg.CornerRadius;
            BorderColorHex = svg.BorderColorHex;
            BorderThickness = svg.BorderThickness;

            UpdatePathGeometry();
        }
    }
}
