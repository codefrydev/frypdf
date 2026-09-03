using System;
using System.Collections.Generic;

namespace PdfEditorApp.Core.Models;

public class PdfDocumentModel
{
    public string Id { get; set; } = Guid.NewGuid().ToString("N");
    public string Title { get; set; } = "Document_2026.pdf";
    public string Author { get; set; } = "CodeFryDev";
    public string Subject { get; set; } = "Annual Financial & Operations Report";
    public string Keywords { get; set; } = "";
    public string Creator { get; set; } = "FryPDF";
    public string Producer { get; set; } = "codefrydev.in";
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
            Keywords = Keywords,
            Creator = Creator,
            Producer = Producer,
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
