using System;
using System.Collections.Generic;

namespace PdfEditorApp.Core.Plugins.Descriptors;

/// <summary>
/// Thread-safe registry and manager for non-modal floating overlays targeting the 'shell.overlay' slot.
/// </summary>
public interface IOverlayRegistry
{
    /// <summary>
    /// Registers an overlay descriptor contributed by a plugin.
    /// </summary>
    IDisposable RegisterOverlay(OverlayDescriptor descriptor);

    /// <summary>
    /// Unregisters an overlay by its unique ID.
    /// </summary>
    bool UnregisterOverlay(string overlayId);

    /// <summary>
    /// Retrieves a registered overlay descriptor by its ID.
    /// </summary>
    OverlayDescriptor? GetOverlay(string overlayId);

    /// <summary>
    /// Gets all currently registered overlay descriptors.
    /// </summary>
    IReadOnlyList<OverlayDescriptor> GetAllOverlays();

    /// <summary>
    /// Displays the specified overlay in the active shell.
    /// </summary>
    void ShowOverlay(string overlayId);

    /// <summary>
    /// Hides the specified overlay.
    /// </summary>
    void HideOverlay(string overlayId);

    /// <summary>
    /// Toggles the visibility of the specified overlay.
    /// </summary>
    void ToggleOverlay(string overlayId);

    /// <summary>
    /// Checks whether the specified overlay is currently visible and active.
    /// </summary>
    bool IsOverlayVisible(string overlayId);

    /// <summary>
    /// Fired when an overlay descriptor is registered or unregistered.
    /// </summary>
    event Action? RegistryChanged;

    /// <summary>
    /// Fired when active overlay instances are shown, hidden, or modified.
    /// </summary>
    event Action? ActiveOverlaysChanged;
}
