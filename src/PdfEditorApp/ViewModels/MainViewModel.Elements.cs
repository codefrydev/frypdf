using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Avalonia.Platform.Storage;
using CommunityToolkit.Mvvm.Input;
using PdfEditorApp.Core.Models;
using PdfEditorApp.Core.Models.Elements;
using PdfEditorApp.Models;
using PdfEditorApp.Services;
using PdfEditorApp.Services.MathEngine;
using PdfEditorApp.ViewModels.ElementViewModels;

namespace PdfEditorApp.ViewModels;

public partial class MainViewModel
{
    // --- ELEMENT UNDO/REDO WRAPPER ---

    public void AddElementWithUndo(ElementViewModelBase element, string description = "")
    {
        if (CurrentPage == null) return;
        var page = CurrentPage;
        page.AddElement(element);
        string desc = string.IsNullOrEmpty(description) ? $"Add {element.DisplayName}" : description;

        UndoRedo.RecordAction(
            desc,
            () => page.RemoveElement(element),
            () => page.AddElement(element)
        );

        ShowToast(desc, "PlusCircleOutline");
    }

    public IReadOnlyList<PdfEditorApp.Core.Plugins.Descriptors.CanvasElementDescriptor> InsertableElements => _elementService.GetInsertableElements();

    [RelayCommand]
    public void InsertCanvasElement(string elementTypeId)
    {
        if (CurrentPage == null) return;
        var descriptor = _elementService.GetDescriptor(elementTypeId);
        if (descriptor == null) return;

        var vm = _elementService.CreateViewModel(elementTypeId);
        var (posX, posY) = _placementService.GetPlacementPosition(CurrentPage, descriptor.DefaultWidth, descriptor.DefaultHeight);
        vm.X = posX;
        vm.Y = posY;
        vm.Width = descriptor.DefaultWidth;
        vm.Height = descriptor.DefaultHeight;

        AddElementWithUndo(vm, $"Added {descriptor.DisplayName}");
        CurrentPage.SelectedElement = vm;
        Inspector.SelectedElement = vm;
    }

    // --- ELEMENT CREATION COMMANDS ---

    [RelayCommand]
    public void AddTextElement()
    {
        if (CurrentPage == null) return;

        var (posX, posY) = _placementService.GetPlacementPosition(CurrentPage, 400, 80);
        var textEl = new TextElementViewModel
        {
            X = posX,
            Y = posY,
            Width = 400,
            Height = 80,
            Text = "New editable paragraph. Double-click or use inspector to customize text, fonts, colors, and alignments.",
            FontSize = 13,
            TextColorHex = "#201F1E"
        };

        AddElementWithUndo(textEl, "Added Text Paragraph");
    }

    [RelayCommand]
    public void AddHeadingElement()
    {
        if (CurrentPage == null) return;

        var (posX, posY) = _placementService.GetPlacementPosition(CurrentPage, 500, 45);
        var headingEl = new TextElementViewModel
        {
            X = posX,
            Y = posY,
            Width = 500,
            Height = 45,
            Text = "Section Heading",
            FontSize = 22,
            FontFamily = "Georgia",
            IsBold = true,
            TextColorHex = "#111827"
        };

        AddElementWithUndo(headingEl, "Added Section Heading");
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

        double width = shapeType == ShapeType.Circle ? 120 : 240;
        double height = 120;
        var (posX, posY) = _placementService.GetPlacementPosition(CurrentPage, width, height);

        var shapeEl = new ShapeElementViewModel
        {
            X = posX,
            Y = posY,
            Width = width,
            Height = height,
            ShapeType = shapeType,
            FillColorHex = "#F0F7FD",
            StrokeColorHex = "#0F6CBD",
            StrokeThickness = 1.5,
            CornerRadius = shapeType == ShapeType.Circle ? 60 : (shapeType == ShapeType.RoundedRectangle ? 16 : 6)
        };

        AddElementWithUndo(shapeEl, $"Added Shape ({shapeType})");
    }

    [RelayCommand]
    public void AddMedalBadgeElement()
    {
        if (CurrentPage == null) return;
        var (posX, posY) = _placementService.GetPlacementPosition(CurrentPage, 140, 170);

        var badgeEl = new ShapeElementViewModel
        {
            X = posX,
            Y = posY,
            Width = 140,
            Height = 170,
            ShapeType = ShapeType.MedalRibbonBadge,
            FillColorHex = "#F59E0B",
            StrokeColorHex = "#B45309",
            StrokeThickness = 2.5,
            SecondaryFillColorHex = "#990000",
            ZIndex = 5
        };

        AddElementWithUndo(badgeEl, "Added Medal Ribbon Badge");
    }

    [RelayCommand]
    public void AddLaurelSealElement()
    {
        if (CurrentPage == null) return;
        var (posX, posY) = _placementService.GetPlacementPosition(CurrentPage, 110, 110);

        var sealEl = new ShapeElementViewModel
        {
            X = posX,
            Y = posY,
            Width = 110,
            Height = 110,
            ShapeType = ShapeType.LaurelWreathSeal,
            FillColorHex = "#FEF3C7",
            StrokeColorHex = "#B45309",
            StrokeThickness = 2.0,
            Label = "OFFICIAL",
            LabelColorHex = "#92400E",
            LabelFontSize = 10,
            ZIndex = 5
        };

        AddElementWithUndo(sealEl, "Added Laurel Gold Seal");
    }

    [RelayCommand]
    public void AddRibbonBannerElement()
    {
        if (CurrentPage == null) return;
        var (posX, posY) = _placementService.GetPlacementPosition(CurrentPage, 320, 50);

        var bannerEl = new ShapeElementViewModel
        {
            X = posX,
            Y = posY,
            Width = 320,
            Height = 50,
            ShapeType = ShapeType.RibbonBanner,
            FillColorHex = "#990000",
            StrokeColorHex = "#F59E0B",
            StrokeThickness = 1.5,
            Label = "AWARD OF EXCELLENCE",
            LabelColorHex = "#FFFFFF",
            LabelFontSize = 13,
            ZIndex = 5
        };

        AddElementWithUndo(bannerEl, "Added Ribbon Banner");
    }

    [RelayCommand]
    public void AddCornerAccentElement(string? corner = "TopLeft")
    {
        if (CurrentPage == null) return;

        bool isBottomRight = string.Equals(corner, "BottomRight", StringComparison.OrdinalIgnoreCase);
        double w = 240;
        double h = 240;
        double posX = isBottomRight ? Math.Max(0, CurrentPage.Width - w) : 0;
        double posY = isBottomRight ? Math.Max(0, CurrentPage.Height - h) : 0;

        var cornerEl = new ShapeElementViewModel
        {
            X = posX,
            Y = posY,
            Width = w,
            Height = h,
            ShapeType = isBottomRight ? ShapeType.CornerPolygonalAccentBottomRight : ShapeType.CornerPolygonalAccentTopLeft,
            FillColorHex = "#990000",
            StrokeColorHex = "#F59E0B",
            StrokeThickness = 2.0,
            ZIndex = 1
        };

        AddElementWithUndo(cornerEl, $"Added Corner Accent ({corner})");
    }

