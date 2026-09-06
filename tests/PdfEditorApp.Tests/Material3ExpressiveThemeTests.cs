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
        Assert.Contains("BoxShadows x:Key=\"M3ElevationModalDialog\"", tokens);

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
        Assert.Contains("Button.m3-preset-chip", styles);
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

    [Fact]
    public void InspectorSidebarView_TypographyStudioCard_AdheresToMaterial3Expressive()
    {
        var inspectorPath = Path.Combine(_projectRoot, "src", "PdfEditorApp", "Views", "InspectorSidebarView.axaml");
        var inspector = File.ReadAllText(inspectorPath);

        // Verify M3 card container properties on Typography Studio
        Assert.Contains("Classes=\"m3-preset-chip\"", inspector);
        Assert.Contains("M3SurfaceContainerLowestBrush", inspector);
        Assert.Contains("M3PrimaryContainerBrush", inspector);
        Assert.Contains("M3OutlineVariantBrush", inspector);

        // Verify presets use proper vector MaterialIcons rather than emojis/unicode
        Assert.Contains("Kind=\"Waves\"", inspector);
        Assert.Contains("Kind=\"VectorCurve\"", inspector);
        Assert.Contains("Kind=\"Arch\"", inspector);
        Assert.Contains("Kind=\"TrendingDown\"", inspector);
        Assert.Contains("Kind=\"TrendingUp\"", inspector);
        Assert.Contains("Kind=\"ArrowUpBoldOutline\"", inspector);
        Assert.Contains("Kind=\"ArrowDownBoldOutline\"", inspector);
        Assert.Contains("Kind=\"DecagramOutline\"", inspector);
        Assert.Contains("Kind=\"CircleHalfFull\"", inspector);
        Assert.Contains("Kind=\"BorderColor\"", inspector);
        Assert.Contains("Kind=\"LayersOutline\"", inspector);
        Assert.Contains("Kind=\"Creation\"", inspector);
        Assert.Contains("Kind=\"FormatLetterCase\"", inspector);

        // Verify old typography preset buttons with emojis and math unicode symbols are removed
        Assert.DoesNotContain("CommandParameter=\"wave\"><TextBlock Text=\"〰 Wave\"", inspector);
        Assert.DoesNotContain("CommandParameter=\"scurve\"><TextBlock Text=\"∿ S-Curve\"", inspector);
        Assert.DoesNotContain("CommandParameter=\"bridge\"><TextBlock Text=\"⌢ Bridge\"", inspector);
        Assert.DoesNotContain("CommandParameter=\"valley\"><TextBlock Text=\"⌣ Valley\"", inspector);
        Assert.DoesNotContain("CommandParameter=\"rise\"><TextBlock Text=\"↗ Rise\"", inspector);
        Assert.DoesNotContain("CommandParameter=\"archup\"><TextBlock Text=\"⤴ Arch Up\"", inspector);
        Assert.DoesNotContain("CommandParameter=\"archdown\"><TextBlock Text=\"⤵ Arch Down\"", inspector);
        Assert.DoesNotContain("CommandParameter=\"circlebadge\"><TextBlock Text=\"◯ Badge\"", inspector);
        Assert.DoesNotContain("CommandParameter=\"toparc\"><TextBlock Text=\"⌒ Top Arc\"", inspector);
        Assert.DoesNotContain("CommandParameter=\"bottomarc\"><TextBlock Text=\"ᴗ Bottom Arc\"", inspector);
        Assert.DoesNotContain("CommandParameter=\"neonglow\"><TextBlock Text=\"✨ Neon Glow\"", inspector);
    }

    [Fact]
    public void InspectorSidebarView_AllCardsAndElements_AdhereToMaterial3Expressive()
    {
        var inspectorPath = Path.Combine(_projectRoot, "src", "PdfEditorApp", "Views", "InspectorSidebarView.axaml");
        var inspector = File.ReadAllText(inspectorPath);

        // 1. Strict elimination of legacy Win brushes in element bindings
        Assert.DoesNotContain("{DynamicResource WinBgBrush}", inspector);
        Assert.DoesNotContain("{DynamicResource WinPanelBrush}", inspector);
        Assert.DoesNotContain("{DynamicResource WinBorderBrush}", inspector);
        Assert.DoesNotContain("{DynamicResource WinAccentBrush}", inspector);
        Assert.DoesNotContain("{DynamicResource WinAccentLightBrush}", inspector);
        Assert.DoesNotContain("{DynamicResource WinAccentBorderBrush}", inspector);
        Assert.DoesNotContain("{DynamicResource WinTextBrush}", inspector);
        Assert.DoesNotContain("{DynamicResource WinMutedBrush}", inspector);
        Assert.DoesNotContain("{DynamicResource WinHoverBrush}", inspector);
        Assert.DoesNotContain("{DynamicResource WinActiveBrush}", inspector);
        Assert.DoesNotContain("{DynamicResource WinInputBgBrush}", inspector);
        Assert.DoesNotContain("{DynamicResource WinDangerBrush}", inspector);
        Assert.DoesNotContain("{StaticResource WinSubtleBgBrush}", inspector);

        // 2. Strict elimination of raw hex box shadows and rigid corner radii
        Assert.DoesNotContain("#30000000", inspector);
        Assert.DoesNotContain("CornerRadius=\"8\"", inspector);
        Assert.DoesNotContain("CornerRadius=\"4\"", inspector);
        Assert.DoesNotContain("CornerRadius=\"6\"", inspector);
        Assert.DoesNotContain("CornerRadius=\"10\"", inspector);

        // 3. Proper utilization of M3 shape tokens and elevation levels
        Assert.Contains("{StaticResource M3ShapeCornerLarge}", inspector);
        Assert.Contains("{StaticResource M3ShapeCornerFull}", inspector);
        Assert.Contains("{StaticResource M3ShapeCornerMedium}", inspector);
        Assert.Contains("{StaticResource M3ShapeCornerSmall}", inspector);
        Assert.Contains("{StaticResource M3ElevationLevel1}", inspector);
        Assert.Contains("{StaticResource M3ElevationLevel3}", inspector);

        // 4. Modern M3 component classes across the inspector
        Assert.Contains("Classes=\"m3-outlined\"", inspector);
        Assert.Contains("Classes=\"m3-preset-chip\"", inspector);
        Assert.Contains("Classes=\"m3-segmented-container\"", inspector);
        Assert.Contains("Classes=\"m3-segment-btn\"", inspector);
        Assert.Contains("Classes=\"m3-tonal-btn\"", inspector);
        Assert.Contains("Classes=\"m3-icon-btn\"", inspector);
        Assert.Contains("Classes=\"m3-filled-btn\"", inspector);

        // 5. Verification of key specialized cards
        // Mathematical Equation & Formula
        Assert.Contains("LaTeX Formatting • MathFX Engine", inspector);
        Assert.Contains("Kind=\"Sigma\"", inspector);
        // Position & Geometry
        Assert.Contains("Coordinates • Transforms • Hierarchy", inspector);
        Assert.Contains("Kind=\"ArrowAll\"", inspector);
        // QR Code Studio
        Assert.Contains("Kind=\"Qrcode\"", inspector);
        // LiveCharts2 Engine
        Assert.Contains("Kind=\"ChartBar\"", inspector);
        // Table Styles & Grid
        Assert.Contains("Kind=\"TableHeadersEye\"", inspector);
        // AcroForm Field Setup
        Assert.Contains("Kind=\"FormTextbox\"", inspector);
        // Legal Redaction Exemption
        Assert.Contains("Kind=\"EyeOffOutline\"", inspector);
        // Ink & Markup
        Assert.Contains("Kind=\"DrawPen\"", inspector);
        // Measurement & Scale Annotation
        Assert.Contains("Kind=\"RulerSquare\"", inspector);
        // SVG Vector Art Studio
        Assert.Contains("Kind=\"VectorCurve\"", inspector);
    }

    [Fact]
    public void Material3ExpressiveStyles_DefinesNumericUpDownAndButtonSpinnerThemes()
    {
        var stylesPath = Path.Combine(_projectRoot, "src", "PdfEditorApp", "Styles", "Material3ExpressiveStyles.axaml");
        var styles = File.ReadAllText(stylesPath);

        // 1. Authoritative M3 ControlThemes for ButtonSpinner & NumericUpDown
        Assert.Contains("ControlTheme x:Key=\"{x:Type ButtonSpinner}\" TargetType=\"ButtonSpinner\"", styles);
        Assert.Contains("ControlTheme x:Key=\"{x:Type NumericUpDown}\" TargetType=\"NumericUpDown\"", styles);
        Assert.Contains("ControlTheme x:Key=\"M3ButtonSpinnerRepeatButton\" TargetType=\"RepeatButton\"", styles);

        // 2. Compact vertical chevrons layout (Width="18" with RowDefinitions="*,*")
        Assert.Contains("Width=\"18\"", styles);
        Assert.Contains("RowDefinitions=\"*,*\"", styles);
        Assert.Contains("RepeatButton Name=\"PART_IncreaseButton\"", styles);
        Assert.Contains("RepeatButton Name=\"PART_DecreaseButton\"", styles);

        // 3. Seamless transparent TextBox embedding with zero text clipping
        Assert.Contains("NumericUpDown /template/ TextBox#PART_TextBox", styles);
        Assert.Contains("NumericUpDown /template/ TextBox#PART_TextBox /template/ Border#PART_BorderElement", styles);

        // 4. Verification that InspectorSidebarView Highlight / Background Box has non-clipping Left alignment
        var inspectorPath = Path.Combine(_projectRoot, "src", "PdfEditorApp", "Views", "InspectorSidebarView.axaml");
        var inspector = File.ReadAllText(inspectorPath);
        Assert.Contains("TextElement.Padding", inspector);
        Assert.Contains("TextElement.CornerRadius", inspector);
        Assert.Contains("TextElement.BorderThickness", inspector);
        Assert.Contains("ToolTip.Tip=\"Internal Box Padding (0-64 px)\"", inspector);
        Assert.Contains("ToolTip.Tip=\"Box Corner Radius (0-64 px)\"", inspector);
        Assert.Contains("ToolTip.Tip=\"Border Stroke Thickness (0-20 pt)\"", inspector);
    }

    [Fact]
    public void InspectorSidebarView_TextContentEditor_IsVerticallyResizableWithDragGripAndExpandToggle()
    {
        var inspectorPath = Path.Combine(_projectRoot, "src", "PdfEditorApp", "Views", "InspectorSidebarView.axaml");
        var inspector = File.ReadAllText(inspectorPath);

        // 1. Resizable editor container with M3 styling
        Assert.Contains("Classes=\"m3-resizable-editor\"", inspector);
        Assert.Contains("Classes=\"m3-resize-grip\"", inspector);
        Assert.Contains("Classes=\"m3-grip-pill\"", inspector);

        // 2. Interactive drag event bindings and tooltip
        Assert.Contains("PointerPressed=\"OnTextEditorResizeGripPointerPressed\"", inspector);
        Assert.Contains("PointerMoved=\"OnTextEditorResizeGripPointerMoved\"", inspector);
        Assert.Contains("PointerReleased=\"OnTextEditorResizeGripPointerReleased\"", inspector);
        Assert.Contains("DoubleTapped=\"OnTextEditorResizeGripDoubleTapped\"", inspector);

        // 3. Quick toggle expand/collapse button in header
        Assert.Contains("Click=\"OnToggleTextEditorExpandClicked\"", inspector);
        Assert.Contains("Name=\"TextEditorExpandIcon\"", inspector);

        // 4. TextBox dynamic height bounds and scrollbar support
        Assert.Contains("Name=\"SidebarTextEditor\"", inspector);
        Assert.Contains("MinHeight=\"44\"", inspector);
        Assert.Contains("MaxHeight=\"450\"", inspector);
        Assert.Contains("ScrollViewer.VerticalScrollBarVisibility=\"Auto\"", inspector);
    }

    [Fact]
    public void RibbonView_ToolsPanel_HasSufficientHeightAndNonClippingScrollBar()
    {
        var ribbonPath = Path.Combine(_projectRoot, "src", "PdfEditorApp", "Views", "RibbonView.axaml");
        var ribbon = File.ReadAllText(ribbonPath);

        // 1. Tool panel height is >= 112px (114px) to ensure group titles are never clipped by scrollbars
        Assert.Contains("Height=\"114\"", ribbon);

        // 2. Uses sleek ribbon-scroll-viewer class
        Assert.Contains("Classes=\"ribbon-scroll-viewer\"", ribbon);

        // 3. FluentOfficeStyles defines sleek M3 horizontal scrollbar for ribbon
        var stylesPath = Path.Combine(_projectRoot, "src", "PdfEditorApp", "Styles", "FluentOfficeStyles.axaml");
        var styles = File.ReadAllText(stylesPath);
        Assert.Contains("ScrollViewer.ribbon-scroll-viewer /template/ ScrollBar:horizontal", styles);
    }

    [Fact]
    public void InspectorSidebarView_ExpandableSectionHeaders_UseM3SectionToggleWithoutWidthClipping()
    {
        // 1. Material3ExpressiveStyles defines m3-section-toggle with stretch alignment and no fixed width
        var m3StylesPath = Path.Combine(_projectRoot, "src", "PdfEditorApp", "Styles", "Material3ExpressiveStyles.axaml");
        var m3Styles = File.ReadAllText(m3StylesPath);
        Assert.Contains("Style Selector=\"Button.m3-section-toggle\"", m3Styles);
        Assert.Contains("Property=\"HorizontalAlignment\" Value=\"Stretch\"", m3Styles);
        Assert.Contains("Property=\"HorizontalContentAlignment\" Value=\"Stretch\"", m3Styles);

        // 2. InspectorSidebarView uses m3-section-toggle for expandable headers
        var inspectorPath = Path.Combine(_projectRoot, "src", "PdfEditorApp", "Views", "InspectorSidebarView.axaml");
        var inspector = File.ReadAllText(inspectorPath);
        Assert.Contains("Button Classes=\"m3-section-toggle\"", inspector);
        Assert.DoesNotContain("Button Classes=\"mini-tool-btn\" HorizontalAlignment=\"Stretch\"", inspector);
    }

    [Fact]
    public void InspectorSidebarView_LayerOrderAndQuickActions_UsesSegmentedCapsuleAndProportionalButtons()
    {
        var inspectorPath = Path.Combine(_projectRoot, "src", "PdfEditorApp", "Views", "InspectorSidebarView.axaml");
        var inspector = File.ReadAllText(inspectorPath);

        // 1. Layer order is contained within an M3 segmented container capsule with 4 segment buttons
        Assert.Contains("Border Grid.Column=\"0\" Classes=\"m3-segmented-container\" HorizontalAlignment=\"Stretch\" Padding=\"2\"", inspector);
        Assert.Contains("Command=\"{Binding BringToFrontCommand}\"", inspector);
        Assert.Contains("Command=\"{Binding BringForwardCommand}\"", inspector);
        Assert.Contains("Command=\"{Binding SendBackwardCommand}\"", inspector);
        Assert.Contains("Command=\"{Binding SendToBackCommand}\"", inspector);

        // 2. Quick Actions are proportional 32x32 circular buttons avoiding vertical oval distortion
        Assert.Contains("Button Classes=\"m3-tonal-btn\" Width=\"32\" Height=\"32\" Padding=\"0\"", inspector);
        Assert.Contains("Button Classes=\"danger-btn\" Width=\"32\" Height=\"32\" Padding=\"0\"", inspector);
        Assert.Contains("Command=\"{Binding DuplicateSelectedElementCommand}\"", inspector);
        Assert.Contains("Command=\"{Binding DeleteSelectedElementCommand}\"", inspector);

        // 3. The old distorted 6-column unsegmented grid row is completely eliminated
        Assert.DoesNotContain("<Grid ColumnDefinitions=\"*,*,*,*,*,*\">\n                                    <Button Grid.Column=\"0\" Classes=\"m3-tonal-btn\"", inspector);
    }

    [Fact]
    public void MainWindow_TopTitleBar_UsesM3HeaderButtonsWithoutTextClipping()
    {
        // 1. Material3ExpressiveStyles defines m3-header-btn with natural width and hover transitions
        var m3StylesPath = Path.Combine(_projectRoot, "src", "PdfEditorApp", "Styles", "Material3ExpressiveStyles.axaml");
        var m3Styles = File.ReadAllText(m3StylesPath);
        Assert.Contains("Style Selector=\"Button.m3-header-btn\"", m3Styles);

        // 2. FluentOfficeStyles mini-tool-btn uses MinWidth="28" instead of hardcoded Width="28"
        var fluentStylesPath = Path.Combine(_projectRoot, "src", "PdfEditorApp", "Styles", "FluentOfficeStyles.axaml");
        var fluentStyles = File.ReadAllText(fluentStylesPath);
        Assert.Contains("<Setter Property=\"MinWidth\" Value=\"28\" />", fluentStyles);

        // 3. MainWindow brand button and document title use m3-header-btn
        var mainPath = Path.Combine(_projectRoot, "src", "PdfEditorApp", "Views", "MainWindow.axaml");
        var main = File.ReadAllText(mainPath);
        Assert.Contains("Button Classes=\"m3-header-btn\"", main);
        Assert.Contains("TextBlock Text=\"FryPDF\"", main);
        Assert.Contains("TextBlock Text=\"by CodeFryDev\"", main);
        Assert.Contains("TextBlock Text=\"{Binding DocumentTitle}\"", main);

        // 4. PdfViewerView uses m3-header-btn for rename document title
        var viewerPath = Path.Combine(_projectRoot, "src", "PdfEditorApp", "Views", "PdfViewerView.axaml");
        var viewer = File.ReadAllText(viewerPath);
        Assert.Contains("Button Classes=\"m3-header-btn\"", viewer);
    }

    [Fact]
    public void PluginsDialog_AdheresToMaterial3Expressive()
    {
        var pluginsPath = Path.Combine(_projectRoot, "src", "PdfEditorApp", "Views", "Dialogs", "PluginsDialog.axaml");
        var xaml = File.ReadAllText(pluginsPath);

        // 1. Dialog Shell uses M3 Extra Large shape, dedicated M3ElevationModalDialog elevation, and M3 Surface brush
        Assert.Contains("CornerRadius=\"{StaticResource M3ShapeCornerExtraLarge}\"", xaml);
        Assert.Contains("BoxShadow=\"{StaticResource M3ElevationModalDialog}\"", xaml);
        Assert.Contains("Background=\"{DynamicResource M3SurfaceBrush}\"", xaml);

        // 2. Elimination of hardcoded dark navy hex colors and raw hex box shadows
        Assert.DoesNotContain("Background=\"#0F172A\"", xaml);
        Assert.DoesNotContain("Background=\"#1E1B4B\"", xaml);
        Assert.DoesNotContain("BorderBrush=\"#1E293B\"", xaml);
        Assert.DoesNotContain("BorderBrush=\"#818CF8\"", xaml);
        Assert.DoesNotContain("BoxShadow=\"0 28 80 #50000000\"", xaml);

        // 3. Header uses dynamic M3 tokens, tonal badge, and m3-icon-btn close
        Assert.Contains("Background=\"{DynamicResource M3SurfaceContainerLowBrush}\"", xaml);
        Assert.Contains("Background=\"{DynamicResource M3PrimaryContainerBrush}\"", xaml);
        Assert.Contains("Background=\"{DynamicResource M3SecondaryContainerBrush}\"", xaml);
        Assert.Contains("Button Grid.Column=\"2\"\n                                Classes=\"m3-icon-btn\"", xaml);

        // 4. Search bar uses m3-search and Profile selector uses M3 segmented container
        Assert.Contains("Classes=\"m3-search\"", xaml);
        Assert.Contains("Classes=\"m3-segmented-container\"", xaml);
        Assert.Contains("Button Classes=\"m3-segment-btn\"", xaml);

        // 5. Plugin cards use elevated M3 container, tonal icon container, and m3-tonal-btn
        Assert.Contains("Background=\"{DynamicResource M3SurfaceContainerLowestBrush}\"", xaml);
        Assert.Contains("BoxShadow=\"{StaticResource M3ElevationLevel1}\"", xaml);
        Assert.Contains("CornerRadius=\"{StaticResource M3ShapeCornerLarge}\"", xaml);
        Assert.Contains("Classes=\"m3-tonal-btn\"", xaml);

        // 6. Footer bar uses tonal background, m3-tonal-btn, and m3-filled-btn
        Assert.Contains("Classes=\"m3-filled-btn\"", xaml);
    }
}


