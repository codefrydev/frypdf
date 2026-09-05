using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using PdfEditorApp.Core.Models;
using PdfEditorApp.Core.Models.Elements;
using PdfEditorApp.Core.Plugins.Descriptors;

namespace PdfEditorApp.Services.Export;

public class DocumentExporterRegistry : IDocumentExporterRegistry
{
    private readonly ConcurrentDictionary<string, IDocumentExporter> _exporters = new(StringComparer.OrdinalIgnoreCase);

    public event Action? RegistryChanged;

    public DocumentExporterRegistry(IPdfExportService? pdfExportService = null)
    {
        RegisterBuiltInExporters(pdfExportService);
    }

    private void RegisterBuiltInExporters(IPdfExportService? pdfExportService)
    {
        RegisterExporter(new PdfDocumentExporter(pdfExportService));
        RegisterExporter(new MarkdownDocumentExporter());
        RegisterExporter(new HtmlDocumentExporter());
        RegisterExporter(new PlainTextDocumentExporter());
        RegisterExporter(new SvgVectorExporter());
    }

    public IDisposable RegisterExporter(IDocumentExporter exporter)
    {
        ArgumentNullException.ThrowIfNull(exporter);
        _exporters[exporter.ExporterId] = exporter;
        RegistryChanged?.Invoke();

        return new DisposableAction(() =>
        {
            _exporters.TryRemove(exporter.ExporterId, out _);
            RegistryChanged?.Invoke();
        });
    }

    public IDocumentExporter? GetExporter(string exporterId)
    {
        if (string.IsNullOrWhiteSpace(exporterId)) return null;
        return _exporters.GetValueOrDefault(exporterId);
    }

    public IDocumentExporter? FindExporterByExtension(string extension)
    {
        if (string.IsNullOrWhiteSpace(extension)) return null;
        var ext = extension.StartsWith('.') ? extension : "." + extension;

        return _exporters.Values
            .FirstOrDefault(e => string.Equals(e.DefaultExtension, ext, StringComparison.OrdinalIgnoreCase));
    }

    public IReadOnlyList<IDocumentExporter> GetAllExporters()
    {
        return _exporters.Values.ToList();
    }

    private sealed class DisposableAction(Action action) : IDisposable
    {
        private Action? _action = action;
        public void Dispose() => Interlocked.Exchange(ref _action, null)?.Invoke();
    }
}

/// <summary>
/// Default PDF exporter delegating to IPdfExportService.
/// </summary>
public class PdfDocumentExporter : IDocumentExporter
{
    private readonly IPdfExportService? _exportService;

    public PdfDocumentExporter(IPdfExportService? exportService = null)
    {
        _exportService = exportService;
    }

    public string ExporterId => "frypdf.exporter.pdf";
    public string DisplayName => "PDF Document (.pdf)";
    public string DefaultExtension => ".pdf";
    public string FileFilterDescription => "PDF Document (*.pdf)|*.pdf";
    public string IconKind => "FilePdfBox";

    public Task<byte[]> ExportAsync(PdfDocumentModel document, DocumentExportOptions options, CancellationToken ct = default)
    {
        if (_exportService != null)
        {
            return Task.FromResult(_exportService.GeneratePdfBytes(document));
        }

        // Fallback directly to PdfExportService
        var fallback = new PdfExportService();
        return Task.FromResult(fallback.GeneratePdfBytes(document));
    }
}

/// <summary>
/// Markdown exporter extracting text blocks, headings, and tables to formatted Markdown.
/// </summary>
public class MarkdownDocumentExporter : IDocumentExporter
{
    public string ExporterId => "frypdf.exporter.markdown";
    public string DisplayName => "GitHub-Flavored Markdown (.md)";
    public string DefaultExtension => ".md";
    public string FileFilterDescription => "Markdown Document (*.md)|*.md";
    public string IconKind => "LanguageMarkdown";

