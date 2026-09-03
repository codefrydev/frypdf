using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using PdfEditorApp.Models;
using PdfEditorApp.Services.Tools.Core;
using QuestPDF.Fluent;

namespace PdfEditorApp.Services.Tools.Security;

/// <summary>
/// High-performance, extensible QuestPDF native operations engine.
/// Encapsulates native Skia/C++ document operations:
/// multi-document merging with range syntax, AES-256 encryption, decryption,
/// Fast Web View linearization, vector PDF overlays/underlays, attachments, and XMP metadata.
/// </summary>
public interface IQuestPdfOperationsEngine
{
    Task<ToolExecutionResult> MergeAsync(MergeToolOptions options, IProgress<double>? progress = null, CancellationToken ct = default);
    Task<ToolExecutionResult> EncryptAsync(SecurityToolOptions options, IProgress<double>? progress = null, CancellationToken ct = default);
    Task<ToolExecutionResult> DecryptAsync(UnlockToolOptions options, IProgress<double>? progress = null, CancellationToken ct = default);
    Task<ToolExecutionResult> LinearizeAsync(string inputPath, string outputPath, IProgress<double>? progress = null, CancellationToken ct = default);
    Task<ToolExecutionResult> ApplyLayerAsync(string inputPath, string layerPdfPath, bool isOverlay, string targetPages, string? outputPath = null, IProgress<double>? progress = null, CancellationToken ct = default);
    Task<ToolExecutionResult> AddAttachmentAsync(string inputPath, string attachmentFilePath, string? description = null, string? mimeType = null, DocumentOperation.DocumentAttachmentRelationship relationship = DocumentOperation.DocumentAttachmentRelationship.Unspecified, string? outputPath = null, IProgress<double>? progress = null, CancellationToken ct = default);
    Task<ToolExecutionResult> ExtendMetadataAsync(string inputPath, string customXmpXml, string? outputPath = null, IProgress<double>? progress = null, CancellationToken ct = default);
}

public class QuestPdfOperationsEngine : IQuestPdfOperationsEngine
{
    public async Task<ToolExecutionResult> MergeAsync(MergeToolOptions options, IProgress<double>? progress = null, CancellationToken ct = default)
    {
        return await Task.Run(() =>
        {
            if (options.InputFiles == null || options.InputFiles.Count == 0)
                return new ToolExecutionResult { Success = false, ErrorMessage = "No input PDF files provided for merging." };

            var validFiles = options.InputFiles.Where(File.Exists).ToList();
            if (validFiles.Count == 0)
                return new ToolExecutionResult { Success = false, ErrorMessage = "None of the specified input PDF files exist." };

            long totalOriginalBytes = validFiles.Sum(f => new FileInfo(f).Length);
            string outPath = options.OutputFilePath;
            if (string.IsNullOrWhiteSpace(outPath))
            {
                string dir = Path.GetDirectoryName(validFiles[0]) ?? Path.GetTempPath();
                outPath = PdfFileHelper.ResolveSafeOutputPath(null, dir, "Merged_Document", ".pdf", validFiles);
            }

            EnsureDirectoryExists(outPath);
            progress?.Report(10.0);
            ct.ThrowIfCancellationRequested();

            try
            {
                string firstFile = validFiles[0];
                var op = DocumentOperation.LoadFile(firstFile);

                if (options.FilePageRanges != null &&
                    options.FilePageRanges.TryGetValue(firstFile, out var firstRange) &&
                    !string.IsNullOrWhiteSpace(firstRange))
                {
                    op = op.TakePages(firstRange);
                }

                for (int i = 1; i < validFiles.Count; i++)
                {
                    ct.ThrowIfCancellationRequested();
                    string f = validFiles[i];
                    string? range = null;
                    options.FilePageRanges?.TryGetValue(f, out range);

                    op = string.IsNullOrWhiteSpace(range)
                        ? op.MergeFile(f)
                        : op.MergeFile(f, range);

                    progress?.Report(10.0 + ((i + 1) / (double)validFiles.Count * 80.0));
                }

                op.Save(outPath);
                progress?.Report(100.0);

                long outBytes = File.Exists(outPath) ? new FileInfo(outPath).Length : 0;
                return new ToolExecutionResult
                {
                    Success = true,
                    OutputFilePath = outPath,
                    OutputFiles = new List<string> { outPath },
                    OriginalSizeBytes = totalOriginalBytes,
                    OutputSizeBytes = outBytes,
                    Message = $"Successfully merged {validFiles.Count} files into {Path.GetFileName(outPath)} via QuestPDF Native Engine."
                };
            }
            catch (Exception ex)
            {
                return new ToolExecutionResult
                {
                    Success = false,
                    ErrorMessage = $"QuestPDF document merge failed: {ex.Message}"
                };
            }
        }, ct);
    }

