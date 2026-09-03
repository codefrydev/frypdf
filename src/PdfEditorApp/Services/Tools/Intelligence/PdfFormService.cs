using PdfEditorApp.Services.Tools.Core;
using System;
using System.Collections.Generic;
using System.IO;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using PdfEditorApp.Models;
using PdfSharpCore.Pdf;
using PdfSharpCore.Pdf.AcroForms;
using PdfSharpCore.Pdf.IO;

namespace PdfEditorApp.Services.Tools.Intelligence;

public interface IPdfFormService
{
    Task<ToolExecutionResult> ProcessPdfFormsAsync(FormToolOptions options, IProgress<double>? progress = null, CancellationToken ct = default);
    Task<Dictionary<string, string>> GetFormFieldsAsync(string pdfPath, CancellationToken ct = default);
    Task<Dictionary<string, string>> ExtractFormFieldsAsync(string pdfPath, CancellationToken ct = default);
}

public class PdfFormService : IPdfFormService
{
    public Task<Dictionary<string, string>> ExtractFormFieldsAsync(string pdfPath, CancellationToken ct = default) => GetFormFieldsAsync(pdfPath, ct);

    public async Task<Dictionary<string, string>> GetFormFieldsAsync(string pdfPath, CancellationToken ct = default)
    {
        return await Task.Run(() =>
        {
            var dict = new Dictionary<string, string>();
            if (!File.Exists(pdfPath)) return dict;

            try
            {
                using var doc = PdfReader.Open(pdfPath, PdfDocumentOpenMode.ReadOnly);
                var form = doc.AcroForm;
                if (form != null)
                {
                    foreach (var name in form.Fields.Names)
                    {
                        var field = form.Fields[name];
                        dict[name] = field?.Value?.ToString() ?? "";
                    }
                }
            }
            catch { }

            return dict;
        }, ct);
    }

    public async Task<ToolExecutionResult> ProcessPdfFormsAsync(FormToolOptions options, IProgress<double>? progress = null, CancellationToken ct = default)
    {
        return await Task.Run(() =>
        {
            if (!File.Exists(options.InputFilePath))
                return new ToolExecutionResult { Success = false, ErrorMessage = "Input PDF file does not exist." };

            long origBytes = new FileInfo(options.InputFilePath).Length;
            string outPath = options.OutputFilePath;
            if (string.IsNullOrWhiteSpace(outPath))
            {
                string dir = Path.GetDirectoryName(options.InputFilePath) ?? "";
                string name = Path.GetFileNameWithoutExtension(options.InputFilePath);
                outPath = Path.Combine(dir, $"{name}_FormFilled.pdf");
            }

            ct.ThrowIfCancellationRequested();
            progress?.Report(20.0);

            using var doc = PdfReader.Open(options.InputFilePath, PdfDocumentOpenMode.Modify);
            var form = doc.AcroForm;

            int updatedFields = 0;
            if (form != null && options.FieldValues != null)
            {
                foreach (var kvp in options.FieldValues)
                {
                    if (form.Fields.Names.Contains(kvp.Key))
                    {
                        var field = form.Fields[kvp.Key];
                        if (field is PdfTextField textField)
                        {
                            textField.Text = kvp.Value;
                            updatedFields++;
                        }
                        else if (field is PdfCheckBoxField checkBoxField)
                        {
                            checkBoxField.Checked = bool.TryParse(kvp.Value, out bool b) && b;
                            updatedFields++;
                        }
                    }
                }

                if (options.FlattenFields)
                {
                    // Flatten: remove the AcroForm entry so fields become static content
                    if (doc.AcroForm != null)
                        doc.Internals.Catalog.Elements.Remove("/AcroForm");
                }
            }

            progress?.Report(80.0);
            PdfFileHelper.SaveDocumentWithFryPdfMetadata(doc, outPath);

            if (options.ExportFieldValuesJson)
            {
                string jsonPath = Path.ChangeExtension(outPath, ".json");
                string json = JsonSerializer.Serialize(options.FieldValues, new JsonSerializerOptions { WriteIndented = true });
                File.WriteAllText(jsonPath, json);
            }

            progress?.Report(100.0);

            long outBytes = File.Exists(outPath) ? new FileInfo(outPath).Length : 0;
            return new ToolExecutionResult
            {
                Success = true,
                OutputFilePath = outPath,
                OutputFiles = new List<string> { outPath },
                OriginalSizeBytes = origBytes,
                OutputSizeBytes = outBytes,
                Message = $"Processed AcroForm with {updatedFields} filled fields."
            };
        }, ct);
    }
}
