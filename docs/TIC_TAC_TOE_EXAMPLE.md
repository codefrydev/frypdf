# Tic-Tac-Toe Plugin Example for FryPDF

> **Complete Reference Implementation & Architectural Guide**  
> Learn how to build a fully functional, interactive floating mini-game plugin for **FryPDF** using **.NET 10, C# 13, CommunityToolkit.Mvvm, and Avalonia UI (Material Design 3 Expressive)**.

---

## 1. Overview & Objectives

The **Tic-Tac-Toe Plugin** is a gold-standard reference implementation demonstrating how to build an external plugin that integrates deeply with FryPDF without modifying any core application code.

The source code for this example is available in [`docs/examples/TicTacToePlugin/`](examples/TicTacToePlugin).

### Key Features Demonstrated:
- 🎮 **Floating Shell Overlay (`shell.overlay`)**: Uses `OverlayChromeMode.StandardCard` for automatic Material Design 3 window chrome (draggable header, pin, minimize, and close).
- 🤖 **Smart AI & 2-Player Pass-and-Play**: Tactical heuristic AI opponent and local pass-and-play modes.
- 🏆 **Reactive MVVM State**: Built with `CommunityToolkit.Mvvm` (`[ObservableProperty]`, `[RelayCommand]`), tracking 8 winning line combinations and score history.
- 🎨 **Google Material Design 3 (M3) Expressive Styling**: Seamlessly inherits FryPDF's shape tokens (`M3ShapeCornerFull`, `M3ShapeCornerLarge`, `M3ShapeCornerMedium`) and dynamic dark/light theme brushes.
- ⌨️ **Cross-Subsystem Registrations**:
  - **Command Palette**: `Ctrl+Alt+T` / `⌘Alt+T` quick toggle.
  - **Status Bar**: Clickable footer pill widget (`🎮 Tic-Tac-Toe`).
  - **Ribbon**: Action pill button in the **View** tab.
- 📦 **Automated MSBuild Packaging**: Compiles and outputs a ready-to-install `TicTacToe.fryplugin` package on `dotnet build -c Release`.

---

## 2. Architecture & Extension Points

The diagram below illustrates how the Tic-Tac-Toe plugin hooks into FryPDF's microkernel runtime:

```
┌────────────────────────────────────────────────────────────────────────┐
│                        FryPDF Microkernel Runtime                      │
│                                                                        │
│   ┌────────────────────┐   ┌───────────────────┐   ┌───────────────┐   │
│   │  IOverlayRegistry  │   │ ICommandPalette   │   │  IStatusBar   │   │
│   └─────────▲──────────┘   └─────────▲─────────┘   └───────▲───────┘   │
│             │                        │                     │           │
│   ┌─────────┴────────────────────────┴─────────────────────┴───────┐   │
│   │                 TicTacToePlugin (IFryPlugin)                   │   │
│   │  - Id: com.frypdf.plugin.tictactoe                             │   │
│   │  - Chrome: OverlayChromeMode.StandardCard                      │   │
│   │  - Effects: LIFO rollback via ctx.RegisterEffect               │   │
│   └──────────────────────────────────┬─────────────────────────────┘   │
│                                      │                                 │
│             ┌────────────────────────┴─────────────────────┐           │
│             ▼                                              ▼           │
│   ┌────────────────────┐                        ┌──────────────────┐   │
│   │ TicTacToeViewModel │                        │  TicTacToeView   │   │
│   │ (State, AI, Win)   │                        │ (Avalonia M3 UI) │   │
│   └────────────────────┘                        └──────────────────┘   │
└────────────────────────────────────────────────────────────────────────┘
```

---

## 3. Code Walkthrough

### 3.1 Project File (`TicTacToePlugin.csproj`)
External third-party developers **do not need to clone FryPDF's source code**. The project uses a smart `<Choose>` block:
- When built outside the repository, it automatically discovers and references the DLLs from your **installed FryPDF application** (e.g. `/Applications/FryPDF.app`, `C:\Program Files\FryPDF`, or `FRYPDF_HOME`).
- When built inside the repository, it links directly to the local source projects for continuous integration testing.

