using System;
using System.IO;
using System.Text;
using System.Threading.Tasks;
using PdfEditorApp.Core.Models;
using PdfEditorApp.Core.Models.Elements;
using PdfEditorApp.Models;
using PdfEditorApp.Services;
using PdfEditorApp.Templates;
using PdfEditorApp.Templates.Events;
using PdfEditorApp.ViewModels;
using PdfEditorApp.ViewModels.ElementViewModels;
using Xunit;

namespace PdfEditorApp.Tests;

public class PdfEngineTests
{
    private readonly IPdfExportService _exportService = new PdfExportService();
    private readonly ITemplateService _templateService = new TemplateService();
    private readonly IProjectPersistenceService _persistenceService = new ProjectPersistenceService();

    [Fact]
    public void PdfPigSkiaRenderer_RendersPdfPagesToPngStreamsSuccessfully()
    {
        var model = _templateService.CreateInvoiceTemplate();
        byte[] pdfBytes = _exportService.GeneratePdfBytes(model);
        using var doc = UglyToad.PdfPig.PdfDocument.Open(pdfBytes);
        UglyToad.PdfPig.Rendering.Skia.PdfPigExtensions.AddSkiaPageFactory(doc);

        // 1. Direct SKPicture (Vector representation)
        using var picture = UglyToad.PdfPig.Rendering.Skia.PdfPigExtensions.GetPageAsSKPicture(doc, 1);
        Assert.NotNull(picture);
        Assert.True(picture.CullRect.Width > 0);
        Assert.True(picture.CullRect.Height > 0);

        // 2. Direct SKBitmap (Raw in-memory raster)
        using var skBitmap = UglyToad.PdfPig.Rendering.Skia.PdfPigExtensions.GetPageAsSKBitmap(doc, 1, 2.5f, SkiaSharp.SKColors.White);
        Assert.NotNull(skBitmap);
        Assert.True(skBitmap.Width > 0);
        Assert.True(skBitmap.Height > 0);

        // 3. Encoded PNG stream verification
        using var image = SkiaSharp.SKImage.FromBitmap(skBitmap);
        using var data = image.Encode(SkiaSharp.SKEncodedImageFormat.Png, 100);
        Assert.NotNull(data);
        Assert.True(data.Size > 0);
    }

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

        // Deconstruct the exported Annual Report PDF back into editable model
        var importedDoc = PdfEditorApp.Core.Deconstruction.PdfDeconstructionEngine.Deconstruct(pdfBytes, "Annual Report");
        Assert.NotNull(importedDoc);
        Assert.Equal(3, importedDoc.Pages.Count);

        for (int pIdx = 0; pIdx < importedDoc.Pages.Count; pIdx++)
        {
            var origPage = model.Pages[pIdx];
            var impPage = importedDoc.Pages[pIdx];
            Console.WriteLine($"=== PAGE {pIdx + 1} COMPARISON ===");
            Console.WriteLine($"ORIGINAL: {origPage.Elements.Count} elements: {string.Join(", ", origPage.Elements.GroupBy(e => e.GetType().Name).Select(g => $"{g.Key}={g.Count()}"))}");
            Console.WriteLine($"IMPORTED: {impPage.Elements.Count} elements: {string.Join(", ", impPage.Elements.GroupBy(e => e.GetType().Name).Select(g => $"{g.Key}={g.Count()}"))}");

            // Look for missing high-level constructs or broken items
            foreach (var origEl in origPage.Elements)
            {
                if (origEl is PdfTableElement table)
                {
                    Console.WriteLine($"  [ORIGINAL TABLE] X={table.X}, Y={table.Y}, Rows={table.Rows.Count}, Cols={table.Headers.Count}");
                }
                else if (origEl is PdfChartElement chart)
                {
                    Console.WriteLine($"  [ORIGINAL CHART] X={chart.X}, Y={chart.Y}, Type={chart.ChartType}, Title={chart.Title}");
                }
                else if (origEl is PdfQrCodeElement qr)
                {
                    Console.WriteLine($"  [ORIGINAL QR] X={qr.X}, Y={qr.Y}, Size={qr.Width}x{qr.Height}");
                }
            }
        }
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
        Assert.Equal("John_Doe_Executive_Resume.pdf", model.Title);

        var page = model.Pages[0];
        Assert.NotEmpty(page.Elements);
        Assert.True(page.Elements.Count >= 25, "Professional resume should have comprehensive sections and elements");

        // Verify presence of QR code, avatar badge, dividers, and rich text sections
        Assert.Contains(page.Elements, e => e is PdfQrCodeElement);
        Assert.Contains(page.Elements, e => e is PdfShapeElement shape && shape.Label == "JD");
        Assert.Contains(page.Elements, e => e is PdfDividerElement);
        Assert.Contains(page.Elements, e => e is PdfTextElement text && text.Text.Contains("JOHN DOE"));
        Assert.Contains(page.Elements, e => e is PdfTextElement text && text.Text.Contains("EXECUTIVE SUMMARY"));
        Assert.Contains(page.Elements, e => e is PdfTextElement text && text.Text.Contains("PROFESSIONAL EXPERIENCE"));
        Assert.Contains(page.Elements, e => e is PdfTextElement text && text.Text.Contains("EDUCATION & PROFESSIONAL CREDENTIALS"));
        Assert.Contains(page.Elements, e => e is PdfTextElement text && text.Text.Contains("NOTABLE PROJECTS"));

        byte[] pdfBytes = _exportService.GeneratePdfBytes(model);
        Assert.NotNull(pdfBytes);
        Assert.True(pdfBytes.Length > 2000, "Rich PDF byte stream should be substantial");

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
    public void AllRegisteredTemplates_GenerateValidPdfDocumentsAndByteStreams()
    {
        var templates = _templateService.GetAllTemplates();
        Assert.True(templates.Count >= 18, $"Expected at least 18 templates, found {templates.Count}");

        foreach (var def in templates)
        {
            var doc = def.Create();
            Assert.NotNull(doc);
            Assert.NotEmpty(doc.Title);
            Assert.NotEmpty(doc.Pages);

            var page = doc.Pages[0];
            if (def.Id != "" && def.Id != "blank") // Non-blank template
            {
                Assert.True(page.Elements.Count >= 3, $"Template '{def.Name}' ({def.Id}) should have at least 3 elements, found {page.Elements.Count}");
            }

            byte[] bytes = _exportService.GeneratePdfBytes(doc);
            Assert.NotNull(bytes);
            Assert.True(bytes.Length > 500, $"Generated PDF for '{def.Name}' should have non-trivial size");
            Assert.Equal("%PDF-", Encoding.ASCII.GetString(bytes, 0, 5));
        }
    }

    [Fact]
    public void ResumeTemplates_AllFourTypes_GenerateRichContent()
    {
        var executive = _templateService.CreateResumeTemplate();
        var modern = _templateService.CreateResumeModernCleanTemplate();
        var creative = _templateService.CreateResumeCreativeMinimalistTemplate();
        var academicCv = _templateService.CreateResumeAcademicCvTemplate();

        Assert.NotNull(executive);
        Assert.NotNull(modern);
        Assert.NotNull(creative);
        Assert.NotNull(academicCv);

        // Modern Clean tech resume checks
        Assert.Contains(modern.Pages[0].Elements, e => e is PdfTextElement t && t.Text.Contains("JOHN DOE"));
        Assert.Contains(modern.Pages[0].Elements, e => e is PdfQrCodeElement qr && qr.Content.Contains("github.com/codefrydev"));

        // Creative UI/UX resume checks
        Assert.Contains(creative.Pages[0].Elements, e => e is PdfTextElement t && t.Text.Contains("JANE DOE"));
        Assert.Contains(creative.Pages[0].Elements, e => e is PdfQrCodeElement qr && qr.Content.Contains("codefrydev.in"));

        // Academic CV checks
        Assert.Contains(academicCv.Pages[0].Elements, e => e is PdfTextElement t && t.Text.Contains("JOHN DOE"));
        Assert.Contains(academicCv.Pages[0].Elements, e => e is PdfTableElement tbl && tbl.Headers.Contains("Total Amount"));
        Assert.Contains(academicCv.Pages[0].Elements, e => e is PdfQrCodeElement qr && qr.Content.Contains("orcid.org"));
    }

