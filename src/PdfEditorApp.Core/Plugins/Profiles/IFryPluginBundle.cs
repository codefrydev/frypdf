using System.Collections.Generic;

namespace PdfEditorApp.Core.Plugins.Profiles;

/// <summary>
/// Represents a cohesive bundle of plugins, inspired by the Cordis bundle concept.
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
