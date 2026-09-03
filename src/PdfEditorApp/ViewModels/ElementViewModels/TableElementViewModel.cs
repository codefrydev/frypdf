using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using PdfEditorApp.Core.Models;
using PdfEditorApp.Core.Models.Elements;
using PdfEditorApp.Models;

namespace PdfEditorApp.ViewModels.ElementViewModels;

public partial class TableHeaderItem : ObservableObject
{
    [ObservableProperty]
    private string _text = "";

    public TableHeaderItem(string text = "")
    {
        _text = text;
    }

    public override string ToString() => Text;
}

public partial class TableCellItem : ObservableObject
{
    [ObservableProperty]
    private string _text = "";

    public TableCellItem(string text = "")
    {
        _text = text;
    }

    public override string ToString() => Text;
}

public class TableRowItem : ObservableObject
{
    public ObservableCollection<TableCellItem> Cells { get; } = new();

    public TableRowItem(IEnumerable<string> cells)
    {
        foreach (var c in cells) Cells.Add(new TableCellItem(c));
    }

    public TableRowItem(IEnumerable<TableCellItem> cells)
    {
        foreach (var c in cells) Cells.Add(c);
    }
}

public partial class TableElementViewModel : ElementViewModelBase
{
    [ObservableProperty]
    private string _headerBackgroundHex = "#0F6CBD";

    [ObservableProperty]
    private string _headerTextHex = "#FFFFFF";

    [ObservableProperty]
    private string _alternateRowBackgroundHex = "#F8F9FA";

    [ObservableProperty]
    private string _borderColorHex = "#E1DFDD";

    public ObservableCollection<TableHeaderItem> Headers { get; } = new();
    public ObservableCollection<TableRowItem> Rows { get; } = new();

    public override ElementKind Kind => ElementKind.Table;
    public override string DisplayName => $"Table ({Headers.Count} cols, {Rows.Count} rows)";

    public TableElementViewModel()
    {
        Headers.Add(new TableHeaderItem("Item Description"));
        Headers.Add(new TableHeaderItem("Qty"));
        Headers.Add(new TableHeaderItem("Rate"));
        Headers.Add(new TableHeaderItem("Amount"));

        Rows.Add(new TableRowItem(new[] { "Cloud Architecture Consulting", "40 hrs", "$150.00", "$6,000.00" }));
        Rows.Add(new TableRowItem(new[] { "Avalonia Desktop UI Engineering", "60 hrs", "$140.00", "$8,400.00" }));
        Rows.Add(new TableRowItem(new[] { "QuestPDF Engine Integration", "25 hrs", "$160.00", "$4,000.00" }));
    }

    [RelayCommand]
    public void AddRow()
    {
        var cells = new List<string>();
        for (int i = 0; i < Headers.Count; i++)
        {
            cells.Add($"Data {Rows.Count + 1}.{i + 1}");
        }
        Rows.Add(new TableRowItem(cells));
        Height = System.Math.Max(Height, 40 + (Rows.Count * 28));
        OnPropertyChanged(nameof(DisplayName));
    }

    [RelayCommand]
    public void RemoveRow()
    {
        if (Rows.Count > 1)
        {
            Rows.RemoveAt(Rows.Count - 1);
            OnPropertyChanged(nameof(DisplayName));
        }
    }

    [RelayCommand]
    public void AddColumn()
    {
        string headerName = $"Col {Headers.Count + 1}";
        Headers.Add(new TableHeaderItem(headerName));
        foreach (var r in Rows)
        {
            r.Cells.Add(new TableCellItem("---"));
        }
        OnPropertyChanged(nameof(DisplayName));
    }

    [RelayCommand]
    public void RemoveColumn()
    {
        if (Headers.Count > 1)
        {
            int colIdx = Headers.Count - 1;
            Headers.RemoveAt(colIdx);
            foreach (var r in Rows)
            {
                if (r.Cells.Count > colIdx)
                {
                    r.Cells.RemoveAt(colIdx);
                }
            }
            OnPropertyChanged(nameof(DisplayName));
        }
    }

