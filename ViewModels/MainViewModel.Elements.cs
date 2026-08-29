using System;
using System.IO;
using System.Threading.Tasks;
using Avalonia.Platform.Storage;
using CommunityToolkit.Mvvm.Input;
using PdfEditorApp.Models;
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

        AddElementWithUndo(textEl, "Added Text Paragraph");
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

        AddElementWithUndo(shapeEl, $"Added Shape ({shapeType})");
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

                    AddElementWithUndo(imgEl, $"Inserted Image: {Path.GetFileName(filePath)}");
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

        AddElementWithUndo(stampEl, $"Added Stamp ({label})");
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

        AddElementWithUndo(noteEl, "Added Sticky Note");
        RefreshComments();
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

        AddElementWithUndo(divEl, "Added Divider Line");
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

        var chartEl = new ChartElementViewModel
        {
            X = 100,
            Y = 250,
            Width = 400,
            Height = 220,
            ChartType = chartType,
            Title = $"{chartType} Chart Analysis"
        };

        AddElementWithUndo(chartEl, $"Added {chartType} Chart");
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

        AddElementWithUndo(formEl, $"Added Form Field ({fieldType})");
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

        AddElementWithUndo(qrEl, "Added Vector QR Code");
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

        AddElementWithUndo(barcodeEl, "Added Barcode");
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

        AddElementWithUndo(inkEl, isHighlighter ? "Added Highlighter Stroke" : "Added Freehand Ink Stroke");
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
        ShowToast("Applied Bates Numbering (CONF-BATES-000001...)", "Numeric");
    }
}
