using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using PdfEditorApp.Core.Plugins;
using PdfEditorApp.Core.Plugins.Descriptors;
using PdfEditorApp.Core.Plugins.Profiles;
using PdfEditorApp.Views.Dialogs;

namespace PdfEditorApp.Plugins.Bundles;

/// <summary>
/// Plugin bundle providing all modal dialogs, studio overlays, and floating assistant inspectors.
/// </summary>
public class DialogsBundle : IFryPluginBundle
{
    public string Id => "FryPdf.Bundle.Dialogs";
    public string Name => "Modal Dialogs & Studios Bundle";
    public string Description => "Modular modal dialogs: New Document, About, Shortcuts Help, Signatures, Security, Preflight Diagnostics, Watermark, Math Studio, and Data Studio.";

    public IReadOnlyList<IFryPlugin> Plugins => new IFryPlugin[]
    {
        new CoreAppDialogsPlugin(),
        new SecurityDialogsPlugin(),
        new DocumentUtilityDialogsPlugin(),
        new AdvancedStudioDialogsPlugin()
    };
}

public class CoreAppDialogsPlugin : IFryPlugin
{
    public string Id => "frypdf.dialogs.core";
    public string Name => "Core Application Dialogs";
    public Version Version => new(1, 0, 0);
    public IReadOnlyList<Type> RequiredServices => Array.Empty<Type>();

    public Task ApplyAsync(IFryPluginContext ctx, CancellationToken ct = default)
    {
        ctx.RegisterDialog(new DialogDescriptor
        {
            Id = "frypdf.dialog.about",
            Title = "About FryPDF",
            ViewType = typeof(AboutDialog),
            ViewFactory = sp => new AboutDialog()
        });

        ctx.RegisterDialog(new DialogDescriptor
        {
            Id = "frypdf.dialog.shortcuts",
            Title = "Keyboard Shortcuts",
            ViewType = typeof(ShortcutsHelpDialog),
            ViewFactory = sp => new ShortcutsHelpDialog()
        });

        ctx.RegisterDialog(new DialogDescriptor
        {
            Id = "frypdf.dialog.plugins",
            Title = "Plugins & Extensions Studio",
            ViewType = typeof(PluginsDialog),
            ViewFactory = sp => new PluginsDialog()
        });

        ctx.RegisterDialog(new DialogDescriptor
        {
            Id = "frypdf.dialog.newdocument",
            Title = "Create New Document",
            ViewType = typeof(NewDocumentDialog),
            ViewFactory = sp => new NewDocumentDialog()
        });

        ctx.RegisterDialog(new DialogDescriptor
        {
            Id = "frypdf.dialog.exportsuccess",
            Title = "Export Completed",
            ViewType = typeof(ExportSuccessDialog),
            ViewFactory = sp => new ExportSuccessDialog()
        });

        return Task.CompletedTask;
    }
}

public class SecurityDialogsPlugin : IFryPlugin
{
    public string Id => "frypdf.dialogs.security";
    public string Name => "Document Security & Privacy Dialogs";
    public Version Version => new(1, 0, 0);
    public IReadOnlyList<Type> RequiredServices => Array.Empty<Type>();

    public Task ApplyAsync(IFryPluginContext ctx, CancellationToken ct = default)
    {
        ctx.RegisterDialog(new DialogDescriptor
        {
            Id = "frypdf.dialog.security",
            Title = "Document Security & Permissions",
            ViewType = typeof(DocumentSecurityDialog),
            ViewFactory = sp => new DocumentSecurityDialog()
        });

        ctx.RegisterDialog(new DialogDescriptor
        {
            Id = "frypdf.dialog.searchredact",
            Title = "Search & Redact Sensitive Content",
            ViewType = typeof(SearchRedactDialog),
            ViewFactory = sp => new SearchRedactDialog()
        });

        ctx.RegisterDialog(new DialogDescriptor
        {
            Id = "frypdf.dialog.watermark",
            Title = "Watermark Manager",
            ViewType = typeof(WatermarkManagerDialog),
            ViewFactory = sp => new WatermarkManagerDialog()
        });

        return Task.CompletedTask;
    }
}

