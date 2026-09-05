using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Avalonia.Input.Platform;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using PdfEditorApp.Core.Models;
using PdfEditorApp.Core.Models.Elements;
using PdfEditorApp.ViewModels.ElementViewModels;

namespace PdfEditorApp.ViewModels.FryPdfViewer;

/// <summary>
/// Represents a row in an interactive data table with cell lookup and formatted display.
/// </summary>
public class InteractiveTableRowItem : ObservableObject
{
    public List<string> Cells { get; }

    public InteractiveTableRowItem(IEnumerable<string> cells)
    {
        Cells = cells.ToList();
    }

    public string GetCell(int index) => (index >= 0 && index < Cells.Count) ? Cells[index] : string.Empty;

    public bool MatchesFilter(string query)
    {
        if (string.IsNullOrWhiteSpace(query)) return true;
        for (int i = 0; i < Cells.Count; i++)
        {
            if (Cells[i].Contains(query, StringComparison.OrdinalIgnoreCase))
                return true;
        }
        return false;
    }
}

/// <summary>
/// Interactive ViewModel wrapping a data table in the .frypdf document.
/// Provides capabilities impossible in static binary PDFs:
/// - Real-time in-table row search & filtering
/// - Clickable column headers with automatic ascending/descending sorting
/// - Sticky header and smooth virtualized scrolling
/// - One-click "Copy Table as CSV" to clipboard
/// - Alternating row styling & row selection highlighting
/// </summary>
public partial class InteractiveTableViewModel : ElementViewModelBase
{
    private PdfTableElement? _sourceModel;

    public override ElementKind Kind => ElementKind.Table;
    public override string DisplayName => "Interactive Table";

    [ObservableProperty]
    private string _headerBackgroundHex = "#0F6CBD";

    [ObservableProperty]
    private string _headerTextHex = "#FFFFFF";

    [ObservableProperty]
    private string _alternateRowBackgroundHex = "#F8FAFC";

    [ObservableProperty]
    private string _borderColorHex = "#E2E8F0";

    [ObservableProperty]
    private string _filterQuery = "";

    [ObservableProperty]
    private int? _sortedColumnIndex = null;

    [ObservableProperty]
    private bool _sortAscending = true;

    [ObservableProperty]
    private InteractiveTableRowItem? _selectedRow;

    public ObservableCollection<string> Headers { get; } = new();
    public List<InteractiveTableRowItem> AllRows { get; } = new();
    public ObservableCollection<InteractiveTableRowItem> DisplayedRows { get; } = new();

    public int TotalRowCount => AllRows.Count;
    public int FilteredRowCount => DisplayedRows.Count;

    public string RowCountSummary => string.IsNullOrWhiteSpace(FilterQuery)
        ? $"{TotalRowCount} rows"
        : $"{FilteredRowCount} of {TotalRowCount} rows match";

    public InteractiveTableViewModel()
    {
        Width = 500;
        Height = 240;
        ZIndex = 500;
    }

    public InteractiveTableViewModel(TableElementViewModel tableVm)
    {
        Id = tableVm.Id;
        X = tableVm.X;
        Y = tableVm.Y;
        Width = tableVm.Width;
        Height = tableVm.Height;
        ZIndex = tableVm.ZIndex;
        Rotation = tableVm.Rotation;
        Opacity = tableVm.Opacity;
        HeaderBackgroundHex = tableVm.HeaderBackgroundHex;
        HeaderTextHex = tableVm.HeaderTextHex;
        AlternateRowBackgroundHex = tableVm.AlternateRowBackgroundHex;
        BorderColorHex = tableVm.BorderColorHex;

        foreach (var h in tableVm.Headers)
        {
            Headers.Add(h.Text);
        }

        foreach (var r in tableVm.Rows)
        {
            var cellValues = r.Cells.Select(c => c.Text).ToList();
            var rowItem = new InteractiveTableRowItem(cellValues);
            AllRows.Add(rowItem);
            DisplayedRows.Add(rowItem);
        }
    }

    public InteractiveTableViewModel(PdfTableElement tableModel)
    {
        _sourceModel = tableModel;
        PopulateBaseProperties(tableModel);
        LoadFromTableModel(tableModel);
    }

    private void LoadFromTableModel(PdfTableElement tableModel)
    {
        HeaderBackgroundHex = tableModel.HeaderBackgroundHex;
        HeaderTextHex = tableModel.HeaderTextHex;
        AlternateRowBackgroundHex = tableModel.AlternateRowBackgroundHex;
        BorderColorHex = tableModel.BorderColorHex;

        Headers.Clear();
        foreach (var h in tableModel.Headers)
        {
            Headers.Add(h);
        }

        AllRows.Clear();
        DisplayedRows.Clear();
        foreach (var r in tableModel.Rows)
        {
            var rowItem = new InteractiveTableRowItem(r);
            AllRows.Add(rowItem);
            DisplayedRows.Add(rowItem);
        }
    }