```xml
<Project Sdk="Microsoft.NET.Sdk">
  <PropertyGroup>
    <TargetFramework>net10.0</TargetFramework>
    <Nullable>enable</Nullable>
    <ImplicitUsings>enable</ImplicitUsings>
    <LangVersion>13</LangVersion>
    <TreatWarningsAsErrors>true</TreatWarningsAsErrors>
    <AssemblyName>TicTacToePlugin</AssemblyName>
    <RootNamespace>FryPdf.Plugin.TicTacToe</RootNamespace>
    <Version>1.0.0</Version>
  </PropertyGroup>

  <!-- Dual-mode resolution: Standalone installed app reference vs In-repo project reference -->
  <Choose>
    <When Condition="Exists('..\..\..\src\PdfEditorApp.Core\PdfEditorApp.Core.csproj')">
      <ItemGroup>
        <ProjectReference Include="..\..\..\src\PdfEditorApp.Core\PdfEditorApp.Core.csproj">
          <Private>false</Private>
          <ExcludeAssets>runtime</ExcludeAssets>
        </ProjectReference>
        <ProjectReference Include="..\..\..\src\PdfEditorApp\PdfEditorApp.csproj">
          <Private>false</Private>
          <ExcludeAssets>runtime</ExcludeAssets>
        </ProjectReference>
      </ItemGroup>
    </When>
    <Otherwise>
      <PropertyGroup>
        <FryPdfInstallDir Condition="'$(FryPdfInstallDir)' == '' and Exists('/Applications/FryPDF.app/Contents/MacOS')">/Applications/FryPDF.app/Contents/MacOS</FryPdfInstallDir>
        <FryPdfInstallDir Condition="'$(FryPdfInstallDir)' == '' and Exists('C:\Program Files\FryPDF')">C:\Program Files\FryPDF</FryPdfInstallDir>
        <FryPdfInstallDir Condition="'$(FryPdfInstallDir)' == '' and Exists('/opt/FryPDF')">/opt/FryPDF</FryPdfInstallDir>
        <FryPdfInstallDir Condition="'$(FryPdfInstallDir)' == ''">$(FRYPDF_HOME)</FryPdfInstallDir>
      </PropertyGroup>
      <ItemGroup>
        <Reference Include="PdfEditorApp.Core">
          <HintPath>$(FryPdfInstallDir)\PdfEditorApp.Core.dll</HintPath>
          <Private>false</Private>
        </Reference>
        <Reference Include="PdfEditorApp">
          <HintPath>$(FryPdfInstallDir)\PdfEditorApp.dll</HintPath>
          <Private>false</Private>
        </Reference>
      </ItemGroup>
    </Otherwise>
  </Choose>

  <ItemGroup>
    <PackageReference Include="Avalonia" Version="12.1.1" PrivateAssets="all" />
    <PackageReference Include="CommunityToolkit.Mvvm" Version="8.4.2" PrivateAssets="all" />
    <PackageReference Include="Material.Icons.Avalonia" Version="3.0.2" PrivateAssets="all" />
  </ItemGroup>

  <!-- Automated MSBuild packager creating .fryplugin on Release build -->
  <Target Name="PackageFryPlugin" AfterTargets="Build" Condition="'$(Configuration)' == 'Release'">
    <PropertyGroup>
      <PluginStagingDir>$(TargetDir)staging\</PluginStagingDir>
      <OutputFryPlugin>$(TargetDir)TicTacToe.fryplugin</OutputFryPlugin>
    </PropertyGroup>

    <RemoveDir Directories="$(PluginStagingDir)" />
    <MakeDir Directories="$(PluginStagingDir)" />

    <ItemGroup>
      <PluginFiles Include="$(TargetDir)TicTacToePlugin.dll" />
      <PluginFiles Include="$(ProjectDir)plugin.json" />
    </ItemGroup>

    <Copy SourceFiles="@(PluginFiles)" DestinationFolder="$(PluginStagingDir)" />
    <Delete Files="$(OutputFryPlugin)" Condition="Exists('$(OutputFryPlugin)')" />
    <ZipDirectory SourceDirectory="$(PluginStagingDir)" DestinationFile="$(OutputFryPlugin)" />
    <RemoveDir Directories="$(PluginStagingDir)" />

    <Message Importance="High" Text="✨ Successfully packaged FryPDF plugin: $(OutputFryPlugin)" />
  </Target>
</Project>
```

---

