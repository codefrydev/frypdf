using System;
using System.Collections.Generic;
using PdfEditorApp.Services;

namespace PdfEditorApp.Models;

public enum PdfToolId
{
    MergePdf,
    SplitPdf,
    CompressPdf,
    PdfToWord,
    PdfToPowerPoint,
    PdfToExcel,
    WordToPdf,
    PowerPointToPdf,
    ExcelToPdf,
    EditPdf,
    PdfToJpg,
    JpgToPdf,
    SignPdf,
    Watermark,
    RotatePdf,
    HtmlToPdf,
    UnlockPdf,
    ProtectPdf,
    OrganizePdf,
    PdfToPdfA,
    RepairPdf,
    PageNumbers,
    ScanToPdf,
    OcrPdf,
    ComparePdf,
    RedactPdf,
    CropPdf,
    PdfForms,
    AiSummarizer,
    TranslatePdf,
    PdfToMarkdown,
    WorkflowBuilder
}

public enum PdfToolCategory
{
    All,
    OrganizeAndPage,
    OptimizeAndSecurity,
    ConvertFromPdf,
    ConvertToPdf,
    EditAndForms,
    AiAndAutomation
}

public enum PdfCompressionLevel
{
    MaximumQuality,
    HighQuality,
    Balanced,
    SmallSize,
    MaximumCompression
}

public enum PdfAStandard
{
    PdfA1b,
    PdfA2b,
    PdfA3b
}

public enum WatermarkType
{
    Text,
    Image
}

public enum WatermarkPosition
{
    Center,
    TopLeft,
    TopCenter,
    TopRight,
    BottomLeft,
    BottomCenter,
    BottomRight,
    Tiled
}

public enum PageNumberPosition
{
    TopLeft,
    TopCenter,
    TopRight,
    BottomLeft,
    BottomCenter,
    BottomRight
}

public enum PageTargetSelection
{
    AllPages,
    OddPagesOnly,
    EvenPagesOnly,
    CustomRange
}

public class PdfToolDefinition
{
    public PdfToolId Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public PdfToolCategory Category { get; set; }
    public string CategoryDisplayName => Category switch
    {
        PdfToolCategory.OrganizeAndPage => "Organize & Page",
        PdfToolCategory.OptimizeAndSecurity => "Optimize & Security",
        PdfToolCategory.ConvertFromPdf => "Convert from PDF",
        PdfToolCategory.ConvertToPdf => "Convert to PDF",
        PdfToolCategory.EditAndForms => "Edit & Forms",
        PdfToolCategory.AiAndAutomation => "AI & Automation",
        _ => "All Tools"
    };

    public string IconKind { get; set; } = "FileDocumentOutline";
    public string IconColorHex { get; set; } = "#0F6CBD";
    public string BackgroundAccentHex { get; set; } = "#EFF6FF";
    public bool IsNew { get; set; }
    public bool IsWorkflowBanner { get; set; }
    public string AcceptedFileExtensions { get; set; } = ".pdf";
    public bool SupportsMultiFile { get; set; }
}

public record ToolExecutionResult
{
    public bool Success { get; init; }
    public string? OutputFilePath { get; init; }
    public List<string> OutputFiles { get; init; } = new();
    public string? Message { get; init; }
    public string? ErrorMessage { get; init; }
    public long OriginalSizeBytes { get; init; }
    public long OutputSizeBytes { get; init; }
    public double SavingsPercentage => OriginalSizeBytes > 0 && OutputSizeBytes > 0
        ? Math.Max(0, (OriginalSizeBytes - OutputSizeBytes) / (double)OriginalSizeBytes * 100.0)
        : 0;
    public Dictionary<string, object> ExtraData { get; init; } = new();
}

public class MergeToolOptions
{
    public List<string> InputFiles { get; set; } = new();
    public string OutputFilePath { get; set; } = string.Empty;
    public bool PreserveBookmarks { get; set; } = true;
    public bool NormalizePageSizes { get; set; }
}

