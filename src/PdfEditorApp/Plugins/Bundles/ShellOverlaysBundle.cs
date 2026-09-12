using System;
using System.Collections.Generic;
using PdfEditorApp.Core.Plugins;
using PdfEditorApp.Core.Plugins.Profiles;
using PdfEditorApp.Plugins.Loading;

namespace PdfEditorApp.Plugins.Bundles;

/// <summary>
/// Plugin bundle providing full-screen loading overlays and non-modal interactive floating shell overlays.
/// </summary>
public class ShellOverlaysBundle : IFryPluginBundle
{
    public string Id => "FryPdf.Bundle.ShellOverlays";
    public string Name => "Shell Overlays & Arcade Bundle";
    public string Description => "Slot provider for non-modal floating widgets and universal full-screen loading progress overlays.";

    public IReadOnlyList<IFryPlugin> Plugins => new IFryPlugin[]
    {
        new LoadingProgressPlugin()
    };
}
