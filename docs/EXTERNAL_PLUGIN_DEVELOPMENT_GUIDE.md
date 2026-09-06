# Developing External Plugins for FryPDF

> **Authoritative Guide & Developer Manual**  
> Learn how to build, test, package, and distribute third-party plugins for **FryPDF** using **.NET 10, C# 13, and Avalonia UI (Material Design 3 Expressive)**.

---

## 1. Overview & Architectural Foundation

FryPDF is built upon an **"Everything is a Plugin"** modular microkernel architecture inspired by the **Cordis** framework. The core desktop shell privileges no single subsystem: tools, dynamic ribbon actions, editor sidebars, contextual property panels, floating overlays, AI engines, and file converters are all isolated plugins mounted into an active runtime context (`IFryPluginContext`).

### Why Build a FryPDF External Plugin?
- **Zero Monolithic Wiring**: You never need to fork or recompile FryPDF to introduce new features.
- **Isolated Collectible Loading**: Plugins execute inside dedicated `CollectiblePluginLoadContext` (`AssemblyLoadContext`) sandboxes. Plugins can be loaded, enabled, disabled, updated, and completely unloaded from memory without restarting the application.
- **Dynamic M3 Expressive Integration**: Your UI automatically integrates with Google Material Design 3 (M3) Expressive themes, color tokens, and elevation physics.
- **Auto-Generated Settings UI**: Declare typed configuration options in your code or manifest; FryPDF automatically generates an M3 Expressive settings dialog.
- **Single-File Distribution (`.fryplugin`)**: Distribute your plugin as a single `.fryplugin` package (ZIP archive containing `plugin.json`, DLLs, and resources) that users can install via drag-and-drop.

---

## 2. Prerequisites & Environment Setup

