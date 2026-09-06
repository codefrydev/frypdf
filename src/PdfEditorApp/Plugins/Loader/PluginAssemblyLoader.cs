using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Runtime.Loader;
using PdfEditorApp.Core.Plugins;

namespace PdfEditorApp.Plugins.Loader;

/// <summary>
/// Container holding instantiated plugins from an isolated assembly, along with its collectible load context.
/// </summary>
public sealed class PluginAssemblyPackage : IDisposable
{
    private readonly CollectiblePluginLoadContext _context;
    private bool _isDisposed;

    public IReadOnlyList<IFryPlugin> Plugins { get; }
    public string AssemblyPath { get; }

    public PluginAssemblyPackage(
        string assemblyPath,
        IReadOnlyList<IFryPlugin> plugins,
        CollectiblePluginLoadContext context)
    {
        AssemblyPath = assemblyPath;
        Plugins = plugins;
        _context = context;
    }

    public void Dispose()
    {
        if (_isDisposed) return;
        _isDisposed = true;

        if (_context.IsCollectible)
        {
            _context.Unload();
            GC.Collect();
            GC.WaitForPendingFinalizers();
        }
    }
}

/// <summary>
/// Collectible <see cref="AssemblyLoadContext"/> that allows dynamic plugin DLLs to be loaded and later completely unloaded.
/// </summary>
public sealed class CollectiblePluginLoadContext : AssemblyLoadContext
{
    private readonly AssemblyDependencyResolver _resolver;
    private readonly string _pluginDirectory;

    public CollectiblePluginLoadContext(string pluginPath, bool isCollectible = false)
        : base(name: Path.GetFileNameWithoutExtension(pluginPath), isCollectible: isCollectible)
    {
        _resolver = new AssemblyDependencyResolver(pluginPath);
        _pluginDirectory = Path.GetDirectoryName(pluginPath) ?? string.Empty;
    }

    protected override Assembly? Load(AssemblyName assemblyName)
    {
        string? assemblyPath = _resolver.ResolveAssemblyToPath(assemblyName);
        if (assemblyPath != null && File.Exists(assemblyPath))
        {
            return LoadFromAssemblyPath(assemblyPath);
        }

        // Direct fallback: check for <assemblyName.Name>.dll in the plugin directory
        if (!string.IsNullOrEmpty(_pluginDirectory))
        {
            var candidate = Path.Combine(_pluginDirectory, $"{assemblyName.Name}.dll");
            if (File.Exists(candidate))
            {
                return LoadFromAssemblyPath(candidate);
            }

            // Also search plugin directory recursively if dependencies are in subfolders
            try
            {
                var match = Directory.GetFiles(_pluginDirectory, $"{assemblyName.Name}.dll", SearchOption.AllDirectories).FirstOrDefault();
                if (match != null && File.Exists(match))
                {
                    return LoadFromAssemblyPath(match);
                }
            }
            catch { }
        }

        return null;
    }

    protected override IntPtr LoadUnmanagedDll(string unmanagedDllName)
    {
        string? libraryPath = _resolver.ResolveUnmanagedDllToPath(unmanagedDllName);
        if (libraryPath != null && File.Exists(libraryPath))
        {
            return LoadUnmanagedDllFromPath(libraryPath);
        }

        // Direct fallback: check in runtimes/<rid>/native/ or plugin directory
        if (!string.IsNullOrEmpty(_pluginDirectory))
        {
            string rid = OperatingSystem.IsMacOS()
                ? (System.Runtime.InteropServices.RuntimeInformation.ProcessArchitecture == System.Runtime.InteropServices.Architecture.Arm64 ? "osx-arm64" : "osx-x64")
                : OperatingSystem.IsWindows()
                    ? (System.Runtime.InteropServices.RuntimeInformation.ProcessArchitecture == System.Runtime.InteropServices.Architecture.Arm64 ? "win-arm64" : "win-x64")
                    : (System.Runtime.InteropServices.RuntimeInformation.ProcessArchitecture == System.Runtime.InteropServices.Architecture.Arm64 ? "linux-arm64" : "linux-x64");

            var candidatePaths = new[]
            {
                Path.Combine(_pluginDirectory, "runtimes", rid, "native", unmanagedDllName),
                Path.Combine(_pluginDirectory, "runtimes", rid, "native", $"{unmanagedDllName}.dylib"),
                Path.Combine(_pluginDirectory, "runtimes", rid, "native", $"lib{unmanagedDllName}.dylib"),
                Path.Combine(_pluginDirectory, "runtimes", rid, "native", $"{unmanagedDllName}.so"),
                Path.Combine(_pluginDirectory, "runtimes", rid, "native", $"lib{unmanagedDllName}.so"),
                Path.Combine(_pluginDirectory, "runtimes", rid, "native", $"{unmanagedDllName}.dll"),
                Path.Combine(_pluginDirectory, unmanagedDllName),
                Path.Combine(_pluginDirectory, $"{unmanagedDllName}.dylib"),
                Path.Combine(_pluginDirectory, $"lib{unmanagedDllName}.dylib"),
                Path.Combine(_pluginDirectory, $"{unmanagedDllName}.dll")
            };

            foreach (var p in candidatePaths)
            {
                if (File.Exists(p))
                {
                    return LoadUnmanagedDllFromPath(p);
                }
            }
        }

        return base.LoadUnmanagedDll(unmanagedDllName);
    }
}

