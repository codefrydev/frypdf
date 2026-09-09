using Avalonia.Controls;
using Avalonia.Markup.Xaml;

namespace PdfEditorApp.Views.Controls;

/// <summary>
/// Renders a <see cref="ViewModels.PageViewModel"/> at full page geometry.
/// </summary>
/// <remarks>
/// Only used off-screen by <see cref="Services.TemplatePreviewRasterizer"/> to produce gallery
/// thumbnails — it is deliberately not placed in a live visual tree.
/// </remarks>
public partial class TemplatePagePreview : UserControl
{
    public TemplatePagePreview()
    {
        InitializeComponent();
    }

    private void InitializeComponent()
    {
        AvaloniaXamlLoader.Load(this);
    }
}
