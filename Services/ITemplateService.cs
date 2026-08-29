using PdfEditorApp.Models;

namespace PdfEditorApp.Services;

public interface ITemplateService
{
    PdfDocumentModel CreateAnnualReportTemplate();
    PdfDocumentModel CreateInvoiceTemplate();
    PdfDocumentModel CreateResumeTemplate();
    PdfDocumentModel CreateAcademicPaperTemplate();
    PdfDocumentModel CreateCertificateTemplate();
    PdfDocumentModel CreateBlankDocument(PageFormat format = PageFormat.A4, PageOrientation orientation = PageOrientation.Portrait);
}