    public async Task<ToolExecutionResult> EncryptAsync(SecurityToolOptions options, IProgress<double>? progress = null, CancellationToken ct = default)
    {
        return await Task.Run(() =>
        {
            if (!File.Exists(options.InputFilePath))
                return new ToolExecutionResult { Success = false, ErrorMessage = "Input PDF file does not exist." };

            long origBytes = new FileInfo(options.InputFilePath).Length;
            progress?.Report(20.0);
            ct.ThrowIfCancellationRequested();

            string outPath = options.OutputFilePath;
            if (string.IsNullOrWhiteSpace(outPath))
            {
                string dir = Path.GetDirectoryName(options.InputFilePath) ?? "";
                string name = Path.GetFileNameWithoutExtension(options.InputFilePath);
                outPath = Path.Combine(dir, $"{name}_Protected.pdf");
            }

            EnsureDirectoryExists(outPath);

            string userPwd = options.UserPassword ?? string.Empty;
            string ownerPwd = options.OwnerPassword ?? string.Empty;
            string? generatedOwnerPassword = null;
            if (string.IsNullOrEmpty(ownerPwd) && !string.IsNullOrEmpty(userPwd))
            {
                generatedOwnerPassword = GenerateRandomPassword();
                ownerPwd = generatedOwnerPassword;
            }

            try
            {
                var op = DocumentOperation.LoadFile(options.InputFilePath);
                switch (options.EncryptionLevel)
                {
                    case PdfEncryptionLevel.Aes256Bit:
                        op = op.Encrypt(new DocumentOperation.Encryption256Bit
                        {
                            UserPassword = userPwd,
                            OwnerPassword = ownerPwd,
                            AllowPrinting = options.AllowPrinting,
                            AllowModification = options.AllowModifying,
                            AllowContentExtraction = options.AllowCopying,
                            AllowAnnotation = options.AllowAnnotating,
                            AllowFillingForms = options.AllowFormFilling,
                            AllowAssembly = options.AllowAssembly,
                            EncryptMetadata = options.EncryptMetadata
                        });
                        break;

                    case PdfEncryptionLevel.Aes128Bit:
                        op = op.Encrypt(new DocumentOperation.Encryption128Bit
                        {
                            UserPassword = userPwd,
                            OwnerPassword = ownerPwd,
                            AllowPrinting = options.AllowPrinting,
                            AllowModification = options.AllowModifying,
                            AllowContentExtraction = options.AllowCopying,
                            AllowAnnotation = options.AllowAnnotating,
                            AllowFillingForms = options.AllowFormFilling,
                            AllowAssembly = options.AllowAssembly,
                            EncryptMetadata = options.EncryptMetadata
                        });
                        break;

                    case PdfEncryptionLevel.Rc440Bit:
                        op = op.Encrypt(new DocumentOperation.Encryption40Bit
                        {
                            UserPassword = userPwd,
                            OwnerPassword = ownerPwd,
                            AllowPrinting = options.AllowPrinting,
                            AllowModification = options.AllowModifying,
                            AllowContentExtraction = options.AllowCopying,
                            AllowAnnotation = options.AllowAnnotating
                        });
                        break;
                }

                progress?.Report(85.0);
                op.Save(outPath);
                progress?.Report(100.0);

                long outBytes = File.Exists(outPath) ? new FileInfo(outPath).Length : 0;
                string levelText = options.EncryptionLevel switch
                {
                    PdfEncryptionLevel.Aes256Bit => "256-bit AES (QuestPDF Native)",
                    PdfEncryptionLevel.Aes128Bit => "128-bit AES",
                    _ => "40-bit Legacy"
                };

                return new ToolExecutionResult
                {
                    Success = true,
                    OutputFilePath = outPath,
                    OutputFiles = new List<string> { outPath },
                    OriginalSizeBytes = origBytes,
                    OutputSizeBytes = outBytes,
                    Message = generatedOwnerPassword != null
                        ? $"Document successfully encrypted with {levelText} and secured with requested permission constraints. Generated owner password: {generatedOwnerPassword}"
                        : $"Document successfully encrypted with {levelText} and secured with requested permission constraints."
                };
            }
            catch (Exception ex)
            {
                return new ToolExecutionResult
                {
                    Success = false,
                    ErrorMessage = $"QuestPDF encryption failed: {ex.Message}"
                };
            }
        }, ct);
    }

