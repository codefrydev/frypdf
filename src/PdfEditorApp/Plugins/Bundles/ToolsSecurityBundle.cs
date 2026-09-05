using System;
using System.Collections.Generic;
using Microsoft.Extensions.DependencyInjection;
using PdfEditorApp.Core.Plugins;
using PdfEditorApp.Core.Plugins.Descriptors;
using PdfEditorApp.Core.Plugins.Profiles;
using PdfEditorApp.Models;
using PdfEditorApp.ViewModels.Tools.Security;

namespace PdfEditorApp.Plugins.Bundles;

public class ToolsSecurityBundle : IFryPluginBundle
{
    public string Id => "FryPdf.Bundle.Tools.Security";
    public string Name => "PDF Optimization & Security Tools Bundle";
    public string Description => "Document optimization, compression, encryption, digital signatures, redaction and watermarking.";

    public IReadOnlyList<IFryPlugin> Plugins => new IFryPlugin[]
    {
        new CompressPdfToolPlugin(),
        new RepairPdfToolPlugin(),
        new ProtectPdfToolPlugin(),
        new UnlockPdfToolPlugin(),
        new SignPdfToolPlugin(),
        new RedactPdfToolPlugin(),
        new WatermarkToolPlugin()
    };
}

public class CompressPdfToolPlugin : ToolPluginBase
{
    public override string Id => "frypdf.tool.compress";
    public override string Name => "Compress PDF";

    protected override PdfToolDescriptor CreateDescriptor() => new()
    {
        Id = Id,
        LegacyId = (int)PdfToolId.CompressPdf,
        Name = Name,
        Description = "Reduce file size while optimizing for maximal PDF quality.",
        Category = "OptimizeAndSecurity",
        IconKind = "ArrowCollapseAll",
        IconColorHex = "#16A34A",
        BackgroundAccentHex = "#F0FDF4",
        SupportsMultiFile = true,
        AcceptedFileExtensions = ".pdf",
        CreateViewModel = sp => ActivatorUtilities.CreateInstance<CompressPdfToolViewModel>(sp)
    };
}

public class RepairPdfToolPlugin : ToolPluginBase
{
    public override string Id => "frypdf.tool.repair";
    public override string Name => "Repair PDF";

    protected override PdfToolDescriptor CreateDescriptor() => new()
    {
        Id = Id,
        LegacyId = (int)PdfToolId.RepairPdf,
        Name = Name,
        Description = "Repair a damaged PDF and recover data from corrupt files. Fix PDF files with our Repair tool.",
        Category = "OptimizeAndSecurity",
        IconKind = "WrenchOutline",
        IconColorHex = "#D97706",
        BackgroundAccentHex = "#FFFBEB",
        SupportsMultiFile = true,
        AcceptedFileExtensions = ".pdf",
        CreateViewModel = sp => ActivatorUtilities.CreateInstance<RepairPdfToolViewModel>(sp)
    };
}

public class ProtectPdfToolPlugin : ToolPluginBase
{
    public override string Id => "frypdf.tool.protect";
    public override string Name => "Protect PDF";

    protected override PdfToolDescriptor CreateDescriptor() => new()
    {
        Id = Id,
        LegacyId = (int)PdfToolId.ProtectPdf,
        Name = Name,
        Description = "Encrypt and password protect your PDF files. Prevent unauthorized access to sensitive information.",
        Category = "OptimizeAndSecurity",
        IconKind = "LockOutline",
        IconColorHex = "#DC2626",
        BackgroundAccentHex = "#FEF2F2",
        SupportsMultiFile = true,
        AcceptedFileExtensions = ".pdf",
        CreateViewModel = sp => ActivatorUtilities.CreateInstance<ProtectPdfToolViewModel>(sp)
    };
}

public class UnlockPdfToolPlugin : ToolPluginBase
{
    public override string Id => "frypdf.tool.unlock";
    public override string Name => "Unlock PDF";

    protected override PdfToolDescriptor CreateDescriptor() => new()
    {
        Id = Id,
        LegacyId = (int)PdfToolId.UnlockPdf,
        Name = Name,
        Description = "Remove PDF password security, giving you the freedom to use your PDFs as you want.",
        Category = "OptimizeAndSecurity",
        IconKind = "LockOpenOutline",
        IconColorHex = "#16A34A",
        BackgroundAccentHex = "#F0FDF4",
        SupportsMultiFile = true,
        AcceptedFileExtensions = ".pdf",
        CreateViewModel = sp => ActivatorUtilities.CreateInstance<UnlockPdfToolViewModel>(sp)
    };
}

public class SignPdfToolPlugin : ToolPluginBase
{
    public override string Id => "frypdf.tool.sign";
    public override string Name => "Sign PDF";

    protected override PdfToolDescriptor CreateDescriptor() => new()
    {
        Id = Id,
        LegacyId = (int)PdfToolId.SignPdf,
        Name = Name,
        Description = "Sign yourself or request electronic signatures from others. Draw, type, or upload an image of your signature.",
        Category = "OptimizeAndSecurity",
        IconKind = "Draw",
        IconColorHex = "#0284C7",
        BackgroundAccentHex = "#F0F9FF",
        SupportsMultiFile = false,
        AcceptedFileExtensions = ".pdf",
        CreateViewModel = sp => ActivatorUtilities.CreateInstance<SignPdfToolViewModel>(sp)
    };
}

public class RedactPdfToolPlugin : ToolPluginBase
{
    public override string Id => "frypdf.tool.redact";
    public override string Name => "Redact PDF";

    protected override PdfToolDescriptor CreateDescriptor() => new()
    {
        Id = Id,
        LegacyId = (int)PdfToolId.RedactPdf,
        Name = Name,
        Description = "Permanently remove sensitive text, numbers, and images from your PDF documents for compliance.",
        Category = "OptimizeAndSecurity",
        IconKind = "FormatColorFill",
        IconColorHex = "#475569",
        BackgroundAccentHex = "#F8FAFC",
        SupportsMultiFile = false,
        AcceptedFileExtensions = ".pdf",
        CreateViewModel = sp => ActivatorUtilities.CreateInstance<RedactPdfToolViewModel>(sp)
    };
}

public class WatermarkToolPlugin : ToolPluginBase
{
    public override string Id => "frypdf.tool.watermark";
    public override string Name => "Watermark PDF";

    protected override PdfToolDescriptor CreateDescriptor() => new()
    {
        Id = Id,
        LegacyId = (int)PdfToolId.Watermark,
        Name = Name,
        Description = "Stamp an image or text over your PDF in seconds. Choose typography, transparency and position.",
        Category = "OptimizeAndSecurity",
        IconKind = "Watermark",
        IconColorHex = "#0D9488",
        BackgroundAccentHex = "#F0FDFA",
        SupportsMultiFile = true,
        AcceptedFileExtensions = ".pdf",
        CreateViewModel = sp => ActivatorUtilities.CreateInstance<WatermarkToolViewModel>(sp)
    };
}
