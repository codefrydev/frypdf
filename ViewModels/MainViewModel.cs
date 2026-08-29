using System;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Avalonia.Platform.Storage;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using PdfEditorApp.Models;
using PdfEditorApp.Models.Elements;
using PdfEditorApp.Services;
using PdfEditorApp.ViewModels.ElementViewModels;

namespace PdfEditorApp.ViewModels;

public partial class MainViewModel : ViewModelBase
{
    private readonly IPdfExportService _exportService;
    private readonly ITemplateService _templateService;
    private readonly IProjectPersistenceService _persistenceService;

    [ObservableProperty]
    private string _documentTitle = "Annual_Report_2026.pdf";

    [ObservableProperty]
    private string _documentAuthor = "ACME CORP.";

    [ObservableProperty]
    private string _documentSubject = "Fiscal Year 2026 Annual Report";

    [ObservableProperty]
    private RibbonTabKind _activeRibbonTab = RibbonTabKind.Edit;

    [ObservableProperty]
    private ToolMode _activeToolMode = ToolMode.Select;

    [ObservableProperty]
    private double _zoomLevel = 1.0; // 100%

    [ObservableProperty]
    private string _searchQuery = "";

    partial void OnSearchQueryChanged(string value)
    {
        if (string.IsNullOrWhiteSpace(value)) return;

        for (int p = 0; p < Pages.Count; p++)
        {
            var page = Pages[p];
            foreach (var el in page.Elements)
            {
                if (el is TextElementViewModel txt && txt.Text.Contains(value, StringComparison.OrdinalIgnoreCase))
                {
                    SelectPage(page);
                    page.SelectElement(txt);
                    UpdateStatus($"Found match on Page {p + 1}: \"{txt.Text.Split('\n').FirstOrDefault()}\"");
                    return;
                }
            }
        }
        UpdateStatus($"No results for \"{value}\"");
    }

    [ObservableProperty]
    private string _statusMessage = "Ready";

    [ObservableProperty]
    private string _lastExportedFilePath = "";

    [ObservableProperty]
    private bool _isExportSuccessDialogOpen;

    [ObservableProperty]
    private bool _isNewDocumentDialogOpen;

    [ObservableProperty]
    private SidebarTabKind _activeSidebarTab = SidebarTabKind.Thumbnails;

    [ObservableProperty]
    private PageViewModel? _currentPage;

    public ObservableCollection<PageViewModel> Pages { get; } = new();

    public ObservableCollection<OutlineItem> OutlineItems { get; } = new();

    public ObservableCollection<CommentItem> CommentItems { get; } = new();

    public InspectorViewModel Inspector { get; } = new();

    public UndoRedoService UndoRedo { get; } = new();

    private ElementViewModelBase? _clipboardElement;

    public int CurrentPageNumber => CurrentPage != null ? Pages.IndexOf(CurrentPage) + 1 : 1;
    public int TotalPagesCount => Pages.Count;
    public string PageDimensionsDisplay => CurrentPage?.Format switch
    {
        PageFormat.A4 => "A4 (8.27 x 11.69 in)",
        PageFormat.Letter => "Letter (8.5 x 11.0 in)",
        PageFormat.Legal => "Legal (8.5 x 14.0 in)",
        PageFormat.Executive => "Executive (7.25 x 10.5 in)",
        _ => "8.5 x 11.0 in"
    };

    // Global file storage provider setter for Avalonia desktop file dialogs
    public static IStorageProvider? StorageProvider { get; set; }

    public MainViewModel() : this(new PdfExportService(), new TemplateService(), new ProjectPersistenceService())
    {
    }

    public MainViewModel(
        IPdfExportService exportService,
        ITemplateService templateService,
        IProjectPersistenceService persistenceService)
    {
        _exportService = exportService;
        _templateService = templateService;
        _persistenceService = persistenceService;

        // Load default Annual Report 2026 template matching the user's mockup
        LoadTemplate("AnnualReport");
    }

    public void LoadTemplate(string templateName)
    {
        PdfDocumentModel doc = templateName switch
        {
            "AnnualReport" => _templateService.CreateAnnualReportTemplate(),
            "Invoice" => _templateService.CreateInvoiceTemplate(),
            "Resume" => _templateService.CreateResumeTemplate(),
            "AcademicPaper" => _templateService.CreateAcademicPaperTemplate(),
            "Certificate" => _templateService.CreateCertificateTemplate(),
            _ => _templateService.CreateBlankDocument()
        };

        LoadFromDocumentModel(doc);
    }

