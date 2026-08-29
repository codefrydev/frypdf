using System;
using System.IO;
using System.Text;
using System.Threading.Tasks;
using PdfEditorApp.Models;
using PdfEditorApp.Services;
using PdfEditorApp.ViewModels;
using Xunit;

namespace PdfEditorApp.Tests;

public class PdfEngineTests
{
    private readonly IPdfExportService _exportService = new PdfExportService();
    private readonly ITemplateService _templateService = new TemplateService();
    private readonly IProjectPersistenceService _persistenceService = new ProjectPersistenceService();

    [Fact]
    public void GenerateAnnualReport_ProducesValidPdfBytes()
    {
        var model = _templateService.CreateAnnualReportTemplate();
        Assert.NotNull(model);
        Assert.Equal(3, model.Pages.Count);

        byte[] pdfBytes = _exportService.GeneratePdfBytes(model);
        Assert.NotNull(pdfBytes);
        Assert.True(pdfBytes.Length > 1000);

        string header = Encoding.ASCII.GetString(pdfBytes, 0, 5);
        Assert.Equal("%PDF-", header);
    }

    [Fact]
    public void GenerateInvoice_ProducesValidPdfBytes()
    {
        var model = _templateService.CreateInvoiceTemplate();
        Assert.NotNull(model);
        Assert.Single(model.Pages);

        byte[] pdfBytes = _exportService.GeneratePdfBytes(model);
        Assert.NotNull(pdfBytes);
        Assert.True(pdfBytes.Length > 500);

        string header = Encoding.ASCII.GetString(pdfBytes, 0, 5);
        Assert.Equal("%PDF-", header);
    }

    [Fact]
    public void GenerateResume_ProducesValidPdfBytes()
    {
        var model = _templateService.CreateResumeTemplate();
        Assert.NotNull(model);
        Assert.Single(model.Pages);

        byte[] pdfBytes = _exportService.GeneratePdfBytes(model);
        Assert.NotNull(pdfBytes);
        Assert.True(pdfBytes.Length > 500);

        string header = Encoding.ASCII.GetString(pdfBytes, 0, 5);
        Assert.Equal("%PDF-", header);
    }

    [Fact]
    public void GenerateAcademicPaperAndCertificate_ProducesValidPdfBytes()
    {
        var paper = _templateService.CreateAcademicPaperTemplate();
        Assert.NotNull(paper);
        byte[] paperBytes = _exportService.GeneratePdfBytes(paper);
        Assert.NotNull(paperBytes);
        Assert.Equal("%PDF-", Encoding.ASCII.GetString(paperBytes, 0, 5));

        var cert = _templateService.CreateCertificateTemplate();
        Assert.NotNull(cert);
        byte[] certBytes = _exportService.GeneratePdfBytes(cert);
        Assert.NotNull(certBytes);
        Assert.Equal("%PDF-", Encoding.ASCII.GetString(certBytes, 0, 5));
    }

    [Fact]
    public async Task ProjectPersistence_RoundTripMatches()
    {
        var original = _templateService.CreateAnnualReportTemplate();
        string tempPath = Path.Combine(Path.GetTempPath(), $"test_project_{Guid.NewGuid():N}.pdfproj");

        try
        {
            await _persistenceService.SaveProjectAsync(original, tempPath);
            Assert.True(File.Exists(tempPath));

            var loaded = await _persistenceService.LoadProjectAsync(tempPath);
            Assert.NotNull(loaded);
            Assert.Equal(original.Title, loaded.Title);
            Assert.Equal(original.Pages.Count, loaded.Pages.Count);
            Assert.Equal(original.Pages[0].Elements.Count, loaded.Pages[0].Elements.Count);
        }
        finally
        {
            if (File.Exists(tempPath)) File.Delete(tempPath);
        }
    }

    [Fact]
    public void UndoRedo_StackOperations_FunctionCorrectly()
    {
        var undoRedo = new UndoRedoService();
        int val = 0;

        Assert.False(undoRedo.CanUndo);
        Assert.False(undoRedo.CanRedo);

        undoRedo.RecordAction("Increment", () => val = 0, () => val = 10);
        val = 10;
        Assert.True(undoRedo.CanUndo);
        Assert.False(undoRedo.CanRedo);

        undoRedo.Undo();
        Assert.Equal(0, val);
        Assert.False(undoRedo.CanUndo);
        Assert.True(undoRedo.CanRedo);

        undoRedo.Redo();
        Assert.Equal(10, val);
        Assert.True(undoRedo.CanUndo);
        Assert.False(undoRedo.CanRedo);
    }

