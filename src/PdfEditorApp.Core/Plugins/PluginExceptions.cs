using System;

namespace PdfEditorApp.Core.Plugins;

/// <summary>
/// Base exception for all plugin-related runtime errors.
/// </summary>
public class PluginException : Exception
{
    public PluginException(string message) : base(message) { }
    public PluginException(string message, Exception innerException) : base(message, innerException) { }
}

/// <summary>
/// Thrown when a plugin requires a service contract that was not registered by any loaded plugin or the host.
/// </summary>
public class PluginMissingDependencyException : PluginException
{
    public string PluginId { get; }
    public Type MissingServiceType { get; }

    public PluginMissingDependencyException(string pluginId, Type missingServiceType)
        : base($"Plugin '{pluginId}' requires service '{missingServiceType.FullName}', but it was not provided by any active plugin or host service.")
    {
        PluginId = pluginId;
        MissingServiceType = missingServiceType;
    }
}

/// <summary>
/// Thrown when a cycle is detected in plugin dependency declarations.
/// </summary>
public class PluginCircularDependencyException : PluginException
{
    public PluginCircularDependencyException(string message) : base(message) { }
}
