using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Avalonia.Input.Platform;
using Avalonia.Media.Imaging;
using Avalonia.Platform.Storage;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using PdfEditorApp.Core.Analysis;
using PdfEditorApp.Core.Data;
using PdfEditorApp.Models;
using PdfEditorApp.Models.Elements;
using PdfEditorApp.Services;
using PdfEditorApp.ViewModels.ElementViewModels;

namespace PdfEditorApp.ViewModels.DataStudio;

public class DataColumnSelectionItem : ObservableObject
{
    public int Index { get; set; }
    public string Header { get; set; } = string.Empty;
    public DataColumnType ColumnType { get; set; }

    private bool _isSelected;
    public bool IsSelected
    {
        get => _isSelected;
        set => SetProperty(ref _isSelected, value);
    }
}

public class DataGridRowViewModel : ObservableObject
{
    public int RowIndex { get; set; }
    public ObservableCollection<string> Cells { get; } = new();

    public DataGridRowViewModel(int rowIndex, IEnumerable<string> cells)
    {
        RowIndex = rowIndex;
        foreach (var c in cells) Cells.Add(c);
    }
}

public partial class DataStudioViewModel : ViewModelBase
{
    private readonly IDataSourceService _dataSourceService;
    private readonly IDataBindingService _dataBindingService;

    public IStorageProvider? StorageProvider { get; set; }
    public IUndoRedoService? UndoRedo { get; set; }

    [ObservableProperty]
    private bool _isOpen;