    [Fact]
    public void ResearchPaperTemplates_AllFourTypes_GenerateRichContent()
    {
        var cs = _templateService.CreateAcademicPaperTemplate();
        var math = _templateService.CreateMathResearchPaperTemplate();
        var physics = _templateService.CreatePhysicsResearchPaperTemplate();
        var history = _templateService.CreateHistoryResearchPaperTemplate();
        var finance = _templateService.CreateFinanceResearchPaperTemplate();

        Assert.NotNull(cs);
        Assert.NotNull(math);
        Assert.NotNull(physics);
        Assert.NotNull(history);
        Assert.NotNull(finance);

        // Mathematics paper checks
        Assert.Contains(math.Pages[0].Elements, e => e is PdfTextElement t && t.Text.Contains("Discrete Hodge"));
        Assert.Contains(math.Pages[0].Elements, e => e is PdfTableElement tbl && tbl.Headers.Contains("β₁(M)"));

        // Physics paper checks
        Assert.Contains(physics.Pages[0].Elements, e => e is PdfTextElement t && t.Text.Contains("Cavity Quantum Electrodynamics"));
        Assert.Contains(physics.Pages[0].Elements, e => e is PdfTableElement tbl && tbl.Headers.Contains("T₁ (μs)"));

        // History paper checks
        Assert.Contains(history.Pages[0].Elements, e => e is PdfTextElement t && t.Text.Contains("Maritime Trade Networks"));
        Assert.Contains(history.Pages[0].Elements, e => e is PdfTableElement tbl && tbl.Headers.Contains("Gold Flow (Ducats)"));

        // Finance paper checks
        Assert.Contains(finance.Pages[0].Elements, e => e is PdfTextElement t && t.Text.Contains("Multi-Factor Jump-Diffusion"));
        Assert.Contains(finance.Pages[0].Elements, e => e is PdfTableElement tbl && tbl.Headers.Contains("Max DD"));
    }

