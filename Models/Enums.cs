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
    Card,
    RoundedRectangle,
    Circle,
    Line,
    Arrow,
    Star,
    Callout,
    StickyNote
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
