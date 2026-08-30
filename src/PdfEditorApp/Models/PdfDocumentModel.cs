using System;
using System.Collections.Generic;

namespace PdfEditorApp.Models;

public class PdfDocumentModel
{
    public string Id { get; set; } = Guid.NewGuid().ToString("N");
    public string Title { get; set; } = "Annual_Report_2026.pdf";
    public string Author { get; set; } = "Acme Corp.";
    public string Subject { get; set; } = "Annual Financial & Operations Report";
    public DateTime CreatedDate { get; set; } = DateTime.Now;
    public DateTime ModifiedDate { get; set; } = DateTime.Now;

    public List<PdfPageModel> Pages { get; set; } = new();
    public PdfSecuritySettings SecuritySettings { get; set; } = new();

    public PdfDocumentModel Clone()
    {
        var clone = new PdfDocumentModel
        {
            Id = Guid.NewGuid().ToString("N"),
            Title = Title,
            Author = Author,
            Subject = Subject,
            CreatedDate = CreatedDate,
            ModifiedDate = DateTime.Now,
            SecuritySettings = SecuritySettings.Clone()
        };

        foreach (var page in Pages)
        {
            clone.Pages.Add(page.Clone());
        }

        return clone;
    }
}
