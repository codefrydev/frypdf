using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Runtime.Loader;
using PdfEditorApp.Core.Plugins;
using PdfEditorApp.Services; // FryPdfPaths — MSIX-safe writable paths; AppLogService — diagnostic logging

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

            AppLogService.Instance.Log(AppLogLevel.Debug, "PluginLoader",
                $"Probing native library '{unmanagedDllName}' (rid={rid}) across {candidatePaths.Length} candidate path(s) under '{_pluginDirectory}'.");

            foreach (var p in candidatePaths)
            {
                if (File.Exists(p))
                {
                    AppLogService.Instance.Log(AppLogLevel.Debug, "PluginLoader",
                        $"Resolved native library '{unmanagedDllName}' to '{p}'.");
                    return LoadUnmanagedDllFromPath(p);
                }
            }

            AppLogService.Instance.Log(AppLogLevel.Debug, "PluginLoader",
                $"Native library '{unmanagedDllName}' not found in any candidate path; falling back to default resolution.");
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
    /// If the assembly lives outside the writable plugins directory (e.g. Downloads, Desktop, or a
    /// read-only Program Files install dir), it is first staged to
    /// <c>FryPdfPaths.PluginsDirectory\.staging\</c>. This also strips the Windows
    /// Zone.Identifier NTFS alternate data stream (Mark of the Web) that blocks DLL loading
    /// when a file is downloaded from the internet.
    /// </summary>
    public static PluginAssemblyPackage LoadPluginAssembly(string assemblyPath)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(assemblyPath);
        if (!File.Exists(assemblyPath))
        {
            var notFoundEx = new FileNotFoundException($"Plugin assembly '{assemblyPath}' not found.");
            AppLogService.Instance.LogError("PluginLoader", "Assembly file not found", notFoundEx);
            throw notFoundEx;
        }

        var sw = Stopwatch.StartNew();
        var fullPath = Path.GetFullPath(assemblyPath);

        // If the DLL is not already inside the writable plugins dir, copy it there first.
        // This serves two purposes:
        //   1. Strips the Windows Zone.Identifier (Mark of the Web) which blocks ALC loads.
        //   2. Ensures we have write access to the directory (needed for runtimes/ unpack).
        var pluginsRoot = FryPdfPaths.PluginsDirectory;
        bool staged = !fullPath.StartsWith(pluginsRoot, StringComparison.OrdinalIgnoreCase);
        if (staged)
        {
            AppLogService.Instance.Log(AppLogLevel.Info, "PluginLoader",
                $"'{fullPath}' is outside the writable plugins directory; staging to strip Windows Zone.Identifier (Mark of the Web) and ensure write access.");
            fullPath = StageToPluginsDirectory(fullPath, pluginsRoot);
        }

        var alc = new CollectiblePluginLoadContext(fullPath);
        Assembly assembly;
        try
        {
            assembly = alc.LoadFromAssemblyPath(fullPath);
        }
        catch (Exception ex)
        {
            AppLogService.Instance.LogError("PluginLoader",
                $"Failed to load assembly '{fullPath}' into an isolated load context (staged={staged})", ex);
            throw;
        }

        List<Type> pluginTypes;
        try
        {
            pluginTypes = assembly.GetTypes()
                .Where(t => typeof(IFryPlugin).IsAssignableFrom(t) && !t.IsAbstract && !t.IsInterface)
                .ToList();
        }
        catch (ReflectionTypeLoadException ex)
        {
            // ex.ToString() alone omits LoaderExceptions (the actual per-type failure reasons) —
            // LogError's helper unwraps them so a Windows-only missing-dependency case is diagnosable.
            AppLogService.Instance.LogError("PluginLoader", $"Failed to reflect types from '{fullPath}'", ex);
            throw;
        }

        var plugins = new List<IFryPlugin>();
        foreach (var type in pluginTypes)
        {
            try
            {
                if (Activator.CreateInstance(type) is IFryPlugin instance)
                    plugins.Add(instance);
            }
            catch (Exception ex)
            {
                AppLogService.Instance.LogError("PluginLoader", $"Could not instantiate '{type.FullName}'", ex);
            }
        }

        var package = new PluginAssemblyPackage(fullPath, plugins, alc);
        lock (_activePackages)
        {
            _activePackages.Add(package);
        }

        AppLogService.Instance.Log(AppLogLevel.Info, "PluginLoader",
            $"Loaded assembly '{Path.GetFileName(fullPath)}' with {plugins.Count} plugin(s) in {sw.ElapsedMilliseconds}ms (staged={staged}).");
        return package;
    }

    /// <summary>
    /// Copies a DLL and all sibling files (dependencies, runtimes/, etc.) from
    /// <paramref name="sourceDllPath"/> into a uniquely-named staging sub-folder under
    /// <c>FryPdfPaths.PluginsDirectory\.staging\</c>.
    /// The copy naturally strips the Windows Zone.Identifier NTFS alternate data stream.
    /// Returns the full path to the copied DLL.
    /// </summary>
    private static string StageToPluginsDirectory(string sourceDllPath, string pluginsRoot)
    {
        var sw = Stopwatch.StartNew();
        var sourceDir = Path.GetDirectoryName(sourceDllPath) ?? string.Empty;
        var dllName = Path.GetFileNameWithoutExtension(sourceDllPath);
        var stagingDir = Path.Combine(pluginsRoot, ".staging", $"{dllName}_{Path.GetRandomFileName().Replace(".", "")}");
        Directory.CreateDirectory(stagingDir);

        // Copy the target DLL
        var destDll = Path.Combine(stagingDir, Path.GetFileName(sourceDllPath));
        File.Copy(sourceDllPath, destDll, overwrite: true);

        // Copy sibling files (dependencies, runtimes/, native/, etc.) if source is a real dir
        int companionCount = 0;
        if (!string.IsNullOrEmpty(sourceDir) && Directory.Exists(sourceDir))
        {
            foreach (var file in Directory.EnumerateFiles(sourceDir, "*", SearchOption.AllDirectories))
            {
                if (string.Equals(file, sourceDllPath, StringComparison.OrdinalIgnoreCase))
                    continue; // already copied

                var relative = Path.GetRelativePath(sourceDir, file);
                var destFile = Path.Combine(stagingDir, relative);
                var destFileDir = Path.GetDirectoryName(destFile);
                if (!string.IsNullOrEmpty(destFileDir))
                    Directory.CreateDirectory(destFileDir);

                try
                {
                    File.Copy(file, destFile, overwrite: true);
                    companionCount++;
                }
                catch (Exception ex)
                {
                    // non-fatal: skip unreadable companions — same behavior, now visible in diagnostics
                    AppLogService.Instance.LogWarning("PluginLoader", $"Skipped copying companion file '{file}' during staging", ex);
                }
            }
        }

        AppLogService.Instance.Log(AppLogLevel.Info, "PluginLoader",
            $"Staged '{Path.GetFileName(sourceDllPath)}' to '{stagingDir}' ({companionCount} companion file(s), stripped Zone.Identifier) in {sw.ElapsedMilliseconds}ms.");
        return destDll;
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
                AppLogService.Instance.LogWarning("PluginLoader", $"Failed to unpack/load package '{pkgFile}'", ex);
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
                AppLogService.Instance.LogWarning("PluginLoader", $"Failed to load plugin from subdirectory '{subDir}'", ex);
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