    [Fact]
    public void MainViewModel_FullFeatureSuite_WorksCorrectly()
    {
        var vm = new MainViewModel(_exportService, _templateService, _persistenceService);
        Assert.Equal(3, vm.Pages.Count);
        Assert.NotNull(vm.CurrentPage);

        // Add page
        vm.AddPageCommand.Execute(null);
        Assert.Equal(4, vm.Pages.Count);

        // Test Element Additions
        int initialCount = vm.CurrentPage.Elements.Count;
        vm.AddTextElementCommand.Execute(null);
        vm.AddHeadingElementCommand.Execute(null);
        vm.AddShapeElementCommand.Execute("Circle");
        vm.AddTableElementCommand.Execute(null);
        vm.AddChartElementCommand.Execute(null);
        vm.AddWatermarkElementCommand.Execute(null);
        vm.AddStampElementCommand.Execute("Approved");
        vm.AddStickyNoteElementCommand.Execute(null);

        Assert.Equal(initialCount + 8, vm.CurrentPage.Elements.Count);

        // Clipboard Copy & Paste & Duplicate
        vm.CopyCommand.Execute(null);
        vm.PasteCommand.Execute(null);
        Assert.Equal(initialCount + 9, vm.CurrentPage.Elements.Count);

        vm.DuplicateCommand.Execute(null);
        Assert.Equal(initialCount + 10, vm.CurrentPage.Elements.Count);

        // Reordering & Rotation
        int rotation = vm.CurrentPage.RotationAngle;
        vm.RotateCurrentPageCommand.Execute(null);
        Assert.Equal((rotation + 90) % 360, vm.CurrentPage.RotationAngle);

        vm.RotateCurrentPageCounterClockwiseCommand.Execute(null);
        Assert.Equal(rotation, vm.CurrentPage.RotationAngle);
    }

    [Fact]
    public void GeneratePdf_WithAllAcroFormsAndVisualElements_Succeeds()
    {
        var doc = new PdfDocumentModel { Title = "Adobe Acrobat Pro Replacement Test Document" };
        var page = new PdfPageModel { PageNumber = 1, Width = 800, Height = 1100, ShowHeaderFooter = true };

        page.Elements.Add(new PdfEditorApp.Models.Elements.PdfFormFieldElement
        {
            FieldType = FormFieldType.Text,
            Label = "Candidate Name:",
            Placeholder = "Jane Doe"
        });

        page.Elements.Add(new PdfEditorApp.Models.Elements.PdfFormFieldElement
        {
            FieldType = FormFieldType.Checkbox,
            Label = "NDA Signed",
            IsChecked = true
        });

        page.Elements.Add(new PdfEditorApp.Models.Elements.PdfFormFieldElement
        {
            FieldType = FormFieldType.Signature,
            Label = "Authorized Signatory",
            Value = "John Hancock"
        });

        page.Elements.Add(new PdfEditorApp.Models.Elements.PdfQrCodeElement
        {
            Content = "https://github.com/PrashantUnity/PDFCreator",
            Label = "VERIFICATION QR"
        });

        page.Elements.Add(new PdfEditorApp.Models.Elements.PdfBarcodeElement
        {
            CodeValue = "DOC-998822",
            ShowText = true
        });

        page.Elements.Add(new PdfEditorApp.Models.Elements.PdfRedactionElement
        {
            ExemptionCode = "[REDACTED - (b)(4)]"
        });

        page.Elements.Add(new PdfEditorApp.Models.Elements.PdfInkElement
        {
            IsHighlighter = true,
            StrokeThickness = 8
        });

        page.Elements.Add(new PdfEditorApp.Models.Elements.PdfStickyNoteElement
        {
            Author = "Lead Counsel",
            NoteText = "Approved for filing."
        });

        doc.Pages.Add(page);

        byte[] bytes = _exportService.GeneratePdfBytes(doc);
        Assert.NotNull(bytes);
        Assert.True(bytes.Length > 800);
        Assert.Equal("%PDF-", Encoding.ASCII.GetString(bytes, 0, 5));
    }

