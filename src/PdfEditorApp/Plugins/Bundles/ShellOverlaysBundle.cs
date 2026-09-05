using System.Collections.Generic;
using PdfEditorApp.Core.Plugins;
using PdfEditorApp.Core.Plugins.Profiles;
using PdfEditorApp.Plugins.Snake;

namespace PdfEditorApp.Plugins.Bundles;

/// <summary>
/// Plugin bundle providing non-modal, interactive floating shell overlays.
/// Adopts the DeepSeek Harness 'shell.overlay' dynamic composability model.
/// </summary>
public class ShellOverlaysBundle : IFryPluginBundle
{
    public string Id => "FryPdf.Bundle.ShellOverlays";
    public string Name => "Shell Overlays & Arcade Bundle";
    public string Description => "Non-modal floating widgets and dynamic overlays (including the retro-arcade Snake game) rendered into the 'shell.overlay' slot.";

    public IReadOnlyList<IFryPlugin> Plugins => new IFryPlugin[]
    {
        new SnakeGamePlugin()
    };
}
