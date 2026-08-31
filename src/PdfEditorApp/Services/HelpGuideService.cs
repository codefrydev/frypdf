using System;
using System.Collections.Generic;
using System.Linq;
using PdfEditorApp.Models;

namespace PdfEditorApp.Services;

/// <summary>
/// Comprehensive, offline-first service providing step-by-step guides, pro-tips, key features, and specifications
/// for all 32 PDF tools, the live canvas document editor, automation workflows, batch generation, and international typography.
/// </summary>
public class HelpGuideService : IHelpGuideService
{
    private readonly List<HelpGuideItem> _guides;
    private readonly List<string> _categories;

    public HelpGuideService()
    {
        _guides = new List<HelpGuideItem>
        {
            // =========================================================================
            // 1. GETTING STARTED & CORE BASICS
            // =========================================================================
            new HelpGuideItem
            {
                Id = "getting-started",
                Title = "Welcome to FryPDF Studio",
                Category = "Getting Started",
                Summary = "Learn the basics of FryPDF: a privacy-first, ultra-fast PDF creation and manipulation studio.",
                Description = "FryPDF is an all-in-one PDF powerhouse engineered with .NET 10, Avalonia UI, QuestPDF, and SkiaSharp. Everything runs 100% locally on your machine—no cloud uploads, no subscriptions, and total data privacy.",
                Steps = new List<string>
                {
                    "Launch FryPDF to arrive at the Home Dashboard featuring quick actions, recent files, and 32 specialized PDF tools.",
                    "Choose 'New Document' to start from a blank canvas or pick from 19+ professionally designed templates.",
                    "Use 'Open File' or drag and drop any .frypdf, .pdf, or .json file to view or edit existing documents.",
                    "Press ⌘K (or Ctrl+K on Windows/Linux) anytime to bring up the Quick Command Palette to launch tools, search guides, or insert elements.",
                    "Switch between Light and Dark themes anytime using the toggle button in the bottom-left sidebar."
                },
                KeyFeatures = new List<string>
                {
                    "100% Offline & Private: Zero telemetry or external server processing",
                    "Dual-Mode Studio: High-performance Acrobat-style Reader + Full Canvas Editor",
                    "32 Built-in Single & Multi-File PDF Utility Tools",
                    "Native .frypdf Workspace Persistence with embedded project states in exported PDFs"
                },
                ProTips = new List<string>
                {
                    "You can double-click any recent file from the Home Dashboard to open it immediately.",
                    "Press F1 or Shift+? at any point to view the keyboard shortcut cheat sheet."
                },
                SupportedFormats = "Native: .frypdf, .json | Standard: .pdf, .docx, .xlsx, .pptx, images",
                KeyboardShortcut = "⌘K / Ctrl+K (Command Palette)",
                IconKind = "RocketLaunchOutline",
                IconColorHex = "#0284C7",
                BackgroundAccentHex = "#E0F2FE",
                Badge = "Getting Started",
                Keywords = "intro start quickstart overview privacy offline mac windows linux theme darkmode",
                IsFeatured = true
            },
            new HelpGuideItem
            {
                Id = "pdf-reader-mode",
                Title = "PDF Reader & Acrobat Navigation",
                Category = "Getting Started",
                Summary = "Read and review PDF documents with continuous scroll, bookmark outline trees, and reading themes.",
                Description = "The dedicated PDF Reader mode allows you to read large documents comfortably with high-precision vector rendering, thumbnail previews, bookmark navigation, and eye-friendly color themes.",
                Steps = new List<string>
                {
                    "Click 'PDF Reader' in the left sidebar or select 'PDF Reader Mode' from the Home Dashboard.",
                    "Click 'Open PDF Document' or drag and drop any standard PDF file into the drop zone.",
                    "Use the sidebar to view page thumbnails or navigate via the interactive Document Outline tree.",
                    "Choose your preferred viewing theme: Default White, Eye-Comfort Sepia, Dark Night, or High Contrast.",
                    "Use the zoom slider (⌘+ / ⌘-) or quick buttons (Fit Width ⌘1, Fit Page ⌘9) to optimize your reading layout."
                },
                KeyFeatures = new List<string>
                {
                    "Multi-Theme Reading: Default, Sepia, Dark, High-Contrast",
                    "Continuous Vertical Scrolling and Single/Spread Page View Modes",
                    "Interactive Table of Contents & Bookmarks Navigation",
                    "In-Document Text Search with live match highlighting"
                },
                ProTips = new List<string>
                {
                    "Use the Sepia theme for long-form reading sessions to minimize eye fatigue.",
                    "Click the 'Edit in Canvas' button in the viewer toolbar to deconstruct and edit the PDF directly."
                },
                SupportedFormats = "Input: .pdf",
                KeyboardShortcut = "⌘1 (Fit Width), ⌘9 (Fit Page)",
                IconKind = "BookOpenPageVariantOutline",
                IconColorHex = "#DC2626",
                BackgroundAccentHex = "#FEE2E2",
                Badge = "Reader Mode",
                Keywords = "reader read pdf viewer sepia dark outline bookmarks scroll pages zoom",
                IsFeatured = true
            },
            new HelpGuideItem
            {
                Id = "templates-guide",
                Title = "Template Studio & Document Gallery",
                Category = "Getting Started",
                Summary = "Jumpstart your documents with 19+ curated templates including Invoices, Resumes, and Academic Papers.",
                Description = "The Template Studio provides a rich library of pre-formatted, production-ready templates. Every template is fully editable down to each text block, logo, color palette, and vector element.",
                Steps = new List<string>
                {
                    "Select 'New Document' from the left sidebar to enter the Template Gallery.",
                    "Filter templates by category: Business, Finance, Academic & Research, Certificates, Invitations, or General.",
                    "Click on any template card to instantly instantiate and open it inside the live Canvas Editor.",
                    "Double-click any text box to replace sample copy with your own information.",
                    "Export your finished document using ⌘E (Export to PDF)."
                },
                KeyFeatures = new List<string>
                {
                    "19+ High-Quality Templates: Invoices, Executive CVs, Discrete Math Research, Certificates, Art Deco Gala Invitations",
                    "Pre-configured typography, color schemes, and vector embellishments",
                    "Instant blank document creation with standard paper sizes (A4, Letter, Legal, Tabloid)"
                },
                ProTips = new List<string>
                {
                    "Academic and math templates include pre-rendered LaTeX formulas and 2-column scientific grids.",
                    "All templates automatically detect your system fonts and fallback gracefully to embedded Noto Sans families."
                },
                SupportedFormats = "Output: .frypdf, .pdf",
                KeyboardShortcut = "⌘N / Ctrl+N (New Document)",
                IconKind = "ViewDashboardOutline",
                IconColorHex = "#10B981",
                BackgroundAccentHex = "#ECFDF5",
                Badge = "Templates",
                Keywords = "templates gallery new invoice resume cv academic certificate letter formal blank",
                IsFeatured = true
            },

            // =========================================================================
            // 2. ALL 32 PDF TOOLS (ORGANIZE, OPTIMIZE, CONVERT, EDIT, AI & AUTOMATION)
            // =========================================================================
            new HelpGuideItem
            {
                Id = "tool-merge",
                Title = "Merge PDF Files",
                Category = "32 PDF Tools",
                Summary = "Combine multiple PDF documents into a single unified file in custom order.",
                Description = "The Merge PDF tool combines two or more PDF files into a single, cohesive document. You can drag and drop multiple files, reorder them with up/down arrows, and compile them with a single click.",
                Steps = new List<string>
                {
                    "Open 'Merge PDF' from the Tools Studio or press ⌘K and type 'Merge PDF'.",
                    "Click 'Add PDF Files' or drag and drop your PDFs into the file staging area.",
                    "Reorder files using the move up/down controls or remove any unwanted files.",
                    "Specify your desired output filename or leave default.",
                    "Click 'Merge PDFs' to generate your combined PDF in seconds."
                },
                KeyFeatures = new List<string>
                {
                    "Batch multi-file support with visual reordering",
                    "Preserves internal bookmarks, links, and high-resolution images",
                    "Generates compliant, optimized output with zero quality loss"
                },
                ProTips = new List<string>
                {
                    "You can drag and drop dozens of PDF files at once directly onto the tool page.",
                    "After merging, you can open the result in the PDF Viewer or Editor with one click."
                },
                SupportedFormats = "Inputs: Multiple .pdf | Output: Single .pdf",
                RelatedToolId = PdfToolId.MergePdf,
                IconKind = "CallMerge",
                IconColorHex = "#EA580C",
                BackgroundAccentHex = "#FFF7ED",
                Badge = "Organize",
                Keywords = "merge combine join pdfs bind assemble files batch",
                IsFeatured = true
            },
            new HelpGuideItem
            {
                Id = "tool-split",
                Title = "Split PDF Pages & Ranges",
                Category = "32 PDF Tools",
                Summary = "Extract specific page ranges, split by intervals, or separate into individual page files.",
                Description = "The Split PDF tool allows you to divide a large PDF into smaller standalone documents by custom page ranges, fixed page intervals, or individual single pages.",
                Steps = new List<string>
                {
                    "Open 'Split PDF' from the Tools Studio.",
                    "Select or drop your source PDF document.",
                    "Choose your split mode: 'Custom Ranges' (e.g. 1-3, 5, 8-10), 'Fixed Intervals' (e.g. every 2 pages), or 'All Pages'.",
                    "Review the output file preview list.",
                    "Click 'Split PDF' to export your new files to the chosen directory."
                },
                KeyFeatures = new List<string>
                {
                    "Custom page range syntax (e.g. '1-4, 7, 9-12')",
                    "Burst mode: Extract each page as a distinct single-page PDF",
                    "Odd / Even page split support"
                },
                ProTips = new List<string>
                {
                    "Use comma-separated ranges to create multiple output files in a single pass.",
                    "All extracted files retain full vector quality and embedded fonts."
                },
                SupportedFormats = "Input: .pdf | Output: Multiple .pdf files",
                RelatedToolId = PdfToolId.SplitPdf,
                IconKind = "CallSplit",
                IconColorHex = "#DC2626",
                BackgroundAccentHex = "#FEF2F2",
                Badge = "Organize",
                Keywords = "split extract divide separate pages ranges interval burst",
                IsFeatured = true
            },
            new HelpGuideItem
            {
                Id = "tool-compress",
                Title = "Compress & Optimize PDF",
                Category = "32 PDF Tools",
                Summary = "Reduce PDF file sizes dramatically while retaining crisp vector typography and clear images.",
                Description = "Compress PDF analyzes embedded image streams, strips duplicate metadata, and applies optimized stream compression algorithms to drastically shrink file sizes for email sharing and web publishing.",
                Steps = new List<string>
                {
                    "Open 'Compress PDF' from the Tools Studio.",
                    "Select the PDF file(s) you wish to shrink.",
                    "Select your compression level: 'Recommended' (best balance), 'Extreme' (smallest size), or 'Light' (maximal fidelity).",
                    "Click 'Compress PDF'.",
                    "Inspect the before/after file size summary and percentage savings."
                },
                KeyFeatures = new List<string>
                {
                    "3 Compression Presets: Extreme, Recommended, and Light",
                    "Intelligent Skia image resampling and DCT quality tuning",
                    "Removes redundant stream objects and unreferenced fonts"
                },
                ProTips = new List<string>
                {
                    "Scanned image-heavy PDFs often see 70%–90% file size reduction with Recommended compression.",
                    "Pure vector documents with text remain razor-sharp even under extreme compression."
                },
                SupportedFormats = "Input: .pdf | Output: Optimized .pdf",
                RelatedToolId = PdfToolId.CompressPdf,
                IconKind = "ArrowCollapseAll",
                IconColorHex = "#16A34A",
                BackgroundAccentHex = "#F0FDF4",
                Badge = "Optimize",
                Keywords = "compress shrink optimize reduce file size email small quality",
                IsFeatured = true
            },
            new HelpGuideItem
            {
                Id = "tool-pdf-to-word",
                Title = "PDF to Word (DOCX)",
                Category = "32 PDF Tools",
                Summary = "Convert PDF documents into fully editable Microsoft Word DOC and DOCX files.",
                Description = "Extracts text paragraphs, headings, bullet lists, formatting, and tables from PDF files into standard Microsoft Word DOCX documents compatible with Microsoft 365, Word 2016+, and LibreOffice.",
                Steps = new List<string>
                {
                    "Open 'PDF to Word' from the Convert from PDF category.",
                    "Choose your source PDF file.",
                    "Select formatting preservation options (maintain paragraph flow vs exact layout).",
                    "Click 'Convert to Word'.",
                    "Open the generated .docx file in Microsoft Word or any compatible word processor."
                },
                KeyFeatures = new List<string>
                {
                    "Preserves paragraph structure, font styling, and colors",
                    "Extracts tables into native Word table grids",
                    "Embeds extracted images in their original positions"
                },
                ProTips = new List<string>
                {
                    "For scanned documents without selectable text, run 'OCR Text Recognition' first for best results."
                },
                SupportedFormats = "Input: .pdf | Output: .docx",
                RelatedToolId = PdfToolId.PdfToWord,
                IconKind = "FileWordOutline",
                IconColorHex = "#2563EB",
                BackgroundAccentHex = "#EFF6FF",
                Badge = "Convert",
                Keywords = "word doc docx convert export office microsoft word editable",
                IsFeatured = true
            },
            new HelpGuideItem
            {
                Id = "tool-pdf-to-excel",
                Title = "PDF to Excel (XLSX)",
                Category = "32 PDF Tools",
                Summary = "Extract tables, financial figures, and balance sheets from PDF into Microsoft Excel spreadsheets.",
                Description = "Extracts structured tabular data from PDF invoices, financial statements, and reports into real editable cells inside Microsoft Excel .xlsx workbooks.",
                Steps = new List<string>
                {
                    "Open 'PDF to Excel' from the Tools Studio.",
                    "Select the PDF containing tabular data.",
                    "Configure table detection sensitivity (Automatic or Strict Grid).",
                    "Click 'Convert to Excel'.",
                    "Open the generated .xlsx workbook in Microsoft Excel or Google Sheets."
                },
                KeyFeatures = new List<string>
                {
                    "Detects table columns and cell boundaries automatically",
                    "Preserves numeric formatting for immediate calculations",
                    "Creates separate worksheets per PDF page"
                },
                ProTips = new List<string>
                {
                    "Perfect for extracting transaction statements, product catalogs, and invoice line items."
                },
                SupportedFormats = "Input: .pdf | Output: .xlsx",
                RelatedToolId = PdfToolId.PdfToExcel,
                IconKind = "FileExcelOutline",
                IconColorHex = "#16A34A",
                BackgroundAccentHex = "#F0FDF4",
                Badge = "Convert",
                Keywords = "excel xlsx spreadsheet table financial data grid convert export",
                IsFeatured = false
            },
            new HelpGuideItem
            {
                Id = "tool-pdf-to-powerpoint",
                Title = "PDF to PowerPoint (PPTX)",
                Category = "32 PDF Tools",
                Summary = "Transform PDF presentations into editable Microsoft PowerPoint PPTX slide decks.",
                Description = "Converts each page of a PDF document into a slide in Microsoft PowerPoint, allowing you to edit text boxes, rearrange slides, and apply presentation animations.",
                Steps = new List<string>
                {
                    "Open 'PDF to PowerPoint' from the Tools Studio.",
                    "Select the PDF presentation deck.",
                    "Click 'Convert to PowerPoint'.",
                    "Save and open the resulting .pptx file in Microsoft PowerPoint or Keynote."
                },
                KeyFeatures = new List<string>
                {
                    "Maps each PDF page to an individual 16:9 or 4:3 slide",
                    "Extracts text blocks as movable slide text frames",
                    "Retains vector background graphics and slide illustrations"
                },
                ProTips = new List<string>
                {
                    "Ideal for repurposing exported webinar slide decks, pitch decks, and lecture handouts."
                },
                SupportedFormats = "Input: .pdf | Output: .pptx",
                RelatedToolId = PdfToolId.PdfToPowerPoint,
                IconKind = "FilePowerpointOutline",
                IconColorHex = "#EA580C",
                BackgroundAccentHex = "#FFF7ED",
                Badge = "Convert",
                Keywords = "powerpoint ppt pptx slides presentation deck convert export",
                IsFeatured = false
            },
            new HelpGuideItem
            {
                Id = "tool-word-to-pdf",
                Title = "Word to PDF (DOCX to PDF)",
                Category = "32 PDF Tools",
                Summary = "Compile Microsoft Word DOC and DOCX files into standardized, high-fidelity PDF documents.",
                Description = "Converts Microsoft Word documents into vector PDFs with consistent typography, crisp layout formatting, and embedded metadata.",
                Steps = new List<string>
                {
                    "Open 'Word to PDF' from the Convert to PDF category.",
                    "Select or drop your .docx file.",
                    "Click 'Convert to PDF'.",
                    "Open or export your compiled PDF."
                },
                KeyFeatures = new List<string>
                {
                    "Accurate font and margin translation",
                    "Embeds vector typography for universal cross-platform viewing",
                    "Fast batch processing support"
                },
                ProTips = new List<string>
                {
                    "Batch convert multiple .docx files in one click to standardize company document archives."
                },
                SupportedFormats = "Input: .docx, .doc | Output: .pdf",
                RelatedToolId = PdfToolId.WordToPdf,
                IconKind = "FileWord",
                IconColorHex = "#2563EB",
                BackgroundAccentHex = "#EFF6FF",
                Badge = "Convert",
                Keywords = "word to pdf docx doc compile convert document import",
                IsFeatured = false
            },
            new HelpGuideItem
            {
                Id = "tool-excel-to-pdf",
                Title = "Excel to PDF (XLSX to PDF)",
                Category = "32 PDF Tools",
                Summary = "Convert Excel spreadsheets and tables into cleanly formatted, printable PDF documents.",
                Description = "Renders Microsoft Excel worksheets into paginated, publication-grade PDF documents with clean gridlines and fit-to-page scaling.",
                Steps = new List<string>
                {
                    "Open 'Excel to PDF' from the Tools Studio.",
                    "Select your .xlsx or .xls workbook.",
                    "Choose page orientation (Landscape or Portrait) and scaling options.",
                    "Click 'Convert to PDF'."
                },
                KeyFeatures = new List<string>
                {
                    "Automatic column width fitting and table boundary alignment",
                    "Preserves table borders, number formats, and header colors",
                    "Multi-sheet workbook support"
                },
                ProTips = new List<string>
                {
                    "Select Landscape orientation for spreadsheets with more than 6 columns to prevent horizontal clipping."
                },
                SupportedFormats = "Input: .xlsx, .xls | Output: .pdf",
                RelatedToolId = PdfToolId.ExcelToPdf,
                IconKind = "FileExcel",
                IconColorHex = "#16A34A",
                BackgroundAccentHex = "#F0FDF4",
                Badge = "Convert",
                Keywords = "excel to pdf xlsx xls spreadsheet table sheet convert",
                IsFeatured = false
            },
            new HelpGuideItem
            {
                Id = "tool-powerpoint-to-pdf",
                Title = "PowerPoint to PDF (PPTX to PDF)",
                Category = "32 PDF Tools",
                Summary = "Convert PowerPoint presentations into high-resolution, presentation-ready PDF slide decks.",
                Description = "Compiles PPTX slide decks into crisp, universally shareable PDF handouts and presentation documents.",
                Steps = new List<string>
                {
                    "Open 'PowerPoint to PDF' from the Tools Studio.",
                    "Select your .pptx presentation.",
                    "Click 'Convert to PDF'.",
                    "Review the generated PDF slides in the viewer."
                },
                KeyFeatures = new List<string>
                {
                    "Preserves slide dimensions (16:9 widescreen or 4:3 standard)",
                    "Retains slide backgrounds, vector shapes, and diagrams",
                    "High-resolution vector output"
                },
                ProTips = new List<string>
                {
                    "Use this tool to create unalterable PDF handouts for clients and conference attendees."
                },
                SupportedFormats = "Input: .pptx, .ppt | Output: .pdf",
                RelatedToolId = PdfToolId.PowerPointToPdf,
                IconKind = "FilePowerpoint",
                IconColorHex = "#EA580C",
                BackgroundAccentHex = "#FFF7ED",
                Badge = "Convert",
                Keywords = "powerpoint to pdf pptx ppt presentation slides convert",
                IsFeatured = false
            },
            new HelpGuideItem
            {
                Id = "tool-edit-pdf",
                Title = "Live PDF Deconstruction & Editing",
                Category = "32 PDF Tools",
                Summary = "Deconstruct any existing 3rd-party PDF into an editable canvas with live text, images, and shapes.",
                Description = "FryPDF's breakthrough deconstruction engine parses raw PDF streams, performs layout and typography analysis, detects Unicode scripts, and converts static PDF pages into fully editable, layered canvas elements.",
                Steps = new List<string>
                {
                    "Open 'Edit PDF' from the Tools Studio or drag and drop any PDF into the editor.",
                    "The deconstruction engine analyzes text bounding boxes, font metrics, and embedded graphics.",
                    "Click any text element to edit its content, font size, weight, and color directly.",
                    "Move, resize, or delete shapes, logos, and images on the visual canvas.",
                    "Save as a .frypdf project file or export directly back to vector PDF via ⌘E."
                },
                KeyFeatures = new List<string>
                {
                    "Intelligent Line & Paragraph Clustering with baseline tracking",
                    "Multi-Format Skia Image Extraction (DCT, JPEG2000, CMYK, PNG)",
                    "Script-Aware Font Fallback (Devanagari, CJK, Arabic, Latin)",
                    "Strict Layered Z-Index Allocation preventing occlusion"
                },
                ProTips = new List<string>
                {
                    "FryPDF preserves exact coordinates and typography so your edits look indistinguishable from the original.",
                    "Use the Properties Inspector (⌘⇧P) to fine-tune letter spacing, line height multipliers, and padding."
                },
                SupportedFormats = "Inputs: .pdf, .frypdf, .json | Output: .pdf, .frypdf",
                RelatedToolId = PdfToolId.EditPdf,
                IconKind = "Draw",
                IconColorHex = "#9333EA",
                BackgroundAccentHex = "#FAF5FF",
                Badge = "Editor",
                Keywords = "edit deconstruct modify alter text images replace delete change pdf",
                IsFeatured = true
            },
            new HelpGuideItem
            {
                Id = "tool-pdf-to-jpg",
                Title = "PDF to JPG & PNG Images",
                Category = "32 PDF Tools",
                Summary = "Render each page of a PDF into high-resolution JPG or PNG images, or extract all embedded photos.",
                Description = "Converts PDF pages into standalone image files at selectable DPI settings (72 DPI, 150 DPI, 300 DPI print grade), or extracts raw embedded images from inside the document.",
                Steps = new List<string>
                {
                    "Open 'PDF to JPG' from the Tools Studio.",
                    "Select the PDF file.",
                    "Choose mode: 'Render Entire Pages' or 'Extract Embedded Images Only'.",
                    "Select target image format (JPG or PNG) and resolution DPI.",
                    "Click 'Convert to Images' to export to a dedicated folder."
                },
                KeyFeatures = new List<string>
                {
                    "High-Fidelity SkiaSharp Raster Engine with sub-pixel antialiasing",
                    "Supports 72, 150, and 300 DPI output resolutions",
                    "Lossless PNG or compressed JPEG formats"
                },
                ProTips = new List<string>
                {
                    "Choose 300 DPI PNG when generating images intended for print catalogs or marketing materials."
                },
                SupportedFormats = "Input: .pdf | Output: .jpg, .png",
                RelatedToolId = PdfToolId.PdfToJpg,
                IconKind = "FileImageOutline",
                IconColorHex = "#EAB308",
                BackgroundAccentHex = "#FEFCE8",
                Badge = "Convert",
                Keywords = "jpg png image photo picture render extract dpi export",
                IsFeatured = false
            },
            new HelpGuideItem
            {
                Id = "tool-images-to-pdf",
                Title = "Images to PDF (JPG/PNG to PDF)",
                Category = "32 PDF Tools",
                Summary = "Combine multiple JPG, PNG, WebP, and BMP images into a single bound PDF document.",
                Description = "Import one or multiple photos, scans, and graphic files, arrange their order, configure page orientation and margins, and compile them into a clean PDF album or portfolio.",
                Steps = new List<string>
                {
                    "Open 'Images to PDF' from the Tools Studio.",
                    "Click 'Add Images' or drag and drop image files into the window.",
                    "Drag images to arrange their page sequence.",
                    "Select page sizing (Fit Image, A4, or Letter) and margin settings.",
                    "Click 'Convert to PDF'."
                },
                KeyFeatures = new List<string>
                {
                    "Supports JPG, PNG, WebP, BMP, and GIF formats",
                    "Automatic orientation matching (portrait vs landscape per image)",
                    "Custom margin options: None, Small, or Standard"
                },
                ProTips = new List<string>
                {
                    "Set margins to 'None' for full-bleed photo albums and graphic presentations."
                },
                SupportedFormats = "Inputs: .jpg, .jpeg, .png, .webp, .bmp | Output: .pdf",
                RelatedToolId = PdfToolId.JpgToPdf,
                IconKind = "FileImagePlusOutline",
                IconColorHex = "#EAB308",
                BackgroundAccentHex = "#FEFCE8",
                Badge = "Convert",
                Keywords = "images to pdf photos jpg png picture album scan combine convert",
                IsFeatured = false
            },
            new HelpGuideItem
            {
                Id = "tool-sign-pdf",
                Title = "Digital Signature Studio",
                Category = "32 PDF Tools",
                Summary = "Add handwritten mouse/stylus signatures, typed calligraphy, or certificate stamps to PDFs.",
                Description = "The Signature Studio lets you create and place professional signatures on contracts, agreements, and forms. You can draw your signature, type it using elegant cursive typography, or upload a signature image.",
                Steps = new List<string>
                {
                    "Open 'Sign PDF' from the Tools Studio or launch Signature Studio from the editor ribbon.",
                    "Select your signature style: 'Draw' (handwritten with pen/mouse), 'Type' (calligraphy fonts), or 'Upload Image'.",
                    "Configure stroke thickness and ink color (Classic Black, Executive Navy Blue, or Crimson).",
                    "Position the signature box on the desired page and coordinates.",
                    "Click 'Apply Signature' to stamp the document."
                },
                KeyFeatures = new List<string>
                {
                    "Smooth vector Bézier smoothing for drawn signatures",
                    "Pre-styled executive script font options",
                    "Multi-page stamping and coordinate alignment"
                },
                ProTips = new List<string>
                {
                    "Executive Navy Blue ink (#0F2942) provides a distinctive authentic pen-and-paper look on formal legal agreements."
                },
                SupportedFormats = "Input: .pdf | Output: Signed .pdf",
                RelatedToolId = PdfToolId.SignPdf,
                IconKind = "DrawPen",
                IconColorHex = "#059669",
                BackgroundAccentHex = "#ECFDF5",
                Badge = "Security",
                Keywords = "sign signature e-sign contract agreement draw calligraphy legal ink",
                IsFeatured = true
            },
            new HelpGuideItem
            {
                Id = "tool-protect-pdf",
                Title = "Protect PDF (AES-256 Encryption)",
                Category = "32 PDF Tools",
                Summary = "Secure confidential PDFs with robust 256-bit AES passwords and granular access permissions.",
                Description = "Encrypts your PDF documents with industry-standard 256-bit AES encryption. You can set a Document Open password as well as a Permissions Master password to prevent unauthorized printing, copying, or modifying.",
                Steps = new List<string>
                {
                    "Open 'Protect PDF' from the Tools Studio.",
                    "Select the PDF you wish to encrypt.",
                    "Enter a strong Document Open Password.",
                    "Optionally set permissions: Prevent Printing, Prevent Content Copying, or Restrict Editing.",
                    "Click 'Encrypt & Protect PDF' to save your secured file."
                },
                KeyFeatures = new List<string>
                {
                    "256-bit AES Encryption (Adobe Acrobat Standard & PDF 2.0 compliant)",
                    "Granular permissions: Disallow copying, printing, or form alterations",
                    "Strong password strength indicator"
                },
                ProTips = new List<string>
                {
                    "Always store your encryption password securely; FryPDF encrypts locally and cannot recover forgotten passwords."
                },
                SupportedFormats = "Input: .pdf | Output: Encrypted .pdf",
                RelatedToolId = PdfToolId.ProtectPdf,
                IconKind = "LockOutline",
                IconColorHex = "#DC2626",
                BackgroundAccentHex = "#FEF2F2",
                Badge = "Security",
                Keywords = "protect password encrypt lock security aes permissions secure confidential",
                IsFeatured = true
            },
            new HelpGuideItem
            {
                Id = "tool-unlock-pdf",
                Title = "Unlock PDF (Remove Password)",
                Category = "32 PDF Tools",
                Summary = "Remove password protection and permission locks from authenticated PDF files.",
                Description = "Removes encryption and security restrictions from PDF files when you possess the authorization password, making the document freely editable and printable.",
                Steps = new List<string>
                {
                    "Open 'Unlock PDF' from the Tools Studio.",
                    "Select the encrypted PDF.",
                    "Enter the valid password.",
                    "Click 'Unlock PDF' to produce an unrestricted, open version of the document."
                },
                KeyFeatures = new List<string>
                {
                    "Removes open password and all editing/printing permission restrictions",
                    "Fast stream decryption with zero data corruption",
                    "Safe local processing"
                },
                ProTips = new List<string>
                {
                    "Once unlocked, you can freely edit, merge, or convert the document with any other FryPDF tool."
                },
                SupportedFormats = "Input: Password-Protected .pdf | Output: Unlocked .pdf",
                RelatedToolId = PdfToolId.UnlockPdf,
                IconKind = "LockOpenOutline",
                IconColorHex = "#16A34A",
                BackgroundAccentHex = "#F0FDF4",
                Badge = "Security",
                Keywords = "unlock decrypt remove password unprotect permissions open",
                IsFeatured = false
            },
            new HelpGuideItem
            {
                Id = "tool-rotate-pdf",
                Title = "Rotate PDF Pages",
                Category = "32 PDF Tools",
                Summary = "Rotate upside-down or sideways pages by 90°, 180°, or 270° degrees.",
                Description = "Fix misaligned, upside-down, or sideways scanned pages across the entire document or for selected individual pages.",
                Steps = new List<string>
                {
                    "Open 'Rotate PDF' from the Tools Studio.",
                    "Select the PDF file.",
                    "Choose rotation angle: 90° Clockwise, 180° Inverted, or 90° Counter-Clockwise.",
                    "Specify page selection: All Pages, Odd Pages Only, Even Pages Only, or Custom Range.",
                    "Click 'Apply Rotation' to save the corrected PDF."
                },
                KeyFeatures = new List<string>
                {
                    "Rotates page bounding boxes and graphics vectors losslessly",
                    "Supports selective odd/even or range-based rotation",
                    "Immediate visual thumbnail preview"
                },
                ProTips = new List<string>
                {
                    "Inside the live editor, you can also press ⌘⇧R (or Ctrl+Shift+R) to rotate the current active page."
                },
                SupportedFormats = "Input: .pdf | Output: Rotated .pdf",
                RelatedToolId = PdfToolId.RotatePdf,
                IconKind = "RotateRight",
                IconColorHex = "#2563EB",
                BackgroundAccentHex = "#EFF6FF",
                Badge = "Organize",
                Keywords = "rotate orientation angle clockwise landscape portrait upside down",
                IsFeatured = false
            },
            new HelpGuideItem
            {
                Id = "tool-add-watermark",
                Title = "Add Watermark",
                Category = "32 PDF Tools",
                Summary = "Stamp diagonal text or graphic logos across pages with customizable opacity and angle.",
                Description = "Apply prominent security watermarks such as 'CONFIDENTIAL', 'DRAFT', 'COPY', or custom company logos across all pages of a PDF document.",
                Steps = new List<string>
                {
                    "Open 'Watermark PDF' from the Tools Studio or click 'Watermark' in the editor ribbon.",
                    "Select watermark type: 'Text' or 'Image Logo'.",
                    "If text, type your message (e.g. 'CONFIDENTIAL — DO NOT DISTRIBUTE') and pick a font.",
                    "Adjust opacity (e.g. 15%–30%), rotation angle (e.g. 45° diagonal), and position.",
                    "Click 'Apply Watermark'."
                },
                KeyFeatures = new List<string>
                {
                    "Adjustable opacity slider (0%–100%)",
                    "Custom rotation angle and font sizing",
                    "Choice between background layer (behind text) or foreground overlay"
                },
                ProTips = new List<string>
                {
                    "A 20% opacity at a 45° angle provides high visibility without obscuring underlying document text."
                },
                SupportedFormats = "Input: .pdf | Output: Watermarked .pdf",
                RelatedToolId = PdfToolId.Watermark,
                IconKind = "Watermark",
                IconColorHex = "#0891B2",
                BackgroundAccentHex = "#ECFEFF",
                Badge = "Security",
                Keywords = "watermark stamp draft confidential copyright logo security overlay opacity",
                IsFeatured = false
            },
            new HelpGuideItem
            {
                Id = "tool-html-to-pdf",
                Title = "HTML to PDF",
                Category = "32 PDF Tools",
                Summary = "Convert HTML5 web pages, raw HTML markup, and CSS into standardized PDF documents.",
                Description = "Render web articles, invoices, receipts, and online reports directly from HTML files or URLs into cleanly formatted PDF pages.",
                Steps = new List<string>
                {
                    "Open 'HTML to PDF' from the Tools Studio.",
                    "Select an .html file or enter a web URL.",
                    "Configure page size (A4, Letter) and margin settings.",
                    "Click 'Convert to PDF'."
                },
                KeyFeatures = new List<string>
                {
                    "Full CSS3 styling, flexbox, and web font rendering",
                    "Automatic page break calculations",
                    "Retains embedded hyperlinks"
                },
                ProTips = new List<string>
                {
                    "Add CSS @media print styles to your HTML for perfect print layout control."
                },
                SupportedFormats = "Input: .html, .htm | Output: .pdf",
                RelatedToolId = PdfToolId.HtmlToPdf,
                IconKind = "LanguageHtml5",
                IconColorHex = "#EA580C",
                BackgroundAccentHex = "#FFF7ED",
                Badge = "Convert",
                Keywords = "html url web webpage css html to pdf compile convert",
                IsFeatured = false
            },
            new HelpGuideItem
            {
                Id = "tool-page-numbers",
                Title = "Add Page Numbers",
                Category = "32 PDF Tools",
                Summary = "Insert customizable page numbering headers and footers with custom formats and offsets.",
                Description = "Stamp running page numbers across PDF documents with flexible placement, format strings like 'Page {0} of {1}', Roman numerals, and starting offsets.",
                Steps = new List<string>
                {
                    "Open 'Add Page Numbers' from the Tools Studio.",
                    "Select the PDF file.",
                    "Choose position: Top/Bottom, Left/Center/Right.",
                    "Select numbering format: '1, 2, 3...', 'Page 1 of N', or 'I, II, III...'.",
                    "Optionally specify a starting page offset (e.g. skip cover page).",
                    "Click 'Apply Page Numbers'."
                },
                KeyFeatures = new List<string>
                {
                    "6 Placement Positions (Top/Bottom, Left/Center/Right)",
                    "Custom format strings (e.g. 'Page {0} of {1}' or 'Document Ref - Page {0}')",
                    "Option to skip cover or title pages"
                },
                ProTips = new List<string>
                {
                    "Set 'Start on Page' to 2 when your document includes a formal cover page."
                },
                SupportedFormats = "Input: .pdf | Output: Numbered .pdf",
                RelatedToolId = PdfToolId.PageNumbers,
                IconKind = "Numeric",
                IconColorHex = "#6366F1",
                BackgroundAccentHex = "#EEF2FF",
                Badge = "Organize",
                Keywords = "page numbers footer header numbering pagination count stamp",
                IsFeatured = false
            },
            new HelpGuideItem
            {
                Id = "tool-crop-pdf",
                Title = "Crop PDF Margins",
                Category = "32 PDF Tools",
                Summary = "Trim unwanted margins, white borders, and header/footer areas visually.",
                Description = "Adjust the visible page boundaries (CropBox) of a PDF to remove excess margins, scanner edges, or crop to specific content regions.",
                Steps = new List<string>
                {
                    "Open 'Crop PDF' from the Tools Studio.",
                    "Select the PDF document.",
                    "Drag the visual crop bounding box handles or enter exact margin dimensions in points.",
                    "Apply to current page or all pages.",
                    "Click 'Crop PDF'."
                },
                KeyFeatures = new List<string>
                {
                    "Interactive visual crop handle bounding box",
                    "Preserves vector text quality without re-rasterizing",
                    "Batch crop across all pages"
                },
                ProTips = new List<string>
                {
                    "Cropping adjusts the display boundary without destroying underlying vectors, ensuring crisp rendering."
                },
                SupportedFormats = "Input: .pdf | Output: Cropped .pdf",
                RelatedToolId = PdfToolId.CropPdf,
                IconKind = "Crop",
                IconColorHex = "#0284C7",
                BackgroundAccentHex = "#E0F2FE",
                Badge = "Organize",
                Keywords = "crop trim margins border cut resize bounding box",
                IsFeatured = false
            },
            new HelpGuideItem
            {
                Id = "tool-organize-pages",
                Title = "Organize & Reorder Pages",
                Category = "32 PDF Tools",
                Summary = "Visually drag and drop thumbnails to reorder, duplicate, rotate, or delete pages in a PDF.",
                Description = "Provides an interactive grid view of all pages in a document. Easily drag pages to resequence them, duplicate key slides, or delete accidental pages.",
                Steps = new List<string>
                {
                    "Open 'Organize Pages' from the Tools Studio.",
                    "Select your PDF to load the page thumbnail grid.",
                    "Click and drag any page thumbnail to change its position.",
                    "Hover over any page to access quick Rotate, Duplicate, or Delete icons.",
                    "Click 'Save Organized PDF' to export the reordered document."
                },
                KeyFeatures = new List<string>
                {
                    "High-resolution interactive visual thumbnail grid",
                    "Drag-and-drop page reordering",
                    "Per-page rotate, duplicate, and delete actions"
                },
                ProTips = new List<string>
                {
                    "You can also manage and reorder pages directly inside the Editor using the left thumbnail sidebar."
                },
                SupportedFormats = "Input: .pdf | Output: Organized .pdf",
                RelatedToolId = PdfToolId.OrganizePdf,
                IconKind = "ViewGridOutline",
                IconColorHex = "#4F46E5",
                BackgroundAccentHex = "#EEF2FF",
                Badge = "Organize",
                Keywords = "organize reorder pages sequence thumbnails duplicate delete rearrange",
                IsFeatured = true
            },
            new HelpGuideItem
            {
                Id = "tool-extract-pages",
                Title = "Extract Pages",
                Category = "32 PDF Tools",
                Summary = "Extract specific pages or page selections into a brand-new standalone PDF file.",
                Description = "Select specific pages or enter page ranges to pull out and generate a new independent PDF without modifying the original document.",
                Steps = new List<string>
                {
                    "Open 'Extract Pages' from the Tools Studio.",
                    "Select your source PDF.",
                    "Click to select thumbnails or enter page numbers (e.g. '1, 3, 5-7').",
                    "Click 'Extract to New PDF'."
                },
                KeyFeatures = new List<string>
                {
                    "Visual multi-select thumbnail picker",
                    "Custom range syntax support",
                    "Zero quality loss during extraction"
                },
                ProTips = new List<string>
                {
                    "Great for isolating single chapters from textbooks or extracting specific invoices from monthly ledgers."
                },
                SupportedFormats = "Input: .pdf | Output: Extracted .pdf",
                RelatedToolId = PdfToolId.OrganizePdf,
                IconKind = "Export",
                IconColorHex = "#6366F1",
                BackgroundAccentHex = "#EEF2FF",
                Badge = "Organize",
                Keywords = "extract pull pages subset select export isolate",
                IsFeatured = false
            },
            new HelpGuideItem
            {
                Id = "tool-remove-pages",
                Title = "Remove Pages",
                Category = "32 PDF Tools",
                Summary = "Delete blank, duplicate, or unnecessary pages from your PDF document.",
                Description = "Quickly prune unwanted pages from a document. Simply click the pages you want gone and export the streamlined PDF.",
                Steps = new List<string>
                {
                    "Open 'Remove Pages' from the Tools Studio.",
                    "Select your PDF.",
                    "Click the trash icon on any pages you wish to delete.",
                    "Click 'Remove Pages & Save'."
                },
                KeyFeatures = new List<string>
                {
                    "Click-to-remove visual interface",
                    "Shows instant preview of the remaining document structure",
                    "Automatic page numbering re-indexing"
                },
                ProTips = new List<string>
                {
                    "Inside the canvas editor, you can also press ⌘⇧⌫ (Ctrl+Shift+Delete) to remove the active page."
                },
                SupportedFormats = "Input: .pdf | Output: Pruned .pdf",
                RelatedToolId = PdfToolId.OrganizePdf,
                IconKind = "TrashCanOutline",
                IconColorHex = "#DC2626",
                BackgroundAccentHex = "#FEF2F2",
                Badge = "Organize",
                Keywords = "remove delete prune pages blank unwanted trash",
                IsFeatured = false
            },
            new HelpGuideItem
            {
                Id = "tool-compare-documents",
                Title = "Compare Documents",
                Category = "32 PDF Tools",
                Summary = "Perform a side-by-side visual and structural difference comparison between two PDF revisions.",
                Description = "The Compare Documents tool analyzes two versions of a PDF document, highlighting visual changes, modified text paragraphs, altered tables, and moved graphic elements with color-coded diff overlays.",
                Steps = new List<string>
                {
                    "Open 'Compare Documents' from the Tools Studio or press ⌘K and search 'Compare'.",
                    "Select 'Document A' (Original Version) and 'Document B' (Revised Version).",
                    "Choose comparison mode: 'Side-by-Side Diff' or 'Overlay Blend'.",
                    "Click 'Compare Revisions'.",
                    "Inspect the highlighted difference markers: Green for additions, Red for deletions, Yellow for modifications."
                },
                KeyFeatures = new List<string>
                {
                    "Side-by-Side synchronized scrolling comparison",
                    "Pixel-level Skia difference heatmaps",
                    "Structural text change reports"
                },
                ProTips = new List<string>
                {
                    "Invaluable for legal contract revisions, architectural drawing revisions, and editing proof checks."
                },
                SupportedFormats = "Inputs: Two .pdf files | Output: Diff Report & Visual Comparison",
                RelatedToolId = PdfToolId.ComparePdf,
                IconKind = "CompareHorizontal",
                IconColorHex = "#0284C7",
                BackgroundAccentHex = "#E0F2FE",
                Badge = "Security",
                Keywords = "compare diff revision versions legal check visual difference side by side",
                IsFeatured = true
            },
            new HelpGuideItem
            {
                Id = "tool-redact-pdf",
                Title = "Search & Redact Sensitive Data",
                Category = "32 PDF Tools",
                Summary = "Permanently black-out and sanitize PII, SSNs, credit card numbers, and secret names from PDFs.",
                Description = "Redacts sensitive text and information with permanent, irreversible black-box sanitization. Redacted content is completely purged from underlying PDF content streams—not just covered with a black box.",
                Steps = new List<string>
                {
                    "Open 'Search & Redact' from the Tools Studio.",
                    "Select your PDF.",
                    "Enter search keywords (e.g. 'Social Security', 'Confidential') or choose a regex pattern (SSN, Email, Phone, Credit Card).",
                    "Review matching occurrences across pages.",
                    "Click 'Apply Permanent Redaction' to purge all matched content."
                },
                KeyFeatures = new List<string>
                {
                    "True stream sanitization: completely purges text from vector and text streams",
                    "Built-in PII pattern detectors: SSN, Credit Cards, Email Addresses, Phone Numbers",
                    "Visual rectangle redaction drawing tool"
                },
                ProTips = new List<string>
                {
                    "Never use plain highlighter or drawing tools to hide secrets; always use True Redaction to purge the text from search indices."
                },
                SupportedFormats = "Input: .pdf | Output: Redacted & Sanitized .pdf",
                RelatedToolId = PdfToolId.RedactPdf,
                IconKind = "EyeOffOutline",
                IconColorHex = "#DC2626",
                BackgroundAccentHex = "#FEF2F2",
                Badge = "Security",
                Keywords = "redact black out pii ssn privacy sanitize secret erase legal security",
                IsFeatured = true
            },
            new HelpGuideItem
            {
                Id = "tool-bates-numbering",
                Title = "Bates Numbering",
                Category = "32 PDF Tools",
                Summary = "Apply sequential legal Bates numbering with customizable prefixes, suffixes, and digit padding.",
                Description = "Bates numbering is the global legal standard for indexing discovery documents, trial exhibits, and compliance records. FryPDF applies Bates stamps with customizable prefixes, digit counts, and positions.",
                Steps = new List<string>
                {
                    "Open 'Bates Numbering' from the Tools Studio.",
                    "Select one or multiple PDF files.",
                    "Configure Prefix (e.g. 'EXHIBIT-A-'), Starting Number (e.g. 1), and Digit Padding (e.g. 6 digits: '000001').",
                    "Choose stamp placement (Bottom Right, Top Right, etc.) and font style.",
                    "Click 'Apply Bates Numbering'."
                },
                KeyFeatures = new List<string>
                {
                    "Customizable Prefix and Suffix strings",
                    "Adjustable digit padding (e.g. 4 to 8 digits)",
                    "Batch multi-document continuous sequence numbering"
                },
                ProTips = new List<string>
                {
                    "When stamping multi-file court filings, load all files together to maintain an unbroken sequential Bates index across documents."
                },
                SupportedFormats = "Inputs: Multiple .pdf files | Output: Bates-Stamped .pdf files",
                RelatedToolId = PdfToolId.PageNumbers,
                IconKind = "Numeric",
                IconColorHex = "#7C3AED",
                BackgroundAccentHex = "#F5F3FF",
                Badge = "Security",
                Keywords = "bates numbering legal court exhibit litigation discovery compliance index",
                IsFeatured = false
            },
            new HelpGuideItem
            {
                Id = "tool-preflight-diagnostics",
                Title = "Preflight Diagnostics & Repair",
                Category = "32 PDF Tools",
                Summary = "Audit documents for corrupted streams, missing fonts, PDF/A compliance, and auto-repair issues.",
                Description = "Runs a comprehensive preflight validation check against your PDF file, detecting broken xref tables, unembedded fonts, corrupt image streams, and syntax warnings, offering one-click auto-repair.",
                Steps = new List<string>
                {
                    "Open 'Preflight Diagnostics' from the Tools Studio.",
                    "Select the PDF you want to audit.",
                    "Click 'Run Diagnostic Scan'.",
                    "Review the diagnostic report: Security status, PDF/A compliance, embedded fonts, and stream integrity.",
                    "Click 'Auto-Repair & Rebuild PDF' if any issues are detected."
                },
                KeyFeatures = new List<string>
                {
                    "Comprehensive structural and syntax validation engine",
                    "Scans for missing glyphs, unembedded fonts, and broken xref tables",
                    "One-Click stream rebuilding and sanitization"
                },
                ProTips = new List<string>
                {
                    "Run Preflight Diagnostics before sending files to professional print shops or archival repositories."
                },
                SupportedFormats = "Input: .pdf | Output: Diagnostic Report & Repaired .pdf",
                RelatedToolId = PdfToolId.RepairPdf,
                IconKind = "ShieldCheckOutline",
                IconColorHex = "#059669",
                BackgroundAccentHex = "#ECFDF5",
                Badge = "Security",
                Keywords = "preflight diagnostics repair audit test validate fix pdfa corrupted broken",
                IsFeatured = false
            },
            new HelpGuideItem
            {
                Id = "tool-custom-stamp",
                Title = "Custom Stamp Studio",
                Category = "32 PDF Tools",
                Summary = "Stamp APPROVED, CONFIDENTIAL, VOID, DRAFT, or custom vector corporate seals on PDFs.",
                Description = "Apply professional dynamic office stamps with date/time stamps, reviewer names, and vector borders to approve invoices, verify contracts, or mark drafts.",
                Steps = new List<string>
                {
                    "Open 'Custom Stamp Studio' from the Tools Studio or ribbon.",
                    "Pick a preset stamp ('APPROVED', 'CONFIDENTIAL', 'COMPLETED', 'RECEIVED', 'REJECTED') or create a custom text stamp.",
                    "Optionally include dynamic Date, Time, and User Name fields.",
                    "Select ink color (Emerald Green, Crimson Red, Deep Blue, Amber).",
                    "Click on the page where you want to place the stamp."
                },
                KeyFeatures = new List<string>
                {
                    "Pre-designed vector border styles (Classic Boxed, Double Border, Ribbon)",
                    "Dynamic date and reviewer name stamps",
                    "Resizable and rotatable vector stamp elements"
                },
                ProTips = new List<string>
                {
                    "Add dynamic date stamps to invoice approvals to establish a clear audit trail."
                },
                SupportedFormats = "Input: .pdf | Output: Stamped .pdf",
                RelatedToolId = PdfToolId.PdfForms,
                IconKind = "Stamp",
                IconColorHex = "#EA580C",
                BackgroundAccentHex = "#FFF7ED",
                Badge = "Editor",
                Keywords = "stamp approved confidential draft seal logo office mark dynamic date",
                IsFeatured = false
            },
            new HelpGuideItem
            {
                Id = "tool-header-footer",
                Title = "Header & Footer Studio",
                Category = "32 PDF Tools",
                Summary = "Apply document-wide running headers, footers, metadata titles, and margin offsets.",
                Description = "Add running headers and footers across entire documents with customized left, center, and right text segments, page counts, and document titles.",
                Steps = new List<string>
                {
                    "Open 'Header & Footer Studio' from the Tools Studio.",
                    "Select your PDF.",
                    "Configure Header text (Left, Center, Right) and Footer text.",
                    "Use variables like {{PageNumber}}, {{TotalPages}}, {{DocumentTitle}}, and {{Date}}.",
                    "Click 'Apply Headers & Footers'."
                },
                KeyFeatures = new List<string>
                {
                    "3-Column Header & Footer layout (Left, Center, Right)",
                    "Dynamic variable interpolation (Page {0} of {1}, Date, Filename)",
                    "Independent first page (cover page) suppression"
                },
                ProTips = new List<string>
                {
                    "Enable 'Suppress on Page 1' to keep cover pages and report title pages clean."
                },
                SupportedFormats = "Input: .pdf | Output: .pdf with Headers/Footers",
                RelatedToolId = PdfToolId.PageNumbers,
                IconKind = "DockTop",
                IconColorHex = "#0284C7",
                BackgroundAccentHex = "#E0F2FE",
                Badge = "Editor",
                Keywords = "header footer running title date page count margins metadata",
                IsFeatured = false
            },
            new HelpGuideItem
            {
                Id = "tool-ocr-pdf",
                Title = "OCR Text Recognition",
                Category = "32 PDF Tools",
                Summary = "Extract selectable, searchable, and copyable text from scanned image PDFs and photos.",
                Description = "Converts flat bitmap scans and scanned paper documents into searchable PDFs with an invisible, selectable text layer aligned over the original scan.",
                Steps = new List<string>
                {
                    "Open 'OCR PDF' from the AI & Automation tools category.",
                    "Select your scanned PDF or image.",
                    "Select document language (English, Spanish, French, German, Hindi, Chinese, etc.).",
                    "Click 'Perform OCR'.",
                    "The resulting PDF now features fully selectable, copyable, and searchable text."
                },
                KeyFeatures = new List<string>
                {
                    "Multi-language recognition engine",
                    "Generates invisible text overlay matching scan geometry",
                    "Exports searchable PDF or pure plain text"
                },
                ProTips = new List<string>
                {
                    "Running OCR on scanned contracts enables fast in-document keyword search (⌘F) in any PDF reader."
                },
                SupportedFormats = "Input: Scanned .pdf, images | Output: Searchable .pdf, .txt",
                RelatedToolId = PdfToolId.OcrPdf,
                IconKind = "TextRecognition",
                IconColorHex = "#7C3AED",
                BackgroundAccentHex = "#F5F3FF",
                Badge = "AI & Automation",
                Keywords = "ocr text recognition scan scanned searchable copyable extract text image",
                IsFeatured = true
            },
            new HelpGuideItem
            {
                Id = "tool-document-translation",
                Title = "Document Translation",
                Category = "32 PDF Tools",
                Summary = "Translate document content into multiple languages with script-aware font matching.",
                Description = "Translate entire PDF documents into different languages while preserving the original layout structure, headers, and visual tables, with automatic font fallback for non-Latin scripts.",
                Steps = new List<string>
                {
                    "Open 'Translate Document' from AI & Automation.",
                    "Select your PDF file.",
                    "Choose source and target languages.",
                    "Click 'Translate & Rebuild Document'.",
                    "Inspect the translated document with matching localized typography."
                },
                KeyFeatures = new List<string>
                {
                    "Preserves document layout, columns, tables, and image positions",
                    "Automatic Unicode script detection and font family fallback",
                    "Side-by-side bilingual export option"
                },
                ProTips = new List<string>
                {
                    "Ensure you have the required language font pack installed via 'Language & Fonts' in the sidebar for optimal glyph rendering."
                },
                SupportedFormats = "Input: .pdf | Output: Translated .pdf",
                RelatedToolId = PdfToolId.TranslatePdf,
                IconKind = "Translate",
                IconColorHex = "#0284C7",
                BackgroundAccentHex = "#E0F2FE",
                Badge = "AI & Automation",
                Keywords = "translate translation language multilingual international scripts localized",
                IsFeatured = false
            },
            new HelpGuideItem
            {
                Id = "tool-ai-summarizer",
                Title = "AI Summarizer & Analysis",
                Category = "32 PDF Tools",
                Summary = "Generate structured summaries, executive briefs, key takeaways, and section analyses from PDFs.",
                Description = "Extracts and synthesizes document contents into high-level executive summaries, bulleted key takeaways, and section-by-section breakdowns.",
                Steps = new List<string>
                {
                    "Open 'AI Summarizer' from AI & Automation.",
                    "Select your document.",
                    "Choose summary type: Executive Brief, Bulleted Key Takeaways, or Detailed Section Breakdown.",
                    "Click 'Generate Summary'.",
                    "Copy summary text or export it as a clean PDF summary report."
                },
                KeyFeatures = new List<string>
                {
                    "Multiple summary modes: Executive, Key Points, Q&A Brief",
                    "Analyzes multi-page research papers and lengthy legal contracts",
                    "Exports formatted markdown or PDF reports"
                },
                ProTips = new List<string>
                {
                    "Use Key Takeaways mode on 50+ page annual reports to extract financial highlights in under 10 seconds."
                },
                SupportedFormats = "Input: .pdf | Output: .txt, .md, .pdf Summary Report",
                RelatedToolId = PdfToolId.AiSummarizer,
                IconKind = "AutoFix",
                IconColorHex = "#EC4899",
                BackgroundAccentHex = "#FDF2F8",
                Badge = "AI & Automation",
                Keywords = "ai summarize summarizer analysis overview key points takeaways brief report",
                IsFeatured = true
            },

            // =========================================================================
            // 3. LIVE CANVAS DOCUMENT EDITOR & INSPECTOR STUDIO
            // =========================================================================
            new HelpGuideItem
            {
                Id = "editor-canvas-basics",
                Title = "Canvas Navigation, Grid & Rulers",
                Category = "Live Editor",
                Summary = "Master the interactive canvas: moving elements, snapping to grid, guideline alignment, and rulers.",
                Description = "The FryPDF Document Canvas is an interactive vector design workspace. Elements can be clicked, dragged, resized via control handles, and snapped to precise alignments.",
                Steps = new List<string>
                {
                    "Click on any canvas element to select it; selection handles will appear around its bounding box.",
                    "Drag the element to reposition it; smart alignment guides appear automatically to help align with nearby elements.",
                    "Drag corner or edge handles to resize elements proportionally (hold Shift to maintain aspect ratio).",
                    "Use canvas rulers along the top and left to measure exact point coordinates.",
                    "Zoom in and out using ⌘+ / ⌘- or hold Ctrl while scrolling the mouse wheel."
                },
                KeyFeatures = new List<string>
                {
                    "Live Canvas Rulers with cursor tracking in physical PDF points",
                    "Snap-to-Grid and dynamic object alignment guides",
                    "Multi-selection: Drag a selection marquee or Shift-click elements",
                    "Zoom range from 40% up to 250% with instant 100% reset (⌘0)"
                },
                ProTips = new List<string>
                {
                    "Hold Spacebar and drag the mouse to pan smoothly across the canvas at high zoom levels.",
                    "Press ⌘1 to fit the page width or ⌘9 to fit the entire page on screen."
                },
                SupportedFormats = "Live Canvas Workspace",
                KeyboardShortcut = "⌘1 (Fit Width), ⌘9 (Fit Page), ⌘0 (Reset 100%)",
                IconKind = "RulerSquare",
                IconColorHex = "#0F6CBD",
                BackgroundAccentHex = "#EFF6FF",
                Badge = "Editor",
                Keywords = "canvas rulers grid snap alignment pan zoom selection move resize handles",
                IsFeatured = true
            },
            new HelpGuideItem
            {
                Id = "editor-typography",
                Title = "Typography, Fonts & Text Blocks",
                Category = "Live Editor",
                Summary = "Format rich text blocks: font families, weights, font sizes, line heights, and letter spacing.",
                Description = "Text blocks in FryPDF provide fine-grained typographic controls. You can adjust font families (Inter, Roboto, Noto Sans, Playfair, Courier, etc.), font sizes, line heights, letter spacing, and alignments via the Properties Inspector.",
                Steps = new List<string>
                {
                    "Click 'Text' in the ribbon toolbar to insert a new text element, or double-click any existing text.",
                    "Open the Properties Inspector on the right sidebar (⌘⇧P).",
                    "Choose font family, font weight (Regular, Medium, SemiBold, Bold), and font size.",
                    "Adjust 'Line Height' multiplier (1.25x to 1.5x) for optimal paragraph readability.",
                    "Pick text color, text alignment (Left, Center, Right, Justify), and background padding."
                },
                KeyFeatures = new List<string>
                {
                    "Rich Font Library with embedded Unicode fallbacks",
                    "Fine-grained Typography Controls: Size, Weight, Line Height, Letter Spacing",
                    "Multi-line wrapping with auto-height bounding box calculations",
                    "Rich hex color picker with preset palette swatches"
                },
                ProTips = new List<string>
                {
                    "Set Line Height between 1.3x and 1.45x for comfortable reading in multi-paragraph body text.",
                    "When typing in non-Latin scripts (Hindi, Japanese, Arabic), FryPDF automatically selects the optimal Noto Sans script family."
                },
                SupportedFormats = "Typography & Text Elements",
                KeyboardShortcut = "⌘⇧P / Ctrl+Shift+P (Properties Inspector)",
                IconKind = "FormatSize",
                IconColorHex = "#6366F1",
                BackgroundAccentHex = "#EEF2FF",
                Badge = "Editor",
                Keywords = "text typography font family size line height spacing bold italic alignment color",
                IsFeatured = true
            },
            new HelpGuideItem
            {
                Id = "editor-rich-text",
                Title = "Rich Text & Inline Markdown Formatting",
                Category = "Live Editor",
                Summary = "Style words within the same text box with mixed bold, italics, underlines, strikethroughs, sub/superscripts, colors, and links.",
                Description = "FryPDF supports high-fidelity rich text with multi-span inline styling. You can format distinct words and phrases within a single text box using quick Markdown shorthand or HTML-style tags during in-place editing, or via the Properties Inspector.",
                Steps = new List<string>
                {
                    "Double-click any text box on the canvas to activate in-place editing mode.",
                    "Type standard Markdown shorthand: use **bold** for bold text, *italic* for italic text, <u>underline</u> for underlines, and ~~strikethrough~~ for revisions.",
                    "Use scientific scripts: type ~2~ for subscript (e.g. H~2~O) and ^2^ for superscript (e.g. E=mc^2^, 1^st^).",
                    "Apply custom inline colors with <color=#HEX>text</color> (e.g. <color=#0F6CBD>Blue</color>) and highlights with <mark=#HEX>text</mark>.",
                    "Add clickable hyperlinks with [Label Text](https://example.com).",
                    "Press Enter or click outside the text box: FryPDF instantly compiles the markup into high-performance multi-span runs rendered on the canvas and exported to vector PDF."
                },
                KeyFeatures = new List<string>
                {
                    "Multi-Span Architecture: Single unified text element with granular typography for every word",
                    "In-Place Markdown & HTML Tag Parsing on double-tap and blur",
                    "Scientific Subscript (~x~) and Superscript (^x^) Notation Support",
                    "Inline Hex Color Tags (<color=#HEX>) and Highlights (<mark=#HEX>)",
                    "Clickable PDF Hyperlinks ([Title](url)) compiled directly to QuestPDF links",
                    "Template DataMerge Support: {{Variable}} tags evaluate seamlessly inside rich spans",
                    "100% Vector PDF Export with zero rasterization"
                },
                ProTips = new List<string>
                {
                    "Combine formatting freely! For example, **<color=#0F6CBD>Bold Blue</color>** creates styled highlighted headings in one step.",
                    "Press ⌘Z (Ctrl+Z) after editing to undo any formatting changes atomically.",
                    "Check out the 'Rich Typography & Publishing Specimen' in the Template Studio for live examples."
                },
                SupportedFormats = "Live Canvas, Markdown Shorthand, QuestPDF Vector Export, SVG <tspan>",
                KeyboardShortcut = "Double-Click text (In-Place Edit)",
                IconKind = "FormatColorText",
                IconColorHex = "#0F6CBD",
                BackgroundAccentHex = "#EFF6FF",
                Badge = "New Feature",
                Keywords = "richtext rich text markdown bold italic underline strikethrough subscript superscript color highlight link spans formatting tags",
                IsFeatured = true
            },
            new HelpGuideItem
            {
                Id = "editor-shapes-ink",
                Title = "Vector Shapes, Cards & Freehand Ink",
                Category = "Live Editor",
                Summary = "Add vector rectangles, rounded container cards, divider rules, and freehand stylus ink drawings.",
                Description = "Enhance your document layouts with crisp vector shapes. Insert rectangles, badges, callout cards, divider lines, or draw freehand ink annotations directly on the canvas.",
                Steps = new List<string>
                {
                    "Click 'Shapes' in the ribbon to insert a Rectangle, Ellipse, or Divider Line.",
                    "Use the Properties Inspector to adjust Fill Color, Stroke Color, Border Thickness, and Corner Radius.",
                    "To draw freehand annotations, select the 'Ink Pen' tool from the ribbon and draw directly on the canvas.",
                    "Adjust stroke width and pen color in the inspector."
                },
                KeyFeatures = new List<string>
                {
                    "Vector Rectangles, Rounded Cards, Ellipses, and Divider Lines",
                    "Customizable Corner Radius, Border Thickness, and Box Shadows",
                    "Freehand Stylus & Mouse Ink Pen with Bézier smoothing",
                    "Exported as razor-sharp vector SVG/QuestPDF paths"
                },
                ProTips = new List<string>
                {
                    "Use rounded cards with a subtle border and 8px corner radius for modern corporate document cards.",
                    "Set element opacity in the inspector to create translucent background badges."
                },
                SupportedFormats = "Vector Elements",
                IconKind = "ShapeOutline",
                IconColorHex = "#F59E0B",
                BackgroundAccentHex = "#FFFBEB",
                Badge = "Editor",
                Keywords = "shapes rectangle card rounded ellipse divider line ink pen draw freehand vector",
                IsFeatured = false
            },
            new HelpGuideItem
            {
                Id = "editor-tables",
                Title = "Tables & Data Grids",
                Category = "Live Editor",
                Summary = "Create and format professional tables with custom column widths, header styles, and alternating shading.",
                Description = "Insert and customize structured tables for financial reports, pricing sheets, and technical data. Easily edit cell text, adjust column widths, configure borders, and apply alternating row colors.",
                Steps = new List<string>
                {
                    "Click 'Table' in the ribbon toolbar to insert a new data grid on the canvas.",
                    "Specify initial rows and columns in the dialog.",
                    "Double-click any cell to enter text, numbers, or headers.",
                    "Use the Inspector to customize Header Row Background, Alternating Row Shading, and Border Colors.",
                    "Drag column dividers to adjust column proportions."
                },
                KeyFeatures = new List<string>
                {
                    "Customizable Row and Column counts",
                    "Header row formatting with distinct typography and background colors",
                    "Alternating row shading (Zebra striping) for readability",
                    "Adjustable cell padding and border styles"
                },
                ProTips = new List<string>
                {
                    "Right-align numeric and currency cells to ensure decimal points align cleanly for readers."
                },
                SupportedFormats = "Table Elements",
                IconKind = "Table",
                IconColorHex = "#10B981",
                BackgroundAccentHex = "#ECFDF5",
                Badge = "Editor",
                Keywords = "table grid rows columns cells data financial report format borders",
                IsFeatured = false
            },
            new HelpGuideItem
            {
                Id = "editor-math-latex",
                Title = "LaTeX Math Equation Studio",
                Category = "Live Editor",
                Summary = "Typeset publication-grade LaTeX math formulas, integrals, fractions, and matrices into vector paths.",
                Description = "The built-in Math Equation Studio allows academic researchers, students, and engineers to write LaTeX equations (e.g. fractions, integrals, Greek letters, summations, matrices) and render them directly into crisp vector elements on the canvas.",
                Steps = new List<string>
                {
                    "Click 'Math Equation' in the ribbon or press ⌘K and type 'Math Equation'.",
                    "Type your LaTeX expression in the formula editor (e.g. '\\int_{0}^{\\infty} e^{-x^2} dx = \\frac{\\sqrt{\\pi}}{2}').",
                    "Inspect the live real-time formula preview.",
                    "Choose font size and text color.",
                    "Click 'Insert Equation' to place the formula on the canvas."
                },
                KeyFeatures = new List<string>
                {
                    "Full LaTeX Syntax Support: Fractions, Superscripts/Subscripts, Integrals, Summations, Greek Symbols, Matrices",
                    "Live Real-Time Vector Formula Preview",
                    "Rendered as infinite-resolution vector paths in exported PDFs"
                },
                ProTips = new List<string>
                {
                    "Check the 'Math & Academic Research' templates for pre-configured theorem blocks and math formulas."
                },
                SupportedFormats = "LaTeX Math Elements",
                IconKind = "Sigma",
                IconColorHex = "#7C3AED",
                BackgroundAccentHex = "#F5F3FF",
                Badge = "Editor",
                Keywords = "math latex equation formula integral fraction matrix greek physics academic science",
                IsFeatured = true
            },
            new HelpGuideItem
            {
                Id = "editor-qr-barcodes",
                Title = "QR Codes & 2D Barcodes",
                Category = "Live Editor",
                Summary = "Generate instant QR codes for URLs, contact vCards, Wi-Fi keys, and embed them in layouts.",
                Description = "Generate and embed scannable QR codes directly on your PDF pages. Ideal for business cards, resumes, event flyers, invoices, and product packaging.",
                Steps = new List<string>
                {
                    "Click 'QR Code' in the ribbon toolbar.",
                    "Enter your target URL, contact vCard, Wi-Fi credentials, or text.",
                    "Configure QR code size, foreground color, and background color.",
                    "Click 'Insert QR Code' to position it on your page.",
                    "Test scan the preview on your mobile phone camera."
                },
                KeyFeatures = new List<string>
                {
                    "Supports URLs, Plain Text, vCard Contacts, and Payment Links",
                    "Customizable QR pixel colors and background transparencies",
                    "High-resolution vector generation ensuring instant scan reliability"
                },
                ProTips = new List<string>
                {
                    "Maintain high contrast (e.g. dark QR pixels on a light background) to ensure fast scanning in all lighting conditions."
                },
                SupportedFormats = "QR Code Elements",
                IconKind = "Qrcode",
                IconColorHex = "#0F172A",
                BackgroundAccentHex = "#F1F5F9",
                Badge = "Editor",
                Keywords = "qr code barcode scan link url vcard contact wifi embed generate",
                IsFeatured = false
            },
            new HelpGuideItem
            {
                Id = "editor-zindex-layers",
                Title = "Layer Hierarchy & Z-Index Management",
                Category = "Live Editor",
                Summary = "Control visual stacking order: bring elements forward, send backward, and manage container layers.",
                Description = "Learn how visual layer stacking works in FryPDF. Layer ordering ensures background cards stay behind content, while headers, labels, and interactive form fields remain on top.",
                Steps = new List<string>
                {
                    "Select an element on the canvas.",
                    "Use the Layer controls in the ribbon or Inspector: 'Bring Forward', 'Send Backward', 'Bring to Front', or 'Send to Back'.",
                    "Background shapes and cards automatically occupy Z-indices 0–99.",
                    "Images and photos occupy Z-indices 100–499.",
                    "Text blocks and headings occupy Z-indices 1000–1999."
                },
                KeyFeatures = new List<string>
                {
                    "Predictable Layered Z-Index Architecture",
                    "Prevents background cards from occluding photos and text",
                    "One-click Bring to Front and Send to Back controls"
                },
                ProTips = new List<string>
                {
                    "Use 'Send to Back' whenever adding a background card behind existing text to immediately nest it behind all content."
                },
                SupportedFormats = "Layer Management",
                IconKind = "LayersOutline",
                IconColorHex = "#4F46E5",
                BackgroundAccentHex = "#EEF2FF",
                Badge = "Editor",
                Keywords = "layers zindex stacking order bring forward send back front arrange hierarchy",
                IsFeatured = false
            },
            new HelpGuideItem
            {
                Id = "editor-undo-redo",
                Title = "Undo, Redo & Project History",
                Category = "Live Editor",
                Summary = "Never lose work with unlimited atomic undo/redo history and accidental change rollback.",
                Description = "Every modification in FryPDF—element moves, text edits, color adjustments, deletions, and page additions—is recorded in an atomic undo/redo history stack.",
                Steps = new List<string>
                {
                    "Press ⌘Z (Ctrl+Z) to undo your last action.",
                    "Press ⌘⇧Z or ⌘Y (Ctrl+Y) to redo an undone action.",
                    "Use the Quick Undo/Redo arrow buttons in the top toolbar.",
                    "History tracks moves, property edits, deletions, and layout changes seamlessly."
                },
                KeyFeatures = new List<string>
                {
                    "Deep Atomic Undo/Redo stack",
                    "Full cross-platform shortcuts (macOS Cmd vs Win/Linux Ctrl)",
                    "Captures canvas movements, resizing, deletions, and text modifications"
                },
                ProTips = new List<string>
                {
                    "You can undo multiple actions in rapid succession without losing document integrity."
                },
                SupportedFormats = "Editor History",
                KeyboardShortcut = "⌘Z (Undo), ⌘⇧Z / ⌘Y (Redo)",
                IconKind = "Undo",
                IconColorHex = "#F59E0B",
                BackgroundAccentHex = "#FFFBEB",
                Badge = "Editor",
                Keywords = "undo redo history revert rollback restore accidental mistake change",
                IsFeatured = false
            },
            new HelpGuideItem
            {
                Id = "editor-export-persistence",
                Title = "Exporting Vector PDFs & Native Projects",
                Category = "Live Editor",
                Summary = "Save native .frypdf workspaces and compile high-resolution vector QuestPDF documents.",
                Description = "FryPDF offers dual-mode persistence: save your project as a lightweight `.frypdf` workspace file for future editing, or export a publication-ready `.pdf` with embedded project metadata.",
                Steps = new List<string>
                {
                    "Press ⌘S (Save Project) to save your work as a native `.frypdf` file.",
                    "Press ⌘E (Export to PDF) to compile the document into a high-fidelity vector PDF.",
                    "In the Export dialog, choose your destination folder and filename.",
                    "Exported PDFs embed the editable project state inside metadata, allowing them to be reopened and edited in FryPDF anytime!"
                },
                KeyFeatures = new List<string>
                {
                    "QuestPDF High-Fidelity Vector Compilation Engine",
                    "Embedded Project State: Exported PDFs can be reopened as live editable files",
                    "Compact Native .frypdf JSON File Format"
                },
                ProTips = new List<string>
                {
                    "Because FryPDF embeds project metadata in exported PDFs, you can send a PDF to a colleague and they can open and edit it directly in FryPDF!"
                },
                SupportedFormats = "Save: .frypdf | Export: .pdf",
                KeyboardShortcut = "⌘S (Save Project), ⌘E (Export PDF)",
                IconKind = "ContentSaveOutline",
                IconColorHex = "#059669",
                BackgroundAccentHex = "#ECFDF5",
                Badge = "Editor",
                Keywords = "save export pdf compile frypdf project metadata persistence embed",
                IsFeatured = true
            },

            // =========================================================================
            // 4. AUTOMATION, WORKFLOWS & BATCH MASS GENERATION
            // =========================================================================
            new HelpGuideItem
            {
                Id = "automation-workflow-builder",
                Title = "Workflow Builder: Chained PDF Pipelines",
                Category = "Automation",
                Summary = "Build and execute multi-step automated PDF processing pipelines with one click.",
                Description = "Workflow Builder allows you to automate repetitive document tasks by chaining multiple PDF tools into an automated sequence (e.g. Merge -> Compress -> Watermark -> Encrypt).",
                Steps = new List<string>
                {
                    "Open 'Workflow Builder' from AI & Automation or click the Workflow banner on the Home Dashboard.",
                    "Choose a workflow preset or click 'Create Custom Workflow'.",
                    "Add pipeline steps from the tool catalog (e.g. Step 1: Merge, Step 2: Compress, Step 3: Watermark, Step 4: Protect).",
                    "Configure step parameters (e.g. Watermark text, Encryption password).",
                    "Select input files and click 'Run Automated Workflow'."
                },
                KeyFeatures = new List<string>
                {
                    "Chain any combination of the 32 PDF tools",
                    "Visual pipeline flowchart builder",
                    "Batch multi-file pipeline execution with live progress logging"
                },
                ProTips = new List<string>
                {
                    "Save your frequently used pipelines as custom presets to run routine end-of-month reporting workflows in seconds."
                },
                SupportedFormats = "Pipeline Automation Engine",
                RelatedToolId = PdfToolId.WorkflowBuilder,
                IconKind = "VectorPolyline",
                IconColorHex = "#7C3AED",
                BackgroundAccentHex = "#F5F3FF",
                Badge = "Automation",
                Keywords = "workflow pipeline automated chain sequence batch batching process auto",
                IsFeatured = true
            },
            new HelpGuideItem
            {
                Id = "automation-data-studio",
                Title = "Data Studio & Batch Mass PDF Generation",
                Category = "Automation",
                Summary = "Import CSV or JSON data to mail-merge placeholders and generate hundreds of personalized PDFs.",
                Description = "Data Studio connects datasets (CSV, JSON, Excel) directly to document template placeholders (e.g. {{CustomerName}}, {{InvoiceNumber}}, {{DueDate}}, {{Amount}}), compiling hundreds of personalized PDFs in seconds.",
                Steps = new List<string>
                {
                    "Design your base document in the Canvas Editor using placeholder variables like {{Name}}, {{Amount}}, {{Date}}.",
                    "Click 'Batch Generation / Data Studio' in the ribbon or Home Dashboard.",
                    "Import your CSV or JSON data source file.",
                    "Map dataset columns to the document placeholders.",
                    "Preview sample merged records in real-time.",
                    "Click 'Generate Batch PDFs' to produce individual PDFs or a single combined batch file."
                },
                KeyFeatures = new List<string>
                {
                    "Supports CSV, JSON, and Excel tabular datasets",
                    "Live Data Binding Preview: Cycle through records to verify layout before generating",
                    "Mass High-Speed Parallel Generation Engine (100+ PDFs in seconds)",
                    "Custom dynamic output naming (e.g. 'Invoice_{{InvoiceNumber}}_{{CustomerName}}.pdf')"
                },
                ProTips = new List<string>
                {
                    "Use Data Studio to generate monthly payroll slips, customized certificates of completion, conference badges, and bulk invoices."
                },
                SupportedFormats = "Inputs: .csv, .json, .xlsx | Output: Batch .pdf files",
                IconKind = "DatabaseArrowRightOutline",
                IconColorHex = "#2563EB",
                BackgroundAccentHex = "#EFF6FF",
                Badge = "Automation",
                Keywords = "batch mass generation data studio csv json mail merge variables certificates invoices",
                IsFeatured = true
            },

            // =========================================================================
            // 5. FONTS & INTERNATIONAL SCRIPT SUPPORT
            // =========================================================================
            new HelpGuideItem
            {
                Id = "fonts-language-packs",
                Title = "Font Manager & Unicode Script Support",
                Category = "Language & Fonts",
                Summary = "Manage on-demand Google Noto Sans font packages for Devanagari, CJK, Arabic, and Indic scripts.",
                Description = "FryPDF includes comprehensive multi-script Unicode intelligence. It eliminates missing font boxes (tofu '□□□') by automatically detecting text scripts and falling back to embedded Google Noto Sans font families.",
                Steps = new List<string>
                {
                    "Select 'Language & Fonts' from the left sidebar.",
                    "Browse available language packages: Devanagari (Hindi, Marathi, Sanskrit), CJK (Chinese, Japanese, Korean), Arabic, Tamil, Telugu, Thai, Cyrillic, Greek, Hebrew.",
                    "View installed font status and package details.",
                    "When deconstructing imported PDFs with Indic or CJK text, FryPDF automatically activates the appropriate script rendering rules."
                },
                KeyFeatures = new List<string>
                {
                    "Automatic Unicode Script Range Detection",
                    "Eliminates tofu boxes (□□□) across 100+ languages",
                    "Script-Aware token joining for Indic and Asian scripts without artificial spaces",
                    "100% Embedded and self-contained: zero external web font requests"
                },
                ProTips = new List<string>
                {
                    "If an imported PDF displays missing character glyphs, verify the corresponding language package is active in Language & Fonts."
                },
                SupportedFormats = "TrueType / OpenType Fonts (.ttf, .otf)",
                IconKind = "Translate",
                IconColorHex = "#0284C7",
                BackgroundAccentHex = "#E0F2FE",
                Badge = "Typography",
                Keywords = "fonts languages unicode noto sans devanagari hindi cjk chinese japanese arabic tofu glyphs",
                IsFeatured = false
            },

            // =========================================================================
            // 6. KEYBOARD SHORTCUTS & PRODUCTIVITY
            // =========================================================================
            new HelpGuideItem
            {
                Id = "shortcuts-reference",
                Title = "Keyboard Shortcuts & Mouse Gestures",
                Category = "Shortcuts",
                Summary = "Comprehensive cheat sheet for all global hotkeys, editor shortcuts, and navigation gestures.",
                Description = "Boost your editing productivity with full keyboard shortcuts designed for both macOS (Cmd key) and Windows/Linux (Ctrl key).",
                Steps = new List<string>
                {
                    "Press F1 or Shift+? anytime to open the interactive Keyboard Shortcuts dialog.",
                    "Use ⌘K to open the Quick Command Palette to run any command without touching the mouse.",
                    "Use ⌘S to save your project and ⌘E to export to PDF.",
                    "Use ⌘B to toggle the left sidebar, ⌘F1 to toggle the ribbon, and ⌘⇧P to toggle the Inspector.",
                    "Use ⌘1, ⌘9, and ⌘0 for quick zoom fitting and reset."
                },
                KeyFeatures = new List<string>
                {
                    "File Operations: ⌘N (New), ⌘O (Open), ⌘S (Save), ⌘E (Export)",
                    "Edit Operations: ⌘Z (Undo), ⌘⇧Z / ⌘Y (Redo), ⌘C (Copy), ⌘V (Paste), ⌘X (Cut), ⌘D (Duplicate)",
                    "View Operations: ⌘+ (Zoom In), ⌘- (Zoom Out), ⌘0 (100%), ⌘1 (Fit Width), ⌘9 (Fit Page)",
                    "Page Management: PageDown / PageUp, ⌘⇧N (Add Page), ⌘⇧D (Duplicate Page), ⌘⇧R (Rotate Page), ⌘⇧⌫ (Delete Page)",
                    "Workspace Panels: ⌘B (Sidebar), ⌘F1 (Ribbon), ⌘⇧P (Inspector), ⌘K (Command Palette)"
                },
                ProTips = new List<string>
                {
                    "Hold Shift while dragging an element's corner handle to constrain its aspect ratio.",
                    "Hold Alt / Option while dragging an element to clone and duplicate it instantly."
                },
                SupportedFormats = "Hotkeys & Gestures",
                KeyboardShortcut = "F1 / Shift+? (Shortcuts Dialog)",
                IconKind = "KeyboardOutline",
                IconColorHex = "#64748B",
                BackgroundAccentHex = "#F1F5F9",
                Badge = "Productivity",
                Keywords = "shortcuts hotkeys keys keyboard mouse gestures cheat sheet f1 cmd ctrl",
                IsFeatured = true
            },

            // =========================================================================
            // 7. FREQUENTLY ASKED QUESTIONS & TROUBLESHOOTING
            // =========================================================================
            new HelpGuideItem
            {
                Id = "faq-troubleshooting",
                Title = "Frequently Asked Questions & Troubleshooting",
                Category = "FAQ & Support",
                Summary = "Find answers to common questions, performance tips, and troubleshooting resolutions.",
                Description = "Get quick answers regarding privacy, file compatibility, high-resolution rendering, and troubleshooting common PDF challenges.",
                Steps = new List<string>
                {
                    "Q: Is my document uploaded to the internet or any cloud server? -> A: Absolutely not. FryPDF is 100% offline and processes everything strictly locally on your computer.",
                    "Q: Why do some non-Latin characters appear as squares (□□□)? -> A: Open 'Language & Fonts' from the sidebar and confirm the appropriate Google Noto Sans font pack is available.",
                    "Q: Can I edit a PDF created in another application? -> A: Yes! Use 'Edit PDF' or drag the PDF into the editor. The deconstruction engine parses text, shapes, and images into editable layers.",
                    "Q: How do I reduce the file size of a PDF for email? -> A: Use the 'Compress PDF' tool with 'Recommended' compression preset to achieve maximum file size reduction with zero visible quality loss.",
                    "Q: How do I report a bug or suggest a feature? -> A: Visit the CodeFryDev developer portal at https://codefrydev.in or check the 'Licenses & Tools' page."
                },
                KeyFeatures = new List<string>
                {
                    "100% Local Privacy Guarantee",
                    "Comprehensive Troubleshooting Guide",
                    "Performance tuning for 100+ page documents"
                },
                ProTips = new List<string>
                {
                    "If an imported PDF has flattened raster text (such as a physical scan), run 'OCR Text Recognition' before editing."
                },
                SupportedFormats = "Knowledge Base & FAQ",
                IconKind = "HelpBoxOutline",
                IconColorHex = "#0284C7",
                BackgroundAccentHex = "#E0F2FE",
                Badge = "FAQ",
                Keywords = "faq help questions troubleshooting issue problem privacy tofu error crash support",
                IsFeatured = true
            }
        };

        _categories = new List<string>
        {
            "All Guides",
            "Getting Started",
            "32 PDF Tools",
            "Live Editor",
            "Automation",
            "Language & Fonts",
            "Shortcuts",
            "FAQ & Support"
        };
    }

    public IReadOnlyList<HelpGuideItem> GetAllGuides() => _guides;

    public IReadOnlyList<HelpGuideItem> GetGuidesByCategory(string category)
    {
        if (string.IsNullOrWhiteSpace(category) || category.Equals("All Guides", StringComparison.OrdinalIgnoreCase) || category.Equals("All", StringComparison.OrdinalIgnoreCase))
            return _guides;

        return _guides.Where(g => g.Category.Equals(category, StringComparison.OrdinalIgnoreCase)).ToList();
    }

    public HelpGuideItem? GetGuideById(string id)
    {
        if (string.IsNullOrWhiteSpace(id)) return null;
        return _guides.FirstOrDefault(g => g.Id.Equals(id, StringComparison.OrdinalIgnoreCase));
    }

    public HelpGuideItem? GetGuideByToolId(PdfToolId toolId)
    {
        return _guides.FirstOrDefault(g => g.RelatedToolId == toolId);
    }

    public IReadOnlyList<string> GetAllCategories() => _categories;
}