    public void LoadFromDocumentModel(PdfDocumentModel model)
    {
        DocumentTitle = model.Title;
        DocumentAuthor = model.Author;
        DocumentSubject = model.Subject;

        Pages.Clear();
        foreach (var pageModel in model.Pages)
        {
            var pageVm = new PageViewModel();
            pageVm.LoadFromModel(pageModel);
            pageVm.SelectionChanged += OnElementSelectionChanged;
            Pages.Add(pageVm);
        }

        if (Pages.Count > 0)
        {
            SelectPage(Pages[0]);
        }
        else
        {
            AddPage();
        }

        UpdateStatus($"Document loaded: {DocumentTitle}");
    }

    public PdfDocumentModel ToDocumentModel()
    {
        var doc = new PdfDocumentModel
        {
            Title = DocumentTitle,
            Author = DocumentAuthor,
            Subject = DocumentSubject,
            CreatedDate = DateTime.Now,
            ModifiedDate = DateTime.Now
        };

        foreach (var pageVm in Pages)
        {
            doc.Pages.Add(pageVm.ToModel());
        }

        return doc;
    }

    private void OnElementSelectionChanged(ElementViewModelBase? element)
    {
        Inspector.UpdateSelection(element, CurrentPage);
    }

    [RelayCommand]
    public void SelectRibbonTab(RibbonTabKind tab)
    {
        ActiveRibbonTab = tab;
    }

    [RelayCommand]
    public void SelectPage(PageViewModel page)
    {
        foreach (var p in Pages)
        {
            p.IsSelected = (p == page);
        }

        CurrentPage = page;
        Inspector.UpdateSelection(page.SelectedElement, page);
        OnPropertyChanged(nameof(CurrentPageNumber));
        OnPropertyChanged(nameof(TotalPagesCount));
        OnPropertyChanged(nameof(PageDimensionsDisplay));
    }

    [RelayCommand]
    public void AddPage()
    {
        var newPage = new PageViewModel
        {
            PageNumber = Pages.Count + 1,
            Format = CurrentPage?.Format ?? PageFormat.A4,
            Orientation = CurrentPage?.Orientation ?? PageOrientation.Portrait,
            Width = CurrentPage?.Width ?? 800,
            Height = CurrentPage?.Height ?? 1131,
            FooterRight = $"Page {Pages.Count + 1} of {Pages.Count + 1}"
        };

        newPage.SelectionChanged += OnElementSelectionChanged;
        Pages.Add(newPage);
        SelectPage(newPage);
        UpdateStatus($"Added new Page {newPage.PageNumber}");
    }

    [RelayCommand]
    public void DuplicateCurrentPage()
    {
        if (CurrentPage == null) return;
        var model = CurrentPage.ToModel();
        var clonedPage = new PageViewModel();
        clonedPage.LoadFromModel(model);
        clonedPage.PageNumber = Pages.Count + 1;
        clonedPage.SelectionChanged += OnElementSelectionChanged;
        Pages.Add(clonedPage);
        SelectPage(clonedPage);
        UpdateStatus($"Duplicated Page {CurrentPage.PageNumber}");
    }

    [RelayCommand]
    public void DeleteCurrentPage()
    {
        if (Pages.Count <= 1 || CurrentPage == null)
        {
            UpdateStatus("Cannot delete the only page in the document.");
            return;
        }

        int index = Pages.IndexOf(CurrentPage);
        var toRemove = CurrentPage;
        Pages.Remove(toRemove);

        // Renumber pages
        for (int i = 0; i < Pages.Count; i++)
        {
            Pages[i].PageNumber = i + 1;
        }

        int newIndex = Math.Min(index, Pages.Count - 1);
        SelectPage(Pages[newIndex]);
        UpdateStatus("Page deleted.");
    }

    [RelayCommand]
    public void RotateCurrentPage()
    {
        CurrentPage?.RotateClockwise();
        UpdateStatus($"Page rotated to {CurrentPage?.RotationAngle}°");
    }

