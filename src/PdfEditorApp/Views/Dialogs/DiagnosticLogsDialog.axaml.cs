using Avalonia.Controls;
using Avalonia.Input.Platform;
using Avalonia.Interactivity;
using PdfEditorApp.Services;
using PdfEditorApp.ViewModels;

namespace PdfEditorApp.Views.Dialogs;

public partial class DiagnosticLogsDialog : UserControl
{
    private readonly DiagnosticLogsViewModel _vm;

    public DiagnosticLogsDialog()
    {
        InitializeComponent();

        _vm = new DiagnosticLogsViewModel(AppLogService.Instance);
        DataContext = _vm;

        // Wire clipboard via TopLevel (Avalonia 12 pattern).
        // AttachedToVisualTree fires once the control is in the visual tree and TopLevel is available.
        AttachedToVisualTree += (_, _) =>
        {
            _vm.SetClipboardText = async text =>
            {
                var clipboard = TopLevel.GetTopLevel(this)?.Clipboard;
                if (clipboard != null)
                    await clipboard.SetTextAsync(text);
            };
        };

        DetachedFromVisualTree += (_, _) =>
        {
            // Deactivate to unregister WeakReferenceMessenger subscriptions when navigated away
            _vm.IsActive = false;
            _vm.SetClipboardText = null;
        };
    }

    // ── Per-row copy button ──────────────────────────────────────────────────

    private async void OnCopyRowClick(object? sender, RoutedEventArgs e)
    {
        if (sender is Button { Tag: AppLogEntry entry })
        {
            var clipboard = TopLevel.GetTopLevel(this)?.Clipboard;
            if (clipboard != null)
                await clipboard.SetTextAsync(entry.FormattedLine);
        }
    }
}
