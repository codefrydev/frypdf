using System;
using System.Collections.Generic;
using System.Linq;
using LiveChartsCore;
using LiveChartsCore.Defaults;
using LiveChartsCore.Drawing;
using LiveChartsCore.Measure;
using LiveChartsCore.SkiaSharpView;
using LiveChartsCore.SkiaSharpView.Painting;
using LiveChartsCore.SkiaSharpView.SKCharts;
using LiveChartsCore.SkiaSharpView.VisualElements;
using PdfEditorApp.Core.Models;
using PdfEditorApp.Core.Models.Elements;
using SkiaSharp;

namespace PdfEditorApp.Core.Analysis;

/// <summary>
/// Headless SkiaSharp LiveCharts2 rendering engine for high-resolution chart export in QuestPDF and preview rasterization.
/// </summary>
public static class LiveChartsRenderer
{
    private static bool _isConfigured;
    private static readonly object _configLock = new();

    public static void EnsureConfigured()
    {
        if (_isConfigured) return;
        lock (_configLock)
        {
            if (!_isConfigured)
            {
                LiveCharts.Configure(config => LiveChartsSkiaSharp.UseDefaults(config));
                _isConfigured = true;
            }
        }
    }

    public static IReadOnlyList<string> GetPaletteHexColors(ChartPalette palette)
    {
        return palette switch
        {
            ChartPalette.CorporateBlue => new[] { "#0F6CBD", "#3D95E6", "#82BDF0", "#C7E0F4", "#0B4A82", "#115EA3" },
            ChartPalette.EmeraldGreen => new[] { "#107C41", "#27A25D", "#57C488", "#97E3B5", "#0E5E32", "#188A4C" },
            ChartPalette.SunsetOrange => new[] { "#D83B01", "#F7630C", "#FF8C00", "#FCE100", "#A80000", "#EA4300" },
            ChartPalette.CyberNeon => new[] { "#8764B8", "#B146C2", "#00B7C3", "#00CC6A", "#E3008C", "#FFB900" },
            ChartPalette.ExecutiveSlate => new[] { "#323130", "#605E5C", "#8A8886", "#B3B0AD", "#201F1E", "#484644" },
            ChartPalette.PastelHarmony => new[] { "#7986CB", "#4DB6AC", "#FFD54F", "#BA68C8", "#FF8A65", "#A1887F" },
            ChartPalette.VibrantRainbow => new[] { "#E63946", "#F4A261", "#E9C46A", "#2A9D8F", "#264653", "#457B9D" },
            _ => new[] { "#0F6CBD", "#3D95E6", "#82BDF0", "#C7E0F4", "#0B4A82" }
        };
    }

    public static SKColor ParseColor(string? hex, SKColor fallback)
    {
        if (string.IsNullOrWhiteSpace(hex)) return fallback;
        return SKColor.TryParse(hex, out var color) ? color : fallback;
    }

    public static byte[] RenderChartToPngBytes(PdfChartElement chartEl, int width = 500, int height = 300, float dpiScale = 2.0f)
    {
        EnsureConfigured();

        int targetWidth = Math.Max(200, (int)(width * dpiScale));
        int targetHeight = Math.Max(150, (int)(height * dpiScale));

        var palette = GetPaletteHexColors(chartEl.Palette);
        var legendPos = ToLiveChartsLegend(chartEl.LegendPosition);

        using SKImage? img = chartEl.ChartType switch
        {
            ChartType.DonutPie or ChartType.GaugeProgress => RenderPieChart(chartEl, targetWidth, targetHeight, palette, legendPos, dpiScale),
            ChartType.Radar or ChartType.PolarArea => RenderPolarChart(chartEl, targetWidth, targetHeight, palette, legendPos, dpiScale),
            _ => RenderCartesianChart(chartEl, targetWidth, targetHeight, palette, legendPos, dpiScale)
        };

        if (img == null)
            return Array.Empty<byte>();

        using var data = img.Encode(SKEncodedImageFormat.Png, 100);
        return data.ToArray();
    }

    private static LegendPosition ToLiveChartsLegend(ChartLegendPosition pos)
    {
        return pos switch
        {
            ChartLegendPosition.Top => LegendPosition.Top,
            ChartLegendPosition.Bottom => LegendPosition.Bottom,
            ChartLegendPosition.Left => LegendPosition.Left,
            ChartLegendPosition.Right => LegendPosition.Right,
            _ => LegendPosition.Hidden
        };
    }

