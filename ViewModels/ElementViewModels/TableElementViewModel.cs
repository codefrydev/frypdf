using System.Collections.Generic;
using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using PdfEditorApp.Models;
using PdfEditorApp.Models.Elements;

namespace PdfEditorApp.ViewModels.ElementViewModels;

public class TableRowItem : ObservableObject
{
    public ObservableCollection<string> Cells { get; } = new();

    public TableRowItem(IEnumerable<string> cells)
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

    public ObservableCollection<string> Headers { get; } = new();
    public ObservableCollection<TableRowItem> Rows { get; } = new();

    public override ElementKind Kind => ElementKind.Table;
    public override string DisplayName => $"Table ({Headers.Count} cols, {Rows.Count} rows)";

    public TableElementViewModel()
    {
        Headers.Add("Item Description");
        Headers.Add("Qty");
        Headers.Add("Rate");
        Headers.Add("Amount");

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
        Headers.Add(headerName);
        foreach (var r in Rows)
        {
            r.Cells.Add("---");
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
            Headers = new List<string>(Headers),
            Rows = new List<List<string>>()
        };

        foreach (var row in Rows)
        {
            model.Rows.Add(new List<string>(row.Cells));
        }

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
            foreach (var h in table.Headers) Headers.Add(h);

            Rows.Clear();
            foreach (var r in table.Rows) Rows.Add(new TableRowItem(r));
        }
    }
}
