using System;
using System.Collections.Generic;
using System.Linq;
using PdfEditorApp.Models;

namespace PdfEditorApp.Services.Tools.Core;

using PdfEditorApp.Core.Plugins;

public interface IPdfToolRegistry
{
    event Action? RegistryChanged;
    IReadOnlyList<PdfToolDefinition> GetAllTools();
    IReadOnlyList<PdfToolDefinition> GetToolsByCategory(PdfToolCategory category);
    PdfToolDefinition? GetTool(PdfToolId id);
    PdfToolDefinition? GetTool(string stringId);
    void RegisterTool(PdfToolDefinition tool);
}

public class PdfToolRegistry : IPdfToolRegistry
{
    public event Action? RegistryChanged;
    private readonly List<PdfToolDefinition> _tools;
    private readonly IFryPluginContext? _pluginContext;
    private readonly bool _seedDefaults;

    public PdfToolRegistry(IFryPluginContext? pluginContext = null, bool seedDefaults = true)
    {
        _pluginContext = pluginContext;
        _seedDefaults = seedDefaults;
        _tools = seedDefaults ? GetBuiltInToolDefinitions() : new List<PdfToolDefinition>();
        if (_pluginContext != null)
        {
            _pluginContext.ToolsChanged += () => RegistryChanged?.Invoke();
        }
    }

