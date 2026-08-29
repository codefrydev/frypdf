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
    StickyNote
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
    Completed
}

public enum RibbonTabKind
{
    File,
    Home,
    Edit,
    Comment,
    Insert,
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
    Draw
}