    [RelayCommand]
    public void AddSignatureBlock()
    {
        if (CurrentPage == null) return;
        var (posX, posY) = _placementService.GetPlacementPosition(CurrentPage, 260, 110);

        var scriptEl = new TextElementViewModel
        {
            X = posX,
            Y = posY,
            Width = 260,
            Height = 45,
            Text = "John Smith",
            FontSize = 28,
            FontFamily = "Great Vibes",
            IsItalic = true,
            TextColorHex = "#1E293B",
            Alignment = TextAlignmentMode.Center,
            ZIndex = 5
        };

        var lineEl = new DividerElementViewModel
        {
            X = posX,
            Y = posY + 50,
            Width = 260,
            Thickness = 1.5,
            ColorHex = "#1E293B",
            ZIndex = 5
        };

        var nameEl = new TextElementViewModel
        {
            X = posX,
            Y = posY + 58,
            Width = 260,
            Height = 24,
            Text = "Mr. John Smith",
            FontSize = 15,
            FontFamily = "Inter",
            IsBold = true,
            TextColorHex = "#1E293B",
            Alignment = TextAlignmentMode.Center,
            ZIndex = 5
        };

        var titleEl = new TextElementViewModel
        {
            X = posX,
            Y = posY + 82,
            Width = 260,
            Height = 22,
            Text = "President & Dean",
            FontSize = 13,
            FontFamily = "Inter",
            TextColorHex = "#64748B",
            Alignment = TextAlignmentMode.Center,
            ZIndex = 5
        };

        AddElementWithUndo(scriptEl, "Added Signature Block");
        if (CurrentPage != null)
        {
            CurrentPage.AddElement(lineEl);
            CurrentPage.AddElement(nameEl);
            CurrentPage.AddElement(titleEl);
        }
    }

