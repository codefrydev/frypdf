using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using Avalonia.Threading;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using PdfEditorApp.Core.Analysis;
using PdfEditorApp.Core.Models;
using PdfEditorApp.Core.Models.Elements;
using PdfEditorApp.ViewModels.ElementViewModels;

namespace PdfEditorApp.ViewModels.FryPdfViewer;

/// <summary>
/// Individual interactive bar/data item with animation interpolation and hover details.
/// </summary>
public partial class InteractiveChartItem : ObservableObject
{
    [ObservableProperty]
    private string _category = "";

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(AnimatedValue))]
    [NotifyPropertyChangedFor(nameof(AnimatedHeightPx))]
    private double _targetValue;

    [ObservableProperty]
    private string _valueLabel = "";

    [ObservableProperty]
    private string _colorHex = "#0F6CBD";

    [ObservableProperty]
    private double _percentage;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(AnimatedValue))]
    [NotifyPropertyChangedFor(nameof(AnimatedHeightPx))]
    [NotifyPropertyChangedFor(nameof(AnimatedWidthPx))]
    private double _animationProgress = 1.0;

    public double AnimatedValue => TargetValue * AnimationProgress;

    public double MaxPlotHeight { get; set; } = 150.0;
    public double MaxPlotWidth { get; set; } = 260.0;
    public double MaxValue { get; set; } = 1.0;

    public double AnimatedHeightPx
    {
        get
        {
            if (MaxValue <= 0) return 6;
            double ratio = Math.Clamp(AnimatedValue / MaxValue, 0.0, 1.0);
            return Math.Max(6, ratio * MaxPlotHeight);
        }
    }

    public double AnimatedWidthPx
    {
        get
        {
            if (MaxValue <= 0) return 8;
            double ratio = Math.Clamp(AnimatedValue / MaxValue, 0.0, 1.0);
            return Math.Max(8, ratio * MaxPlotWidth);
        }
    }

    public string ProgressPercentageString => $"{Percentage:F1}%";
    public string TooltipText => $"{Category}: {ValueLabel} ({Percentage:F1}%)";
}

/// <summary>
/// Interactive ViewModel wrapping a chart in the .frypdf document.
/// Provides dynamic capabilities impossible in static binary PDFs:
/// - Smooth animated chart transitions & entry animations
/// - Interactive hover tooltips showing category, value, and share %
/// - Multi-chart support: Column Bar, Horizontal Bar, Donut/Pie, Line/Area, and Gauge
/// - Instant toggle between visual chart and underlying data table
/// - Replay animation trigger for presentations
/// </summary>
public partial class InteractiveChartViewModel : ElementViewModelBase
{
    private DispatcherTimer? _animationTimer;
    private int _animationStep = 0;
    private const int TotalAnimationSteps = 24;
    private PdfChartElement? _sourceModel;

    public override ElementKind Kind => ElementKind.Chart;
    public override string DisplayName => "Interactive Chart";