### 3.2 Manifest Specification (`plugin.json`)
The manifest declares identity, entry point assembly, and a typed `settingsSchema`. When users click "Settings" in FryPDF's Plugins Manager, FryPDF automatically renders an M3 Expressive configuration form:

```json
{
  "id": "com.frypdf.plugin.tictactoe",
  "name": "Tic-Tac-Toe",
  "version": "1.0.0",
  "author": "Code Fry Dev",
  "description": "Interactive floating Tic-Tac-Toe mini-game with local 2-Player mode and intelligent AI opponent.",
  "entryPoint": "TicTacToePlugin.dll",
  "icon": "GamepadVariantOutline",
  "dependencies": [],
  "settingsSchema": {
    "DefaultMode": {
      "type": "select",
      "label": "Default Game Mode",
      "description": "Choose between single player vs AI or pass-and-play two player mode",
      "default": "PlayerVsAi",
      "options": ["PlayerVsAi", "TwoPlayers"]
    },
    "AiDifficulty": {
      "type": "select",
      "label": "AI Difficulty",
      "description": "Strategy level used by the computer opponent",
      "default": "Smart",
      "options": ["Smart", "Unbeatable", "Easy"]
    },
    "SoundEnabled": {
      "type": "boolean",
      "label": "Sound Effects",
      "description": "Play subtle audio cues on move placements and victories",
      "default": true
    }
  }
}
```

---

### 3.3 The Plugin Class (`TicTacToePlugin.cs`)
The plugin class implements `IFryPlugin`. During `ApplyAsync`, it registers across 4 dynamic extension slots and registers rollback actions:

```csharp
public class TicTacToePlugin : IFryPlugin
{
    public string Id => "com.frypdf.plugin.tictactoe";
    public string Name => "Tic-Tac-Toe";
    public Version Version => new(1, 0, 0);

    public IReadOnlyList<Type> RequiredServices => Array.Empty<Type>();

    public Task ApplyAsync(IFryPluginContext ctx, CancellationToken ct = default)
    {
        // 1. Register Floating Shell Overlay
        var overlayReg = ctx.RegisterOverlay(new OverlayDescriptor
        {
            Id = Id,
            Title = "🎮 Tic-Tac-Toe",
            Slot = "shell.overlay",
            DefaultWidth = 330,
            DefaultHeight = 440,
            IsDraggable = true,
            IsMinimizable = true,
            IsClosable = true,
            IconKind = "GamepadVariantOutline",
            ChromeMode = OverlayChromeMode.StandardCard,
            ViewType = typeof(TicTacToeView),
            ViewModelType = typeof(TicTacToeViewModel),
            ViewFactory = _ => new TicTacToeView { DataContext = new TicTacToeViewModel() },
            ViewModelFactory = _ => new TicTacToeViewModel()
        });

        // 2. Register Command in Command Palette (Ctrl+Alt+T / ⌘Alt+T)
        var cmdReg = ctx.RegisterCommand(new CommandPaletteDescriptor
        {
            Id = "cmd.overlay.tictactoe",
            Title = "Play Tic-Tac-Toe (Shell Overlay)",
            Category = "Shell Overlays",
            IconKind = "GamepadVariantOutline",
            Shortcut = "Ctrl+Alt+T",
            Action = sp => (sp.GetService(typeof(IOverlayRegistry)) as IOverlayRegistry)?.ToggleOverlay(Id)
        });

        // 3. Register Footer Status Bar Widget
        var statusReg = ctx.RegisterStatusBarWidget(new StatusBarWidgetDescriptor
        {
            WidgetId = "frypdf.status.tictactoe",
            Alignment = StatusBarAlignment.Right,
            Order = 16,
            Factory = sp => new StatusBarWidgetViewModel
            {
                WidgetId = "frypdf.status.tictactoe",
                Label = "🎮 Tic-Tac-Toe",
                IconKind = "GamepadVariantOutline",
                Command = new RelayCommand(() => (sp.GetService(typeof(IOverlayRegistry)) as IOverlayRegistry)?.ToggleOverlay(Id))
            }
        });

        // 4. Register Quick-Action Pill in Ribbon 'View' Tab
        var ribbonReg = ctx.RegisterRibbonAction(new RibbonActionDescriptor
        {
            Id = "frypdf.ribbon.action.tictactoe",
            TabId = "view",
            GroupId = "plugins",
            Label = "Tic-Tac-Toe",
            IconKind = "GamepadVariantOutline",
            Action = sp => (sp.GetService(typeof(IOverlayRegistry)) as IOverlayRegistry)?.ToggleOverlay(Id)
        });

        // 5. Reversible Effects (Clean LIFO Rollback)
        ctx.RegisterEffect(() =>
        {
            overlayReg.Dispose();
            cmdReg.Dispose();
            statusReg.Dispose();
            ribbonReg.Dispose();
            if (ctx.TryGetService<IOverlayRegistry>(out var reg)) reg.HideOverlay(Id);
        });

        return Task.CompletedTask;
    }
}
```

