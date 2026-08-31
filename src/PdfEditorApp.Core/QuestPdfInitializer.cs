using System.Runtime.CompilerServices;
using QuestPDF.Infrastructure;

namespace PdfEditorApp.Core;

/// <summary>
/// Global static module initializer that configures QuestPDF license tier automatically
/// on module load before any document generation code or tests run.
/// </summary>
internal static class QuestPdfInitializer
{
#pragma warning disable CA2255 // The 'ModuleInitializer' attribute is only intended to be used in application code
    [ModuleInitializer]
    internal static void Initialize()
    {
        QuestPDF.Settings.License = LicenseType.Community;
    }
#pragma warning restore CA2255
}
