using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Avalonia.Media.Imaging;
using Avalonia.Platform.Storage;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using PdfEditorApp.Core.Analysis;
using PdfEditorApp.Core.Data;
using PdfEditorApp.Core.Models;
using PdfEditorApp.Core.Models.Elements;
using PdfEditorApp.Models;
using PdfEditorApp.Services;
using PdfEditorApp.ViewModels.DataStudio;

namespace PdfEditorApp.ViewModels.BatchGeneration;

public partial class FieldMappingItemViewModel : ObservableObject
{
    [ObservableProperty]
    private string _placeholderTag = string.Empty;

    [ObservableProperty]
    private string _selectedColumnName = string.Empty;

    [ObservableProperty]
    private FieldTransformType _transform = FieldTransformType.None;

    [ObservableProperty]
    private string _customFormat = string.Empty;

    [ObservableProperty]
    private string _defaultValue = string.Empty;

    [ObservableProperty]
    private string _sampleValue = string.Empty;

    public ObservableCollection<string> AvailableColumns { get; } = new();

    public FieldMappingItem ToModel()
    {
        return new FieldMappingItem
        {
            PlaceholderTag = PlaceholderTag,
            DataColumnName = SelectedColumnName,
            Transform = Transform,
            CustomFormat = CustomFormat,
            DefaultValue = DefaultValue,
            SampleValue = SampleValue
        };
    }
}

public class BuiltInTemplateOption
{
    public string Id { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string Category { get; set; } = string.Empty;
    public string IconKind { get; set; } = "FileDocumentOutline";
}

public partial class BatchGenerationViewModel : ViewModelBase
{
    private readonly IDataSourceService _dataSourceService;
    private readonly IDataMergeEngine _mergeEngine;
    private readonly IBatchPdfGenerator _batchGenerator;
    private readonly ITemplateService _templateService;

    public IStorageProvider? StorageProvider { get; set; }
    private CancellationTokenSource? _generationCts;

    [ObservableProperty]
    private bool _isOpen;

    [ObservableProperty]
    private string _dialogTitle = "Batch Mail Merge & Mass PDF Generator";

    // 0: Data & Template, 1: Field Mapping, 2: Live Preview, 3: Output & Execution
    [ObservableProperty]
    private int _selectedStepIndex = 0;

    // --- Template Configuration ---
    [ObservableProperty]
    private string _selectedTemplateMode = "CurrentDocument"; // "CurrentDocument", "BuiltInTemplate"

    public ObservableCollection<BuiltInTemplateOption> BuiltInTemplates { get; } = new();

    [ObservableProperty]
    private BuiltInTemplateOption? _selectedBuiltInTemplate;

    public PdfDocumentModel ActiveTemplateModel { get; private set; } = new();
    public ObservableCollection<string> DetectedPlaceholders { get; } = new();

    // --- Data Source Configuration ---
    [ObservableProperty]
    private int _selectedDataSourceTab = 0; // 0 = Excel/CSV, 1 = REST API, 2 = Manual/Paste

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
    private string _apiUrl = "https://dummyjson.com/users?limit=8";

    [ObservableProperty]
    private string _apiAuthBearer = string.Empty;

    [ObservableProperty]
    private string _apiKeyHeader = string.Empty;

    [ObservableProperty]
    private string _apiKeyValue = string.Empty;

    [ObservableProperty]
    private string _apiJsonPath = "users";

    [ObservableProperty]
    private bool _isFetchingApi;

    [ObservableProperty]
    private string _apiStatusMessage = "Ready to connect";

    [ObservableProperty]
    private bool _apiHasError;

    // --- Manual / Raw Input State ---
    [ObservableProperty]
    private string _rawTextInput = "";

    // --- Active Matrix & Grid ---
    public DataMatrix CurrentMatrix { get; private set; } = new();
    public ObservableCollection<string> MatrixHeaders { get; } = new();
    public ObservableCollection<DataGridRowViewModel> GridRows { get; } = new();