public class SplitToolOptions
{
    public string InputFilePath { get; set; } = string.Empty;
    public string OutputDirectory { get; set; } = string.Empty;
    public SplitExtractMode Mode { get; set; } = SplitExtractMode.SplitEveryNPages;
    public int PagesPerSplit { get; set; } = 1;
    public string RangeExpression { get; set; } = "1-3, 5, 7-10";
    public bool SplitOddEven { get; set; }
    public bool ExtractOddPages { get; set; }
}

public class CompressToolOptions
{
    public string InputFilePath { get; set; } = string.Empty;
    public string OutputFilePath { get; set; } = string.Empty;
    public PdfCompressionLevel Level { get; set; } = PdfCompressionLevel.Balanced;
    public int ImageQualityDpi { get; set; } = 150;
    public int JpegQuality { get; set; } = 0; // 0 = automatic based on level, or 20-100
    public int MaxImageDimension { get; set; } = 0; // 0 = automatic based on level
    public bool ConvertToGrayscale { get; set; }
    public bool RemoveMetadata { get; set; }
    public bool RemoveDuplicateObjects { get; set; } = true;
    public bool CompressStreams { get; set; } = true;
}

public class WordConversionOptions
{
    public string InputFilePath { get; set; } = string.Empty;
    public string OutputFilePath { get; set; } = string.Empty;
    public bool ExtractTables { get; set; } = true;
    public bool ExtractImages { get; set; } = true;
    public bool OcrFallback { get; set; }
}

public class ExcelConversionOptions
{
    public string InputFilePath { get; set; } = string.Empty;
    public string OutputFilePath { get; set; } = string.Empty;
    public bool DetectAllTables { get; set; } = true;
    public bool SeparateSheetsPerPage { get; set; }
}

public class PptxConversionOptions
{
    public string InputFilePath { get; set; } = string.Empty;
    public string OutputFilePath { get; set; } = string.Empty;
    public bool EditableText { get; set; } = true;
}

public class OfficeToPdfOptions
{
    public string InputFilePath { get; set; } = string.Empty;
    public string OutputFilePath { get; set; } = string.Empty;
    public PageOrientation Orientation { get; set; } = PageOrientation.Portrait;
}

public class ImageConversionOptions
{
    public string InputFilePath { get; set; } = string.Empty;
    public string OutputDirectory { get; set; } = string.Empty;
    public string OutputFormat { get; set; } = "jpg"; // jpg, png
    public int Dpi { get; set; } = 300;
    public int JpgQuality { get; set; } = 90;
    public bool Grayscale { get; set; }
    public PageTargetSelection TargetPages { get; set; } = PageTargetSelection.AllPages;
    public string PageRange { get; set; } = "1";
}

public class ImagesToPdfOptions
{
    public List<string> ImageFiles { get; set; } = new();
    public string OutputFilePath { get; set; } = string.Empty;
    public PageFormat PageFormat { get; set; } = PageFormat.A4;
    public PageOrientation Orientation { get; set; } = PageOrientation.Portrait;
    public bool AutoOrientation { get; set; } = true;
    public bool FitToPage { get; set; } = true;
    public double MarginPoints { get; set; } = 20;
    public int ImagesPerPage { get; set; } = 1; // 1, 2, 4
}

public class SignToolOptions
{
    public string InputFilePath { get; set; } = string.Empty;
    public string OutputFilePath { get; set; } = string.Empty;
    public SignatureStyle Style { get; set; } = SignatureStyle.CursiveElegance;
    public string SignerName { get; set; } = "Jane Doe";
    public string? Reason { get; set; } = "Document Approved and Verified";
    public string? Location { get; set; }
    public string? CertificatePath { get; set; }
    public string? CertificatePassword { get; set; }
    public int TargetPageNumber { get; set; } = 1;
    public double X { get; set; } = 100;
    public double Y { get; set; } = 600;
    public double Width { get; set; } = 200;
    public double Height { get; set; } = 70;
    public string? SignatureImageDataUri { get; set; }
}

