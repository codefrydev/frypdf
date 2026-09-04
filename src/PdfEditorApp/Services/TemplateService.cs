using System;
using System.Collections.Generic;
using System.Linq;
using PdfEditorApp.Core.Models;
using PdfEditorApp.Models;
using PdfEditorApp.Templates;
using PdfEditorApp.Templates.Academic;
using PdfEditorApp.Templates.Career;
using PdfEditorApp.Templates.Certificates;
using PdfEditorApp.Templates.Corporate;
using PdfEditorApp.Templates.Events;
using PdfEditorApp.Templates.Finance;
using PdfEditorApp.Templates.General;
using PdfEditorApp.Templates.Education;
using PdfEditorApp.Templates.Technical;

namespace PdfEditorApp.Services;

public class TemplateService : ITemplateService
{
    private readonly Dictionary<string, ITemplateDefinition> _templates = new(StringComparer.OrdinalIgnoreCase);
    private readonly BlankDocumentTemplate _blankTemplate = new();

    public TemplateService()
    {
        // Corporate
        RegisterTemplate(new AnnualReportTemplate());
        RegisterTemplate(new EmployeePayslipTemplate());
        RegisterTemplate(new OcrAnalysisReportTemplate());

        // Finance
        RegisterTemplate(new InvoiceTemplate());
        RegisterTemplate(new FinanceResearchPaperTemplate());

        // Career / Resumes
        RegisterTemplate(new ResumeTemplate());
        RegisterTemplate(new ResumeModernCleanTemplate());
        RegisterTemplate(new ResumeCreativeMinimalistTemplate());
        RegisterTemplate(new ResumeAcademicCvTemplate());

        // Academic Research
        RegisterTemplate(new AcademicPaperTemplate());
        RegisterTemplate(new MathResearchPaperTemplate());
        RegisterTemplate(new PhysicsResearchPaperTemplate());
        RegisterTemplate(new HistoryResearchPaperTemplate());

        // Certificates & Diplomas
        RegisterTemplate(new CertificateTemplate());
        RegisterTemplate(new CertificateNavyGoldTemplate());
        RegisterTemplate(new DiplomaAcademicTemplate());

        // Events, Design & Publishing
        RegisterTemplate(new WeddingInvitationTraditionalTemplate());
        RegisterTemplate(new WeddingInvitationRoyalFloralTemplate());
        RegisterTemplate(new GalaInvitationTemplate());
        RegisterTemplate(new CreativeTypographyShowcaseTemplate());
        RegisterTemplate(new RichTextPublishingShowcaseTemplate());

        // Education & Practice Worksheets
        RegisterTemplate(new MathBODMASWorksheetTemplate());
        RegisterTemplate(new FactorizationWorksheetTemplate());
        RegisterTemplate(new BilingualExamPaperTemplate());
        RegisterTemplate(new StatesOfMatterDiagramNotesTemplate());
        RegisterTemplate(new QuadrilateralsGuideTemplate());

        // Technical & Cheat Sheets
        RegisterTemplate(new ArduinoCheatSheetTemplate());

        // General
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
    public PdfDocumentModel CreateEmployeePayslipTemplate() => CreateTemplate("payslip");

    public PdfDocumentModel CreateInvoiceTemplate() => CreateTemplate("invoice");

    public PdfDocumentModel CreateResumeTemplate() => CreateTemplate("resume");

    public PdfDocumentModel CreateResumeModernCleanTemplate() => CreateTemplate("resumemodern");

    public PdfDocumentModel CreateResumeCreativeMinimalistTemplate() => CreateTemplate("resumecreative");

    public PdfDocumentModel CreateResumeAcademicCvTemplate() => CreateTemplate("resumeacademic");

    public PdfDocumentModel CreateAcademicPaperTemplate() => CreateTemplate("academic");

    public PdfDocumentModel CreateMathResearchPaperTemplate() => CreateTemplate("mathresearch");

    public PdfDocumentModel CreatePhysicsResearchPaperTemplate() => CreateTemplate("physicsresearch");

    public PdfDocumentModel CreateHistoryResearchPaperTemplate() => CreateTemplate("historyresearch");

    public PdfDocumentModel CreateFinanceResearchPaperTemplate() => CreateTemplate("financeresearch");

    public PdfDocumentModel CreateCertificateTemplate() => CreateTemplate("certificate");

    public PdfDocumentModel CreateCertificateNavyGoldTemplate() => CreateTemplate("certificatenavygold");

    public PdfDocumentModel CreateDiplomaAcademicTemplate() => CreateTemplate("diploma");

    public PdfDocumentModel CreateWeddingInvitationTraditionalTemplate() => CreateTemplate("weddingtraditional");

    public PdfDocumentModel CreateWeddingInvitationRoyalFloralTemplate() => CreateTemplate("weddingroyalfloral");

    public PdfDocumentModel CreateGalaInvitationTemplate() => CreateTemplate("galainvitation");
    public PdfDocumentModel CreateTypographyShowcaseTemplate() => CreateTemplate("typographyshowcase");
    public PdfDocumentModel CreateRichTextShowcaseTemplate() => CreateTemplate("richtextshowcase");
    public PdfDocumentModel CreateOcrAnalysisReportTemplate() => CreateTemplate("ocranalysisreport");

    public PdfDocumentModel CreateMathBODMASWorksheetTemplate() => CreateTemplate("bodmas_worksheet");
    public PdfDocumentModel CreateFactorizationWorksheetTemplate() => CreateTemplate("factorization_worksheet");
    public PdfDocumentModel CreateBilingualExamPaperTemplate() => CreateTemplate("bilingual_exam_paper");
    public PdfDocumentModel CreateStatesOfMatterDiagramNotesTemplate() => CreateTemplate("states_of_matter_notes");
    public PdfDocumentModel CreateQuadrilateralsGuideTemplate() => CreateTemplate("quadrilaterals_guide");
    public PdfDocumentModel CreateArduinoCheatSheetTemplate() => CreateTemplate("arduino_cheatsheet");

    public PdfDocumentModel CreateBlankDocument(PageFormat format = PageFormat.A4, PageOrientation orientation = PageOrientation.Portrait)
    {
        return _blankTemplate.Create(format, orientation);
    }
}