    public Task<byte[]> ExportAsync(PdfDocumentModel document, DocumentExportOptions options, CancellationToken ct = default)
    {
        var sb = new StringBuilder();
        sb.AppendLine($"# {document.Title}");
        if (!string.IsNullOrWhiteSpace(document.Author))
        {
            sb.AppendLine($"*Author: {document.Author}*");
        }
        sb.AppendLine();

        int pageNum = 1;
        foreach (var page in document.Pages)
        {
            if (document.Pages.Count > 1)
            {
                sb.AppendLine($"---");
                sb.AppendLine($"## Page {pageNum}");
                sb.AppendLine();
            }

            var sortedElements = page.Elements.OrderBy(e => e.Y).ThenBy(e => e.X);
            foreach (var el in sortedElements)
            {
                if (el is PdfTextElement text && !string.IsNullOrWhiteSpace(text.Text))
                {
                    if (text.FontSize >= 20)
                    {
                        sb.AppendLine($"# {text.Text.Trim()}");
                    }
                    else if (text.FontSize >= 15)
                    {
                        sb.AppendLine($"## {text.Text.Trim()}");
                    }
                    else if (text.FontSize >= 12 && text.IsBold)
                    {
                        sb.AppendLine($"### {text.Text.Trim()}");
                    }
                    else
                    {
                        sb.AppendLine(text.Text.Trim());
                    }
                    sb.AppendLine();
                }
                else if (el is PdfTableElement table)
                {
                    sb.AppendLine($"*[Table: {table.Rows.Count}x{table.Headers.Count}]*");
                    sb.AppendLine();
                }
            }

            pageNum++;
        }

        return Task.FromResult(Encoding.UTF8.GetBytes(sb.ToString()));
    }
}

/// <summary>
/// Plain text exporter extracting raw text in layout order.
/// </summary>
public class PlainTextDocumentExporter : IDocumentExporter
{
    public string ExporterId => "frypdf.exporter.text";
    public string DisplayName => "Plain Text (.txt)";
    public string DefaultExtension => ".txt";
    public string FileFilterDescription => "Text Document (*.txt)|*.txt";
    public string IconKind => "TextRecognition";

    public Task<byte[]> ExportAsync(PdfDocumentModel document, DocumentExportOptions options, CancellationToken ct = default)
    {
        var sb = new StringBuilder();
        foreach (var page in document.Pages)
        {
            var textElements = page.Elements.OfType<PdfTextElement>().OrderBy(e => e.Y).ThenBy(e => e.X);
            foreach (var t in textElements)
            {
                if (!string.IsNullOrWhiteSpace(t.Text))
                {
                    sb.AppendLine(t.Text);
                }
            }
            sb.AppendLine();
        }

        return Task.FromResult(Encoding.UTF8.GetBytes(sb.ToString()));
    }
}

/// <summary>
/// HTML5 document exporter rendering styled semantic HTML.
/// </summary>
public class HtmlDocumentExporter : IDocumentExporter
{
    public string ExporterId => "frypdf.exporter.html";
    public string DisplayName => "Web Page HTML5 (.html)";
    public string DefaultExtension => ".html";
    public string FileFilterDescription => "HTML Document (*.html)|*.html";
    public string IconKind => "LanguageHtml5";

