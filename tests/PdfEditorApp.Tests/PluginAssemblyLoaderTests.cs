using System;
using System.IO;
using PdfEditorApp.Plugins.Loader;
using Xunit;

namespace PdfEditorApp.Tests;

public class PluginAssemblyLoaderTests
{
    [Fact]
    public void LoadPluginAssembly_ThrowsWhenFileNotFound()
    {
        var nonExistentPath = Path.Combine(Path.GetTempPath(), $"missing_{Guid.NewGuid():N}.dll");
        Assert.Throws<FileNotFoundException>(() => PluginAssemblyLoader.LoadPluginAssembly(nonExistentPath));
    }

    [Fact]
    public void DiscoverAndLoadDirectory_HandlesNonExistentDirectoryGracefully()
    {
        var nonExistentDir = Path.Combine(Path.GetTempPath(), $"dir_{Guid.NewGuid():N}");
        var results = PluginAssemblyLoader.DiscoverAndLoadDirectory(nonExistentDir);
        Assert.Empty(results);
    }

    [Fact]
    public void DiscoverAndLoadDirectory_HandlesEmptyDirectory()
    {
        var tempDir = Path.Combine(Path.GetTempPath(), $"plugins_{Guid.NewGuid():N}");
        Directory.CreateDirectory(tempDir);
        try
        {
            var results = PluginAssemblyLoader.DiscoverAndLoadDirectory(tempDir);
            Assert.Empty(results);
        }
        finally
        {
            if (Directory.Exists(tempDir))
            {
                Directory.Delete(tempDir, recursive: true);
            }
        }
    }
}
