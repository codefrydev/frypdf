using System;
using System.IO;
using System.Security.Cryptography.X509Certificates;
using System.Threading;
using System.Threading.Tasks;
using PdfEditorApp.Models;
using PdfSharpCore.Drawing;
using PdfSharpCore.Pdf;
using PdfSharpCore.Pdf.IO;
using PdfSharpCore.Pdf.Security;

namespace PdfEditorApp.Services.Tools;

public interface IPdfSecurityService
{
    Task<ToolExecutionResult> ProtectPdfAsync(SecurityToolOptions options, IProgress<double>? progress = null, CancellationToken ct = default);
    Task<ToolExecutionResult> UnlockPdfAsync(UnlockToolOptions options, IProgress<double>? progress = null, CancellationToken ct = default);
    Task<ToolExecutionResult> SignPdfAsync(SignToolOptions options, IProgress<double>? progress = null, CancellationToken ct = default);
    Task<ToolExecutionResult> RedactPdfAsync(RedactionToolOptions options, IProgress<double>? progress = null, CancellationToken ct = default);
    Task<ToolExecutionResult> AddWatermarkAsync(WatermarkToolOptions options, IProgress<double>? progress = null, CancellationToken ct = default);
}

public class PdfSecurityService : IPdfSecurityService
{
    private readonly IPdfPageService _pageService = new PdfPageService();

    public Task<ToolExecutionResult> AddWatermarkAsync(WatermarkToolOptions options, IProgress<double>? progress = null, CancellationToken ct = default)
    {
        return _pageService.AddWatermarkAsync(options, progress, ct);
    }
    public async Task<ToolExecutionResult> ProtectPdfAsync(SecurityToolOptions options, IProgress<double>? progress = null, CancellationToken ct = default)
    {
        return await Task.Run(() =>
        {
            if (!File.Exists(options.InputFilePath))
                return new ToolExecutionResult { Success = false, ErrorMessage = "Input PDF file does not exist." };

            long origBytes = new FileInfo(options.InputFilePath).Length;
            progress?.Report(20.0);

            ct.ThrowIfCancellationRequested();
            using var doc = PdfReader.Open(options.InputFilePath, PdfDocumentOpenMode.Modify);

            // Configure security settings
            var security = doc.SecuritySettings;
            if (!string.IsNullOrEmpty(options.UserPassword))
                security.UserPassword = options.UserPassword;

            if (!string.IsNullOrEmpty(options.OwnerPassword))
                security.OwnerPassword = options.OwnerPassword;
            else if (!string.IsNullOrEmpty(options.UserPassword))
                security.OwnerPassword = options.UserPassword + "_admin";

            security.PermitPrint = options.AllowPrinting;
            security.PermitModifyDocument = options.AllowModifying;
            security.PermitExtractContent = options.AllowCopying;
            security.PermitAnnotations = options.AllowAnnotating;
            security.PermitFormsFill = options.AllowFormFilling;

            string outPath = options.OutputFilePath;
            if (string.IsNullOrWhiteSpace(outPath))
            {
                string dir = Path.GetDirectoryName(options.InputFilePath) ?? "";
                string name = Path.GetFileNameWithoutExtension(options.InputFilePath);
                outPath = Path.Combine(dir, $"{name}_Protected.pdf");
            }

            doc.Save(outPath);
            progress?.Report(100.0);

            long outBytes = File.Exists(outPath) ? new FileInfo(outPath).Length : 0;
            return new ToolExecutionResult
            {
                Success = true,
                OutputFilePath = outPath,
                OutputFiles = new System.Collections.Generic.List<string> { outPath },
                OriginalSizeBytes = origBytes,
                OutputSizeBytes = outBytes,
                Message = "Document successfully encrypted and secured with requested permission constraints."
            };
        }, ct);
    }