    [RelayCommand]
    public void ApplyPresetStyle(string styleStr)
    {
        if (Enum.TryParse<TablePresetStyle>(styleStr, true, out var style))
        {
            switch (style)
            {
                case TablePresetStyle.ModernMinimal:
                    HeaderBackgroundHex = "#F1F5F9";
                    HeaderTextHex = "#0F172A";
                    AlternateRowBackgroundHex = "#FFFFFF";
                    BorderColorHex = "#E2E8F0";
                    break;
                case TablePresetStyle.EnterpriseBlue:
                    HeaderBackgroundHex = "#0F6CBD";
                    HeaderTextHex = "#FFFFFF";
                    AlternateRowBackgroundHex = "#F0F7FD";
                    BorderColorHex = "#CBD5E1";
                    break;
                case TablePresetStyle.DarkModeSlate:
                    HeaderBackgroundHex = "#1E293B";
                    HeaderTextHex = "#FFFFFF";
                    AlternateRowBackgroundHex = "#F8FAFC";
                    BorderColorHex = "#334155";
                    break;
                case TablePresetStyle.ZebraStriped:
                    HeaderBackgroundHex = "#475569";
                    HeaderTextHex = "#FFFFFF";
                    AlternateRowBackgroundHex = "#F1F5F9";
                    BorderColorHex = "#CBD5E1";
                    break;
                case TablePresetStyle.EmeraldGreen:
                    HeaderBackgroundHex = "#047857";
                    HeaderTextHex = "#FFFFFF";
                    AlternateRowBackgroundHex = "#ECFDF5";
                    BorderColorHex = "#A7F3D0";
                    break;
                case TablePresetStyle.AmberAccent:
                    HeaderBackgroundHex = "#D97706";
                    HeaderTextHex = "#FFFFFF";
                    AlternateRowBackgroundHex = "#FFFBEB";
                    BorderColorHex = "#FDE68A";
                    break;
                case TablePresetStyle.FinancialBordered:
                    HeaderBackgroundHex = "#0F172A";
                    HeaderTextHex = "#FFFFFF";
                    AlternateRowBackgroundHex = "#FFFFFF";
                    BorderColorHex = "#0F172A";
                    break;
                case TablePresetStyle.CompactClean:
                    HeaderBackgroundHex = "#E2E8F0";
                    HeaderTextHex = "#1E293B";
                    AlternateRowBackgroundHex = "#FAFAFA";
                    BorderColorHex = "#E2E8F0";
                    break;
            }
        }
    }

    public override PdfElementBase ToModel()
    {
        var model = new PdfTableElement
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
            HeaderBackgroundHex = HeaderBackgroundHex,
            HeaderTextHex = HeaderTextHex,
            AlternateRowBackgroundHex = AlternateRowBackgroundHex,
            BorderColorHex = BorderColorHex,
            Headers = Headers.Select(h => h.Text).ToList(),
            Rows = Rows.Select(r => r.Cells.Select(c => c.Text).ToList()).ToList()
        };

        return model;
    }

    public override void LoadFromModel(PdfElementBase model)
    {
        if (model is PdfTableElement table)
        {
            Id = table.Id;
            X = table.X;
            Y = table.Y;
            Width = table.Width;
            Height = table.Height;
            ZIndex = table.ZIndex;
            Rotation = table.Rotation;
            Opacity = table.Opacity;
            IsLocked = table.IsLocked;

            HeaderBackgroundHex = table.HeaderBackgroundHex;
            HeaderTextHex = table.HeaderTextHex;
            AlternateRowBackgroundHex = table.AlternateRowBackgroundHex;
            BorderColorHex = table.BorderColorHex;

            Headers.Clear();
            foreach (var h in table.Headers) Headers.Add(new TableHeaderItem(h));

            Rows.Clear();
            foreach (var r in table.Rows) Rows.Add(new TableRowItem(r));
        }
    }
}
