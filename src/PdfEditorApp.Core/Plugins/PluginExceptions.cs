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

/// <summary>
/// Thrown when a plugin was compiled against a version of a host assembly that this build of the
/// application does not provide. The runtime reports this as a bare
/// <see cref="System.IO.FileNotFoundException"/> ("cannot find the file specified") even when the
/// assembly is present on disk, so it is translated into this type to keep the cause actionable.
/// </summary>
public class PluginAbiMismatchException : PluginException
{
    public string AssemblyName { get; }
    public Version? RequestedVersion { get; }
    public Version? HostVersion { get; }

    public PluginAbiMismatchException(
        string assemblyName, Version? requestedVersion, Version? hostVersion, Exception innerException)
        : base($"This plugin was built against {assemblyName} " +
               $"{requestedVersion?.ToString() ?? "an unknown version"} but this build of FryPDF provides " +
               $"{hostVersion?.ToString() ?? "no matching version"}. Rebuild the plugin against the installed application.",
               innerException)
    {
        AssemblyName = assemblyName;
        RequestedVersion = requestedVersion;
        HostVersion = hostVersion;
    }
}
