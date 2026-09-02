using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using LiveChartsCore;
using LiveChartsCore.Defaults;
using LiveChartsCore.Drawing;
using LiveChartsCore.Measure;
using LiveChartsCore.SkiaSharpView;
using LiveChartsCore.SkiaSharpView.Painting;
using LiveChartsCore.SkiaSharpView.VisualElements;
using PdfEditorApp.Core.Analysis;
using PdfEditorApp.Models;
using PdfEditorApp.Models.Elements;
using SkiaSharp;

namespace PdfEditorApp.ViewModels.ElementViewModels;

public class ChartBarItem : ObservableObject
{
    private string _category = "Q1";
    public string Category
    {
        get => _category;
        set => SetProperty(ref _category, value);
    }

    private double _value = 1.0;
    public double Value
    {
        get => _value;
        set
        {
            if (SetProperty(ref _value, value))
            {
                OnPropertyChanged(nameof(BarHeightPx));
            }
        }
    }

    private string _valueLabel = "$1.0B";
    public string ValueLabel
    {
        get => _valueLabel;
        set => SetProperty(ref _valueLabel, value);
    }

    private string _colorHex = "#0F6CBD";
    public string ColorHex
    {
        get => _colorHex;
        set => SetProperty(ref _colorHex, value);
    }

    public double BarHeightPx => Math.Max(10, Value * 35);
}

