using System;
using QuestPDF.Fluent;
using QuestPDF.Infrastructure;

namespace PdfEditorApp.Services;

/// <summary>
/// Universal QuestPDF IDocument wrapper ensuring enterprise FryPDF Creator and codefrydev.in Producer metadata branding.
/// </summary>
public class FryPdfDocument : IDocument
{
    private readonly Action<IDocumentContainer> _compose;
    private readonly string _title;
    private readonly string _author;
    private readonly string _subject;
    private readonly string _keywords;
    private readonly string _creator;
    private readonly string _producer;

    public FryPdfDocument(
        Action<IDocumentContainer> compose,
        string? title = null,
        string? author = null,
        string? subject = null,
        string? keywords = null,
        string? creator = null,
        string? producer = null)
    {
        _compose = compose;
        _title = title ?? "FryPDF Document";
        _author = author ?? "FryPDF";
        _subject = subject ?? "";
        _keywords = keywords ?? "";
        _creator = !string.IsNullOrWhiteSpace(creator) ? creator : "FryPDF";
        _producer = !string.IsNullOrWhiteSpace(producer) ? producer : "codefrydev.in";
    }

    public DocumentMetadata GetMetadata() => new DocumentMetadata
    {
        Title = _title,
        Author = _author,
        Subject = _subject,
        Keywords = _keywords,
        Creator = _creator,
        Producer = _producer,
        CreationDate = DateTime.UtcNow,
        ModifiedDate = DateTime.UtcNow
    };

    public DocumentSettings GetSettings() => DocumentSettings.Default;

    public void Compose(IDocumentContainer container) => _compose(container);

    public static IDocument Create(
        Action<IDocumentContainer> compose,
        string? title = null,
        string? author = null,
        string? subject = null,
        string? keywords = null,
        string? creator = null,
        string? producer = null)
    {
        return new FryPdfDocument(compose, title, author, subject, keywords, creator, producer);
    }
}