    [RelayCommand]
    public void RotateCurrentPageCounterClockwise()
    {
        if (CurrentPage != null)
        {
            CurrentPage.RotationAngle = (CurrentPage.RotationAngle + 270) % 360;
            UpdateStatus($"Page rotated to {CurrentPage.RotationAngle}°");
        }
    }

    [RelayCommand]
    public void MovePageUp()
    {
        if (CurrentPage == null) return;
        int idx = Pages.IndexOf(CurrentPage);
        if (idx > 0)
        {
            Pages.Move(idx, idx - 1);
            RenumberPages();
            SelectPage(Pages[idx - 1]);
        }
    }

    [RelayCommand]
    public void MovePageDown()
    {
        if (CurrentPage == null) return;
        int idx = Pages.IndexOf(CurrentPage);
        if (idx < Pages.Count - 1)
        {
            Pages.Move(idx, idx + 1);
            RenumberPages();
            SelectPage(Pages[idx + 1]);
        }
    }

    private void RenumberPages()
    {
        for (int i = 0; i < Pages.Count; i++)
        {
            Pages[i].PageNumber = i + 1;
        }
        OnPropertyChanged(nameof(CurrentPageNumber));
        OnPropertyChanged(nameof(TotalPagesCount));
    }

    // --- UNDO / REDO & CLIPBOARD COMMANDS ---

    [RelayCommand]
    public void Undo()
    {
        if (UndoRedo.CanUndo)
        {
            UndoRedo.Undo();
            UpdateStatus("Undo performed");
        }
        else
        {
            UpdateStatus("Nothing to undo");
        }
    }

    [RelayCommand]
    public void Redo()
    {
        if (UndoRedo.CanRedo)
        {
            UndoRedo.Redo();
            UpdateStatus("Redo performed");
        }
        else
        {
            UpdateStatus("Nothing to redo");
        }
    }

    [RelayCommand]
    public void Copy()
    {
        if (CurrentPage?.SelectedElement != null)
        {
            _clipboardElement = CurrentPage.SelectedElement;
            UpdateStatus($"Copied: {_clipboardElement.DisplayName}");
        }
    }

    [RelayCommand]
    public void Cut()
    {
        if (CurrentPage?.SelectedElement != null)
        {
            _clipboardElement = CurrentPage.SelectedElement;
            var elToRemove = CurrentPage.SelectedElement;
            CurrentPage.RemoveElement(elToRemove);
            UpdateStatus($"Cut: {_clipboardElement.DisplayName}");
        }
    }

    [RelayCommand]
    public void Paste()
    {
        if (_clipboardElement != null && CurrentPage != null)
        {
            var model = _clipboardElement.ToModel();
            var clone = model.Clone();
            clone.Id = Guid.NewGuid().ToString();
            clone.X += 20;
            clone.Y += 20;

            ElementViewModelBase? newVm = clone.Kind switch
            {
                ElementKind.Text => new TextElementViewModel(),
                ElementKind.Heading => new TextElementViewModel(),
                ElementKind.Shape => new ShapeElementViewModel(),
                ElementKind.Image => new ImageElementViewModel(),
                ElementKind.Divider => new DividerElementViewModel(),
                ElementKind.Table => new TableElementViewModel(),
                ElementKind.Chart => new ChartElementViewModel(),
                ElementKind.Watermark => new WatermarkElementViewModel(),
                ElementKind.FormField => new FormFieldElementViewModel(),
                ElementKind.QrCode => new QrCodeElementViewModel(),
                ElementKind.Barcode => new BarcodeElementViewModel(),
                ElementKind.Redaction => new RedactionElementViewModel(),
                ElementKind.Ink => new InkElementViewModel(),
                ElementKind.StickyNote => new StickyNoteElementViewModel(),
                _ => new TextElementViewModel()
            };

            newVm.LoadFromModel(clone);
            CurrentPage.AddElement(newVm);
            UpdateStatus($"Pasted: {newVm.DisplayName}");
        }
    }

