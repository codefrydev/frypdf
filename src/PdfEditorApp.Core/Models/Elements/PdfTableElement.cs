using System.Collections.Generic;

namespace PdfEditorApp.Core.Models.Elements;

public class PdfTableElement : PdfElementBase
{
    public override ElementKind Kind => ElementKind.Table;

    public List<string> Headers { get; set; } = new() { "Item Description", "Qty", "Rate", "Amount" };
    public List<List<string>> Rows { get; set; } = new()
    {
        new() { "Cloud Architecture Consulting", "40 hrs", "$150.00", "$6,000.00" },
        new() { "Avalonia Desktop UI Engineering", "60 hrs", "$140.00", "$8,400.00" },
        new() { "QuestPDF Engine Integration", "25 hrs", "$160.00", "$4,000.00" },
        new() { "QA, Testing & Performance Tuning", "15 hrs", "$120.00", "$1,800.00" }
    };

    public string HeaderBackgroundHex { get; set; } = "#0F6CBD";
    public string HeaderTextHex { get; set; } = "#FFFFFF";
    public string AlternateRowBackgroundHex { get; set; } = "#F8F9FA";
    public string BorderColorHex { get; set; } = "#E1DFDD";

    public override PdfElementBase Clone()
    {
        var clone = (PdfTableElement)base.Clone();
        clone.Headers = new List<string>(Headers);
        clone.Rows = new List<List<string>>();
        foreach (var row in Rows)
        {
            clone.Rows.Add(new List<string>(row));
        }
        return clone;
    }
}