    private static List<PdfToolDefinition> GetBuiltInToolDefinitions() => new()
    {
            // 1. MERGE PDF
            new PdfToolDefinition
            {
                Id = PdfToolId.MergePdf,
                Name = "Merge PDF",
                Description = "Combine PDFs in the order you want with the easiest PDF merger available.",
                Category = PdfToolCategory.OrganizeAndPage,
                IconKind = "CallMerge",
                IconColorHex = "#EA580C",
                BackgroundAccentHex = "#FFF7ED",
                SupportsMultiFile = true,
                AcceptedFileExtensions = ".pdf"
            },
            // 2. SPLIT PDF
            new PdfToolDefinition
            {
                Id = PdfToolId.SplitPdf,
                Name = "Split PDF",
                Description = "Separate one page or a whole set for easy conversion into independent PDF files.",
                Category = PdfToolCategory.OrganizeAndPage,
                IconKind = "CallSplit",
                IconColorHex = "#DC2626",
                BackgroundAccentHex = "#FEF2F2",
                SupportsMultiFile = false,
                AcceptedFileExtensions = ".pdf"
            },
            // 3. COMPRESS PDF
            new PdfToolDefinition
            {
                Id = PdfToolId.CompressPdf,
                Name = "Compress PDF",
                Description = "Reduce file size while optimizing for maximal PDF quality.",
                Category = PdfToolCategory.OptimizeAndSecurity,
                IconKind = "ArrowCollapseAll",
                IconColorHex = "#16A34A",
                BackgroundAccentHex = "#F0FDF4",
                SupportsMultiFile = true,
                AcceptedFileExtensions = ".pdf"
            },
            // 4. PDF TO WORD
            new PdfToolDefinition
            {
                Id = PdfToolId.PdfToWord,
                Name = "PDF to Word",
                Description = "Easily convert your PDF files into easy to edit DOC and DOCX documents.",
                Category = PdfToolCategory.ConvertFromPdf,
                IconKind = "FileWordOutline",
                IconColorHex = "#2563EB",
                BackgroundAccentHex = "#EFF6FF",
                SupportsMultiFile = true,
                AcceptedFileExtensions = ".pdf"
            },
            // 5. PDF TO POWERPOINT
            new PdfToolDefinition
            {
                Id = PdfToolId.PdfToPowerPoint,
                Name = "PDF to PowerPoint",
                Description = "Turn your PDF files into easy to edit PPT and PPTX slideshows.",
                Category = PdfToolCategory.ConvertFromPdf,
                IconKind = "FilePowerpointOutline",
                IconColorHex = "#EA580C",
                BackgroundAccentHex = "#FFF7ED",
                SupportsMultiFile = true,
                AcceptedFileExtensions = ".pdf"
            },
            // 6. PDF TO EXCEL
            new PdfToolDefinition
            {
                Id = PdfToolId.PdfToExcel,
                Name = "PDF to Excel",
                Description = "Pull data straight from PDFs into Excel spreadsheets in a few short seconds.",
                Category = PdfToolCategory.ConvertFromPdf,
                IconKind = "FileExcelOutline",
                IconColorHex = "#16A34A",
                BackgroundAccentHex = "#F0FDF4",
                SupportsMultiFile = true,
                AcceptedFileExtensions = ".pdf"
            },
            // 7. WORD TO PDF
            new PdfToolDefinition
            {
                Id = PdfToolId.WordToPdf,
                Name = "Word to PDF",
                Description = "Make DOC and DOCX files easy to read by converting them to PDF.",
                Category = PdfToolCategory.ConvertToPdf,
                IconKind = "FileWord",
                IconColorHex = "#2563EB",
                BackgroundAccentHex = "#EFF6FF",
                SupportsMultiFile = true,
                AcceptedFileExtensions = ".docx,.doc"
            },
            // 8. POWERPOINT TO PDF
            new PdfToolDefinition
            {
                Id = PdfToolId.PowerPointToPdf,
                Name = "PowerPoint to PDF",
                Description = "Make PPT and PPTX slideshows easy to view by converting them to PDF.",
                Category = PdfToolCategory.ConvertToPdf,
                IconKind = "FilePowerpoint",
                IconColorHex = "#EA580C",
                BackgroundAccentHex = "#FFF7ED",
                SupportsMultiFile = true,
                AcceptedFileExtensions = ".pptx,.ppt"
            },
            // 9. EXCEL TO PDF
            new PdfToolDefinition
            {
                Id = PdfToolId.ExcelToPdf,
                Name = "Excel to PDF",
                Description = "Make EXCEL spreadsheets easy to read by converting them to PDF.",
                Category = PdfToolCategory.ConvertToPdf,
                IconKind = "FileExcel",
                IconColorHex = "#16A34A",
                BackgroundAccentHex = "#F0FDF4",
                SupportsMultiFile = true,
                AcceptedFileExtensions = ".xlsx,.xls"
            },
            // 10. EDIT PDF
            new PdfToolDefinition
            {
                Id = PdfToolId.EditPdf,
                Name = "Edit PDF",
                Description = "Add text, images, shapes or freehand annotations to a PDF document. Edit size, font and color.",
                Category = PdfToolCategory.EditAndForms,
                IconKind = "Draw",
                IconColorHex = "#9333EA",
                BackgroundAccentHex = "#FAF5FF",
                SupportsMultiFile = false,
                AcceptedFileExtensions = ".pdf,.frypdf,.json"
            },
            // 11. PDF TO JPG
            new PdfToolDefinition
            {
                Id = PdfToolId.PdfToJpg,
                Name = "PDF to JPG",
                Description = "Convert each PDF page into a JPG or extract all images contained in a PDF.",
                Category = PdfToolCategory.ConvertFromPdf,
                IconKind = "FileImageOutline",
                IconColorHex = "#CA8A04",
                BackgroundAccentHex = "#FEFCE8",
                SupportsMultiFile = true,
                AcceptedFileExtensions = ".pdf"
            },
            // 12. JPG TO PDF
            new PdfToolDefinition
            {
                Id = PdfToolId.JpgToPdf,
                Name = "JPG to PDF",
                Description = "Convert JPG images to PDF in seconds. Easily adjust orientation and margins.",
                Category = PdfToolCategory.ConvertToPdf,
                IconKind = "ImageOutline",
                IconColorHex = "#CA8A04",
                BackgroundAccentHex = "#FEFCE8",
                SupportsMultiFile = true,
                AcceptedFileExtensions = ".jpg,.jpeg,.png,.bmp,.webp"
            },
            // 13. SIGN PDF
            new PdfToolDefinition
            {
                Id = PdfToolId.SignPdf,
                Name = "Sign PDF",
                Description = "Sign yourself with ink, typed cursive, image stamp, or digital certificate signatures.",
                Category = PdfToolCategory.OptimizeAndSecurity,
                IconKind = "DrawPen",
                IconColorHex = "#2563EB",
                BackgroundAccentHex = "#EFF6FF",
                SupportsMultiFile = false,
                AcceptedFileExtensions = ".pdf"
            },
            // 14. WATERMARK
            new PdfToolDefinition
            {
                Id = PdfToolId.Watermark,
                Name = "Watermark",
                Description = "Stamp an image or text over your PDF in seconds. Choose typography, transparency and position.",
                Category = PdfToolCategory.EditAndForms,
                IconKind = "Watermark",
                IconColorHex = "#9333EA",
                BackgroundAccentHex = "#FAF5FF",
                SupportsMultiFile = true,
                AcceptedFileExtensions = ".pdf"
            },
            // 15. ROTATE PDF
            new PdfToolDefinition
            {
                Id = PdfToolId.RotatePdf,
                Name = "Rotate PDF",
                Description = "Rotate your PDFs the way you need them. You can even rotate multiple PDFs at once!",
                Category = PdfToolCategory.OrganizeAndPage,
                IconKind = "RotateRight",
                IconColorHex = "#9333EA",
                BackgroundAccentHex = "#FAF5FF",
                SupportsMultiFile = true,
                AcceptedFileExtensions = ".pdf"
            },
            // 16. HTML TO PDF
            new PdfToolDefinition
            {
                Id = PdfToolId.HtmlToPdf,
                Name = "HTML to PDF",
                Description = "Convert webpages or HTML files to PDF. Copy and paste URL or load local HTML.",
                Category = PdfToolCategory.ConvertToPdf,
                IconKind = "LanguageHtml5",
                IconColorHex = "#CA8A04",
                BackgroundAccentHex = "#FEFCE8",
                SupportsMultiFile = false,
                AcceptedFileExtensions = ".html,.htm"
            },
            // 17. UNLOCK PDF
            new PdfToolDefinition
            {
                Id = PdfToolId.UnlockPdf,
                Name = "Unlock PDF",
                Description = "Remove PDF password security, giving you the freedom to use your PDFs as you want.",
                Category = PdfToolCategory.OptimizeAndSecurity,
                IconKind = "LockOpenOutline",
                IconColorHex = "#0284C7",
                BackgroundAccentHex = "#F0F9FF",
                SupportsMultiFile = true,
                AcceptedFileExtensions = ".pdf"
            },
            // 18. PROTECT PDF
            new PdfToolDefinition
            {
                Id = PdfToolId.ProtectPdf,
                Name = "Protect PDF",
                Description = "Protect PDF files with a password. Encrypt PDF documents to prevent unauthorized access.",
                Category = PdfToolCategory.OptimizeAndSecurity,
                IconKind = "ShieldCheckOutline",
                IconColorHex = "#0284C7",
                BackgroundAccentHex = "#F0F9FF",
                SupportsMultiFile = true,
                AcceptedFileExtensions = ".pdf"
            },
            // 19. ORGANIZE PDF
            new PdfToolDefinition
            {
                Id = PdfToolId.OrganizePdf,
                Name = "Organize PDF",
                Description = "Sort pages of your PDF file however you like. Delete PDF pages or add PDF pages at convenience.",
                Category = PdfToolCategory.OrganizeAndPage,
                IconKind = "SortAlphabeticalVariant",
                IconColorHex = "#EA580C",
                BackgroundAccentHex = "#FFF7ED",
                SupportsMultiFile = false,
                AcceptedFileExtensions = ".pdf"
            },
            // 20. PDF TO PDF/A
            new PdfToolDefinition
            {
                Id = PdfToolId.PdfToPdfA,
                Name = "PDF to PDF/A",
                Description = "Transform your PDF to PDF/A, the ISO-standardized version of PDF for long-term archiving.",
                Category = PdfToolCategory.OptimizeAndSecurity,
                IconKind = "FileCheckOutline",
                IconColorHex = "#0284C7",
                BackgroundAccentHex = "#F0F9FF",
                SupportsMultiFile = true,
                AcceptedFileExtensions = ".pdf"
            },
            // 21. REPAIR PDF
            new PdfToolDefinition
            {
                Id = PdfToolId.RepairPdf,
                Name = "Repair PDF",
                Description = "Repair a damaged PDF and recover data from corrupt PDF. Fix PDF files with our repair tool.",
                Category = PdfToolCategory.OptimizeAndSecurity,
                IconKind = "WrenchOutline",
                IconColorHex = "#65A30D",
                BackgroundAccentHex = "#F7FEE7",
                SupportsMultiFile = true,
                AcceptedFileExtensions = ".pdf"
            },
            // 22. PAGE NUMBERS
            new PdfToolDefinition
            {
                Id = PdfToolId.PageNumbers,
                Name = "Page numbers",
                Description = "Add page numbers into PDFs with ease. Choose your positions, dimensions, typography.",
                Category = PdfToolCategory.OrganizeAndPage,
                IconKind = "Numeric1BoxMultipleOutline",
                IconColorHex = "#9333EA",
                BackgroundAccentHex = "#FAF5FF",
                SupportsMultiFile = true,
                AcceptedFileExtensions = ".pdf"
            },
            // 23. SCAN TO PDF
            new PdfToolDefinition
            {
                Id = PdfToolId.ScanToPdf,
                Name = "Scan to PDF",
                Description = "Capture document scans from your mobile device or scanner and enhance them cleanly.",
                Category = PdfToolCategory.ConvertToPdf,
                IconKind = "Scanner",
                IconColorHex = "#EA580C",
                BackgroundAccentHex = "#FFF7ED",
                SupportsMultiFile = true,
                AcceptedFileExtensions = ".jpg,.jpeg,.png,.tiff,.bmp"
            },
            // 24. OCR PDF
            new PdfToolDefinition
            {
                Id = PdfToolId.OcrPdf,
                Name = "OCR PDF",
                Description = "Easily convert scanned PDF into searchable and selectable documents.",
                Category = PdfToolCategory.AiAndAutomation,
                IconKind = "TextRecognition",
                IconColorHex = "#65A30D",
                BackgroundAccentHex = "#F7FEE7",
                SupportsMultiFile = true,
                AcceptedFileExtensions = ".pdf"
            },
            // 25. COMPARE PDF
            new PdfToolDefinition
            {
                Id = PdfToolId.ComparePdf,
                Name = "Compare PDF",
                Description = "Show a side-by-side document comparison and easily spot changes between different file versions.",
                Category = PdfToolCategory.EditAndForms,
                IconKind = "BookOpenPageVariantOutline",
                IconColorHex = "#2563EB",
                BackgroundAccentHex = "#EFF6FF",
                SupportsMultiFile = true,
                AcceptedFileExtensions = ".pdf"
            },
            // 26. REDACT PDF
            new PdfToolDefinition
            {
                Id = PdfToolId.RedactPdf,
                Name = "Redact PDF",
                Description = "Redact text and graphics to permanently remove sensitive information from a PDF.",
                Category = PdfToolCategory.OptimizeAndSecurity,
                IconKind = "SelectRemove",
                IconColorHex = "#0284C7",
                BackgroundAccentHex = "#F0F9FF",
                SupportsMultiFile = false,
                AcceptedFileExtensions = ".pdf"
            },
            // 27. CROP PDF
            new PdfToolDefinition
            {
                Id = PdfToolId.CropPdf,
                Name = "Crop PDF",
                Description = "Crop margins of PDF documents or select specific areas, then apply changes to one or all pages.",
                Category = PdfToolCategory.OrganizeAndPage,
                IconKind = "Crop",
                IconColorHex = "#9333EA",
                BackgroundAccentHex = "#FAF5FF",
                SupportsMultiFile = true,
                AcceptedFileExtensions = ".pdf"
            },
            // 28. PDF FORMS (New!)
            new PdfToolDefinition
            {
                Id = PdfToolId.PdfForms,
                Name = "PDF Forms",
                Description = "Detect form fields automatically, create interactive fillable PDFs, or fill PDF forms yourself.",
                Category = PdfToolCategory.EditAndForms,
                IconKind = "TextBoxOutline",
                IconColorHex = "#9333EA",
                BackgroundAccentHex = "#FAF5FF",
                IsNew = true,
                SupportsMultiFile = false,
                AcceptedFileExtensions = ".pdf"
            },
            // 29. AI SUMMARIZER (New!)
            new PdfToolDefinition
            {
                Id = PdfToolId.AiSummarizer,
                Name = "AI Summarizer",
                Description = "Quickly generate concise summaries from articles, paragraphs, and essays with key points in seconds.",
                Category = PdfToolCategory.AiAndAutomation,
                IconKind = "FormatListBulletedSquare",
                IconColorHex = "#6366F1",
                BackgroundAccentHex = "#EEF2FF",
                IsNew = true,
                SupportsMultiFile = false,
                AcceptedFileExtensions = ".pdf"
            },
            // 30. TRANSLATE PDF (New!)
            new PdfToolDefinition
            {
                Id = PdfToolId.TranslatePdf,
                Name = "Translate PDF",
                Description = "Easily translate PDF files powered by AI. Keep fonts, layout, and formatting perfectly intact.",
                Category = PdfToolCategory.AiAndAutomation,
                IconKind = "Translate",
                IconColorHex = "#6366F1",
                BackgroundAccentHex = "#EEF2FF",
                IsNew = true,
                SupportsMultiFile = false,
                AcceptedFileExtensions = ".pdf"
            },
            // 31. PDF TO MARKDOWN (New!)
            new PdfToolDefinition
            {
                Id = PdfToolId.PdfToMarkdown,
                Name = "PDF to Markdown",
                Description = "Easily turn PDFs into Markdown files for notes, docs, and LLMs. Headings, tables, and lists preserved.",
                Category = PdfToolCategory.ConvertFromPdf,
                IconKind = "CodeTags",
                IconColorHex = "#6366F1",
                BackgroundAccentHex = "#EEF2FF",
                IsNew = true,
                SupportsMultiFile = true,
                AcceptedFileExtensions = ".pdf"
            },
            // 32. CREATE A WORKFLOW (New Callout Banner!)
            new PdfToolDefinition
            {
                Id = PdfToolId.WorkflowBuilder,
                Name = "Create a workflow",
                Description = "Create custom workflows with your favorite tools, automate tasks, and reuse them anytime.",
                Category = PdfToolCategory.AiAndAutomation,
                IconKind = "TuneVariant",
                IconColorHex = "#E11D48",
                BackgroundAccentHex = "#FFE4E6",
                IsNew = true,
                IsWorkflowBanner = true,
                SupportsMultiFile = true,
                AcceptedFileExtensions = ".pdf,.fryflow,.json"
            },
            // 33. BATCH MAIL MERGE & MASS PDF GENERATOR (New!)
            new PdfToolDefinition
            {
                Id = PdfToolId.BatchMailMerge,
                Name = "Batch Mail Merge & Mass PDF",
                Description = "Generate hundreds of personalized PDFs (payslips, certificates, invoices, badges) in one click using Excel, CSV, or REST APIs.",
                Category = PdfToolCategory.AiAndAutomation,
                IconKind = "DatabaseArrowDownOutline",
                IconColorHex = "#0F6CBD",
                BackgroundAccentHex = "#EFF6FF",
                IsNew = true,
                SupportsMultiFile = false,
                AcceptedFileExtensions = ".xlsx,.csv,.tsv,.json"
            }
        };

