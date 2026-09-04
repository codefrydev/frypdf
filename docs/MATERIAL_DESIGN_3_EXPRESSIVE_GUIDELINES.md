# Google Material Design 3 (M3) Expressive Guidelines

This document provides developer guidelines and design principles for building and maintaining user interfaces in **FryPDF** according to **Google Material Design 3 (M3) Expressive**.

---

## 1. Core Principles of Material 3 Expressive

1. **Expressive Geometry & Rounded Contours**:
   - Material 3 Expressive moves away from sharp, boxy, or rigid rectangular UI.
   - Elements feature soft, welcoming, and tactile corner radii:
     - Large pill shapes for interactive triggers, search fields, and buttons.
     - 28px extra-large rounded corners for modal dialogs and alert surfaces.
     - 16px large corners for cards, form fields, and inspector panels.

2. **Tonal Hierarchy & Semantic Color Roles**:
   - Rather than arbitrary saturated colors, M3 Expressive employs a balanced tonal hierarchy:
     - **Primary & OnPrimary**: High-emphasis focal actions.
     - **PrimaryContainer & OnPrimaryContainer**: Standout highlights and contextual notices.
     - **SecondaryContainer & OnSecondaryContainer**: Selected tabs, active pills, badges, chips.
     - **Surface, SurfaceDim, SurfaceContainer (Lowest to Highest)**: Layered depth without harsh outlines.
     - **Outline & OutlineVariant**: Subtle boundary separation.

3. **Tactile Interaction & Motion Physics**:
   - Interactive elements respond with tactile feedback:
     - Chubby sliders with 8-10px tracks and 20-22px thumbs that scale slightly on hover.
     - Smooth 150ms brush and transform transitions on buttons, chips, and menu items.

4. **100% Theme Adaptability (Light & Dark)**:
   - Never hardcode raw hex colors in XAML.
   - Always reference `{DynamicResource ...}` keys so the UI transitions seamlessly between Light and Dark modes.

---

## 2. Shape Scale Tokens Reference

All shape tokens are defined in [`src/PdfEditorApp/Styles/Material3ExpressiveTokens.axaml`](../src/PdfEditorApp/Styles/Material3ExpressiveTokens.axaml):

| Token Resource Key | Value | Typical Use Case |
|---|---|---|
| `M3ShapeCornerNone` | `0px` | Full-bleed splitters, root windows, edge dividers |
| `M3ShapeCornerExtraSmall` | `4px` | Micro-badges, inline code spans, keyboard shortcut hints (`kbd-badge`) |
| `M3ShapeCornerSmall` | `8px` | Badges, thumbnail cards, small chips, icon button frames |
| `M3ShapeCornerMedium` | `12px` | Context menus, flyouts, tooltips, list items, card inner sections |
| `M3ShapeCornerLarge` | `16px` | Content cards (`m3-card-elevated`), `TextBox.m3-outlined`, `ComboBox`, `NumericUpDown`, inspector section groups |
| `M3ShapeCornerExtraLarge` | `28px` | Modal dialog cards (`Border.m3-dialog-card`, `AboutDialog`, `CommandPaletteDialog`, `AiAssistantDialog`), hero promo banners |
| `M3ShapeCornerFull` | `9999px` | Tactile pill buttons, segmented button capsules (`m3-segmented-container`), global search bars (`TextBox.m3-search`), Floating Action Buttons (`m3-fab`), chubby slider thumbs |

---

## 3. Color Roles Reference

| Token Resource Key | Light Theme Role | Dark Theme Role | Description |
|---|---|---|---|
| `M3PrimaryBrush` | `#00639A` | `#92CCFF` | High-emphasis actions and accents |
| `M3OnPrimaryBrush` | `#FFFFFF` | `#003353` | Text/icons on top of Primary |
| `M3PrimaryContainerBrush` | `#CEE5FF` | `#004B76` | Prominent highlights and container fills |
| `M3OnPrimaryContainerBrush` | `#001D32` | `#CEE5FF` | Text/icons on top of PrimaryContainer |
| `M3SecondaryBrush` | `#51606F` | `#B8C8DA` | Supporting secondary actions |
| `M3SecondaryContainerBrush` | `#D5E4F7` | `#3A4857` | Active navigation tabs, selected chips, segmented items |
| `M3OnSecondaryContainerBrush` | `#0E1D2A` | `#D5E4F7` | Text/icons on top of SecondaryContainer |
| `M3SurfaceBrush` | `#F8F9FF` | `#101418` | Base canvas and screen background |
| `M3SurfaceDimBrush` | `#D8DAE0` | `#101418` | Dimmed surface contrast |
| `M3SurfaceContainerLowestBrush`| `#FFFFFF` | `#0B0F12` | Deepest recessed containers |
| `M3SurfaceContainerLowBrush` | `#F2F3FA` | `#181C20` | Subtle background panels |
| `M3SurfaceContainerBrush` | `#ECEEF4` | `#1D2024` | Default cards and panels |
| `M3SurfaceContainerHighBrush` | `#E6E8EE` | `#272A2F` | Hovered items and elevated headers |
| `M3SurfaceContainerHighestBrush`| `#E0E2E8` | `#32353A` | Search bars and segmented capsules |
| `M3OutlineBrush` | `#727780` | `#8C919A` | High-contrast component borders |
| `M3OutlineVariantBrush` | `#C2C7CF` | `#42474E` | Subtle dividers and card outlines |

