# FryPDF Agent Operating Instructions (GEMINI.md)

## CRITICAL UI MANDATE: Google Material Design 3 (M3) Expressive (STRICT)

**ATTENTION AI AGENT**:
You MUST strictly adhere to **Google Material Design 3 (M3) Expressive** whenever modifying or creating any UI elements, views, sidebars, buttons, dialogs, or styles in this repository.

1. **Shape Scale Hierarchy**:
   - **Pills (`CornerRadius="{StaticResource M3ShapeCornerFull}"` or `9999`)**: All buttons (`primary-btn`, `accent-btn`, `action-pill-btn`, `m3-filled-btn`, `m3-tonal-btn`), search inputs (`TextBox.m3-search`), segmented capsules (`m3-segmented-container`), FABs (`m3-fab`), and chubby slider thumbs.
   - **28px Extra-Large (`CornerRadius="{StaticResource M3ShapeCornerExtraLarge}"`)**: All modal dialog cards (`Border.m3-dialog-card`, `AboutDialog`, `CommandPaletteDialog`, `AiAssistantDialog`) and hero banners.
   - **16px Large (`CornerRadius="{StaticResource M3ShapeCornerLarge}"`)**: Cards (`m3-card-elevated`), inputs (`TextBox.m3-outlined`), `ComboBox`, `NumericUpDown`, inspector section groups.
   - **12px Medium (`CornerRadius="{StaticResource M3ShapeCornerMedium}"`)**: Context menus, tooltips, list items.
   - **8px Small (`CornerRadius="{StaticResource M3ShapeCornerSmall}"`)**: Badges, thumbnail cards, chips (`m3-chip`, `m3-filter-chip`).
   - **NEVER** use small arbitrary corner radii (like `2` or `4`) on interactive elements.

2. **Colors & Dark Mode**:
   - **NEVER** hardcode hex colors in templates or hover triggers.
   - Always reference `{DynamicResource ...}` keys: `M3PrimaryBrush`, `M3SecondaryContainerBrush`, `M3SurfaceBrush`, `M3SurfaceContainer...`, `M3OutlineBrush`, or legacy aliases (`WinBgBrush`, `WinAccentBrush`, etc.).

3. **Chubby Tactile Sliders**:
   - Always maintain chubby tactile sliders with 8-10px tracks and 20-22px thumbs with hover transitions.

4. **CommunityToolkit.Mvvm Mandate**:
   - Maximize use of `CommunityToolkit.Mvvm` source generators (`[ObservableProperty]`, `[RelayCommand]`, `[AsyncRelayCommand]`).
   - Use `[NotifyPropertyChangedFor]` and `[NotifyCanExecuteChangedFor]` to cleanly cascade property and command updates.
   - Use `WeakReferenceMessenger.Default` for decoupled pub/sub messaging across ViewModels and services.
   - Use `ObservableValidator` with DataAnnotations for dialogs and user forms.

5. **Reference Files**:
   - Tokens: [`src/PdfEditorApp/Styles/Material3ExpressiveTokens.axaml`](src/PdfEditorApp/Styles/Material3ExpressiveTokens.axaml)
   - Styles: [`src/PdfEditorApp/Styles/Material3ExpressiveStyles.axaml`](src/PdfEditorApp/Styles/Material3ExpressiveStyles.axaml)
   - Guidelines: [`docs/MATERIAL_DESIGN_3_EXPRESSIVE_GUIDELINES.md`](docs/MATERIAL_DESIGN_3_EXPRESSIVE_GUIDELINES.md)
   - Operating Guide: [`.agents/AGENTS.md`](.agents/AGENTS.md)
