using System;
using System.Collections.Generic;
using Microsoft.Extensions.DependencyInjection;
using PdfEditorApp.Core.Plugins;
using PdfEditorApp.Core.Plugins.Descriptors;
using PdfEditorApp.Core.Plugins.Profiles;
using PdfEditorApp.Models;
using PdfEditorApp.ViewModels.Tools.Conversion;

namespace PdfEditorApp.Plugins.Bundles;

public class ToolsConversionBundle : IFryPluginBundle
{
    public string Id => "FryPdf.Bundle.Tools.Conversion";
    public string Name => "PDF Conversion Tools Bundle";
    public string Description => "Bi-directional conversions between PDF and Office (Word, Excel, PowerPoint), Images, HTML, Markdown, and PDF/A.";

    public IReadOnlyList<IFryPlugin> Plugins => new IFryPlugin[]
    {
        new PdfToWordToolPlugin(),
        new WordToPdfToolPlugin(),
        new PdfToPowerPointToolPlugin(),
        new PowerPointToPdfToolPlugin(),
        new PdfToExcelToolPlugin(),
        new ExcelToPdfToolPlugin(),
        new PdfToJpgToolPlugin(),
        new JpgToPdfToolPlugin(),
        new HtmlToPdfToolPlugin(),
        new PdfToMarkdownToolPlugin(),
        new PdfToPdfAToolPlugin(),
        new ScanToPdfToolPlugin()
    };
}

public class PdfToWordToolPlugin : ToolPluginBase
{
    public override string Id => "frypdf.tool.pdftoword";
    public override string Name => "PDF to Word";

    protected override PdfToolDescriptor CreateDescriptor() => new()
    {
        Id = Id,
        LegacyId = (int)PdfToolId.PdfToWord,
        Name = Name,
        Description = "Easily convert your PDF files into easy to edit DOC and DOCX documents.",
        Category = "ConvertFromPdf",
        IconKind = "FileWordOutline",
        IconColorHex = "#2563EB",
        BackgroundAccentHex = "#EFF6FF",
        SupportsMultiFile = true,
        AcceptedFileExtensions = ".pdf",
        CreateViewModel = sp => ActivatorUtilities.CreateInstance<PdfToWordToolViewModel>(sp)
    };
}

public class WordToPdfToolPlugin : ToolPluginBase
{
    public override string Id => "frypdf.tool.wordtopdf";
    public override string Name => "Word to PDF";

    protected override PdfToolDescriptor CreateDescriptor() => new()
    {
        Id = Id,
        LegacyId = (int)PdfToolId.WordToPdf,
        Name = Name,
        Description = "Make DOC and DOCX files easy to read by converting them to PDF.",
        Category = "ConvertToPdf",
        IconKind = "FileWordOutline",
        IconColorHex = "#2563EB",
        BackgroundAccentHex = "#EFF6FF",
        SupportsMultiFile = true,
        AcceptedFileExtensions = ".doc,.docx",
        CreateViewModel = sp => ActivatorUtilities.CreateInstance<WordToPdfToolViewModel>(sp)
    };
}

public class PdfToPowerPointToolPlugin : ToolPluginBase
{
    public override string Id => "frypdf.tool.pdftopowerpoint";
    public override string Name => "PDF to PowerPoint";

    protected override PdfToolDescriptor CreateDescriptor() => new()
    {
        Id = Id,
        LegacyId = (int)PdfToolId.PdfToPowerPoint,
        Name = Name,
        Description = "Turn your PDF files into easy to edit PPT and PPTX slideshows.",
        Category = "ConvertFromPdf",
        IconKind = "FilePowerpointOutline",
        IconColorHex = "#EA580C",
        BackgroundAccentHex = "#FFF7ED",
        SupportsMultiFile = true,
        AcceptedFileExtensions = ".pdf",
        CreateViewModel = sp => ActivatorUtilities.CreateInstance<PdfToPowerPointToolViewModel>(sp)
    };
}

