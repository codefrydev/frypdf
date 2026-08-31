using Avalonia.Controls;
using Avalonia.Input;

namespace PdfEditorApp.Views;

public partial class CanvasTextHudView : UserControl
{
    public CanvasTextHudView()
    {
        InitializeComponent();
    }

    private void OnHudPointerPressed(object? sender, PointerPressedEventArgs e)
    {
        // Stop pointer press from propagating to the underlying canvas scroll viewer
        e.Handled = true;
    }
}