    [Fact]
    public void BatesNumbering_AppliesSequentialCodesAcrossPages()
    {
        var vm = new MainViewModel(_exportService, _templateService, _persistenceService);
        Assert.True(vm.Pages.Count >= 3);

        vm.ApplyBatesNumberingCommand.Execute(null);

        Assert.Equal("CONF-BATES-000001", vm.Pages[0].FooterLeft);
        Assert.Equal("CONF-BATES-000002", vm.Pages[1].FooterLeft);
        Assert.Equal("CONF-BATES-000003", vm.Pages[2].FooterLeft);
    }

    [Fact]
    public void ExpandedCharts_And_Shapes_Generate_ValidPdf()
    {
        var doc = new PdfDocumentModel { Title = "Multi-Chart & Multi-Shape Showcase" };
        var page = new PdfPageModel { PageNumber = 1, Width = 800, Height = 1100 };

        // Test Horizontal Bar, Donut Pie, and Column Charts
        page.Elements.Add(new PdfEditorApp.Models.Elements.PdfChartElement
        {
            Title = "Departmental Efficiency",
            ChartType = ChartType.HorizontalBar,
            Categories = new System.Collections.Generic.List<string> { "Dev", "QA", "Sales" },
            Values = new System.Collections.Generic.List<double> { 8.5, 7.2, 9.1 },
            ValueLabels = new System.Collections.Generic.List<string> { "85%", "72%", "91%" }
        });

        page.Elements.Add(new PdfEditorApp.Models.Elements.PdfChartElement
        {
            Title = "Budget Allocation",
            ChartType = ChartType.DonutPie,
            Categories = new System.Collections.Generic.List<string> { "Engineering", "Marketing", "Legal" },
            Values = new System.Collections.Generic.List<double> { 50, 30, 20 },
            ValueLabels = new System.Collections.Generic.List<string> { "$5M", "$3M", "$2M" }
        });

        // Test Shapes
        page.Elements.Add(new PdfEditorApp.Models.Elements.PdfShapeElement
        {
            ShapeType = ShapeType.Star5,
            FillColorHex = "#F59E0B"
        });

        page.Elements.Add(new PdfEditorApp.Models.Elements.PdfShapeElement
        {
            ShapeType = ShapeType.Heart,
            FillColorHex = "#DC2626"
        });

        doc.Pages.Add(page);

        byte[] pdf = _exportService.GeneratePdfBytes(doc);
        Assert.NotNull(pdf);
        Assert.Equal("%PDF-", Encoding.ASCII.GetString(pdf, 0, 5));
    }

    [Fact]
    public void TablePresets_And_BarcodeFormats_ApplyCorrectly()
    {
        var tableVm = new PdfEditorApp.ViewModels.ElementViewModels.TableElementViewModel();
        tableVm.ApplyPresetStyleCommand.Execute("EnterpriseBlue");
        Assert.Equal("#0F6CBD", tableVm.HeaderBackgroundHex);
        Assert.Equal("#F0F7FD", tableVm.AlternateRowBackgroundHex);

        tableVm.ApplyPresetStyleCommand.Execute("DarkModeSlate");
        Assert.Equal("#1E293B", tableVm.HeaderBackgroundHex);

        var barcodeVm = new PdfEditorApp.ViewModels.ElementViewModels.BarcodeElementViewModel();
        barcodeVm.SetFormatCommand.Execute("Ean13");
        Assert.Equal("Ean13", barcodeVm.BarcodeFormat);

        barcodeVm.SetFormatCommand.Execute("Pdf417");
        Assert.Equal("Pdf417", barcodeVm.BarcodeFormat);
    }

    [Fact]
    public void QrCodePresets_ApplyAccuratePayloads()
    {
        var qrVm = new PdfEditorApp.ViewModels.ElementViewModels.QrCodeElementViewModel();
        qrVm.ApplyPresetTypeCommand.Execute("Wifi");
        Assert.Contains("WIFI:S:", qrVm.Content);
        Assert.Equal("SCAN TO CONNECT WI-FI", qrVm.Label);

        qrVm.ApplyPresetTypeCommand.Execute("VCard");
        Assert.Contains("BEGIN:VCARD", qrVm.Content);

        qrVm.ApplyPresetTypeCommand.Execute("PhoneCall");
        Assert.StartsWith("tel:", qrVm.Content);
    }