    [RelayCommand]
    public void AddDateBlock()
    {
        if (CurrentPage == null) return;
        var (posX, posY) = _placementService.GetPlacementPosition(CurrentPage, 240, 70);

        var dateEl = new TextElementViewModel
        {
            X = posX,
            Y = posY,
            Width = 240,
            Height = 28,
            Text = DateTime.Now.ToString("MMMM dd, yyyy"),
            FontSize = 15,
            FontFamily = "Inter",
            IsBold = true,
            TextColorHex = "#1E293B",
            Alignment = TextAlignmentMode.Center,
            ZIndex = 5
        };

        var lineEl = new DividerElementViewModel
        {
            X = posX,
            Y = posY + 32,
            Width = 240,
            Thickness = 1.5,
            ColorHex = "#1E293B",
            ZIndex = 5
        };

        var labelEl = new TextElementViewModel
        {
            X = posX,
            Y = posY + 40,
            Width = 240,
            Height = 24,
            Text = "Date of Issuance",
            FontSize = 14,
            FontFamily = "Inter",
            IsBold = true,
            TextColorHex = "#1E293B",
            Alignment = TextAlignmentMode.Center,
            ZIndex = 5
        };

        AddElementWithUndo(dateEl, "Added Date Block");
        if (CurrentPage != null)
        {
            CurrentPage.AddElement(lineEl);
            CurrentPage.AddElement(labelEl);
        }
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
                    var (posX, posY) = _placementService.GetPlacementPosition(CurrentPage, 260, 180);
                    var imgEl = new ImageElementViewModel
                    {
                        X = posX,
                        Y = posY,
                        Width = 260,
                        Height = 180,
                        ImagePath = filePath
                    };

                    AddElementWithUndo(imgEl, $"Inserted Image: {Path.GetFileName(filePath)}");
                    return;
                }
            }

            // Fallback placeholder image element
            var (fallbackX, fallbackY) = _placementService.GetPlacementPosition(CurrentPage, 260, 180);
            var fallbackImg = new ImageElementViewModel
            {
                X = fallbackX,
                Y = fallbackY,
                Width = 260,
                Height = 180,
                AltText = "Inserted Graphic"
            };
            AddElementWithUndo(fallbackImg, "Inserted Image Placeholder");
        }
        catch (Exception ex)
        {
            ShowToast($"Image error: {ex.Message}", "AlertCircleOutline");
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

        var (posX, posY) = _placementService.GetPlacementPosition(CurrentPage, 180, 60);
        var stampEl = new ShapeElementViewModel
        {
            X = posX,
            Y = posY,
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

        AddElementWithUndo(stampEl, $"Added Stamp ({label})");
    }

    [RelayCommand]
    public void AddStickyNoteElement()
    {
        if (CurrentPage == null) return;

        var (posX, posY) = _placementService.GetPlacementPosition(CurrentPage, 200, 150);
        var noteEl = new StickyNoteElementViewModel
        {
            X = posX,
            Y = posY,
            Width = 200,
            Height = 150,
            Author = "Lead Reviewer",
            NoteText = "Please verify financial data and audit metrics prior to final executive sign-off.",
            Status = "Pending Review",
            ColorHex = "#FEF3C7",
            BorderColorHex = "#F59E0B"
        };

        AddElementWithUndo(noteEl, "Added Sticky Note");
        RefreshComments();
    }

    [RelayCommand]
    public void AddDividerElement()
    {
        if (CurrentPage == null) return;

        double width = Math.Max(200, CurrentPage.Width - 80);
        var (posX, posY) = _placementService.GetPlacementPosition(CurrentPage, width, 16);
        var divEl = new DividerElementViewModel
        {
            X = posX,
            Y = posY,
            Width = width,
            Height = 3,
            Thickness = 2,
            ColorHex = "#0F6CBD"
        };

        AddElementWithUndo(divEl, "Added Divider Line");
    }

    [RelayCommand]
    public void AddWaveDividerElement()
    {
        if (CurrentPage == null) return;
        double width = Math.Max(200, CurrentPage.Width - 80);
        var (posX, posY) = _placementService.GetPlacementPosition(CurrentPage, width, 24);
        var divEl = new DividerElementViewModel
        {
            X = posX,
            Y = posY,
            Width = width,
            Height = 24,
            Thickness = 2.0,
            Style = DividerStyle.Wave,
            WaveAmplitude = 8.0,
            WaveFrequency = 5.0,
            ColorHex = "#2563EB"
        };
        AddElementWithUndo(divEl, "Added Wave Divider");
    }

    [RelayCommand]
    public void AddFlourishDividerElement()
    {
        if (CurrentPage == null) return;
        double width = Math.Max(200, CurrentPage.Width - 80);
        var (posX, posY) = _placementService.GetPlacementPosition(CurrentPage, width, 24);
        var divEl = new DividerElementViewModel
        {
            X = posX,
            Y = posY,
            Width = width,
            Height = 24,
            Thickness = 1.8,
            Style = DividerStyle.CalligraphicFlourish,
            WaveAmplitude = 6.0,
            ColorHex = "#D97706"
        };
        AddElementWithUndo(divEl, "Added Calligraphic Flourish Divider");
    }

    [RelayCommand]
    public void AddBezierCurveElement()
    {
        if (CurrentPage == null) return;
        var (posX, posY) = _placementService.GetPlacementPosition(CurrentPage, 220, 90);
        var curveEl = new ShapeElementViewModel
        {
            X = posX,
            Y = posY,
            Width = 220,
            Height = 90,
            ShapeType = ShapeType.BezierCurve,
            FillColorHex = "#00000000",
            StrokeColorHex = "#2563EB",
            StrokeThickness = 2.5,
            StartCap = LineEndCap.None,
            EndCap = LineEndCap.None
        };
        AddElementWithUndo(curveEl, "Added Bézier Curve Line");
    }

    [RelayCommand]
    public void AddCurvedArrowElement()
    {
        if (CurrentPage == null) return;
        var (posX, posY) = _placementService.GetPlacementPosition(CurrentPage, 220, 90);
        var arrowEl = new ShapeElementViewModel
        {
            X = posX,
            Y = posY,
            Width = 220,
            Height = 90,
            ShapeType = ShapeType.CurvedArrow,
            FillColorHex = "#00000000",
            StrokeColorHex = "#7C3AED",
            StrokeThickness = 2.5,
            EndCap = LineEndCap.Arrow
        };
        AddElementWithUndo(arrowEl, "Added Curved Arrow");
    }

    [RelayCommand]
    public void AddCurlyBraceElement()
    {
        if (CurrentPage == null) return;
        var (posX, posY) = _placementService.GetPlacementPosition(CurrentPage, 40, 160);
        var braceEl = new ShapeElementViewModel
        {
            X = posX,
            Y = posY,
            Width = 40,
            Height = 160,
            ShapeType = ShapeType.CurlyBrace,
            FillColorHex = "#00000000",
            StrokeColorHex = "#0F6CBD",
            StrokeThickness = 2.0
        };
        AddElementWithUndo(braceEl, "Added Calligraphic Curly Brace");
    }

    [RelayCommand]
    public void AddTableElement()
    {
        if (CurrentPage == null) return;

        double width = Math.Max(300, CurrentPage.Width - 80);
        var (posX, posY) = _placementService.GetPlacementPosition(CurrentPage, width, 180);
        var tableEl = new TableElementViewModel
        {
            X = posX,
            Y = posY,
            Width = width,
            Height = 180
        };

        AddElementWithUndo(tableEl, "Added Table");
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

        var (posX, posY) = _placementService.GetPlacementPosition(CurrentPage, 400, 220);
        var chartEl = new ChartElementViewModel
        {
            X = posX,
            Y = posY,
            Width = 400,
            Height = 220,
            ChartType = chartType,
            Title = $"{chartType} Chart Analysis"
        };

        AddElementWithUndo(chartEl, $"Added {chartType} Chart");
    }

    [RelayCommand]
    public void OpenDataStudio(string? mode = "NewChart")
    {
        DataStudio.StorageProvider = StorageProvider;
        if (Inspector.ChartElement != null)
        {
            DataStudio.OpenForChart(Inspector.ChartElement, CurrentPage);
        }
        else if (Inspector.TableElement != null)
        {
            DataStudio.OpenForTable(Inspector.TableElement, CurrentPage);
        }
        else
        {
            DataStudio.OpenForNew(mode ?? "NewChart", CurrentPage);
        }
    }

    [RelayCommand]
    public void OpenBatchGeneration(string? preset = null)
    {
        BatchGeneration.StorageProvider = StorageProvider;
        var doc = (IsEditorVisible && Pages.Count > 0) ? ToDocumentModel() : _templateService.CreateEmployeePayslipTemplate();
        BatchGeneration.OpenWithDocument(doc);
        if (preset == "payslip")
        {
            BatchGeneration.LoadDefaultSamplePayslipDataset();
        }
    }

    [RelayCommand]
    public async Task ImportExcelToChartAsync()
    {
        DataStudio.StorageProvider = StorageProvider;
        DataStudio.OpenForNew("NewChart", CurrentPage);
        DataStudio.SelectedTabIndex = 0;
        await DataStudio.BrowseFileAsync();
    }

    [RelayCommand]
    public async Task ImportExcelToTableAsync()
    {
        DataStudio.StorageProvider = StorageProvider;
        DataStudio.OpenForNew("NewTable", CurrentPage);
        DataStudio.SelectedTabIndex = 0;
        await DataStudio.BrowseFileAsync();
    }

    [RelayCommand]
    public void FetchRestApiToChart()
    {
        DataStudio.StorageProvider = StorageProvider;
        DataStudio.OpenForNew("NewChart", CurrentPage);
        DataStudio.SelectedTabIndex = 1;
    }

    [RelayCommand]
    public void FetchRestApiToTable()
    {
        DataStudio.StorageProvider = StorageProvider;
        DataStudio.OpenForNew("NewTable", CurrentPage);
        DataStudio.SelectedTabIndex = 1;
    }

    [RelayCommand]
    public void AddWatermarkElement()
    {
        if (CurrentPage == null) return;

        var (posX, posY) = _placementService.GetPlacementPosition(CurrentPage, 400, 100);
        var wmEl = new WatermarkElementViewModel
        {
            X = posX,
            Y = posY,
            Text = "CONFIDENTIAL",
            FontSize = 56,
            ColorHex = "#DC2626",
            Opacity = 0.15,
            Angle = -35
        };

        AddElementWithUndo(wmEl, "Added Watermark Overlay");
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

        double width = fieldType == FormFieldType.Checkbox ? 180 : (fieldType == FormFieldType.Signature ? 260 : 340);
        double height = fieldType == FormFieldType.Signature ? 90 : (fieldType == FormFieldType.MultilineText ? 80 : 42);
        var (posX, posY) = _placementService.GetPlacementPosition(CurrentPage, width, height);

        var formEl = new FormFieldElementViewModel
        {
            X = posX,
            Y = posY,
            Width = width,
            Height = height,
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

        AddElementWithUndo(formEl, $"Added Form Field ({fieldType})");
    }

    [RelayCommand]
    public void AddQrCodeElement()
    {
        if (CurrentPage == null) return;

        var (posX, posY) = _placementService.GetPlacementPosition(CurrentPage, 130, 150);
        var qrEl = new QrCodeElementViewModel
        {
            X = posX,
            Y = posY,
            Width = 130,
            Height = 150,
            Content = "https://github.com/PrashantUnity/PDFCreator",
            Label = "SCAN TO VERIFY CREDENTIAL"
        };

        AddElementWithUndo(qrEl, "Added Vector QR Code");
    }

    [RelayCommand]
    public void AddBarcodeElement()
    {
        if (CurrentPage == null) return;

        var (posX, posY) = _placementService.GetPlacementPosition(CurrentPage, 240, 65);
        var barcodeEl = new BarcodeElementViewModel
        {
            X = posX,
            Y = posY,
            Width = 240,
            Height = 65,
            CodeValue = $"DOC-2026-{Random.Shared.Next(100000, 999999)}"
        };

        AddElementWithUndo(barcodeEl, "Added Barcode");
    }

    [RelayCommand]
    public void AddRedactionElement(string? exemptionCode = "[REDACTED - (b)(4) PRIVILEGED]")
    {
        if (CurrentPage == null) return;

        var (posX, posY) = _placementService.GetPlacementPosition(CurrentPage, 320, 36);
        var redEl = new RedactionElementViewModel
        {
            X = posX,
            Y = posY,
            Width = 320,
            Height = 36,
            ExemptionCode = exemptionCode ?? "[REDACTED]"
        };

        AddElementWithUndo(redEl, "Added Redaction Box");
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

        double height = isHighlighter ? 24 : 12;
        var (posX, posY) = _placementService.GetPlacementPosition(CurrentPage, 260, height);

        var inkEl = new InkElementViewModel
        {
            X = posX,
            Y = posY,
            Width = 260,
            Height = height,
            StrokeColorHex = isHighlighter ? "#FEF08A" : "#0F6CBD",
            StrokeThickness = isHighlighter ? 14.0 : 3.0,
            Opacity = isHighlighter ? 0.45 : 1.0,
            IsHighlighter = isHighlighter
        };

        AddElementWithUndo(inkEl, isHighlighter ? "Added Highlighter Stroke" : "Added Freehand Ink Stroke");
    }

    [RelayCommand]
    public void AddSvgElement(string? presetName = null)
    {
        if (CurrentPage == null) return;

        string preset = string.IsNullOrWhiteSpace(presetName) ? "GaneshaCrest" : presetName;
        double w = preset switch
        {
            "MarigoldToran" => 500,
            "DottedFloralDivider" => 400,
            "MandapArch" => 480,
            "PlantainTrees" => 90,
            "TraditionalDeepam" => 60,
            _ => 150
        };
        double h = preset switch
        {
            "MarigoldToran" => 75,
            "DottedFloralDivider" => 25,
            "MandapArch" => 55,
            "PlantainTrees" => 180,
            "TraditionalDeepam" => 140,
            _ => 150
        };

        var (posX, posY) = _placementService.GetPlacementPosition(CurrentPage, w, h);
        var svgEl = new SvgElementViewModel
        {
            X = posX,
            Y = posY,
            Width = w,
            Height = h,
            PresetName = preset,
            SvgSource = SvgOrnamentLibrary.GetSvg(preset)
        };

        AddElementWithUndo(svgEl, $"Added Vector Ornament ({preset})");
    }

    [RelayCommand]
    public void AddOrnamentElement(string ornamentName)
    {
        AddSvgElement(ornamentName);
    }

    // --- MATHEMATICAL EQUATIONS & FORMULA STUDIO ---

    [RelayCommand]
    public void AddMathElement(string? presetId = null)
    {
        if (CurrentPage == null) return;

        var mathEl = new MathElementViewModel();

        if (!string.IsNullOrEmpty(presetId))
        {
            var preset = MathPresetsLibrary.FindById(presetId) ?? MathPresetsLibrary.FindByName(presetId);
            if (preset != null)
            {
                mathEl.Formula = preset.Formula;
                mathEl.PresetName = preset.Name;
                mathEl.Description = preset.Description;
                mathEl.Category = preset.Category;
                mathEl.EquationNumber = preset.DefaultEquationNumber;
                mathEl.Width = preset.DefaultWidth;
                mathEl.Height = preset.DefaultHeight;
            }
        }

        var (posX, posY) = _placementService.GetPlacementPosition(CurrentPage, mathEl.Width, mathEl.Height);
        mathEl.X = posX;
        mathEl.Y = posY;
        mathEl.RenderSvg();

        AddElementWithUndo(mathEl, $"Added Equation: {mathEl.DisplayName}");
    }

    [RelayCommand]
    public void OpenMathStudio(MathElementViewModel? target = null)
    {
        EditingMathElement = target;
        if (target != null)
        {
            MathStudioFormula = target.Formula;
            MathStudioPresetName = target.PresetName ?? "Custom Formula";
            MathStudioEquationNumber = target.EquationNumber;
            MathStudioShowNumber = target.ShowEquationNumber;
            MathStudioCategory = target.Category;
        }
        else
        {
            MathStudioFormula = @"\int_{-\infty}^{\infty} e^{-x^2} \, dx = \sqrt{\pi}";
            MathStudioPresetName = "Gaussian Integral";
            MathStudioEquationNumber = "(1)";
            MathStudioShowNumber = false;
            MathStudioCategory = MathCategory.Calculus;
        }

        UpdateMathStudioPreview();
        IsMathStudioOpen = true;
        OpenRegisteredDialog("frypdf.dialog.math");
    }

    [RelayCommand]
    public void CloseMathStudio()
    {
        IsMathStudioOpen = false;
        EditingMathElement = null;
        CloseDynamicDialog();
    }

    [RelayCommand]
    public void ApplyMathStudioEquation()
    {
        if (EditingMathElement != null)
        {
            var el = EditingMathElement;
            string oldFormula = el.Formula;
            string newFormula = MathStudioFormula;
            bool oldNum = el.ShowEquationNumber;
            bool newNum = MathStudioShowNumber;
            string oldNumVal = el.EquationNumber;
            string newNumVal = MathStudioEquationNumber;

            el.Formula = newFormula;
            el.ShowEquationNumber = newNum;
            el.EquationNumber = newNumVal;
            el.PresetName = MathStudioPresetName;
            el.Category = MathStudioCategory;
            el.RenderSvg();

            UndoRedo.RecordAction(
                "Edit Equation",
                () => { el.Formula = oldFormula; el.ShowEquationNumber = oldNum; el.EquationNumber = oldNumVal; el.RenderSvg(); },
                () => { el.Formula = newFormula; el.ShowEquationNumber = newNum; el.EquationNumber = newNumVal; el.RenderSvg(); }
            );

            ShowToast("Equation Updated", "Sigma");
        }
        else if (CurrentPage != null)
        {
            var (posX, posY) = _placementService.GetPlacementPosition(CurrentPage, 320, 60);
            var newEl = new MathElementViewModel
            {
                X = posX,
                Y = posY,
                Width = 320,
                Height = 60,
                Formula = MathStudioFormula,
                PresetName = MathStudioPresetName,
                ShowEquationNumber = MathStudioShowNumber,
                EquationNumber = MathStudioEquationNumber,
                Category = MathStudioCategory
            };
            newEl.RenderSvg();
            AddElementWithUndo(newEl, "Inserted Math Equation");
        }

        CloseMathStudio();
    }

    [RelayCommand]
    public void InsertMathStudioSymbol(string snippet)
    {
        if (string.IsNullOrEmpty(snippet)) return;
        string resolved = MathPresetsLibrary.ResolveSnippet(snippet);
        MathStudioFormula = string.IsNullOrWhiteSpace(MathStudioFormula) ? resolved : $"{MathStudioFormula} {resolved}";
    }

    [RelayCommand]
    public void ApplyMathStudioPreset(string presetId)
    {
        if (string.IsNullOrEmpty(presetId)) return;
        var preset = MathPresetsLibrary.FindById(presetId) ?? MathPresetsLibrary.FindByName(presetId);
        if (preset != null)
        {
            MathStudioFormula = preset.Formula;
            MathStudioPresetName = preset.Name;
            MathStudioEquationNumber = preset.DefaultEquationNumber;
            MathStudioCategory = preset.Category;
        }
    }

    // --- FILL & SIGN / DIGITAL SIGNATURE STUDIO ---

    [RelayCommand]
    public void OpenSignatureStudio()
    {
        IsSignatureStudioOpen = true;
        OpenRegisteredDialog("frypdf.dialog.signature");
    }

    [RelayCommand]
    public void CloseSignatureStudio()
    {
        IsSignatureStudioOpen = false;
        CloseDynamicDialog();
    }

    [RelayCommand]
    public void PlaceSignatureFromStudio()
    {
        if (CurrentPage == null) return;
        string name = string.IsNullOrWhiteSpace(SignatureSignerName) ? "Jane Doe" : SignatureSignerName.Trim();

        var (posX, posY) = _placementService.GetPlacementPosition(CurrentPage, 260, 55);
        var sigEl = _signatureService.CreateCursiveSignatureElement(name, SelectedSignatureStyle, posX, posY);
        var vm = new TextElementViewModel();
        vm.LoadFromModel(sigEl);

        AddElementWithUndo(vm, $"Placed Signature ({name})");
        CloseSignatureStudio();
    }

    [RelayCommand]
    public void AddDateStamp()
    {
        if (CurrentPage == null) return;
        var (posX, posY) = _placementService.GetPlacementPosition(CurrentPage, 180, 35);
        var dateEl = _signatureService.CreateDateStampElement(posX, posY);
        var vm = new TextElementViewModel();
        vm.LoadFromModel(dateEl);
        AddElementWithUndo(vm, "Added Date Stamp");
    }

    [RelayCommand]
    public void AddInitialsBadge(string? initials = "JD")
    {
        if (CurrentPage == null) return;
        var (posX, posY) = _placementService.GetPlacementPosition(CurrentPage, 55, 55);
        var initEl = _signatureService.CreateInitialsElement(initials ?? "JD", posX, posY);
        var vm = new ShapeElementViewModel();
        vm.LoadFromModel(initEl);
        AddElementWithUndo(vm, $"Added Initials ({initEl.Label})");
    }

    [RelayCommand]
    public void AddCheckmarkBadge()
    {
        if (CurrentPage == null) return;
        var (posX, posY) = _placementService.GetPlacementPosition(CurrentPage, 42, 42);
        var badge = _signatureService.CreateMarkupBadge("✓", "#16A34A", posX, posY);
        var vm = new ShapeElementViewModel();
        vm.LoadFromModel(badge);
        AddElementWithUndo(vm, "Added Checkmark (✓)");
    }

    [RelayCommand]
    public void AddCrossBadge()
    {
        if (CurrentPage == null) return;
        var (posX, posY) = _placementService.GetPlacementPosition(CurrentPage, 42, 42);
        var badge = _signatureService.CreateMarkupBadge("✕", "#DC2626", posX, posY);
        var vm = new ShapeElementViewModel();
        vm.LoadFromModel(badge);
        AddElementWithUndo(vm, "Added Cross Mark (✕)");
    }

    // --- WATERMARK MANAGER ---

    [RelayCommand]
    public void OpenWatermarkManager()
    {
        IsWatermarkManagerOpen = true;
        OpenRegisteredDialog("frypdf.dialog.watermark");
    }

    [RelayCommand]
    public void CloseWatermarkManager()
    {
        IsWatermarkManagerOpen = false;
        CloseDynamicDialog();
    }

    [RelayCommand]
    public void ApplyWatermarkToAllPages()
    {
        string text = string.IsNullOrWhiteSpace(WatermarkPresetText) ? "CONFIDENTIAL" : WatermarkPresetText.Trim();
        foreach (var page in Pages)
        {
            var wm = new WatermarkElementViewModel
            {
                Text = text,
                ColorHex = WatermarkColorHex,
                Opacity = WatermarkOpacity,
                Angle = WatermarkAngle,
                FontSize = 56,
                X = Math.Max(0, (page.Width - 400) / 2),
                Y = Math.Max(0, (page.Height - 100) / 2)
            };

            // Remove existing watermark if any
            var existing = page.Elements.OfType<WatermarkElementViewModel>().ToList();
            foreach (var ex in existing) page.RemoveElement(ex);

            page.AddElement(wm);
        }

        CloseWatermarkManager();
        ShowToast($"Applied watermark '{text}' to all {Pages.Count} pages", "Watermark");
    }

    [RelayCommand]
    public void RemoveAllWatermarks()
    {
        int count = 0;
        foreach (var page in Pages)
        {
            var existing = page.Elements.OfType<WatermarkElementViewModel>().ToList();
            foreach (var ex in existing)
            {
                page.RemoveElement(ex);
                count++;
            }
        }
        CloseWatermarkManager();
        ShowToast($"Removed {count} watermarks from document", "WatermarkOff");
    }

    // --- SEARCH & REDACT (FOIA / PRIVACY) ---

    [RelayCommand]
    public void OpenSearchRedactDialog()
    {
        IsSearchRedactDialogOpen = true;
        OpenRegisteredDialog("frypdf.dialog.searchredact");
    }

    [RelayCommand]
    public void CloseSearchRedactDialog()
    {
        IsSearchRedactDialogOpen = false;
        CloseDynamicDialog();
    }

    [RelayCommand]
    public void ExecuteSearchAndRedact()
    {
        if (string.IsNullOrWhiteSpace(SearchRedactQuery) || CurrentPage == null) return;
        string query = SearchRedactQuery.Trim();
        string exemption = SelectedExemptionCode ?? "[REDACTED]";

        int matchesCount = 0;
        var textElements = CurrentPage.Elements.OfType<TextElementViewModel>().ToList();

        foreach (var textEl in textElements)
        {
            if (textEl.Text.Contains(query, StringComparison.OrdinalIgnoreCase))
            {
                var redEl = new RedactionElementViewModel
                {
                    X = textEl.X,
                    Y = textEl.Y,
                    Width = textEl.Width,
                    Height = textEl.Height,
                    ExemptionCode = exemption
                };
                CurrentPage.AddElement(redEl);
                matchesCount++;
            }
        }

        CloseSearchRedactDialog();
        ShowToast($"Placed {matchesCount} redaction boxes matching '{query}'", "EyeOffOutline");
    }

    [RelayCommand]
    public void BurnInAllRedactions()
    {
        int burned = 0;
        foreach (var page in Pages)
        {
            var redactions = page.Elements.OfType<RedactionElementViewModel>().ToList();
            foreach (var red in redactions)
            {
                red.FillColorHex = "#000000";
                red.BorderColorHex = "#000000";
                red.TextColorHex = "#FFFFFF";
                red.ShowOverlayText = true;
                burned++;
            }
        }
        ShowToast($"Permanently committed & burned in {burned} redactions", "ShieldCheckOutline");
    }

    // --- CUSTOM STAMP CREATOR ---

    [RelayCommand]
    public void OpenCustomStampDialog()
    {
        IsCustomStampDialogOpen = true;
        OpenRegisteredDialog("frypdf.dialog.customstamp");
    }

    [RelayCommand]
    public void CloseCustomStampDialog()
    {
        IsCustomStampDialogOpen = false;
        CloseDynamicDialog();
    }

    [RelayCommand]
    public void PlaceCustomStamp()
    {
        if (CurrentPage == null) return;
        string text = string.IsNullOrWhiteSpace(CustomStampText) ? "RECEIVED" : CustomStampText.Trim().ToUpper();
        string color = string.IsNullOrWhiteSpace(CustomStampColorHex) ? "#0F6CBD" : CustomStampColorHex;

        var (posX, posY) = _placementService.GetPlacementPosition(CurrentPage, 220, 65);
        var stampEl = new ShapeElementViewModel
        {
            X = posX,
            Y = posY,
            Width = 220,
            Height = 65,
            ShapeType = ShapeType.RoundedRectangle,
            CornerRadius = 8,
            FillColorHex = "#FFFFFF",
            StrokeColorHex = color,
            StrokeThickness = 2.5,
            Label = $"{text}\n{DateTime.Now:yyyy-MM-dd HH:mm}",
            LabelColorHex = color,
            LabelFontSize = 13
        };

        AddElementWithUndo(stampEl, $"Placed Custom Stamp ({text})");
        CloseCustomStampDialog();
    }

    // --- PREFLIGHT & HEALTH DIAGNOSTICS ---

    [RelayCommand]
    public async Task OpenPreflightDialogAsync()
    {
        IsPreflightDialogOpen = true;
        OpenRegisteredDialog("frypdf.dialog.preflight");
        IsAuditRunning = true;
        try
        {
            var docModel = ToDocumentModel();
            ActiveAuditReport = await _auditService.RunAuditAsync(docModel);
        }
        finally
        {
            IsAuditRunning = false;
        }
    }

    [RelayCommand]
    public async Task AutoFixPreflightIssuesAsync()
    {
        var docModel = ToDocumentModel();
        int fixedCount = _auditService.AutoFixAllIssues(docModel);

        if (fixedCount > 0)
        {
            for (int pIdx = 0; pIdx < docModel.Pages.Count && pIdx < Pages.Count; pIdx++)
            {
                var pModel = docModel.Pages[pIdx];
                var pVm = Pages[pIdx];

                for (int eIdx = 0; eIdx < pModel.Elements.Count && eIdx < pVm.Elements.Count; eIdx++)
                {
                    var elModel = pModel.Elements[eIdx];
                    var elVm = pVm.Elements[eIdx];

                    if (elModel is PdfTextElement textModel && elVm is TextElementViewModel textVm)
                    {
                        textVm.TextColorHex = textModel.TextColorHex;
                    }
                    else if (elModel is PdfImageElement imgModel && elVm is ImageElementViewModel imgVm)
                    {
                        imgVm.AltText = imgModel.AltText;
                    }
                }
            }

            if (!string.IsNullOrWhiteSpace(docModel.Title)) DocumentTitle = docModel.Title;

            IsAuditRunning = true;
            try
            {
                ActiveAuditReport = await _auditService.RunAuditAsync(docModel);
            }
            finally
            {
                IsAuditRunning = false;
            }
            ShowToast($"Auto-remediated {fixedCount} compliance issues", "CheckCircleOutline");
        }
        else
        {
            ShowToast("No auto-fixable issues detected", "InformationOutline");
        }
    }

    [RelayCommand]
    public void NavigateToPreflightIssue(AuditIssueItem? issue)
    {
        if (issue == null) return;

        int pageIdx = Math.Clamp(issue.PageIndex - 1, 0, Pages.Count - 1);
        if (pageIdx >= 0 && pageIdx < Pages.Count)
        {
            CurrentPage = Pages[pageIdx];

            if (!string.IsNullOrWhiteSpace(issue.ElementId))
            {
                var el = CurrentPage.Elements.FirstOrDefault(e => e.Id == issue.ElementId);
                if (el != null)
                {
                    CurrentPage.SelectElement(el);
                    Inspector.UpdateSelection(el, CurrentPage);
                }
            }

            IsPreflightDialogOpen = false;
            ShowToast($"Jumped to Page {issue.PageIndex}: {issue.Title}", "Magnify");
        }
    }

    [RelayCommand]
    public void ClosePreflightDialog()
    {
        IsPreflightDialogOpen = false;
        CloseDynamicDialog();
    }

    [RelayCommand]
    public async Task ExportPreflightReportAsync()
    {
        if (ActiveAuditReport == null)
        {
            var docModel = ToDocumentModel();
            ActiveAuditReport = await _auditService.RunAuditAsync(docModel);
        }

        var sb = new System.Text.StringBuilder();
        sb.AppendLine($"# PDF Preflight Audit Report: {DocumentTitle}");
        sb.AppendLine($"Generated on: {DateTime.Now:yyyy-MM-dd HH:mm:ss} UTC");
        sb.AppendLine($"Health Score: {ActiveAuditReport.HealthScore}/100 (Grade {ActiveAuditReport.Grade})");
        sb.AppendLine($"Total Pages: {ActiveAuditReport.TotalPages} | Total Elements: {ActiveAuditReport.TotalElements}");
        sb.AppendLine($"Word Count: {ActiveAuditReport.TotalWordCount} | Est. Reading Time: {ActiveAuditReport.ReadingTimeDisplay}");
        sb.AppendLine($"Fonts Used ({ActiveAuditReport.UniqueFontsUsed.Count}): {string.Join(", ", ActiveAuditReport.UniqueFontsUsed)}");
        sb.AppendLine();
        sb.AppendLine("## Audit Findings & Checks");
        foreach (var issue in ActiveAuditReport.Issues)
        {
            sb.AppendLine($"- [{issue.Severity.ToUpper()}] (Page {issue.PageIndex}) {issue.Title}: {issue.Description}");
        }

        string reportText = sb.ToString();
        string reportPath = "";

        if (StorageProvider != null)
        {
            var file = await StorageProvider.SaveFilePickerAsync(new FilePickerSaveOptions
            {
                Title = "Save Preflight Audit Report",
                DefaultExtension = "md",
                SuggestedFileName = $"{System.IO.Path.GetFileNameWithoutExtension(DocumentTitle)}_Audit_Report.md"
            });
            if (file != null)
            {
                reportPath = file.Path.LocalPath;
            }
            else
            {
                UpdateStatus("Export cancelled.");
                return;
            }
        }
        else
        {
            reportPath = System.IO.Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.Desktop), $"{System.IO.Path.GetFileNameWithoutExtension(DocumentTitle)}_Audit_Report.md");
        }

        await System.IO.File.WriteAllTextAsync(reportPath, reportText);
        ShowToast($"Saved audit report to {System.IO.Path.GetFileName(reportPath)}", "FileCheckOutline");
    }

    [RelayCommand]
    public async Task ExportCommentsSummaryAsync()
    {
        var sb = new System.Text.StringBuilder();
        sb.AppendLine($"# Review & Annotations Summary: {DocumentTitle}");
        sb.AppendLine($"Generated: {DateTime.Now:yyyy-MM-dd HH:mm:ss}");
        sb.AppendLine();

        RefreshComments();
        if (CommentItems.Count == 0)
        {
            sb.AppendLine("No comments or sticky notes found in this document.");
        }
        else
        {
            foreach (var comment in CommentItems)
            {
                sb.AppendLine($"### Page {comment.PageIndex} - {comment.Author} ({comment.Status})");
                sb.AppendLine($"> {comment.Text}");
                sb.AppendLine();
            }
        }

        string exportPath = "";
        if (StorageProvider != null)
        {
            var file = await StorageProvider.SaveFilePickerAsync(new FilePickerSaveOptions
            {
                Title = "Save Comments Summary",
                DefaultExtension = "md",
                SuggestedFileName = $"{System.IO.Path.GetFileNameWithoutExtension(DocumentTitle)}_Review_Notes.md"
            });
            if (file != null)
            {
                exportPath = file.Path.LocalPath;
            }
            else
            {
                UpdateStatus("Export cancelled.");
                return;
            }
        }
        else
        {
            exportPath = System.IO.Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.Desktop), $"{System.IO.Path.GetFileNameWithoutExtension(DocumentTitle)}_Review_Notes.md");
        }

        await System.IO.File.WriteAllTextAsync(exportPath, sb.ToString());
        ShowToast($"Saved review summary to {System.IO.Path.GetFileName(exportPath)}", "CommentTextMultipleOutline");
    }

    // --- CANVAS GRID & SNAP COMMANDS ---

    [RelayCommand]
    public void ToggleGrid()
    {
        ShowGrid = !ShowGrid;
        ShowToast(ShowGrid ? "Canvas Grid Enabled" : "Canvas Grid Disabled", "Grid");
    }

    [RelayCommand]
    public void ToggleSnapToGrid()
    {
        SnapToGrid = !SnapToGrid;
        ShowToast(SnapToGrid ? $"Snap to Grid Active ({(int)GridSnapSize}pt)" : "Snap to Grid Disabled", "Magnet");
    }

    [RelayCommand]
    public void SetGridSnapSize(string sizeStr)
    {
        if (Enum.TryParse<GridSnapSize>(sizeStr, true, out var size))
        {
            GridSnapSize = size;
            ShowToast($"Grid Snap Interval: {(int)size}pt", "GridLarge");
        }
    }

    // --- BATES NUMBERING STUDIO COMMANDS ---

    public string BatesSamplePreview =>
        $"{BatesPrefix}{BatesStartingNumber.ToString().PadLeft(BatesNumberOfDigits, '0')}{BatesSuffix}";

    [RelayCommand]
    public void OpenBatesNumberingDialog()
    {
        OnPropertyChanged(nameof(BatesSamplePreview));
        IsBatesNumberingDialogOpen = true;
        OpenRegisteredDialog("frypdf.dialog.bates");
    }

    [RelayCommand]
    public void CloseBatesNumberingDialog()
    {
        IsBatesNumberingDialogOpen = false;
        CloseDynamicDialog();
    }

    [RelayCommand]
    public void SetBatesPosition(BatesPosition pos)
    {
        BatesPosition = pos;
    }

    private CancellationTokenSource? _batesCts;

    [RelayCommand]
    public async Task ApplyBatesNumberingAsync()
    {
        IsApplyingBatesNumbers = true;
        BatesProgressPercentage = 0;
        _batesCts = new CancellationTokenSource();
        var ct = _batesCts.Token;

        int currentNum = BatesStartingNumber;
        int total = Pages.Count;

        try
        {
            for (int i = 0; i < Pages.Count; i++)
            {
                ct.ThrowIfCancellationRequested();

                var page = Pages[i];
                string batesText = $"{BatesPrefix}{currentNum.ToString().PadLeft(BatesNumberOfDigits, '0')}{BatesSuffix}";

                // Remove any existing Bates on page
                var existing = page.Elements.OfType<TextElementViewModel>().Where(e => e.Text.StartsWith(BatesPrefix) || e.DisplayName.Contains("Bates")).ToList();
                foreach (var el in existing) page.RemoveElement(el);

                double x = 40;
                double y = 40;
                double pageWidth = page.Width > 0 ? page.Width : 800;
                double pageHeight = page.Height > 0 ? page.Height : 1131;

                switch (BatesPosition)
                {
                    case BatesPosition.TopLeft:
                        page.HeaderLeft = batesText;
                        page.ShowHeaderFooter = true;
                        x = 40; y = 25; break;
                    case BatesPosition.TopCenter:
                        page.HeaderCenter = batesText;
                        page.ShowHeaderFooter = true;
                        x = (pageWidth - 220) / 2; y = 25; break;
                    case BatesPosition.TopRight:
                        page.HeaderRight = batesText;
                        page.ShowHeaderFooter = true;
                        x = pageWidth - 240; y = 25; break;
                    case BatesPosition.BottomLeft:
                        page.FooterLeft = batesText;
                        page.ShowHeaderFooter = true;
                        x = 40; y = pageHeight - 45; break;
                    case BatesPosition.BottomCenter:
                        page.FooterCenter = batesText;
                        page.ShowHeaderFooter = true;
                        x = (pageWidth - 220) / 2; y = pageHeight - 45; break;
                    case BatesPosition.BottomRight:
                        page.FooterRight = batesText;
                        page.ShowHeaderFooter = true;
                        x = pageWidth - 240; y = pageHeight - 45; break;
                }

                var batesEl = new TextElementViewModel
                {
                    X = x,
                    Y = y,
                    Width = 200,
                    Height = 25,
                    Text = batesText,
                    FontSize = BatesFontSize,
                    FontFamily = "Consolas",
                    TextColorHex = BatesFontColorHex,
                    IsBold = true
                };

                page.AddElement(batesEl);
                currentNum++;

                BatesProgressPercentage = total > 0 ? (i + 1) * 100.0 / total : 100.0;
                if ((i + 1) % 5 == 0)
                {
                    await Task.Yield();
                }
            }

            ShowToast($"Applied legal Bates stamp across all {Pages.Count} pages", "Numeric");
        }
        catch (OperationCanceledException)
        {
            ShowToast("Bates numbering cancelled.", "CloseCircleOutline");
        }
        finally
        {
            IsApplyingBatesNumbers = false;
            IsBatesNumberingDialogOpen = false;
        }
    }

    [RelayCommand]
    public void CancelBatesNumbering()
    {
        _batesCts?.Cancel();
    }

    [RelayCommand]
    public void RemoveBatesNumbering()
    {
        IsBatesNumberingDialogOpen = false;
        int removed = 0;
        foreach (var page in Pages)
        {
            if (page.HeaderLeft?.StartsWith(BatesPrefix) == true) page.HeaderLeft = "";
            if (page.HeaderCenter?.StartsWith(BatesPrefix) == true) page.HeaderCenter = "";
            if (page.HeaderRight?.StartsWith(BatesPrefix) == true) page.HeaderRight = "";
            if (page.FooterLeft?.StartsWith(BatesPrefix) == true) page.FooterLeft = "";
            if (page.FooterCenter?.StartsWith(BatesPrefix) == true) page.FooterCenter = "";
            if (page.FooterRight?.StartsWith(BatesPrefix) == true) page.FooterRight = "";

            var existing = page.Elements.OfType<TextElementViewModel>().Where(e => e.Text.StartsWith(BatesPrefix) || e.DisplayName.Contains("Bates")).ToList();
            foreach (var el in existing)
            {
                page.RemoveElement(el);
                removed++;
            }
        }
        ShowToast($"Removed {removed} Bates stamps from document", "DeleteOutline");
    }

    // --- DOCUMENT COMPARISON DIFF COMMANDS ---

    [RelayCommand]
    public async Task OpenCompareDialogAsync()
    {
        IsCompareDialogOpen = true;
        OpenRegisteredDialog("frypdf.dialog.compare");
        IsComparing = true;
        try
        {
            var compareService = new DocumentCompareService();
            var currentDoc = ToDocumentModel();
            var templateDoc = _templateService.CreateAnnualReportTemplate(); // Compare against standard baseline
            ActiveComparisonReport = await compareService.CompareDocumentsAsync(templateDoc, currentDoc);
        }
        finally
        {
            IsComparing = false;
        }
    }

    [RelayCommand]
    public void CloseCompareDialog()
    {
        IsCompareDialogOpen = false;
        CloseDynamicDialog();
    }

    // --- IN-CANVAS FIND & REPLACE COMMANDS ---

    [RelayCommand]
    public void OpenFindReplace()
    {
        IsFindReplaceOpen = true;
        if (!string.IsNullOrWhiteSpace(FindQuery))
        {
            FindNext();
        }
    }

    [RelayCommand]
    public void CloseFindReplace()
    {
        IsFindReplaceOpen = false;
    }

    [RelayCommand]
    public void FindNext()
    {
        if (string.IsNullOrWhiteSpace(FindQuery) || Pages.Count == 0) return;

        var comparison = FindMatchCase ? StringComparison.Ordinal : StringComparison.OrdinalIgnoreCase;
        int totalMatches = 0;
        bool found = false;

        foreach (var page in Pages)
        {
            var textElements = page.Elements.OfType<TextElementViewModel>().ToList();
            foreach (var el in textElements)
            {
                if (el.Text.Contains(FindQuery, comparison))
                {
                    totalMatches++;
                    if (!found)
                    {
                        SelectPage(page);
                        page.SelectElement(el);
                        found = true;
                    }
                }
            }
        }

        FindMatchesCount = totalMatches;
        if (totalMatches > 0)
        {
            ShowToast($"Found {totalMatches} match(es) for \"{FindQuery}\"", "Magnify");
        }
        else
        {
            ShowToast($"No matches found for \"{FindQuery}\"", "InformationOutline");
        }
    }

    [RelayCommand]
    public void FindPrevious()
    {
        FindNext();
    }

    [RelayCommand]
    public void ReplaceNext()
    {
        if (CurrentPage?.SelectedElement is TextElementViewModel txt && !string.IsNullOrWhiteSpace(FindQuery))
        {
            var comparison = FindMatchCase ? StringComparison.Ordinal : StringComparison.OrdinalIgnoreCase;
            if (txt.Text.Contains(FindQuery, comparison))
            {
                txt.Text = txt.Text.Replace(FindQuery, ReplaceQuery ?? "", comparison);
                ShowToast("Replaced occurrence", "FindReplace");
                FindNext();
            }
        }
        else
        {
            FindNext();
        }
    }

    [RelayCommand]
    public void ReplaceAll()
    {
        if (string.IsNullOrWhiteSpace(FindQuery)) return;

        var comparison = FindMatchCase ? StringComparison.Ordinal : StringComparison.OrdinalIgnoreCase;
        int replacements = 0;

        foreach (var page in Pages)
        {
            var textElements = page.Elements.OfType<TextElementViewModel>().ToList();
            foreach (var el in textElements)
            {
                if (el.Text.Contains(FindQuery, comparison))
                {
                    el.Text = el.Text.Replace(FindQuery, ReplaceQuery ?? "", comparison);
                    replacements++;
                }
            }
        }

        ShowToast($"Replaced {replacements} occurrences with \"{ReplaceQuery}\"", "FindReplace");
        FindMatchesCount = 0;
    }

    // --- MEASUREMENT & ACROBAT ANNOTATION COMMANDS ---

    [RelayCommand]
    public void AddMeasurementElement()
    {
        if (CurrentPage == null) return;

        var (posX, posY) = _placementService.GetPlacementPosition(CurrentPage, 240, 36);
        var measureEl = new MeasurementElementViewModel
        {
            X = posX,
            Y = posY,
            Width = 240,
            Height = 36,
            Unit = RulerUnit,
            StrokeColorHex = "#DC2626",
            StrokeThickness = 1.5,
            FontSize = 10
        };

        AddElementWithUndo(measureEl, "Added Measurement Dimension");
    }

    [RelayCommand]
    public void AddCalloutElement()
    {
        if (CurrentPage == null) return;

        var (posX, posY) = _placementService.GetPlacementPosition(CurrentPage, 220, 80);
        var callout = new ShapeElementViewModel
        {
            X = posX,
            Y = posY,
            Width = 220,
            Height = 80,
            ShapeType = ShapeType.Callout,
            FillColorHex = "#EFF6FF",
            StrokeColorHex = "#0F6CBD",
            StrokeThickness = 1.5,
            Label = "Important: Review Section",
            LabelFontSize = 11,
            LabelColorHex = "#0F6CBD"
        };

        AddElementWithUndo(callout, "Added Callout Note");
    }

    [RelayCommand]
    public void AddRevisionCloudElement()
    {
        if (CurrentPage == null) return;

        var (posX, posY) = _placementService.GetPlacementPosition(CurrentPage, 260, 140);
        var cloud = new ShapeElementViewModel
        {
            X = posX,
            Y = posY,
            Width = 260,
            Height = 140,
            ShapeType = ShapeType.RevisionCloud,
            FillColorHex = "#FEF2F2",
            StrokeColorHex = "#DC2626",
            StrokeThickness = 2.0,
            Opacity = 0.85
        };

        AddElementWithUndo(cloud, "Added Revision Cloud Markup");
    }

    // --- RULERS, PRESENTATION & THEME COMMANDS ---

    [RelayCommand]
    public void ToggleRulers()
    {
        ShowRulers = !ShowRulers;
        ShowToast(ShowRulers ? "Canvas Rulers Visible" : "Canvas Rulers Hidden", "Ruler");
    }

    [RelayCommand]
    public void SetRulerUnit(string unitStr)
    {
        if (Enum.TryParse<RulerUnit>(unitStr, true, out var unit))
        {
            RulerUnit = unit;
            ShowToast($"Ruler Units: {unit}", "RulerSquare");
        }
    }

    [RelayCommand]
    public void TogglePresentationMode()
    {
        IsPresentationMode = !IsPresentationMode;
        ShowToast(IsPresentationMode ? "Entered Full-Screen Presentation Mode (Esc to Exit)" : "Exited Presentation Mode", "Presentation");
    }
}