public class WatermarkToolOptions
{
    public string InputFilePath { get; set; } = string.Empty;
    public string OutputFilePath { get; set; } = string.Empty;
    public WatermarkType Type { get; set; } = WatermarkType.Text;
    public string Text { get; set; } = "CONFIDENTIAL";
    public string? ImagePath { get; set; }
    public string FontFamily { get; set; } = "Helvetica";
    public double FontSize { get; set; } = 48;
    public string ColorHex { get; set; } = "#EF4444";
    public double Opacity { get; set; } = 0.35;
    public double RotationAngle { get; set; } = -45;
    public WatermarkPosition Position { get; set; } = WatermarkPosition.Center;
    public PageTargetSelection TargetPages { get; set; } = PageTargetSelection.AllPages;
    public string CustomRange { get; set; } = "";
    public bool LayerOnTop { get; set; } = true;
}

public class RotateToolOptions
{
    public string InputFilePath { get; set; } = string.Empty;
    public string OutputFilePath { get; set; } = string.Empty;
    public int RotationDegrees { get; set; } = 90; // 90, 180, 270
    public PageFilterTarget TargetFilter { get; set; } = PageFilterTarget.All;
    public string CustomRange { get; set; } = "";
}

public class HtmlToPdfOptions
{
    public string HtmlContentOrUrl { get; set; } = string.Empty;
    public bool IsUrl { get; set; }
    public string OutputFilePath { get; set; } = string.Empty;
    public PageFormat Format { get; set; } = PageFormat.A4;
    public PageOrientation Orientation { get; set; } = PageOrientation.Portrait;
    public double MarginPoints { get; set; } = 36;
    public bool IncludePageNumbers { get; set; } = true;
}

public class SecurityToolOptions
{
    public string InputFilePath { get; set; } = string.Empty;
    public string OutputFilePath { get; set; } = string.Empty;
    public string UserPassword { get; set; } = string.Empty;
    public string OwnerPassword { get; set; } = string.Empty;
    public bool AllowPrinting { get; set; } = true;
    public bool AllowModifying { get; set; } = false;
    public bool AllowCopying { get; set; } = false;
    public bool AllowAnnotating { get; set; } = true;
    public bool AllowFormFilling { get; set; } = true;
}

public class UnlockToolOptions
{
    public string InputFilePath { get; set; } = string.Empty;
    public string OutputFilePath { get; set; } = string.Empty;
    public string Password { get; set; } = string.Empty;
}

public class PageOrganizeAction
{
    public int SourcePageIndex { get; set; }
    public int RotationDegrees { get; set; }
    public bool Delete { get; set; }
    public bool Duplicate { get; set; }
    public string? InsertPdfFilePath { get; set; }
    public int? InsertPdfPageIndex { get; set; }
}

public class OrganizeToolOptions
{
    public string InputFilePath { get; set; } = string.Empty;
    public string OutputFilePath { get; set; } = string.Empty;
    public List<int> PageOrder { get; set; } = new(); // 0-based page indices in target order
    public List<int> PagesToDelete { get; set; } = new();
    public Dictionary<int, int> PageRotations { get; set; } = new(); // pageIndex -> angle
}

public class PdfAToolOptions
{
    public string InputFilePath { get; set; } = string.Empty;
    public string OutputFilePath { get; set; } = string.Empty;
    public PdfAStandard Standard { get; set; } = PdfAStandard.PdfA2b;
}

public class RepairToolOptions
{
    public string InputFilePath { get; set; } = string.Empty;
    public string OutputFilePath { get; set; } = string.Empty;
}

public class PageNumberToolOptions
{
    public string InputFilePath { get; set; } = string.Empty;
    public string OutputFilePath { get; set; } = string.Empty;
    public PageNumberPosition Position { get; set; } = PageNumberPosition.BottomCenter;
    public string Template { get; set; } = "Page {n} of {total}"; // e.g. "Page {n} of {total}", "{n}", "Bates: DOC-{n:D6}"
    public int StartingNumber { get; set; } = 1;
    public string FontFamily { get; set; } = "Helvetica";
    public double FontSize { get; set; } = 10;
    public string ColorHex { get; set; } = "#334155";
    public PageTargetSelection TargetPages { get; set; } = PageTargetSelection.AllPages;
    public string CustomRange { get; set; } = "";
    public double MarginPoints { get; set; } = 28;
}