public class PowerPointToPdfToolPlugin : ToolPluginBase
{
    public override string Id => "frypdf.tool.powerpointtopdf";
    public override string Name => "PowerPoint to PDF";

    protected override PdfToolDescriptor CreateDescriptor() => new()
    {
        Id = Id,
        LegacyId = (int)PdfToolId.PowerPointToPdf,
        Name = Name,
        Description = "Make PPT and PPTX slideshows easy to view by converting them to PDF.",
        Category = "ConvertToPdf",
        IconKind = "FilePowerpointOutline",
        IconColorHex = "#EA580C",
        BackgroundAccentHex = "#FFF7ED",
        SupportsMultiFile = true,
        AcceptedFileExtensions = ".ppt,.pptx",
        CreateViewModel = sp => ActivatorUtilities.CreateInstance<PowerPointToPdfToolViewModel>(sp)
    };
}

public class PdfToExcelToolPlugin : ToolPluginBase
{
    public override string Id => "frypdf.tool.pdftoexcel";
    public override string Name => "PDF to Excel";

    protected override PdfToolDescriptor CreateDescriptor() => new()
    {
        Id = Id,
        LegacyId = (int)PdfToolId.PdfToExcel,
        Name = Name,
        Description = "Pull data straight from PDFs into Excel spreadsheets in a few short seconds.",
        Category = "ConvertFromPdf",
        IconKind = "FileExcelOutline",
        IconColorHex = "#16A34A",
        BackgroundAccentHex = "#F0FDF4",
        SupportsMultiFile = true,
        AcceptedFileExtensions = ".pdf",
        CreateViewModel = sp => ActivatorUtilities.CreateInstance<PdfToExcelToolViewModel>(sp)
    };
}

public class ExcelToPdfToolPlugin : ToolPluginBase
{
    public override string Id => "frypdf.tool.exceltopdf";
    public override string Name => "Excel to PDF";

    protected override PdfToolDescriptor CreateDescriptor() => new()
    {
        Id = Id,
        LegacyId = (int)PdfToolId.ExcelToPdf,
        Name = Name,
        Description = "Make EXCEL spreadsheets easy to read by converting them to PDF.",
        Category = "ConvertToPdf",
        IconKind = "FileExcelOutline",
        IconColorHex = "#16A34A",
        BackgroundAccentHex = "#F0FDF4",
        SupportsMultiFile = true,
        AcceptedFileExtensions = ".xls,.xlsx",
        CreateViewModel = sp => ActivatorUtilities.CreateInstance<ExcelToPdfToolViewModel>(sp)
    };
}

public class PdfToJpgToolPlugin : ToolPluginBase
{
    public override string Id => "frypdf.tool.pdftojpg";
    public override string Name => "PDF to JPG";

    protected override PdfToolDescriptor CreateDescriptor() => new()
    {
        Id = Id,
        LegacyId = (int)PdfToolId.PdfToJpg,
        Name = Name,
        Description = "Convert each PDF page into a JPG or extract all images contained in a PDF.",
        Category = "ConvertFromPdf",
        IconKind = "FileImageOutline",
        IconColorHex = "#D97706",
        BackgroundAccentHex = "#FFFBEB",
        SupportsMultiFile = true,
        AcceptedFileExtensions = ".pdf",
        CreateViewModel = sp => ActivatorUtilities.CreateInstance<PdfToJpgToolViewModel>(sp)
    };
}

public class JpgToPdfToolPlugin : ToolPluginBase
{
    public override string Id => "frypdf.tool.jpgtopdf";
    public override string Name => "JPG to PDF";

