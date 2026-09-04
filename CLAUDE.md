# Claude Code Operating Guidelines for FryPDF

## Build & Test Commands
- Build: `dotnet build` (Warning-free required: `TreatWarningsAsErrors=true`)
- Run Tests: `dotnet test`
- Filtered Tests: `dotnet test --filter "FullyQualifiedName~Material3ExpressiveThemeTests"`

## CRITICAL UI MANDATE: Google Material Design 3 (M3) Expressive
ALL UI work in this repository must strictly adhere to Google Material Design 3 (M3) Expressive:
- **Shape Scale**:
  - Buttons, search bars, segmented capsules, FABs, chubby slider thumbs $\to$ Pill (`CornerRadius="{StaticResource M3ShapeCornerFull}"` or `9999`)
  - Dialog cards $\to$ Extra Large (`CornerRadius="{StaticResource M3ShapeCornerExtraLarge}"` or `28`)
  - Cards, inputs (`TextBox.m3-outlined`), ComboBox, NumericUpDown $\to$ Large (`CornerRadius="{StaticResource M3ShapeCornerLarge}"` or `16`)
  - Menus and list items $\to$ Medium (`CornerRadius="{StaticResource M3ShapeCornerMedium}"` or `12`)
  - Chips and badges $\to$ Small (`CornerRadius="{StaticResource M3ShapeCornerSmall}"` or `8`)
- **Colors**: Never hardcode hex colors. Always reference dynamic tokens: `M3PrimaryBrush`, `M3SecondaryContainerBrush`, `M3SurfaceContainer...`, or legacy aliases (`WinBgBrush`, `WinAccentBrush`, etc.).
- **Chubby Sliders**: Retain chubby 8-10px tracks and 20-22px thumbs with hover transitions.
- **CommunityToolkit.Mvvm**: Take maximum advantage of CommunityToolkit source generators (`[ObservableProperty]`, `[RelayCommand]`, `[AsyncRelayCommand]`), cascading notifications (`[NotifyPropertyChangedFor]`, `[NotifyCanExecuteChangedFor]`), and `WeakReferenceMessenger.Default` for decoupled pub/sub messaging.
- **Reference**: Read `.agents/rules/m3_expressive_ui_mandate.md` and `docs/MATERIAL_DESIGN_3_EXPRESSIVE_GUIDELINES.md`.
