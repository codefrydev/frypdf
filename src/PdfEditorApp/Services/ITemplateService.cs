using System.Collections.Generic;
using PdfEditorApp.Models;
using PdfEditorApp.Templates;

namespace PdfEditorApp.Services;

public interface ITemplateService
{
    IReadOnlyList<ITemplateDefinition> GetAllTemplates();
    PdfDocumentModel CreateTemplate(string templateId);

    PdfDocumentModel CreateAnnualReportTemplate();
    PdfDocumentModel CreateEmployeePayslipTemplate();
    PdfDocumentModel CreateInvoiceTemplate();
    PdfDocumentModel CreateResumeTemplate();
    PdfDocumentModel CreateResumeModernCleanTemplate();
    PdfDocumentModel CreateResumeCreativeMinimalistTemplate();
    PdfDocumentModel CreateResumeAcademicCvTemplate();
    PdfDocumentModel CreateAcademicPaperTemplate();
    PdfDocumentModel CreateMathResearchPaperTemplate();
    PdfDocumentModel CreatePhysicsResearchPaperTemplate();
    PdfDocumentModel CreateHistoryResearchPaperTemplate();
    PdfDocumentModel CreateFinanceResearchPaperTemplate();
    PdfDocumentModel CreateCertificateTemplate();
    PdfDocumentModel CreateCertificateNavyGoldTemplate();
    PdfDocumentModel CreateDiplomaAcademicTemplate();
    PdfDocumentModel CreateWeddingInvitationTraditionalTemplate();
    PdfDocumentModel CreateWeddingInvitationRoyalFloralTemplate();
    PdfDocumentModel CreateGalaInvitationTemplate();
    PdfDocumentModel CreateTypographyShowcaseTemplate();
    PdfDocumentModel CreateRichTextShowcaseTemplate();
    PdfDocumentModel CreateOcrAnalysisReportTemplate();
    PdfDocumentModel CreateBlankDocument(PageFormat format = PageFormat.A4, PageOrientation orientation = PageOrientation.Portrait);
}