    [RelayCommand]
    public void Duplicate()
    {
        if (CurrentPage?.SelectedElement != null)
        {
            var model = CurrentPage.SelectedElement.ToModel();
            var clone = model.Clone();
            clone.Id = Guid.NewGuid().ToString();
            clone.X += 20;
            clone.Y += 20;

            ElementViewModelBase? newVm = clone.Kind switch
            {
                ElementKind.Text => new TextElementViewModel(),
                ElementKind.Heading => new TextElementViewModel(),
                ElementKind.Shape => new ShapeElementViewModel(),
                ElementKind.Image => new ImageElementViewModel(),
                ElementKind.Divider => new DividerElementViewModel(),
                ElementKind.Table => new TableElementViewModel(),
                ElementKind.Chart => new ChartElementViewModel(),
                ElementKind.Watermark => new WatermarkElementViewModel(),
                ElementKind.FormField => new FormFieldElementViewModel(),
                ElementKind.QrCode => new QrCodeElementViewModel(),
                ElementKind.Barcode => new BarcodeElementViewModel(),
                ElementKind.Redaction => new RedactionElementViewModel(),
                ElementKind.Ink => new InkElementViewModel(),
                ElementKind.StickyNote => new StickyNoteElementViewModel(),
                _ => new TextElementViewModel()
            };

            newVm.LoadFromModel(clone);
            CurrentPage.AddElement(newVm);
            UpdateStatus($"Duplicated: {newVm.DisplayName}");
        }
    }

    // --- ELEMENT CREATION COMMANDS ---

    [RelayCommand]
    public void AddTextElement()
    {
        if (CurrentPage == null) return;

        var textEl = new TextElementViewModel
        {
            X = 100,
            Y = 150,
            Width = 400,
            Height = 80,
            Text = "New editable paragraph. Double-click or use inspector to customize text, fonts, colors, and alignments.",
            FontSize = 13,
            TextColorHex = "#201F1E"
        };

        CurrentPage.AddElement(textEl);
        UpdateStatus("Added Text Element");
    }

    [RelayCommand]
    public void AddHeadingElement()
    {
        if (CurrentPage == null) return;

        var headingEl = new TextElementViewModel
        {
            X = 100,
            Y = 100,
            Width = 500,
            Height = 45,
            Text = "Section Heading",
            FontSize = 22,
            FontFamily = "Georgia",
            IsBold = true,
            TextColorHex = "#111827"
        };

        CurrentPage.AddElement(headingEl);
        UpdateStatus("Added Heading Element");
    }

    [RelayCommand]
    public void AddShapeElement(string? shapeTypeStr = "Rectangle")
    {
        if (CurrentPage == null) return;

        var shapeType = ShapeType.Rectangle;
        if (!string.IsNullOrEmpty(shapeTypeStr) && Enum.TryParse<ShapeType>(shapeTypeStr, true, out var parsed))
        {
            shapeType = parsed;
        }

        var shapeEl = new ShapeElementViewModel
        {
            X = 120,
            Y = 200,
            Width = shapeType == ShapeType.Circle ? 120 : 240,
            Height = 120,
            ShapeType = shapeType,
            FillColorHex = "#F0F7FD",
            StrokeColorHex = "#0F6CBD",
            StrokeThickness = 1.5,
            CornerRadius = shapeType == ShapeType.Circle ? 60 : (shapeType == ShapeType.RoundedRectangle ? 16 : 6)
        };

        CurrentPage.AddElement(shapeEl);
        UpdateStatus($"Added Shape ({shapeType})");
    }

    [RelayCommand]
    public async Task AddImageElementAsync()
    {
        if (CurrentPage == null) return;

        try
        {
            if (StorageProvider != null)
            {
                var files = await StorageProvider.OpenFilePickerAsync(new FilePickerOpenOptions
                {
                    Title = "Insert Image",
                    AllowMultiple = false,
                    FileTypeFilter = new[]
                    {
                        new FilePickerFileType("Image Files (*.png, *.jpg, *.jpeg, *.webp)")
                        {
                            Patterns = new[] { "*.png", "*.jpg", "*.jpeg", "*.webp", "*.bmp" }
                        }
                    }
                });

                if (files.Count > 0)
                {
                    string filePath = files[0].Path.LocalPath;
                    var imgEl = new ImageElementViewModel
                    {
                        X = 100,
                        Y = 200,
                        Width = 260,
                        Height = 180,
                        ImagePath = filePath
                    };

                    CurrentPage.AddElement(imgEl);
                    UpdateStatus($"Inserted Image: {Path.GetFileName(filePath)}");
                    return;
                }
            }

            // Fallback placeholder image element
            var fallbackImg = new ImageElementViewModel
            {
                X = 100,
                Y = 200,
                Width = 260,
                Height = 180,
                AltText = "Inserted Graphic"
            };
            CurrentPage.AddElement(fallbackImg);
            UpdateStatus("Inserted Image Placeholder");
        }
        catch (Exception ex)
        {
            UpdateStatus($"Image insert error: {ex.Message}");
        }
    }

