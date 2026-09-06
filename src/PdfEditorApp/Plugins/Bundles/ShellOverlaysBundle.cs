using System;
using System.Collections.Generic;
using PdfEditorApp.Core.Plugins;
using PdfEditorApp.Core.Plugins.Profiles;

namespace PdfEditorApp.Plugins.Bundles;

/// <summary>
/// Plugin bundle providing non-modal, interactive floating shell overlays.
/// Dynamic third-party and arcade overlays are installed on-demand from the FryPDF Marketplace.
/// </summary>
public class ShellOverlaysBundle : IFryPluginBundle
{
    public string Id => "FryPdf.Bundle.ShellOverlays";
    public string Name => "Shell Overlays & Arcade Bundle";
    public string Description => "Slot provider for non-modal floating widgets and dynamic overlays rendered into the 'shell.overlay' slot.";

    public IReadOnlyList<IFryPlugin> Plugins => Array.Empty<IFryPlugin>();
}
