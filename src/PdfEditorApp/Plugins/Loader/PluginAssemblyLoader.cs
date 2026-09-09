using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Runtime.Loader;
using PdfEditorApp.Core.Plugins;
using PdfEditorApp.Core.Plugins.Manifests;
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

        // Drop the static root first, otherwise the package (and through it the ALC)
        // stays reachable and the context can never actually be collected.
        PluginAssemblyLoader.Forget(this);

        if (_context.IsCollectible)
        {
            // Unload is cooperative: the runtime collects the context once nothing
            // references it. Deliberately no GC.Collect() here — directory discovery
            // disposes one package per non-plugin DLL, and forcing a blocking
            // gen-2 collection per DLL made startup pathological.
            _context.Unload();
        }
    }
}

/// <summary>
/// Collectible <see cref="AssemblyLoadContext"/> that allows dynamic plugin DLLs to be loaded and later completely unloaded.
/// </summary>
public sealed class CollectiblePluginLoadContext : AssemblyLoadContext
{
    /// <summary>
    /// Assemblies the host owns. Plugins compile against these but never ship them
    /// (<c>Private=false</c> / <c>ExcludeAssets=runtime</c>), so they MUST resolve to the copy the
    /// host already has loaded — otherwise <see cref="IFryPlugin"/> would have two distinct type
    /// identities and no plugin would ever be discovered.
    /// </summary>
    private static readonly HashSet<string> HostContractAssemblies =
        new(StringComparer.OrdinalIgnoreCase) { "PdfEditorApp", "PdfEditorApp.Core" };

    private readonly AssemblyDependencyResolver _resolver;
    private readonly string _pluginDirectory;

    public CollectiblePluginLoadContext(string pluginPath, bool isCollectible = true)
        : base(name: Path.GetFileNameWithoutExtension(pluginPath), isCollectible: isCollectible)
    {
        _resolver = new AssemblyDependencyResolver(pluginPath);
        _pluginDirectory = Path.GetDirectoryName(pluginPath) ?? string.Empty;
    }

    protected override Assembly? Load(AssemblyName assemblyName)
    {
        // Stage 1 — host contracts, matched on simple name only.
        // A plugin records whatever AssemblyVersion the host had when it was compiled, and the
        // default binder refuses to substitute a *lower* version (reported, confusingly, as
        // "cannot find the file specified" even though the DLL sits next to the exe). Binding by
        // simple name keeps plugins loadable across host version drift, and running before the
        // directory probe below guarantees a single contract identity even if a plugin wrongly
        // bundles its own copy.
        if (assemblyName.Name != null && HostContractAssemblies.Contains(assemblyName.Name))
        {
            var host = ResolveFromDefaultContext(assemblyName.Name);
            if (host != null)
            {
                AppLogService.Instance.Log(AppLogLevel.Debug, "PluginLoader",
                    $"Redirected host contract '{assemblyName.Name}' (plugin requested v{assemblyName.Version?.ToString() ?? "?"}) " +
                    $"to the host's loaded v{host.GetName().Version?.ToString() ?? "?"}.");
                return host;
            }
        }

        // Stage 2 — plugin-private dependencies win over anything the host happens to carry.
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

        // Stage 3 — host-provided third-party dependencies (Avalonia, CommunityToolkit.Mvvm,
        // Material.Icons.Avalonia, Microsoft.Extensions.*). Plugins reference these with
        // PrivateAssets="all" and never ship them, so they hit the same version-drift wall as the
        // host contracts above. Returning null here would hand the request to the strict default
        // binder; a simple-name match keeps the plugin loadable.
        if (assemblyName.Name != null)
        {
            var shared = ResolveFromDefaultContext(assemblyName.Name);
            if (shared != null)
            {
                AppLogService.Instance.Log(AppLogLevel.Debug, "PluginLoader",
                    $"Resolved host-provided dependency '{assemblyName.Name}' (plugin requested v{assemblyName.Version?.ToString() ?? "?"}) " +
                    $"to the host's loaded v{shared.GetName().Version?.ToString() ?? "?"}.");
                return shared;
            }
        }

        return null;
    }