    [RelayCommand]
    public void AddStampElement(string? stampTypeStr = "Approved")
    {
        if (CurrentPage == null) return;

        string label = stampTypeStr?.ToUpper() ?? "APPROVED";
        string fillHex = stampTypeStr?.ToLower() switch
        {
            "approved" => "#DCFCE7",
            "confidential" => "#FEE2E2",
            "draft" => "#F1F5F9",
            "urgent" => "#FEF3C7",
            "void" => "#FFEDD5",
            _ => "#EFF6FF"
        };
        string strokeHex = stampTypeStr?.ToLower() switch
        {
            "approved" => "#16A34A",
            "confidential" => "#DC2626",
            "draft" => "#64748B",
            "urgent" => "#D97706",
            "void" => "#EA580C",
            _ => "#0F6CBD"
        };

        var stampEl = new ShapeElementViewModel
        {
            X = 200,
            Y = 200,
            Width = 180,
            Height = 60,
            ShapeType = ShapeType.RoundedRectangle,
            CornerRadius = 8,
            FillColorHex = fillHex,
            StrokeColorHex = strokeHex,
            StrokeThickness = 2.0,
            Label = label,
            LabelColorHex = strokeHex,
            LabelFontSize = 18
        };

        CurrentPage.AddElement(stampEl);
        UpdateStatus($"Added Stamp ({label})");
    }

    [RelayCommand]
    public void AddStickyNoteElement()
    {
        if (CurrentPage == null) return;

        var noteEl = new StickyNoteElementViewModel
        {
            X = 120,
            Y = 180,
            Width = 200,
            Height = 150,
            Author = "Lead Reviewer",
            NoteText = "Please verify financial data and audit metrics prior to final executive sign-off.",
            Status = "Pending Review",
            ColorHex = "#FEF3C7",
            BorderColorHex = "#F59E0B"
        };

        CurrentPage.AddElement(noteEl);
        RefreshComments();
        UpdateStatus("Added Sticky Note");
    }

    [RelayCommand]
    public void AddDividerElement()
    {
        if (CurrentPage == null) return;

        var divEl = new DividerElementViewModel
        {
            X = 60,
            Y = 250,
            Width = 680,
            Height = 3,
            Thickness = 2,
            ColorHex = "#0F6CBD"
        };

        CurrentPage.AddElement(divEl);
        UpdateStatus("Added Divider Line");
    }

    [RelayCommand]
    public void AddTableElement()
    {
        if (CurrentPage == null) return;

        var tableEl = new TableElementViewModel
        {
            X = 60,
            Y = 250,
            Width = 680,
            Height = 180
        };

        CurrentPage.AddElement(tableEl);
        UpdateStatus("Added Table Element");
    }

    [RelayCommand]
    public void AddChartElement(string? chartTypeStr = "BarColumn")
    {
        if (CurrentPage == null) return;

        var chartType = ChartType.BarColumn;
        if (!string.IsNullOrEmpty(chartTypeStr) && Enum.TryParse<ChartType>(chartTypeStr, true, out var parsed))
        {
            chartType = parsed;
        }

        var chartEl = new ChartElementViewModel
        {
            X = 100,
            Y = 250,
            Width = 400,
            Height = 220,
            ChartType = chartType,
            Title = $"{chartType} Chart Analysis"
        };

        CurrentPage.AddElement(chartEl);
        UpdateStatus($"Added {chartType} Chart");
    }

    [RelayCommand]
    public void AddWatermarkElement()
    {
        if (CurrentPage == null) return;

        var wmEl = new WatermarkElementViewModel
        {
            X = 100,
            Y = 350,
            Text = "CONFIDENTIAL",
            FontSize = 56,
            ColorHex = "#DC2626",
            Opacity = 0.15,
            Angle = -35
        };

        CurrentPage.AddElement(wmEl);
        UpdateStatus("Added Watermark Overlay");
    }

