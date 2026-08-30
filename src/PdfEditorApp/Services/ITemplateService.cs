using System.Collections.Generic;
using PdfEditorApp.Models;
using PdfEditorApp.Templates;

namespace PdfEditorApp.Services;

public interface ITemplateService
{
    IReadOnlyList<ITemplateDefinition> GetAllTemplates();
    PdfDocumentModel CreateTemplate(string templateId);

    PdfDocumentModel CreateAnnualReportTemplate();
    PdfDocumentModel CreateInvoiceTemplate();
    PdfDocumentModel CreateResumeTemplate();
    PdfDocumentModel CreateAcademicPaperTemplate();
    PdfDocumentModel CreateCertificateTemplate();
    PdfDocumentModel CreateCertificateNavyGoldTemplate();
    PdfDocumentModel CreateDiplomaAcademicTemplate();
    PdfDocumentModel CreateWeddingInvitationTraditionalTemplate();
    PdfDocumentModel CreateWeddingInvitationRoyalFloralTemplate();
    PdfDocumentModel CreateGalaInvitationTemplate();
    PdfDocumentModel CreateBlankDocument(PageFormat format = PageFormat.A4, PageOrientation orientation = PageOrientation.Portrait);
}
