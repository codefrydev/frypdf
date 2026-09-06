using System;
using System.IO;
using Xunit;

namespace PdfEditorApp.Tests;

public class Material3ExpressiveThemeTests
{
    private readonly string _projectRoot;

    public Material3ExpressiveThemeTests()
    {
        var currentDir = AppContext.BaseDirectory;
        string? projectRoot = null;
        var dir = new DirectoryInfo(currentDir);
        while (dir != null)
        {
            if (File.Exists(Path.Combine(dir.FullName, "FryPDF.sln")) || Directory.Exists(Path.Combine(dir.FullName, "src", "PdfEditorApp")))
            {
                projectRoot = dir.FullName;
                break;
            }
            dir = dir.Parent;
        }

        Assert.NotNull(projectRoot);
        _projectRoot = projectRoot!;
    }

    [Fact]
    public void AppAxaml_IncludesMaterial3ExpressiveTokensAndStyles()
    {
        var appAxamlPath = Path.Combine(_projectRoot, "src", "PdfEditorApp", "App.axaml");
        Assert.True(File.Exists(appAxamlPath), "App.axaml must exist");
        var appAxaml = File.ReadAllText(appAxamlPath);

        Assert.Contains("Material3ExpressiveTokens.axaml", appAxaml);
        Assert.Contains("Material3ExpressiveStyles.axaml", appAxaml);
    }

    [Fact]
    public void Material3Tokens_DefinesCompleteM3ShapeScale()
    {
        var tokensPath = Path.Combine(_projectRoot, "src", "PdfEditorApp", "Styles", "Material3ExpressiveTokens.axaml");
        Assert.True(File.Exists(tokensPath), "Material3ExpressiveTokens.axaml must exist");
        var tokens = File.ReadAllText(tokensPath);

        // M3 Expressive shape scale tokens
        Assert.Contains("CornerRadius x:Key=\"M3ShapeCornerNone\">0</CornerRadius>", tokens);
        Assert.Contains("CornerRadius x:Key=\"M3ShapeCornerExtraSmall\">4</CornerRadius>", tokens);
        Assert.Contains("CornerRadius x:Key=\"M3ShapeCornerSmall\">8</CornerRadius>", tokens);
        Assert.Contains("CornerRadius x:Key=\"M3ShapeCornerMedium\">12</CornerRadius>", tokens);
        Assert.Contains("CornerRadius x:Key=\"M3ShapeCornerLarge\">16</CornerRadius>", tokens);
        Assert.Contains("CornerRadius x:Key=\"M3ShapeCornerExtraLarge\">28</CornerRadius>", tokens);
        Assert.Contains("CornerRadius x:Key=\"M3ShapeCornerFull\">9999</CornerRadius>", tokens);
    }

    [Fact]
    public void Material3Tokens_DefinesElevationLevelsAndChubbySliderMetrics()
    {
        var tokensPath = Path.Combine(_projectRoot, "src", "PdfEditorApp", "Styles", "Material3ExpressiveTokens.axaml");
        var tokens = File.ReadAllText(tokensPath);

        // M3 Elevation shadows
        Assert.Contains("BoxShadows x:Key=\"M3ElevationLevel0\"", tokens);
        Assert.Contains("BoxShadows x:Key=\"M3ElevationLevel1\"", tokens);
        Assert.Contains("BoxShadows x:Key=\"M3ElevationLevel2\"", tokens);
        Assert.Contains("BoxShadows x:Key=\"M3ElevationLevel3\"", tokens);
        Assert.Contains("BoxShadows x:Key=\"M3ElevationLevel4\"", tokens);
        Assert.Contains("BoxShadows x:Key=\"M3ElevationLevel5\"", tokens);

        // Chubby tactile slider tokens
        Assert.Contains("SliderTrackThemeHeight", tokens);
        Assert.Contains("SliderHorizontalThumbWidth", tokens);
        Assert.Contains("SliderThumbTactileRadius", tokens);
    }

    [Fact]
    public void Material3Tokens_DefinesLightAndDarkExpressivePalettesWithBackwardCompatibleAliases()
    {
        var tokensPath = Path.Combine(_projectRoot, "src", "PdfEditorApp", "Styles", "Material3ExpressiveTokens.axaml");
        var tokens = File.ReadAllText(tokensPath);

        // M3 Core Color Roles
        Assert.Contains("M3PrimaryBrush", tokens);
        Assert.Contains("M3OnPrimaryBrush", tokens);
        Assert.Contains("M3PrimaryContainerBrush", tokens);
        Assert.Contains("M3OnPrimaryContainerBrush", tokens);
        Assert.Contains("M3SecondaryBrush", tokens);
        Assert.Contains("M3SecondaryContainerBrush", tokens);
        Assert.Contains("M3TertiaryBrush", tokens);
        Assert.Contains("M3SurfaceBrush", tokens);
        Assert.Contains("M3SurfaceContainerBrush", tokens);
        Assert.Contains("M3SurfaceContainerHighBrush", tokens);
        Assert.Contains("M3OutlineBrush", tokens);
        Assert.Contains("M3OutlineVariantBrush", tokens);

        // Backward compatibility brush aliases for existing FluentOffice views
        Assert.Contains("WinBgBrush", tokens);
        Assert.Contains("WinPanelBrush", tokens);
        Assert.Contains("WinBorderBrush", tokens);
        Assert.Contains("WinAccentBrush", tokens);
        Assert.Contains("WinTextBrush", tokens);
        Assert.Contains("WinMutedBrush", tokens);
        Assert.Contains("WinHoverBrush", tokens);
        Assert.Contains("WinActiveBrush", tokens);
        Assert.Contains("WinInputBgBrush", tokens);
    }

