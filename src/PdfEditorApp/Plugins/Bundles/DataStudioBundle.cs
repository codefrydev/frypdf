using System;
using System.Collections.Generic;
using Microsoft.Extensions.DependencyInjection;
using PdfEditorApp.Core.Plugins;
using PdfEditorApp.Core.Plugins.Descriptors;
using PdfEditorApp.Core.Plugins.Profiles;
using PdfEditorApp.Models;
using PdfEditorApp.ViewModels.BatchGeneration;
using PdfEditorApp.ViewModels.DataStudio;

namespace PdfEditorApp.Plugins.Bundles;

public class DataStudioBundle : IFryPluginBundle
{
    public string Id => "FryPdf.Bundle.DataStudio";
    public string Name => "Data Studio & Batch Generation Bundle";
    public string Description => "Tabular dataset ingestion (Excel, CSV, REST APIs), dynamic data binding, and high-throughput batch PDF generation.";

    public IReadOnlyList<IFryPlugin> Plugins => new IFryPlugin[]
    {
        new DataStudioToolPlugin(),
        new BatchGenerationToolPlugin()
    };
}

public class DataStudioToolPlugin : ToolPluginBase
{
    public override string Id => "frypdf.tool.datastudio";
    public override string Name => "Data Studio";

    protected override PdfToolDescriptor CreateDescriptor() => new()
    {
        Id = Id,
        LegacyId = (int)PdfToolId.WorkflowBuilder,
        Name = Name,
        Description = "Connect external datasets (Excel, CSV, REST API) and bind dynamic fields to PDF tables, charts, and text templates.",
        Category = "AiAndAutomation",
        IconKind = "DatabaseOutline",
        IconColorHex = "#0284C7",
        BackgroundAccentHex = "#F0F9FF",
        SupportsMultiFile = false,
        AcceptedFileExtensions = ".xlsx,.csv,.tsv,.json",
        CreateViewModel = sp => ActivatorUtilities.CreateInstance<DataStudioViewModel>(sp)
    };
}

public class BatchGenerationToolPlugin : ToolPluginBase
{
    public override string Id => "frypdf.tool.batchgeneration";
    public override string Name => "Batch Mail Merge";

    protected override PdfToolDescriptor CreateDescriptor() => new()
    {
        Id = Id,
        LegacyId = (int)PdfToolId.BatchMailMerge,
        Name = Name,
        Description = "Generate hundreds of personalized PDFs (payslips, certificates, invoices, badges) in one click using Excel, CSV, or REST APIs.",
        Category = "AiAndAutomation",
        IconKind = "DatabaseArrowDownOutline",
        IconColorHex = "#0F6CBD",
        BackgroundAccentHex = "#EFF6FF",
        SupportsMultiFile = false,
        AcceptedFileExtensions = ".xlsx,.csv,.tsv,.json",
        CreateViewModel = sp => ActivatorUtilities.CreateInstance<BatchGenerationViewModel>(sp)
    };
}