public class DocumentUtilityDialogsPlugin : IFryPlugin
{
    public string Id => "frypdf.dialogs.utilities";
    public string Name => "Document Utility Dialogs";
    public Version Version => new(1, 0, 0);
    public IReadOnlyList<Type> RequiredServices => Array.Empty<Type>();

    public Task ApplyAsync(IFryPluginContext ctx, CancellationToken ct = default)
    {
        ctx.RegisterDialog(new DialogDescriptor
        {
            Id = "frypdf.dialog.preflight",
            Title = "Preflight Diagnostics",
            ViewType = typeof(PreflightDiagnosticsDialog),
            ViewFactory = sp => new PreflightDiagnosticsDialog()
        });

        ctx.RegisterDialog(new DialogDescriptor
        {
            Id = "frypdf.dialog.headerfooter",
            Title = "Header & Footer Studio",
            ViewType = typeof(HeaderFooterDialog),
            ViewFactory = sp => new HeaderFooterDialog()
        });

        ctx.RegisterDialog(new DialogDescriptor
        {
            Id = "frypdf.dialog.customstamp",
            Title = "Custom Dynamic Stamps",
            ViewType = typeof(CustomStampDialog),
            ViewFactory = sp => new CustomStampDialog()
        });

        ctx.RegisterDialog(new DialogDescriptor
        {
            Id = "frypdf.dialog.bates",
            Title = "Bates Legal Numbering",
            ViewType = typeof(BatesNumberingDialog),
            ViewFactory = sp => new BatesNumberingDialog()
        });

        ctx.RegisterDialog(new DialogDescriptor
        {
            Id = "frypdf.dialog.splitextract",
            Title = "Split & Extract Pages",
            ViewType = typeof(SplitExtractDialog),
            ViewFactory = sp => new SplitExtractDialog()
        });

        ctx.RegisterDialog(new DialogDescriptor
        {
            Id = "frypdf.dialog.compare",
            Title = "Compare Documents",
            ViewType = typeof(CompareDocumentsDialog),
            ViewFactory = sp => new CompareDocumentsDialog()
        });

        return Task.CompletedTask;
    }
}

public class AdvancedStudioDialogsPlugin : IFryPlugin
{
    public string Id => "frypdf.dialogs.studios";
    public string Name => "Advanced Floating Studio Dialogs";
    public Version Version => new(1, 0, 0);
    public IReadOnlyList<Type> RequiredServices => Array.Empty<Type>();

    public Task ApplyAsync(IFryPluginContext ctx, CancellationToken ct = default)
    {
        ctx.RegisterDialog(new DialogDescriptor
        {
            Id = "frypdf.dialog.signature",
            Title = "Digital Signature Studio",
            ViewType = typeof(SignatureStudioDialog),
            ViewFactory = sp => new SignatureStudioDialog()
        });

        ctx.RegisterDialog(new DialogDescriptor
        {
            Id = "frypdf.dialog.math",
            Title = "LaTeX Math Formula Studio",
            ViewType = typeof(MathEquationStudioDialog),
            ViewFactory = sp => new MathEquationStudioDialog()
        });

        ctx.RegisterDialog(new DialogDescriptor
        {
            Id = "frypdf.dialog.datastudio",
            Title = "Data Studio",
            ViewType = typeof(DataStudioDialog),
            ViewFactory = sp => new DataStudioDialog()
        });

        ctx.RegisterDialog(new DialogDescriptor
        {
            Id = "frypdf.dialog.workflow",
            Title = "PDF Workflow Builder",
            ViewType = typeof(WorkflowBuilderDialog),
            ViewFactory = sp => new WorkflowBuilderDialog()
        });

        ctx.RegisterDialog(new DialogDescriptor
        {
            Id = "frypdf.dialog.batch",
            Title = "Batch Generation Studio",
            ViewType = typeof(BatchGenerationDialog),
            ViewFactory = sp => new BatchGenerationDialog()
        });

        ctx.RegisterDialog(new DialogDescriptor
        {
            Id = "frypdf.dialog.aiassistant",
            Title = "AI Document Assistant",
            ViewType = typeof(AiAssistantDialog),
            ViewFactory = sp => new AiAssistantDialog()
        });

        return Task.CompletedTask;
    }
}