    [RelayCommand]
    public void AddFormFieldElement(string? formTypeStr = "Text")
    {
        if (CurrentPage == null) return;

        var fieldType = FormFieldType.Text;
        if (!string.IsNullOrEmpty(formTypeStr) && Enum.TryParse<FormFieldType>(formTypeStr, true, out var parsed))
        {
            fieldType = parsed;
        }

        var formEl = new FormFieldElementViewModel
        {
            X = 100,
            Y = 220,
            Width = fieldType == FormFieldType.Checkbox ? 180 : (fieldType == FormFieldType.Signature ? 260 : 340),
            Height = fieldType == FormFieldType.Signature ? 90 : (fieldType == FormFieldType.MultilineText ? 80 : 42),
            FieldType = fieldType,
            Label = fieldType switch
            {
                FormFieldType.Text => "Full Legal Name:",
                FormFieldType.MultilineText => "Additional Notes / Comments:",
                FormFieldType.Checkbox => "I accept the Terms & Conditions",
                FormFieldType.Radio => "Select Option:",
                FormFieldType.Dropdown => "Country / Jurisdiction:",
                FormFieldType.Signature => "Authorized Officer Signature:",
                _ => "Field:"
            },
            Placeholder = fieldType == FormFieldType.Signature ? "Click to Sign / Verify Identity" : "Enter value..."
        };

        CurrentPage.AddElement(formEl);
        UpdateStatus($"Added Form Field ({fieldType})");
    }

    [RelayCommand]
    public void AddQrCodeElement()
    {
        if (CurrentPage == null) return;

        var qrEl = new QrCodeElementViewModel
        {
            X = 100,
            Y = 220,
            Width = 130,
            Height = 150,
            Content = "https://github.com/PrashantUnity/PDFCreator",
            Label = "SCAN TO VERIFY CREDENTIAL"
        };

        CurrentPage.AddElement(qrEl);
        UpdateStatus("Added Vector QR Code");
    }

    [RelayCommand]
    public void AddBarcodeElement()
    {
        if (CurrentPage == null) return;

        var barcodeEl = new BarcodeElementViewModel
        {
            X = 100,
            Y = 220,
            Width = 240,
            Height = 65,
            CodeValue = $"DOC-2026-{Random.Shared.Next(100000, 999999)}"
        };

        CurrentPage.AddElement(barcodeEl);
        UpdateStatus("Added Barcode Element");
    }

    [RelayCommand]
    public void AddRedactionElement(string? exemptionCode = "[REDACTED - (b)(4) PRIVILEGED]")
    {
        if (CurrentPage == null) return;

        var redEl = new RedactionElementViewModel
        {
            X = 80,
            Y = 200,
            Width = 320,
            Height = 36,
            ExemptionCode = exemptionCode ?? "[REDACTED]"
        };

        CurrentPage.AddElement(redEl);
        UpdateStatus("Added Redaction Blackout Block");
    }

    [RelayCommand]
    public void AddInkElement(object? isHighlighterParam = null)
    {
        if (CurrentPage == null) return;

        bool isHighlighter = isHighlighterParam switch
        {
            bool b => b,
            string s when bool.TryParse(s, out var parsed) => parsed,
            _ => false
        };

        var inkEl = new InkElementViewModel
        {
            X = 100,
            Y = 250,
            Width = 260,
            Height = isHighlighter ? 24 : 12,
            StrokeColorHex = isHighlighter ? "#FEF08A" : "#0F6CBD",
            StrokeThickness = isHighlighter ? 14.0 : 3.0,
            Opacity = isHighlighter ? 0.45 : 1.0,
            IsHighlighter = isHighlighter
        };

        CurrentPage.AddElement(inkEl);
        UpdateStatus(isHighlighter ? "Added Highlighter Stroke" : "Added Freehand Ink Stroke");
    }

