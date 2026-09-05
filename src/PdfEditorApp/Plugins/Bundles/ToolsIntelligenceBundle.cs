using System;
using System.Collections.Generic;
using Microsoft.Extensions.DependencyInjection;
using PdfEditorApp.Core.Plugins;
using PdfEditorApp.Core.Plugins.Descriptors;
using PdfEditorApp.Core.Plugins.Profiles;
using PdfEditorApp.Models;
using PdfEditorApp.ViewModels.Tools.Intelligence;

namespace PdfEditorApp.Plugins.Bundles;

public class ToolsIntelligenceBundle : IFryPluginBundle
{
    public string Id => "FryPdf.Bundle.Tools.Intelligence";
    public string Name => "PDF Intelligence & Editing Tools Bundle";
    public string Description => "AI summarization, machine translation, OCR text extraction, visual document diffing, and PDF form design.";

    public IReadOnlyList<IFryPlugin> Plugins => new IFryPlugin[]
    {
        new AiSummarizerToolPlugin(),
        new TranslatePdfToolPlugin(),
        new OcrPdfToolPlugin(),
        new ComparePdfToolPlugin(),
        new EditPdfToolPlugin(),
        new PdfFormsToolPlugin()
    };
}

public class AiSummarizerToolPlugin : ToolPluginBase
{
    public override string Id => "frypdf.tool.aisummarizer";
    public override string Name => "AI Summarizer";

    protected override PdfToolDescriptor CreateDescriptor() => new()
    {
        Id = Id,
        LegacyId = (int)PdfToolId.AiSummarizer,
        Name = Name,
        Description = "Generate comprehensive, structured executive summaries and key takeaway bullet points using AI.",
        Category = "AiAndAutomation",
        IconKind = "Sparkles",
        IconColorHex = "#7C3AED",
        BackgroundAccentHex = "#F5F3FF",
        SupportsMultiFile = false,
        AcceptedFileExtensions = ".pdf",
        CreateViewModel = sp => ActivatorUtilities.CreateInstance<AiSummarizerToolViewModel>(sp)
    };
}

public class TranslatePdfToolPlugin : ToolPluginBase
{
    public override string Id => "frypdf.tool.translate";
    public override string Name => "Translate PDF";

    protected override PdfToolDescriptor CreateDescriptor() => new()
    {
        Id = Id,
        LegacyId = (int)PdfToolId.TranslatePdf,
        Name = Name,
        Description = "Translate entire PDF documents into 50+ world languages while preserving visual layout and geometry.",
        Category = "AiAndAutomation",
        IconKind = "Translate",
        IconColorHex = "#0284C7",
        BackgroundAccentHex = "#F0F9FF",
        SupportsMultiFile = false,
        AcceptedFileExtensions = ".pdf",
        CreateViewModel = sp => ActivatorUtilities.CreateInstance<TranslatePdfToolViewModel>(sp)
    };
}

public class OcrPdfToolPlugin : ToolPluginBase
{
    public override string Id => "frypdf.tool.ocr";
    public override string Name => "OCR PDF";

    protected override PdfToolDescriptor CreateDescriptor() => new()
    {
        Id = Id,
        LegacyId = (int)PdfToolId.OcrPdf,
        Name = Name,
        Description = "Convert scanned documents and image PDFs into fully searchable, selectable, and copyable text.",
        Category = "AiAndAutomation",
        IconKind = "TextRecognition",
        IconColorHex = "#D97706",
        BackgroundAccentHex = "#FFFBEB",
        SupportsMultiFile = true,
        AcceptedFileExtensions = ".pdf,.jpg,.jpeg,.png",
        CreateViewModel = sp => ActivatorUtilities.CreateInstance<OcrPdfToolViewModel>(sp)
    };
}

public class ComparePdfToolPlugin : ToolPluginBase
{
    public override string Id => "frypdf.tool.compare";
    public override string Name => "Compare PDF";

    protected override PdfToolDescriptor CreateDescriptor() => new()
    {
        Id = Id,
        LegacyId = (int)PdfToolId.ComparePdf,
        Name = Name,
        Description = "Show a side-by-side comparison of two PDF documents and easily spot differences, additions, and deletions.",
        Category = "AiAndAutomation",
        IconKind = "FileCompare",
        IconColorHex = "#2563EB",
        BackgroundAccentHex = "#EFF6FF",
        SupportsMultiFile = false,
        AcceptedFileExtensions = ".pdf",
        CreateViewModel = sp => ActivatorUtilities.CreateInstance<ComparePdfToolViewModel>(sp)
    };
}

public class EditPdfToolPlugin : ToolPluginBase
{
    public override string Id => "frypdf.tool.edit";
    public override string Name => "Edit PDF";

    protected override PdfToolDescriptor CreateDescriptor() => new()
    {
        Id = Id,
        LegacyId = (int)PdfToolId.EditPdf,
        Name = Name,
        Description = "Add text, images, shapes or freehand annotations to a PDF document. Edit the size, font, and color of added content.",
        Category = "EditAndForms",
        IconKind = "SquareEditOutline",
        IconColorHex = "#7C3AED",
        BackgroundAccentHex = "#F5F3FF",
        SupportsMultiFile = false,
        AcceptedFileExtensions = ".pdf",
        CreateViewModel = sp => ActivatorUtilities.CreateInstance<EditPdfToolViewModel>(sp)
    };
}

public class PdfFormsToolPlugin : ToolPluginBase
{
    public override string Id => "frypdf.tool.forms";
    public override string Name => "PDF Forms";

    protected override PdfToolDescriptor CreateDescriptor() => new()
    {
        Id = Id,
        LegacyId = (int)PdfToolId.PdfForms,
        Name = Name,
        Description = "Design interactive AcroForms with text inputs, checkboxes, radio options, dropdowns, and digital signature boxes.",
        Category = "EditAndForms",
        IconKind = "FormSelect",
        IconColorHex = "#059669",
        BackgroundAccentHex = "#ECFDF5",
        SupportsMultiFile = false,
        AcceptedFileExtensions = ".pdf",
        CreateViewModel = sp => ActivatorUtilities.CreateInstance<PdfFormsToolViewModel>(sp)
    };
}