    [ObservableProperty]
    private string _dialogTitle = "Data Studio & Connector";

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(IsChartTarget))]
    [NotifyPropertyChangedFor(nameof(IsTableTarget))]
    private string _targetMode = "Chart"; // "Chart", "Table", "NewChart", "NewTable"

    public bool IsChartTarget => TargetMode == "Chart" || TargetMode == "NewChart";
    public bool IsTableTarget => TargetMode == "Table" || TargetMode == "NewTable";

    public ChartElementViewModel? TargetChart { get; private set; }
    public TableElementViewModel? TargetTable { get; private set; }
    public PageViewModel? TargetPage { get; private set; }

    // Active Source Tab: 0 = Excel/Files, 1 = REST API, 2 = Spreadsheet / Clipboard
    [ObservableProperty]
    private int _selectedTabIndex = 0;

    // --- File Import State ---
    [ObservableProperty]
    private string _selectedFilePath = string.Empty;

    [ObservableProperty]
    private string _selectedFileName = string.Empty;

    public ObservableCollection<string> ExcelSheets { get; } = new();

    [ObservableProperty]
    private string? _selectedExcelSheet;

    [ObservableProperty]
    private bool _hasMultipleSheets;

    [ObservableProperty]
    private bool _firstRowIsHeader = true;

    [ObservableProperty]
    private string _selectedDelimiter = ",";

    // --- REST API State ---
    [ObservableProperty]
    private string _apiUrl = "https://dummyjson.com/products?limit=6";

    [ObservableProperty]
    private string _apiAuthBearer = string.Empty;

    [ObservableProperty]
    private string _apiKeyHeader = string.Empty;

    [ObservableProperty]
    private string _apiKeyValue = string.Empty;

    [ObservableProperty]
    private string _apiJsonPath = "products";

    [ObservableProperty]
    private bool _isFetchingApi;

    [ObservableProperty]
    private string _apiStatusMessage = "Ready to connect";

    [ObservableProperty]
    private bool _apiHasError;

    // --- Manual / Raw Text Input State ---
    [ObservableProperty]
    private string _rawTextInput = "Category\t2025 Actual\t2026 Target\nQ1\t120\t140\nQ2\t150\t175\nQ3\t180\t210\nQ4\t200\t240";

    // --- Live Matrix & Grid State ---
    public DataMatrix CurrentMatrix { get; private set; } = new();

    public ObservableCollection<string> MatrixHeaders { get; } = new();
    public ObservableCollection<DataGridRowViewModel> GridRows { get; } = new();
    public ObservableCollection<DataColumnSelectionItem> AvailableColumns { get; } = new();

    [ObservableProperty]
    private int _selectedCategoryColumnIndex = 0;

    [ObservableProperty]
    private Bitmap? _chartPreviewBitmap;

    [ObservableProperty]
    private string _matrixSummary = "0 rows, 0 columns loaded";

    public event Action<ElementViewModelBase, string>? OnElementCreated;

    public DataStudioViewModel(IDataSourceService dataSourceService, IDataBindingService dataBindingService)
    {
        _dataSourceService = dataSourceService;
        _dataBindingService = dataBindingService;

        LoadDefaultSampleMatrix();
    }

    public void OpenForChart(ChartElementViewModel chart, PageViewModel? page)
    {
        TargetChart = chart;
        TargetTable = null;
        TargetPage = page;
        TargetMode = "Chart";
        DialogTitle = $"Data Studio — Edit Data for '{chart.Title}'";

        // Extract current data from chart into matrix
        var model = (PdfChartElement)chart.ToModel();
        var matrix = _dataBindingService.ExtractFromChart(model);
        SetMatrix(matrix);

        IsOpen = true;
    }

    public void OpenForTable(TableElementViewModel table, PageViewModel? page)
    {
        TargetChart = null;
        TargetTable = table;
        TargetPage = page;
        TargetMode = "Table";
        DialogTitle = $"Data Studio — Edit Data for Table";

        var model = (PdfTableElement)table.ToModel();
        var matrix = _dataBindingService.ExtractFromTable(model);
        SetMatrix(matrix);

        IsOpen = true;
    }

    [RelayCommand]
    public void SelectTab(string tabIndexStr)
    {
        if (int.TryParse(tabIndexStr, out int idx))
        {
            SelectedTabIndex = idx;
        }
    }

    [RelayCommand]
    public void SetPresetApi(string presetKey)
    {
        switch (presetKey.ToLowerInvariant())
        {
            case "products":
            case "dummyjson":
                ApiUrl = "https://dummyjson.com/products?limit=8";
                ApiJsonPath = "products";
                break;

            case "crypto":
            case "coingecko":
                ApiUrl = "https://api.coingecko.com/api/v3/coins/markets?vs_currency=usd&order=market_cap_desc&per_page=8&page=1";
                ApiJsonPath = "";
                break;

            case "users":
            case "jsonplaceholder":
                ApiUrl = "https://jsonplaceholder.typicode.com/users";
                ApiJsonPath = "";
                break;

            case "sales":
                ApiUrl = "https://dummyjson.com/carts/1";
                ApiJsonPath = "products";
                break;
        }

        ApiStatusMessage = "Preset configured. Click 'Fetch API Data' to connect.";
        ApiHasError = false;
    }

    public void OpenForNew(string mode, PageViewModel? page)
    {
        TargetChart = null;
        TargetTable = null;
        TargetPage = page;
        TargetMode = mode;
        DialogTitle = mode == "NewChart" ? "Data Studio — Import Data to New Chart" : "Data Studio — Import Data to New Table";

        LoadDefaultSampleMatrix();
        IsOpen = true;
    }

    [RelayCommand]
    public void Close()
    {
        IsOpen = false;
    }

    private void LoadDefaultSampleMatrix()
    {
        var matrix = _dataSourceService.ParseTsv(RawTextInput, true);
        SetMatrix(matrix);
    }

    public void SetMatrix(DataMatrix matrix)
    {
        CurrentMatrix = matrix;
        MatrixHeaders.Clear();
        foreach (var h in matrix.Headers) MatrixHeaders.Add(h);

        GridRows.Clear();
        for (int r = 0; r < matrix.RowCount; r++)
        {
            GridRows.Add(new DataGridRowViewModel(r + 1, matrix.Rows[r]));
        }

        AvailableColumns.Clear();
        for (int c = 0; c < matrix.ColumnCount; c++)
        {
            var colType = matrix.InferColumnType(c);
            AvailableColumns.Add(new DataColumnSelectionItem
            {
                Index = c,
                Header = matrix.Headers[c],
                ColumnType = colType,
                IsSelected = c != SelectedCategoryColumnIndex
            });
        }

        SelectedCategoryColumnIndex = Math.Clamp(SelectedCategoryColumnIndex, 0, Math.Max(0, matrix.ColumnCount - 1));
        MatrixSummary = $"{matrix.RowCount} rows × {matrix.ColumnCount} columns loaded";

        UpdateChartPreview();
    }

    partial void OnSelectedCategoryColumnIndexChanged(int value)
    {
        UpdateChartPreview();
    }

    partial void OnFirstRowIsHeaderChanged(bool value)
    {
        if (!string.IsNullOrEmpty(SelectedFilePath) && File.Exists(SelectedFilePath))
        {
            ReloadCurrentFile();
        }
    }

    partial void OnSelectedExcelSheetChanged(string? value)
    {
        if (!string.IsNullOrEmpty(value) && !string.IsNullOrEmpty(SelectedFilePath) && File.Exists(SelectedFilePath))
        {
            ReloadCurrentFile();
        }
    }

    [RelayCommand]
    public async Task BrowseFileAsync()
    {
        if (StorageProvider == null) return;

        var files = await StorageProvider.OpenFilePickerAsync(new FilePickerOpenOptions
        {
            Title = "Select Excel Workbook, CSV, or Data File",
            AllowMultiple = false,
            FileTypeFilter = new[]
            {
                new FilePickerFileType("Supported Data Files (*.xlsx, *.csv, *.tsv, *.txt)")
                {
                    Patterns = new[] { "*.xlsx", "*.csv", "*.tsv", "*.txt" }
                },
                new FilePickerFileType("Excel Workbooks (*.xlsx)")
                {
                    Patterns = new[] { "*.xlsx" }
                },
                new FilePickerFileType("CSV / Delimited (*.csv, *.tsv)")
                {
                    Patterns = new[] { "*.csv", "*.tsv", "*.txt" }
                }
            }
        });

        if (files != null && files.Count > 0)
        {
            var file = files[0];
            string path = file.Path.LocalPath;
            await LoadFileAsync(path);
        }
    }

    public async Task LoadFileAsync(string filePath)
    {
        if (!File.Exists(filePath)) return;

        SelectedFilePath = filePath;
        SelectedFileName = Path.GetFileName(filePath);
        string ext = Path.GetExtension(filePath).ToLowerInvariant();

        try
        {
            if (ext == ".xlsx")
            {
                using var fs = File.OpenRead(filePath);
                var sheetNames = _dataSourceService.GetExcelSheetNames(fs);
                ExcelSheets.Clear();
                foreach (var name in sheetNames) ExcelSheets.Add(name);

                HasMultipleSheets = ExcelSheets.Count > 1;
                SelectedExcelSheet = ExcelSheets.FirstOrDefault();

                fs.Position = 0;
                var matrix = _dataSourceService.ParseExcel(fs, SelectedExcelSheet, FirstRowIsHeader);
                SetMatrix(matrix);
            }
            else
            {
                HasMultipleSheets = false;
                ExcelSheets.Clear();
                string text = await File.ReadAllTextAsync(filePath);
                char? delim = ext == ".tsv" ? '\t' : (SelectedDelimiter.Length > 0 ? SelectedDelimiter[0] : (char?)null);
                var matrix = _dataSourceService.ParseCsv(text, delim, FirstRowIsHeader);
                SetMatrix(matrix);
            }
        }
        catch (Exception ex)
        {
            ApiHasError = true;
            ApiStatusMessage = $"File read error: {ex.Message}";
        }
    }

    private void ReloadCurrentFile()
    {
        if (!string.IsNullOrEmpty(SelectedFilePath) && File.Exists(SelectedFilePath))
        {
            _ = LoadFileAsync(SelectedFilePath);
        }
    }

    [RelayCommand]
    public async Task FetchApiDataAsync()
    {
        if (string.IsNullOrWhiteSpace(ApiUrl)) return;

        IsFetchingApi = true;
        ApiHasError = false;
        ApiStatusMessage = "Connecting to API endpoint...";

        try
        {
            var headers = new Dictionary<string, string>();
            if (!string.IsNullOrWhiteSpace(ApiAuthBearer))
            {
                headers["Authorization"] = ApiAuthBearer.StartsWith("Bearer ", StringComparison.OrdinalIgnoreCase)
                    ? ApiAuthBearer
                    : $"Bearer {ApiAuthBearer}";
            }
            if (!string.IsNullOrWhiteSpace(ApiKeyHeader) && !string.IsNullOrWhiteSpace(ApiKeyValue))
            {
                headers[ApiKeyHeader] = ApiKeyValue;
            }

            var matrix = await _dataSourceService.FetchFromRestApiAsync(ApiUrl, headers, ApiJsonPath);
            if (matrix.RowCount == 0)
            {
                ApiHasError = true;
                ApiStatusMessage = "API returned 0 rows or empty structure.";
            }
            else
            {
                SetMatrix(matrix);
                ApiStatusMessage = $"Successfully fetched {matrix.RowCount} rows from API!";
            }
        }
        catch (Exception ex)
        {
            ApiHasError = true;
            ApiStatusMessage = $"API Error: {ex.Message}";
        }
        finally
        {
            IsFetchingApi = false;
        }
    }

    [RelayCommand]
    public async Task PasteFromClipboardAsync()
    {
        if (Avalonia.Application.Current?.ApplicationLifetime is Avalonia.Controls.ApplicationLifetimes.IClassicDesktopStyleApplicationLifetime desktop &&
            desktop.MainWindow?.Clipboard != null)
        {
            string? text = await desktop.MainWindow.Clipboard.TryGetTextAsync();
            if (!string.IsNullOrWhiteSpace(text))
            {
                RawTextInput = text;
                var matrix = _dataSourceService.ParseCsv(text, null, FirstRowIsHeader);
                SetMatrix(matrix);
            }
        }
    }

    [RelayCommand]
    public void ParseRawTextInput()
    {
        if (!string.IsNullOrWhiteSpace(RawTextInput))
        {
            var matrix = _dataSourceService.ParseCsv(RawTextInput, null, FirstRowIsHeader);
            SetMatrix(matrix);
        }
    }

    [RelayCommand]
    public void AddRow()
    {
        var cells = Enumerable.Range(1, CurrentMatrix.ColumnCount).Select(c => $"New {GridRows.Count + 1}.{c}");
        CurrentMatrix.AddRow(cells);
        SetMatrix(CurrentMatrix);
    }

    [RelayCommand]
    public void RemoveRow()
    {
        if (CurrentMatrix.RowCount > 1)
        {
            CurrentMatrix.RemoveRow(CurrentMatrix.RowCount - 1);
            SetMatrix(CurrentMatrix);
        }
    }

    [RelayCommand]
    public void AddColumn()
    {
        CurrentMatrix.AddColumn($"Col {CurrentMatrix.ColumnCount + 1}");
        SetMatrix(CurrentMatrix);
    }

    [RelayCommand]
    public void RemoveColumn()
    {
        if (CurrentMatrix.ColumnCount > 1)
        {
            CurrentMatrix.RemoveColumn(CurrentMatrix.ColumnCount - 1);
            SetMatrix(CurrentMatrix);
        }
    }

    private void UpdateChartPreview()
    {
        if (CurrentMatrix.RowCount == 0 || CurrentMatrix.ColumnCount == 0)
        {
            ChartPreviewBitmap = null;
            return;
        }

        try
        {
            var previewChart = new PdfChartElement
            {
                Width = 360,
                Height = 200,
                Title = TargetChart?.Title ?? "Data Preview",
                ChartType = TargetChart?.ChartType ?? ChartType.BarColumn,
                Palette = TargetChart?.Palette ?? ChartPalette.CorporateBlue,
                LegendPosition = ChartLegendPosition.Top
            };

            int catCol = SelectedCategoryColumnIndex;
            var valCols = AvailableColumns.Where(c => c.IsSelected && c.Index != catCol).Select(c => c.Index).ToList();
            if (valCols.Count == 0)
            {
                valCols.Add(catCol == 0 ? Math.Min(1, CurrentMatrix.ColumnCount - 1) : 0);
            }

            _dataBindingService.ApplyToChart(CurrentMatrix, previewChart, catCol, valCols);

            byte[] png = LiveChartsRenderer.RenderChartToPngBytes(previewChart, 360, 200, 2.0f);
            if (png != null && png.Length > 0)
            {
                using var ms = new MemoryStream(png);
                ChartPreviewBitmap = new Bitmap(ms);
            }
        }
        catch
        {
            ChartPreviewBitmap = null;
        }
    }

    [RelayCommand]
    public void ApplyData()
    {
        if (CurrentMatrix.RowCount == 0 || CurrentMatrix.ColumnCount == 0)
        {
            IsOpen = false;
            return;
        }

        int catCol = SelectedCategoryColumnIndex;
        var valCols = AvailableColumns.Where(c => c.IsSelected && c.Index != catCol).Select(c => c.Index).ToList();
        if (valCols.Count == 0)
        {
            valCols.Add(catCol == 0 ? Math.Min(1, CurrentMatrix.ColumnCount - 1) : 0);
        }

        if (TargetMode == "Chart" && TargetChart != null)
        {
            var oldModel = (PdfChartElement)TargetChart.ToModel();
            var newModel = (PdfChartElement)oldModel.Clone();

            _dataBindingService.ApplyToChart(CurrentMatrix, newModel, catCol, valCols);

            TargetChart.LoadFromModel(newModel);
            UndoRedo?.RecordAction(
                "Update Chart Data",
                () => TargetChart.LoadFromModel(oldModel),
                () => TargetChart.LoadFromModel(newModel)
            );
        }
        else if (TargetMode == "Table" && TargetTable != null)
        {
            var oldModel = (PdfTableElement)TargetTable.ToModel();
            var newModel = (PdfTableElement)oldModel.Clone();

            _dataBindingService.ApplyToTable(CurrentMatrix, newModel);

            TargetTable.LoadFromModel(newModel);
            UndoRedo?.RecordAction(
                "Update Table Data",
                () => TargetTable.LoadFromModel(oldModel),
                () => TargetTable.LoadFromModel(newModel)
            );
        }
        else if (TargetMode == "NewChart" && TargetPage != null)
        {
            var newChartModel = new PdfChartElement
            {
                X = 50,
                Y = 150,
                Width = 420,
                Height = 240,
                Title = "Imported Data Analysis",
                ChartType = ChartType.BarColumn,
                Palette = ChartPalette.CorporateBlue
            };
            _dataBindingService.ApplyToChart(CurrentMatrix, newChartModel, catCol, valCols);

            var chartVm = new ChartElementViewModel();
            chartVm.LoadFromModel(newChartModel);
            OnElementCreated?.Invoke(chartVm, "Imported Chart Data");
        }
        else if (TargetMode == "NewTable" && TargetPage != null)
        {
            var newTableModel = new PdfTableElement
            {
                X = 50,
                Y = 150,
                Width = 450,
                Height = 35 + (CurrentMatrix.RowCount * 26)
            };
            _dataBindingService.ApplyToTable(CurrentMatrix, newTableModel);

            var tableVm = new TableElementViewModel();
            tableVm.LoadFromModel(newTableModel);
            OnElementCreated?.Invoke(tableVm, "Imported Table Data");
        }

        IsOpen = false;
    }

    [RelayCommand]
    public async Task ExportToCsvAsync()
    {
        if (StorageProvider == null || CurrentMatrix.RowCount == 0) return;

        var file = await StorageProvider.SaveFilePickerAsync(new FilePickerSaveOptions
        {
            Title = "Export Data as CSV",
            DefaultExtension = "csv",
            SuggestedFileName = "dataset_export.csv",
            FileTypeChoices = new[]
            {
                new FilePickerFileType("CSV (Comma Delimited) (*.csv)") { Patterns = new[] { "*.csv" } }
            }
        });

        if (file != null)
        {
            string csvContent = _dataSourceService.FormatAsCsv(CurrentMatrix);
            await File.WriteAllTextAsync(file.Path.LocalPath, csvContent);
        }
    }
}
