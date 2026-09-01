using Avalonia;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Interactivity;
using Avalonia.VisualTree;
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
                vm.BatchGeneration.StorageProvider = StorageProvider;
            }
        };

        AddHandler(KeyDownEvent, (sender, e) =>
        {
            if (DataContext is not MainViewModel vm) return;

            // Check if focus or event source is inside an active text input control
            var topLevel = TopLevel.GetTopLevel(this);
            var focused = topLevel?.FocusManager?.GetFocusedElement();

            bool isSourceTextBox = e.Source is TextBox ||
                                   e.Source is Avalonia.Controls.Presenters.TextPresenter ||
                                   (e.Source is Visual sv && sv.FindAncestorOfType<TextBox>() != null);

            bool isFocusedTextBox = focused is TextBox ||
                                    focused is Avalonia.Controls.Presenters.TextPresenter ||
                                    (focused is Visual fv && fv.FindAncestorOfType<TextBox>() != null);

            bool isInEditMode = vm.CurrentPage?.SelectedElement?.IsInEditMode == true;

            if (isSourceTextBox || isFocusedTextBox || isInEditMode)
            {
                return;
            }

            bool isCtrlOrCmd = e.KeyModifiers.HasFlag(KeyModifiers.Control) || e.KeyModifiers.HasFlag(KeyModifiers.Meta);

            // Handle Delete / Backspace when canvas elements are selected
            if ((e.Key == Key.Delete || e.Key == Key.Back) && !isCtrlOrCmd)
            {
                if (vm.CurrentPage != null && (vm.CurrentPage.SelectedElements.Count > 0 || vm.CurrentPage.SelectedElement != null))
                {
                    vm.Inspector.DeleteSelectedElementCommand.Execute(null);
                    e.Handled = true;
                    return;
                }
            }

            if (isCtrlOrCmd)
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