### Legacy Aliases (Fully Supported)
For compatibility with existing views, the following aliases automatically resolve to the corresponding M3 color tokens:
- `WinBgBrush` $\to$ `M3SurfaceBrush`
- `WinPanelBrush` $\to$ `M3SurfaceContainerBrush`
- `WinBorderBrush` $\to$ `M3OutlineVariantBrush`
- `WinAccentBrush` $\to$ `M3PrimaryBrush`
- `WinAccentHoverBrush` $\to$ `M3PrimaryHoverBrush`
- `WinTextBrush` $\to$ `M3OnSurfaceBrush`
- `WinMutedBrush` $\to$ `M3OnSurfaceVariantBrush`
- `WinHoverBrush` $\to$ `M3SurfaceContainerHighBrush`
- `WinActiveBrush` $\to$ `M3SecondaryContainerBrush`
- `WinInputBgBrush` $\to$ `M3SurfaceContainerLowestBrush`

---

## 4. Component Styles Reference

All pre-built M3 Expressive styles are defined in [`src/PdfEditorApp/Styles/Material3ExpressiveStyles.axaml`](../src/PdfEditorApp/Styles/Material3ExpressiveStyles.axaml):

### Buttons
```xml
<!-- Filled Primary Action Button -->
<Button Classes="m3-filled-btn" Content="Save Document" />

<!-- Tonal Secondary Action Button -->
<Button Classes="m3-tonal-btn" Content="Duplicate Page" />

<!-- Elevated Action Button -->
<Button Classes="m3-elevated-btn" Content="Export" />

<!-- Outlined Action Button -->
<Button Classes="m3-outlined-btn" Content="Cancel" />

<!-- Borderless Text Button -->
<Button Classes="m3-text-btn" Content="Learn More" />

<!-- Circular / Pill Floating Action Button (FAB) -->
<Button Classes="m3-fab">
    <materialIcons:MaterialIcon Kind="Plus" Width="24" Height="24" />
</Button>

<!-- Extended FAB with Icon + Text -->
<Button Classes="m3-fab-extended">
    <StackPanel Orientation="Horizontal" Spacing="8">
        <materialIcons:MaterialIcon Kind="Plus" Width="20" Height="20" />
        <TextBlock Text="New Page" />
    </StackPanel>
</Button>

<!-- Icon Trigger Button -->
<Button Classes="m3-icon-btn">
    <materialIcons:MaterialIcon Kind="DotsVertical" Width="20" Height="20" />
</Button>
```

### Segmented Buttons Capsule
```xml
<Border Classes="m3-segmented-container">
    <StackPanel Orientation="Horizontal" Spacing="2">
        <Button Classes="m3-segment-btn active" Content="Design" />
        <Button Classes="m3-segment-btn" Content="Preview" />
        <Button Classes="m3-segment-btn" Content="Code" />
    </StackPanel>
</Border>
```

### Chips & Filter Pills
```xml
<!-- Action / Suggestion Chip -->
<Button Classes="m3-chip" Content="Landscape" />

<!-- Filter Toggle Chip -->
<ToggleButton Classes="m3-filter-chip" IsChecked="True" Content="Vector Only" />
```

### Text Inputs & Search Bars
```xml
<!-- Expressive Outlined Input -->
<TextBox Classes="m3-outlined" PlaceholderText="Document Title" />

<!-- Expressive Pill Search Bar -->
<TextBox Classes="m3-search" PlaceholderText="Search elements or tools..." />
```

### Dialog Cards & Modal Containers
```xml
<Border Classes="m3-dialog-card" Width="640">
    <Grid RowDefinitions="Auto,*,Auto">
        <!-- Dialog Header, Body, and Pill Action Footer -->
    </Grid>
</Border>
```

---

## 5. Rules for Future UI Development

1. **NO Hardcoded Dimensions for Corner Radii**: Always use `{StaticResource M3ShapeCorner...}`.
2. **NO Hardcoded Hex Colors**: Always use `{DynamicResource M3...Brush}` or legacy aliases (`{DynamicResource Win...Brush}`).
3. **Pills for Primary Interactivity**: Buttons, global search fields, and segmented selectors must use pill contours (`M3ShapeCornerFull` / `CornerRadius="9999"`).
4. **28px for Modals & Dialogs**: All popup/modal dialog cards must have `CornerRadius="{StaticResource M3ShapeCornerExtraLarge}"` and multi-layered diffuse shadows (`M3ElevationLevel4` or `M3ElevationLevel5`).
5. **Always Run Tests**: Verify that any UI changes pass `dotnet build` (with 0 warnings) and `dotnet test --filter "FullyQualifiedName~Material3ExpressiveThemeTests"`.
