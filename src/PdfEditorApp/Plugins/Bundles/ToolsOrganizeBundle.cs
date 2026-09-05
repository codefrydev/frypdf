using System;
using System.Collections.Generic;
using Microsoft.Extensions.DependencyInjection;
using PdfEditorApp.Core.Plugins;
using PdfEditorApp.Core.Plugins.Descriptors;
using PdfEditorApp.Core.Plugins.Profiles;
using PdfEditorApp.Models;
using PdfEditorApp.ViewModels.Tools.Organize;

namespace PdfEditorApp.Plugins.Bundles;

public class ToolsOrganizeBundle : IFryPluginBundle
{
    public string Id => "FryPdf.Bundle.Tools.Organize";
    public string Name => "PDF Organize & Page Tools Bundle";
    public string Description => "Essential page manipulation and document organizing tools (Merge, Split, Rotate, Organize, Crop, Page Numbers).";

    public IReadOnlyList<IFryPlugin> Plugins => new IFryPlugin[]
    {
        new MergePdfToolPlugin(),
        new SplitPdfToolPlugin(),
        new RotatePdfToolPlugin(),
        new OrganizePdfToolPlugin(),
        new CropPdfToolPlugin(),
        new PageNumbersToolPlugin()
    };
}

public class MergePdfToolPlugin : ToolPluginBase
{
    public override string Id => "frypdf.tool.merge";
    public override string Name => "Merge PDF";

    protected override PdfToolDescriptor CreateDescriptor() => new()
    {
        Id = Id,
        LegacyId = (int)PdfToolId.MergePdf,
        Name = Name,
        Description = "Combine PDFs in the order you want with the easiest PDF merger available.",
        Category = "OrganizeAndPage",
        IconKind = "CallMerge",
        IconColorHex = "#EA580C",
        BackgroundAccentHex = "#FFF7ED",
        SupportsMultiFile = true,
        AcceptedFileExtensions = ".pdf",
        CreateViewModel = sp => ActivatorUtilities.CreateInstance<MergePdfToolViewModel>(sp)
    };
}

public class SplitPdfToolPlugin : ToolPluginBase
{
    public override string Id => "frypdf.tool.split";
    public override string Name => "Split PDF";

    protected override PdfToolDescriptor CreateDescriptor() => new()
    {
        Id = Id,
        LegacyId = (int)PdfToolId.SplitPdf,
        Name = Name,
        Description = "Separate one page or a whole set for easy conversion into independent PDF files.",
        Category = "OrganizeAndPage",
        IconKind = "CallSplit",
        IconColorHex = "#DC2626",
        BackgroundAccentHex = "#FEF2F2",
        SupportsMultiFile = false,
        AcceptedFileExtensions = ".pdf",
        CreateViewModel = sp => ActivatorUtilities.CreateInstance<SplitPdfToolViewModel>(sp)
    };
}

public class RotatePdfToolPlugin : ToolPluginBase
{
    public override string Id => "frypdf.tool.rotate";
    public override string Name => "Rotate PDF";

    protected override PdfToolDescriptor CreateDescriptor() => new()
    {
        Id = Id,
        LegacyId = (int)PdfToolId.RotatePdf,
        Name = Name,
        Description = "Rotate your PDF pages however you'd like. You can even rotate multiple PDFs at once!",
        Category = "OrganizeAndPage",
        IconKind = "RotateRight",
        IconColorHex = "#0284C7",
        BackgroundAccentHex = "#F0F9FF",
        SupportsMultiFile = true,
        AcceptedFileExtensions = ".pdf",
        CreateViewModel = sp => ActivatorUtilities.CreateInstance<RotatePdfToolViewModel>(sp)
    };
}

public class OrganizePdfToolPlugin : ToolPluginBase
{
    public override string Id => "frypdf.tool.organize";
    public override string Name => "Organize PDF";

    protected override PdfToolDescriptor CreateDescriptor() => new()
    {
        Id = Id,
        LegacyId = (int)PdfToolId.OrganizePdf,
        Name = Name,
        Description = "Sort, add and delete PDF pages. Drag and drop the page thumbnails to reorganize them as you wish.",
        Category = "OrganizeAndPage",
        IconKind = "ViewGridOutline",
        IconColorHex = "#7C3AED",
        BackgroundAccentHex = "#F5F3FF",
        SupportsMultiFile = false,
        AcceptedFileExtensions = ".pdf",
        CreateViewModel = sp => ActivatorUtilities.CreateInstance<OrganizePdfToolViewModel>(sp)
    };
}

public class CropPdfToolPlugin : ToolPluginBase
{
    public override string Id => "frypdf.tool.crop";
    public override string Name => "Crop PDF";

    protected override PdfToolDescriptor CreateDescriptor() => new()
    {
        Id = Id,
        LegacyId = (int)PdfToolId.CropPdf,
        Name = Name,
        Description = "Trim margins, remove unnecessary whitespace, and adjust canvas bounding boxes.",
        Category = "OrganizeAndPage",
        IconKind = "Crop",
        IconColorHex = "#059669",
        BackgroundAccentHex = "#ECFDF5",
        SupportsMultiFile = false,
        AcceptedFileExtensions = ".pdf",
        CreateViewModel = sp => ActivatorUtilities.CreateInstance<CropPdfToolViewModel>(sp)
    };
}

public class PageNumbersToolPlugin : ToolPluginBase
{
    public override string Id => "frypdf.tool.pagenumbers";
    public override string Name => "Page Numbers";

    protected override PdfToolDescriptor CreateDescriptor() => new()
    {
        Id = Id,
        LegacyId = (int)PdfToolId.PageNumbers,
        Name = Name,
        Description = "Add page numbers into PDFs with ease. Choose position, dimensions, typography and formatting.",
        Category = "OrganizeAndPage",
        IconKind = "Numeric",
        IconColorHex = "#D97706",
        BackgroundAccentHex = "#FFFBEB",
        SupportsMultiFile = true,
        AcceptedFileExtensions = ".pdf",
        CreateViewModel = sp => ActivatorUtilities.CreateInstance<PageNumbersToolViewModel>(sp)
    };
}
