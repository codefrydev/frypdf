using System;
using System.IO;
using System.Text.RegularExpressions;
using Avalonia.Media.Imaging;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using PdfEditorApp.Core.Models;
using PdfEditorApp.Core.Models.Elements;
using PdfEditorApp.Models;
using PdfEditorApp.Services;
using PdfEditorApp.Services.Tools.Core;

namespace PdfEditorApp.ViewModels.ElementViewModels;

public partial class SvgElementViewModel : ElementViewModelBase, IDisposable
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

    [ObservableProperty]
    private Bitmap? _previewBitmap;

    private Bitmap? _previousPreviewBitmap;

    /// <summary>
    /// Disposes the outgoing native bitmap whenever a new SVG is rasterized,
    /// preventing unmanaged Skia memory leaks per AGENTS.md Section 4.E.
    /// </summary>
    partial void OnPreviewBitmapChanged(Bitmap? value)
    {
        if (_previousPreviewBitmap != null && _previousPreviewBitmap != value)
        {
            _previousPreviewBitmap.Dispose();
        }
        _previousPreviewBitmap = value;
    }

    public override ElementKind Kind => ElementKind.Svg;
    public override string DisplayName => !string.IsNullOrEmpty(PresetName) ? $"SVG ({PresetName})" : (!string.IsNullOrEmpty(FilePath) ? Path.GetFileName(FilePath) : "Vector SVG");

    public SvgElementViewModel()
    {
        Width = 160;
        Height = 160;
        RefreshSvgPreview();
    }

    partial void OnSvgSourceChanged(string value) => RefreshSvgPreview();
    partial void OnPresetNameChanged(string? value) => RefreshSvgPreview();
    partial void OnTintColorHexChanged(string? value) => RefreshSvgPreview();

    public void RefreshSvgPreview()
    {
        UpdatePathGeometry();

        if (string.IsNullOrWhiteSpace(SvgSource))
        {
            PreviewBitmap = null;
            return;
        }

        try
        {
            string svgData = SvgSource;
            if (!string.IsNullOrWhiteSpace(TintColorHex))
            {
                svgData = svgData.Replace("currentColor", TintColorHex);
            }

            var bmp = PdfPageRenderer.RenderSvgToBitmap(svgData, Width, Height);
            if (bmp != null)
            {
                PreviewBitmap = bmp;
            }
        }
        catch
        {
            // Retain existing or fallback
        }
    }

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
        RefreshSvgPreview();
    }

    public void LoadFromFile(string path)
    {
        if (File.Exists(path))
        {
            FilePath = path;
            SvgSource = File.ReadAllText(path);
            PresetName = Path.GetFileNameWithoutExtension(path);
            RefreshSvgPreview();
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

            SvgSource = !string.IsNullOrWhiteSpace(svg.SvgSource)
                ? svg.SvgSource
                : (!string.IsNullOrWhiteSpace(svg.PresetName)
                    ? SvgOrnamentLibrary.GetSvg(svg.PresetName, svg.TintColorHex)
                    : SvgOrnamentLibrary.GetGaneshaCrestSvg());
            FilePath = svg.FilePath;
            TintColorHex = svg.TintColorHex;
            PresetName = svg.PresetName;
            KeepAspectRatio = svg.KeepAspectRatio;
            CornerRadius = svg.CornerRadius;
            BorderColorHex = svg.BorderColorHex;
            BorderThickness = svg.BorderThickness;

            RefreshSvgPreview();
        }
    }

    public void Dispose()
    {
        _previousPreviewBitmap?.Dispose();
        _previousPreviewBitmap = null;
        PreviewBitmap?.Dispose();
        PreviewBitmap = null;
    }
}
