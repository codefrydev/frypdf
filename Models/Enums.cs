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
    Ink
}

public enum FormFieldType
{
    Text,
    MultilineText,
    Checkbox,
    Radio,
    Dropdown,
    Signature,
    Button
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
    Executive
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
    ArrowRight,
    ArrowLeft,
    Callout,
    Heart,
    Cloud,
    Line,
    Arrow,
    Star,
    Card,
    StickyNote
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

public enum QrCodePresetKind
{
    Url,
    Wifi,
    VCard,
    PlainText,
    Email,
    PhoneCall,
    GeoLocation
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
    Expired
}

public enum RibbonTabKind
{
    File,
    Home,
    Edit,
    Comment,
    Insert,
    Forms,
    Organize,
    Export
}

public enum ToolMode
{
    Select,
    Pan,
    Text,
    Shape,
    Highlight,
    Draw,
    Form,
    Redact
}

public enum SidebarTabKind
{
    Thumbnails,
    Outline,
    Comments
}
