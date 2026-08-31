using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Interactivity;
using PdfEditorApp.ViewModels;

namespace PdfEditorApp.Views;

public partial class MainWindow : Window
{
    public MainWindow()
    {
        InitializeComponent();
        MainViewModel.StorageProvider = StorageProvider;

        DataContextChanged += (s, e) =>
        {
            if (DataContext is MainViewModel vm)
            {
                vm.DataStudio.StorageProvider = StorageProvider;
            }
        };

        AddHandler(KeyDownEvent, (sender, e) =>
        {
            if (e.Source is TextBox) return;

            bool isCtrlOrCmd = e.KeyModifiers.HasFlag(KeyModifiers.Control) || e.KeyModifiers.HasFlag(KeyModifiers.Meta);
            if (isCtrlOrCmd && DataContext is MainViewModel vm)
            {
                switch (e.Key)
                {
                    case Key.OemPlus:
                    case Key.Add:
                        vm.ZoomInCommand.Execute(null);
                        e.Handled = true;
                        break;
                    case Key.OemMinus:
                    case Key.Subtract:
                        vm.ZoomOutCommand.Execute(null);
                        e.Handled = true;
                        break;
                    case Key.D0:
                    case Key.NumPad0:
                        vm.ResetZoomCommand.Execute(null);
                        e.Handled = true;
                        break;
                    case Key.D1:
                    case Key.NumPad1:
                        vm.FitToWidthCommand.Execute(null);
                        e.Handled = true;
                        break;
                    case Key.D9:
                    case Key.NumPad9:
                        vm.FitToPageCommand.Execute(null);
                        e.Handled = true;
                        break;
                }
            }
        }, RoutingStrategies.Tunnel);
    }
}