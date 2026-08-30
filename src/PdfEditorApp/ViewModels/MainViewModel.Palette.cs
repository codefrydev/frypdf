using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Reflection;
using Avalonia.Input.Platform;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using PdfEditorApp.Models;

namespace PdfEditorApp.ViewModels;

public partial class MainViewModel
{
    public string AppVersion
    {
        get
        {
            try
            {
                var assembly = Assembly.GetExecutingAssembly();
                var infoVer = assembly.GetCustomAttribute<AssemblyInformationalVersionAttribute>()?.InformationalVersion;
                if (!string.IsNullOrWhiteSpace(infoVer))
                {
                    return infoVer.Split('+')[0];
                }

                var ver = assembly.GetName().Version;
                if (ver != null)
                {
                    int build = ver.Build >= 0 ? ver.Build : 0;
                    return $"{ver.Major}.{ver.Minor}.{build}";
                }
            }
            catch { }
            return "1.0.0";
        }
    }

    public string AppVersionDisplay => $"v{AppVersion} Open Source";

    // --- COMMAND PALETTE & SHORTCUTS HELP STATE ---

    [ObservableProperty]
    private bool _isCommandPaletteOpen;

    [ObservableProperty]
    private string _commandSearchQuery = "";

    [ObservableProperty]
    private int _selectedPaletteIndex;

    [ObservableProperty]
    private bool _isShortcutsHelpDialogOpen;

    [ObservableProperty]
    private bool _isAboutDialogOpen;

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

    [RelayCommand]
    public void OpenAboutDialog()
    {
        IsAboutDialogOpen = true;
    }

    [RelayCommand]
    public void CloseAboutDialog()
    {
        IsAboutDialogOpen = false;
    }

    [RelayCommand]
    public async System.Threading.Tasks.Task CopySupportEmail()
    {
        const string email = "codefrydev@gmail.com";
        try
        {
            if (Avalonia.Application.Current?.ApplicationLifetime is Avalonia.Controls.ApplicationLifetimes.IClassicDesktopStyleApplicationLifetime desktop && desktop.MainWindow?.Clipboard != null)
            {
                await desktop.MainWindow.Clipboard.SetTextAsync(email);
            }
        }
        catch { }
        ShowToast($"Copied support email: {email}", "EmailOutline");
    }

    [RelayCommand]
    public async System.Threading.Tasks.Task OpenCompanyWebsite()
    {
        const string url = "https://codefrydev.in";
        try
        {
            if (Avalonia.Application.Current?.ApplicationLifetime is Avalonia.Controls.ApplicationLifetimes.IClassicDesktopStyleApplicationLifetime desktop && desktop.MainWindow?.Launcher != null)
            {
                await desktop.MainWindow.Launcher.LaunchUriAsync(new Uri(url));
            }
            else
            {
                System.Diagnostics.Process.Start(new System.Diagnostics.ProcessStartInfo { FileName = url, UseShellExecute = true });
            }
        }
        catch
        {
            try
            {
                if (Avalonia.Application.Current?.ApplicationLifetime is Avalonia.Controls.ApplicationLifetimes.IClassicDesktopStyleApplicationLifetime desktop && desktop.MainWindow?.Clipboard != null)
                {
                    await desktop.MainWindow.Clipboard.SetTextAsync(url);
                }
            }
            catch { }
            ShowToast($"Copied website link: {url}", "Web");
            return;
        }
        ShowToast("Opening codefrydev.in...", "Web");
    }

    [RelayCommand]
    public async System.Threading.Tasks.Task OpenMicrosoftStore()
    {
        const string storeUrl = "https://apps.microsoft.com/detail/9P5GW2Q81B33";
        try
        {
            if (Avalonia.Application.Current?.ApplicationLifetime is Avalonia.Controls.ApplicationLifetimes.IClassicDesktopStyleApplicationLifetime desktop && desktop.MainWindow?.Launcher != null)
            {
                await desktop.MainWindow.Launcher.LaunchUriAsync(new Uri(storeUrl));
            }
            else
            {
                System.Diagnostics.Process.Start(new System.Diagnostics.ProcessStartInfo { FileName = storeUrl, UseShellExecute = true });
            }
        }
        catch
        {
            try
            {
                if (Avalonia.Application.Current?.ApplicationLifetime is Avalonia.Controls.ApplicationLifetimes.IClassicDesktopStyleApplicationLifetime desktop && desktop.MainWindow?.Clipboard != null)
                {
                    await desktop.MainWindow.Clipboard.SetTextAsync(storeUrl);
                }
            }
            catch { }
            ShowToast($"Copied Microsoft Store link: {storeUrl}", "Microsoft");
            return;
        }
        ShowToast("Opening Microsoft Store...", "Microsoft");
    }

