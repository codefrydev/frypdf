namespace PdfEditorApp.Models;

public class CommentItem
{
    public string Author { get; set; } = "";
    public string Timestamp { get; set; } = "";
    public string Text { get; set; } = "";
    public string Status { get; set; } = "";
    public int PageIndex { get; set; }
}