public partial class ChartElementViewModel : ElementViewModelBase
{
    [ObservableProperty]
    private string _title = "Revenue Growth (Q1-Q4)";

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(IsBarColumn))]
    [NotifyPropertyChangedFor(nameof(IsHorizontalBar))]
    [NotifyPropertyChangedFor(nameof(IsLine))]
    [NotifyPropertyChangedFor(nameof(IsSmoothLine))]
    [NotifyPropertyChangedFor(nameof(IsArea))]
    [NotifyPropertyChangedFor(nameof(IsDonutPie))]
    [NotifyPropertyChangedFor(nameof(IsStackedBar))]
    [NotifyPropertyChangedFor(nameof(IsStackedHorizontalBar))]
    [NotifyPropertyChangedFor(nameof(IsRadar))]
    [NotifyPropertyChangedFor(nameof(IsPolarArea))]
    [NotifyPropertyChangedFor(nameof(IsFunnel))]
    [NotifyPropertyChangedFor(nameof(IsWaterfall))]
    [NotifyPropertyChangedFor(nameof(IsGaugeProgress))]
    [NotifyPropertyChangedFor(nameof(IsStepLine))]
    [NotifyPropertyChangedFor(nameof(IsPyramid))]
    [NotifyPropertyChangedFor(nameof(IsScatterPlot))]
    [NotifyPropertyChangedFor(nameof(IsCandlestick))]
    [NotifyPropertyChangedFor(nameof(IsCartesianChart))]
    [NotifyPropertyChangedFor(nameof(IsPieChart))]
    [NotifyPropertyChangedFor(nameof(IsPolarChart))]
    [NotifyPropertyChangedFor(nameof(ChartTypeDescription))]
    private ChartType _chartType = ChartType.BarColumn;

    public bool IsBarColumn => ChartType == ChartType.BarColumn;
    public bool IsHorizontalBar => ChartType == ChartType.HorizontalBar;
    public bool IsLine => ChartType == ChartType.Line;
    public bool IsSmoothLine => ChartType == ChartType.SmoothLine;
    public bool IsArea => ChartType == ChartType.Area;
    public bool IsDonutPie => ChartType == ChartType.DonutPie;
    public bool IsStackedBar => ChartType == ChartType.StackedBar;
    public bool IsStackedHorizontalBar => ChartType == ChartType.StackedHorizontalBar;
    public bool IsRadar => ChartType == ChartType.Radar;
    public bool IsPolarArea => ChartType == ChartType.PolarArea;
    public bool IsFunnel => ChartType == ChartType.Funnel;
    public bool IsWaterfall => ChartType == ChartType.Waterfall;
    public bool IsGaugeProgress => ChartType == ChartType.GaugeProgress;
    public bool IsStepLine => ChartType == ChartType.StepLine;
    public bool IsPyramid => ChartType == ChartType.Pyramid;
    public bool IsScatterPlot => ChartType == ChartType.ScatterPlot;
    public bool IsCandlestick => ChartType == ChartType.Candlestick;

    public bool IsPieChart => ChartType == ChartType.DonutPie || ChartType == ChartType.GaugeProgress;
    public bool IsPolarChart => ChartType == ChartType.Radar || ChartType == ChartType.PolarArea;
    public bool IsCartesianChart => !IsPieChart && !IsPolarChart;

    public string ChartTypeDescription => ChartType.ToString();

    [ObservableProperty]
    private ChartPalette _palette = ChartPalette.CorporateBlue;

    [ObservableProperty]
    private ChartLegendPosition _legendPosition = ChartLegendPosition.Top;

    [ObservableProperty]
    private bool _showDataLabels = true;

    [ObservableProperty]
    private bool _showGridlines = true;

    [ObservableProperty]
    private double _donutHoleRatio = 0.6;

    [ObservableProperty]
    private double _curveSmoothness = 0.65;

    [ObservableProperty]
    private double _strokeThickness = 3.0;

    [ObservableProperty]
    private string _backgroundColorHex = "#FAFAFA";

    [ObservableProperty]
    private string _borderColorHex = "#E2E8F0";

    public ObservableCollection<ChartBarItem> Bars { get; } = new();

    // LiveCharts2 Series & Axes
    [ObservableProperty]
    private ISeries[] _cartesianSeries = Array.Empty<ISeries>();

    [ObservableProperty]
    private ISeries[] _pieSeries = Array.Empty<ISeries>();

    [ObservableProperty]
    private ISeries[] _polarSeries = Array.Empty<ISeries>();

    [ObservableProperty]
    private Axis[] _xAxes = Array.Empty<Axis>();

    [ObservableProperty]
    private Axis[] _yAxes = Array.Empty<Axis>();

    [ObservableProperty]
    private PolarAxis[] _angleAxes = Array.Empty<PolarAxis>();

    [ObservableProperty]
    private PolarAxis[] _radiusAxes = Array.Empty<PolarAxis>();

    [ObservableProperty]
    private LiveChartsCore.Measure.LegendPosition _liveLegendPosition = LiveChartsCore.Measure.LegendPosition.Top;

    [ObservableProperty]
    private double _pieMaxAngle = 360;

    [ObservableProperty]
    private double _pieInitialRotation = -90;

    [ObservableProperty]
    private Avalonia.Media.Imaging.Bitmap? _chartBitmap;

    private Avalonia.Media.Imaging.Bitmap? _previousChartBitmap;

    /// <summary>Disposes the outgoing bitmap whenever a new one is rasterized — <see cref="ChartBitmap"/>
    /// is re-rasterized on nearly every property change with no other lifecycle owner, so without this
    /// the old native bitmap leaks.</summary>
    partial void OnChartBitmapChanged(Avalonia.Media.Imaging.Bitmap? value)
    {
        if (_previousChartBitmap != null && _previousChartBitmap != value)
        {
            _previousChartBitmap.Dispose();
        }
        _previousChartBitmap = value;
    }

    public override ElementKind Kind => ElementKind.Chart;
    public override string DisplayName => Title;

    public ChartElementViewModel()
    {
        LiveChartsRenderer.EnsureConfigured();

        // Default sample bars
        Bars.Add(new ChartBarItem { Category = "Q1", Value = 1.2, ValueLabel = "$1.2B", ColorHex = "#93C5FD" });
        Bars.Add(new ChartBarItem { Category = "Q2", Value = 1.8, ValueLabel = "$1.8B", ColorHex = "#60A5FA" });
        Bars.Add(new ChartBarItem { Category = "Q3", Value = 2.5, ValueLabel = "$2.5B", ColorHex = "#3B82F6" });
        Bars.Add(new ChartBarItem { Category = "Q4", Value = 3.1, ValueLabel = "$3.1B", ColorHex = "#0F6CBD" });

        Bars.CollectionChanged += (s, e) => { if (!_suppressChartUpdate) UpdateLiveChart(); };
        PropertyChanged += (s, e) =>
        {
            if (e.PropertyName == nameof(Width) || e.PropertyName == nameof(Height))
            {
                // Width/Height fire on every pointer-move during an interactive resize drag —
                // rasterizing a fresh chart bitmap on every one of those would make resize feel
                // like it's dragging through mud. Coalesce to roughly one rasterize per settle.
                RequestChartUpdate();
            }
        };
        UpdateLiveChart();
    }

    private bool _suppressChartUpdate;
    private CancellationTokenSource? _chartUpdateDebounceCts;

    private void RequestChartUpdate()
    {
        _chartUpdateDebounceCts?.Cancel();
        _chartUpdateDebounceCts = new CancellationTokenSource();
        var token = _chartUpdateDebounceCts.Token;

        _ = Task.Run(async () =>
        {
            try
            {
                await Task.Delay(120, token);
            }
            catch (TaskCanceledException)
            {
                return;
            }
            if (token.IsCancellationRequested) return;

            Avalonia.Threading.Dispatcher.UIThread.Post(() =>
            {
                if (!token.IsCancellationRequested)
                {
                    UpdateLiveChart();
                }
            });
        }, token);
    }

    partial void OnChartTypeChanged(ChartType value) => UpdateLiveChart();
    partial void OnPaletteChanged(ChartPalette value) => UpdateLiveChart();
    partial void OnLegendPositionChanged(ChartLegendPosition value) => UpdateLiveChart();
    partial void OnShowDataLabelsChanged(bool value) => UpdateLiveChart();
    partial void OnShowGridlinesChanged(bool value) => UpdateLiveChart();
    partial void OnDonutHoleRatioChanged(double value) => UpdateLiveChart();
    partial void OnCurveSmoothnessChanged(double value) => UpdateLiveChart();
    partial void OnStrokeThicknessChanged(double value) => UpdateLiveChart();
    partial void OnTitleChanged(string value) => UpdateLiveChart();

    [RelayCommand]
    public void SetChartType(string typeStr)
    {
        if (Enum.TryParse<ChartType>(typeStr, true, out var type))
        {
            ChartType = type;
        }
    }

    [RelayCommand]
    public void SetPalette(string paletteStr)
    {
        if (Enum.TryParse<ChartPalette>(paletteStr, true, out var pal))
        {
            Palette = pal;
        }
    }

    [RelayCommand]
    public void SetLegendPosition(string posStr)
    {
        if (Enum.TryParse<ChartLegendPosition>(posStr, true, out var pos))
        {
            LegendPosition = pos;
        }
    }

    [RelayCommand]
    public void AddDataPoint()
    {
        int nextNum = Bars.Count + 1;
        var palette = LiveChartsRenderer.GetPaletteHexColors(Palette);
        string color = palette[(nextNum - 1) % palette.Count];
        double val = Math.Round(1.0 + (nextNum * 0.5), 1);

        _suppressChartUpdate = true;
        Bars.Add(new ChartBarItem
        {
            Category = $"Q{nextNum}",
            Value = val,
            ValueLabel = $"${val:F1}B",
            ColorHex = color
        });
        _suppressChartUpdate = false;
        OnPropertyChanged(nameof(Bars));
        UpdateLiveChart();
    }

    [RelayCommand]
    public void RemoveDataPoint()
    {
        if (Bars.Count > 1)
        {
            _suppressChartUpdate = true;
            Bars.RemoveAt(Bars.Count - 1);
            _suppressChartUpdate = false;
            OnPropertyChanged(nameof(Bars));
            UpdateLiveChart();
        }
    }

    public void UpdateLiveChart()
    {
        var palette = LiveChartsRenderer.GetPaletteHexColors(Palette);
        LiveLegendPosition = LegendPosition switch
        {
            ChartLegendPosition.Top => LiveChartsCore.Measure.LegendPosition.Top,
            ChartLegendPosition.Bottom => LiveChartsCore.Measure.LegendPosition.Bottom,
            ChartLegendPosition.Left => LiveChartsCore.Measure.LegendPosition.Left,
            ChartLegendPosition.Right => LiveChartsCore.Measure.LegendPosition.Right,
            _ => LiveChartsCore.Measure.LegendPosition.Hidden
        };

        var categories = Bars.Select(b => b.Category).ToArray();
        var values = Bars.Select(b => b.Value).ToArray();

        // 1. Cartesian Series
        var cSeries = new List<ISeries>();
        switch (ChartType)
        {
            case ChartType.HorizontalBar:
            {
                var color = LiveChartsRenderer.ParseColor(Bars.FirstOrDefault()?.ColorHex ?? palette[0], SKColors.RoyalBlue);
                cSeries.Add(new RowSeries<double>
                {
                    Name = Title,
                    Values = values,
                    Fill = new SolidColorPaint(color),
                    Stroke = null,
                    DataLabelsPaint = ShowDataLabels ? new SolidColorPaint(SKColors.DarkSlateGray) : null
                });
                break;
            }

            case ChartType.StackedBar:
            {
                for (int i = 0; i < values.Length; i++)
                {
                    var color = LiveChartsRenderer.ParseColor(i < Bars.Count ? Bars[i].ColorHex : palette[i % palette.Count], SKColors.RoyalBlue);
                    cSeries.Add(new StackedColumnSeries<double>
                    {
                        Name = i < categories.Length ? categories[i] : $"Segment {i + 1}",
                        Values = new double[] { values[i] },
                        Fill = new SolidColorPaint(color),
                        Stroke = null
                    });
                }
                break;
            }

            case ChartType.StackedHorizontalBar:
            {
                var color = LiveChartsRenderer.ParseColor(Bars.FirstOrDefault()?.ColorHex ?? palette[0], SKColors.RoyalBlue);
                cSeries.Add(new StackedRowSeries<double>
                {
                    Name = Title,
                    Values = values,
                    Fill = new SolidColorPaint(color),
                    Stroke = null
                });
                break;
            }

            case ChartType.Line:
            {
                var color = LiveChartsRenderer.ParseColor(Bars.FirstOrDefault()?.ColorHex ?? palette[0], SKColors.RoyalBlue);
                cSeries.Add(new LineSeries<double>
                {
                    Name = Title,
                    Values = values,
                    Stroke = new SolidColorPaint(color, (float)StrokeThickness),
                    Fill = null,
                    GeometrySize = 7,
                    GeometryStroke = new SolidColorPaint(color, 2),
                    GeometryFill = new SolidColorPaint(SKColors.White),
                    LineSmoothness = 0.0
                });
                break;
            }

            case ChartType.SmoothLine:
            {
                var color = LiveChartsRenderer.ParseColor(Bars.FirstOrDefault()?.ColorHex ?? palette[0], SKColors.RoyalBlue);
                cSeries.Add(new LineSeries<double>
                {
                    Name = Title,
                    Values = values,
                    Stroke = new SolidColorPaint(color, (float)StrokeThickness),
                    Fill = null,
                    GeometrySize = 8,
                    GeometryStroke = new SolidColorPaint(color, 2),
                    GeometryFill = new SolidColorPaint(SKColors.White),
                    LineSmoothness = (float)CurveSmoothness
                });
                break;
            }

            case ChartType.Area:
            {
                var color = LiveChartsRenderer.ParseColor(Bars.FirstOrDefault()?.ColorHex ?? palette[0], SKColors.RoyalBlue);
                var fillColor = color.WithAlpha(65);
                cSeries.Add(new LineSeries<double>
                {
                    Name = Title,
                    Values = values,
                    Stroke = new SolidColorPaint(color, (float)StrokeThickness),
                    Fill = new SolidColorPaint(fillColor),
                    GeometrySize = 7,
                    GeometryStroke = new SolidColorPaint(color, 2),
                    GeometryFill = new SolidColorPaint(SKColors.White),
                    LineSmoothness = (float)CurveSmoothness
                });
                break;
            }

            case ChartType.StepLine:
            {
                var color = LiveChartsRenderer.ParseColor(Bars.FirstOrDefault()?.ColorHex ?? palette[0], SKColors.RoyalBlue);
                cSeries.Add(new StepLineSeries<double>
                {
                    Name = Title,
                    Values = values,
                    Stroke = new SolidColorPaint(color, (float)StrokeThickness),
                    Fill = null,
                    GeometrySize = 7,
                    GeometryStroke = new SolidColorPaint(color, 2),
                    GeometryFill = new SolidColorPaint(SKColors.White)
                });
                break;
            }

            case ChartType.ScatterPlot:
            {
                var color = LiveChartsRenderer.ParseColor(Bars.FirstOrDefault()?.ColorHex ?? palette[0], SKColors.RoyalBlue);
                var points = new List<ObservablePoint>();
                for (int i = 0; i < values.Length; i++)
                {
                    points.Add(new ObservablePoint(i + 1, values[i]));
                }
                cSeries.Add(new ScatterSeries<ObservablePoint>
                {
                    Name = Title,
                    Values = points,
                    Stroke = new SolidColorPaint(color, 2),
                    Fill = new SolidColorPaint(color.WithAlpha(180)),
                    GeometrySize = 12
                });
                break;
            }

            case ChartType.Candlestick:
            {
                var financialPoints = new List<FinancialPoint>();
                for (int i = 0; i < values.Length; i++)
                {
                    double close = values[i];
                    double open = i > 0 ? values[i - 1] : close * 0.95;
                    double high = Math.Max(open, close) * 1.08;
                    double low = Math.Min(open, close) * 0.92;
                    financialPoints.Add(new FinancialPoint(new DateTime(2026, 1, 1).AddDays(i), high, open, close, low));
                }
                cSeries.Add(new CandlesticksSeries<FinancialPoint>
                {
                    Name = Title,
                    Values = financialPoints,
                    UpStroke = new SolidColorPaint(SKColors.ForestGreen, 2),
                    UpFill = new SolidColorPaint(SKColors.ForestGreen.WithAlpha(200)),
                    DownStroke = new SolidColorPaint(SKColors.IndianRed, 2),
                    DownFill = new SolidColorPaint(SKColors.IndianRed.WithAlpha(200))
                });
                break;
            }

            case ChartType.Waterfall:
            case ChartType.Funnel:
            case ChartType.Pyramid:
            case ChartType.BarColumn:
            default:
            {
                var color = LiveChartsRenderer.ParseColor(Bars.FirstOrDefault()?.ColorHex ?? palette[0], SKColors.RoyalBlue);
                cSeries.Add(new ColumnSeries<double>
                {
                    Name = Title,
                    Values = values,
                    Fill = new SolidColorPaint(color),
                    Stroke = null,
                    Rx = 4,
                    Ry = 4,
                    DataLabelsPaint = ShowDataLabels ? new SolidColorPaint(SKColors.DarkSlateGray) : null
                });
                break;
            }
        }
        CartesianSeries = cSeries.ToArray();

        // Cartesian Axes
        XAxes = new Axis[]
        {
            new Axis
            {
                Labels = categories,
                LabelsPaint = new SolidColorPaint(SKColors.DarkSlateGray),
                TextSize = 11,
                SeparatorsPaint = ShowGridlines ? new SolidColorPaint(SKColors.LightGray.WithAlpha(110)) { StrokeThickness = 1 } : null
            }
        };

        YAxes = new Axis[]
        {
            new Axis
            {
                LabelsPaint = new SolidColorPaint(SKColors.DarkSlateGray),
                TextSize = 11,
                SeparatorsPaint = ShowGridlines ? new SolidColorPaint(SKColors.LightGray.WithAlpha(110)) { StrokeThickness = 1 } : null
            }
        };

        // 2. Pie & Donut Series
        var pSeries = new List<ISeries>();
        double innerRadius = ChartType == ChartType.DonutPie ? Math.Max(0.1, DonutHoleRatio) : 0.0;
        PieMaxAngle = ChartType == ChartType.GaugeProgress ? 180 : 360;
        PieInitialRotation = ChartType == ChartType.GaugeProgress ? -180 : -90;

        for (int i = 0; i < values.Length; i++)
        {
            var color = LiveChartsRenderer.ParseColor(i < Bars.Count ? Bars[i].ColorHex : palette[i % palette.Count], SKColors.RoyalBlue);
            string name = i < categories.Length ? categories[i] : $"Segment {i + 1}";
            string valLabel = i < Bars.Count && !string.IsNullOrEmpty(Bars[i].ValueLabel) ? Bars[i].ValueLabel : values[i].ToString("G");

            pSeries.Add(new PieSeries<double>
            {
                Name = $"{name} ({valLabel})",
                Values = new double[] { values[i] },
                Fill = new SolidColorPaint(color),
                Stroke = new SolidColorPaint(SKColors.White, 2),
                InnerRadius = innerRadius,
                DataLabelsPaint = ShowDataLabels ? new SolidColorPaint(SKColors.White) : null
            });
        }
        PieSeries = pSeries.ToArray();

        // 3. Polar & Radar Series
        var polSeries = new List<ISeries>();
        var polColor = LiveChartsRenderer.ParseColor(Bars.FirstOrDefault()?.ColorHex ?? palette[0], SKColors.RoyalBlue);
        bool isArea = ChartType == ChartType.PolarArea;

        polSeries.Add(new PolarLineSeries<double>
        {
            Name = Title,
            Values = values,
            Stroke = new SolidColorPaint(polColor, (float)StrokeThickness),
            Fill = isArea ? new SolidColorPaint(polColor.WithAlpha(90)) : null,
            GeometrySize = 7,
            GeometryStroke = new SolidColorPaint(polColor, 2),
            GeometryFill = new SolidColorPaint(SKColors.White),
            IsClosed = true
        });
        PolarSeries = polSeries.ToArray();

        AngleAxes = new PolarAxis[]
        {
            new PolarAxis
            {
                Labels = categories,
                LabelsPaint = new SolidColorPaint(SKColors.DarkSlateGray),
                TextSize = 10,
                SeparatorsPaint = ShowGridlines ? new SolidColorPaint(SKColors.LightGray.WithAlpha(120)) { StrokeThickness = 1 } : null
            }
        };

        RadiusAxes = new PolarAxis[]
        {
            new PolarAxis
            {
                LabelsPaint = new SolidColorPaint(SKColors.Gray),
                TextSize = 9,
                SeparatorsPaint = ShowGridlines ? new SolidColorPaint(SKColors.LightGray.WithAlpha(100)) { StrokeThickness = 1 } : null
            }
        };

        // Render high-DPI live bitmap for native Avalonia canvas
        try
        {
            var model = (PdfChartElement)ToModel();
            int renderW = Math.Max(200, (int)Width);
            int renderH = Math.Max(120, (int)Height);
            byte[] pngBytes = LiveChartsRenderer.RenderChartToPngBytes(model, renderW, renderH, 2.0f);
            if (pngBytes != null && pngBytes.Length > 0)
            {
                using var ms = new System.IO.MemoryStream(pngBytes);
                ChartBitmap = new Avalonia.Media.Imaging.Bitmap(ms);
            }
        }
        catch
        {
            // Fallback for edge cases
        }
    }

    public List<ChartSeriesItem> MultiSeries { get; set; } = new();

    public override PdfElementBase ToModel()
    {
        var model = new PdfChartElement
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
            Title = Title,
            ChartType = ChartType,
            Palette = Palette,
            LegendPosition = LegendPosition,
            ShowDataLabels = ShowDataLabels,
            ShowGridlines = ShowGridlines,
            DonutHoleRatio = DonutHoleRatio,
            CurveSmoothness = CurveSmoothness,
            StrokeThickness = StrokeThickness,
            BackgroundColorHex = BackgroundColorHex,
            BorderColorHex = BorderColorHex,
            Categories = new List<string>(),
            Values = new List<double>(),
            ValueLabels = new List<string>(),
            BarColorsHex = new List<string>(),
            MultiSeries = MultiSeries.Select(s => s.Clone()).ToList()
        };

        foreach (var bar in Bars)
        {
            model.Categories.Add(bar.Category);
            model.Values.Add(bar.Value);
            model.ValueLabels.Add(bar.ValueLabel);
            model.BarColorsHex.Add(bar.ColorHex);
        }

        return model;
    }

    public override void LoadFromModel(PdfElementBase model)
    {
        if (model is PdfChartElement chart)
        {
            Id = chart.Id;
            X = chart.X;
            Y = chart.Y;
            Width = chart.Width;
            Height = chart.Height;
            ZIndex = chart.ZIndex;
            Rotation = chart.Rotation;
            Opacity = chart.Opacity;
            IsLocked = chart.IsLocked;

            Title = chart.Title;
            ChartType = chart.ChartType;
            Palette = chart.Palette;
            LegendPosition = chart.LegendPosition;
            ShowDataLabels = chart.ShowDataLabels;
            ShowGridlines = chart.ShowGridlines;
            DonutHoleRatio = chart.DonutHoleRatio;
            CurveSmoothness = chart.CurveSmoothness;
            StrokeThickness = chart.StrokeThickness;
            BackgroundColorHex = chart.BackgroundColorHex;
            BorderColorHex = chart.BorderColorHex;
            MultiSeries = chart.MultiSeries?.Select(s => s.Clone()).ToList() ?? new();

            // Adding N bars one at a time would otherwise fire UpdateLiveChart() (and a full
            // chart rasterization) once per bar — suppress until the whole set has loaded,
            // then rasterize exactly once.
            _suppressChartUpdate = true;
            Bars.Clear();
            for (int i = 0; i < chart.Categories.Count; i++)
            {
                Bars.Add(new ChartBarItem
                {
                    Category = chart.Categories[i],
                    Value = i < chart.Values.Count ? chart.Values[i] : 1.0,
                    ValueLabel = i < chart.ValueLabels.Count ? chart.ValueLabels[i] : "",
                    ColorHex = i < chart.BarColorsHex.Count ? chart.BarColorsHex[i] : "#0F6CBD"
                });
            }
            _suppressChartUpdate = false;
            UpdateLiveChart();
        }
    }
}
