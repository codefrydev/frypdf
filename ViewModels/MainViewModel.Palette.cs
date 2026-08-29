using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using PdfEditorApp.Models;

namespace PdfEditorApp.ViewModels;

public partial class MainViewModel
{
    // --- COMMAND PALETTE & SHORTCUTS HELP STATE ---

    [ObservableProperty]
    private bool _isCommandPaletteOpen;

    [ObservableProperty]
    private string _commandSearchQuery = "";

    [ObservableProperty]
    private int _selectedPaletteIndex;

    [ObservableProperty]
    private bool _isShortcutsHelpDialogOpen;

    public ObservableCollection<CommandPaletteItem> FilteredPaletteCommands { get; } = new();
    public List<CommandPaletteItem> AllPaletteCommands { get; } = new();

    partial void OnCommandSearchQueryChanged(string value)
    {
        FilterPaletteCommands(value);
    }

    [RelayCommand]
    public void OpenCommandPalette()
    {
        CommandSearchQuery = "";
        FilterPaletteCommands("");
        IsCommandPaletteOpen = true;
    }

    [RelayCommand]
    public void CloseCommandPalette()
    {
        IsCommandPaletteOpen = false;
        CommandSearchQuery = "";
    }

    [RelayCommand]
    public void OpenShortcutsHelp()
    {
        IsShortcutsHelpDialogOpen = true;
    }

    [RelayCommand]
    public void CloseShortcutsHelp()
    {
        IsShortcutsHelpDialogOpen = false;
    }

    public void SelectNextPaletteCommand()
    {
        if (FilteredPaletteCommands.Count == 0) return;
        SelectedPaletteIndex = (SelectedPaletteIndex + 1) % FilteredPaletteCommands.Count;
    }

    public void SelectPreviousPaletteCommand()
    {
        if (FilteredPaletteCommands.Count == 0) return;
        SelectedPaletteIndex = (SelectedPaletteIndex - 1 + FilteredPaletteCommands.Count) % FilteredPaletteCommands.Count;
    }

    public void ExecuteSelectedPaletteCommand()
    {
        if (SelectedPaletteIndex >= 0 && SelectedPaletteIndex < FilteredPaletteCommands.Count)
        {
            var item = FilteredPaletteCommands[SelectedPaletteIndex];
            CloseCommandPalette();
            item.Action?.Invoke();
        }
    }

