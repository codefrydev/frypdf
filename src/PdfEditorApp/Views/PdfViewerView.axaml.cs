using Avalonia.Controls;
using Avalonia.Markup.Xaml;

namespace PdfEditorApp.Views;

public partial class PdfViewerView : UserControl
{
    public PdfViewerView()
    {
        InitializeComponent();
    }

    private void InitializeComponent()
    {
        AvaloniaXamlLoader.Load(this);
    }
}
