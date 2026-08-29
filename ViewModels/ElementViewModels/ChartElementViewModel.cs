using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using PdfEditorApp.Models;
using PdfEditorApp.Models.Elements;

namespace PdfEditorApp.ViewModels.ElementViewModels;

public class ChartBarItem : ObservableObject
{
    public string Category { get; set; } = "Q1";
    public double Value { get; set; } = 1.0;
    public string ValueLabel { get; set; } = "$1.0B";
    public string ColorHex { get; set; } = "#0F6CBD";
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
    [NotifyPropertyChangedFor(nameof(IsArea))]
    [NotifyPropertyChangedFor(nameof(IsDonutPie))]
    [NotifyPropertyChangedFor(nameof(IsStackedBar))]
    [NotifyPropertyChangedFor(nameof(IsRadar))]
    [NotifyPropertyChangedFor(nameof(IsFunnel))]
    [NotifyPropertyChangedFor(nameof(IsWaterfall))]
    [NotifyPropertyChangedFor(nameof(IsGaugeProgress))]
    [NotifyPropertyChangedFor(nameof(IsStepLine))]
    [NotifyPropertyChangedFor(nameof(IsPyramid))]
    [NotifyPropertyChangedFor(nameof(ChartTypeDescription))]
    private ChartType _chartType = ChartType.BarColumn;

    public bool IsBarColumn => ChartType == ChartType.BarColumn;
    public bool IsHorizontalBar => ChartType == ChartType.HorizontalBar;
    public bool IsLine => ChartType == ChartType.Line;
    public bool IsArea => ChartType == ChartType.Area;
    public bool IsDonutPie => ChartType == ChartType.DonutPie;
    public bool IsStackedBar => ChartType == ChartType.StackedBar;
    public bool IsRadar => ChartType == ChartType.Radar;
    public bool IsFunnel => ChartType == ChartType.Funnel;
    public bool IsWaterfall => ChartType == ChartType.Waterfall;
    public bool IsGaugeProgress => ChartType == ChartType.GaugeProgress;
    public bool IsStepLine => ChartType == ChartType.StepLine;
    public bool IsPyramid => ChartType == ChartType.Pyramid;

    public string ChartTypeDescription => ChartType.ToString();

    [ObservableProperty]
    private string _backgroundColorHex = "#FAFAFA";

    [ObservableProperty]
    private string _borderColorHex = "#E2E8F0";

    [RelayCommand]
    public void SetChartType(string typeStr)
    {
        if (Enum.TryParse<ChartType>(typeStr, true, out var type))
        {
            ChartType = type;
        }
    }

    public ObservableCollection<ChartBarItem> Bars { get; } = new();

    public override ElementKind Kind => ElementKind.Chart;
    public override string DisplayName => Title;

    public ChartElementViewModel()
    {
        // Default sample bars
        Bars.Add(new ChartBarItem { Category = "Q1", Value = 1.2, ValueLabel = "$1.2B", ColorHex = "#93C5FD" });
        Bars.Add(new ChartBarItem { Category = "Q2", Value = 1.8, ValueLabel = "$1.8B", ColorHex = "#60A5FA" });
        Bars.Add(new ChartBarItem { Category = "Q3", Value = 2.5, ValueLabel = "$2.5B", ColorHex = "#3B82F6" });
        Bars.Add(new ChartBarItem { Category = "Q4", Value = 3.1, ValueLabel = "$3.1B", ColorHex = "#0F6CBD" });
    }

    [RelayCommand]
    public void AddDataPoint()
    {
        int nextNum = Bars.Count + 1;
        var colors = new[] { "#93C5FD", "#60A5FA", "#3B82F6", "#0F6CBD", "#1E3A8A", "#7C3AED", "#10B981", "#F59E0B" };
        string color = colors[(nextNum - 1) % colors.Length];
        double val = Math.Round(1.0 + (nextNum * 0.5), 1);

        Bars.Add(new ChartBarItem
        {
            Category = $"Q{nextNum}",
            Value = val,
            ValueLabel = $"${val:F1}B",
            ColorHex = color
        });
        OnPropertyChanged(nameof(Bars));
    }

    [RelayCommand]
    public void RemoveDataPoint()
    {
        if (Bars.Count > 1)
        {
            Bars.RemoveAt(Bars.Count - 1);
            OnPropertyChanged(nameof(Bars));
        }
    }

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
            BackgroundColorHex = BackgroundColorHex,
            BorderColorHex = BorderColorHex,
            Categories = new List<string>(),
            Values = new List<double>(),
            ValueLabels = new List<string>(),
            BarColorsHex = new List<string>()
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
            BackgroundColorHex = chart.BackgroundColorHex;
            BorderColorHex = chart.BorderColorHex;

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
        }
    }
}
