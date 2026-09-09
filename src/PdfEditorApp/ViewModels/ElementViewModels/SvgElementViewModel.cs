using System;
using System.IO;
using System.Threading.Tasks;
using System.Text.RegularExpressions;
using Avalonia.Media.Imaging;
using Avalonia.Threading;
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

    /// <summary>
    /// Coalesces preview rasterizations; see <see cref="RefreshSvgPreview"/>.
    /// </summary>
    private readonly UiDebouncer _previewDebouncer;

    /// <summary>Guards against a stale off-thread rasterize overwriting a newer one.</summary>
    private int _previewGeneration;

    public SvgElementViewModel()
    {
        _previewDebouncer = new UiDebouncer(PreviewDebounceMs, RefreshSvgPreview);

        Width = 160;
        Height = 160;
        RefreshSvgPreview();
    }

    /// <summary>Quiet period before an SVG preview is re-rasterized.</summary>
    private const int PreviewDebounceMs = 180;

    // These are bound to editable inputs — SvgSource to a multi-line TextBox, TintColorHex to a
    // colour picker — so they fired on every keystroke and every picker tick. Each one used to
    // run RefreshSvgPreview synchronously on the UI thread, and that generates a complete PDF
    // with QuestPDF, reparses it with PdfPig and rasterizes it with Skia at up to 2048px. That
    // is hundreds of milliseconds of UI-thread block per character typed.
    partial void OnSvgSourceChanged(string value) => _previewDebouncer.Request();
    partial void OnPresetNameChanged(string? value) => _previewDebouncer.Request();
    partial void OnTintColorHexChanged(string? value) => _previewDebouncer.Request();

    /// <summary>
    /// Re-rasterizes the SVG preview, doing the expensive work on a background thread.
    /// </summary>
    /// <remarks>
    /// Kept public and synchronous-looking because callers treat it as "refresh now"; the
    /// rasterize itself is offloaded per section 1 of
    /// .agents/rules/performance_and_zero_lag_mandate.md, which lists Skia rendering and
    /// QuestPDF generation as things that must never run on the UI thread.
    /// </remarks>
    public void RefreshSvgPreview()
    {
        UpdatePathGeometry();

        if (string.IsNullOrWhiteSpace(SvgSource))
        {
            PreviewBitmap = null;
            return;
        }

        // Snapshot the inputs on the calling thread so the background render never reads
        // view model state that the user is still editing.
        string svgData = SvgSource;
        if (!string.IsNullOrWhiteSpace(TintColorHex))
        {
            svgData = svgData.Replace("currentColor", TintColorHex);
        }

        double width = Width;
        double height = Height;
        int generation = ++_previewGeneration;

        _ = Task.Run(() =>
        {
            Bitmap? bmp;
            try
            {
                bmp = PdfPageRenderer.RenderSvgToBitmap(svgData, width, height);
            }
            catch
            {
                return; // Retain existing or fallback
            }

            if (bmp == null) return;

            Dispatcher.UIThread.Post(() =>
            {
                // A newer edit already superseded this render; drop it rather than flicker
                // backwards, and dispose the bitmap we are throwing away.
                if (generation != _previewGeneration)
                {
                    bmp.Dispose();
                    return;
                }

                PreviewBitmap = bmp;
            }, DispatcherPriority.Background);
        });
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
        _previewDebouncer.Dispose();
        _previousPreviewBitmap?.Dispose();
        _previousPreviewBitmap = null;
        PreviewBitmap?.Dispose();
        PreviewBitmap = null;
    }
}
