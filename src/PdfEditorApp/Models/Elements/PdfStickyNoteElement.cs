using System;

namespace PdfEditorApp.Models.Elements;

public class PdfStickyNoteElement : PdfElementBase
{
    public override ElementKind Kind => ElementKind.StickyNote;

    public string Author { get; set; } = "Senior Reviewer";
    public string Timestamp { get; set; } = DateTime.Now.ToString("MMM dd, yyyy HH:mm");
    public string NoteText { get; set; } = "Please verify the audit figures with the legal compliance team prior to final PDF release.";
    public string Status { get; set; } = "Pending Review";
    public string ColorHex { get; set; } = "#FEF3C7";
    public string BorderColorHex { get; set; } = "#F59E0B";
    public bool IsExpanded { get; set; } = true;

    public override PdfElementBase Clone()
    {
        return (PdfStickyNoteElement)base.Clone();
    }
}
