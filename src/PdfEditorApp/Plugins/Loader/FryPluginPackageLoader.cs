using System;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Text.Json;
using PdfEditorApp.Core.Plugins;
using PdfEditorApp.Core.Plugins.Manifests;

namespace PdfEditorApp.Plugins.Loader;

/// <summary>
/// Result of unpacking and loading a self-contained .fryplugin package.
/// </summary>
public sealed class FryPluginPackageResult : IDisposable
{
    public required PluginManifest Manifest { get; init; }
    public required PluginAssemblyPackage AssemblyPackage { get; init; }
    public required string InstallDirectory { get; init; }

    public void Dispose()
    {
        AssemblyPackage.Dispose();
    }
}

/// <summary>
/// Manages packing, unpacking, validation, and loading of self-contained .fryplugin distribution archives.
/// A .fryplugin is a standard ZIP archive containing a plugin.json manifest, entry assemblies, dependencies, and assets.
/// </summary>
public static class FryPluginPackageLoader
{
    /// <summary>
    /// Unpacks a .fryplugin package to the target plugins directory and loads its contained plugins.
    /// </summary>
    public static FryPluginPackageResult UnpackAndLoad(string packageFilePath, string? targetPluginsDirectory = null)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(packageFilePath);
        if (!File.Exists(packageFilePath))
        {
            throw new FileNotFoundException($"Plugin package '{packageFilePath}' not found.");
        }

        var baseDirectory = targetPluginsDirectory ?? Path.Combine(AppContext.BaseDirectory, "plugins");
        Directory.CreateDirectory(baseDirectory);

        // 1. Read manifest from ZIP before extracting to know target folder name
        PluginManifest? manifest = null;
        using (var archive = ZipFile.OpenRead(packageFilePath))
        {
            var manifestEntry = archive.Entries.FirstOrDefault(e =>
                string.Equals(e.FullName, "plugin.json", StringComparison.OrdinalIgnoreCase) ||
                string.Equals(Path.GetFileName(e.FullName), "plugin.json", StringComparison.OrdinalIgnoreCase));

            if (manifestEntry != null)
            {
                using var stream = manifestEntry.Open();
                using var reader = new StreamReader(stream);
                var json = reader.ReadToEnd();
                manifest = JsonSerializer.Deserialize<PluginManifest>(json);
            }
        }

        var pluginId = !string.IsNullOrWhiteSpace(manifest?.Id)
            ? manifest.Id
            : Path.GetFileNameWithoutExtension(packageFilePath);

        var destinationFolder = Path.Combine(baseDirectory, pluginId);
        if (Directory.Exists(destinationFolder))
        {
            try
            {
                Directory.Delete(destinationFolder, recursive: true);
            }
            catch
            {
                // If locked, create unique timestamped folder
                destinationFolder = Path.Combine(baseDirectory, $"{pluginId}_{DateTime.UtcNow.Ticks}");
            }
        }

        Directory.CreateDirectory(destinationFolder);
        ZipFile.ExtractToDirectory(packageFilePath, destinationFolder, overwriteFiles: true);

        // 2. Identify entry assembly DLL
        string? entryDll = null;
        if (!string.IsNullOrWhiteSpace(manifest?.EntryPoint))
        {
            var candidate = Path.Combine(destinationFolder, manifest.EntryPoint);
            if (File.Exists(candidate))
            {
                entryDll = candidate;
            }
        }

        if (entryDll == null)
        {
            // Search for DLL matching pluginId or any DLL containing IFryPlugin
            var allDlls = Directory.GetFiles(destinationFolder, "*.dll", SearchOption.AllDirectories);
            entryDll = allDlls.FirstOrDefault(d => string.Equals(Path.GetFileNameWithoutExtension(d), pluginId, StringComparison.OrdinalIgnoreCase))
                       ?? allDlls.FirstOrDefault();
        }

        if (string.IsNullOrWhiteSpace(entryDll) || !File.Exists(entryDll))
        {
            throw new InvalidOperationException($"No entry DLL found in package '{packageFilePath}'.");
        }

        // 3. Load assembly into isolated collectible ALC
        var assemblyPackage = PluginAssemblyLoader.LoadPluginAssembly(entryDll);

        // Create fallback manifest if plugin.json was not packaged
        if (manifest == null)
        {
            var firstPlugin = assemblyPackage.Plugins.FirstOrDefault();
            manifest = new PluginManifest
            {
                Id = firstPlugin?.Id ?? pluginId,
                Name = firstPlugin?.Name ?? pluginId,
                Version = firstPlugin?.Version.ToString() ?? "1.0.0",
                EntryPoint = Path.GetFileName(entryDll),
                Description = "Extracted .fryplugin archive"
            };
        }

        return new FryPluginPackageResult
        {
            Manifest = manifest,
            AssemblyPackage = assemblyPackage,
            InstallDirectory = destinationFolder
        };
    }

    /// <summary>
    /// Creates a .fryplugin package archive from a directory containing a plugin.json manifest and built DLLs.
    /// </summary>
    public static string CreatePackage(string sourceDirectory, string outputPackagePath)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(sourceDirectory);
        ArgumentException.ThrowIfNullOrWhiteSpace(outputPackagePath);

        if (!Directory.Exists(sourceDirectory))
        {
            throw new DirectoryNotFoundException($"Source directory '{sourceDirectory}' not found.");
        }

        var outDir = Path.GetDirectoryName(outputPackagePath);
        if (!string.IsNullOrWhiteSpace(outDir))
        {
            Directory.CreateDirectory(outDir);
        }

        if (File.Exists(outputPackagePath))
        {
            File.Delete(outputPackagePath);
        }

        ZipFile.CreateFromDirectory(sourceDirectory, outputPackagePath, CompressionLevel.Optimal, includeBaseDirectory: false);
        return outputPackagePath;
    }
}