    public void InitCommandPalette()
    {
        AllPaletteCommands.Clear();

        // 1. File Operations
        AllPaletteCommands.Add(new CommandPaletteItem { Title = "Save Project", Subtitle = "Save editable PDF creator project archive (.pdfproj)", Category = "File", IconKind = "ContentSaveOutline", Shortcut = "⌘S", Action = () => SaveProjectCommand.Execute(null) });
        AllPaletteCommands.Add(new CommandPaletteItem { Title = "Open Project", Subtitle = "Open existing project from disk", Category = "File", IconKind = "FolderOpenOutline", Shortcut = "⌘O", Action = () => OpenProjectCommand.Execute(null) });
        AllPaletteCommands.Add(new CommandPaletteItem { Title = "New Document / Templates", Subtitle = "Browse and create from executive templates", Category = "File", IconKind = "FilePlusOutline", Shortcut = "⌘N", Action = () => OpenNewDocumentDialogCommand.Execute(null) });
        AllPaletteCommands.Add(new CommandPaletteItem { Title = "Export Production PDF", Subtitle = "Compile document to high-resolution vector PDF", Category = "File", IconKind = "FilePdfBox", Shortcut = "⌘E", Action = () => ExportPdfCommand.Execute(null) });

        // 2. Edit & History
        AllPaletteCommands.Add(new CommandPaletteItem { Title = "Undo Action", Subtitle = "Revert last canvas or page operation", Category = "Edit", IconKind = "Undo", Shortcut = "⌘Z", Action = () => UndoCommand.Execute(null) });
        AllPaletteCommands.Add(new CommandPaletteItem { Title = "Redo Action", Subtitle = "Reapply reverted operation", Category = "Edit", IconKind = "Redo", Shortcut = "⌘Y", Action = () => RedoCommand.Execute(null) });
        AllPaletteCommands.Add(new CommandPaletteItem { Title = "Copy Element", Subtitle = "Copy selected element to internal clipboard", Category = "Edit", IconKind = "ContentCopy", Shortcut = "⌘C", Action = () => CopyCommand.Execute(null) });
        AllPaletteCommands.Add(new CommandPaletteItem { Title = "Cut Element", Subtitle = "Cut selected element to internal clipboard", Category = "Edit", IconKind = "ContentCut", Shortcut = "⌘X", Action = () => CutCommand.Execute(null) });
        AllPaletteCommands.Add(new CommandPaletteItem { Title = "Paste Element", Subtitle = "Paste element from clipboard to current page", Category = "Edit", IconKind = "ContentPaste", Shortcut = "⌘V", Action = () => PasteCommand.Execute(null) });
        AllPaletteCommands.Add(new CommandPaletteItem { Title = "Duplicate Element", Subtitle = "Clone selected element with offset", Category = "Edit", IconKind = "ContentDuplicate", Shortcut = "⌘D", Action = () => DuplicateCommand.Execute(null) });
        AllPaletteCommands.Add(new CommandPaletteItem { Title = "Delete Selected Element", Subtitle = "Remove active element from canvas", Category = "Edit", IconKind = "DeleteOutline", Shortcut = "⌫", Action = () => Inspector.DeleteSelectedElementCommand.Execute(null) });

        // 3. Insert Elements
        AllPaletteCommands.Add(new CommandPaletteItem { Title = "Insert Text Paragraph", Subtitle = "Add multi-line editable rich text block", Category = "Insert", IconKind = "FormatColorText", Shortcut = "T", Action = () => AddTextElementCommand.Execute(null) });
        AllPaletteCommands.Add(new CommandPaletteItem { Title = "Insert Section Heading", Subtitle = "Add bold Georgia 22pt section title", Category = "Insert", IconKind = "FormatHeader1", Shortcut = "H", Action = () => AddHeadingElementCommand.Execute(null) });
        AllPaletteCommands.Add(new CommandPaletteItem { Title = "Insert Image Graphic", Subtitle = "Import PNG, JPEG, or WebP graphic from disk", Category = "Insert", IconKind = "ImageOutline", Shortcut = "⌘I", Action = () => AddImageElementCommand.Execute(null) });
        AllPaletteCommands.Add(new CommandPaletteItem { Title = "Insert Divider Line", Subtitle = "Add horizontal section divider line", Category = "Insert", IconKind = "VectorLine", Action = () => AddDividerElementCommand.Execute(null) });
        AllPaletteCommands.Add(new CommandPaletteItem { Title = "Insert Data Table", Subtitle = "Add customizable multi-column data grid", Category = "Insert", IconKind = "TableLarge", Action = () => AddTableElementCommand.Execute(null) });
        AllPaletteCommands.Add(new CommandPaletteItem { Title = "Insert Watermark Overlay", Subtitle = "Add confidential watermark stamp", Category = "Insert", IconKind = "Watermark", Action = () => AddWatermarkElementCommand.Execute(null) });

        // 4. Shapes & Stamps
        AllPaletteCommands.Add(new CommandPaletteItem { Title = "Insert Rectangle Shape", Subtitle = "Add geometric rectangle with fill & stroke", Category = "Shapes", IconKind = "SquareOutline", Shortcut = "R", Action = () => AddShapeElementCommand.Execute("Rectangle") });
        AllPaletteCommands.Add(new CommandPaletteItem { Title = "Insert Circle / Ellipse Shape", Subtitle = "Add circular vector shape", Category = "Shapes", IconKind = "CircleOutline", Action = () => AddShapeElementCommand.Execute("Circle") });
        AllPaletteCommands.Add(new CommandPaletteItem { Title = "Insert 5-Point Star", Subtitle = "Add decorative badge star", Category = "Shapes", IconKind = "StarOutline", Action = () => AddShapeElementCommand.Execute("Star5") });
        AllPaletteCommands.Add(new CommandPaletteItem { Title = "Insert 'APPROVED' Stamp", Subtitle = "Green legal certification stamp", Category = "Stamps", IconKind = "CheckCircleOutline", Action = () => AddStampElementCommand.Execute("Approved") });
        AllPaletteCommands.Add(new CommandPaletteItem { Title = "Insert 'CONFIDENTIAL' Stamp", Subtitle = "Red security classification stamp", Category = "Stamps", IconKind = "ShieldLockOutline", Action = () => AddStampElementCommand.Execute("Confidential") });

        // 5. Data & Charts
        AllPaletteCommands.Add(new CommandPaletteItem { Title = "Insert Bar / Column Chart", Subtitle = "Visual vertical bar comparison chart", Category = "Charts", IconKind = "ChartBar", Action = () => AddChartElementCommand.Execute("BarColumn") });
        AllPaletteCommands.Add(new CommandPaletteItem { Title = "Insert Donut Pie Chart", Subtitle = "Proportional breakdown donut chart", Category = "Charts", IconKind = "ChartPie", Action = () => AddChartElementCommand.Execute("DonutPie") });
        AllPaletteCommands.Add(new CommandPaletteItem { Title = "Insert Vector QR Code", Subtitle = "Dynamic URL, Wi-Fi, or vCard QR generator", Category = "Data", IconKind = "Qrcode", Action = () => AddQrCodeElementCommand.Execute(null) });
        AllPaletteCommands.Add(new CommandPaletteItem { Title = "Insert Barcode", Subtitle = "Code128 / EAN / PDF417 optical barcode", Category = "Data", IconKind = "Barcode", Action = () => AddBarcodeElementCommand.Execute(null) });

        // 6. Security & Markup
        AllPaletteCommands.Add(new CommandPaletteItem { Title = "Insert Sticky Review Note", Subtitle = "Collaborative annotation note", Category = "Markup", IconKind = "NoteTextOutline", Shortcut = "N", Action = () => AddStickyNoteElementCommand.Execute(null) });
        AllPaletteCommands.Add(new CommandPaletteItem { Title = "Insert Highlighter Stroke", Subtitle = "Yellow semi-transparent highlight marker", Category = "Markup", IconKind = "Marker", Shortcut = "H", Action = () => AddInkElementCommand.Execute(true) });
        AllPaletteCommands.Add(new CommandPaletteItem { Title = "Insert Freehand Ink Drawing", Subtitle = "Freehand pen stroke vector element", Category = "Markup", IconKind = "DrawPen", Shortcut = "D", Action = () => AddInkElementCommand.Execute(false) });
        AllPaletteCommands.Add(new CommandPaletteItem { Title = "Insert Redaction Blackout Box", Subtitle = "Permanent FOIA / GDPR privileged blackout", Category = "Security", IconKind = "EyeOffOutline", Action = () => AddRedactionElementCommand.Execute("[REDACTED]") });
        AllPaletteCommands.Add(new CommandPaletteItem { Title = "Apply Bates Numbering", Subtitle = "Sequential legal discovery numbering (CONF-BATES-000001)", Category = "Security", IconKind = "Numeric", Action = () => ApplyBatesNumberingCommand.Execute(null) });

        // 7. Pages & Navigation
        AllPaletteCommands.Add(new CommandPaletteItem { Title = "Add Blank Page", Subtitle = "Insert new page at end of document", Category = "Pages", IconKind = "FilePlusOutline", Shortcut = "⌘⇧N", Action = () => AddPageCommand.Execute(null) });
        AllPaletteCommands.Add(new CommandPaletteItem { Title = "Duplicate Current Page", Subtitle = "Clone active page with all elements", Category = "Pages", IconKind = "FileMultipleOutline", Shortcut = "⌘⇧D", Action = () => DuplicateCurrentPageCommand.Execute(null) });
        AllPaletteCommands.Add(new CommandPaletteItem { Title = "Rotate Page 90° Clockwise", Subtitle = "Rotate current page orientation", Category = "Pages", IconKind = "RotateRight", Shortcut = "⌘⇧R", Action = () => RotateCurrentPageCommand.Execute(null) });
        AllPaletteCommands.Add(new CommandPaletteItem { Title = "Delete Current Page", Subtitle = "Remove active page from document", Category = "Pages", IconKind = "DeleteOutline", Shortcut = "⌘⇧⌫", Action = () => DeleteCurrentPageCommand.Execute(null) });
        AllPaletteCommands.Add(new CommandPaletteItem { Title = "Next Page", Subtitle = "Go to next document page", Category = "Navigation", IconKind = "ChevronRight", Shortcut = "PgDn", Action = () => NextPageCommand.Execute(null) });
        AllPaletteCommands.Add(new CommandPaletteItem { Title = "Previous Page", Subtitle = "Go to previous document page", Category = "Navigation", IconKind = "ChevronLeft", Shortcut = "PgUp", Action = () => PreviousPageCommand.Execute(null) });

        // 8. View & Zoom
        AllPaletteCommands.Add(new CommandPaletteItem { Title = "Zoom In", Subtitle = "Increase canvas scale by 10%", Category = "View", IconKind = "MagnifyPlusOutline", Shortcut = "⌘+", Action = () => ZoomInCommand.Execute(null) });
        AllPaletteCommands.Add(new CommandPaletteItem { Title = "Zoom Out", Subtitle = "Decrease canvas scale by 10%", Category = "View", IconKind = "MagnifyMinusOutline", Shortcut = "⌘-", Action = () => ZoomOutCommand.Execute(null) });
        AllPaletteCommands.Add(new CommandPaletteItem { Title = "Reset Zoom (100%)", Subtitle = "Reset canvas view to 1:1 scale", Category = "View", IconKind = "Magnify", Shortcut = "⌘0", Action = () => ResetZoomCommand.Execute(null) });
        AllPaletteCommands.Add(new CommandPaletteItem { Title = "Fit to Width", Subtitle = "Scale page to fill viewport width", Category = "View", IconKind = "ArrowExpandHorizontal", Shortcut = "⌘1", Action = () => FitToWidthCommand.Execute(null) });
        AllPaletteCommands.Add(new CommandPaletteItem { Title = "Fit to Page", Subtitle = "Scale page to view whole sheet", Category = "View", IconKind = "FitToPageOutline", Shortcut = "⌘9", Action = () => FitToPageCommand.Execute(null) });
        AllPaletteCommands.Add(new CommandPaletteItem { Title = "Keyboard Shortcuts Reference", Subtitle = "Open keyboard cheatsheet dialog", Category = "Help", IconKind = "KeyboardOutline", Shortcut = "F1", Action = () => OpenShortcutsHelpCommand.Execute(null) });

        FilterPaletteCommands("");
    }

    public void FilterPaletteCommands(string query)
    {
        FilteredPaletteCommands.Clear();
        var q = query?.Trim() ?? "";

        var matching = string.IsNullOrEmpty(q)
            ? AllPaletteCommands
            : AllPaletteCommands.Where(c =>
                c.Title.Contains(q, StringComparison.OrdinalIgnoreCase) ||
                c.Subtitle.Contains(q, StringComparison.OrdinalIgnoreCase) ||
                c.Category.Contains(q, StringComparison.OrdinalIgnoreCase) ||
                c.Shortcut.Contains(q, StringComparison.OrdinalIgnoreCase));

        foreach (var cmd in matching)
        {
            FilteredPaletteCommands.Add(cmd);
        }

        SelectedPaletteIndex = FilteredPaletteCommands.Count > 0 ? 0 : -1;
    }
}
