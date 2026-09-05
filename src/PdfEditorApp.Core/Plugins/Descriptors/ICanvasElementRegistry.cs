using System;
using System.Collections.Generic;

namespace PdfEditorApp.Core.Plugins.Descriptors;

/// <summary>
/// Registry for canvas element types contributed by plugins or core modules.
/// </summary>
public interface ICanvasElementRegistry
{
    /// <summary>
    /// Registers a canvas element descriptor.
    /// </summary>
    void RegisterElement(CanvasElementDescriptor descriptor);

    /// <summary>
    /// Unregisters a canvas element descriptor by its unique ID.
    /// </summary>
    bool UnregisterElement(string elementTypeId);

    /// <summary>
    /// Looks up an element descriptor by its unique element type ID.
    /// </summary>
    CanvasElementDescriptor? GetDescriptor(string elementTypeId);

    /// <summary>
    /// Looks up an element descriptor matching a domain model type.
    /// </summary>
    CanvasElementDescriptor? GetDescriptorByModelType(Type modelType);

    /// <summary>
    /// Returns all registered canvas element descriptors.
    /// </summary>
    IReadOnlyList<CanvasElementDescriptor> GetAllDescriptors();

    /// <summary>
    /// Returns all canvas element descriptors that can be inserted from the toolbar.
    /// </summary>
    IReadOnlyList<CanvasElementDescriptor> GetInsertableElements();
}
