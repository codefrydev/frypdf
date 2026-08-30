namespace PdfEditorApp.Models;

public enum ElementKind
{
    Text,
    Heading,
    Image,
    Shape,
    Divider,
    Table,
    Chart,
    Watermark,
    Stamp,
    StickyNote,
    FormField,
    QrCode,
    Barcode,
    Redaction,
    Ink,
    Measurement
}

public enum FormFieldType
{
    Text,
    MultilineText,
    Checkbox,
    Radio,
    Dropdown,
    Signature,
    Button,
    DatePicker,
    Number,
    SignatureLine
}

public enum CalculationFormula
{
    None,
    Sum,
    Average,
    Product,
    Min,
    Max
}

public enum FormButtonAction
{
    SubmitForm,
    ResetForm,
    PrintDocument,
    GotoPage
}

public enum BatesPosition
{
    TopLeft,
    TopCenter,
    TopRight,
    BottomLeft,
    BottomCenter,
    BottomRight
}

public enum SplitExtractMode
{
    SplitEveryNPages,
    SplitByPageRanges,
    ExtractSelectedPages
}

public enum CompareDiffType
{
    ElementAdded,
    ElementRemoved,
    ElementModified,
    TextModified,
    FormattingModified,
    MetadataModified,
    SecurityModified,
    PageCountChanged
}

public enum RulerUnit
{
    Points,
    Inches,
    Millimeters
}

public enum FormValidationType
{
    None,
    Email,
    Numeric,
    Phone,
    Date,
    CustomRegex
}

public enum SignatureStyle
{
    DrawnInk,
    CursiveElegance,
    SignatureCasual,
    ClassicScript,
    ModernHandwriting,
    UploadedImage
}

public enum GridSnapSize
{
    None = 0,
    Points10 = 10,
    Points20 = 20,
    Points50 = 50
}

public enum RedactionMode
{
    Blackout,
    Whiteout,
    Grayout
}

public enum PageFormat
{
    A4,
    Letter,
    Legal,
    Executive,
    A3,
    A5,
    Tabloid,
    Custom
}

public enum PageOrientation
{
    Portrait,
    Landscape
}

public enum TextAlignmentMode
{
    Left,
    Center,
    Right,
    Justify
}

public enum ShapeType
{
    Rectangle,
    RoundedRectangle,
    Circle,
    Triangle,
    RightTriangle,
    Diamond,
    Pentagon,
    Hexagon,
    Octagon,
    Star5,
    Star4Badge,
    Star8Badge,
    Star12Seal,
    RosetteSeal,
    LaurelWreathSeal,
    MedalRibbonBadge,
    RibbonBanner,
    CornerPolygonalAccentTopLeft,
    CornerPolygonalAccentBottomRight,
    CornerDiagonalWedge,
    Chevron,
    Trapezoid,
    Parallelogram,
    ShieldBadge,
    AwardBadge,
    ArrowRight,
    ArrowLeft,
    Callout,
    Heart,
    Cloud,
    Line,
    Arrow,
    Star,
    Card,
    StickyNote,
    RevisionCloud,
    DimensionLine,
    CustomSvgPath
}

public enum ChartType
{
    BarColumn,
    HorizontalBar,
    Line,
    Area,
    DonutPie,
    StackedBar,
    ScatterPlot,
    Radar,
    Funnel,
    Waterfall,
    GaugeProgress,
    StepLine,
    Pyramid
}

public enum TablePresetStyle
{
    ModernMinimal,
    EnterpriseBlue,
    DarkModeSlate,
    ZebraStriped,
    FinancialBordered,
    EmeraldGreen,
    AmberAccent,
    CompactClean
}

public enum BarcodeType
{
    Code128,
    Code39,
    Ean13,
    UpcA,
    Itf14,
    Codabar,
    Pdf417
}

public enum QrCodeEccLevel
{
    L, // Low (~7% recovery)
    M, // Medium (~15% recovery)
    Q, // Quartile (~25% recovery)
    H  // High (~30% recovery)
}

public enum QrCodePresetKind
{
    Url,
    Wifi,
    VCard,
    PlainText,
    Email,
    PhoneCall,
    Sms,
    GeoLocation,
    CryptoAddress,
    EventCalendar
}

public enum StampType
{
    Approved,
    Confidential,
    Draft,
    Urgent,
    SignHere,
    Void,
    Completed,
    Final,
    Expired,
    Received,
    Custom
}

public enum RibbonTabKind
{
    File,
    Home,
    Edit,
    Comment,
    Insert,
    Sign,
    Forms,
    Organize,
    Protect,
    Audit,
    Export
}

public enum ToolMode
{
    Select,
    Pan,
    Zoom,
    Text,
    Shape,
    Highlight,
    Draw,
    Form,
    Redact,
    Signature,
    Measure
}

public enum SidebarTabKind
{
    Thumbnails,
    Outline,
    Comments
}