    [Fact]
    public async Task ProjectPersistence_RoundTripMatches()
    {
        var original = _templateService.CreateAnnualReportTemplate();
        string tempPath = Path.Combine(Path.GetTempPath(), $"test_project_{Guid.NewGuid():N}.frypdf");

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

        page.Elements.Add(new PdfFormFieldElement
        {
            FieldType = FormFieldType.Text,
            Label = "Candidate Name:",
            Placeholder = "Jane Doe"
        });

        page.Elements.Add(new PdfFormFieldElement
        {
            FieldType = FormFieldType.Checkbox,
            Label = "NDA Signed",
            IsChecked = true
        });

        page.Elements.Add(new PdfFormFieldElement
        {
            FieldType = FormFieldType.Signature,
            Label = "Authorized Signatory",
            Value = "John Hancock"
        });

        page.Elements.Add(new PdfQrCodeElement
        {
            Content = "https://github.com/PrashantUnity/PDFCreator",
            Label = "VERIFICATION QR"
        });

        page.Elements.Add(new PdfBarcodeElement
        {
            CodeValue = "DOC-998822",
            ShowText = true
        });

        page.Elements.Add(new PdfRedactionElement
        {
            ExemptionCode = "[REDACTED - (b)(4)]"
        });

        page.Elements.Add(new PdfInkElement
        {
            IsHighlighter = true,
            StrokeThickness = 8
        });

        page.Elements.Add(new PdfStickyNoteElement
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
        page.Elements.Add(new PdfChartElement
        {
            Title = "Departmental Efficiency",
            ChartType = ChartType.HorizontalBar,
            Categories = new System.Collections.Generic.List<string> { "Dev", "QA", "Sales" },
            Values = new System.Collections.Generic.List<double> { 8.5, 7.2, 9.1 },
            ValueLabels = new System.Collections.Generic.List<string> { "85%", "72%", "91%" }
        });

        page.Elements.Add(new PdfChartElement
        {
            Title = "Budget Allocation",
            ChartType = ChartType.DonutPie,
            Categories = new System.Collections.Generic.List<string> { "Engineering", "Marketing", "Legal" },
            Values = new System.Collections.Generic.List<double> { 50, 30, 20 },
            ValueLabels = new System.Collections.Generic.List<string> { "$5M", "$3M", "$2M" }
        });

        // Test Shapes
        page.Elements.Add(new PdfShapeElement
        {
            ShapeType = ShapeType.Star5,
            FillColorHex = "#F59E0B"
        });

        page.Elements.Add(new PdfShapeElement
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

        qrVm.ApplyPresetTypeCommand.Execute("Sms");
        Assert.StartsWith("SMSTO:", qrVm.Content);

        qrVm.ApplyPresetTypeCommand.Execute("CryptoAddress");
        Assert.StartsWith("bitcoin:", qrVm.Content);

        qrVm.ApplyPresetTypeCommand.Execute("EventCalendar");
        Assert.Contains("BEGIN:VEVENT", qrVm.Content);
    }

    [Fact]
    public void QrCodeHelper_GeneratesValidPngAndAvaloniaBitmapWithCustomColorsAndEcc()
    {
        // 1. Generate PNG bytes with custom colors
        byte[] pngBytes = QrCodeHelper.GeneratePngBytes(
            "https://github.com/PrashantUnity/PDFCreator",
            darkHex: "#0F6CBD",
            lightHex: "#EFF6FF",
            ecc: QrCodeEccLevel.H,
            pixelsPerModule: 12,
            drawQuietZones: true);

        Assert.NotNull(pngBytes);
        Assert.True(pngBytes.Length > 100);
        // Verify PNG magic signature: 0x89, 'P', 'N', 'G'
        Assert.Equal(0x89, pngBytes[0]);
        Assert.Equal((byte)'P', pngBytes[1]);
        Assert.Equal((byte)'N', pngBytes[2]);
        Assert.Equal((byte)'G', pngBytes[3]);

        // 2. Generate PNG bytes with custom wifi and error correction
        byte[] wifiBytes = QrCodeHelper.GeneratePngBytes(
            "WIFI:S:TestNetwork;T:WPA2;P:Secret123;;",
            darkHex: "#990000",
            lightHex: "#FFFFFF",
            ecc: QrCodeEccLevel.Q);

        Assert.NotNull(wifiBytes);
        Assert.True(wifiBytes.Length > 100);
        Assert.Equal(0x89, wifiBytes[0]);
    }

    [Fact]
    public void QrCodeElementViewModel_LiveBitmapUpdates_OnPropertyChanged()
    {
        var vm = new PdfEditorApp.ViewModels.ElementViewModels.QrCodeElementViewModel();
        Assert.NotNull(vm.QrPngBytes);
        Assert.True(vm.QrPngBytes.Length > 0);

        vm.Content = "https://codefrydev.in/docs";
        Assert.NotNull(vm.QrPngBytes);

        vm.DarkColorHex = "#16A34A";
        Assert.NotNull(vm.QrPngBytes);

        vm.EccLevel = QrCodeEccLevel.H;
        Assert.NotNull(vm.QrPngBytes);
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

            var model = chartVm.ToModel() as PdfChartElement;
            Assert.NotNull(model);
            Assert.Equal(chartType, model.ChartType);

            var roundtripVm = new PdfEditorApp.ViewModels.ElementViewModels.ChartElementViewModel();
            roundtripVm.LoadFromModel(model);
            Assert.Equal(chartType, roundtripVm.ChartType);
        }
    }
    [Fact]
    public void LiveCharts2_HeadlessSkiaRendering_AllChartTypes_Work()
    {
        foreach (ChartType chartType in Enum.GetValues<ChartType>())
        {
            var chartEl = new PdfChartElement
            {
                Title = $"Test {chartType}",
                ChartType = chartType,
                Categories = new List<string> { "Q1", "Q2", "Q3", "Q4" },
                Values = new List<double> { 12, 19, 3, 25 },
                ValueLabels = new List<string> { "$12M", "$19M", "$3M", "$25M" },
                Palette = ChartPalette.CyberNeon,
                LegendPosition = ChartLegendPosition.Top,
                ShowDataLabels = true,
                ShowGridlines = true
            };

            byte[] pngBytes = PdfEditorApp.Core.Analysis.LiveChartsRenderer.RenderChartToPngBytes(chartEl, width: 400, height: 250);
            Assert.NotNull(pngBytes);
            Assert.NotEmpty(pngBytes);
            Assert.True(pngBytes.Length > 100);
        }
    }

    [Fact]
    public void LiveCharts2_MultiSeries_CartesianRendering_Works()
    {
        var chartEl = new PdfChartElement
        {
            Title = "2025 vs 2026 Sales Comparison",
            ChartType = ChartType.BarColumn,
            Categories = new List<string> { "North", "South", "East", "West" },
            Palette = ChartPalette.CorporateBlue,
            LegendPosition = ChartLegendPosition.Top,
            MultiSeries = new List<ChartSeriesItem>
            {
                new() { Name = "2025 Actual", Values = new List<double> { 120, 150, 180, 200 }, ColorHex = "#82BDF0" },
                new() { Name = "2026 Target", Values = new List<double> { 140, 175, 210, 240 }, ColorHex = "#0F6CBD" }
            }
        };

        byte[] pngBytes = PdfEditorApp.Core.Analysis.LiveChartsRenderer.RenderChartToPngBytes(chartEl, width: 500, height: 300);
        Assert.NotNull(pngBytes);
        Assert.NotEmpty(pngBytes);
        Assert.True(pngBytes.Length > 200);
    }

    [Fact]
    public void LiveCharts2_AllPalettes_RenderSuccessfully()
    {
        foreach (ChartPalette palette in Enum.GetValues<ChartPalette>())
        {
            var colors = PdfEditorApp.Core.Analysis.LiveChartsRenderer.GetPaletteHexColors(palette);
            Assert.NotEmpty(colors);
            Assert.True(colors.Count >= 4);

            var chartEl = new PdfChartElement
            {
                Title = $"Palette {palette}",
                ChartType = ChartType.SmoothLine,
                Palette = palette,
                Categories = new List<string> { "Jan", "Feb", "Mar", "Apr" },
                Values = new List<double> { 50, 65, 80, 95 }
            };

            byte[] pngBytes = PdfEditorApp.Core.Analysis.LiveChartsRenderer.RenderChartToPngBytes(chartEl, 300, 200);
            Assert.NotEmpty(pngBytes);
        }
    }

    [Fact]
    public void LiveCharts2_ChartElementViewModel_ReactiveUpdates_Work()
    {
        var vm = new PdfEditorApp.ViewModels.ElementViewModels.ChartElementViewModel();
        Assert.NotNull(vm.CartesianSeries);
        Assert.NotEmpty(vm.CartesianSeries);

        // Change palette
        vm.SetPaletteCommand.Execute("EmeraldGreen");
        Assert.Equal(ChartPalette.EmeraldGreen, vm.Palette);

        // Change legend position
        vm.SetLegendPositionCommand.Execute("Right");
        Assert.Equal(ChartLegendPosition.Right, vm.LegendPosition);
        Assert.Equal(LiveChartsCore.Measure.LegendPosition.Right, vm.LiveLegendPosition);

        // Switch to DonutPie
        vm.SetChartTypeCommand.Execute("DonutPie");
        Assert.Equal(ChartType.DonutPie, vm.ChartType);
        Assert.True(vm.IsDonutPie);
        Assert.True(vm.IsPieChart);
        Assert.False(vm.IsCartesianChart);
        Assert.NotEmpty(vm.PieSeries);

        // Switch to Radar
        vm.SetChartTypeCommand.Execute("Radar");
        Assert.Equal(ChartType.Radar, vm.ChartType);
        Assert.True(vm.IsRadar);
        Assert.True(vm.IsPolarChart);
        Assert.NotEmpty(vm.PolarSeries);

        // Add and Remove Data points
        int initialBars = vm.Bars.Count;
        vm.AddDataPointCommand.Execute(null);
        Assert.Equal(initialBars + 1, vm.Bars.Count);

        vm.RemoveDataPointCommand.Execute(null);
        Assert.Equal(initialBars, vm.Bars.Count);
    }

    [Fact]
    public void LiveCharts2_PdfExport_ContainsRenderedChart()
    {
        var doc = new PdfDocumentModel
        {
            Pages = new List<PdfPageModel>
            {
                new()
                {
                    Width = 595,
                    Height = 842,
                    Elements = new List<PdfElementBase>
                    {
                        new PdfChartElement
                        {
                            X = 50,
                            Y = 100,
                            Width = 495,
                            Height = 250,
                            Title = "Executive Financial Summary",
                            ChartType = ChartType.SmoothLine,
                            Palette = ChartPalette.CorporateBlue,
                            Categories = new List<string> { "2023", "2024", "2025", "2026" },
                            Values = new List<double> { 12.5, 18.2, 24.8, 31.0 }
                        }
                    }
                }
            }
        };

        byte[] pdfBytes = _exportService.GeneratePdfBytes(doc);
        Assert.NotNull(pdfBytes);
        Assert.NotEmpty(pdfBytes);
        Assert.True(pdfBytes.Length > 1000);
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

    [Fact]
    public void CommandPalette_IndexingAndFiltering_WorksAccurately()
    {
        var vm = new MainViewModel(_exportService, _templateService, _persistenceService);
        Assert.NotEmpty(vm.AllPaletteCommands);
        Assert.True(vm.AllPaletteCommands.Count >= 25);

        // Open palette
        vm.OpenCommandPaletteCommand.Execute(null);
        Assert.True(vm.IsCommandPaletteOpen);
        Assert.Equal(vm.AllPaletteCommands.Count, vm.FilteredPaletteCommands.Count);

        // Search for "text"
        vm.CommandSearchQuery = "text";
        Assert.NotEmpty(vm.FilteredPaletteCommands);
        Assert.All(vm.FilteredPaletteCommands, item =>
            Assert.True(item.Title.Contains("text", StringComparison.OrdinalIgnoreCase) ||
                        item.Subtitle.Contains("text", StringComparison.OrdinalIgnoreCase) ||
                        item.Category.Contains("text", StringComparison.OrdinalIgnoreCase)));

        // Search for "save"
        vm.CommandSearchQuery = "save";
        Assert.NotEmpty(vm.FilteredPaletteCommands);
        Assert.Contains(vm.FilteredPaletteCommands, item => item.Title.Contains("Save", StringComparison.OrdinalIgnoreCase));

        // Navigation
        vm.SelectNextPaletteCommand();
        Assert.True(vm.SelectedPaletteIndex >= 0);
        vm.SelectPreviousPaletteCommand();
        Assert.Equal(0, vm.SelectedPaletteIndex);

        // Close palette
        vm.CloseCommandPalette();
        Assert.False(vm.IsCommandPaletteOpen);
    }

    [Fact]
    public void PageNavigation_NextPreviousFirstLast_NavigatesProperly()
    {
        var vm = new MainViewModel(_exportService, _templateService, _persistenceService);
        Assert.Equal(3, vm.Pages.Count);
        Assert.Equal(1, vm.CurrentPageNumber);

        // Next page
        vm.NextPageCommand.Execute(null);
        Assert.Equal(2, vm.CurrentPageNumber);

        // Next page
        vm.NextPageCommand.Execute(null);
        Assert.Equal(3, vm.CurrentPageNumber);

        // Next page at boundary (should remain at 3)
        vm.NextPageCommand.Execute(null);
        Assert.Equal(3, vm.CurrentPageNumber);

        // Previous page
        vm.PreviousPageCommand.Execute(null);
        Assert.Equal(2, vm.CurrentPageNumber);

        // First page
        vm.FirstPageCommand.Execute(null);
        Assert.Equal(1, vm.CurrentPageNumber);

        // Last page
        vm.LastPageCommand.Execute(null);
        Assert.Equal(3, vm.CurrentPageNumber);
    }

    [Fact]
    public void PageOperations_WithUndoRedo_WorkAccurately()
    {
        var vm = new MainViewModel(_exportService, _templateService, _persistenceService);
        int initialPageCount = vm.Pages.Count;

        // Add page
        vm.AddPageCommand.Execute(null);
        Assert.Equal(initialPageCount + 1, vm.Pages.Count);

        // Undo add page
        vm.UndoCommand.Execute(null);
        Assert.Equal(initialPageCount, vm.Pages.Count);

        // Redo add page
        vm.RedoCommand.Execute(null);
        Assert.Equal(initialPageCount + 1, vm.Pages.Count);

        // Duplicate page
        vm.DuplicateCurrentPageCommand.Execute(null);
        Assert.Equal(initialPageCount + 2, vm.Pages.Count);

        // Undo duplicate
        vm.UndoCommand.Execute(null);
        Assert.Equal(initialPageCount + 1, vm.Pages.Count);

        // Rotate page with undo
        int currentAngle = vm.CurrentPage!.RotationAngle;
        vm.RotateCurrentPageCommand.Execute(null);
        Assert.Equal((currentAngle + 90) % 360, vm.CurrentPage.RotationAngle);

        vm.UndoCommand.Execute(null);
        Assert.Equal(currentAngle, vm.CurrentPage.RotationAngle);

        // Delete page with undo
        int beforeDeleteCount = vm.Pages.Count;
        vm.DeleteCurrentPageCommand.Execute(null);
        Assert.Equal(beforeDeleteCount - 1, vm.Pages.Count);

        vm.UndoCommand.Execute(null);
        Assert.Equal(beforeDeleteCount, vm.Pages.Count);
    }

    [Fact]
    public void ToastNotification_And_ShortcutsDialog_Work()
    {
        var vm = new MainViewModel(_exportService, _templateService, _persistenceService);

        // Show Toast
        vm.ShowToast("Document saved successfully", "CheckCircleOutline");
        Assert.Equal("Document saved successfully", vm.ToastMessage);
        Assert.Equal("CheckCircleOutline", vm.ToastIcon);
        Assert.True(vm.IsToastVisible);

        // Shortcuts Dialog
        vm.OpenShortcutsHelpCommand.Execute(null);
        Assert.True(vm.IsShortcutsHelpDialogOpen);

        vm.CloseShortcutsHelpCommand.Execute(null);
        Assert.False(vm.IsShortcutsHelpDialogOpen);

        // Tool Mode
        vm.SetToolMode("Draw");
        Assert.Equal(ToolMode.Draw, vm.ActiveToolMode);

        vm.SetToolMode("Select");
        Assert.Equal(ToolMode.Select, vm.ActiveToolMode);
    }

    [Fact]
    public void UndoRedo_InspectorTypography_RevertsAndRedoes()
    {
        var vm = new MainViewModel(_exportService, _templateService, _persistenceService);
        var page = vm.CurrentPage!;

        var textEl = new TextElementViewModel
        {
            Text = "Sample Headline",
            FontFamily = "Segoe UI",
            FontSize = 14,
            IsBold = false,
            TextColorHex = "#201F1E",
            Alignment = TextAlignmentMode.Left
        };
        page.AddElement(textEl);
        vm.Inspector.UpdateSelection(textEl, page);

        // 1. Text Color change
        vm.Inspector.SetTextColorCommand.Execute("#DC2626");
        Assert.Equal("#DC2626", textEl.TextColorHex);
        Assert.Equal("Change Text Color", vm.UndoRedo.NextUndoDescription);

        vm.UndoCommand.Execute(null);
        Assert.Equal("#201F1E", textEl.TextColorHex);

        vm.RedoCommand.Execute(null);
        Assert.Equal("#DC2626", textEl.TextColorHex);

        // 2. Bold toggle
        vm.Inspector.ToggleBoldCommand.Execute(null);
        Assert.True(textEl.IsBold);
        Assert.Equal("Format Bold", vm.UndoRedo.NextUndoDescription);

        vm.UndoCommand.Execute(null);
        Assert.False(textEl.IsBold);

        vm.RedoCommand.Execute(null);
        Assert.True(textEl.IsBold);

        // 3. Text Alignment
        vm.Inspector.SetAlignmentCommand.Execute("Center");
        Assert.Equal(TextAlignmentMode.Center, textEl.Alignment);

        vm.UndoCommand.Execute(null);
        Assert.Equal(TextAlignmentMode.Left, textEl.Alignment);

        vm.RedoCommand.Execute(null);
        Assert.Equal(TextAlignmentMode.Center, textEl.Alignment);
    }

    [Fact]
    public void UndoRedo_InspectorShapeFormatting_RevertsAndRedoes()
    {
        var vm = new MainViewModel(_exportService, _templateService, _persistenceService);
        var page = vm.CurrentPage!;

        var shapeEl = new ShapeElementViewModel
        {
            ShapeType = ShapeType.Rectangle,
            FillColorHex = "#F0F7FD",
            StrokeColorHex = "#0F6CBD"
        };
        page.AddElement(shapeEl);
        vm.Inspector.UpdateSelection(shapeEl, page);

        // Fill Color
        vm.Inspector.SetShapeFillColorCommand.Execute("#16A34A");
        Assert.Equal("#16A34A", shapeEl.FillColorHex);

        vm.UndoCommand.Execute(null);
        Assert.Equal("#F0F7FD", shapeEl.FillColorHex);

        vm.RedoCommand.Execute(null);
        Assert.Equal("#16A34A", shapeEl.FillColorHex);

        // Stroke Color
        vm.Inspector.SetShapeStrokeColorCommand.Execute("#EA580C");
        Assert.Equal("#EA580C", shapeEl.StrokeColorHex);

        vm.UndoCommand.Execute(null);
        Assert.Equal("#0F6CBD", shapeEl.StrokeColorHex);

        vm.RedoCommand.Execute(null);
        Assert.Equal("#EA580C", shapeEl.StrokeColorHex);

        // Shape Type
        vm.Inspector.SetShapeTypeCommand.Execute("Circle");
        Assert.Equal(ShapeType.Circle, shapeEl.ShapeType);

        vm.UndoCommand.Execute(null);
        Assert.Equal(ShapeType.Rectangle, shapeEl.ShapeType);

        vm.RedoCommand.Execute(null);
        Assert.Equal(ShapeType.Circle, shapeEl.ShapeType);
    }

    [Fact]
    public void UndoRedo_InspectorAlignment_And_Layering_RevertsAndRedoes()
    {
        var vm = new MainViewModel(_exportService, _templateService, _persistenceService);
        var page = vm.CurrentPage!;

        page.Elements.Clear();
        var bottomEl = new ShapeElementViewModel { X = 10, Y = 10 };
        var shapeEl = new ShapeElementViewModel
        {
            X = 200,
            Y = 300,
            Width = 100,
            Height = 100
        };
        var topEl = new ShapeElementViewModel { X = 50, Y = 50 };
        page.AddElement(bottomEl);
        page.AddElement(shapeEl);
        page.AddElement(topEl);
        vm.Inspector.UpdateSelection(shapeEl, page);

        // Align Left
        vm.Inspector.AlignLeftCommand.Execute(null);
        Assert.Equal(60, shapeEl.X);

        vm.UndoCommand.Execute(null);
        Assert.Equal(200, shapeEl.X);

        vm.RedoCommand.Execute(null);
        Assert.Equal(60, shapeEl.X);

        // Align Center
        vm.Inspector.AlignCenterCommand.Execute(null);
        double expectedCenter = (page.Width - shapeEl.Width) / 2;
        Assert.Equal(expectedCenter, shapeEl.X);

        vm.UndoCommand.Execute(null);
        Assert.Equal(60, shapeEl.X);

        // Layering Z-Order
        int oldZ = shapeEl.ZIndex;
        vm.Inspector.BringToFrontCommand.Execute(null);
        Assert.True(shapeEl.ZIndex > oldZ);
        Assert.Equal(page.Elements.Count - 1, page.Elements.IndexOf(shapeEl));

        vm.UndoCommand.Execute(null);
        Assert.Equal(oldZ, shapeEl.ZIndex);
        Assert.Equal(1, page.Elements.IndexOf(shapeEl));

        // Send to Back
        vm.Inspector.SendToBackCommand.Execute(null);
        Assert.Equal(1, shapeEl.ZIndex);
        Assert.Equal(0, page.Elements.IndexOf(shapeEl));

        vm.UndoCommand.Execute(null);
        Assert.Equal(oldZ, shapeEl.ZIndex);
        Assert.Equal(1, page.Elements.IndexOf(shapeEl));
    }

    [Fact]
    public void UndoRedo_ElementDeleteAndDuplicate_WorksAccurately()
    {
        var vm = new MainViewModel(_exportService, _templateService, _persistenceService);
        var page = vm.CurrentPage!;
        int initialCount = page.Elements.Count;

        var textEl = new TextElementViewModel { Text = "Target Element" };
        page.AddElement(textEl);
        Assert.Equal(initialCount + 1, page.Elements.Count);

        vm.Inspector.UpdateSelection(textEl, page);

        // Duplicate
        vm.Inspector.DuplicateSelectedElementCommand.Execute(null);
        Assert.Equal(initialCount + 2, page.Elements.Count);

        vm.UndoCommand.Execute(null);
        Assert.Equal(initialCount + 1, page.Elements.Count);

        vm.RedoCommand.Execute(null);
        Assert.Equal(initialCount + 2, page.Elements.Count);

        // Delete
        vm.Inspector.DeleteSelectedElementCommand.Execute(null);
        Assert.Equal(initialCount + 1, page.Elements.Count);

        vm.UndoCommand.Execute(null);
        Assert.Equal(initialCount + 2, page.Elements.Count);
    }

    [Fact]
    public void SignatureService_GeneratesValidSignaturesAndInitials()
    {
        var sigService = new SignatureService();

        var sig = sigService.CreateCursiveSignatureElement("Alexander Hamilton", SignatureStyle.CursiveElegance, 100, 200);
        Assert.NotNull(sig);
        Assert.Equal("Alexander Hamilton", sig.Text);
        Assert.Equal("Georgia", sig.FontFamily);
        Assert.True(sig.IsItalic);

        var dateStamp = sigService.CreateDateStampElement(100, 200);
        Assert.NotNull(dateStamp);
        Assert.Contains(DateTime.Now.Year.ToString(), dateStamp.Text);

        var initials = sigService.CreateInitialsElement("AH", 100, 200);
        Assert.NotNull(initials);
        Assert.Equal("AH", initials.Label);
        Assert.Equal(ShapeType.Circle, initials.ShapeType);

        var checkmark = sigService.CreateMarkupBadge("✓", "#16A34A", 100, 200);
        Assert.NotNull(checkmark);
        Assert.Equal("✓", checkmark.Label);
    }

    [Fact]
    public void DocumentAuditService_EvaluatesHealthAndChecks()
    {
        var auditService = new DocumentAuditService();
        var doc = _templateService.CreateAnnualReportTemplate();

        var report = auditService.RunAudit(doc);
        Assert.NotNull(report);
        Assert.True(report.HealthScore >= 70);
        Assert.NotEmpty(report.Grade);
        Assert.Equal(3, report.TotalPages);
        Assert.True(report.TotalWordCount > 0);
        Assert.NotEmpty(report.Issues);
    }

    [Fact]
    public void SecuritySettings_PasswordEncryptionAndPermissions_ExportPasses()
    {
        var doc = _templateService.CreateInvoiceTemplate();
        doc.SecuritySettings.IsPasswordProtected = true;
        doc.SecuritySettings.OpenPassword = "SecurePassword123!";
        doc.SecuritySettings.AllowPrinting = true;
        doc.SecuritySettings.AllowContentCopying = false;
        doc.SecuritySettings.ScrubMetadataOnExport = true;

        byte[] pdfBytes = _exportService.GeneratePdfBytes(doc);
        Assert.NotNull(pdfBytes);
        Assert.True(pdfBytes.Length > 500);
        Assert.Equal("%PDF-", Encoding.ASCII.GetString(pdfBytes, 0, 5));
    }

    [Fact]
    public void DocumentSanitization_ClearsMetadataAndComments()
    {
        var vm = new MainViewModel(_exportService, _templateService, _persistenceService);
        var page = vm.CurrentPage!;
        page.AddElement(new StickyNoteElementViewModel { NoteText = "Internal confidential review comment" });

        vm.SanitizeDocument();
        Assert.Equal("Anonymous", vm.DocumentAuthor);
        Assert.Equal("", vm.DocumentSubject);
        Assert.True(vm.SecuritySettings.ScrubMetadataOnExport);
        Assert.True(vm.SecuritySettings.RemoveCommentsOnExport);
        Assert.DoesNotContain(page.Elements, e => e is StickyNoteElementViewModel);
    }

    [Fact]
    public void Typography_StrikethroughAndTextCase_TransformationsWork()
    {
        var textVm = new TextElementViewModel { Text = "hello world acrobat suite" };

        textVm.TransformUppercase();
        Assert.Equal("HELLO WORLD ACROBAT SUITE", textVm.Text);

        textVm.TransformTitleCase();
        Assert.Equal("Hello World Acrobat Suite", textVm.Text);

        textVm.TransformLowercase();
        Assert.Equal("hello world acrobat suite", textVm.Text);

        textVm.ToggleBulletList();
        Assert.StartsWith("• ", textVm.Text);

        textVm.ToggleNumberedList();
        Assert.StartsWith("1. ", textVm.Text);

        textVm.IsStrikethrough = true;
        var model = (PdfTextElement)textVm.ToModel();
        Assert.True(model.IsStrikethrough);
    }

    [Fact]
    public void AcroForms_ValidationAndOptions_WorkCorrectly()
    {
        var formVm = new FormFieldElementViewModel
        {
            FieldType = FormFieldType.Dropdown,
            FieldName = "StateProvince",
            ValidationType = FormValidationType.CustomRegex,
            CustomValidationRegex = @"^[A-Z]{2}$",
            IsReadOnly = false,
            IsRequired = true,
            DefaultValue = "CA",
            Tooltip = "Select 2-letter state code"
        };
        formVm.Options.Clear();
        formVm.AddOption("NY");
        formVm.AddOption("TX");

        Assert.Equal(2, formVm.Options.Count);

        var model = (PdfFormFieldElement)formVm.ToModel();
        Assert.Equal(FormFieldType.Dropdown, model.FieldType);
        Assert.Equal("StateProvince", model.FieldName);
        Assert.Equal(FormValidationType.CustomRegex, model.ValidationType);
        Assert.Equal("CA", model.DefaultValue);
        Assert.Equal(2, model.Options.Count);
    }

    [Fact]
    public void SmartDistribution_DistributesElementsEvenly()
    {
        var vm = new MainViewModel(_exportService, _templateService, _persistenceService);
        var page = vm.CurrentPage!;
        page.Elements.Clear();

        var el1 = new ShapeElementViewModel { X = 0, Y = 100, Width = 50, Height = 50 };
        var el2 = new ShapeElementViewModel { X = 100, Y = 100, Width = 50, Height = 50 };
        var el3 = new ShapeElementViewModel { X = 300, Y = 100, Width = 50, Height = 50 };

        page.AddElement(el1);
        page.AddElement(el2);
        page.AddElement(el3);

        vm.Inspector.UpdateSelection(el1, page);
        vm.Inspector.DistributeHorizontally();

        // Total span: 0 to 350 (width 350). Elements total width: 150. Remaining space: 200 / 2 = 100 gap.
        // Expected X: el1 = 0, el2 = 150, el3 = 300.
        Assert.Equal(0, el1.X);
        Assert.Equal(150, el2.X);
        Assert.Equal(300, el3.X);
    }

    [Fact]
    public void Inspector_TypographyAndTextDecorations_ReflectImmediately()
    {
        var vm = new MainViewModel(_exportService, _templateService, _persistenceService);
        var page = vm.CurrentPage!;
        var textEl = new TextElementViewModel { Text = "Sample Heading", FontSize = 16 };
        page.AddElement(textEl);

        vm.Inspector.UpdateSelection(textEl, page);
        Assert.NotNull(vm.Inspector.TextElement);

        // Toggle Strikethrough and Underline
        vm.Inspector.ToggleStrikethroughCommand.Execute(null);
        Assert.True(textEl.IsStrikethrough);
        Assert.NotNull(textEl.TextDecorations);

        vm.Inspector.ToggleUnderlineCommand.Execute(null);
        Assert.True(textEl.IsUnderline);
        Assert.NotNull(textEl.TextDecorations);

        // Change text color
        vm.Inspector.SetTextColorCommand.Execute("#0F6CBD");
        Assert.Equal("#0F6CBD", textEl.TextColorHex);

        // Set alignment
        vm.Inspector.SetAlignmentCommand.Execute("Center");
        Assert.Equal(TextAlignmentMode.Center, textEl.Alignment);

        // Switch Font Family
        vm.Inspector.SelectedFontFamily = "Georgia";
        Assert.Equal("Georgia", textEl.FontFamily);

        vm.Inspector.SelectedFontFamily = "Roboto";
        Assert.Equal("Roboto", textEl.FontFamily);
        Assert.NotNull(textEl.AvaloniaFontFamily);

        vm.Inspector.SelectedFontFamily = "Playfair Display";
        Assert.Equal("Playfair Display", textEl.FontFamily);
        Assert.NotNull(textEl.AvaloniaFontFamily);

        vm.Inspector.SelectedFontFamily = "Dancing Script";
        Assert.Equal("Dancing Script", textEl.FontFamily);
        Assert.NotNull(textEl.AvaloniaFontFamily);

        vm.Inspector.SelectedFontFamily = "Courier New";
        Assert.Equal("Courier New", textEl.FontFamily);

        vm.Inspector.SelectedFontFamily = "Impact";
        Assert.Equal("Impact", textEl.FontFamily);

        // Computed line height
        textEl.LineHeight = 1.5;
        Assert.Equal(16 * 1.5, textEl.ComputedLineHeight);
    }

    [Fact]
    public void Inspector_ShapeStylesAndLabels_ReflectImmediately()
    {
        var vm = new MainViewModel(_exportService, _templateService, _persistenceService);
        var page = vm.CurrentPage!;
        var shapeEl = new ShapeElementViewModel { Width = 120, Height = 60 };
        page.AddElement(shapeEl);

        vm.Inspector.UpdateSelection(shapeEl, page);
        Assert.NotNull(vm.Inspector.ShapeElement);

        // Change shape type
        vm.Inspector.SetShapeTypeCommand.Execute("Star4Badge");
        Assert.Equal(ShapeType.Star4Badge, shapeEl.ShapeType);
        Assert.NotEmpty(shapeEl.PathData);

        // Set colors
        vm.Inspector.SetShapeFillColorCommand.Execute("#16A34A");
        Assert.Equal("#16A34A", shapeEl.FillColorHex);

        vm.Inspector.SetShapeStrokeColorCommand.Execute("#DC2626");
        Assert.Equal("#DC2626", shapeEl.StrokeColorHex);

        // Set label
        shapeEl.Label = "CERTIFIED";
        vm.Inspector.SetShapeLabelColorCommand.Execute("#FFFFFF");
        Assert.Equal("#FFFFFF", shapeEl.LabelColorHex);
    }

    [Fact]
    public void Inspector_AllElementColorsAndProperties_ReflectProperly()
    {
        var vm = new MainViewModel(_exportService, _templateService, _persistenceService);
        var page = vm.CurrentPage!;

        // Divider
        var divEl = new DividerElementViewModel();
        page.AddElement(divEl);
        vm.Inspector.UpdateSelection(divEl, page);
        vm.Inspector.SetDividerColorCommand.Execute("#EA580C");
        Assert.Equal("#EA580C", divEl.ColorHex);

        // Ink
        var inkEl = new InkElementViewModel();
        page.AddElement(inkEl);
        vm.Inspector.UpdateSelection(inkEl, page);
        vm.Inspector.SetInkColorCommand.Execute("#7C3AED");
        Assert.Equal("#7C3AED", inkEl.StrokeColorHex);

        // Sticky note
        var noteEl = new StickyNoteElementViewModel();
        page.AddElement(noteEl);
        vm.Inspector.UpdateSelection(noteEl, page);
        vm.Inspector.SetStickyNoteColorCommand.Execute("#DBEAFE");
        Assert.Equal("#DBEAFE", noteEl.ColorHex);

        // Redaction
        var redEl = new RedactionElementViewModel();
        page.AddElement(redEl);
        vm.Inspector.UpdateSelection(redEl, page);
        vm.Inspector.SetRedactionFillColorCommand.Execute("#0F172A");
        Assert.Equal("#0F172A", redEl.FillColorHex);

        // Form field
        var formEl = new FormFieldElementViewModel();
        page.AddElement(formEl);
        vm.Inspector.UpdateSelection(formEl, page);
        vm.Inspector.SetFormFieldTypeCommand.Execute("DatePicker");
        Assert.Equal(FormFieldType.DatePicker, formEl.FieldType);
        vm.Inspector.SetFormFieldBorderColorCommand.Execute("#16A34A");
        Assert.Equal("#16A34A", formEl.BorderColorHex);
    }

    [Fact]
    public void DocumentCompareService_DetectsDifferences_BetweenRevisions()
    {
        var compareService = new DocumentCompareService();

        var original = new PdfDocumentModel
        {
            Title = "Contract v1.0",
            Pages = new System.Collections.Generic.List<PdfPageModel>
            {
                new PdfPageModel
                {
                    PageNumber = 1,
                    Elements = new System.Collections.Generic.List<PdfElementBase>
                    {
                        new PdfTextElement { Id = "el-1", Text = "Initial Clause A", X = 50, Y = 50 },
                        new PdfTextElement { Id = "el-2", Text = "Unchanged Clause B", X = 50, Y = 100 }
                    }
                }
            }
        };

        var modified = new PdfDocumentModel
        {
            Title = "Contract v2.0",
            Pages = new System.Collections.Generic.List<PdfPageModel>
            {
                new PdfPageModel
                {
                    PageNumber = 1,
                    Elements = new System.Collections.Generic.List<PdfElementBase>
                    {
                        new PdfTextElement { Id = "el-1", Text = "Amended Clause A (Updated)", X = 50, Y = 50 },
                        new PdfTextElement { Id = "el-2", Text = "Unchanged Clause B", X = 50, Y = 100 },
                        new PdfTextElement { Id = "el-3", Text = "Newly Added Clause C", X = 50, Y = 150 }
                    }
                }
            }
        };

        var report = compareService.CompareDocuments(original, modified);

        Assert.NotNull(report);
        Assert.Equal(1, report.AdditionsCount); // el-3 added
        Assert.Equal(1, report.ModificationsCount); // el-1 modified
        Assert.Equal(0, report.DeletionsCount);
        Assert.Contains(report.Differences, d => d.DiffType == CompareDiffType.ElementAdded && d.Description.Contains("Newly Added Clause C"));
        Assert.Contains(report.Differences, d => d.DiffType == CompareDiffType.TextModified && d.OldValue == "Initial Clause A");
    }

    [Fact]
    public void BatesNumbering_CustomPrefixAndPosition_AppliesAndRemovesCorrectly()
    {
        var vm = new MainViewModel(_exportService, _templateService, _persistenceService);
        Assert.True(vm.Pages.Count >= 3);

        vm.BatesPrefix = "LEGAL-BATES-";
        vm.BatesStartingNumber = 101;
        vm.BatesNumberOfDigits = 5;
        vm.BatesPosition = BatesPosition.TopRight;

        vm.ApplyBatesNumberingCommand.Execute(null);

        Assert.Equal("LEGAL-BATES-00101", vm.Pages[0].HeaderRight);
        Assert.Equal("LEGAL-BATES-00102", vm.Pages[1].HeaderRight);
        Assert.Equal("LEGAL-BATES-00103", vm.Pages[2].HeaderRight);

        // Remove Bates numbering
        vm.RemoveBatesNumberingCommand.Execute(null);
        Assert.True(string.IsNullOrEmpty(vm.Pages[0].HeaderRight));
        Assert.True(string.IsNullOrEmpty(vm.Pages[1].HeaderRight));
    }

    [Fact]
    public void OrganizePages_BatchRotate_RotatesPagesCorrectly()
    {
        var vm = new MainViewModel(_exportService, _templateService, _persistenceService);
        int initialPage1Angle = vm.Pages[0].RotationAngle;
        int initialPage2Angle = vm.Pages[1].RotationAngle;

        vm.BatchRotatePagesCommand.Execute("all");

        Assert.Equal((initialPage1Angle + 90) % 360, vm.Pages[0].RotationAngle);
        Assert.Equal((initialPage2Angle + 90) % 360, vm.Pages[1].RotationAngle);
    }

    [Fact]
    public void AcroForms_RecalculateFormFields_CalculatesFormulas()
    {
        var page = new PageViewModel();

        var f1 = new FormFieldElementViewModel { FieldName = "Subtotal", Value = "150.00" };
        var f2 = new FormFieldElementViewModel { FieldName = "Tax", Value = "15.00" };
        var f3 = new FormFieldElementViewModel
        {
            FieldName = "Total",
            CalculationFormula = CalculationFormula.Sum,
            CalculationSourceFields = "Subtotal, Tax"
        };
        var fAvg = new FormFieldElementViewModel
        {
            FieldName = "AverageItem",
            CalculationFormula = CalculationFormula.Average,
            CalculationSourceFields = "Subtotal, Tax"
        };

        page.AddElement(f1);
        page.AddElement(f2);
        page.AddElement(f3);
        page.AddElement(fAvg);

        page.RecalculateFormFields();

        Assert.Equal("165.00", f3.Value);
        Assert.Equal("82.50", fAvg.Value);
    }

    [Fact]
    public void MeasurementElement_CalculatesDistance_AndFormatsUnits()
    {
        var meas = new PdfMeasurementElement
        {
            StartX = 0,
            StartY = 0,
            EndX = 72,
            EndY = 0, // 72 pts = 1.00 inch
            Unit = RulerUnit.Inches
        };

        Assert.Equal(72.0, meas.CalculateDistance(), 2);
        Assert.Equal("1.00 in", meas.GetFormattedDistance());

        meas.Unit = RulerUnit.Millimeters;
        Assert.Equal("25.4 mm", meas.GetFormattedDistance());

        meas.Unit = RulerUnit.Points;
        Assert.Equal("72.0 pt", meas.GetFormattedDistance());
    }

    [Fact]
    public void FindAndReplace_FindsAndReplacesText_AcrossElements()
    {
        var vm = new MainViewModel(_exportService, _templateService, _persistenceService);
        var page = vm.CurrentPage!;

        var txt1 = new TextElementViewModel { Text = "Acme Global Corporation Quarterly Financial Summary" };
        var txt2 = new TextElementViewModel { Text = "Prepared exclusively for Acme Global Leadership" };
        page.AddElement(txt1);
        page.AddElement(txt2);

        vm.FindQuery = "Acme Global";
        vm.ReplaceQuery = "Apex International";
        vm.FindMatchCase = true;

        vm.FindNextCommand.Execute(null);
        Assert.Equal(2, vm.FindMatchesCount);

        vm.ReplaceAllCommand.Execute(null);

        Assert.Contains("Apex International Corporation", txt1.Text);
        Assert.Contains("Apex International Leadership", txt2.Text);
    }

    [Fact]
    public void AboutDialog_OpenClose_AndSupportCommands_Work()
    {
        var vm = new MainViewModel(_exportService, _templateService, _persistenceService);

        // Verify initial state
        Assert.False(vm.IsAboutDialogOpen);

        // Open About Dialog
        vm.OpenAboutDialogCommand.Execute(null);
        Assert.True(vm.IsAboutDialogOpen);

        // Close About Dialog
        vm.CloseAboutDialogCommand.Execute(null);
        Assert.False(vm.IsAboutDialogOpen);

        // Direct method call
        vm.OpenAboutDialog();
        Assert.True(vm.IsAboutDialogOpen);
        vm.CloseAboutDialog();
        Assert.False(vm.IsAboutDialogOpen);

        // Verify Command Palette has About & Support commands
        vm.OpenCommandPaletteCommand.Execute(null);
        vm.FilterPaletteCommands("About");
        Assert.True(vm.FilteredPaletteCommands.Count > 0);

        vm.FilterPaletteCommands("codefrydev@gmail.com");
        Assert.True(vm.FilteredPaletteCommands.Count > 0);

        vm.FilterPaletteCommands("codefrydev.in");
        Assert.True(vm.FilteredPaletteCommands.Count > 0);

        vm.FilterPaletteCommands("Microsoft Store");
        Assert.True(vm.FilteredPaletteCommands.Count > 0);

        vm.FilterPaletteCommands("GitHub");
        Assert.True(vm.FilteredPaletteCommands.Count > 0);

        Assert.NotNull(vm.OpenMicrosoftStoreCommand);
        Assert.NotNull(vm.OpenGitHubCommand);
    }

    [Fact]
    public void TemplateRegistry_AllDefinitions_ProduceValidDocumentsAndExport()
    {
        var templateService = new TemplateService();
        var allTemplates = templateService.GetAllTemplates();

        Assert.NotEmpty(allTemplates);
        Assert.True(allTemplates.Count >= 6, "Expected at least 6 default templates registered");

        foreach (var t in allTemplates)
        {
            Assert.False(string.IsNullOrWhiteSpace(t.Id));
            Assert.False(string.IsNullOrWhiteSpace(t.Name));
            Assert.False(string.IsNullOrWhiteSpace(t.Description));
            Assert.False(string.IsNullOrWhiteSpace(t.IconKind));
            Assert.False(string.IsNullOrWhiteSpace(t.AccentColorHex));

            var doc = t.Create();
            Assert.NotNull(doc);
            Assert.NotEmpty(doc.Pages);

            byte[] bytes = _exportService.GeneratePdfBytes(doc);
            Assert.NotNull(bytes);
            Assert.True(bytes.Length > 200);
            Assert.Equal("%PDF-", Encoding.ASCII.GetString(bytes, 0, 5));
        }
    }

    [Fact]
    public void CommunityTemplate_CustomRegistration_Works()
    {
        var templateService = new TemplateService();
        var customTemplate = new CustomCommunitySampleTemplate();

        templateService.RegisterTemplate(customTemplate);

        var retrieved = templateService.CreateTemplate("custom-flyer");
        Assert.NotNull(retrieved);
        Assert.Equal("Community Event Flyer", retrieved.Title);
        Assert.Single(retrieved.Pages);

        var vm = new MainViewModel(_exportService, templateService, _persistenceService);
        vm.CreateNewFromTemplate("custom-flyer");
        Assert.Equal("Community Event Flyer", vm.DocumentTitle);
    }

    private class CustomCommunitySampleTemplate : PdfEditorApp.Templates.ITemplateDefinition
    {
        public string Id => "custom-flyer";
        public string Name => "Community Flyer";
        public string Description => "Sample community event flyer template";
        public string Category => "Events";
        public string IconKind => "BullhornOutline";
        public string AccentColorHex => "#E11D48";

        public PdfDocumentModel Create()
        {
            var doc = new PdfDocumentModel { Title = "Community Event Flyer" };
            var page = new PdfPageModel { PageNumber = 1, Width = 800, Height = 1131 };
            page.Elements.Add(new PdfTextElement { Text = "Community Open Day 2026", FontSize = 28, IsBold = true });
            doc.Pages.Add(page);
            return doc;
        }
    }

    [Fact]
    public void TemplateGallery_FilteringAndSearch_WorksProperly()
    {
        var vm = new MainViewModel(_exportService, _templateService, _persistenceService);

        // Open Dialog resets query and category
        vm.OpenNewDocumentDialog();
        Assert.True(vm.IsNewDocumentDialogOpen);
        Assert.Equal("All", vm.SelectedTemplateCategory);
        Assert.Equal("", vm.TemplateSearchQuery);

        // All visible initially
        Assert.True(vm.IsAnnualReportTemplateVisible);
        Assert.True(vm.IsInvoiceTemplateVisible);
        Assert.True(vm.IsResumeTemplateVisible);
        Assert.True(vm.IsAcademicPaperTemplateVisible);
        Assert.True(vm.IsCertificateTemplateVisible);
        Assert.True(vm.IsBlankTemplateVisible);
        Assert.False(vm.HasNoMatchingTemplates);

        // Category filter: Career
        vm.SetTemplateCategory("Career");
        Assert.True(vm.IsResumeTemplateVisible);
        Assert.False(vm.IsAnnualReportTemplateVisible);
        Assert.False(vm.IsInvoiceTemplateVisible);

        // Category filter: Finance
        vm.SetTemplateCategory("Finance");
        Assert.True(vm.IsInvoiceTemplateVisible);
        Assert.False(vm.IsResumeTemplateVisible);

        // Search query filter: "report"
        vm.SetTemplateCategory("All");
        vm.TemplateSearchQuery = "report";
        Assert.True(vm.IsAnnualReportTemplateVisible);
        Assert.False(vm.IsInvoiceTemplateVisible);
        Assert.False(vm.IsResumeTemplateVisible);

        // Clear search
        vm.ClearTemplateSearch();
        Assert.Equal("", vm.TemplateSearchQuery);
        Assert.True(vm.IsResumeTemplateVisible);

        // Non-existent search query shows empty state
        vm.TemplateSearchQuery = "xyznonexistenttemplate";
        Assert.True(vm.HasNoMatchingTemplates);

        // Close dialog
        vm.CloseNewDocumentDialog();
        Assert.False(vm.IsNewDocumentDialogOpen);
    }

    [Fact]
    public void CertificateOfAchievement_RedAndGold_HasFullVectorAccentsAndMatchesReferenceImage()
    {
        var model = _templateService.CreateCertificateTemplate();
        Assert.NotNull(model);
        Assert.Single(model.Pages);
        Assert.Equal("Certificate_of_Achievement.pdf", model.Title);

        var page = model.Pages[0];
        Assert.Equal(PageOrientation.Landscape, page.Orientation);
        Assert.NotEmpty(page.Elements);

        // Verify corner polygonal shapes exist
        var shapes = page.Elements.OfType<PdfShapeElement>().ToList();
        Assert.True(shapes.Count >= 7, "Must contain all corner polygon wedges, accents, and medal badge");

        // Verify medal ribbon badge
        var medal = shapes.FirstOrDefault(s => s.ShapeType == ShapeType.MedalRibbonBadge);
        Assert.NotNull(medal);
        Assert.Equal("#F59E0B", medal.FillColorHex);
        Assert.Equal("#990000", medal.SecondaryFillColorHex);

        // Verify typography elements
        var texts = page.Elements.OfType<PdfTextElement>().ToList();
        Assert.Contains(texts, t => t.Text == "CERTIFICATE" && t.TextColorHex == "#990000");
        Assert.Contains(texts, t => t.Text.Contains("OUTSTANDING ACHIEVEMENT"));
        Assert.Contains(texts, t => t.Text.Contains("THIS CERTIFICATE IS PROUDLY PRESENTED TO"));
        Assert.Contains(texts, t => t.Text == "John Doe" && t.FontFamily == "Great Vibes");
        Assert.Contains(texts, t => t.Text.Contains("Mathematics & Computational Science Olympiad"));
        Assert.Contains(texts, t => t.Text == "Jane Doe" && t.FontFamily == "Great Vibes");
        Assert.Contains(texts, t => t.Text.Contains("Dr. Jane Doe, Ph.D."));
        Assert.Contains(texts, t => t.Text.Contains("President"));
        Assert.Contains(texts, t => t.Text.Contains("Date"));

        // Verify PDF byte export
        byte[] pdfBytes = _exportService.GeneratePdfBytes(model);
        Assert.NotNull(pdfBytes);
        Assert.True(pdfBytes.Length > 2000);
        Assert.Equal("%PDF-", Encoding.ASCII.GetString(pdfBytes, 0, 5));
    }

    [Fact]
    public void CertificateNavyGoldAndDiploma_ProduceValidPdfBytes()
    {
        var navyCert = _templateService.CreateCertificateNavyGoldTemplate();
        Assert.NotNull(navyCert);
        byte[] navyBytes = _exportService.GeneratePdfBytes(navyCert);
        Assert.NotNull(navyBytes);
        Assert.True(navyBytes.Length > 1500);
        Assert.Equal("%PDF-", Encoding.ASCII.GetString(navyBytes, 0, 5));

        var diploma = _templateService.CreateDiplomaAcademicTemplate();
        Assert.NotNull(diploma);
        byte[] diplomaBytes = _exportService.GeneratePdfBytes(diploma);
        Assert.NotNull(diplomaBytes);
        Assert.True(diplomaBytes.Length > 1500);
        Assert.Equal("%PDF-", Encoding.ASCII.GetString(diplomaBytes, 0, 5));
    }

    [Fact]
    public void SvgShapeHelper_AllShapeTypes_GenerateValidSvgPaths()
    {
        foreach (ShapeType type in Enum.GetValues<ShapeType>())
        {
            string path = SvgShapeHelper.GetVectorPath(type, 200, 150, 8, "M 0,0 L 50,50 Z");
            Assert.False(string.IsNullOrWhiteSpace(path), $"Path for {type} should not be empty");
            Assert.StartsWith("M", path);

            var element = new PdfShapeElement
            {
                ShapeType = type,
                Width = 200,
                Height = 150,
                FillColorHex = "#990000",
                StrokeColorHex = "#F59E0B",
                StrokeThickness = 2.0,
                CustomPathData = type == ShapeType.CustomSvgPath ? "M 0,0 L 100,0 L 50,100 Z" : null
            };

            string svg = SvgShapeHelper.GenerateSvgMarkup(element);
            Assert.Contains("<svg", svg);
            Assert.Contains("viewBox", svg);
            Assert.Contains("</svg>", svg);
        }
    }

    [Fact]
    public async Task ShapeElement_CustomPathDataAndColors_PersistCorrectly()
    {
        var model = new PdfDocumentModel { Title = "ShapePersistenceTest.pdf" };
        var page = new PdfPageModel { Width = 800, Height = 600 };
        page.Elements.Add(new PdfShapeElement
        {
            ShapeType = ShapeType.CustomSvgPath,
            CustomPathData = "M 0,0 L 220,0 L 160,380 Z",
            FillColorHex = "#990000",
            StrokeColorHex = "#F59E0B",
            StrokeThickness = 2.5,
            SecondaryFillColorHex = "#D97706",
            SecondaryStrokeColorHex = "#B45309",
            X = 50,
            Y = 50,
            Width = 220,
            Height = 380
        });
        model.Pages.Add(page);

        string tempPath = Path.Combine(Path.GetTempPath(), $"shape_test_{Guid.NewGuid():N}.frypdf");
        try
        {
            await _persistenceService.SaveProjectAsync(model, tempPath);
            var loaded = await _persistenceService.LoadProjectAsync(tempPath);

            Assert.NotNull(loaded);
            Assert.Single(loaded.Pages);
            var loadedShape = Assert.IsType<PdfShapeElement>(loaded.Pages[0].Elements[0]);
            Assert.Equal(ShapeType.CustomSvgPath, loadedShape.ShapeType);
            Assert.Equal("M 0,0 L 220,0 L 160,380 Z", loadedShape.CustomPathData);
            Assert.Equal("#990000", loadedShape.FillColorHex);
            Assert.Equal("#F59E0B", loadedShape.StrokeColorHex);
            Assert.Equal("#D97706", loadedShape.SecondaryFillColorHex);
            Assert.Equal("#B45309", loadedShape.SecondaryStrokeColorHex);
        }
        finally
        {
            if (File.Exists(tempPath)) File.Delete(tempPath);
        }
    }

    [Fact]
    public void MainViewModel_CertificateQuickInsertionCommands_FunctionProperly()
    {
        var vm = new MainViewModel();

        Assert.NotNull(vm.CurrentPage);
        int initialCount = vm.CurrentPage.Elements.Count;

        vm.AddMedalBadgeElement();
        Assert.Equal(initialCount + 1, vm.CurrentPage.Elements.Count);
        Assert.IsType<ShapeElementViewModel>(vm.CurrentPage.Elements.Last());

        vm.AddLaurelSealElement();
        Assert.Equal(initialCount + 2, vm.CurrentPage.Elements.Count);

        vm.AddRibbonBannerElement();
        Assert.Equal(initialCount + 3, vm.CurrentPage.Elements.Count);

        vm.AddCornerAccentElement("TopLeft");
        Assert.Equal(initialCount + 4, vm.CurrentPage.Elements.Count);

        vm.AddCornerAccentElement("BottomRight");
        Assert.Equal(initialCount + 5, vm.CurrentPage.Elements.Count);

        vm.AddSignatureBlock();
        Assert.True(vm.CurrentPage.Elements.Count >= initialCount + 9);

        vm.AddDateBlock();
        Assert.True(vm.CurrentPage.Elements.Count >= initialCount + 12);
    }

    [Fact]
    public void WeddingInvitationTraditionalTemplate_MatchesReferenceAndGeneratesValidPdfBytes()
    {
        var doc = WeddingInvitationTraditionalTemplate.GenerateDocument();
        Assert.NotNull(doc);
        Assert.Single(doc.Pages);

        var page = doc.Pages[0];
        Assert.Equal(600, page.Width);
        Assert.Equal(900, page.Height);

        // Assert presence of key ceremonial elements
        Assert.Contains(page.Elements, e => e is PdfSvgElement svg && svg.PresetName == "MarigoldToran");
        Assert.Contains(page.Elements, e => e is PdfSvgElement svg && svg.PresetName == "GaneshaCrest");
        Assert.Contains(page.Elements, e => e is PdfSvgElement svg && svg.PresetName == "DottedFloralDivider");
        Assert.Contains(page.Elements, e => e is PdfSvgElement svg && svg.PresetName == "TraditionalDeepam");
        Assert.Contains(page.Elements, e => e is PdfSvgElement svg && svg.PresetName == "PlantainTrees");
        Assert.Contains(page.Elements, e => e is PdfTextElement txt && txt.Text.Contains("Shree Ganeshay Namah"));
        Assert.Contains(page.Elements, e => e is PdfTextElement txt && txt.Text == "MUHURTHAM");
        Assert.Contains(page.Elements, e => e is PdfTextElement txt && txt.Text == "RECEPTION");

        // Export to PDF
        byte[] pdfBytes = _exportService.GeneratePdfBytes(doc);
        Assert.NotNull(pdfBytes);
        Assert.True(pdfBytes.Length > 2000);

        string header = System.Text.Encoding.ASCII.GetString(pdfBytes.Take(5).ToArray());
        Assert.Equal("%PDF-", header);
    }

    [Fact]
    public void WeddingInvitationRoyalFloralTemplate_GeneratesValidPdfBytes()
    {
        var doc = WeddingInvitationRoyalFloralTemplate.GenerateDocument();
        Assert.NotNull(doc);
        Assert.Single(doc.Pages);

        var page = doc.Pages[0];
        Assert.Contains(page.Elements, e => e is PdfSvgElement svg && svg.PresetName == "BotanicalWreath");
        Assert.Contains(page.Elements, e => e is PdfQrCodeElement qr && qr.Label.Contains("RSVP"));

        byte[] pdfBytes = _exportService.GeneratePdfBytes(doc);
        Assert.NotNull(pdfBytes);
        Assert.True(pdfBytes.Length > 2000);
        Assert.Equal("%PDF-", System.Text.Encoding.ASCII.GetString(pdfBytes.Take(5).ToArray()));
    }

    [Fact]
    public void GalaInvitationTemplate_GeneratesValidPdfBytes()
    {
        var doc = GalaInvitationTemplate.GenerateDocument();
        Assert.NotNull(doc);
        Assert.Single(doc.Pages);

        var page = doc.Pages[0];
        Assert.Contains(page.Elements, e => e is PdfSvgElement svg && svg.PresetName == "ArtDecoFrame");
        Assert.Contains(page.Elements, e => e is PdfQrCodeElement qr && qr.Label.Contains("VIP"));

        byte[] pdfBytes = _exportService.GeneratePdfBytes(doc);
        Assert.NotNull(pdfBytes);
        Assert.True(pdfBytes.Length > 2000);
        Assert.Equal("%PDF-", System.Text.Encoding.ASCII.GetString(pdfBytes.Take(5).ToArray()));
    }

    [Fact]
    public void SvgOrnamentLibrary_AllPresets_GenerateValidSvgMarkup()
    {
        Assert.True(SvgOrnamentLibrary.Presets.Count >= 10);

        foreach (var (name, svg) in SvgOrnamentLibrary.Presets)
        {
            Assert.False(string.IsNullOrWhiteSpace(svg), $"Preset {name} is empty");
            Assert.StartsWith("<svg", svg.TrimStart());
            Assert.EndsWith("</svg>", svg.TrimEnd());
            Assert.Contains("viewBox", svg);

            // Test tinting
            string tinted = SvgOrnamentLibrary.GetSvg(name, "#FF5500");
            Assert.NotNull(tinted);
        }
    }

    [Fact]
    public async Task SvgElement_PersistenceAndViewModelRoundTrip_Succeeds()
    {
        var original = new PdfSvgElement
        {
            X = 50,
            Y = 100,
            Width = 180,
            Height = 180,
            PresetName = "GaneshaCrest",
            SvgSource = SvgOrnamentLibrary.GetGaneshaCrestSvg(),
            TintColorHex = "#B45309",
            KeepAspectRatio = true
        };

        var vm = new SvgElementViewModel();
        vm.LoadFromModel(original);

        Assert.Equal(50, vm.X);
        Assert.Equal(100, vm.Y);
        Assert.Equal(180, vm.Width);
        Assert.Equal("GaneshaCrest", vm.PresetName);
        Assert.Equal("#B45309", vm.TintColorHex);
        Assert.False(string.IsNullOrEmpty(vm.PathGeometryData));

        var exportedModel = Assert.IsType<PdfSvgElement>(vm.ToModel());
        Assert.Equal(original.PresetName, exportedModel.PresetName);
        Assert.Equal(original.TintColorHex, exportedModel.TintColorHex);

        var doc = new PdfDocumentModel();
        var page = new PdfPageModel();
        page.Elements.Add(exportedModel);
        doc.Pages.Add(page);

        string tempPath = Path.Combine(Path.GetTempPath(), $"svg_test_{Guid.NewGuid():N}.frypdf");
        try
        {
            await _persistenceService.SaveProjectAsync(doc, tempPath);
            var loaded = await _persistenceService.LoadProjectAsync(tempPath);
            Assert.NotNull(loaded);
            var loadedSvg = Assert.IsType<PdfSvgElement>(loaded.Pages[0].Elements[0]);
            Assert.Equal("GaneshaCrest", loadedSvg.PresetName);
            Assert.Equal("#B45309", loadedSvg.TintColorHex);
        }
        finally
        {
            if (File.Exists(tempPath)) File.Delete(tempPath);
        }
    }

    [Fact]
    public void MainViewModel_SvgQuickInsertionCommands_FunctionProperly()
    {
        var vm = new MainViewModel();
        Assert.NotNull(vm.CurrentPage);
        int initialCount = vm.CurrentPage.Elements.Count;

        vm.AddSvgElement("GaneshaCrest");
        Assert.Equal(initialCount + 1, vm.CurrentPage.Elements.Count);
        var svgEl = Assert.IsType<SvgElementViewModel>(vm.CurrentPage.Elements.Last());
        Assert.Equal("GaneshaCrest", svgEl.PresetName);

        vm.AddOrnamentElement("MarigoldToran");
        Assert.Equal(initialCount + 2, vm.CurrentPage.Elements.Count);

        vm.AddOrnamentElement("BotanicalWreath");
        Assert.Equal(initialCount + 3, vm.CurrentPage.Elements.Count);
    }
}



