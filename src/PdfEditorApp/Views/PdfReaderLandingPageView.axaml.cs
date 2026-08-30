using Avalonia.Controls;
using Avalonia.Markup.Xaml;

namespace PdfEditorApp.Views;

public partial class PdfReaderLandingPageView : UserControl
{
    public PdfReaderLandingPageView()
    {
        InitializeComponent();
    }

    private void InitializeComponent()
    {
        AvaloniaXamlLoader.Load(this);
    }
}