Before starting, ensure you have:
- **[.NET 10 SDK](https://dotnet.microsoft.com/download/dotnet/10.0)** (v10.0 or later)
- An IDE with modern C# 13+ support:
  - Visual Studio Code (with C# Dev Kit extension)
  - JetBrains Rider (2025.1+)
  - Visual Studio 2026 (v18+)

### Zero Source Code Requirement
> [!TIP]
> **You do NOT need to clone FryPDF's repository or download its source code to create plugins!**  
> External developers develop plugins independently against either:
> 1. **Their installed FryPDF application** (referencing the compiled `.dll` files from the install directory).
> 2. **The `FryPdf.PluginSdk` NuGet package** (`dotnet add package FryPdf.PluginSdk`).

### Referenced Host Contracts
External plugins compile against the following host contracts (provided by the installed application at runtime):
- **`PdfEditorApp.Core.dll`**: Core domain models, plugin contracts (`IFryPlugin`, `IFryPluginContext`), and extension descriptors.
- **`PdfEditorApp.dll`** *(optional, for UI plugins)*: Avalonia UI views, viewmodels, base classes (`ToolPluginBase`), and services.
- **`CommunityToolkit.Mvvm`**: Standard MVVM source generators (`[ObservableProperty]`, `[RelayCommand]`).
- **`Avalonia`** (12.x): Cross-platform UI controls and styling.

---

## 3. The Anatomy of an External Plugin

Every external plugin consists of:
1. **A .NET 10 Class Library** targeting `net10.0`.
2. **An Implementation of `IFryPlugin`** (or subclass of `ToolPluginBase`).
3. **Reversible Effects & Dynamic Registrations** mounted via `ApplyAsync`.
4. **A `plugin.json` Manifest File** describing metadata, dependencies, and settings.
5. **A `.fryplugin` Archive** bundle for distribution.

### 3.1 The `IFryPlugin` Interface
Every plugin must implement the `IFryPlugin` contract:

```csharp
using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using PdfEditorApp.Core.Plugins;
using PdfEditorApp.Core.Plugins.Manifests;

namespace MyCompany.FryPdf.Plugins;

public class MyCustomPlugin : IFryPlugin
{
    // Unique identifier (reverse-domain style: <vendor>.<category>.<name>)
    public string Id => "com.mycompany.frypdf.customtool";

    // User-friendly display name
    public string Name => "Custom PDF Tool";

    // Semantic version of the plugin
    public Version Version => new(1, 0, 0);

    // Declares required service contracts that must be registered before mounting
    public IReadOnlyList<Type> RequiredServices => Array.Empty<Type>();

    // Declares service contracts this plugin provides to other plugins
    public IReadOnlyList<Type> ProvidedServices => Array.Empty<Type>();

    // Declarative configuration schema for user-editable settings
    public IReadOnlyDictionary<string, PluginSettingDefinition>? SettingsSchema => null;

    // Called when the plugin is mounted into FryPDF
    public Task ApplyAsync(IFryPluginContext ctx, CancellationToken ct = default)
    {
        // Register tools, ribbon buttons, sidebars, pipelines, or services here
        return Task.CompletedTask;
    }
}
```

---

## 4. The 12 Dynamic Capability Registries

FryPDF provides 12 dynamic extension pillars accessible through `IFryPluginContext`. Never write hardcoded UI wiring—always register through these pillars:

| Pillar | Registry / Context Method | Typical Use Case |
| :--- | :--- | :--- |
| **1. PDF Tools** | `ctx.RegisterTool(PdfToolDescriptor)` | Standalone tools (Merge, Split, Rotate, Watermark, Compress, etc.). |
| **2. Dynamic Ribbon** | `ctx.RegisterRibbonAction(RibbonActionDescriptor)`<br/>`ctx.RegisterRibbonTab(RibbonTabDescriptor)`<br/>`ctx.RegisterRibbonGroup(RibbonGroupDescriptor)` | Action pill buttons, custom ribbon groups, or dedicated ribbon tabs. |
| **3. Extensible Sidebars** | `ctx.RegisterSidebarTab(SidebarTabDescriptor)` | Custom sidebar panels (e.g. Audit Logs, Bookmarks, AI Chat, Metadata). |
| **4. Contextual Inspector** | `ctx.RegisterInspectorSection(InspectorSectionDescriptor)` | Custom property editor panels targeting specific canvas elements. |
| **5. Canvas Elements** | `ctx.RegisterCanvasElement(CanvasElementDescriptor)` | Custom vector elements (Barcode, LaTeX Math, Stamps, Ink, Form Fields). |
| **6. Shell Overlays** | `ctx.RegisterOverlay(OverlayDescriptor)` | Floating, draggable, minimizable M3 utility cards (e.g. Scratchpad, Notes). |
| **7. Command Palette** | `ctx.RegisterCommand(CommandPaletteDescriptor)` | Searchable commands triggered via ⌘K / Ctrl+K with keyboard shortcuts. |
| **8. Status Bar Widgets** | `ctx.RegisterStatusBarWidget(StatusBarWidgetDescriptor)` | Footer status pills, counters, toggles, or telemetry badges. |
| **9. Document Importers** | `ctx.RegisterImporter(IDocumentImporter)` | Custom file format decoders (Markdown, DOCX, XLSX, HTML, EPUB). |
| **10. Document Exporters** | `ctx.RegisterExporter(IDocumentExporter)` | Custom file generators (PDF/A, multi-image sequences, CSV, HTML). |
| **11. OCR & AI Engines** | `ctx.RegisterOcrEngine(IOcrEngine)` | Platform-specific OCR engines (Apple Vision, Tesseract) or local AI models. |
| **12. Data Connectors** | `ctx.RegisterDataConnector(IDataConnector)` | Tabular data loaders (REST APIs, SQLite databases, JSON feeds). |

### Reversible Effects Rule (`ctx.RegisterEffect`)
Whenever your plugin registers event listeners, creates file hooks, or starts background workers, **you MUST track them using `ctx.RegisterEffect`**. When the plugin unloads, FryPDF unwinds these actions in reverse order (LIFO), guaranteeing zero memory leaks and zero dangling event listeners:

```csharp
public Task ApplyAsync(IFryPluginContext ctx, CancellationToken ct = default)
{
    // Subscribe to a message or event
    var token = WeakReferenceMessenger.Default.Register<DocumentChangedMessage>(this, HandleDocumentChanged);

    // Register rollback action for clean teardown
    ctx.RegisterEffect(() =>
    {
        WeakReferenceMessenger.Default.Unregister<DocumentChangedMessage>(this);
    });

    return Task.CompletedTask;
}
```

---

## 5. Step-by-Step Tutorial: Building a "Watermark Plus" Plugin

Let's build a real, fully functional external plugin: **`FryPdf.Plugin.WatermarkPlus`**.  
This plugin:
1. Adds a dedicated full-page PDF tool to the "Optimize & Security" suite.
2. Contributes a quick-action pill to the Ribbon's **View** tab.
3. Contributes a keyboard shortcut to the **Command Palette** (⌘K / Ctrl+K).
4. Provides configurable settings (Default Watermark Text, Default Opacity) with auto-generated M3 UI.

### Step 5.1: Create the Project File (`.csproj`)
Create a new directory named `FryPdf.Plugin.WatermarkPlus` and add `FryPdf.Plugin.WatermarkPlus.csproj`:

```xml
<Project Sdk="Microsoft.NET.Sdk">
  <PropertyGroup>
    <TargetFramework>net10.0</TargetFramework>
    <Nullable>enable</Nullable>
    <ImplicitUsings>enable</ImplicitUsings>
    <LangVersion>13</LangVersion>
    <TreatWarningsAsErrors>true</TreatWarningsAsErrors>
    <AssemblyName>FryPdf.Plugin.WatermarkPlus</AssemblyName>
    <RootNamespace>FryPdf.Plugin.WatermarkPlus</RootNamespace>
    <Version>1.0.0</Version>
    <Company>Acme Document Solutions</Company>
    <Description>Enterprise Watermarking Plugin for FryPDF</Description>
  </PropertyGroup>

  <!-- Option A: Direct reference to your installed FryPDF application (Zero source code needed) -->
  <PropertyGroup>
    <!-- Auto-detect FryPDF install path or use FRYPDF_HOME environment variable -->
    <FryPdfInstallDir Condition="'$(FryPdfInstallDir)' == '' and Exists('/Applications/FryPDF.app/Contents/MacOS')">/Applications/FryPDF.app/Contents/MacOS</FryPdfInstallDir>
    <FryPdfInstallDir Condition="'$(FryPdfInstallDir)' == '' and Exists('C:\Program Files\FryPDF')">C:\Program Files\FryPDF</FryPdfInstallDir>
    <FryPdfInstallDir Condition="'$(FryPdfInstallDir)' == '' and Exists('/opt/FryPDF')">/opt/FryPDF</FryPdfInstallDir>
    <FryPdfInstallDir Condition="'$(FryPdfInstallDir)' == ''">$(FRYPDF_HOME)</FryPdfInstallDir>
  </PropertyGroup>

  <ItemGroup>
    <!-- Compile against installed application binaries -->
    <Reference Include="PdfEditorApp.Core">
      <HintPath>$(FryPdfInstallDir)\PdfEditorApp.Core.dll</HintPath>
      <Private>false</Private> <!-- Do not bundle host assemblies into your plugin -->
    </Reference>
    <Reference Include="PdfEditorApp">
      <HintPath>$(FryPdfInstallDir)\PdfEditorApp.dll</HintPath>
      <Private>false</Private>
    </Reference>
  </ItemGroup>

  <!-- Option B: Alternatively reference via NuGet SDK package -->
  <!--
  <ItemGroup>
    <PackageReference Include="FryPdf.PluginSdk" Version="1.0.0" PrivateAssets="all" />
  </ItemGroup>
  -->

  <ItemGroup>
    <!-- UI & MVVM dependencies (marked Private=false if provided by host) -->
    <PackageReference Include="Avalonia" Version="12.1.1" PrivateAssets="all" />
    <PackageReference Include="CommunityToolkit.Mvvm" Version="8.4.2" PrivateAssets="all" />
  </ItemGroup>

  <ItemGroup>
    <!-- Include manifest in build output -->
    <None Include="plugin.json" CopyToOutputDirectory="PreserveNewest" />
  </ItemGroup>
</Project>
```

> [!IMPORTANT]
> **Host Dependency Exclusion (`<Private>false</Private>`)**:  
> Always mark `PdfEditorApp.Core`, `PdfEditorApp`, `Avalonia`, and `CommunityToolkit.Mvvm` as compile-only (`Private=false` or `PrivateAssets="all"`). The host application already loads these assemblies. Packaging duplicate copies of Avalonia or Core DLLs inside your plugin will cause type casting errors across `AssemblyLoadContext` boundaries.

---

### Step 5.2: Create the `plugin.json` Manifest
Create `plugin.json` in the project root:

```json
{
  "id": "com.acme.frypdf.watermarkplus",
  "name": "Watermark Plus",
  "version": "1.0.0",
  "author": "Acme Document Solutions",
  "description": "Apply enterprise security watermarks, confidential stamps, and company emblems to multi-page PDFs.",
  "entryPoint": "FryPdf.Plugin.WatermarkPlus.dll",
  "icon": "Watermark",
  "dependencies": [],
  "settingsSchema": {
    "DefaultText": {
      "type": "string",
      "label": "Default Stamp Text",
      "description": "Default text prefilled in the watermark input",
      "default": "CONFIDENTIAL"
    },
    "DefaultOpacity": {
      "type": "number",
      "label": "Default Opacity (%)",
      "description": "Default stamp opacity percentage (1-100)",
      "default": 35
    },
    "IncludeDateStamp": {
      "type": "boolean",
      "label": "Append Current Date",
      "description": "Automatically append today's date below the watermark text",
      "default": true
    },
    "StampColor": {
      "type": "select",
      "label": "Stamp Color",
      "description": "Default watermark tone",
      "default": "Red",
      "options": ["Red", "DarkSlate", "NavyBlue", "EmeraldGreen"]
    }
  }
}
```

#### Supported `settingsSchema` Types:
- **`"string"`**: Renders a Material Design 3 `TextBox.m3-outlined`.
- **`"secret"`**: Renders a secure password field with `PasswordChar="●"`.
- **`"number"`**: Renders an M3 `NumericUpDown.m3-outlined`.
- **`"boolean"`**: Renders a tactile M3 `ToggleSwitch`.
- **`"select"`**: Renders an M3 `ComboBox.m3-outlined` populated with the `options` array.

---

### Step 5.3: Implement the Tool ViewModel
Create `WatermarkPlusViewModel.cs`. Use `CommunityToolkit.Mvvm` for reactive properties and commands:

```csharp
using System;
using System.IO;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using PdfEditorApp.Services.Tools.Core;

namespace FryPdf.Plugin.WatermarkPlus;

public partial class WatermarkPlusViewModel : PdfToolViewModelBase
{
    public override string ToolId => "com.acme.frypdf.watermarkplus";
    public override string DisplayName => "Watermark Plus";
    public override string Category => "Optimize & Security";

    [ObservableProperty]
    private string _stampText = "CONFIDENTIAL";

    [ObservableProperty]
    private double _opacityPercent = 35.0;

    [ObservableProperty]
    private bool _includeDate = true;

    [ObservableProperty]
    private string _statusMessage = "Ready to stamp documents.";

    public WatermarkPlusViewModel(IServiceProvider serviceProvider) : base(serviceProvider)
    {
    }

    [RelayCommand]
    private async Task ApplyWatermarkAsync()
    {
        if (SelectedFiles.Count == 0)
        {
            StatusMessage = "Please select at least one PDF document.";
            return;
        }

        IsProcessing = true;
        ProgressValue = 0;
        StatusMessage = "Applying watermark stamps...";

        try
        {
            // Execute heavy PDF stamping off the UI thread via Task.Run
            await Task.Run(async () =>
            {
                int count = SelectedFiles.Count;
                for (int i = 0; i < count; i++)
                {
                    var file = SelectedFiles[i];
                    // Example: Apply stamp using Core deconstruction or QuestPDF pipelines
                    await Task.Delay(150); // Simulate processing

                    ProgressValue = (double)(i + 1) / count * 100.0;
                }
            });

            StatusMessage = $"Successfully watermarked {SelectedFiles.Count} documents!";
        }
        catch (Exception ex)
        {
            StatusMessage = $"Error: {ex.Message}";
        }
        finally
        {
            IsProcessing = false;
        }
    }
}
```

---

### Step 5.4: Implement the Avalonia M3 Tool View
Create `WatermarkPlusView.axaml` adhering strictly to **Material Design 3 Expressive** guidelines:

```xml
<UserControl xmlns="https://github.com/avaloniaui"
             xmlns:x="http://schemas.microsoft.com/winfx/2006/xaml"
             xmlns:vm="using:FryPdf.Plugin.WatermarkPlus"
             xmlns:materialIcons="clr-namespace:Material.Icons.Avalonia;assembly=Material.Icons.Avalonia"
             x:Class="FryPdf.Plugin.WatermarkPlus.WatermarkPlusView"
             x:DataType="vm:WatermarkPlusViewModel">

    <Grid RowDefinitions="Auto,*,Auto" Margin="24">
        <!-- Header Banner (M3 ShapeCornerLarge) -->
        <Border Grid.Row="0"
                Classes="m3-card-elevated"
                Background="{DynamicResource M3SurfaceContainerBrush}"
                CornerRadius="{StaticResource M3ShapeCornerLarge}"
                Padding="20" Margin="0,0,0,20">
            <StackPanel Spacing="6">
                <StackPanel Orientation="Horizontal" Spacing="12" VerticalAlignment="Center">
                    <Border Width="40" Height="40"
                            CornerRadius="{StaticResource M3ShapeCornerFull}"
                            Background="{DynamicResource M3PrimaryContainerBrush}">
                        <materialIcons:MaterialIcon Kind="Watermark" Width="22" Height="22"
                                                    Foreground="{DynamicResource M3OnPrimaryContainerBrush}" />
                    </Border>
                    <TextBlock Text="Watermark Plus Studio"
                               FontSize="22" FontWeight="SemiBold"
                               Foreground="{DynamicResource M3OnSurfaceBrush}"
                               VerticalAlignment="Center" />
                </StackPanel>
                <TextBlock Text="Apply high-security diagonal and marginalia stamps across multi-page PDFs."
                           FontSize="13" Foreground="{DynamicResource M3OnSurfaceVariantBrush}" />
            </StackPanel>
        </Border>

        <!-- Configuration Body -->
        <ScrollViewer Grid.Row="1">
            <StackPanel Spacing="16" MaxWidth="600" HorizontalAlignment="Left">
                <!-- Stamp Text Input -->
                <TextBlock Text="Stamp Text" FontWeight="Medium"
                           Foreground="{DynamicResource M3OnSurfaceBrush}" />
                <TextBox Classes="m3-outlined"
                         Text="{Binding StampText}"
                         Watermark="e.g. STRICTLY CONFIDENTIAL" />

                <!-- Tactile Chubby Opacity Slider -->
                <TextBlock Text="Stamp Opacity" FontWeight="Medium"
                           Foreground="{DynamicResource M3OnSurfaceBrush}" />
                <Grid ColumnDefinitions="*,Auto" Margin="0,4,0,0">
                    <Slider Grid.Column="0"
                            Minimum="5" Maximum="100"
                            Value="{Binding OpacityPercent}"
                            Margin="0,0,16,0" />
                    <TextBlock Grid.Column="1"
                               Text="{Binding OpacityPercent, StringFormat='{}{0:F0}%'}"
                               FontWeight="Bold" VerticalAlignment="Center"
                               Foreground="{DynamicResource M3PrimaryBrush}" />
                </Grid>

                <!-- Append Date Toggle -->
                <CheckBox Content="Append Current Date &amp; Timestamp"
                          IsChecked="{Binding IncludeDate}"
                          Foreground="{DynamicResource M3OnSurfaceBrush}" />

                <!-- Status Feedback -->
                <TextBlock Text="{Binding StatusMessage}"
                           FontSize="12" Foreground="{DynamicResource M3OutlineBrush}" />
            </StackPanel>
        </ScrollViewer>

        <!-- Action Footer with Tactile Pill Button -->
        <Border Grid.Row="2" Padding="0,16,0,0" BorderBrush="{DynamicResource M3OutlineVariantBrush}" BorderThickness="0,1,0,0">
            <StackPanel Orientation="Horizontal" HorizontalAlignment="Right" Spacing="12">
                <Button Classes="m3-filled-btn"
                        CornerRadius="{StaticResource M3ShapeCornerFull}"
                        Command="{Binding ApplyWatermarkCommand}"
                        Padding="24,10">
                    <StackPanel Orientation="Horizontal" Spacing="8">
                        <materialIcons:MaterialIcon Kind="CheckCircleOutline" Width="18" Height="18" />
                        <TextBlock Text="Apply Watermark" FontWeight="SemiBold" />
                    </StackPanel>
                </Button>
            </StackPanel>
        </Border>
    </Grid>
</UserControl>
```

And the code-behind in `WatermarkPlusView.axaml.cs`:

```csharp
using Avalonia.Controls;

namespace FryPdf.Plugin.WatermarkPlus;

public partial class WatermarkPlusView : UserControl
{
    public WatermarkPlusView()
    {
        InitializeComponent();
    }
}
```

---

### Step 5.5: Implement the Main Plugin Class
Now connect everything together by implementing `IFryPlugin` in `WatermarkPlusPlugin.cs`:

```csharp
using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using PdfEditorApp.Core.Plugins;
using PdfEditorApp.Core.Plugins.Descriptors;
using PdfEditorApp.Core.Plugins.Manifests;
using PdfEditorApp.Services.Tools.Core;

namespace FryPdf.Plugin.WatermarkPlus;

public class WatermarkPlusPlugin : IFryPlugin
{
    public string Id => "com.acme.frypdf.watermarkplus";
    public string Name => "Watermark Plus";
    public Version Version => new(1, 0, 0);

    public IReadOnlyList<Type> RequiredServices => Array.Empty<Type>();
    public IReadOnlyList<Type> ProvidedServices => Array.Empty<Type>();

    public IReadOnlyDictionary<string, PluginSettingDefinition>? SettingsSchema => new Dictionary<string, PluginSettingDefinition>
    {
        ["DefaultText"] = new()
        {
            Type = "string",
            Label = "Default Stamp Text",
            Description = "Default text prefilled in the watermark input",
            DefaultValue = "CONFIDENTIAL"
        },
        ["DefaultOpacity"] = new()
        {
            Type = "number",
            Label = "Default Opacity (%)",
            Description = "Default stamp opacity percentage (1-100)",
            DefaultValue = 35
        }
    };

    public Task ApplyAsync(IFryPluginContext ctx, CancellationToken ct = default)
    {
        // 1. Register Dedicated Full-Page Tool
        var toolRegistration = ctx.RegisterTool(new PdfToolDescriptor
        {
            Id = Id,
            Name = Name,
            Description = "Apply custom security watermarks with dynamic opacity and timestamp stamps.",
            Category = "OptimizeAndSecurity",
            IconKind = "Watermark",
            IconColorHex = "#0284C7",
            BackgroundAccentHex = "#F0F9FF",
            SupportsMultiFile = true,
            AcceptedFileExtensions = ".pdf",
            CreateViewModel = sp => ActivatorUtilities.CreateInstance<WatermarkPlusViewModel>(sp)
        });

        // 2. Register Ribbon Action in the 'Tools' Tab
        var ribbonRegistration = ctx.RegisterRibbonAction(new RibbonActionDescriptor
        {
            Id = "action.ribbon.watermarkplus",
            TabId = "tools",
            GroupId = "security",
            Label = "Watermark Plus",
            Tooltip = "Open Watermark Plus Studio",
            IconKind = "Watermark",
            Order = 105,
            Action = sp =>
            {
                if (sp.GetService(typeof(IPdfToolRegistry)) is IPdfToolRegistry toolRegistry)
                {
                    toolRegistry.OpenTool(Id);
                }
            }
        });

        // 3. Register Command in Command Palette (Ctrl+K / ⌘K)
        var cmdRegistration = ctx.RegisterCommand(new CommandPaletteDescriptor
        {
            Id = "cmd.watermarkplus.open",
            Title = "Open Watermark Plus Studio",
            Subtitle = "Security stamp and watermark tool",
            Category = "Security",
            IconKind = "Watermark",
            Shortcut = "Ctrl+Shift+W",
            Order = 80,
            Action = sp =>
            {
                if (sp.GetService(typeof(IPdfToolRegistry)) is IPdfToolRegistry toolRegistry)
                {
                    toolRegistry.OpenTool(Id);
                }
            }
        });

        // 4. Register Reversible Effects for 100% Clean Unload
        ctx.RegisterEffect(() =>
        {
            toolRegistration.Dispose();
            ribbonRegistration.Dispose();
            cmdRegistration.Dispose();
        });

        return Task.CompletedTask;
    }
}
```

---

## 6. Packaging Your Plugin (`.fryplugin`)

FryPDF uses `.fryplugin` distribution packages. A `.fryplugin` file is simply a standard ZIP archive with the following structure:

```
WatermarkPlus.fryplugin (ZIP file)
├── plugin.json                    # Root manifest
├── FryPdf.Plugin.WatermarkPlus.dll# Compiled entry assembly
└── [OtherDependency.dll]          # Any external 3rd-party dependencies (excluding host DLLs)
```

### 6.1 Automated Packaging with MSBuild (Recommended)
Add the following snippet to the bottom of your `.csproj` file. Every time you run `dotnet build -c Release`, MSBuild will automatically package your plugin into a `.fryplugin` file ready for installation:

```xml
  <!-- Automated .fryplugin packager -->
  <Target Name="PackageFryPlugin" AfterTargets="Build" Condition="'$(Configuration)' == 'Release'">
    <PropertyGroup>
      <PluginStagingDir>$(TargetDir)staging\</PluginStagingDir>
      <OutputFryPlugin>$(TargetDir)$(AssemblyName).fryplugin</OutputFryPlugin>
    </PropertyGroup>

    <RemoveDir Directories="$(PluginStagingDir)" />
    <MakeDir Directories="$(PluginStagingDir)" />

    <!-- Copy compiled DLLs and plugin.json -->
    <ItemGroup>
      <PluginFiles Include="$(TargetDir)*.dll" Exclude="$(TargetDir)PdfEditorApp*.dll;$(TargetDir)Avalonia*.dll" />
      <PluginFiles Include="$(ProjectDir)plugin.json" />
    </ItemGroup>

    <Copy SourceFiles="@(PluginFiles)" DestinationFolder="$(PluginStagingDir)" />
    <Delete Files="$(OutputFryPlugin)" Condition="Exists('$(OutputFryPlugin)')" />
    <ZipDirectory SourceDirectory="$(PluginStagingDir)" DestinationFile="$(OutputFryPlugin)" />
    <RemoveDir Directories="$(PluginStagingDir)" />

    <Message Importance="High" Text="✨ Successfully packaged FryPDF plugin: $(OutputFryPlugin)" />
  </Target>
```

### 6.2 Manual Packaging via CLI
You can also package your plugin manually using standard command-line tools:

```bash
# 1. Build project in Release mode
dotnet build -c Release

# 2. Navigate to output folder
cd bin/Release/net10.0/

# 3. Create ZIP archive renamed to .fryplugin
zip -r WatermarkPlus.fryplugin plugin.json FryPdf.Plugin.WatermarkPlus.dll
```

---

## 7. Installing and Testing Your Plugin in FryPDF

FryPDF supports 3 seamless ways to load and test your external plugin:

### Method 1: Drag-and-Drop Installation (Easiest)
1. Launch **FryPDF**.
2. Open the **Plugins Manager** (Click the Settings gear in the header $\to$ **Plugins & Extensions**, or press `Ctrl+Shift+P` / `⌘Shift+P`).
3. Drag and drop your `.fryplugin` (or `.dll`) directly onto the dialog window.
4. FryPDF immediately unpacks the package, inspects the manifest, loads the assembly into an isolated `CollectiblePluginLoadContext`, and mounts your plugin with zero restart required!

### Method 2: Command Palette or File Picker
1. In FryPDF, press `Ctrl+K` or `⌘K` to open the **Command Palette**.
2. Type `Install Plugin Package...` and press Enter.
3. Select your `.fryplugin` or compiled `.dll` file from the open file dialog.

### Method 3: Direct File Placement (Auto-Discovery)
On startup, FryPDF scans designated plugin directories and automatically loads all unpacked folders, `.fryplugin` archives, and standalone `.dll` files. You can drop your plugin files into either of the following locations:

#### Platform User Directories:
- **macOS**: `~/Library/Application Support/FryPdf/plugins/<PluginId>/`
- **Windows**: `%APPDATA%\FryPdf\plugins\<PluginId>\` (e.g. `C:\Users\<User>\AppData\Roaming\FryPdf\plugins\<PluginId>\`)
- **Linux**: `~/.config/FryPdf/plugins/<PluginId>/`

#### Application Local Directory:
- `<ApplicationDirectory>/plugins/<PluginId>/`

---

## 8. Material Design 3 (M3) Expressive Styling Mandate

To maintain visual harmony with FryPDF, all external plugins must adhere to **Google Material Design 3 Expressive**:

### 8.1 Shape Scale Tokens
Always reference centralized M3 shape scale tokens rather than hardcoding arbitrary numbers:

| Token Key | Radius | Usage |
| :--- | :--- | :--- |
| `{StaticResource M3ShapeCornerFull}` | `9999px` | Buttons (`m3-filled-btn`, `m3-tonal-btn`), segmented pills, search boxes (`TextBox.m3-search`), slider thumbs. |
| `{StaticResource M3ShapeCornerExtraLarge}` | `28px` | Modal dialog cards (`Border.m3-dialog-card`), hero banners. |
| `{StaticResource M3ShapeCornerLarge}` | `16px` | Content cards (`m3-card-elevated`), inputs (`TextBox.m3-outlined`), ComboBoxes. |
| `{StaticResource M3ShapeCornerMedium}` | `12px` | Context menus, flyouts, tooltips, list items. |
| `{StaticResource M3ShapeCornerSmall}` | `8px` | Badges, chips, thumbnail cards. |

### 8.2 Dynamic Tonal Brushes (Dark & Light Mode)
**NEVER** hardcode hex colors (like `#FFFFFF` or `#1E293B`) in your views. Always reference semantic `{DynamicResource ...}` keys so your plugin seamlessly responds to daylight, warm sepia, dark night, and high contrast themes:

- **Primary Colors**: `M3PrimaryBrush`, `M3OnPrimaryBrush`, `M3PrimaryContainerBrush`, `M3OnPrimaryContainerBrush`
- **Surfaces**: `M3SurfaceBrush`, `M3SurfaceContainerBrush`, `M3SurfaceContainerHighestBrush`, `M3SurfaceDimBrush`
- **Text & Outlines**: `M3OnSurfaceBrush`, `M3OnSurfaceVariantBrush`, `M3OutlineBrush`, `M3OutlineVariantBrush`

---

## 9. Performance & Memory Safety Mandates

FryPDF is an interactive 60+ FPS studio. External plugins must comply with the following performance rules:

1. **Zero UI Thread Blocking**:
   - **Never** perform PDF parsing, document rendering, OCR, or file I/O synchronously on the Avalonia UI thread.
   - Offload heavy tasks using `await Task.Run(...)` and support cooperative cancellation (`CancellationToken`).
2. **Unmanaged Graphics Disposal**:
   - If using `SkiaSharp` (`SKBitmap`, `SKImage`, `SKSurface`), always dispose old instances when replacing preview images to prevent native memory leaks.
3. **No Large Object Heap (LOH) Allocations**:
   - Do not allocate large byte arrays ($\ge 85\text{ KB}$) in tight loops. Use `ArrayPool<byte>.Shared` or streams.
4. **Collectible ALC Hygiene**:
   - Avoid creating static references, global event hooks, or long-lived thread-pool timers that reference your plugin types without an unsubscription mechanism. Static references will anchor your `AssemblyLoadContext` and prevent the plugin from being garbage collected when unloaded.

---

## 10. Automated Testing of External Plugins

You can verify your plugin's mounting and lifecycle in an xUnit test project using the same loader FryPDF uses:

```csharp
using System.IO;
using PdfEditorApp.Core.Plugins;
using PdfEditorApp.Plugins.Loader;
using Xunit;

namespace FryPdf.Plugin.WatermarkPlus.Tests;

public class PluginLifecycleTests
{
    [Fact]
    public void Plugin_Loads_And_Instantiates_Successfully()
    {
        var dllPath = typeof(WatermarkPlusPlugin).Assembly.Location;
        using var package = PluginAssemblyLoader.LoadPluginAssembly(dllPath);

        Assert.NotEmpty(package.Plugins);
        var plugin = package.Plugins[0];
        Assert.Equal("com.acme.frypdf.watermarkplus", plugin.Id);
        Assert.Equal("Watermark Plus", plugin.Name);
    }

    [Fact]
    public void PluginPackage_Unpacks_And_Validates_Manifest()
    {
        var tempPkg = Path.Combine(Path.GetTempPath(), "test.fryplugin");
        var stagingDir = Path.GetDirectoryName(typeof(WatermarkPlusPlugin).Assembly.Location)!;

        // Create package
        FryPluginPackageLoader.CreatePackage(stagingDir, tempPkg);
        Assert.True(File.Exists(tempPkg));

        // Unpack and verify
        using var result = FryPluginPackageLoader.UnpackAndLoad(tempPkg);
        Assert.Equal("com.acme.frypdf.watermarkplus", result.Manifest.Id);
        Assert.NotNull(result.AssemblyPackage);

        File.Delete(tempPkg);
    }
}
```

---

## 11. Summary & Next Steps

With FryPDF's modular microkernel architecture, creating professional external plugins is clean, fast, and robust:
- Implement `IFryPlugin` (or inherit `ToolPluginBase`).
- Register capabilities into `IFryPluginContext` with reversible rollback.
- Add `plugin.json` and package as `.fryplugin`.
- Drag and drop onto FryPDF to run!

For in-depth details on internal pipeline mechanics, DAG solvers, and core architecture, see:
- [Plugin-Based Architecture Manual (`PLUGIN_BASED_ARCHITECTURE.md`)](PLUGIN_BASED_ARCHITECTURE.md)
- [Complete Tic-Tac-Toe Reference Plugin Guide (`TIC_TAC_TOE_EXAMPLE.md`)](TIC_TAC_TOE_EXAMPLE.md) (Source: [`docs/examples/TicTacToePlugin/`](examples/TicTacToePlugin))
- [Material Design 3 Expressive Guidelines (`MATERIAL_DESIGN_3_EXPRESSIVE_GUIDELINES.md`)](MATERIAL_DESIGN_3_EXPRESSIVE_GUIDELINES.md)
- [PDF Deconstruction and Vector Editing (`PDF_DECONSTRUCTION_AND_EDITING.md`)](PDF_DECONSTRUCTION_AND_EDITING.md)