    [ObservableProperty]
    private string _matrixSummary = "0 rows, 0 columns loaded";

    // --- Field Mappings ---
    public ObservableCollection<FieldMappingItemViewModel> FieldMappings { get; } = new();

    // --- Live Record Preview ---
    [ObservableProperty]
    private int _currentPreviewIndex = 0;

    [ObservableProperty]
    private int _totalRecordsCount = 0;

    [ObservableProperty]
    private string _currentRecordSummary = "Record 0 of 0";

    [ObservableProperty]
    private Bitmap? _previewBitmap;

    // --- Output Configuration ---
    [ObservableProperty]
    private BatchOutputMode _outputMode = BatchOutputMode.SeparateFiles;

    [ObservableProperty]
    private string _outputDirectory = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.Desktop), "FryPDF_Batch_Export");

    [ObservableProperty]
    private string _outputFilePath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.Desktop), "FryPDF_Batch_Export", "All_Batch_Documents.pdf");

    [ObservableProperty]
    private string _filenamePattern = "Payslip_{{EmployeeId}}_{{EmployeeName}}_{{PayPeriod}}.pdf";

    [ObservableProperty]
    private bool _skipEmptyRows = true;

    // --- Execution & Progress State ---
    [ObservableProperty]
    private bool _isGenerating;

    [ObservableProperty]
    private bool _isCompleted;

    [ObservableProperty]
    private double _progressPercentage;

    [ObservableProperty]
    private string _progressMessage = "Ready to generate";

    [ObservableProperty]
    private string _currentItemName = "";

    [ObservableProperty]
    private int _succeededCount;

    [ObservableProperty]
    private int _failedCount;

    [ObservableProperty]
    private string _generationSummary = "";

    public BatchGenerationResult? LastResult { get; private set; }

    public BatchGenerationViewModel(
        IDataSourceService dataSourceService,
        IDataMergeEngine mergeEngine,
        IBatchPdfGenerator batchGenerator,
        ITemplateService templateService)
    {
        _dataSourceService = dataSourceService ?? throw new ArgumentNullException(nameof(dataSourceService));
        _mergeEngine = mergeEngine ?? throw new ArgumentNullException(nameof(mergeEngine));
        _batchGenerator = batchGenerator ?? throw new ArgumentNullException(nameof(batchGenerator));
        _templateService = templateService ?? throw new ArgumentNullException(nameof(templateService));

        InitializeBuiltInTemplates();
        LoadDefaultSamplePayslipDataset();
    }

    private void InitializeBuiltInTemplates()
    {
        BuiltInTemplates.Clear();
        BuiltInTemplates.Add(new BuiltInTemplateOption { Id = "payslip", Name = "Employee Monthly Payslip", Category = "Corporate", IconKind = "CashMultiple" });
        BuiltInTemplates.Add(new BuiltInTemplateOption { Id = "certificate", Name = "Achievement Certificate", Category = "Certificates", IconKind = "CertificateOutline" });
        BuiltInTemplates.Add(new BuiltInTemplateOption { Id = "invoice", Name = "Commercial Invoice", Category = "Finance", IconKind = "ReceiptTextOutline" });
        BuiltInTemplates.Add(new BuiltInTemplateOption { Id = "annualreport", Name = "Executive Report", Category = "Corporate", IconKind = "FileChartOutline" });

        SelectedBuiltInTemplate = BuiltInTemplates.FirstOrDefault();
    }

    public void OpenWithDocument(PdfDocumentModel currentDoc, DataMatrix? preloadedMatrix = null)
    {
        ActiveTemplateModel = currentDoc?.Clone() ?? _templateService.CreateEmployeePayslipTemplate();
        SelectedTemplateMode = "CurrentDocument";

        if (preloadedMatrix != null && preloadedMatrix.RowCount > 0)
        {
            SetMatrix(preloadedMatrix);
        }
        else if (CurrentMatrix.RowCount == 0)
        {
            LoadDefaultSamplePayslipDataset();
        }

        RefreshTemplatePlaceholders();
        AutoMapFields();

        SelectedStepIndex = 0;
        IsGenerating = false;
        IsCompleted = false;
        IsOpen = true;

        UpdateLiveRecordPreview();
    }

    [RelayCommand]
    public void SelectStep(string stepIndexStr)
    {
        if (int.TryParse(stepIndexStr, out int idx))
        {
            SelectedStepIndex = Math.Clamp(idx, 0, 3);
            if (SelectedStepIndex == 2)
            {
                UpdateLiveRecordPreview();
            }
        }
    }

    [RelayCommand]
    public void SelectDataSourceTab(string tabIndexStr)
    {
        if (int.TryParse(tabIndexStr, out int idx))
        {
            SelectedDataSourceTab = Math.Clamp(idx, 0, 2);
        }
    }

    [RelayCommand]
    public void NextStep()
    {
        SelectedStepIndex = Math.Clamp(SelectedStepIndex + 1, 0, 3);
        if (SelectedStepIndex == 2)
        {
            UpdateLiveRecordPreview();
        }
    }

    [RelayCommand]
    public void PreviousStep()
    {
        SelectedStepIndex = Math.Clamp(SelectedStepIndex - 1, 0, 3);
    }

    [RelayCommand]
    public void Close()
    {
        if (IsGenerating && _generationCts != null)
        {
            _generationCts.Cancel();
        }
        IsOpen = false;
    }

    public void SetTemplate(PdfDocumentModel doc)
    {
        ActiveTemplateModel = doc.Clone();
        RefreshTemplatePlaceholders();
        AutoMapFields();
        UpdateLiveRecordPreview();
    }

    partial void OnSelectedBuiltInTemplateChanged(BuiltInTemplateOption? value)
    {
        if (value != null && SelectedTemplateMode == "BuiltInTemplate")
        {
            var templateDoc = _templateService.CreateTemplate(value.Id);
            SetTemplate(templateDoc);
        }
    }

    partial void OnSelectedTemplateModeChanged(string value)
    {
        if (value == "BuiltInTemplate" && SelectedBuiltInTemplate != null)
        {
            var templateDoc = _templateService.CreateTemplate(SelectedBuiltInTemplate.Id);
            SetTemplate(templateDoc);
        }
    }

    public void RefreshTemplatePlaceholders()
    {
        DetectedPlaceholders.Clear();
        var tags = _mergeEngine.DetectPlaceholders(ActiveTemplateModel);
        foreach (var t in tags)
        {
            DetectedPlaceholders.Add(t);
        }

        // Adjust suggested filename pattern if placeholders detected
        if (DetectedPlaceholders.Contains("EmployeeId") && DetectedPlaceholders.Contains("EmployeeName"))
        {
            FilenamePattern = "Payslip_{{EmployeeId}}_{{EmployeeName}}_{{PayPeriod}}.pdf";
        }
        else if (DetectedPlaceholders.Count > 0)
        {
            FilenamePattern = $"Doc_{{{{Index}}}}_{{{{{DetectedPlaceholders[0]}}}}}.pdf";
        }
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

        TotalRecordsCount = matrix.RowCount;
        MatrixSummary = $"{matrix.RowCount} records × {matrix.ColumnCount} columns ready for merge";
        CurrentPreviewIndex = 0;

        AutoMapFields();
        UpdateLiveRecordPreview();
    }

    [RelayCommand]
    public void AutoMapFields()
    {
        FieldMappings.Clear();

        foreach (var tag in DetectedPlaceholders)
        {
            var mapping = new FieldMappingItemViewModel
            {
                PlaceholderTag = tag
            };

            foreach (var col in MatrixHeaders)
            {
                mapping.AvailableColumns.Add(col);
            }

            // Find best matching column name
            string? matchedCol = MatrixHeaders.FirstOrDefault(h => string.Equals(h, tag, StringComparison.OrdinalIgnoreCase));
            if (matchedCol == null)
            {
                matchedCol = MatrixHeaders.FirstOrDefault(h =>
                    h.Replace(" ", "").Replace("_", "").Equals(tag.Replace(" ", "").Replace("_", ""), StringComparison.OrdinalIgnoreCase));
            }

            mapping.SelectedColumnName = matchedCol ?? MatrixHeaders.FirstOrDefault() ?? string.Empty;

            // Infer transform
            if (tag.Contains("Salary", StringComparison.OrdinalIgnoreCase) ||
                tag.Contains("Earnings", StringComparison.OrdinalIgnoreCase) ||
                tag.Contains("Deductions", StringComparison.OrdinalIgnoreCase) ||
                tag.Contains("Amount", StringComparison.OrdinalIgnoreCase) ||
                tag.Contains("Price", StringComparison.OrdinalIgnoreCase) ||
                tag.Contains("Bonus", StringComparison.OrdinalIgnoreCase) ||
                tag.Contains("Allowance", StringComparison.OrdinalIgnoreCase) ||
                tag.Contains("HRA", StringComparison.OrdinalIgnoreCase) ||
                tag.Contains("Tax", StringComparison.OrdinalIgnoreCase))
            {
                mapping.Transform = FieldTransformType.Currency;
            }
            else if (tag.Contains("Date", StringComparison.OrdinalIgnoreCase) || tag.Contains("Period", StringComparison.OrdinalIgnoreCase))
            {
                mapping.Transform = FieldTransformType.Date;
            }

            // Populate sample value from Row 0
            if (CurrentMatrix.RowCount > 0 && !string.IsNullOrEmpty(mapping.SelectedColumnName))
            {
                int colIdx = CurrentMatrix.Headers.IndexOf(mapping.SelectedColumnName);
                if (colIdx >= 0)
                {
                    mapping.SampleValue = CurrentMatrix.GetCellValue(0, colIdx);
                }
            }

            FieldMappings.Add(mapping);
        }
    }

    [RelayCommand]
    public void LoadDefaultSamplePayslipDataset()
    {
        // 1. Create Payslip Template
        var payslipDoc = _templateService.CreateEmployeePayslipTemplate();
        SetTemplate(payslipDoc);

        // 2. High-fidelity CSV dataset with 6 realistic employee records
        string sampleCsv =
@"EmployeeId,EmployeeName,Designation,Department,JoiningDate,BankName,AccountNumber,TaxId,WorkingDays,BasicSalary,HRA,SpecialAllowance,Bonus,MedicalAllowance,GrossEarnings,ProvidentFund,IncomeTax,ProfessionalTax,Insurance,OtherDeductions,TotalDeductions,NetSalary,NetSalaryInWords,PayPeriod,CompanyName,AuthHash
EMP-2026-0842,John Doe,Senior Software Architect,Cloud Infrastructure,2021-03-15,First National Bank,****8492,US-TAX-8429,30,8500,3400,1800,1200,600,15500,950,1850,200,350,0,3350,12150,Twelve Thousand One Hundred Fifty US Dollars,August 2026,CodeFryDev Inc.,9A4F-8201-B732
EMP-2026-0914,Jane Doe,Principal UI/UX Designer,Product Experience,2022-06-01,Metro Commercial Bank,****3198,US-TAX-1942,30,7800,3120,1500,900,500,13820,850,1550,200,300,0,2900,10920,Ten Thousand Nine Hundred Twenty US Dollars,August 2026,CodeFryDev Inc.,7E2B-9410-C311
EMP-2026-1052,Alex Doe,Lead DevOps & Reliability Engineer,Cloud Infrastructure,2020-11-10,Standard City Bank,****9041,US-TAX-7731,30,8200,3280,1650,1100,550,14780,900,1720,200,320,0,3140,11640,Eleven Thousand Six Hundred Forty US Dollars,August 2026,CodeFryDev Inc.,4D1C-8822-A904
EMP-2026-1188,Sam Doe,VP of Product Management,Executive Staff,2019-08-20,Apex Commerce Bank,****4412,US-TAX-5510,30,11000,4400,2400,2000,800,20600,1200,2900,200,450,0,4750,15850,Fifteen Thousand Eight Hundred Fifty US Dollars,August 2026,CodeFryDev Inc.,8F3D-1190-E542
EMP-2026-1240,Taylor Doe,Senior QA Automation Engineer,Quality Assurance,2023-01-16,Pacific Trust Bank,****6720,US-TAX-3398,30,6900,2760,1300,750,450,12160,780,1320,200,280,0,2580,9580,Nine Thousand Five Hundred Eighty US Dollars,August 2026,CodeFryDev Inc.,2A9E-6541-D881
EMP-2026-1315,Jordan Doe,Director of People & Culture,Human Resources,2021-09-01,Union Federal Bank,****5593,US-TAX-6624,30,9200,3680,1900,1400,650,16830,1050,2150,200,380,0,3780,13050,Thirteen Thousand Fifty US Dollars,August 2026,CodeFryDev Inc.,5C7F-3309-F104";

        var matrix = _dataSourceService.ParseCsv(sampleCsv, ',', true);
        SetMatrix(matrix);
    }

    [RelayCommand]
    public async Task BrowseFileAsync()
    {
        if (StorageProvider == null) return;

        var files = await StorageProvider.OpenFilePickerAsync(new FilePickerOpenOptions
        {
            Title = "Select Dataset for Batch Merge (Excel, CSV, TSV)",
            AllowMultiple = false,
            FileTypeFilter = new[]
            {
                new FilePickerFileType("Supported Datasets (*.xlsx, *.csv, *.tsv)")
                {
                    Patterns = new[] { "*.xlsx", "*.csv", "*.tsv", "*.txt" }
                },
                new FilePickerFileType("Excel Workbooks (*.xlsx)")
                {
                    Patterns = new[] { "*.xlsx" }
                },
                new FilePickerFileType("CSV Delimited (*.csv, *.tsv)")
                {
                    Patterns = new[] { "*.csv", "*.tsv" }
                }
            }
        });

        if (files != null && files.Count > 0)
        {
            var file = files[0];
            await LoadFileAsync(file.Path.LocalPath);
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
            ApiStatusMessage = $"File error: {ex.Message}";
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
                ApiStatusMessage = "API returned 0 records.";
            }
            else
            {
                SetMatrix(matrix);
                ApiStatusMessage = $"Successfully fetched {matrix.RowCount} records from REST API!";
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
    public async Task BrowseOutputDirectoryAsync()
    {
        if (StorageProvider == null) return;

        var folders = await StorageProvider.OpenFolderPickerAsync(new FolderPickerOpenOptions
        {
            Title = "Select Output Directory for Generated PDFs",
            AllowMultiple = false
        });

        if (folders != null && folders.Count > 0)
        {
            OutputDirectory = folders[0].Path.LocalPath;
            OutputFilePath = Path.Combine(OutputDirectory, "All_Merged_Documents.pdf");
        }
    }

    [RelayCommand]
    public async Task BrowseOutputFilePathAsync()
    {
        if (StorageProvider == null) return;

        string defaultExt = OutputMode == BatchOutputMode.ZipArchive ? "zip" : "pdf";
        string filterName = OutputMode == BatchOutputMode.ZipArchive ? "ZIP Archive (*.zip)" : "PDF Document (*.pdf)";
        string defaultPattern = OutputMode == BatchOutputMode.ZipArchive ? "*.zip" : "*.pdf";

        var file = await StorageProvider.SaveFilePickerAsync(new FilePickerSaveOptions
        {
            Title = "Select Output Destination File",
            DefaultExtension = defaultExt,
            SuggestedFileName = OutputMode == BatchOutputMode.ZipArchive ? "Batch_PDFs_Archive.zip" : "All_Merged_Documents.pdf",
            FileTypeChoices = new[]
            {
                new FilePickerFileType(filterName) { Patterns = new[] { defaultPattern } }
            }
        });

        if (file != null)
        {
            OutputFilePath = file.Path.LocalPath;
        }
    }

    [RelayCommand]
    public void InsertPlaceholderToFilename(string tag)
    {
        if (!string.IsNullOrWhiteSpace(tag))
        {
            FilenamePattern += $"_{{{{{tag}}}}}";
        }
    }

    // --- Live Record Navigation ---
    [RelayCommand]
    public void NextPreviewRecord()
    {
        if (CurrentMatrix.RowCount > 0)
        {
            CurrentPreviewIndex = (CurrentPreviewIndex + 1) % CurrentMatrix.RowCount;
            UpdateLiveRecordPreview();
        }
    }

    [RelayCommand]
    public void PreviousPreviewRecord()
    {
        if (CurrentMatrix.RowCount > 0)
        {
            CurrentPreviewIndex = (CurrentPreviewIndex - 1 + CurrentMatrix.RowCount) % CurrentMatrix.RowCount;
            UpdateLiveRecordPreview();
        }
    }

    public void UpdateLiveRecordPreview()
    {
        if (CurrentMatrix.RowCount == 0 || ActiveTemplateModel == null)
        {
            PreviewBitmap = null;
            CurrentRecordSummary = "No records available";
            return;
        }

        CurrentPreviewIndex = Math.Clamp(CurrentPreviewIndex, 0, CurrentMatrix.RowCount - 1);
        var mappings = FieldMappings.Select(m => m.ToModel()).ToList();
        var record = BatchPdfGeneratorService.BuildRecordDictionary(CurrentMatrix, CurrentPreviewIndex, mappings);

        // Build brief summary for UI display
        string primaryName = record.TryGetValue("EmployeeName", out var name) ? name :
                            (record.TryGetValue("Name", out var n) ? n :
                            (CurrentMatrix.ColumnCount > 1 ? CurrentMatrix.GetCellValue(CurrentPreviewIndex, 1) : ""));

        string primaryId = record.TryGetValue("EmployeeId", out var id) ? id :
                          (record.TryGetValue("Id", out var i) ? i :
                          (CurrentMatrix.ColumnCount > 0 ? CurrentMatrix.GetCellValue(CurrentPreviewIndex, 0) : ""));

        CurrentRecordSummary = $"Record {CurrentPreviewIndex + 1} of {CurrentMatrix.RowCount}: {primaryName} (ID: {primaryId})";

        try
        {
            // Hydrate document model
            var hydratedDoc = _mergeEngine.HydrateDocument(ActiveTemplateModel, record);

            // Rasterize preview using Skia renderer
            if (hydratedDoc.Pages.Count > 0)
            {
                var firstPage = hydratedDoc.Pages[0];
                float w = (float)firstPage.Width;
                float h = (float)firstPage.Height;

                using var surface = SkiaSharp.SKSurface.Create(new SkiaSharp.SKImageInfo((int)w, (int)h));
                var canvas = surface.Canvas;
                canvas.Clear(SkiaSharp.SKColors.White);

                // Render basic elements preview
                foreach (var el in firstPage.Elements.OrderBy(e => e.ZIndex))
                {
                    if (el is PdfShapeElement shape)
                    {
                        using var paint = new SkiaSharp.SKPaint
                        {
                            Color = SkiaSharp.SKColor.TryParse(shape.FillColorHex, out var c) ? c : SkiaSharp.SKColors.LightGray,
                            Style = SkiaSharp.SKPaintStyle.Fill,
                            IsAntialias = true
                        };
                        canvas.DrawRoundRect(new SkiaSharp.SKRect((float)shape.X, (float)shape.Y, (float)(shape.X + shape.Width), (float)(shape.Y + shape.Height)), (float)shape.CornerRadius, (float)shape.CornerRadius, paint);
                    }
                    else if (el is PdfTextElement text)
                    {
                        using var font = new SkiaSharp.SKFont(SkiaSharp.SKTypeface.Default, (float)Math.Max(8, text.FontSize));
                        font.Embolden = text.IsBold;
                        using var textPaint = new SkiaSharp.SKPaint
                        {
                            Color = SkiaSharp.SKColor.TryParse(text.TextColorHex, out var tc) ? tc : SkiaSharp.SKColors.Black,
                            IsAntialias = true
                        };
                        float yOffset = (float)text.Y + (float)text.FontSize;
                        string[] lines = text.Text.Split('\n');
                        foreach (var l in lines)
                        {
                            canvas.DrawText(l, (float)text.X, yOffset, SkiaSharp.SKTextAlign.Left, font, textPaint);
                            yOffset += (float)(text.FontSize * text.LineHeight);
                        }
                    }
                    else if (el is PdfTableElement table)
                    {
                        using var headerBg = new SkiaSharp.SKPaint { Color = SkiaSharp.SKColor.Parse(table.HeaderBackgroundHex), Style = SkiaSharp.SKPaintStyle.Fill };
                        canvas.DrawRect(new SkiaSharp.SKRect((float)table.X, (float)table.Y, (float)(table.X + table.Width), (float)(table.Y + 24)), headerBg);

                        using var fontHeader = new SkiaSharp.SKFont(SkiaSharp.SKTypeface.Default, 10);
                        fontHeader.Embolden = true;
                        using var textPaint = new SkiaSharp.SKPaint { Color = SkiaSharp.SKColors.White, IsAntialias = true };
                        canvas.DrawText(table.Headers.FirstOrDefault() ?? "TABLE", (float)table.X + 8, (float)table.Y + 16, SkiaSharp.SKTextAlign.Left, fontHeader, textPaint);

                        float rowY = (float)table.Y + 24;
                        using var fontCell = new SkiaSharp.SKFont(SkiaSharp.SKTypeface.Default, 9);
                        using var cellPaint = new SkiaSharp.SKPaint { Color = SkiaSharp.SKColors.Black, IsAntialias = true };

                        for (int r = 0; r < Math.Min(6, table.Rows.Count); r++)
                        {
                            using var rowBg = new SkiaSharp.SKPaint { Color = r % 2 == 0 ? SkiaSharp.SKColors.White : SkiaSharp.SKColor.Parse("#F8FAFC"), Style = SkiaSharp.SKPaintStyle.Fill };
                            canvas.DrawRect(new SkiaSharp.SKRect((float)table.X, rowY, (float)(table.X + table.Width), rowY + 20), rowBg);

                            string cellText = string.Join(" | ", table.Rows[r]);
                            canvas.DrawText(cellText, (float)table.X + 8, rowY + 14, SkiaSharp.SKTextAlign.Left, fontCell, cellPaint);
                            rowY += 20;
                        }
                    }
                    else if (el is PdfQrCodeElement qr)
                    {
                        using var qrPaint = new SkiaSharp.SKPaint { Color = SkiaSharp.SKColor.Parse(qr.DarkColorHex), Style = SkiaSharp.SKPaintStyle.Fill };
                        canvas.DrawRect(new SkiaSharp.SKRect((float)qr.X, (float)qr.Y, (float)(qr.X + qr.Width), (float)(qr.Y + qr.Height)), qrPaint);

                        using var fontQr = new SkiaSharp.SKFont(SkiaSharp.SKTypeface.Default, 9);
                        using var tPaint = new SkiaSharp.SKPaint { Color = SkiaSharp.SKColors.White, IsAntialias = true };
                        canvas.DrawText("QR CODE", (float)qr.X + 10, (float)qr.Y + (float)qr.Height / 2, SkiaSharp.SKTextAlign.Left, fontQr, tPaint);
                    }
                }

                using var image = surface.Snapshot();
                using var data = image.Encode(SkiaSharp.SKEncodedImageFormat.Png, 90);
                using var ms = new MemoryStream(data.ToArray());
                PreviewBitmap = new Bitmap(ms);
            }
        }
        catch
        {
            PreviewBitmap = null;
        }
    }

    // --- Execution ---
    [RelayCommand]
    public async Task StartGenerationAsync()
    {
        if (CurrentMatrix.RowCount == 0)
        {
            ProgressMessage = "No data rows loaded to generate.";
            return;
        }

        IsGenerating = true;
        IsCompleted = false;
        ProgressPercentage = 0;
        SucceededCount = 0;
        FailedCount = 0;
        ProgressMessage = "Initializing batch generation engine...";

        _generationCts = new CancellationTokenSource();

        var config = new BatchGenerationConfig
        {
            OutputMode = OutputMode,
            OutputDirectory = OutputDirectory,
            OutputFilePath = OutputFilePath,
            FilenamePattern = FilenamePattern,
            SkipEmptyRows = SkipEmptyRows
        };

        var mappings = FieldMappings.Select(m => m.ToModel()).ToList();

        var progress = new Progress<BatchProgressReport>(report =>
        {
            ProgressPercentage = report.Percentage;
            SucceededCount = report.SucceededCount;
            FailedCount = report.FailedCount;
            CurrentItemName = report.CurrentItemName;
            ProgressMessage = report.StatusMessage;
        });

        try
        {
            LastResult = await _batchGenerator.GenerateBatchAsync(
                ActiveTemplateModel,
                CurrentMatrix,
                mappings,
                config,
                progress,
                _generationCts.Token);

            IsCompleted = true;
            ProgressPercentage = 100;
            ProgressMessage = $"Batch generation complete! {LastResult.SuccessfulCount} documents created successfully.";
            GenerationSummary = $"Successfully created {LastResult.SuccessfulCount} PDFs in {LastResult.ElapsedTime.TotalSeconds:F2} seconds. Output: {(OutputMode == BatchOutputMode.SeparateFiles ? OutputDirectory : OutputFilePath)}";
        }
        catch (OperationCanceledException)
        {
            ProgressMessage = "Batch generation was cancelled by user.";
        }
        catch (Exception ex)
        {
            ProgressMessage = $"Error during batch generation: {ex.Message}";
        }
        finally
        {
            IsGenerating = false;
        }
    }

    [RelayCommand]
    public void CancelGeneration()
    {
        _generationCts?.Cancel();
    }

    [RelayCommand]
    public void OpenOutputFolder()
    {
        try
        {
            string folder = OutputMode == BatchOutputMode.SeparateFiles
                ? OutputDirectory
                : (Path.GetDirectoryName(OutputFilePath) ?? OutputDirectory);

            if (Directory.Exists(folder))
            {
                Process.Start(new ProcessStartInfo
                {
                    FileName = folder,
                    UseShellExecute = true
                });
            }
        }
        catch { }
    }

    [RelayCommand]
    public void OpenOutputFile()
    {
        try
        {
            string target = OutputMode == BatchOutputMode.SeparateFiles
                ? LastResult?.GeneratedFiles.FirstOrDefault() ?? OutputDirectory
                : OutputFilePath;

            if (File.Exists(target) || Directory.Exists(target))
            {
                Process.Start(new ProcessStartInfo
                {
                    FileName = target,
                    UseShellExecute = true
                });
            }
        }
        catch { }
    }
}
