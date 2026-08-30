using Avalonia.Controls;
using Avalonia.Input;
using PdfEditorApp.ViewModels;

namespace PdfEditorApp.Views.Dialogs;

public partial class AboutDialog : UserControl
{
    public AboutDialog()
    {
        InitializeComponent();
    }

    private void OnBackdropPointerPressed(object? sender, PointerPressedEventArgs e)
    {
        if (DataContext is MainViewModel vm)
        {
            vm.CloseAboutDialog();
        }
    }
}