public class ScanToolOptions
{
    public List<string> InputImageFiles { get; set; } = new();
    public string OutputFilePath { get; set; } = string.Empty;
    public bool AutoDeskew { get; set; } = true;
    public bool EnhanceContrast { get; set; } = true;
    public bool WhitenBackground { get; set; } = true;
    public bool ConvertToGrayscale { get; set; }
    public PageFormat Format { get; set; } = PageFormat.A4;
}

public class OcrToolOptions
{
    public string InputFilePath { get; set; } = string.Empty;
    public string OutputFilePath { get; set; } = string.Empty;
    public string Language { get; set; } = "eng"; // eng, spa, deu, fra, etc.
    public bool GenerateSearchablePdf { get; set; } = true;
    public bool ExtractTextOnly { get; set; }
}

public class CompareToolOptions
{
    public string DocumentAPath { get; set; } = string.Empty;
    public string DocumentBPath { get; set; } = string.Empty;
    public bool DetectTextDiff { get; set; } = true;
    public bool DetectVisualDiff { get; set; } = true;
}

public class RedactionRegion
{
    public int PageIndex { get; set; }
    public double X { get; set; }
    public double Y { get; set; }
    public double Width { get; set; }
    public double Height { get; set; }
    public string Reason { get; set; } = "CONFIDENTIAL";
    public string FillColorHex { get; set; } = "#000000";
}

public class RedactionToolOptions
{
    public string InputFilePath { get; set; } = string.Empty;
    public string OutputFilePath { get; set; } = string.Empty;
    public List<RedactionRegion> Regions { get; set; } = new();
    public string? SearchPatternToRedact { get; set; }
    public bool CaseSensitive { get; set; }
    public bool PermanentScrubText { get; set; } = true;
}

public class CropToolOptions
{
    public string InputFilePath { get; set; } = string.Empty;
    public string OutputFilePath { get; set; } = string.Empty;
    public double CropLeftPoints { get; set; }
    public double CropRightPoints { get; set; }
    public double CropTopPoints { get; set; }
    public double CropBottomPoints { get; set; }
    public PageTargetSelection TargetPages { get; set; } = PageTargetSelection.AllPages;
    public string CustomRange { get; set; } = "";
}

public class FormToolOptions
{
    public string InputFilePath { get; set; } = string.Empty;
    public string OutputFilePath { get; set; } = string.Empty;
    public Dictionary<string, string> FieldValues { get; set; } = new();
    public bool FlattenFields { get; set; }
    public bool ExportFieldValuesJson { get; set; }
}

public class AiSummaryOptions
{
    public string InputFilePath { get; set; } = string.Empty;
    public int MaxBulletPoints { get; set; } = 7;
    public bool IncludeExecutiveSummary { get; set; } = true;
    public bool IncludeActionItems { get; set; } = true;
    public string? CustomPrompt { get; set; }
    public string TargetLanguage { get; set; } = "English";
}

public class TranslationOptions
{
    public string InputFilePath { get; set; } = string.Empty;
    public string OutputFilePath { get; set; } = string.Empty;
    public string SourceLanguage { get; set; } = "Auto";
    public string TargetLanguage { get; set; } = "Spanish"; // Spanish, French, German, Japanese, etc.
    public bool PreserveLayout { get; set; } = true;
}

public class MarkdownConversionOptions
{
    public string InputFilePath { get; set; } = string.Empty;
    public string OutputFilePath { get; set; } = string.Empty;
    public bool IncludeTables { get; set; } = true;
    public bool IncludeImages { get; set; } = true;
    public bool IncludeMetadataHeader { get; set; } = true;
}