    /// <summary>
    /// Finds an assembly in the default (host) load context by simple name, deliberately ignoring
    /// version, culture and public key token.
    /// </summary>
    private static Assembly? ResolveFromDefaultContext(string simpleName)
    {
        foreach (var loaded in Default.Assemblies)
        {
            if (string.Equals(loaded.GetName().Name, simpleName, StringComparison.OrdinalIgnoreCase))
                return loaded;
        }

        // Not loaded yet — ask the default context to bind it by simple name alone, so the
        // version the plugin was compiled against never enters the decision.
        try
        {
            return Default.LoadFromAssemblyName(new AssemblyName(simpleName));
        }
        catch (Exception ex) when (ex is FileNotFoundException or FileLoadException or BadImageFormatException)
        {
            return null;
        }
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
    /// <summary>Safety valve on how many companion files a single staging pass will copy.</summary>
    private const int MaxStagedCompanions = 2000;

    private static readonly List<PluginAssemblyPackage> _activePackages = new();

    /// <summary>Packages currently held open by this loader.</summary>
    public static IReadOnlyList<PluginAssemblyPackage> ActivePackages
    {
        get { lock (_activePackages) { return _activePackages.ToArray(); } }
    }

    /// <summary>
    /// Removes a package from the active list. Called by <see cref="PluginAssemblyPackage.Dispose"/>
    /// so a disposed package does not stay rooted in a static field.
    /// </summary>
    internal static void Forget(PluginAssemblyPackage package)
    {
        lock (_activePackages)
        {
            _activePackages.Remove(package);
        }
    }

    /// <summary>
    /// Disposes every loaded package, unloading their assembly load contexts.
    /// </summary>
    public static void UnloadAll()
    {
        PluginAssemblyPackage[] snapshot;
        lock (_activePackages)
        {
            snapshot = _activePackages.ToArray();
        }

        foreach (var package in snapshot)
        {
            try
            {
                package.Dispose();
            }
            catch (Exception ex)
            {
                AppLogService.Instance.LogWarning("PluginLoader",
                    $"Failed to unload '{Path.GetFileName(package.AssemblyPath)}'", ex);
            }
        }
    }

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
        // Separator-aware: a bare StartsWith treats "<pluginsRoot>Evil/x.dll" as inside
        // the plugins directory.
        bool staged = !PluginIdValidator.IsInside(fullPath, pluginsRoot);
        if (staged)
        {
            AppLogService.Instance.Log(AppLogLevel.Info, "PluginLoader",
                $"'{fullPath}' is outside the writable plugins directory; staging to strip Windows Zone.Identifier (Mark of the Web) and ensure write access.");
            fullPath = StageToPluginsDirectory(fullPath, pluginsRoot);
        }

        var alc = new CollectiblePluginLoadContext(fullPath, isCollectible: true);
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

            // "The system cannot find the file specified" for a host assembly is misleading — the
            // DLL ships next to the executable. Dump what the host actually has so the cause is
            // readable from the log instead of requiring a repro.
            LogHostResolutionState(ex);

            // A version-drift failure reaches us as a bare FileNotFoundException naming an
            // assembly the host actually has, just at a different version. Say so, rather than
            // letting "cannot find the file specified" imply a missing file.
            var mismatch = DetectAbiMismatch(ex);
            if (mismatch != null)
            {
                AppLogService.Instance.Log(AppLogLevel.Error, "PluginLoader", mismatch.Message);
                throw mismatch;
            }

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
    /// Records, for every dependency the plugin could not bind, what the host has under that name
    /// and whether the DLL exists on disk next to the executable. A "cannot find the file
    /// specified" for a host assembly is otherwise unfalsifiable from a log alone: it looks like a
    /// missing file whether the file is missing, present but unbindable, or present at a version
    /// the binder rejected.
    /// </summary>
    private static void LogHostResolutionState(ReflectionTypeLoadException ex)
    {
        foreach (var loaderEx in ex.LoaderExceptions)
        {
            if (loaderEx is not FileNotFoundException { FileName: { Length: > 0 } fileName })
                continue;

            string? simpleName = null;
            try { simpleName = new AssemblyName(fileName).Name; }
            catch (Exception parseEx) when (parseEx is ArgumentException or FileLoadException) { }
            simpleName ??= fileName;

            var loaded = AssemblyLoadContext.Default.Assemblies
                .FirstOrDefault(a => string.Equals(a.GetName().Name, simpleName, StringComparison.OrdinalIgnoreCase));

            string onDisk;
            try
            {
                var candidate = Path.Combine(AppContext.BaseDirectory, $"{simpleName}.dll");
                onDisk = File.Exists(candidate) ? candidate : $"absent from '{AppContext.BaseDirectory}'";
            }
            catch (Exception pathEx) when (pathEx is ArgumentException or PathTooLongException)
            {
                onDisk = "unknown";
            }

            AppLogService.Instance.Log(AppLogLevel.Error, "PluginLoader",
                $"Unresolved dependency '{fileName}'. Host has " +
                (loaded == null
                    ? "NOTHING loaded under that name"
                    : $"v{loaded.GetName().Version?.ToString() ?? "?"} loaded from '{(string.IsNullOrEmpty(loaded.Location) ? "<bundled>" : loaded.Location)}'") +
                $"; on disk: {onDisk}.");
        }
    }

    /// <summary>
    /// Inspects <see cref="ReflectionTypeLoadException.LoaderExceptions"/> for a dependency the
    /// host does provide, but under a different version — the runtime reports that as a plain
    /// "cannot find the file specified", which sends people looking for a missing DLL that is
    /// actually sitting next to the executable.
    /// </summary>
    /// <returns>A describing exception, or <c>null</c> when the failure is something else.</returns>
    private static PluginAbiMismatchException? DetectAbiMismatch(ReflectionTypeLoadException ex)
    {
        foreach (var loaderEx in ex.LoaderExceptions)
        {
            if (loaderEx is not FileNotFoundException { FileName: { Length: > 0 } fileName })
                continue;

            AssemblyName requested;
            try
            {
                requested = new AssemblyName(fileName);
            }
            catch (Exception parseEx) when (parseEx is ArgumentException or FileLoadException)
            {
                continue;
            }

            if (requested.Name is null)
                continue;

            // Only a name the host itself carries indicates version drift; anything else really is
            // a dependency the plugin forgot to ship.
            var hostVersion = AssemblyLoadContext.Default.Assemblies
                .FirstOrDefault(a => string.Equals(a.GetName().Name, requested.Name, StringComparison.OrdinalIgnoreCase))
                ?.GetName().Version;

            if (hostVersion != null && hostVersion != requested.Version)
            {
                return new PluginAbiMismatchException(requested.Name, requested.Version, hostVersion, ex);
            }
        }

        return null;
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
        var stagingRoot = Path.Combine(pluginsRoot, ".staging");
        PruneStagingDirectory(stagingRoot);

        var stagingDir = Path.Combine(stagingRoot, $"{dllName}_{Path.GetRandomFileName().Replace(".", "")}");
        Directory.CreateDirectory(stagingDir);

        // Copy the target DLL
        var destDll = Path.Combine(stagingDir, Path.GetFileName(sourceDllPath));
        File.Copy(sourceDllPath, destDll, overwrite: true);

        // Copy companions. A DLL sitting loose in ~/Downloads or on the Desktop shares its
        // folder with everything else there, so only a folder that looks like a dedicated
        // plugin folder is copied wholesale — otherwise this recursively copied the user's
        // entire Downloads tree into the staging directory.
        int companionCount = 0;
        if (!string.IsNullOrEmpty(sourceDir) && Directory.Exists(sourceDir))
        {
            // Only a folder carrying a plugin.json is treated as a dedicated plugin folder.
            // A .deps.json is not a good enough signal — every .NET output folder has one,
            // including the app's own bin directory.
            bool dedicatedPluginFolder = File.Exists(Path.Combine(sourceDir, "plugin.json"));

            IEnumerable<string> companions = dedicatedPluginFolder
                ? Directory.EnumerateFiles(sourceDir, "*", SearchOption.AllDirectories)
                : EnumerateOwnCompanions(sourceDir, dllName);

            foreach (var file in companions)
            {
                if (string.Equals(file, sourceDllPath, StringComparison.OrdinalIgnoreCase))
                    continue; // already copied

                // The plugins directory can live *inside* the source directory (it does for a
                // dev build, where it is <baseDir>/plugins). Copying it would recurse the
                // staging folder into itself until the path length blows up.
                if (PluginIdValidator.IsInside(file, pluginsRoot))
                    continue;

                if (companionCount >= MaxStagedCompanions)
                {
                    AppLogService.Instance.LogWarning("PluginLoader",
                        $"Stopped staging companions for '{dllName}' at {MaxStagedCompanions} files; " +
                        $"'{sourceDir}' holds more files than a plugin folder should.",
                        null);
                    break;
                }

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

            if (!dedicatedPluginFolder)
            {
                AppLogService.Instance.Log(AppLogLevel.Debug, "PluginLoader",
                    $"'{sourceDir}' has no plugin.json, so it is not treated as a dedicated plugin " +
                    "folder; staged only the assembly's own companion files.");
            }
        }

        AppLogService.Instance.Log(AppLogLevel.Info, "PluginLoader",
            $"Staged '{Path.GetFileName(sourceDllPath)}' to '{stagingDir}' ({companionCount} companion file(s), stripped Zone.Identifier) in {sw.ElapsedMilliseconds}ms.");
        return destDll;
    }

    /// <summary>
    /// Yields the files that belong to <paramref name="dllName"/> itself: its debug symbols,
    /// its deps/runtimeconfig manifests, and any <c>runtimes/</c> native payload beside it.
    /// </summary>
    private static IEnumerable<string> EnumerateOwnCompanions(string sourceDir, string dllName)
    {
        foreach (var suffix in new[] { ".pdb", ".deps.json", ".runtimeconfig.json", ".xml" })
        {
            var candidate = Path.Combine(sourceDir, dllName + suffix);
            if (File.Exists(candidate)) yield return candidate;
        }

        // Top-level managed siblings only — a bare plugin DLL usually ships its dependencies
        // beside it. Deliberately not recursive: that is what copied whole Downloads trees.
        foreach (var sibling in Directory.EnumerateFiles(sourceDir, "*.dll", SearchOption.TopDirectoryOnly))
            yield return sibling;

        var runtimesDir = Path.Combine(sourceDir, "runtimes");
        if (Directory.Exists(runtimesDir))
        {
            foreach (var file in Directory.EnumerateFiles(runtimesDir, "*", SearchOption.AllDirectories))
                yield return file;
        }
    }

    /// <summary>
    /// Deletes staging folders left behind by previous sessions. Each load previously created
    /// a new uniquely-named folder and nothing ever removed them, so .staging grew without
    /// bound. Folders still locked by a loaded assembly simply fail to delete and are skipped.
    /// </summary>
    private static void PruneStagingDirectory(string stagingRoot)
    {
        if (!Directory.Exists(stagingRoot)) return;

        var cutoff = DateTime.UtcNow - TimeSpan.FromDays(1);
        int removed = 0;

        foreach (var dir in Directory.EnumerateDirectories(stagingRoot))
        {
            try
            {
                if (Directory.GetLastWriteTimeUtc(dir) > cutoff) continue;
                Directory.Delete(dir, recursive: true);
                removed++;
            }
            catch
            {
                // Still in use (the DLL is loaded and locked) or not ours to delete — skip it.
            }
        }

        if (removed > 0)
        {
            AppLogService.Instance.Log(AppLogLevel.Debug, "PluginLoader",
                $"Pruned {removed} stale staging folder(s) from '{stagingRoot}'.");
        }
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
            catch (Exception ex)
            {
                // Skip non-.NET or incompatible assemblies — but say so. Every other catch
                // in this file logs; this one silently dropped plugins that failed to load.
                AppLogService.Instance.LogWarning("PluginLoader",
                    $"Skipped '{Path.GetFileName(dll)}': not a loadable plugin assembly", ex);
            }
        }

        return packages;
    }
}