    [Fact]
    public void Material3Styles_DefinesExpressiveComponentClasses()
    {
        var stylesPath = Path.Combine(_projectRoot, "src", "PdfEditorApp", "Styles", "Material3ExpressiveStyles.axaml");
        Assert.True(File.Exists(stylesPath), "Material3ExpressiveStyles.axaml must exist");
        var styles = File.ReadAllText(stylesPath);

        // M3 Expressive Buttons
        Assert.Contains("Button.m3-filled-btn", styles);
        Assert.Contains("Button.m3-tonal-btn", styles);
        Assert.Contains("Button.m3-elevated-btn", styles);
        Assert.Contains("Button.m3-outlined-btn", styles);
        Assert.Contains("Button.m3-text-btn", styles);
        Assert.Contains("Button.m3-fab", styles);
        Assert.Contains("Button.m3-fab-extended", styles);
        Assert.Contains("Button.m3-icon-btn", styles);

        // M3 Segmented Capsules & Chips
        Assert.Contains("Border.m3-segmented-container", styles);
        Assert.Contains("RadioButton.m3-segment-btn", styles);
        Assert.Contains("Button.m3-chip", styles);
        Assert.Contains("ToggleButton.m3-filter-chip", styles);

        // M3 Expressive Cards & Containers
        Assert.Contains("Border.m3-card-elevated", styles);
        Assert.Contains("Border.m3-card-filled", styles);
        Assert.Contains("Border.m3-card-outlined", styles);
        Assert.Contains("Border.m3-dialog-card", styles);
        Assert.Contains("Border.m3-floating-hud", styles);

        // M3 Text Inputs
        Assert.Contains("TextBox.m3-outlined", styles);
        Assert.Contains("TextBox.m3-search", styles);
    }

    [Fact]
    public void Material3Styles_DefinesCardActionButtonWrapperStyles_SuppressingPointerOverPresenterBackground()
    {
        var stylesPath = Path.Combine(_projectRoot, "src", "PdfEditorApp", "Styles", "Material3ExpressiveStyles.axaml");
        var styles = File.ReadAllText(stylesPath);

        // Verify button wrapper selectors exist
        Assert.Contains("Button.card-action-wrapper", styles);
        Assert.Contains("Button.pdf-tool-btn", styles);
        Assert.Contains("Button.template-card-btn", styles);
        Assert.Contains("Button.workflow-banner-btn", styles);
        Assert.Contains("Button.card-btn", styles);

        // Verify template pointerover and pressed content presenter backgrounds are suppressed
        Assert.Contains(":pointerover /template/ ContentPresenter#PART_ContentPresenter", styles);
        Assert.Contains(":pressed /template/ ContentPresenter#PART_ContentPresenter", styles);

        // Verify ClipToBounds is disabled to prevent upward hover translation clipping
        Assert.Contains("Property=\"ClipToBounds\" Value=\"False\"", styles);

        // Verify vertical headroom padding is provided on button wrappers to absorb translateY(-2px)
        Assert.Contains("Property=\"Padding\" Value=\"0,3,0,1\"", styles);
    }

    [Fact]
    public void Views_FollowMaterial3ExpressiveCardHierarchy_WithoutStiffHexShadows()
    {
        var toolsStudioPath = Path.Combine(_projectRoot, "src", "PdfEditorApp", "Views", "PdfToolsStudioView.axaml");
        var starredPath = Path.Combine(_projectRoot, "src", "PdfEditorApp", "Views", "StarredToolsPageView.axaml");

        var toolsStudio = File.ReadAllText(toolsStudioPath);
        var starred = File.ReadAllText(starredPath);

        // Assert M3 elevation and corner tokens are used
        Assert.Contains("StaticResource M3ElevationLevel1", toolsStudio);
        Assert.Contains("StaticResource M3ElevationLevel2", toolsStudio);
        Assert.Contains("StaticResource M3ShapeCornerLarge", toolsStudio);

        Assert.Contains("StaticResource M3ElevationLevel1", starred);
        Assert.Contains("StaticResource M3ElevationLevel2", starred);
        Assert.Contains("StaticResource M3ShapeCornerLarge", starred);

        // Assert old stiff/hardcoded shadows and radii are removed
        Assert.DoesNotContain("0 2 6 #08000000", toolsStudio);
        Assert.DoesNotContain("0 8 20 #150F6CBD", toolsStudio);
        Assert.DoesNotContain("CornerRadius=\"10\"", toolsStudio);

        Assert.DoesNotContain("0 2 6 #08000000", starred);
        Assert.DoesNotContain("0 8 20 #150F6CBD", starred);
        Assert.DoesNotContain("CornerRadius=\"10\"", starred);
    }
}
