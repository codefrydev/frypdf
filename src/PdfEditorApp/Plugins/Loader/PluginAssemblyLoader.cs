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

        _context.Unload();
        GC.Collect();
        GC.WaitForPendingFinalizers();
    }
}

/// <summary>
/// Collectible <see cref="AssemblyLoadContext"/> that allows dynamic plugin DLLs to be loaded and later completely unloaded.
/// </summary>
public sealed class CollectiblePluginLoadContext : AssemblyLoadContext
{
    private readonly AssemblyDependencyResolver _resolver;

    public CollectiblePluginLoadContext(string pluginPath)
        : base(name: Path.GetFileNameWithoutExtension(pluginPath), isCollectible: true)
    {
        _resolver = new AssemblyDependencyResolver(pluginPath);
    }

    protected override Assembly? Load(AssemblyName assemblyName)
    {
        string? assemblyPath = _resolver.ResolveAssemblyToPath(assemblyName);
        return assemblyPath != null ? LoadFromAssemblyPath(assemblyPath) : null;
    }
}

/// <summary>
/// Discovers and loads external .NET 10 plugin assemblies at runtime.
/// </summary>
public static class PluginAssemblyLoader
{
    /// <summary>
    /// Loads an isolated assembly, instantiates any <see cref="IFryPlugin"/> implementations, and returns a collectible package.
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

        return new PluginAssemblyPackage(fullPath, plugins, alc);
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