    protected override PdfToolDescriptor CreateDescriptor() => new()
    {
        Id = Id,
        LegacyId = (int)PdfToolId.JpgToPdf,
        Name = Name,
        Description = "Convert JPG, PNG, BMP, GIF and TIFF images to PDF in seconds with custom orientation and margins.",
        Category = "ConvertToPdf",
        IconKind = "FileImageOutline",
        IconColorHex = "#D97706",
        BackgroundAccentHex = "#FFFBEB",
        SupportsMultiFile = true,
        AcceptedFileExtensions = ".jpg,.jpeg,.png,.bmp,.gif,.webp",
        CreateViewModel = sp => ActivatorUtilities.CreateInstance<JpgToPdfToolViewModel>(sp)
    };
}

public class HtmlToPdfToolPlugin : ToolPluginBase
{
    public override string Id => "frypdf.tool.htmltopdf";
    public override string Name => "HTML to PDF";

    protected override PdfToolDescriptor CreateDescriptor() => new()
    {
        Id = Id,
        LegacyId = (int)PdfToolId.HtmlToPdf,
        Name = Name,
        Description = "Convert webpages or raw HTML files in seconds. Copy and paste the URL or drop local HTML files.",
        Category = "ConvertToPdf",
        IconKind = "LanguageHtml5",
        IconColorHex = "#E11D48",
        BackgroundAccentHex = "#FFF1F2",
        SupportsMultiFile = false,
        AcceptedFileExtensions = ".html,.htm",
        CreateViewModel = sp => ActivatorUtilities.CreateInstance<HtmlToPdfToolViewModel>(sp)
    };
}

public class PdfToMarkdownToolPlugin : ToolPluginBase
{
    public override string Id => "frypdf.tool.pdftomarkdown";
    public override string Name => "PDF to Markdown";

    protected override PdfToolDescriptor CreateDescriptor() => new()
    {
        Id = Id,
        LegacyId = (int)PdfToolId.PdfToMarkdown,
        Name = Name,
        Description = "Extract clean, readable Markdown content and structured tables from your PDF documents.",
        Category = "ConvertFromPdf",
        IconKind = "LanguageMarkdownOutline",
        IconColorHex = "#0284C7",
        BackgroundAccentHex = "#F0F9FF",
        SupportsMultiFile = true,
        AcceptedFileExtensions = ".pdf",
        CreateViewModel = sp => ActivatorUtilities.CreateInstance<PdfToMarkdownToolViewModel>(sp)
    };
}

public class PdfToPdfAToolPlugin : ToolPluginBase
{
    public override string Id => "frypdf.tool.pdftopdfa";
    public override string Name => "PDF to PDF/A";

    protected override PdfToolDescriptor CreateDescriptor() => new()
    {
        Id = Id,
        LegacyId = (int)PdfToolId.PdfToPdfA,
        Name = Name,
        Description = "Transform your PDF to PDF/A, the ISO-standardized version of PDF for long-term digital preservation.",
        Category = "ConvertFromPdf",
        IconKind = "ArchiveOutline",
        IconColorHex = "#0D9488",
        BackgroundAccentHex = "#F0FDFA",
        SupportsMultiFile = true,
        AcceptedFileExtensions = ".pdf",
        CreateViewModel = sp => ActivatorUtilities.CreateInstance<PdfToPdfAToolViewModel>(sp)
    };
}

public class ScanToPdfToolPlugin : ToolPluginBase
{
    public override string Id => "frypdf.tool.scantopdf";
    public override string Name => "Scan to PDF";

    protected override PdfToolDescriptor CreateDescriptor() => new()
    {
        Id = Id,
        LegacyId = (int)PdfToolId.ScanToPdf,
        Name = Name,
        Description = "Capture document photos or connect a scanner to convert physical paperwork directly into PDF.",
        Category = "ConvertToPdf",
        IconKind = "Scanner",
        IconColorHex = "#4F46E5",
        BackgroundAccentHex = "#EEF2FF",
        SupportsMultiFile = false,
        AcceptedFileExtensions = ".jpg,.jpeg,.png,.bmp,.tiff",
        CreateViewModel = sp => ActivatorUtilities.CreateInstance<ScanToPdfToolViewModel>(sp)
    };
}
