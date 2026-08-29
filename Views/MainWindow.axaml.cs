using Avalonia.Controls;
using PdfEditorApp.ViewModels;

namespace PdfEditorApp.Views;

public partial class MainWindow : Window
{
    public MainWindow()
    {
        InitializeComponent();
        MainViewModel.StorageProvider = StorageProvider;
    }
}