    public override PdfElementBase ToModel()
    {
        var model = _sourceModel ?? new PdfTableElement();
        CopyBasePropertiesTo(model);
        model.HeaderBackgroundHex = HeaderBackgroundHex;
        model.HeaderTextHex = HeaderTextHex;
        model.AlternateRowBackgroundHex = AlternateRowBackgroundHex;
        model.BorderColorHex = BorderColorHex;
        model.Headers = Headers.ToList();
        model.Rows = AllRows.Select(r => r.Cells.ToList()).ToList();
        return model;
    }

    public override void LoadFromModel(PdfElementBase model)
    {
        if (model is PdfTableElement table)
        {
            _sourceModel = table;
            PopulateBaseProperties(table);
            LoadFromTableModel(table);
        }
    }

    partial void OnFilterQueryChanged(string value)
    {
        ApplyFilterAndSort();
    }

    /// <summary>
    /// Sorts the table rows by the specified column index.
    /// Toggles between ascending and descending if the same column is clicked repeatedly.
    /// </summary>
    [RelayCommand]
    public void SortByColumn(int columnIndex)
    {
        if (columnIndex < 0 || columnIndex >= Headers.Count) return;

        if (SortedColumnIndex == columnIndex)
        {
            SortAscending = !SortAscending;
        }
        else
        {
            SortedColumnIndex = columnIndex;
            SortAscending = true;
        }

        ApplyFilterAndSort();
    }

    /// <summary>
    /// Clears the current table filter query.
    /// </summary>
    [RelayCommand]
    public void ClearFilter()
    {
        FilterQuery = string.Empty;
    }

    /// <summary>
    /// Copies the currently filtered table data as CSV to the system clipboard.
    /// </summary>
    [RelayCommand]
    public async Task CopyToClipboardAsync()
    {
        var csv = GenerateCsv();
        if (Avalonia.Application.Current?.ApplicationLifetime is Avalonia.Controls.ApplicationLifetimes.IClassicDesktopStyleApplicationLifetime desktop && desktop.MainWindow?.Clipboard != null)
        {
            await desktop.MainWindow.Clipboard.SetTextAsync(csv);
        }
    }

    /// <summary>
    /// Generates a standardized comma-separated CSV string of headers and currently filtered rows.
    /// </summary>
    public string GenerateCsv()
    {
        var sb = new StringBuilder();

        // Headers
        sb.AppendLine(string.Join(",", Headers.Select(EscapeCsvValue)));

        // Rows
        foreach (var row in DisplayedRows)
        {
            sb.AppendLine(string.Join(",", row.Cells.Select(EscapeCsvValue)));
        }

        return sb.ToString();
    }

    private static string EscapeCsvValue(string? value)
    {
        if (string.IsNullOrEmpty(value)) return "\"\"";
        if (value.Contains(',') || value.Contains('\"') || value.Contains('\n') || value.Contains('\r'))
        {
            return $"\"{value.Replace("\"", "\"\"")}\"";
        }
        return value;
    }

    private void ApplyFilterAndSort()
    {
        IEnumerable<InteractiveTableRowItem> rows = AllRows;

        if (!string.IsNullOrWhiteSpace(FilterQuery))
        {
            rows = rows.Where(r => r.MatchesFilter(FilterQuery));
        }

        if (SortedColumnIndex.HasValue && SortedColumnIndex.Value < Headers.Count)
        {
            int colIdx = SortedColumnIndex.Value;
            bool isNumeric = rows.All(r =>
            {
                var val = r.GetCell(colIdx).Trim('$', '€', '£', '%', ' ', ',');
                return double.TryParse(val, out _);
            });

            if (isNumeric)
            {
                rows = SortAscending
                    ? rows.OrderBy(r => ParseNumericCell(r.GetCell(colIdx)))
                    : rows.OrderByDescending(r => ParseNumericCell(r.GetCell(colIdx)));
            }
            else
            {
                rows = SortAscending
                    ? rows.OrderBy(r => r.GetCell(colIdx), StringComparer.CurrentCultureIgnoreCase)
                    : rows.OrderByDescending(r => r.GetCell(colIdx), StringComparer.CurrentCultureIgnoreCase);
            }
        }

        DisplayedRows.Clear();
        foreach (var r in rows)
        {
            DisplayedRows.Add(r);
        }

        OnPropertyChanged(nameof(RowCountSummary));
        OnPropertyChanged(nameof(FilteredRowCount));
    }

    private static double ParseNumericCell(string text)
    {
        string cleaned = text.Trim('$', '€', '£', '%', ' ', ',');
        return double.TryParse(cleaned, out var num) ? num : 0.0;
    }
}
