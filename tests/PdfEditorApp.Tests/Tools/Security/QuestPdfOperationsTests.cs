using PdfEditorApp.Services.Tools.Core;
using PdfEditorApp.Services.Tools.Organize;
using PdfEditorApp.Services.Tools.Security;
using PdfEditorApp.Services.Tools.Conversion;
using PdfEditorApp.Services.Tools.Intelligence;
using PdfEditorApp.ViewModels.Tools.Core;
using PdfEditorApp.ViewModels.Tools.Organize;
using PdfEditorApp.ViewModels.Tools.Security;
using PdfEditorApp.ViewModels.Tools.Conversion;
using PdfEditorApp.ViewModels.Tools.Intelligence;
using PdfEditorApp.Tests.Tools.Core;
using System;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;
using PdfEditorApp.Models;
using PdfEditorApp.Services.Tools;
using UglyToad.PdfPig;
using Xunit;

namespace PdfEditorApp.Tests.Tools.Security;

public class QuestPdfOperationsTests
{
    private readonly IQuestPdfOperationsEngine _questEngine = new QuestPdfOperationsEngine();

    [Fact]
    public async Task MergeAsync_CombinesMultiplePdfs_Successfully()
    {
        string tempDir = ToolTestFixture.CreateIsolatedDirectory("QuestMerge");
        try
        {
            string file1 = ToolTestFixture.CreateSamplePdf("QuestMerge1", 2, tempDir);
            string file2 = ToolTestFixture.CreateSamplePdf("QuestMerge2", 3, tempDir);

            var options = new MergeToolOptions
            {
                InputFiles = new List<string> { file1, file2 },
                Engine = PdfProcessingEngine.QuestPdfNative
            };

            var result = await _questEngine.MergeAsync(options);

            Assert.True(result.Success);
            Assert.NotNull(result.OutputFilePath);
            string outFile = result.OutputFilePath;
            Assert.True(File.Exists(outFile));

            using var doc = PdfDocument.Open(outFile);
            Assert.Equal(5, doc.NumberOfPages);
        }
        finally
        {
            if (Directory.Exists(tempDir)) Directory.Delete(tempDir, recursive: true);
        }
    }

    [Fact]
    public async Task MergeAsync_WithPerFilePageRanges_ExtractsAndMergesCorrectPages()
    {
        string tempDir = ToolTestFixture.CreateIsolatedDirectory("QuestRange");
        try
        {
            string file1 = ToolTestFixture.CreateSamplePdf("QuestRange1", 3, tempDir);
            string file2 = ToolTestFixture.CreateSamplePdf("QuestRange2", 2, tempDir);

            var options = new MergeToolOptions
            {
                InputFiles = new List<string> { file1, file2 },
                Engine = PdfProcessingEngine.QuestPdfNative,
                FilePageRanges = new Dictionary<string, string>
                {
                    [file1] = "1,3",  // Take pages 1 and 3 from doc 1
                    [file2] = "2"     // Take page 2 from doc 2
                }
            };

            var result = await _questEngine.MergeAsync(options);

            Assert.True(result.Success);
            Assert.NotNull(result.OutputFilePath);
            string outFile = result.OutputFilePath;
            Assert.True(File.Exists(outFile));

            using var doc = PdfDocument.Open(outFile);
            Assert.Equal(3, doc.NumberOfPages);
        }
        finally
        {
            if (Directory.Exists(tempDir)) Directory.Delete(tempDir, recursive: true);
        }
    }

    [Fact]
    public async Task EncryptAndDecrypt_Aes256Bit_ProtectsAndUnlocksDocument()
    {
        string sample = ToolTestFixture.CreateSamplePdf("QuestCrypto", 2);
        string? protectedFile = null;
        string? unlockedFile = null;
        const string password = "TestSecurePassword2026!";

        try
        {
            var encryptOptions = new SecurityToolOptions
            {
                InputFilePath = sample,
                UserPassword = password,
                EncryptionLevel = PdfEncryptionLevel.Aes256Bit,
                Engine = PdfProcessingEngine.QuestPdfNative,
                AllowPrinting = true,
                AllowModifying = false
            };

            var encryptResult = await _questEngine.EncryptAsync(encryptOptions);
            Assert.True(encryptResult.Success);
            protectedFile = encryptResult.OutputFilePath;
            Assert.True(File.Exists(protectedFile));

            // Verify document requires password to open
            Assert.ThrowsAny<Exception>(() =>
            {
                using var _ = PdfDocument.Open(protectedFile);
            });

            // Decrypt and remove restrictions using QuestPDF
            var unlockOptions = new UnlockToolOptions
            {
                InputFilePath = protectedFile,
                Password = password,
                Engine = PdfProcessingEngine.QuestPdfNative
            };

            var unlockResult = await _questEngine.DecryptAsync(unlockOptions);
            Assert.True(unlockResult.Success);
            unlockedFile = unlockResult.OutputFilePath;
            Assert.True(File.Exists(unlockedFile));

            // Verify unlocked file opens cleanly without password
            using var unlockedDoc = PdfDocument.Open(unlockedFile);
            Assert.Equal(2, unlockedDoc.NumberOfPages);
        }
        finally
        {
            if (File.Exists(sample)) File.Delete(sample);
            if (protectedFile != null && File.Exists(protectedFile)) File.Delete(protectedFile);
            if (unlockedFile != null && File.Exists(unlockedFile)) File.Delete(unlockedFile);
        }
    }

