using System;
using System.Globalization;
using Material.Icons;
using PdfEditorApp.Converters;
using PdfEditorApp.Services;
using PdfEditorApp.ViewModels;
using Xunit;

namespace PdfEditorApp.Tests;

public class IconValidationTests
{
    [Fact]
    public void SafeMaterialIconKindConverter_ParsesValidIconName()
    {
        var converter = SafeMaterialIconKindConverter.Instance;
        var result = converter.Convert("FilePdfBox", typeof(MaterialIconKind), null, CultureInfo.InvariantCulture);

        Assert.Equal(MaterialIconKind.FilePdfBox, result);
    }

    [Fact]
    public void SafeMaterialIconKindConverter_MapsLegacyStampAliasToCertificateOutline()
    {
        var converter = SafeMaterialIconKindConverter.Instance;
        var result = converter.Convert("Stamp", typeof(MaterialIconKind), null, CultureInfo.InvariantCulture);

        Assert.Equal(MaterialIconKind.CertificateOutline, result);
    }

    [Fact]
    public void SafeMaterialIconKindConverter_FallsBackOnUnknownIconName()
    {
        var converter = SafeMaterialIconKindConverter.Instance;
        var result = converter.Convert("NonExistentIconXYZ123", typeof(MaterialIconKind), null, CultureInfo.InvariantCulture);

        Assert.Equal(MaterialIconKind.FileDocumentOutline, result);
    }

    [Fact]
    public void SafeMaterialIconKindConverter_PassesThroughDirectMaterialIconKind()
    {
        var converter = SafeMaterialIconKindConverter.Instance;
        var result = converter.Convert(MaterialIconKind.Account, typeof(MaterialIconKind), null, CultureInfo.InvariantCulture);

        Assert.Equal(MaterialIconKind.Account, result);
    }

    [Fact]
    public void SafeMaterialIconKindConverter_HandlesNullAndEmptyStrings()
    {
        var converter = SafeMaterialIconKindConverter.Instance;
        var resultNull = converter.Convert(null, typeof(MaterialIconKind), null, CultureInfo.InvariantCulture);
        var resultEmpty = converter.Convert(string.Empty, typeof(MaterialIconKind), null, CultureInfo.InvariantCulture);

        Assert.Equal(MaterialIconKind.FileDocumentOutline, resultNull);
        Assert.Equal(MaterialIconKind.FileDocumentOutline, resultEmpty);
    }

    [Fact]
    public void AllHelpGuides_HaveValidMaterialIcons()
    {
        var service = new HelpGuideService();
        var guides = service.GetAllGuides();

        foreach (var guide in guides)
        {
            var valid = Enum.TryParse<MaterialIconKind>(guide.IconKind, ignoreCase: true, out var kind);
            Assert.True(valid, $"Guide '{guide.Id}' has invalid IconKind '{guide.IconKind}'");
        }
    }

    [Fact]
    public void AllToolsInRegistry_HaveValidOrConvertibleMaterialIcons()
    {
        var registry = new PdfEditorApp.Services.Tools.Core.PdfToolRegistry();
        var tools = registry.GetAllTools();

        foreach (var tool in tools)
        {
            var valid = Enum.TryParse<MaterialIconKind>(tool.IconKind, ignoreCase: true, out _);
            Assert.True(valid, $"Tool '{tool.Id}' has invalid IconKind '{tool.IconKind}'");
        }
    }
}
