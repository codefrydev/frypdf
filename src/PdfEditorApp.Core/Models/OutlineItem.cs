namespace PdfEditorApp.Core.Models;

public class OutlineItem
{
    public string Title { get; set; } = "";
    public int PageIndex { get; set; }
    public string Kind { get; set; } = "Heading";
}