    [RelayCommand]
    public async System.Threading.Tasks.Task CopyDiagnostics()
    {
        var diagnostics = $"FryPDF Open-Source Edition v{AppVersion}\n" +
                          $"Publisher: Code Fry Dev (CN=7E83DE15-E15F-41B6-B068-989D9548D0BF)\n" +
                          $"Package Family Name: CodeFryDev.FryPDF_ntemjm2faw5zw\n" +
                          $"Store ID: 9P5GW2Q81B33 (https://apps.microsoft.com/detail/9P5GW2Q81B33)\n" +
                          $"MSA App ID: 4d091113-f7b6-4421-9318-220eb8b7234e\n" +
                          $"Website: https://codefrydev.in\n" +
                          $"Support: codefrydev@gmail.com\n" +
                          $"OS: {System.Runtime.InteropServices.RuntimeInformation.OSDescription} ({System.Runtime.InteropServices.RuntimeInformation.OSArchitecture})\n" +
                          $"Runtime: .NET {Environment.Version}\n" +
                          $"Avalonia UI: 12.1.1\n" +
                          $"Engine: QuestPDF 2026.8.0 + SkiaSharp\n" +
                          $"Timestamp: {DateTime.UtcNow:yyyy-MM-dd HH:mm:ss} UTC";
        try
        {
            if (Avalonia.Application.Current?.ApplicationLifetime is Avalonia.Controls.ApplicationLifetimes.IClassicDesktopStyleApplicationLifetime desktop && desktop.MainWindow?.Clipboard != null)
            {
                await desktop.MainWindow.Clipboard.SetTextAsync(diagnostics);
            }
        }
        catch { }
        ShowToast("System diagnostics copied to clipboard", "InformationOutline");
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
        AllPaletteCommands.Add(new CommandPaletteItem { Title = "Save Project", Subtitle = "Save editable FryPDF project archive (.frypdf)", Category = "File", IconKind = "ContentSaveOutline", Shortcut = "⌘S", Action = () => SaveProjectCommand.Execute(null) });
        AllPaletteCommands.Add(new CommandPaletteItem { Title = "Open Project", Subtitle = "Open existing FryPDF project archive (.frypdf)", Category = "File", IconKind = "FolderOpenOutline", Shortcut = "⌘O", Action = () => OpenProjectCommand.Execute(null) });
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
        AllPaletteCommands.Add(new CommandPaletteItem { Title = "Search & Redact Pattern", Subtitle = "Auto-redact text occurrences on current page", Category = "Security", IconKind = "DatabaseSearchOutline", Action = () => OpenSearchRedactDialogCommand.Execute(null) });
        AllPaletteCommands.Add(new CommandPaletteItem { Title = "Burn In All Redactions", Subtitle = "Permanently commit solid blackouts to PDF", Category = "Security", IconKind = "ShieldCheckOutline", Action = () => BurnInAllRedactionsCommand.Execute(null) });
        AllPaletteCommands.Add(new CommandPaletteItem { Title = "Document Security & Passwords", Subtitle = "Configure password protection and permissions", Category = "Security", IconKind = "ShieldLockOutline", Action = () => OpenSecurityDialogCommand.Execute(null) });
        AllPaletteCommands.Add(new CommandPaletteItem { Title = "Sanitize Document", Subtitle = "Scrub author metadata and internal review notes", Category = "Security", IconKind = "ShieldCheck", Action = () => SanitizeDocumentCommand.Execute(null) });
        AllPaletteCommands.Add(new CommandPaletteItem { Title = "Apply Bates Numbering", Subtitle = "Sequential legal discovery numbering (CONF-BATES-000001)", Category = "Security", IconKind = "Numeric", Action = () => ApplyBatesNumberingCommand.Execute(null) });

        // 7. Fill & Sign Studio
        AllPaletteCommands.Add(new CommandPaletteItem { Title = "Open Signature Studio", Subtitle = "Draw, type cursive calligraphy, or upload digital signature", Category = "Sign", IconKind = "Draw", Action = () => OpenSignatureStudioCommand.Execute(null) });
        AllPaletteCommands.Add(new CommandPaletteItem { Title = "Stamp Today's Date", Subtitle = "Insert dynamic verified date badge", Category = "Sign", IconKind = "CalendarClockOutline", Action = () => AddDateStampCommand.Execute(null) });
        AllPaletteCommands.Add(new CommandPaletteItem { Title = "Stamp Signer Initials", Subtitle = "Insert circular monogram initial stamp", Category = "Sign", IconKind = "AccountOutline", Action = () => AddInitialsBadgeCommand.Execute("JD") });
        AllPaletteCommands.Add(new CommandPaletteItem { Title = "Stamp Checkmark (✓)", Subtitle = "Insert green verification checkmark", Category = "Sign", IconKind = "CheckBold", Action = () => AddCheckmarkBadgeCommand.Execute(null) });
        AllPaletteCommands.Add(new CommandPaletteItem { Title = "Stamp Cross (✕)", Subtitle = "Insert red rejection cross mark", Category = "Sign", IconKind = "CloseThick", Action = () => AddCrossBadgeCommand.Execute(null) });
        AllPaletteCommands.Add(new CommandPaletteItem { Title = "Custom Stamp Creator", Subtitle = "Create timestamped custom legal certification stamp", Category = "Stamps", IconKind = "Stamp", Action = () => OpenCustomStampDialogCommand.Execute(null) });

        // 8. Watermarks & Headers/Footers
        AllPaletteCommands.Add(new CommandPaletteItem { Title = "Header & Footer Studio", Subtitle = "Configure multi-zone header/footer with dynamic macros", Category = "Organize", IconKind = "PageLayoutHeaderFooter", Action = () => OpenHeaderFooterDialogCommand.Execute(null) });
        AllPaletteCommands.Add(new CommandPaletteItem { Title = "Watermark Manager", Subtitle = "Apply CONFIDENTIAL/DRAFT watermark across all pages", Category = "Organize", IconKind = "Watermark", Action = () => OpenWatermarkManagerCommand.Execute(null) });

        // 9. Preflight Audit & Health Diagnostics
        AllPaletteCommands.Add(new CommandPaletteItem { Title = "Preflight Health Check & Audit", Subtitle = "Inspect PDF compliance, fonts, broken links, accessibility", Category = "Audit", IconKind = "FileCheckOutline", Action = () => OpenPreflightDialogCommand.Execute(null) });
        AllPaletteCommands.Add(new CommandPaletteItem { Title = "Export Comments Summary", Subtitle = "Export all review notes to Markdown document", Category = "Audit", IconKind = "CommentTextMultipleOutline", Action = () => ExportCommentsSummaryCommand.Execute(null) });

        // 10. Pages & Navigation
        AllPaletteCommands.Add(new CommandPaletteItem { Title = "Add Blank Page", Subtitle = "Insert new page at end of document", Category = "Pages", IconKind = "FilePlusOutline", Shortcut = "⌘⇧N", Action = () => AddPageCommand.Execute(null) });
        AllPaletteCommands.Add(new CommandPaletteItem { Title = "Duplicate Current Page", Subtitle = "Clone active page with all elements", Category = "Pages", IconKind = "FileMultipleOutline", Shortcut = "⌘⇧D", Action = () => DuplicateCurrentPageCommand.Execute(null) });
        AllPaletteCommands.Add(new CommandPaletteItem { Title = "Rotate Page 90° Clockwise", Subtitle = "Rotate current page orientation", Category = "Pages", IconKind = "RotateRight", Shortcut = "⌘⇧R", Action = () => RotateCurrentPageCommand.Execute(null) });
        AllPaletteCommands.Add(new CommandPaletteItem { Title = "Delete Current Page", Subtitle = "Remove active page from document", Category = "Pages", IconKind = "DeleteOutline", Shortcut = "⌘⇧⌫", Action = () => DeleteCurrentPageCommand.Execute(null) });
        AllPaletteCommands.Add(new CommandPaletteItem { Title = "Next Page", Subtitle = "Go to next document page", Category = "Navigation", IconKind = "ChevronRight", Shortcut = "PgDn", Action = () => NextPageCommand.Execute(null) });
        AllPaletteCommands.Add(new CommandPaletteItem { Title = "Previous Page", Subtitle = "Go to previous document page", Category = "Navigation", IconKind = "ChevronLeft", Shortcut = "PgUp", Action = () => PreviousPageCommand.Execute(null) });

        // 11. View & Guides
        AllPaletteCommands.Add(new CommandPaletteItem { Title = "Toggle Canvas Grid", Subtitle = "Show/hide alignment grid dots", Category = "View", IconKind = "Grid", Action = () => ToggleGridCommand.Execute(null) });
        AllPaletteCommands.Add(new CommandPaletteItem { Title = "Toggle Snap to Grid", Subtitle = "Snap elements to precise 20pt intervals", Category = "View", IconKind = "Magnet", Action = () => ToggleSnapToGridCommand.Execute(null) });
        AllPaletteCommands.Add(new CommandPaletteItem { Title = "Zoom In", Subtitle = "Increase canvas scale by 10%", Category = "View", IconKind = "MagnifyPlusOutline", Shortcut = "⌘+", Action = () => ZoomInCommand.Execute(null) });
        AllPaletteCommands.Add(new CommandPaletteItem { Title = "Zoom Out", Subtitle = "Decrease canvas scale by 10%", Category = "View", IconKind = "MagnifyMinusOutline", Shortcut = "⌘-", Action = () => ZoomOutCommand.Execute(null) });
        AllPaletteCommands.Add(new CommandPaletteItem { Title = "Reset Zoom (100%)", Subtitle = "Reset canvas view to 1:1 scale", Category = "View", IconKind = "Magnify", Shortcut = "⌘0", Action = () => ResetZoomCommand.Execute(null) });
        AllPaletteCommands.Add(new CommandPaletteItem { Title = "Fit to Width", Subtitle = "Scale page to fill viewport width", Category = "View", IconKind = "ArrowExpandHorizontal", Shortcut = "⌘1", Action = () => FitToWidthCommand.Execute(null) });
        AllPaletteCommands.Add(new CommandPaletteItem { Title = "Fit to Page", Subtitle = "Scale page to view whole sheet", Category = "View", IconKind = "FitToPageOutline", Shortcut = "⌘9", Action = () => FitToPageCommand.Execute(null) });
        AllPaletteCommands.Add(new CommandPaletteItem { Title = "Keyboard Shortcuts Reference", Subtitle = "Open keyboard cheatsheet dialog", Category = "Help", IconKind = "KeyboardOutline", Shortcut = "F1", Action = () => OpenShortcutsHelpCommand.Execute(null) });
        AllPaletteCommands.Add(new CommandPaletteItem { Title = "About FryPDF & CodeFryDev", Subtitle = "Company info, open source licensing & support", Category = "Help", IconKind = "InformationOutline", Action = () => OpenAboutDialogCommand.Execute(null) });
        AllPaletteCommands.Add(new CommandPaletteItem { Title = "Contact Support (codefrydev@gmail.com)", Subtitle = "Copy developer support email to clipboard", Category = "Help", IconKind = "EmailOutline", Action = () => CopySupportEmailCommand.Execute(null) });
        AllPaletteCommands.Add(new CommandPaletteItem { Title = "Visit CodeFryDev Website", Subtitle = "Open official codefrydev.in developer portal", Category = "Help", IconKind = "Web", Action = () => OpenCompanyWebsiteCommand.Execute(null) });
        AllPaletteCommands.Add(new CommandPaletteItem { Title = "Microsoft Store Page", Subtitle = "View FryPDF on Microsoft Store (9P5GW2Q81B33)", Category = "Help", IconKind = "Microsoft", Action = () => OpenMicrosoftStoreCommand.Execute(null) });
        AllPaletteCommands.Add(new CommandPaletteItem { Title = "Copy System Diagnostics", Subtitle = "Copy OS, framework, store identity, and app version report", Category = "Help", IconKind = "BugOutline", Action = () => CopyDiagnosticsCommand.Execute(null) });

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
