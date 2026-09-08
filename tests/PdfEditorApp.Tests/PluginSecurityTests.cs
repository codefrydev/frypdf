using System;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Text;
using System.Text.Json;
using PdfEditorApp.Core.Plugins.Manifests;
using PdfEditorApp.Plugins.Loader;
using PdfEditorApp.Services;
using Xunit;

namespace PdfEditorApp.Tests;

/// <summary>
/// Regression tests for the Tier 2 plugin security and isolation defects.
/// </summary>
[Collection("AppLogService")]
public class PluginSecurityTests
{
    // ─── 2.1 Plugin id path traversal ───────────────────────────────────────

    [Theory]
    [InlineData("../../evil")]
    [InlineData("..")]
    [InlineData(".")]
    [InlineData("a/b")]
    [InlineData("a\\b")]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData("plugin id with spaces")]
    [InlineData("plugin:name")]
    public void PluginIdValidator_RejectsUnsafeIds(string id)
    {
        Assert.False(PluginIdValidator.IsValid(id));
    }

    [Theory]
    [InlineData("com.frypdf.snake")]
    [InlineData("Scratchpad")]
    [InlineData("my-plugin_2")]
    public void PluginIdValidator_AcceptsOrdinaryIds(string id)
    {
        Assert.True(PluginIdValidator.IsValid(id));
    }

    [Fact]
    public void PluginIdValidator_RejectsAnAbsolutePathAsId()
    {
        // Path.Combine returns a rooted second argument verbatim, discarding the root.
        string absolute = OperatingSystem.IsWindows() ? @"C:\Windows\Temp" : "/tmp/evil";
        Assert.False(PluginIdValidator.IsValid(absolute));
    }

    [Fact]
    public void PluginIdValidator_IsInside_RequiresASeparatorBoundary()
    {
        string root = Path.Combine(Path.GetTempPath(), "plugins");

        Assert.True(PluginIdValidator.IsInside(Path.Combine(root, "a", "b.dll"), root));
        Assert.True(PluginIdValidator.IsInside(root, root));

        // A bare StartsWith would call this "inside".
        Assert.False(PluginIdValidator.IsInside(root + "Evil" + Path.DirectorySeparatorChar + "x.dll", root));
    }

    [Fact]
    public void ResolveInstallDirectory_ThrowsForATraversingId()
    {
        string root = Path.Combine(Path.GetTempPath(), $"plugins_{Guid.NewGuid():N}");
        Assert.Throws<ArgumentException>(() =>
            PluginIdValidator.ResolveInstallDirectory(root, "../../evil", "test"));
    }

    /// <summary>
    /// Builds a .fryplugin archive whose manifest declares the given id.
    /// </summary>
    private static string CreatePackageWithManifestId(string manifestId)
    {
        string path = Path.Combine(Path.GetTempPath(), $"evil_{Guid.NewGuid():N}.fryplugin");
        using var archive = ZipFile.Open(path, ZipArchiveMode.Create);

        var manifestEntry = archive.CreateEntry("plugin.json");
        using (var writer = new StreamWriter(manifestEntry.Open(), Encoding.UTF8))
        {
            writer.Write(JsonSerializer.Serialize(new PluginManifest
            {
                Id = manifestId,
                Name = "Evil",
                Version = "1.0.0",
                EntryPoint = "Evil.dll"
            }));
        }

        archive.CreateEntry("Evil.dll");
        return path;
    }

    [Fact]
    public void UnpackAndLoad_RejectsAPackageWhoseManifestIdEscapesThePluginsRoot()
    {
        string pluginsRoot = Path.Combine(Path.GetTempPath(), $"plugins_{Guid.NewGuid():N}");
        Directory.CreateDirectory(pluginsRoot);

        // A sibling directory the traversing id would have resolved into and deleted.
        string victim = Path.Combine(Path.GetDirectoryName(pluginsRoot)!, $"victim_{Guid.NewGuid():N}");
        Directory.CreateDirectory(victim);
        File.WriteAllText(Path.Combine(victim, "important.txt"), "do not delete me");

        string package = CreatePackageWithManifestId($"../{Path.GetFileName(victim)}");
        try
        {
            Assert.Throws<ArgumentException>(() =>
                FryPluginPackageLoader.UnpackAndLoad(package, pluginsRoot));

            // Directory.Delete(destinationFolder, recursive: true) must never have run.
            Assert.True(Directory.Exists(victim));
            Assert.True(File.Exists(Path.Combine(victim, "important.txt")));
        }
        finally
        {
            if (File.Exists(package)) File.Delete(package);
            if (Directory.Exists(victim)) Directory.Delete(victim, recursive: true);
            if (Directory.Exists(pluginsRoot)) Directory.Delete(pluginsRoot, recursive: true);
        }
    }

    [Fact]
    public void UnpackAndLoad_RejectsAnAbsoluteManifestId()
    {
        string pluginsRoot = Path.Combine(Path.GetTempPath(), $"plugins_{Guid.NewGuid():N}");
        Directory.CreateDirectory(pluginsRoot);

        string absolute = OperatingSystem.IsWindows() ? @"C:\Windows\Temp\pwned" : "/tmp/pwned";
        string package = CreatePackageWithManifestId(absolute);
        try
        {
            Assert.Throws<ArgumentException>(() =>
                FryPluginPackageLoader.UnpackAndLoad(package, pluginsRoot));
            Assert.False(Directory.Exists(absolute));
        }
        finally
        {
            if (File.Exists(package)) File.Delete(package);
            if (Directory.Exists(pluginsRoot)) Directory.Delete(pluginsRoot, recursive: true);
        }
    }

    // ─── 2.3 The load context must actually be collectible ──────────────────

    [Fact]
    public void PluginLoadContext_IsCollectibleByDefault()
    {
        // AssemblyDependencyResolver requires a real managed assembly on disk.
        string dll = typeof(PluginSecurityTests).Assembly.Location;
        var alc = new CollectiblePluginLoadContext(dll);

        // Previously defaulted to false, which made PluginAssemblyPackage.Dispose a no-op:
        // it guards on IsCollectible before calling Unload().
        Assert.True(alc.IsCollectible);
    }

    // ─── 2.7 Staging must not copy the world (or itself) ────────────────────

    [Fact]
    public void LoadPluginAssembly_DoesNotRecurseStagingIntoItself()
    {
        // The test assembly's own folder contains a "plugins" subfolder, so the plugins
        // root lives *inside* the source directory. Staging used to copy the source tree
        // recursively, nesting plugins/.staging inside itself until the path was too long.
        string stagingRoot = Path.Combine(FryPdfPaths.PluginsDirectory, ".staging");
        var before = Directory.Exists(stagingRoot)
            ? Directory.EnumerateDirectories(stagingRoot).ToHashSet(StringComparer.OrdinalIgnoreCase)
            : new System.Collections.Generic.HashSet<string>(StringComparer.OrdinalIgnoreCase);

        string source = typeof(PluginSecurityTests).Assembly.Location;
        var package = PluginAssemblyLoader.LoadPluginAssembly(source);
        package.Dispose();

        Assert.True(Directory.Exists(stagingRoot), "Expected the load to stage the assembly.");

        // Only inspect folders this test created — earlier runs may have left debris behind.
        var created = Directory.EnumerateDirectories(stagingRoot).Where(d => !before.Contains(d)).ToList();
        Assert.NotEmpty(created);

        foreach (var dir in created)
        {
            Assert.False(Directory.Exists(Path.Combine(dir, "plugins")),
                $"Staging folder '{dir}' copied the plugins directory into itself.");
        }
    }

    [Fact]
    public void LoadPluginAssembly_DoesNotCopyUnrelatedSiblingsFromANonPluginFolder()
    {
        // Simulate a plugin DLL sitting loose in a folder full of unrelated files,
        // e.g. ~/Downloads. Only the assembly's own companions should be staged.
        string sourceDir = Path.Combine(Path.GetTempPath(), $"downloads_{Guid.NewGuid():N}");
        Directory.CreateDirectory(sourceDir);

        string dll = Path.Combine(sourceDir, "Loose.dll");
        File.Copy(typeof(PluginSecurityTests).Assembly.Location, dll);

        var unrelated = Path.Combine(sourceDir, "holiday-photos");
        Directory.CreateDirectory(unrelated);
        File.WriteAllText(Path.Combine(unrelated, "beach.jpg"), new string('x', 4096));
        File.WriteAllText(Path.Combine(sourceDir, "tax-return.pdf"), new string('y', 4096));

        try
        {
            PluginAssemblyPackage? package = null;
            try
            {
                package = PluginAssemblyLoader.LoadPluginAssembly(dll);
            }
            catch
            {
                // Loading a renamed assembly may fail; staging still ran, which is what we assert.
            }
            finally
            {
                package?.Dispose();
            }

            string stagingRoot = Path.Combine(FryPdfPaths.PluginsDirectory, ".staging");
            var staged = Directory.Exists(stagingRoot)
                ? Directory.EnumerateDirectories(stagingRoot, "Loose_*").ToList()
                : new System.Collections.Generic.List<string>();
            Assert.NotEmpty(staged);

            foreach (var dir in staged)
            {
                Assert.False(File.Exists(Path.Combine(dir, "tax-return.pdf")),
                    "Staging copied an unrelated sibling file.");
                Assert.False(Directory.Exists(Path.Combine(dir, "holiday-photos")),
                    "Staging copied an unrelated sibling directory.");
            }
        }
        finally
        {
            if (Directory.Exists(sourceDir)) Directory.Delete(sourceDir, recursive: true);
        }
    }

    [Fact]
    public void LoadPluginAssembly_RegistersThenForgetsThePackage()
    {
        // Load this test assembly itself: a real, loadable managed assembly that
        // contains no IFryPlugin implementations.
        string source = typeof(PluginSecurityTests).Assembly.Location;
        Assert.False(string.IsNullOrEmpty(source));

        var package = PluginAssemblyLoader.LoadPluginAssembly(source);
        try
        {
            Assert.Contains(PluginAssemblyLoader.ActivePackages, p => ReferenceEquals(p, package));
        }
        finally
        {
            package.Dispose();
        }

        // Dispose must drop the static root, otherwise the ALC can never be collected
        // no matter what Unload() does.
        Assert.DoesNotContain(PluginAssemblyLoader.ActivePackages, p => ReferenceEquals(p, package));
    }
}
