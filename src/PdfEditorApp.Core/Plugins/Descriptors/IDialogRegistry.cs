using System;
using System.Collections.Generic;

namespace PdfEditorApp.Core.Plugins.Descriptors;

/// <summary>
/// Descriptor representing a modal dialog or floating studio contributed by a plugin.
/// </summary>
public sealed class DialogDescriptor
{
    public required string Id { get; init; }
    public required string Title { get; init; }
    public Type? ViewModelType { get; init; }
    public Type? ViewType { get; init; }
    public Func<IServiceProvider, object>? ViewFactory { get; init; }
    public Func<IServiceProvider, object>? ViewModelFactory { get; init; }
    public bool IsModal { get; init; } = true;
    public double? PreferredWidth { get; init; }
    public double? PreferredHeight { get; init; }
}

/// <summary>
/// Registry for pluggable modal dialogs and studio overlays.
/// </summary>
public interface IDialogRegistry
{
    /// <summary>
    /// Registers a dialog descriptor.
    /// </summary>
    IDisposable RegisterDialog(DialogDescriptor descriptor);

    /// <summary>
    /// Unregisters a dialog by its unique ID.
    /// </summary>
    bool UnregisterDialog(string dialogId);

    /// <summary>
    /// Gets a registered dialog descriptor by its ID.
    /// </summary>
    DialogDescriptor? GetDialog(string dialogId);

    /// <summary>
    /// Gets all registered dialog descriptors.
    /// </summary>
    IReadOnlyList<DialogDescriptor> GetAllDialogs();

    /// <summary>
    /// Fired when dialogs are registered or unregistered.
    /// </summary>
    event Action? RegistryChanged;
}