    public Task<byte[]> ExportAsync(PdfDocumentModel document, DocumentExportOptions options, CancellationToken ct = default)
    {
        var sb = new StringBuilder();
        sb.AppendLine("<!DOCTYPE html>");
        sb.AppendLine("<html lang=\"en\">");
        sb.AppendLine("<head>");
        sb.AppendLine("  <meta charset=\"UTF-8\">");
        sb.AppendLine($"  <title>{System.Net.WebUtility.HtmlEncode(document.Title)}</title>");
        sb.AppendLine("  <style>");
        sb.AppendLine("    body { font-family: -apple-system, BlinkMacSystemFont, 'Segoe UI', Roboto, sans-serif; margin: 40px auto; max-width: 800px; color: #1e293b; line-height: 1.6; }");
        sb.AppendLine("    .page { background: #fff; padding: 40px; margin-bottom: 30px; box-shadow: 0 4px 12px rgba(0,0,0,0.08); border-radius: 8px; }");
        sb.AppendLine("    h1, h2, h3 { color: #0f172a; }");
        sb.AppendLine("  </style>");
        sb.AppendLine("</head>");
        sb.AppendLine("<body>");

        int pageNum = 1;
        foreach (var page in document.Pages)
        {
            sb.AppendLine($"  <div class=\"page\" id=\"page-{pageNum}\">");
            var sorted = page.Elements.OrderBy(e => e.Y).ThenBy(e => e.X);
            foreach (var el in sorted)
            {
                if (el is PdfTextElement text && !string.IsNullOrWhiteSpace(text.Text))
                {
                    var encoded = System.Net.WebUtility.HtmlEncode(text.Text);
                    if (text.FontSize >= 20) sb.AppendLine($"    <h1>{encoded}</h1>");
                    else if (text.FontSize >= 15) sb.AppendLine($"    <h2>{encoded}</h2>");
                    else sb.AppendLine($"    <p>{encoded}</p>");
                }
            }
            sb.AppendLine("  </div>");
            pageNum++;
        }

        sb.AppendLine("</body>");
        sb.AppendLine("</html>");

        return Task.FromResult(Encoding.UTF8.GetBytes(sb.ToString()));
    }
}

/// <summary>
/// SVG vector document exporter.
/// </summary>
public class SvgVectorExporter : IDocumentExporter
{
    public string ExporterId => "frypdf.exporter.svg";
    public string DisplayName => "Scalable Vector Graphics (.svg)";
    public string DefaultExtension => ".svg";
    public string FileFilterDescription => "Scalable Vector Graphics (*.svg)|*.svg";
    public string IconKind => "Svg";

    public Task<byte[]> ExportAsync(PdfDocumentModel document, DocumentExportOptions options, CancellationToken ct = default)
    {
        var firstPage = document.Pages.FirstOrDefault() ?? new PdfPageModel { Width = 600, Height = 800 };
        var sb = new StringBuilder();
        sb.AppendLine($"<svg xmlns=\"http://www.w3.org/2000/svg\" viewBox=\"0 0 {firstPage.Width} {firstPage.Height}\" width=\"{firstPage.Width}\" height=\"{firstPage.Height}\">");
        sb.AppendLine($"  <!-- Generated by FryPDF Exporter -->");
        sb.AppendLine($"  <rect width=\"100%\" height=\"100%\" fill=\"#ffffff\"/>");

        foreach (var el in firstPage.Elements.OrderBy(e => e.ZIndex))
        {
            if (el is PdfTextElement text && !string.IsNullOrWhiteSpace(text.Text))
            {
                var encoded = System.Net.WebUtility.HtmlEncode(text.Text);
                sb.AppendLine($"  <text x=\"{text.X}\" y=\"{text.Y + text.FontSize}\" font-size=\"{text.FontSize}\" font-family=\"{text.FontFamily}\" fill=\"{text.TextColorHex}\">{encoded}</text>");
            }
            else if (el is PdfShapeElement shape)
            {
                sb.AppendLine($"  <rect x=\"{shape.X}\" y=\"{shape.Y}\" width=\"{shape.Width}\" height=\"{shape.Height}\" fill=\"{shape.FillColorHex}\" stroke=\"{shape.StrokeColorHex}\" stroke-width=\"{shape.StrokeThickness}\" rx=\"{shape.CornerRadius}\"/>");
            }
        }

        sb.AppendLine("</svg>");
        return Task.FromResult(Encoding.UTF8.GetBytes(sb.ToString()));
    }
}