---

### 3.4 The ViewModel & AI Logic (`TicTacToeViewModel.cs`)
The ViewModel manages cell states, turn switching, and game outcome evaluation:

```csharp
public partial class TicTacToeViewModel : ObservableObject
{
    private static readonly int[][] WinningLines =
    [
        [0, 1, 2], [3, 4, 5], [6, 7, 8], // Rows
        [0, 3, 6], [1, 4, 7], [2, 5, 8], // Columns
        [0, 4, 8], [2, 4, 6]             // Diagonals
    ];

    public ObservableCollection<TicTacToeCellViewModel> Cells { get; } = new();

    [ObservableProperty] private string _currentTurn = "X";
    [ObservableProperty] private string _statusMessage = "Player X's Turn (❌)";
    [ObservableProperty] private bool _isGameOver;
    [ObservableProperty] private bool _isAiMode = true;
    [ObservableProperty] private int _xWins;
    [ObservableProperty] private int _oWins;
    [ObservableProperty] private int _draws;

    [RelayCommand]
    public void MakeMove(int index)
    {
        if (IsGameOver || index < 0 || index >= Cells.Count) return;

        var cell = Cells[index];
        if (!string.IsNullOrEmpty(cell.Value)) return;

        cell.Value = CurrentTurn;
        cell.IsEnabled = false;

        if (EvaluateBoard()) return;

        CurrentTurn = CurrentTurn == "X" ? "O" : "X";
        StatusMessage = CurrentTurn == "X" ? "Player X's Turn (❌)" : "Player O's Turn (⭕)";

        if (IsAiMode && CurrentTurn == "O" && !IsGameOver)
        {
            ExecuteAiTurn();
        }
    }
    // ... Additional AI and evaluation heuristics
}
```

---

### 3.5 The View & M3 Styling (`TicTacToeView.axaml`)
The XAML view uses standard Material Design 3 tokens:
- **`M3ShapeCornerLarge` (16px)** for outer grid frame.
- **`M3ShapeCornerMedium` (12px)** for the 9 cell buttons and scoreboard cards.
- **`M3ShapeCornerFull` (9999px)** for pills and action buttons (`m3-filled-btn`, `m3-tonal-btn`).
- **`{DynamicResource M3SurfaceContainerBrush}`**, **`{DynamicResource M3PrimaryBrush}`**, and **`{DynamicResource M3OnSurfaceBrush}`** for automatic light and dark mode adaptation.

---

## 4. Building and Packaging the Plugin

1. Open your terminal in `docs/examples/TicTacToePlugin/`:
   ```bash
   cd docs/examples/TicTacToePlugin
   ```
2. Run the Release build:
   ```bash
   dotnet build -c Release
   ```
3. The build outputs:
   ```
   ✨ Successfully packaged FryPDF plugin: bin/Release/net10.0/TicTacToe.fryplugin
   ```

---

## 5. Installing and Playing in FryPDF

1. Start **FryPDF**:
   ```bash
   ./run
   ```
2. Open **Plugins Manager** (`Ctrl+Shift+P` or `⌘Shift+P`).
3. Drag and drop `TicTacToe.fryplugin` onto the dialog window.
4. Open the game via any of the 3 entry points:
   - Press `Ctrl+Alt+T` / `⌘Alt+T`.
   - Click the `🎮 Tic-Tac-Toe` pill in the footer status bar.
   - Click the `Tic-Tac-Toe` button in the Ribbon's **View** tab.
5. Enjoy the game! Drag it anywhere across your document canvas, minimize it, or toggle between AI and 2-Player modes.