    public async Task<ToolExecutionResult> DecryptAsync(UnlockToolOptions options, IProgress<double>? progress = null, CancellationToken ct = default)
    {
        return await Task.Run(() =>
        {
            if (!File.Exists(options.InputFilePath))
                return new ToolExecutionResult { Success = false, ErrorMessage = "Input PDF file does not exist." };

            long origBytes = new FileInfo(options.InputFilePath).Length;
            progress?.Report(20.0);
            ct.ThrowIfCancellationRequested();

            string outPath = options.OutputFilePath;
            if (string.IsNullOrWhiteSpace(outPath))
            {
                string dir = Path.GetDirectoryName(options.InputFilePath) ?? "";
                string name = Path.GetFileNameWithoutExtension(options.InputFilePath);
                outPath = Path.Combine(dir, $"{name}_Unlocked.pdf");
            }

            EnsureDirectoryExists(outPath);

            try
            {
                DocumentOperation
                    .LoadFile(options.InputFilePath, options.Password ?? string.Empty)
                    .Decrypt()
                    .RemoveRestrictions()
                    .Save(outPath);

                progress?.Report(100.0);
                long outBytes = File.Exists(outPath) ? new FileInfo(outPath).Length : 0;
                return new ToolExecutionResult
                {
                    Success = true,
                    OutputFilePath = outPath,
                    OutputFiles = new List<string> { outPath },
                    OriginalSizeBytes = origBytes,
                    OutputSizeBytes = outBytes,
                    Message = "Security restrictions and passwords successfully removed via QuestPDF Native Engine. Output PDF is fully unlocked."
                };
            }
            catch (Exception ex)
            {
                return new ToolExecutionResult
                {
                    Success = false,
                    ErrorMessage = $"Failed to unlock PDF via QuestPDF: {ex.Message}"
                };
            }
        }, ct);
    }

    public async Task<ToolExecutionResult> LinearizeAsync(string inputPath, string outputPath, IProgress<double>? progress = null, CancellationToken ct = default)
    {
        return await Task.Run(() =>
        {
            if (!File.Exists(inputPath))
                return new ToolExecutionResult { Success = false, ErrorMessage = "Input PDF file does not exist." };

            long origBytes = new FileInfo(inputPath).Length;
            progress?.Report(20.0);
            ct.ThrowIfCancellationRequested();

            string outPath = outputPath;
            if (string.IsNullOrWhiteSpace(outPath))
            {
                string dir = Path.GetDirectoryName(inputPath) ?? "";
                string name = Path.GetFileNameWithoutExtension(inputPath);
                outPath = Path.Combine(dir, $"{name}_Linearized.pdf");
            }

            EnsureDirectoryExists(outPath);

            try
            {
                bool inPlace = string.Equals(Path.GetFullPath(inputPath), Path.GetFullPath(outPath), StringComparison.OrdinalIgnoreCase);
                string saveTarget = outPath;
                string? tempFile = null;

                if (inPlace)
                {
                    string dir = Path.GetDirectoryName(outPath) ?? Path.GetTempPath();
                    tempFile = Path.Combine(dir, $"quest_lin_temp_{Guid.NewGuid():N}.pdf");
                    saveTarget = tempFile;
                }

                DocumentOperation.LoadFile(inputPath).Linearize().Save(saveTarget);

                if (inPlace && tempFile != null && File.Exists(tempFile))
                {
                    File.Move(tempFile, outPath, overwrite: true);
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
                    Message = $"Document successfully linearized for Fast Web View ({FormatFileSize(outBytes)}). Viewers can stream and display Page 1 immediately."
                };
            }
            catch (Exception ex)
            {
                return new ToolExecutionResult
                {
                    Success = false,
                    ErrorMessage = $"QuestPDF linearization failed: {ex.Message}"
                };
            }
        }, ct);
    }