/// <summary>
/// Discovers and loads external .NET 10 plugin assemblies at runtime.
/// </summary>
public static class PluginAssemblyLoader
{
    private static readonly List<PluginAssemblyPackage> _activePackages = new();

    /// <summary>
    /// Loads an isolated assembly, instantiates any <see cref="IFryPlugin"/> implementations, and returns a package.
    /// </summary>
    public static PluginAssemblyPackage LoadPluginAssembly(string assemblyPath)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(assemblyPath);
        if (!File.Exists(assemblyPath))
        {
            throw new FileNotFoundException($"Plugin assembly '{assemblyPath}' not found.");
        }

        var fullPath = Path.GetFullPath(assemblyPath);
        var alc = new CollectiblePluginLoadContext(fullPath);
        var assembly = alc.LoadFromAssemblyPath(fullPath);

        var pluginTypes = assembly.GetTypes()
            .Where(t => typeof(IFryPlugin).IsAssignableFrom(t) && !t.IsAbstract && !t.IsInterface)
            .ToList();

        var plugins = new List<IFryPlugin>();
        foreach (var type in pluginTypes)
        {
            try
            {
                if (Activator.CreateInstance(type) is IFryPlugin instance)
                {
                    plugins.Add(instance);
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[PluginAssemblyLoader] Could not instantiate '{type.FullName}': {ex.Message}");
            }
        }

        var package = new PluginAssemblyPackage(fullPath, plugins, alc);
        lock (_activePackages)
        {
            _activePackages.Add(package);
        }
        return package;
    }

    /// <summary>
    /// Scans a directory for plugin packages (.fryplugin), plugin subdirectories, and standalone DLLs,
    /// loading all discovered plugin packages into isolated collectible ALC contexts.
    /// </summary>
    public static IReadOnlyList<PluginAssemblyPackage> DiscoverAndLoadDirectory(string pluginsDirectory)
    {
        if (string.IsNullOrWhiteSpace(pluginsDirectory) || !Directory.Exists(pluginsDirectory))
        {
            return Array.Empty<PluginAssemblyPackage>();
        }

        var packages = new List<PluginAssemblyPackage>();
        var loadedPaths = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

        // 1. Discover and unpack .fryplugin packages
        var packageFiles = Directory.GetFiles(pluginsDirectory, "*.fryplugin", SearchOption.TopDirectoryOnly);
        foreach (var pkgFile in packageFiles)
        {
            try
            {
                var pkgResult = FryPluginPackageLoader.UnpackAndLoad(pkgFile, pluginsDirectory);
                if (pkgResult.AssemblyPackage.Plugins.Count > 0)
                {
                    packages.Add(pkgResult.AssemblyPackage);
                    loadedPaths.Add(pkgResult.AssemblyPackage.AssemblyPath);
                }
                else
                {
                    pkgResult.Dispose();
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[PluginAssemblyLoader] Failed to unpack/load package '{pkgFile}': {ex.Message}");
            }
        }

        // 2. Discover unpacked plugin subdirectories
        var subDirectories = Directory.GetDirectories(pluginsDirectory);
        foreach (var subDir in subDirectories)
        {
            try
            {
                string? entryDll = null;
                var manifestFile = Path.Combine(subDir, "plugin.json");
                if (File.Exists(manifestFile))
                {
                    var json = File.ReadAllText(manifestFile);
                    var manifest = System.Text.Json.JsonSerializer.Deserialize<PdfEditorApp.Core.Plugins.Manifests.PluginManifest>(json);
                    if (!string.IsNullOrWhiteSpace(manifest?.EntryPoint))
                    {
                        var candidate = Path.Combine(subDir, manifest.EntryPoint);
                        if (File.Exists(candidate))
                        {
                            entryDll = candidate;
                        }
                    }
                }

                if (entryDll == null)
                {
                    var folderName = Path.GetFileName(subDir);
                    var candidate = Path.Combine(subDir, $"{folderName}.dll");
                    if (File.Exists(candidate))
                    {
                        entryDll = candidate;
                    }
                    else
                    {
                        entryDll = Directory.GetFiles(subDir, "*.dll", SearchOption.TopDirectoryOnly).FirstOrDefault();
                    }
                }

                if (entryDll != null && !loadedPaths.Contains(entryDll))
                {
                    var package = LoadPluginAssembly(entryDll);
                    if (package.Plugins.Count > 0)
                    {
                        packages.Add(package);
                        loadedPaths.Add(entryDll);
                    }
                    else
                    {
                        package.Dispose();
                    }
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[PluginAssemblyLoader] Failed to load plugin from subdirectory '{subDir}': {ex.Message}");
            }
        }

        // 3. Discover top-level standalone DLLs
        var dllFiles = Directory.GetFiles(pluginsDirectory, "*.dll", SearchOption.TopDirectoryOnly);
        foreach (var dll in dllFiles)
        {
            if (loadedPaths.Contains(dll)) continue;

            try
            {
                var package = LoadPluginAssembly(dll);
                if (package.Plugins.Count > 0)
                {
                    packages.Add(package);
                    loadedPaths.Add(dll);
                }
                else
                {
                    // No plugins in this assembly; unload immediately
                    package.Dispose();
                }
            }
            catch
            {
                // Skip non-.NET or incompatible assemblies
            }
        }

        return packages;
    }
}