    [RelayCommand]
    public void ApplyBatesNumbering()
    {
        for (int i = 0; i < Pages.Count; i++)
        {
            string batesCode = $"CONF-BATES-{(i + 1):D6}";
            Pages[i].FooterLeft = batesCode;
            Pages[i].ShowHeaderFooter = true;
        }
        UpdateStatus("Applied Bates Legal Numbering across all pages (CONF-BATES-000001...)");
    }

    // --- ZOOM COMMANDS ---

    [RelayCommand]
    public void ZoomIn()
    {
        ZoomLevel = Math.Min(2.5, Math.Round(ZoomLevel + 0.1, 2));
    }

    [RelayCommand]
    public void ZoomOut()
    {
        ZoomLevel = Math.Max(0.4, Math.Round(ZoomLevel - 0.1, 2));
    }

    [RelayCommand]
    public void ResetZoom()
    {
        ZoomLevel = 1.0;
    }

    [RelayCommand]
    public void FitToWidth()
    {
        ZoomLevel = 1.15;
    }

    [RelayCommand]
    public void FitToPage()
    {
        ZoomLevel = 0.85;
    }

    // --- EXPORT & PERSISTENCE ---

    [RelayCommand]
    public async Task ExportPdfAsync()
    {
        try
        {
            UpdateStatus("Generating PDF with QuestPDF engine...");

            string defaultFileName = Path.ChangeExtension(DocumentTitle, ".pdf");
            if (string.IsNullOrEmpty(defaultFileName)) defaultFileName = "Document.pdf";

            string exportPath = "";

            if (StorageProvider != null)
            {
                var file = await StorageProvider.SaveFilePickerAsync(new FilePickerSaveOptions
                {
                    Title = "Export PDF Document",
                    DefaultExtension = "pdf",
                    SuggestedFileName = defaultFileName,
                    FileTypeChoices = new[]
                    {
                        new FilePickerFileType("PDF Documents (*.pdf)")
                        {
                            Patterns = new[] { "*.pdf" }
                        }
                    }
                });

                if (file != null)
                {
                    exportPath = file.Path.LocalPath;
                }
            }

            if (string.IsNullOrEmpty(exportPath))
            {
                // Fallback to output directory
                exportPath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.Desktop), defaultFileName);
            }

            var docModel = ToDocumentModel();
            await _exportService.ExportToFileAsync(docModel, exportPath);