    public async Task<ToolExecutionResult> ApplyLayerAsync(string inputPath, string layerPdfPath, bool isOverlay, string targetPages, string? outputPath = null, IProgress<double>? progress = null, CancellationToken ct = default)
    {
        return await Task.Run(() =>
        {
            if (!File.Exists(inputPath))
                return new ToolExecutionResult { Success = false, ErrorMessage = "Input PDF file does not exist." };
            if (!File.Exists(layerPdfPath))
                return new ToolExecutionResult { Success = false, ErrorMessage = "Layer/Letterhead PDF file does not exist." };

            long origBytes = new FileInfo(inputPath).Length;
            progress?.Report(20.0);
            ct.ThrowIfCancellationRequested();

            string outPath = outputPath ?? string.Empty;
            if (string.IsNullOrWhiteSpace(outPath))
            {
                string dir = Path.GetDirectoryName(inputPath) ?? "";
                string name = Path.GetFileNameWithoutExtension(inputPath);
                string suffix = isOverlay ? "Overlay" : "Underlay";
                outPath = Path.Combine(dir, $"{name}_{suffix}.pdf");
            }

            EnsureDirectoryExists(outPath);

            try
            {
                var layerConfig = new DocumentOperation.LayerConfiguration
                {
                    FilePath = layerPdfPath,
                    TargetPages = string.IsNullOrWhiteSpace(targetPages) ? "1-z" : targetPages,
                    SourcePages = "1",
                    RepeatSourcePages = "1"
                };

                var op = DocumentOperation.LoadFile(inputPath);
                op = isOverlay ? op.OverlayFile(layerConfig) : op.UnderlayFile(layerConfig);

                progress?.Report(80.0);
                op.Save(outPath);
                progress?.Report(100.0);

                long outBytes = File.Exists(outPath) ? new FileInfo(outPath).Length : 0;
                string layerType = isOverlay ? "Overlay" : "Letterhead / Underlay";
                return new ToolExecutionResult
                {
                    Success = true,
                    OutputFilePath = outPath,
                    OutputFiles = new List<string> { outPath },
                    OriginalSizeBytes = origBytes,
                    OutputSizeBytes = outBytes,
                    Message = $"Successfully applied {layerType} PDF to '{Path.GetFileName(outPath)}' via QuestPDF Native Engine."
                };
            }
            catch (Exception ex)
            {
                return new ToolExecutionResult
                {
                    Success = false,
                    ErrorMessage = $"Failed to apply PDF layer: {ex.Message}"
                };
            }
        }, ct);
    }

    public async Task<ToolExecutionResult> AddAttachmentAsync(
        string inputPath,
        string attachmentFilePath,
        string? description = null,
        string? mimeType = null,
        DocumentOperation.DocumentAttachmentRelationship relationship = DocumentOperation.DocumentAttachmentRelationship.Unspecified,
        string? outputPath = null,
        IProgress<double>? progress = null,
        CancellationToken ct = default)
    {
        return await Task.Run(() =>
        {
            if (!File.Exists(inputPath))
                return new ToolExecutionResult { Success = false, ErrorMessage = "Input PDF file does not exist." };
            if (!File.Exists(attachmentFilePath))
                return new ToolExecutionResult { Success = false, ErrorMessage = "Attachment file does not exist." };

            long origBytes = new FileInfo(inputPath).Length;
            progress?.Report(20.0);
            ct.ThrowIfCancellationRequested();

            string outPath = outputPath ?? string.Empty;
            if (string.IsNullOrWhiteSpace(outPath))
            {
                string dir = Path.GetDirectoryName(inputPath) ?? "";
                string name = Path.GetFileNameWithoutExtension(inputPath);
                outPath = Path.Combine(dir, $"{name}_Attached.pdf");
            }

            EnsureDirectoryExists(outPath);

            try
            {
                var attach = new DocumentOperation.DocumentAttachment
                {
                    FilePath = attachmentFilePath,
                    AttachmentName = Path.GetFileName(attachmentFilePath),
                    Description = description ?? "Embedded supporting document",
                    MimeType = mimeType ?? "application/octet-stream",
                    Relationship = relationship,
                    CreationDate = DateTime.UtcNow,
                    ModificationDate = DateTime.UtcNow,
                    Replace = true
                };

                DocumentOperation
                    .LoadFile(inputPath)
                    .AddAttachment(attach)
                    .Save(outPath);

                progress?.Report(100.0);
                long outBytes = File.Exists(outPath) ? new FileInfo(outPath).Length : 0;
                return new ToolExecutionResult
                {
                    Success = true,
                    OutputFilePath = outPath,
                    OutputFiles = new List<string> { outPath },
                    OriginalSizeBytes = origBytes,
                    OutputSizeBytes = outBytes,
                    Message = $"Successfully embedded attachment '{Path.GetFileName(attachmentFilePath)}' ({relationship}) via QuestPDF Native Engine."
                };
            }
            catch (Exception ex)
            {
                return new ToolExecutionResult
                {
                    Success = false,
                    ErrorMessage = $"Failed to embed attachment: {ex.Message}"
                };
            }
        }, ct);
    }

