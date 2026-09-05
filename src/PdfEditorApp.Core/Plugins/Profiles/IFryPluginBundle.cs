using System.Collections.Generic;

namespace PdfEditorApp.Core.Plugins.Profiles;

/// <summary>
/// Represents a cohesive bundle of plugins, mirroring the DeepSeek Harness bundle concept.
/// </summary>
public interface IFryPluginBundle
{
    /// <summary>Unique identifier for this bundle, e.g. "FryPdf.Bundle.Organize".</summary>
    string Id { get; }

    /// <summary>Display name of the bundle.</summary>
    string Name { get; }

    /// <summary>Description of the bundle's capabilities.</summary>
    string Description { get; }

    /// <summary>Collection of plugins contained within this bundle.</summary>
    IReadOnlyList<IFryPlugin> Plugins { get; }
}