    [ObservableProperty]
    private string _title = "Chart Overview";

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(ChartTypeDescription))]
    [NotifyPropertyChangedFor(nameof(IsBarColumn))]
    [NotifyPropertyChangedFor(nameof(IsHorizontalBar))]
    [NotifyPropertyChangedFor(nameof(IsDonutPie))]
    [NotifyPropertyChangedFor(nameof(IsLineOrArea))]
    [NotifyPropertyChangedFor(nameof(IsGaugeProgress))]
    private ChartType _chartType = ChartType.BarColumn;

    [ObservableProperty]
    private string _backgroundColorHex = "#FFFFFF";

    [ObservableProperty]
    private string _borderColorHex = "#E2E8F0";

    [ObservableProperty]
    private bool _isShowingDataTable = false;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(CenterSummaryValue))]
    [NotifyPropertyChangedFor(nameof(CenterSummaryLabel))]
    private InteractiveChartItem? _hoveredItem;

    [ObservableProperty]
    private double _animationProgress = 1.0;

    [ObservableProperty]
    private Avalonia.Media.Imaging.Bitmap? _chartBitmap;

    public ObservableCollection<InteractiveChartItem> Items { get; } = new();

    public string ChartTypeDescription => ChartType.ToString();
    public bool IsBarColumn => ChartType == ChartType.BarColumn;
    public bool IsHorizontalBar => ChartType == ChartType.HorizontalBar;
    public bool IsDonutPie => ChartType == ChartType.DonutPie;
    public bool IsLineOrArea => ChartType == ChartType.Line || ChartType == ChartType.Area || ChartType == ChartType.SmoothLine;
    public bool IsGaugeProgress => ChartType == ChartType.GaugeProgress;

    public double TotalSum => Items.Sum(i => i.TargetValue);
    public double MaxItemValue => Items.Count > 0 ? Math.Max(1.0, Items.Max(i => i.TargetValue)) : 1.0;

    public string CenterSummaryValue => HoveredItem != null
        ? HoveredItem.ValueLabel
        : (TotalSum >= 1000 ? $"${TotalSum / 1000.0:F1}B" : (TotalSum > 0 ? $"${TotalSum:F1}M" : "100%"));

    public string CenterSummaryLabel => HoveredItem != null
        ? HoveredItem.Category
        : (IsDonutPie ? "Total ARR" : (IsGaugeProgress ? "Attainment" : "Portfolio"));

    public InteractiveChartViewModel()
    {
        Width = 460;
        Height = 260;
        ZIndex = 400;
    }

    public InteractiveChartViewModel(ChartElementViewModel chartVm)
    {
        Id = chartVm.Id;
        X = chartVm.X;
        Y = chartVm.Y;
        Width = chartVm.Width;
        Height = chartVm.Height;
        ZIndex = chartVm.ZIndex;
        Rotation = chartVm.Rotation;
        Opacity = chartVm.Opacity;
        Title = chartVm.Title;
        ChartType = chartVm.ChartType;
        BackgroundColorHex = chartVm.BackgroundColorHex;
        BorderColorHex = chartVm.BorderColorHex;
        ChartBitmap = chartVm.ChartBitmap;

        PopulateFromBars(chartVm.Bars.Select(b => (b.Category, b.Value, b.ValueLabel, b.ColorHex)));
    }

    public InteractiveChartViewModel(PdfChartElement chartModel)
    {
        _sourceModel = chartModel;
        PopulateBaseProperties(chartModel);
        LoadFromChartModel(chartModel);
    }

    private void LoadFromChartModel(PdfChartElement chartModel)
    {
        Title = chartModel.Title;
        ChartType = chartModel.ChartType;
        BackgroundColorHex = chartModel.BackgroundColorHex;
        BorderColorHex = chartModel.BorderColorHex;

        var palette = LiveChartsRenderer.GetPaletteHexColors(chartModel.Palette);
        var barData = new List<(string Category, double Value, string ValueLabel, string ColorHex)>();

        var categories = chartModel.Categories.Count > 0 ? chartModel.Categories : new List<string> { "A", "B", "C", "D" };

        for (int i = 0; i < categories.Count; i++)
        {
            double val = (i < chartModel.Values.Count) ? chartModel.Values[i] : 0.0;
            string label = (i < chartModel.ValueLabels.Count && !string.IsNullOrWhiteSpace(chartModel.ValueLabels[i]))
                ? chartModel.ValueLabels[i]
                : val.ToString("N0");
            string color = (i < chartModel.BarColorsHex.Count && !string.IsNullOrWhiteSpace(chartModel.BarColorsHex[i]))
                ? chartModel.BarColorsHex[i]
                : palette[i % palette.Count];
            barData.Add((categories[i], val, label, color));
        }

        PopulateFromBars(barData);

        if (IsLineOrArea || ChartType == ChartType.Radar || ChartType == ChartType.PolarArea || ChartType == ChartType.ScatterPlot)
        {
            try
            {
                var pngBytes = LiveChartsRenderer.RenderChartToPngBytes(chartModel, (int)Width, (int)Height, 2.0f);
                if (pngBytes.Length > 0)
                {
                    using var ms = new System.IO.MemoryStream(pngBytes);
                    var oldBmp = ChartBitmap;
                    ChartBitmap = new Avalonia.Media.Imaging.Bitmap(ms);
                    oldBmp?.Dispose();
                }
            }
            catch
            {
                // Fallback gracefully in headless test environments
            }
        }
    }

    public override PdfElementBase ToModel()
    {
        var model = _sourceModel ?? new PdfChartElement();
        CopyBasePropertiesTo(model);
        model.Title = Title;
        model.ChartType = ChartType;
        model.BackgroundColorHex = BackgroundColorHex;
        model.BorderColorHex = BorderColorHex;
        model.Categories = Items.Select(i => i.Category).ToList();
        model.Values = Items.Select(i => i.TargetValue).ToList();
        model.ValueLabels = Items.Select(i => i.ValueLabel).ToList();
        model.BarColorsHex = Items.Select(i => i.ColorHex).ToList();
        return model;
    }

    public override void LoadFromModel(PdfElementBase model)
    {
        if (model is PdfChartElement chart)
        {
            _sourceModel = chart;
            PopulateBaseProperties(chart);
            LoadFromChartModel(chart);
        }
    }

    private void PopulateFromBars(IEnumerable<(string Category, double Value, string ValueLabel, string ColorHex)> bars)
    {
        Items.Clear();
        var barList = bars.ToList();
        double sum = barList.Sum(b => b.Value);
        double max = barList.Count > 0 ? Math.Max(1.0, barList.Max(b => b.Value)) : 1.0;
        double plotHeight = Math.Max(60, Height - 80);
        double plotWidth = Math.Max(80, Width - 190);

        foreach (var (cat, val, label, color) in barList)
        {
            double pct = sum > 0 ? (val / sum) * 100.0 : 0.0;
            Items.Add(new InteractiveChartItem
            {
                Category = cat,
                TargetValue = val,
                ValueLabel = string.IsNullOrWhiteSpace(label) ? val.ToString("N0") : label,
                ColorHex = color,
                Percentage = pct,
                MaxPlotHeight = plotHeight,
                MaxPlotWidth = plotWidth,
                MaxValue = max,
                AnimationProgress = 1.0
            });
        }
    }

    /// <summary>
    /// Smoothly triggers or replays the chart entry animation.
    /// </summary>
    [RelayCommand]
    public void ReplayAnimation()
    {
        _animationTimer?.Stop();
        _animationStep = 0;
        AnimationProgress = 0.0;

        foreach (var item in Items)
        {
            item.AnimationProgress = 0.0;
        }

        _animationTimer = new DispatcherTimer
        {
            Interval = TimeSpan.FromMilliseconds(16) // ~60 FPS
        };

        _animationTimer.Tick += (s, e) =>
        {
            _animationStep++;
            double t = (double)_animationStep / TotalAnimationSteps;

            // Ease-out cubic: 1 - (1 - t)^3
            double eased = 1.0 - Math.Pow(1.0 - t, 3.0);
            AnimationProgress = Math.Clamp(eased, 0.0, 1.0);

            foreach (var item in Items)
            {
                item.AnimationProgress = AnimationProgress;
            }

            if (_animationStep >= TotalAnimationSteps)
            {
                _animationTimer.Stop();
                AnimationProgress = 1.0;
                foreach (var item in Items)
                {
                    item.AnimationProgress = 1.0;
                }
            }
        };

        _animationTimer.Start();
    }

    /// <summary>
    /// Toggles between visual chart and underlying tabular data view.
    /// </summary>
    [RelayCommand]
    public void ToggleDataTable()
    {
        IsShowingDataTable = !IsShowingDataTable;
    }

    [RelayCommand]
    public void SetHoveredItem(InteractiveChartItem? item)
    {
        HoveredItem = item;
    }
}