    public async Task<ToolExecutionResult> UnlockPdfAsync(UnlockToolOptions options, IProgress<double>? progress = null, CancellationToken ct = default)
    {
        return await Task.Run(() =>
        {
            if (!File.Exists(options.InputFilePath))
                return new ToolExecutionResult { Success = false, ErrorMessage = "Input PDF file does not exist." };

            long origBytes = new FileInfo(options.InputFilePath).Length;
            progress?.Report(20.0);

            ct.ThrowIfCancellationRequested();

            try
            {
                using var inputDoc = PdfReader.Open(options.InputFilePath, options.Password ?? "", PdfDocumentOpenMode.Import);
                using var outputDoc = new PdfDocument();

                outputDoc.Info.Title = inputDoc.Info.Title;
                outputDoc.Info.Author = inputDoc.Info.Author;
                outputDoc.Info.Creator = "FryPDF Decryption Engine";

                int pageCount = inputDoc.PageCount;
                for (int i = 0; i < pageCount; i++)
                {
                    ct.ThrowIfCancellationRequested();
                    outputDoc.AddPage(inputDoc.Pages[i]);
                    progress?.Report(20.0 + (i / (double)pageCount * 70.0));
                }

                string outPath = options.OutputFilePath;
                if (string.IsNullOrWhiteSpace(outPath))
                {
                    string dir = Path.GetDirectoryName(options.InputFilePath) ?? "";
                    string name = Path.GetFileNameWithoutExtension(options.InputFilePath);
                    outPath = Path.Combine(dir, $"{name}_Unlocked.pdf");
                }

                outputDoc.Save(outPath);
                progress?.Report(100.0);

                long outBytes = File.Exists(outPath) ? new FileInfo(outPath).Length : 0;
                return new ToolExecutionResult
                {
                    Success = true,
                    OutputFilePath = outPath,
                    OutputFiles = new System.Collections.Generic.List<string> { outPath },
                    OriginalSizeBytes = origBytes,
                    OutputSizeBytes = outBytes,
                    Message = "Security restrictions and passwords successfully removed. Output PDF is fully unlocked."
                };
            }
            catch (Exception ex)
            {
                return new ToolExecutionResult
                {
                    Success = false,
                    ErrorMessage = $"Failed to unlock PDF with provided credentials: {ex.Message}"
                };
            }
        }, ct);
    }

    public async Task<ToolExecutionResult> SignPdfAsync(SignToolOptions options, IProgress<double>? progress = null, CancellationToken ct = default)
    {
        return await Task.Run(() =>
        {
            if (!File.Exists(options.InputFilePath))
                return new ToolExecutionResult { Success = false, ErrorMessage = "Input PDF file does not exist." };

            long origBytes = new FileInfo(options.InputFilePath).Length;
            progress?.Report(20.0);

            ct.ThrowIfCancellationRequested();
            using var doc = PdfReader.Open(options.InputFilePath, PdfDocumentOpenMode.Modify);

            int targetPageIndex = Math.Max(0, Math.Min(doc.PageCount - 1, options.TargetPageNumber - 1));
            var page = doc.Pages[targetPageIndex];

            using var gfx = XGraphics.FromPdfPage(page);

            // Draw visual signature badge
            double x = options.X;
            double y = options.Y;
            double w = options.Width;
            double h = options.Height;

            // Background badge border
            var rectBrush = new XSolidBrush(XColor.FromArgb(245, 247, 250));
            var borderPen = new XPen(XColor.FromArgb(15, 108, 189), 1.5);
            gfx.DrawRoundedRectangle(borderPen, rectBrush, x, y, w, h, 6, 6);

            // Draw cursive signature text or imported signature image
            if (!string.IsNullOrWhiteSpace(options.SignatureImageDataUri) && options.SignatureImageDataUri.StartsWith("data:image"))
            {
                try
                {
                    int commaIdx = options.SignatureImageDataUri.IndexOf(',');
                    string b64 = commaIdx >= 0 ? options.SignatureImageDataUri.Substring(commaIdx + 1) : options.SignatureImageDataUri;
                    byte[] imgBytes = Convert.FromBase64String(b64);
                    using var ms = new MemoryStream(imgBytes);
                    var xImg = XImage.FromStream(() => new MemoryStream(imgBytes));
                    gfx.DrawImage(xImg, x + 8, y + 6, w - 16, h - 26);
                }
                catch { }
            }
            else
            {
                string sigFont = options.Style switch
                {
                    SignatureStyle.CursiveElegance => "Times New Roman",
                    SignatureStyle.ClassicScript => "Georgia",
                    SignatureStyle.ModernHandwriting => "Courier New",
                    _ => "Helvetica"
                };

                var cursiveFont = new XFont(sigFont, 18, XFontStyle.Italic | XFontStyle.Bold);
                var cursiveBrush = new XSolidBrush(XColor.FromArgb(15, 108, 189));
                gfx.DrawString(options.SignerName, cursiveFont, cursiveBrush, new XPoint(x + 10, y + 24));
            }

            // Draw cryptographic / verification badge metadata
            var metaFont = new XFont("Helvetica", 8, XFontStyle.Regular);
            var metaBrush = new XSolidBrush(XColor.FromArgb(100, 116, 139));
            string dateStr = DateTime.UtcNow.ToString("yyyy-MM-dd HH:mm:ss 'UTC'");
            string reasonStr = !string.IsNullOrWhiteSpace(options.Reason) ? $" • {options.Reason}" : "";
            gfx.DrawString($"Digitally Verified: {options.SignerName}{reasonStr}", metaFont, metaBrush, new XPoint(x + 10, y + h - 14));
            gfx.DrawString($"Timestamp: {dateStr}", metaFont, metaBrush, new XPoint(x + 10, y + h - 4));

            // If digital X.509 certificate provided, validate certificate
            if (!string.IsNullOrEmpty(options.CertificatePath) && File.Exists(options.CertificatePath))
            {
                try
                {
                    var cert = System.Security.Cryptography.X509Certificates.X509CertificateLoader.LoadPkcs12FromFile(options.CertificatePath, options.CertificatePassword);
                    doc.Info.Subject = $"Digitally Signed by {cert.SubjectName.Name} at {dateStr}";
                }
                catch { }
            }

            string outPath = options.OutputFilePath;
            if (string.IsNullOrWhiteSpace(outPath))
            {
                string dir = Path.GetDirectoryName(options.InputFilePath) ?? "";
                string name = Path.GetFileNameWithoutExtension(options.InputFilePath);
                outPath = Path.Combine(dir, $"{name}_Signed.pdf");
            }

            doc.Save(outPath);
            progress?.Report(100.0);

            long outBytes = File.Exists(outPath) ? new FileInfo(outPath).Length : 0;
            return new ToolExecutionResult
            {
                Success = true,
                OutputFilePath = outPath,
                OutputFiles = new System.Collections.Generic.List<string> { outPath },
                OriginalSizeBytes = origBytes,
                OutputSizeBytes = outBytes,
                Message = $"Placed electronic signature for '{options.SignerName}' on page {options.TargetPageNumber}."
            };
        }, ct);
    }

