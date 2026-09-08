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
        // AttachedToVisualTree fires every time this cached instance is navigated back to
        // (HomeViewModel reuses the same instance per section), not just on first construction —
        // so re-activate and catch up on anything logged while this page was detached (see
        // DetachedFromVisualTree below and DiagnosticLogsViewModel.ResyncAndActivate).
        AttachedToVisualTree += (_, _) =>
        {
            _vm.ResyncAndActivate();
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
