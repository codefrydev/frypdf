using Avalonia.Controls;
using Avalonia.Input;
using PdfEditorApp.Core.Plugins.Marketplace;
using PdfEditorApp.ViewModels;

namespace PdfEditorApp.Views;

public partial class PluginsManagerPageView : UserControl
{
    public PluginsManagerPageView()
    {
        InitializeComponent();
        AttachedToVisualTree += (s, e) =>
        {
            if (DataContext is PluginsManagerViewModel vm)
            {
                var topLevel = TopLevel.GetTopLevel(this);
                if (topLevel != null)
                {
                    vm.StorageProvider = topLevel.StorageProvider;
                    vm.Clipboard = topLevel.Clipboard;
                }
            }
        };
    }

    private void OnInstalledCardPointerPressed(object? sender, PointerPressedEventArgs e)
    {
        if (sender is Border border && border.DataContext is PluginItemViewModel plugin && DataContext is PluginsManagerViewModel vm)
        {
            vm.SelectedInstalledPlugin = plugin;
        }
    }

    private void OnMarketplaceCardPointerPressed(object? sender, PointerPressedEventArgs e)
    {
        if (sender is Border border && border.DataContext is MarketplacePluginItem item && DataContext is PluginsManagerViewModel vm)
        {
            vm.SelectedMarketplacePlugin = item;
        }
    }
}
