using System;
using System.Collections.Generic;

namespace PdfEditorApp.Core.Plugins.Descriptors;

/// <summary>
/// Descriptor for an inspector property panel section contributed by a plugin.
/// </summary>
public class InspectorSectionDescriptor
{
    /// <summary>
    /// Unique identifier for this section, e.g. "frypdf.inspector.geometry" or "frypdf.inspector.barcode".
    /// </summary>
    public string SectionId { get; init; } = "";

    /// <summary>
    /// Section header title displayed in the inspector sidebar.
    /// </summary>
    public string Title { get; init; } = "";

    /// <summary>
    /// Material icon name for the section header.
    /// </summary>
    public string IconKind { get; init; } = "Tune";

    /// <summary>
    /// Display sort order within the inspector (lower numbers appear higher).
    /// </summary>
    public int Order { get; init; } = 100;

    /// <summary>
    /// Evaluates whether this section applies to the given element instance (or null for page/document level).
    /// </summary>
    public Func<object?, bool> AppliesTo { get; init; } = _ => true;

    /// <summary>
    /// Factory delegate to create the section ViewModel or control.
    /// </summary>
    public Func<IServiceProvider, object?, object> Factory { get; init; } = (sp, el) => new object();
}

/// <summary>
/// Registry for discovering and managing dynamic inspector property panels.
/// </summary>
public interface IInspectorRegistry
{
    /// <summary>
    /// Registers a new inspector section into the system.
    /// </summary>
    IDisposable RegisterSection(InspectorSectionDescriptor descriptor);

    /// <summary>
    /// Gets all inspector sections applicable to the selected element or context.
    /// </summary>
    IReadOnlyList<InspectorSectionDescriptor> GetSectionsForTarget(object? target);

    /// <summary>
    /// Gets all registered inspector sections.
    /// </summary>
    IReadOnlyList<InspectorSectionDescriptor> GetAllSections();

    /// <summary>
    /// Raised whenever a section is registered or unregistered.
    /// </summary>
    event Action? RegistryChanged;
}