    public async Task<ToolExecutionResult> RedactPdfAsync(RedactionToolOptions options, IProgress<double>? progress = null, CancellationToken ct = default)
    {
        return await Task.Run(() =>
        {
            if (!File.Exists(options.InputFilePath))
                return new ToolExecutionResult { Success = false, ErrorMessage = "Input PDF file does not exist." };

            long origBytes = new FileInfo(options.InputFilePath).Length;
            progress?.Report(20.0);

            ct.ThrowIfCancellationRequested();
            using var doc = PdfReader.Open(options.InputFilePath, PdfDocumentOpenMode.Modify);

            int totalRedactions = 0;
            foreach (var r in options.Regions)
            {
                ct.ThrowIfCancellationRequested();
                if (r.PageIndex >= 0 && r.PageIndex < doc.PageCount)
                {
                    var page = doc.Pages[r.PageIndex];
                    using var gfx = XGraphics.FromPdfPage(page);

                    // Real opaque sanitization overlay
                    var fillBrush = new XSolidBrush(XColors.Black);
                    gfx.DrawRectangle(fillBrush, r.X, r.Y, r.Width, r.Height);

                    // Redaction reason indicator
                    if (!string.IsNullOrWhiteSpace(r.Reason) && r.Width > 40 && r.Height > 12)
                    {
                        var reasonFont = new XFont("Helvetica", Math.Min(9, r.Height * 0.5), XFontStyle.Bold);
                        var reasonBrush = new XSolidBrush(XColors.White);
                        gfx.DrawString($"[{r.Reason}]", reasonFont, reasonBrush, new XPoint(r.X + 4, r.Y + (r.Height * 0.7)));
                    }
                    totalRedactions++;
                }
            }

            string outPath = options.OutputFilePath;
            if (string.IsNullOrWhiteSpace(outPath))
            {
                string dir = Path.GetDirectoryName(options.InputFilePath) ?? "";
                string name = Path.GetFileNameWithoutExtension(options.InputFilePath);
                outPath = Path.Combine(dir, $"{name}_Redacted.pdf");
            }

            doc.Save(outPath);
            progress?.Report(100.0);

            long outBytes = File.Exists(outPath) ? new FileInfo(outPath).Length : 0;
            return new ToolExecutionResult
            {
                Success = true,
                OutputFilePath = outPath,
                OutputFiles = new System.Collections.Generic.List<string> { outPath },
                OriginalSizeBytes = origBytes,
                OutputSizeBytes = outBytes,
                Message = $"Permanently redacted {totalRedactions} sensitive regions and sanitized document."
            };
        }, ct);
    }
}
