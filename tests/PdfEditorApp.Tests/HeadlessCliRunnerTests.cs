using System.Threading.Tasks;
using PdfEditorApp.Plugins.Cli;
using Xunit;

namespace PdfEditorApp.Tests;

public class HeadlessCliRunnerTests
{
    [Theory]
    [InlineData("--help", true)]
    [InlineData("-h", true)]
    [InlineData("--version", true)]
    [InlineData("-v", true)]
    [InlineData("--tool", true)]
    [InlineData("-t", true)]
    [InlineData("--list-tools", true)]
    [InlineData("--list-plugins", true)]
    [InlineData("--profile", true)]
    [InlineData("somefile.pdf", false)]
    [InlineData("", false)]
    public void IsCliInvocation_DetectsFlagsCorrectly(string flag, bool expected)
    {
        var args = string.IsNullOrEmpty(flag) ? System.Array.Empty<string>() : new[] { flag };
        var actual = HeadlessCliRunner.IsCliInvocation(args);
        Assert.Equal(expected, actual);
    }

    [Fact]
    public async Task RunCliAsync_HelpFlag_ReturnsZero()
    {
        var exitCode = await HeadlessCliRunner.RunCliAsync(new[] { "--help" });
        Assert.Equal(0, exitCode);
    }

    [Fact]
    public async Task RunCliAsync_VersionFlag_ReturnsZero()
    {
        var exitCode = await HeadlessCliRunner.RunCliAsync(new[] { "--version" });
        Assert.Equal(0, exitCode);
    }
}