    private static SKImage RenderCartesianChart(PdfChartElement chartEl, int width, int height, IReadOnlyList<string> palette, LegendPosition legendPos, float dpiScale)
    {
        var seriesList = new List<ISeries>();
        var categories = chartEl.Categories.Count > 0 ? chartEl.Categories : new List<string> { "A", "B", "C", "D" };

        var primaryValues = chartEl.Values.Count > 0 ? chartEl.Values : new List<double> { 10, 20, 30, 40 };

        bool hasMultiSeries = chartEl.MultiSeries.Count > 0;

        switch (chartEl.ChartType)
        {
            case ChartType.HorizontalBar:
            {
                if (hasMultiSeries)
                {
                    for (int sIdx = 0; sIdx < chartEl.MultiSeries.Count; sIdx++)
                    {
                        var s = chartEl.MultiSeries[sIdx];
                        var color = ParseColor(s.ColorHex ?? palette[sIdx % palette.Count], SKColors.RoyalBlue);
                        seriesList.Add(new RowSeries<double>
                        {
                            Name = s.Name,
                            Values = s.Values.Count > 0 ? s.Values : primaryValues,
                            Fill = new SolidColorPaint(color),
                            Stroke = null,
                            DataLabelsSize = 10 * dpiScale,
                            DataLabelsPaint = chartEl.ShowDataLabels ? new SolidColorPaint(SKColors.DarkSlateGray) : null
                        });
                    }
                }
                else
                {
                    var color = ParseColor(chartEl.BarColorsHex.FirstOrDefault() ?? palette[0], SKColors.RoyalBlue);
                    seriesList.Add(new RowSeries<double>
                    {
                        Name = chartEl.Title,
                        Values = primaryValues,
                        Fill = new SolidColorPaint(color),
                        Stroke = null,
                        DataLabelsSize = 10 * dpiScale,
                        DataLabelsPaint = chartEl.ShowDataLabels ? new SolidColorPaint(SKColors.DarkSlateGray) : null
                    });
                }
                break;
            }

            case ChartType.StackedBar:
            {
                if (hasMultiSeries)
                {
                    for (int sIdx = 0; sIdx < chartEl.MultiSeries.Count; sIdx++)
                    {
                        var s = chartEl.MultiSeries[sIdx];
                        var color = ParseColor(s.ColorHex ?? palette[sIdx % palette.Count], SKColors.RoyalBlue);
                        seriesList.Add(new StackedColumnSeries<double>
                        {
                            Name = s.Name,
                            Values = s.Values.Count > 0 ? s.Values : primaryValues,
                            Fill = new SolidColorPaint(color),
                            Stroke = null
                        });
                    }
                }
                else
                {
                    for (int i = 0; i < primaryValues.Count; i++)
                    {
                        var color = ParseColor(i < chartEl.BarColorsHex.Count ? chartEl.BarColorsHex[i] : palette[i % palette.Count], SKColors.RoyalBlue);
                        seriesList.Add(new StackedColumnSeries<double>
                        {
                            Name = i < categories.Count ? categories[i] : $"Segment {i + 1}",
                            Values = new double[] { primaryValues[i] },
                            Fill = new SolidColorPaint(color),
                            Stroke = null
                        });
                    }
                }
                break;
            }

            case ChartType.StackedHorizontalBar:
            {
                if (hasMultiSeries)
                {
                    for (int sIdx = 0; sIdx < chartEl.MultiSeries.Count; sIdx++)
                    {
                        var s = chartEl.MultiSeries[sIdx];
                        var color = ParseColor(s.ColorHex ?? palette[sIdx % palette.Count], SKColors.RoyalBlue);
                        seriesList.Add(new StackedRowSeries<double>
                        {
                            Name = s.Name,
                            Values = s.Values.Count > 0 ? s.Values : primaryValues,
                            Fill = new SolidColorPaint(color),
                            Stroke = null
                        });
                    }
                }
                else
                {
                    var color = ParseColor(chartEl.BarColorsHex.FirstOrDefault() ?? palette[0], SKColors.RoyalBlue);
                    seriesList.Add(new StackedRowSeries<double>
                    {
                        Name = chartEl.Title,
                        Values = primaryValues,
                        Fill = new SolidColorPaint(color),
                        Stroke = null
                    });
                }
                break;
            }

            case ChartType.Line:
            {
                if (hasMultiSeries)
                {
                    for (int sIdx = 0; sIdx < chartEl.MultiSeries.Count; sIdx++)
                    {
                        var s = chartEl.MultiSeries[sIdx];
                        var color = ParseColor(s.ColorHex ?? palette[sIdx % palette.Count], SKColors.RoyalBlue);
                        seriesList.Add(new LineSeries<double>
                        {
                            Name = s.Name,
                            Values = s.Values.Count > 0 ? s.Values : primaryValues,
                            Stroke = new SolidColorPaint(color, (float)chartEl.StrokeThickness * dpiScale),
                            Fill = null,
                            GeometrySize = 6 * dpiScale,
                            GeometryStroke = new SolidColorPaint(color, 2 * dpiScale),
                            GeometryFill = new SolidColorPaint(SKColors.White),
                            LineSmoothness = 0.0
                        });
                    }
                }
                else
                {
                    var color = ParseColor(chartEl.BarColorsHex.FirstOrDefault() ?? palette[0], SKColors.RoyalBlue);
                    seriesList.Add(new LineSeries<double>
                    {
                        Name = chartEl.Title,
                        Values = primaryValues,
                        Stroke = new SolidColorPaint(color, (float)chartEl.StrokeThickness * dpiScale),
                        Fill = null,
                        GeometrySize = 6 * dpiScale,
                        GeometryStroke = new SolidColorPaint(color, 2 * dpiScale),
                        GeometryFill = new SolidColorPaint(SKColors.White),
                        LineSmoothness = 0.0
                    });
                }
                break;
            }

            case ChartType.SmoothLine:
            {
                var color = ParseColor(chartEl.BarColorsHex.FirstOrDefault() ?? palette[0], SKColors.RoyalBlue);
                seriesList.Add(new LineSeries<double>
                {
                    Name = chartEl.Title,
                    Values = primaryValues,
                    Stroke = new SolidColorPaint(color, (float)chartEl.StrokeThickness * dpiScale),
                    Fill = null,
                    GeometrySize = 7 * dpiScale,
                    GeometryStroke = new SolidColorPaint(color, 2 * dpiScale),
                    GeometryFill = new SolidColorPaint(SKColors.White),
                    LineSmoothness = (float)chartEl.CurveSmoothness
                });
                break;
            }

            case ChartType.Area:
            {
                var color = ParseColor(chartEl.BarColorsHex.FirstOrDefault() ?? palette[0], SKColors.RoyalBlue);
                var fillColor = color.WithAlpha(65);
                seriesList.Add(new LineSeries<double>
                {
                    Name = chartEl.Title,
                    Values = primaryValues,
                    Stroke = new SolidColorPaint(color, (float)chartEl.StrokeThickness * dpiScale),
                    Fill = new SolidColorPaint(fillColor),
                    GeometrySize = 6 * dpiScale,
                    GeometryStroke = new SolidColorPaint(color, 2 * dpiScale),
                    GeometryFill = new SolidColorPaint(SKColors.White),
                    LineSmoothness = (float)chartEl.CurveSmoothness
                });
                break;
            }

            case ChartType.StepLine:
            {
                var color = ParseColor(chartEl.BarColorsHex.FirstOrDefault() ?? palette[0], SKColors.RoyalBlue);
                seriesList.Add(new StepLineSeries<double>
                {
                    Name = chartEl.Title,
                    Values = primaryValues,
                    Stroke = new SolidColorPaint(color, (float)chartEl.StrokeThickness * dpiScale),
                    Fill = null,
                    GeometrySize = 6 * dpiScale,
                    GeometryStroke = new SolidColorPaint(color, 2 * dpiScale),
                    GeometryFill = new SolidColorPaint(SKColors.White)
                });
                break;
            }

            case ChartType.ScatterPlot:
            {
                var color = ParseColor(chartEl.BarColorsHex.FirstOrDefault() ?? palette[0], SKColors.RoyalBlue);
                var points = new List<ObservablePoint>();
                for (int i = 0; i < primaryValues.Count; i++)
                {
                    points.Add(new ObservablePoint(i + 1, primaryValues[i]));
                }
                seriesList.Add(new ScatterSeries<ObservablePoint>
                {
                    Name = chartEl.Title,
                    Values = points,
                    Stroke = new SolidColorPaint(color, 2 * dpiScale),
                    Fill = new SolidColorPaint(color.WithAlpha(180)),
                    GeometrySize = 10 * dpiScale
                });
                break;
            }

            case ChartType.Candlestick:
            {
                var financialPoints = new List<FinancialPoint>();
                for (int i = 0; i < primaryValues.Count; i++)
                {
                    double close = primaryValues[i];
                    double open = i > 0 ? primaryValues[i - 1] : close * 0.95;
                    double high = Math.Max(open, close) * 1.08;
                    double low = Math.Min(open, close) * 0.92;
                    financialPoints.Add(new FinancialPoint(new DateTime(2026, 1, 1).AddDays(i), high, open, close, low));
                }
                seriesList.Add(new CandlesticksSeries<FinancialPoint>
                {
                    Name = chartEl.Title,
                    Values = financialPoints,
                    UpStroke = new SolidColorPaint(SKColors.ForestGreen, 2 * dpiScale),
                    UpFill = new SolidColorPaint(SKColors.ForestGreen.WithAlpha(200)),
                    DownStroke = new SolidColorPaint(SKColors.IndianRed, 2 * dpiScale),
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
                if (hasMultiSeries)
                {
                    for (int sIdx = 0; sIdx < chartEl.MultiSeries.Count; sIdx++)
                    {
                        var s = chartEl.MultiSeries[sIdx];
                        var color = ParseColor(s.ColorHex ?? palette[sIdx % palette.Count], SKColors.RoyalBlue);
                        seriesList.Add(new ColumnSeries<double>
                        {
                            Name = s.Name,
                            Values = s.Values.Count > 0 ? s.Values : primaryValues,
                            Fill = new SolidColorPaint(color),
                            Stroke = null,
                            Rx = 4 * dpiScale,
                            Ry = 4 * dpiScale,
                            DataLabelsSize = 9 * dpiScale,
                            DataLabelsPaint = chartEl.ShowDataLabels ? new SolidColorPaint(SKColors.DarkSlateGray) : null
                        });
                    }
                }
                else
                {
                    var color = ParseColor(chartEl.BarColorsHex.FirstOrDefault() ?? palette[0], SKColors.RoyalBlue);
                    seriesList.Add(new ColumnSeries<double>
                    {
                        Name = chartEl.Title,
                        Values = primaryValues,
                        Fill = new SolidColorPaint(color),
                        Stroke = null,
                        Rx = 4 * dpiScale,
                        Ry = 4 * dpiScale,
                        DataLabelsSize = 9 * dpiScale,
                        DataLabelsPaint = chartEl.ShowDataLabels ? new SolidColorPaint(SKColors.DarkSlateGray) : null
                    });
                }
                break;
            }
        }

        var xAxes = new Axis[]
        {
            new Axis
            {
                Labels = categories.ToArray(),
                Name = chartEl.XAxisTitle,
                LabelsPaint = new SolidColorPaint(SKColors.DarkSlateGray),
                TextSize = 10.5f * dpiScale,
                NameTextSize = 11.5f * dpiScale,
                SeparatorsPaint = chartEl.ShowGridlines ? new SolidColorPaint(SKColors.LightGray.WithAlpha(110)) { StrokeThickness = 1 * dpiScale } : null
            }
        };

        var yAxes = new Axis[]
        {
            new Axis
            {
                Name = chartEl.YAxisTitle,
                LabelsPaint = new SolidColorPaint(SKColors.DarkSlateGray),
                TextSize = 10.5f * dpiScale,
                NameTextSize = 11.5f * dpiScale,
                SeparatorsPaint = chartEl.ShowGridlines ? new SolidColorPaint(SKColors.LightGray.WithAlpha(110)) { StrokeThickness = 1 * dpiScale } : null
            }
        };

#pragma warning disable CS0618
        var titlePaint = !string.IsNullOrWhiteSpace(chartEl.Title)
            ? new LabelVisual
            {
                Text = chartEl.Title,
                TextSize = 13.5f * dpiScale,
                Paint = new SolidColorPaint(SKColors.Black)
            }
            : null;
#pragma warning restore CS0618

        var chart = new SKCartesianChart
        {
            Width = width,
            Height = height,
            Series = seriesList,
            XAxes = xAxes,
            YAxes = yAxes,
            Title = titlePaint,
            LegendPosition = legendPos,
            LegendTextSize = 10.5f * dpiScale,
            LegendTextPaint = new SolidColorPaint(SKColors.DarkSlateGray)
        };

        return chart.GetImage();
    }

    private static SKImage RenderPieChart(PdfChartElement chartEl, int width, int height, IReadOnlyList<string> palette, LegendPosition legendPos, float dpiScale)
    {
        var seriesList = new List<ISeries>();
        var categories = chartEl.Categories.Count > 0 ? chartEl.Categories : new List<string> { "Q1", "Q2", "Q3", "Q4" };
        var values = chartEl.Values.Count > 0 ? chartEl.Values : new List<double> { 25, 25, 25, 25 };

        double innerRadius = chartEl.ChartType == ChartType.DonutPie ? Math.Max(0.1, chartEl.DonutHoleRatio) : 0.0;
        double maxAngle = chartEl.ChartType == ChartType.GaugeProgress ? 180 : 360;
        double initialRotation = chartEl.ChartType == ChartType.GaugeProgress ? -180 : -90;

        for (int i = 0; i < values.Count; i++)
        {
            var color = ParseColor(i < chartEl.BarColorsHex.Count ? chartEl.BarColorsHex[i] : palette[i % palette.Count], SKColors.RoyalBlue);
            string name = i < categories.Count ? categories[i] : $"Segment {i + 1}";
            string valLabel = i < chartEl.ValueLabels.Count && !string.IsNullOrEmpty(chartEl.ValueLabels[i])
                ? chartEl.ValueLabels[i]
                : values[i].ToString("G");

            seriesList.Add(new PieSeries<double>
            {
                Name = $"{name} ({valLabel})",
                Values = new double[] { values[i] },
                Fill = new SolidColorPaint(color),
                Stroke = new SolidColorPaint(SKColors.White, 2 * dpiScale),
                InnerRadius = innerRadius,
                DataLabelsSize = 10 * dpiScale,
                DataLabelsPaint = chartEl.ShowDataLabels ? new SolidColorPaint(SKColors.White) : null
            });
        }

#pragma warning disable CS0618
        var titlePaint = !string.IsNullOrWhiteSpace(chartEl.Title)
            ? new LabelVisual
            {
                Text = chartEl.Title,
                TextSize = 13.5f * dpiScale,
                Paint = new SolidColorPaint(SKColors.Black)
            }
            : null;
#pragma warning restore CS0618

        var chart = new SKPieChart
        {
            Width = width,
            Height = height,
            Series = seriesList,
            Title = titlePaint,
            LegendPosition = legendPos,
            LegendTextSize = 10.5f * dpiScale,
            LegendTextPaint = new SolidColorPaint(SKColors.DarkSlateGray),
            MaxAngle = maxAngle,
            InitialRotation = initialRotation
        };

        return chart.GetImage();
    }

    private static SKImage RenderPolarChart(PdfChartElement chartEl, int width, int height, IReadOnlyList<string> palette, LegendPosition legendPos, float dpiScale)
    {
        var seriesList = new List<ISeries>();
        var categories = chartEl.Categories.Count > 0 ? chartEl.Categories : new List<string> { "Speed", "Reliability", "Comfort", "Safety", "Efficiency" };
        var values = chartEl.Values.Count > 0 ? chartEl.Values : new List<double> { 8, 9, 7, 10, 8.5 };

        var color = ParseColor(chartEl.BarColorsHex.FirstOrDefault() ?? palette[0], SKColors.RoyalBlue);
        bool isArea = chartEl.ChartType == ChartType.PolarArea;

        seriesList.Add(new PolarLineSeries<double>
        {
            Name = chartEl.Title,
            Values = values,
            Stroke = new SolidColorPaint(color, (float)chartEl.StrokeThickness * dpiScale),
            Fill = isArea ? new SolidColorPaint(color.WithAlpha(90)) : null,
            GeometrySize = 7 * dpiScale,
            GeometryStroke = new SolidColorPaint(color, 2 * dpiScale),
            GeometryFill = new SolidColorPaint(SKColors.White),
            IsClosed = true
        });

        var angleAxes = new PolarAxis[]
        {
            new PolarAxis
            {
                Labels = categories.ToArray(),
                LabelsPaint = new SolidColorPaint(SKColors.DarkSlateGray),
                TextSize = 10 * dpiScale,
                SeparatorsPaint = chartEl.ShowGridlines ? new SolidColorPaint(SKColors.LightGray.WithAlpha(120)) { StrokeThickness = 1 * dpiScale } : null
            }
        };

        var radiusAxes = new PolarAxis[]
        {
            new PolarAxis
            {
                LabelsPaint = new SolidColorPaint(SKColors.Gray),
                TextSize = 9 * dpiScale,
                SeparatorsPaint = chartEl.ShowGridlines ? new SolidColorPaint(SKColors.LightGray.WithAlpha(100)) { StrokeThickness = 1 * dpiScale } : null
            }
        };

#pragma warning disable CS0618
        var titlePaint = !string.IsNullOrWhiteSpace(chartEl.Title)
            ? new LabelVisual
            {
                Text = chartEl.Title,
                TextSize = 13.5f * dpiScale,
                Paint = new SolidColorPaint(SKColors.Black)
            }
            : null;
#pragma warning restore CS0618

        var chart = new SKPolarChart
        {
            Width = width,
            Height = height,
            Series = seriesList,
            AngleAxes = angleAxes,
            RadiusAxes = radiusAxes,
            Title = titlePaint,
            LegendPosition = legendPos,
            LegendTextSize = 10.5f * dpiScale,
            LegendTextPaint = new SolidColorPaint(SKColors.DarkSlateGray)
        };

        return chart.GetImage();
    }
}