    [Fact]
    public void Typography_FontFamilyAndSize_SynchronizesWithInspector()
    {
        var textEl = new PdfEditorApp.ViewModels.ElementViewModels.TextElementViewModel
        {
            FontFamily = "Georgia",
            FontSize = 24,
            IsBold = true,
            IsItalic = true,
            IsUnderline = true
        };

        var page = new PageViewModel();
        page.AddElement(textEl);

        var inspector = new InspectorViewModel();
        inspector.UpdateSelection(textEl, page);

        Assert.Equal("Georgia", inspector.SelectedFontFamily);
        Assert.Equal(24, inspector.SelectedFontSize);

        inspector.SelectedFontFamily = "Roboto";
        Assert.Equal("Roboto", textEl.FontFamily);

        inspector.SelectedFontSize = 32;
        Assert.Equal(32, textEl.FontSize);

        inspector.ToggleBoldCommand.Execute(null);
        Assert.False(textEl.IsBold);

        inspector.ToggleItalicCommand.Execute(null);
        Assert.False(textEl.IsItalic);

        inspector.ToggleUnderlineCommand.Execute(null);
        Assert.False(textEl.IsUnderline);
    }

    [Fact]
    public void All21ShapeTypes_ProduceValidVectorPaths()
    {
        var shapeVm = new PdfEditorApp.ViewModels.ElementViewModels.ShapeElementViewModel
        {
            Width = 100,
            Height = 100,
            CornerRadius = 12
        };

        foreach (ShapeType shapeType in Enum.GetValues<ShapeType>())
        {
            shapeVm.ShapeType = shapeType;
            string path = shapeVm.PathData;
            Assert.False(string.IsNullOrWhiteSpace(path), $"PathData for {shapeType} should not be empty");
            Assert.StartsWith("M", path);
        }
    }

    [Fact]
    public void All13ChartTypes_PropertiesAndPersistence_Work()
    {
        var chartVm = new PdfEditorApp.ViewModels.ElementViewModels.ChartElementViewModel();

        foreach (ChartType chartType in Enum.GetValues<ChartType>())
        {
            chartVm.SetChartTypeCommand.Execute(chartType.ToString());
            Assert.Equal(chartType, chartVm.ChartType);

            var model = chartVm.ToModel() as PdfEditorApp.Models.Elements.PdfChartElement;
            Assert.NotNull(model);
            Assert.Equal(chartType, model.ChartType);

            var roundtripVm = new PdfEditorApp.ViewModels.ElementViewModels.ChartElementViewModel();
            roundtripVm.LoadFromModel(model);
            Assert.Equal(chartType, roundtripVm.ChartType);
        }
    }

    [Fact]
    public void FullElementDuplicationAndPaste_MaintainsCorrectTypes()
    {
        var vm = new MainViewModel(_exportService, _templateService, _persistenceService);
        Assert.NotNull(vm.CurrentPage);

        // Add StickyNote
        vm.AddStickyNoteElementCommand.Execute(null);
        var sticky = vm.CurrentPage.SelectedElement;
        Assert.IsType<PdfEditorApp.ViewModels.ElementViewModels.StickyNoteElementViewModel>(sticky);

        // Copy and Paste
        vm.CopyCommand.Execute(null);
        vm.PasteCommand.Execute(null);
        var pasted = vm.CurrentPage.SelectedElement;
        Assert.IsType<PdfEditorApp.ViewModels.ElementViewModels.StickyNoteElementViewModel>(pasted);

        // Add FormField
        vm.AddFormFieldElementCommand.Execute("Signature");
        var formField = vm.CurrentPage.SelectedElement;
        Assert.IsType<PdfEditorApp.ViewModels.ElementViewModels.FormFieldElementViewModel>(formField);

        // Duplicate
        vm.DuplicateCommand.Execute(null);
        var dupFormField = vm.CurrentPage.SelectedElement;
        Assert.IsType<PdfEditorApp.ViewModels.ElementViewModels.FormFieldElementViewModel>(dupFormField);

        // Add Ink
        vm.AddInkElementCommand.Execute("True");
        var ink = vm.CurrentPage.SelectedElement as PdfEditorApp.ViewModels.ElementViewModels.InkElementViewModel;
        Assert.NotNull(ink);
        Assert.True(ink.IsHighlighter);

        // Dialog close
        vm.OpenNewDocumentDialogCommand.Execute(null);
        Assert.True(vm.IsNewDocumentDialogOpen);
        vm.CloseNewDocumentDialogCommand.Execute(null);
        Assert.False(vm.IsNewDocumentDialogOpen);
    }
}