    public IReadOnlyList<PdfToolDefinition> GetAllTools()
    {
        if (_pluginContext == null) return _tools;

        var pluginTools = _pluginContext.GetRegisteredTools();
        if (pluginTools.Count == 0 && _seedDefaults) return _tools;

        var merged = _seedDefaults ? new List<PdfToolDefinition>(_tools) : new List<PdfToolDefinition>();
        foreach (var pt in pluginTools)
        {
            var existingIndex = merged.FindIndex(t =>
                (!string.IsNullOrEmpty(t.StringId) && string.Equals(t.StringId, pt.Id, StringComparison.OrdinalIgnoreCase)) ||
                (pt.LegacyId.HasValue && t.Id == (PdfToolId)pt.LegacyId.Value));

            var toolDef = new PdfToolDefinition
            {
                Id = pt.LegacyId.HasValue && Enum.IsDefined(typeof(PdfToolId), pt.LegacyId.Value)
                    ? (PdfToolId)pt.LegacyId.Value
                    : PdfToolId.EditPdf,
                StringId = pt.Id,
                Name = pt.Name,
                Description = pt.Description,
                Category = ParseCategory(pt.Category),
                IconKind = pt.IconKind,
                IconColorHex = pt.IconColorHex,
                BackgroundAccentHex = pt.BackgroundAccentHex,
                SupportsMultiFile = pt.SupportsMultiFile,
                AcceptedFileExtensions = pt.AcceptedFileExtensions,
                ViewModelFactory = pt.CreateViewModel
            };

            if (existingIndex >= 0)
            {
                merged[existingIndex] = toolDef;
            }
            else
            {
                merged.Add(toolDef);
            }
        }
        return merged;
    }

    public IReadOnlyList<PdfToolDefinition> GetToolsByCategory(PdfToolCategory category)
    {
        var all = GetAllTools();
        if (category == PdfToolCategory.All) return all;
        return all.Where(t => t.Category == category).ToList();
    }

    public PdfToolDefinition? GetTool(PdfToolId id) => GetAllTools().FirstOrDefault(t => t.Id == id);

    public PdfToolDefinition? GetTool(string stringId)
    {
        if (string.IsNullOrWhiteSpace(stringId)) return null;
        return GetAllTools().FirstOrDefault(t =>
            string.Equals(t.StringId, stringId, StringComparison.OrdinalIgnoreCase) ||
            string.Equals(t.Id.ToString(), stringId, StringComparison.OrdinalIgnoreCase));
    }

    public void NotifyRegistryChanged() => RegistryChanged?.Invoke();

    public void RegisterTool(PdfToolDefinition tool)
    {
        ArgumentNullException.ThrowIfNull(tool);
        _tools.Add(tool);
        NotifyRegistryChanged();
    }

    private static PdfToolCategory ParseCategory(string category)
    {
        if (Enum.TryParse<PdfToolCategory>(category, true, out var cat))
            return cat;
        return PdfToolCategory.OrganizeAndPage;
    }
}
