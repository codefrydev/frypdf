using System;

namespace PdfEditorApp.Models;

public class CommandPaletteItem
{
    public string Title { get; set; } = "";
    public string Subtitle { get; set; } = "";
    public string Category { get; set; } = "General";
    public string IconKind { get; set; } = "LightningBolt";
    public string Shortcut { get; set; } = "";
    public Action Action { get; set; } = () => { };
}
