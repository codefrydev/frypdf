using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using PdfEditorApp.Core.Plugins;
using PdfEditorApp.Core.Plugins.Profiles;
using PdfEditorApp.Templates.Academic;
using PdfEditorApp.Templates.Career;
using PdfEditorApp.Templates.Certificates;
using PdfEditorApp.Templates.Corporate;
using PdfEditorApp.Templates.Education;
using PdfEditorApp.Templates.Events;
using PdfEditorApp.Templates.Finance;
using PdfEditorApp.Templates.General;
using PdfEditorApp.Templates.Technical;

namespace PdfEditorApp.Plugins.Bundles;

/// <summary>
/// Plugin bundle providing all 28 executive document templates across 8 professional domains.
/// </summary>
public class StandardTemplatesBundle : IFryPluginBundle
{
    public string Id => "FryPdf.Bundle.Templates";
    public string Name => "Executive Templates Bundle";
    public string Description => "28 executive templates: Corporate Annual Reports, Payslips, Invoices, CVs, Academic Papers, Certificates, STEM Worksheets, and Invitations.";

    public IReadOnlyList<IFryPlugin> Plugins => new IFryPlugin[]
    {
        new CorporateTemplatesPlugin(),
        new FinanceTemplatesPlugin(),
        new CareerTemplatesPlugin(),
        new AcademicTemplatesPlugin(),
        new CertificateTemplatesPlugin(),
        new EducationTemplatesPlugin(),
        new PublishingTemplatesPlugin(),
        new TechnicalTemplatesPlugin()
    };
}

public class CorporateTemplatesPlugin : IFryPlugin
{
    public string Id => "frypdf.templates.corporate";
    public string Name => "Corporate Templates Pack";
    public Version Version => new(1, 0, 0);
    public IReadOnlyList<Type> RequiredServices => Array.Empty<Type>();

    public Task ApplyAsync(IFryPluginContext ctx, CancellationToken ct = default)
    {
        ctx.RegisterTemplate(new AnnualReportTemplate());
        ctx.RegisterTemplate(new EmployeePayslipTemplate());
        ctx.RegisterTemplate(new OcrAnalysisReportTemplate());
        return Task.CompletedTask;
    }
}

public class FinanceTemplatesPlugin : IFryPlugin
{
    public string Id => "frypdf.templates.finance";
    public string Name => "Finance & Billing Templates Pack";
    public Version Version => new(1, 0, 0);
    public IReadOnlyList<Type> RequiredServices => Array.Empty<Type>();

    public Task ApplyAsync(IFryPluginContext ctx, CancellationToken ct = default)
    {
        ctx.RegisterTemplate(new InvoiceTemplate());
        ctx.RegisterTemplate(new FinanceResearchPaperTemplate());
        return Task.CompletedTask;
    }
}

public class CareerTemplatesPlugin : IFryPlugin
{
    public string Id => "frypdf.templates.career";
    public string Name => "Career & Resume Templates Pack";
    public Version Version => new(1, 0, 0);
    public IReadOnlyList<Type> RequiredServices => Array.Empty<Type>();

    public Task ApplyAsync(IFryPluginContext ctx, CancellationToken ct = default)
    {
        ctx.RegisterTemplate(new ResumeTemplate());
        ctx.RegisterTemplate(new ResumeModernCleanTemplate());
        ctx.RegisterTemplate(new ResumeCreativeMinimalistTemplate());
        ctx.RegisterTemplate(new ResumeAcademicCvTemplate());
        return Task.CompletedTask;
    }
}

public class AcademicTemplatesPlugin : IFryPlugin
{
    public string Id => "frypdf.templates.academic";
    public string Name => "Academic Research Templates Pack";
    public Version Version => new(1, 0, 0);
    public IReadOnlyList<Type> RequiredServices => Array.Empty<Type>();

    public Task ApplyAsync(IFryPluginContext ctx, CancellationToken ct = default)
    {
        ctx.RegisterTemplate(new AcademicPaperTemplate());
        ctx.RegisterTemplate(new MathResearchPaperTemplate());
        ctx.RegisterTemplate(new PhysicsResearchPaperTemplate());
        ctx.RegisterTemplate(new HistoryResearchPaperTemplate());
        return Task.CompletedTask;
    }
}

public class CertificateTemplatesPlugin : IFryPlugin
{
    public string Id => "frypdf.templates.certificates";
    public string Name => "Certificates & Diplomas Templates Pack";
    public Version Version => new(1, 0, 0);
    public IReadOnlyList<Type> RequiredServices => Array.Empty<Type>();

    public Task ApplyAsync(IFryPluginContext ctx, CancellationToken ct = default)
    {
        ctx.RegisterTemplate(new CertificateTemplate());
        ctx.RegisterTemplate(new CertificateNavyGoldTemplate());
        ctx.RegisterTemplate(new DiplomaAcademicTemplate());
        return Task.CompletedTask;
    }
}

public class EducationTemplatesPlugin : IFryPlugin
{
    public string Id => "frypdf.templates.education";
    public string Name => "STEM & Education Worksheets Templates Pack";
    public Version Version => new(1, 0, 0);
    public IReadOnlyList<Type> RequiredServices => Array.Empty<Type>();

    public Task ApplyAsync(IFryPluginContext ctx, CancellationToken ct = default)
    {
        ctx.RegisterTemplate(new MathBODMASWorksheetTemplate());
        ctx.RegisterTemplate(new FactorizationWorksheetTemplate());
        ctx.RegisterTemplate(new BilingualExamPaperTemplate());
        ctx.RegisterTemplate(new StatesOfMatterDiagramNotesTemplate());
        ctx.RegisterTemplate(new QuadrilateralsGuideTemplate());
        return Task.CompletedTask;
    }
}

public class PublishingTemplatesPlugin : IFryPlugin
{
    public string Id => "frypdf.templates.publishing";
    public string Name => "Invitations & Publishing Templates Pack";
    public Version Version => new(1, 0, 0);
    public IReadOnlyList<Type> RequiredServices => Array.Empty<Type>();

    public Task ApplyAsync(IFryPluginContext ctx, CancellationToken ct = default)
    {
        ctx.RegisterTemplate(new WeddingInvitationTraditionalTemplate());
        ctx.RegisterTemplate(new WeddingInvitationRoyalFloralTemplate());
        ctx.RegisterTemplate(new GalaInvitationTemplate());
        ctx.RegisterTemplate(new CreativeTypographyShowcaseTemplate());
        ctx.RegisterTemplate(new RichTextPublishingShowcaseTemplate());
        return Task.CompletedTask;
    }
}

public class TechnicalTemplatesPlugin : IFryPlugin
{
    public string Id => "frypdf.templates.technical";
    public string Name => "Technical Documentation Templates Pack";
    public Version Version => new(1, 0, 0);
    public IReadOnlyList<Type> RequiredServices => Array.Empty<Type>();

    public Task ApplyAsync(IFryPluginContext ctx, CancellationToken ct = default)
    {
        ctx.RegisterTemplate(new ArduinoCheatSheetTemplate());
        return Task.CompletedTask;
    }
}
