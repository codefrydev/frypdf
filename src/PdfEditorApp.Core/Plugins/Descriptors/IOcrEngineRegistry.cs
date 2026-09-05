using System;
using System.Collections.Generic;
using PdfEditorApp.Core.Models;

namespace PdfEditorApp.Core.Plugins.Descriptors;

/// <summary>
/// Registry for discovering and dispatching OCR engines contributed by plugins.
/// </summary>
public interface IOcrEngineRegistry
{
    /// <summary>
    /// Registers an OCR engine into the system.
    /// </summary>
    IDisposable RegisterEngine(IOcrEngine engine);

    /// <summary>
    /// Finds an OCR engine by its display name.
    /// </summary>
    IOcrEngine? GetEngine(string engineName);

    /// <summary>
    /// Gets all registered OCR engines.
    /// </summary>
    IReadOnlyList<IOcrEngine> GetAllEngines();

    /// <summary>
    /// Gets all available OCR engines that are supported on the current host OS.
    /// </summary>
    IReadOnlyList<IOcrEngine> GetAvailableEngines();

    /// <summary>
    /// Raised whenever an OCR engine is registered or unregistered.
    /// </summary>
    event Action? RegistryChanged;
}
