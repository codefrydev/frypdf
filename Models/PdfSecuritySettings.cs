using System;

namespace PdfEditorApp.Models;

public class PdfSecuritySettings
{
    public bool IsPasswordProtected { get; set; } = false;
    public string OpenPassword { get; set; } = "";
    public string PermissionsPassword { get; set; } = "";

    public bool AllowPrinting { get; set; } = true;
    public bool AllowContentCopying { get; set; } = true;
    public bool AllowModifications { get; set; } = true;
    public bool AllowAnnotations { get; set; } = true;

    public bool ScrubMetadataOnExport { get; set; } = false;
    public bool RemoveCommentsOnExport { get; set; } = false;

    public PdfSecuritySettings Clone()
    {
        return new PdfSecuritySettings
        {
            IsPasswordProtected = IsPasswordProtected,
            OpenPassword = OpenPassword,
            PermissionsPassword = PermissionsPassword,
            AllowPrinting = AllowPrinting,
            AllowContentCopying = AllowContentCopying,
            AllowModifications = AllowModifications,
            AllowAnnotations = AllowAnnotations,
            ScrubMetadataOnExport = ScrubMetadataOnExport,
            RemoveCommentsOnExport = RemoveCommentsOnExport
        };
    }
}