            LastExportedFilePath = exportPath;
            IsExportSuccessDialogOpen = true;
            UpdateStatus($"Successfully exported PDF to {Path.GetFileName(exportPath)}");
        }
        catch (Exception ex)
        {
            UpdateStatus($"Export error: {ex.Message}");
        }
    }

    [RelayCommand]
    public async Task SaveProjectAsync()
    {
        try
        {
            string savePath = "";
            string defaultFileName = Path.ChangeExtension(DocumentTitle, ".pdfproj");

            if (StorageProvider != null)
            {
                var file = await StorageProvider.SaveFilePickerAsync(new FilePickerSaveOptions
                {
                    Title = "Save PDF Creator Project",
                    DefaultExtension = "pdfproj",
                    SuggestedFileName = defaultFileName,
                    FileTypeChoices = new[]
                    {
                        new FilePickerFileType("PDF Creator Project (*.pdfproj)")
                        {
                            Patterns = new[] { "*.pdfproj", "*.json" }
                        }
                    }
                });

                if (file != null)
                {
                    savePath = file.Path.LocalPath;
                }
            }

            if (string.IsNullOrEmpty(savePath))
            {
                savePath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.Desktop), defaultFileName);
            }

            var docModel = ToDocumentModel();
            await _persistenceService.SaveProjectAsync(docModel, savePath);
            UpdateStatus($"Project saved to {Path.GetFileName(savePath)}");
        }
        catch (Exception ex)
        {
            UpdateStatus($"Save error: {ex.Message}");
        }
    }

    [RelayCommand]
    public async Task OpenProjectAsync()
    {
        try
        {
            if (StorageProvider != null)
            {
                var files = await StorageProvider.OpenFilePickerAsync(new FilePickerOpenOptions
                {
                    Title = "Open PDF Creator Project",
                    AllowMultiple = false,
                    FileTypeFilter = new[]
                    {
                        new FilePickerFileType("PDF Creator Project (*.pdfproj, *.json)")
                        {
                            Patterns = new[] { "*.pdfproj", "*.json" }
                        }
                    }
                });

                if (files.Count > 0)
                {
                    var path = files[0].Path.LocalPath;
                    var model = await _persistenceService.LoadProjectAsync(path);
                    if (model != null)
                    {
                        LoadFromDocumentModel(model);
                    }
                }
            }
        }
        catch (Exception ex)
        {
            UpdateStatus($"Open error: {ex.Message}");
        }
    }

    [RelayCommand]
    public void OpenNewDocumentDialog()
    {
        IsNewDocumentDialogOpen = true;
    }

    [RelayCommand]
    public void CloseNewDocumentDialog()
    {
        IsNewDocumentDialogOpen = false;
    }

    [RelayCommand]
    public void SelectTemplate(string templateName)
    {
        IsNewDocumentDialogOpen = false;
        LoadTemplate(templateName);
    }

    [RelayCommand]
    public void CloseExportDialog()
    {
        IsExportSuccessDialogOpen = false;
    }

    [RelayCommand]
    public void SelectSidebarTab(SidebarTabKind tab)
    {
        ActiveSidebarTab = tab;
        if (tab == SidebarTabKind.Outline) RefreshOutline();
        if (tab == SidebarTabKind.Comments) RefreshComments();
    }

    public void RefreshOutline()
    {
        OutlineItems.Clear();
        for (int p = 0; p < Pages.Count; p++)
        {
            var page = Pages[p];
            OutlineItems.Add(new OutlineItem
            {
                Title = $"Page {p + 1}: {page.Format} ({page.Orientation})",
                PageIndex = p,
                Kind = "Page"
            });

            foreach (var el in page.Elements)
            {
                if (el is TextElementViewModel txt && txt.FontSize >= 14)
                {
                    OutlineItems.Add(new OutlineItem
                    {
                        Title = $"  • {txt.Text.Split('\n').FirstOrDefault() ?? "Heading"}",
                        PageIndex = p,
                        Kind = "Section"
                    });
                }
            }
        }
    }

    public void RefreshComments()
    {
        CommentItems.Clear();
        for (int p = 0; p < Pages.Count; p++)
        {
            var page = Pages[p];
            foreach (var el in page.Elements)
            {
                if (el is StickyNoteElementViewModel note)
                {
                    CommentItems.Add(new CommentItem
                    {
                        Author = note.Author,
                        Timestamp = note.Timestamp,
                        Text = note.NoteText,
                        Status = note.Status,
                        PageIndex = p,
                        Element = note
                    });
                }
                else if (el is RedactionElementViewModel red)
                {
                    CommentItems.Add(new CommentItem
                    {
                        Author = "Security / Legal",
                        Timestamp = "Redaction Exemption",
                        Text = red.ExemptionCode,
                        Status = "Redacted",
                        PageIndex = p,
                        Element = red
                    });
                }
                else if (el is FormFieldElementViewModel form && form.FieldType == FormFieldType.Signature)
                {
                    CommentItems.Add(new CommentItem
                    {
                        Author = "Signature Field",
                        Timestamp = "Required Action",
                        Text = form.Label,
                        Status = "Unsigned",
                        PageIndex = p,
                        Element = form
                    });
                }
            }
        }
    }

    [RelayCommand]
    public void JumpToOutlineItem(OutlineItem item)
    {
        if (item.PageIndex >= 0 && item.PageIndex < Pages.Count)
        {
            SelectPage(Pages[item.PageIndex]);
        }
    }

    [RelayCommand]
    public void JumpToCommentItem(CommentItem item)
    {
        if (item.PageIndex >= 0 && item.PageIndex < Pages.Count)
        {
            SelectPage(Pages[item.PageIndex]);
            Pages[item.PageIndex].SelectElement(item.Element);
        }
    }

    private void UpdateStatus(string message)
    {
        StatusMessage = message;
    }
}

public class OutlineItem
{
    public string Title { get; set; } = "";
    public int PageIndex { get; set; }
    public string Kind { get; set; } = "Heading";
}

public class CommentItem
{
    public string Author { get; set; } = "";
    public string Timestamp { get; set; } = "";
    public string Text { get; set; } = "";
    public string Status { get; set; } = "";
    public int PageIndex { get; set; }
    public ElementViewModelBase Element { get; set; } = null!;
}
