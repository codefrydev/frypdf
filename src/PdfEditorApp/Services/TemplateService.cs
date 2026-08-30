using System;
using System.Collections.Generic;
using System.Linq;
using PdfEditorApp.Models;
using PdfEditorApp.Templates;

namespace PdfEditorApp.Services;

public class TemplateService : ITemplateService
{
    private readonly Dictionary<string, ITemplateDefinition> _templates = new(StringComparer.OrdinalIgnoreCase);
    private readonly BlankDocumentTemplate _blankTemplate = new();

    public TemplateService()
    {
        RegisterTemplate(new AnnualReportTemplate());
        RegisterTemplate(new InvoiceTemplate());
        RegisterTemplate(new ResumeTemplate());
        RegisterTemplate(new AcademicPaperTemplate());
        RegisterTemplate(new CertificateTemplate());
        RegisterTemplate(new CertificateNavyGoldTemplate());
        RegisterTemplate(new DiplomaAcademicTemplate());
        RegisterTemplate(new WeddingInvitationTraditionalTemplate());
        RegisterTemplate(new WeddingInvitationRoyalFloralTemplate());
        RegisterTemplate(new GalaInvitationTemplate());
        RegisterTemplate(_blankTemplate);
    }

    public void RegisterTemplate(ITemplateDefinition template)
    {
        _templates[template.Id] = template;
    }

    public IReadOnlyList<ITemplateDefinition> GetAllTemplates()
    {
        return _templates.Values.ToList();
    }

    public PdfDocumentModel CreateTemplate(string templateId)
    {
        if (_templates.TryGetValue(templateId, out var template))
        {
            return template.Create();
        }

        return _blankTemplate.Create();
    }

    public PdfDocumentModel CreateAnnualReportTemplate() => CreateTemplate("annualreport");

    public PdfDocumentModel CreateInvoiceTemplate() => CreateTemplate("invoice");

    public PdfDocumentModel CreateResumeTemplate() => CreateTemplate("resume");

    public PdfDocumentModel CreateAcademicPaperTemplate() => CreateTemplate("academic");

    public PdfDocumentModel CreateCertificateTemplate() => CreateTemplate("certificate");

    public PdfDocumentModel CreateCertificateNavyGoldTemplate() => CreateTemplate("certificatenavygold");

    public PdfDocumentModel CreateDiplomaAcademicTemplate() => CreateTemplate("diploma");

    public PdfDocumentModel CreateWeddingInvitationTraditionalTemplate() => CreateTemplate("weddingtraditional");

    public PdfDocumentModel CreateWeddingInvitationRoyalFloralTemplate() => CreateTemplate("weddingroyalfloral");

    public PdfDocumentModel CreateGalaInvitationTemplate() => CreateTemplate("galainvitation");

    public PdfDocumentModel CreateBlankDocument(PageFormat format = PageFormat.A4, PageOrientation orientation = PageOrientation.Portrait)
    {
        return _blankTemplate.Create(format, orientation);
    }
}