    [Fact]
    public async Task LinearizeAsync_OptimizesDocumentForFastWebView()
    {
        string sample = ToolTestFixture.CreateSamplePdf("QuestLinearize", 2);
        string? linFile = null;

        try
        {
            linFile = Path.Combine(Path.GetTempPath(), $"Linearized_{Guid.NewGuid():N}.pdf");
            var result = await _questEngine.LinearizeAsync(sample, linFile);

            Assert.True(result.Success);
            Assert.True(File.Exists(linFile));
            Assert.True(new FileInfo(linFile).Length > 0);

            // Document must remain fully valid and openable
            using var doc = PdfDocument.Open(linFile);
            Assert.Equal(2, doc.NumberOfPages);
        }
        finally
        {
            if (File.Exists(sample)) File.Delete(sample);
            if (linFile != null && File.Exists(linFile)) File.Delete(linFile);
        }
    }

    [Fact]
    public async Task ApplyLayerAsync_LayersOverlayPdf_Successfully()
    {
        string basePdf = ToolTestFixture.CreateSamplePdf("QuestBase", 2);
        string overlayPdf = ToolTestFixture.CreateSamplePdf("QuestOverlay", 1);
        string? layeredFile = null;

        try
        {
            var result = await _questEngine.ApplyLayerAsync(
                inputPath: basePdf,
                layerPdfPath: overlayPdf,
                isOverlay: true,
                targetPages: "1-z");

            Assert.True(result.Success);
            layeredFile = result.OutputFilePath;
            Assert.NotNull(layeredFile);
            Assert.True(File.Exists(layeredFile));

            using var doc = PdfDocument.Open(layeredFile);
            Assert.Equal(2, doc.NumberOfPages);
        }
        finally
        {
            if (File.Exists(basePdf)) File.Delete(basePdf);
            if (File.Exists(overlayPdf)) File.Delete(overlayPdf);
            if (layeredFile != null && File.Exists(layeredFile)) File.Delete(layeredFile);
        }
    }

    [Fact]
    public async Task AddAttachmentAsync_EmbedsFileAttachment_Successfully()
    {
        string basePdf = ToolTestFixture.CreateSamplePdf("QuestAttachmentDoc", 1);
        string tempAttachment = Path.Combine(Path.GetTempPath(), $"invoice_{Guid.NewGuid():N}.xml");
        File.WriteAllText(tempAttachment, "<Invoice><Id>12345</Id><Total>99.99</Total></Invoice>");
        string? attachedFile = null;

        try
        {
            var result = await _questEngine.AddAttachmentAsync(
                inputPath: basePdf,
                attachmentFilePath: tempAttachment,
                description: "ZUGFeRD Electronic Invoice XML",
                mimeType: "text/xml");

            Assert.True(result.Success);
            attachedFile = result.OutputFilePath;
            Assert.NotNull(attachedFile);
            Assert.True(File.Exists(attachedFile));

            using var doc = PdfDocument.Open(attachedFile);
            Assert.Equal(1, doc.NumberOfPages);
        }
        finally
        {
            if (File.Exists(basePdf)) File.Delete(basePdf);
            if (File.Exists(tempAttachment)) File.Delete(tempAttachment);
            if (attachedFile != null && File.Exists(attachedFile)) File.Delete(attachedFile);
        }
    }

    [Fact]
    public async Task ExtendMetadataAsync_ExtendsXmpMetadata_Successfully()
    {
        string basePdf = ToolTestFixture.CreateSamplePdf("QuestMetadataDoc", 1);
        string customXmp = "<x:xmpmeta xmlns:x='adobe:ns:meta/'><rdf:RDF xmlns:rdf='http://www.w3.org/1999/02/22-rdf-syntax-ns#'><rdf:Description rdf:about='' xmlns:pdfaExtension='http://www.aiim.org/pdfa/ns/extension/' /></rdf:RDF></x:xmpmeta>";
        string? metaFile = null;

        try
        {
            var result = await _questEngine.ExtendMetadataAsync(basePdf, customXmp);

            Assert.True(result.Success);
            metaFile = result.OutputFilePath;
            Assert.NotNull(metaFile);
            Assert.True(File.Exists(metaFile));

            using var doc = PdfDocument.Open(metaFile);
            Assert.Equal(1, doc.NumberOfPages);
        }
        finally
        {
            if (File.Exists(basePdf)) File.Delete(basePdf);
            if (metaFile != null && File.Exists(metaFile)) File.Delete(metaFile);
        }
    }
}