    public async Task<ToolExecutionResult> ExtendMetadataAsync(
        string inputPath,
        string customXmpXml,
        string? outputPath = null,
        IProgress<double>? progress = null,
        CancellationToken ct = default)
    {
        return await Task.Run(() =>
        {
            if (!File.Exists(inputPath))
                return new ToolExecutionResult { Success = false, ErrorMessage = "Input PDF file does not exist." };
            if (string.IsNullOrWhiteSpace(customXmpXml))
                return new ToolExecutionResult { Success = false, ErrorMessage = "No custom XMP metadata XML provided." };

            long origBytes = new FileInfo(inputPath).Length;
            progress?.Report(20.0);
            ct.ThrowIfCancellationRequested();

            string outPath = outputPath ?? string.Empty;
            if (string.IsNullOrWhiteSpace(outPath))
            {
                string dir = Path.GetDirectoryName(inputPath) ?? "";
                string name = Path.GetFileNameWithoutExtension(inputPath);
                outPath = Path.Combine(dir, $"{name}_ExtendedXmp.pdf");
            }

            EnsureDirectoryExists(outPath);

            try
            {
                DocumentOperation
                    .LoadFile(inputPath)
                    .ExtendMetadata(customXmpXml)
                    .Save(outPath);

                progress?.Report(100.0);
                long outBytes = File.Exists(outPath) ? new FileInfo(outPath).Length : 0;
                return new ToolExecutionResult
                {
                    Success = true,
                    OutputFilePath = outPath,
                    OutputFiles = new List<string> { outPath },
                    OriginalSizeBytes = origBytes,
                    OutputSizeBytes = outBytes,
                    Message = "Successfully extended document XMP metadata via QuestPDF Native Engine."
                };
            }
            catch (Exception ex)
            {
                return new ToolExecutionResult
                {
                    Success = false,
                    ErrorMessage = $"Failed to extend metadata: {ex.Message}"
                };
            }
        }, ct);
    }

    private static void EnsureDirectoryExists(string filePath)
    {
        string? dir = Path.GetDirectoryName(filePath);
        if (!string.IsNullOrEmpty(dir) && !Directory.Exists(dir))
        {
            Directory.CreateDirectory(dir);
        }
    }

    private static string GenerateRandomPassword(int length = 20)
    {
        const string chars = "ABCDEFGHJKLMNPQRSTUVWXYZabcdefghijkmnopqrstuvwxyz23456789!@#$%^&*";
        var bytes = RandomNumberGenerator.GetBytes(length);
        var sb = new StringBuilder(length);
        foreach (var b in bytes)
        {
            sb.Append(chars[b % chars.Length]);
        }
        return sb.ToString();
    }

    private static string FormatFileSize(long bytes)
    {
        if (bytes < 1024) return $"{bytes} B";
        if (bytes < 1024 * 1024) return $"{bytes / 1024.0:F1} KB";
        return $"{bytes / (1024.0 * 1024.0):F2} MB";
    }
}
